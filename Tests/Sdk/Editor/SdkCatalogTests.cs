using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace RippleGame.Sdk.Editor.Tests
{
    public sealed class SdkCatalogTests
    {
        const string Fixture = @"{ ""entries"": [
  { ""id"": ""edm"", ""packageName"": ""com.google.edm"", ""version"": ""1.0.0"", ""dependsOn"": [] },
  { ""id"": ""firebase-app"", ""packageName"": ""com.google.firebase.app"", ""version"": ""2.0.0"", ""dependsOn"": [""edm""] },
  { ""id"": ""firebase-remote-config"", ""packageName"": ""com.google.firebase.remote-config"", ""version"": ""2.0.0"", ""dependsOn"": [""firebase-app"", ""edm""] },
  { ""id"": ""max"", ""packageName"": ""com.applovin.mediation.ads"", ""version"": ""8.5.1"", ""dependsOn"": [""edm""],
    ""unityDependencies"": { ""com.unity.ugui"": ""2.0.0"" } }
] }";

        const string CycleFixture = @"{ ""entries"": [
  { ""id"": ""a"", ""dependsOn"": [""b""] },
  { ""id"": ""b"", ""dependsOn"": [""a""] }
] }";

        const string MaxCatalog = @"{ ""entries"": [
  { ""id"": ""max"", ""packageName"": ""com.applovin.mediation.ads"", ""version"": ""8.5.1"", ""dependsOn"": [] }
] }";

        static IEnumerable<KeyValuePair<string, string>> Pair(string path, string json) =>
            new[] { new KeyValuePair<string, string>(path, json) };

        static string Define(string name, string expression) =>
            "{\"versionDefines\":[{\"name\":\"" + name + "\",\"expression\":\"" + expression + "\",\"define\":\"X\"}]}";

        [Test]
        public void InstallOrder_PutsDependenciesFirst()
        {
            var c = SdkCatalog.Parse(Fixture);
            Assert.That(c.InstallOrder("firebase-remote-config").Select(e => e.Id),
                Is.EqualTo(new[] { "edm", "firebase-app", "firebase-remote-config" }));
        }

        [Test]
        public void InstallOrder_Cycle_Throws() =>
            Assert.Throws<InvalidOperationException>(() => SdkCatalog.Parse(CycleFixture).InstallOrder("a"));

        [Test]
        public void DependentsOf_Edm_ListsDirectAndIndirect()
        {
            var c = SdkCatalog.Parse(Fixture);
            Assert.That(c.DependentsOf("edm").Select(e => e.Id),
                Is.EquivalentTo(new[] { "firebase-app", "firebase-remote-config", "max" }));
            Assert.That(c.DependentsOf("firebase-app").Select(e => e.Id), Is.EqualTo(new[] { "firebase-remote-config" }));
        }

        [Test]
        public void Parse_ReadsUnityDependencies_AndDefaultsToEmpty()
        {
            var c = SdkCatalog.Parse(Fixture);
            Assert.That(c.Find("max").UnityDependencies, Is.EqualTo(new Dictionary<string, string> { { "com.unity.ugui", "2.0.0" } }));
            Assert.That(c.Find("edm").UnityDependencies, Is.Empty);
        }

        [Test]
        public void ShippedCatalog_MaxRequiresUgui()
        {
            Assert.That(SdkCatalog.LoadDefault().Find("max").UnityDependencies["com.unity.ugui"], Is.EqualTo("2.0.0"));
        }

        [Test]
        public void Audit_FlagsExpressionThatIsNotExactVersion()
        {
            var catalog = SdkCatalog.Parse(MaxCatalog);
            Assert.That(SdkVersionDefineAudit.FindMismatches(catalog, Pair("a.asmdef", Define("com.applovin.mediation.ads", "8.5.0"))), Has.Count.EqualTo(1));
            Assert.That(SdkVersionDefineAudit.FindMismatches(catalog, Pair("a.asmdef", Define("com.applovin.mediation.ads", "[8.5.1]"))), Is.Empty);
        }

        [Test]
        public void Audit_IgnoresNonCatalogPackages()
        {
            var catalog = SdkCatalog.Parse(MaxCatalog);
            Assert.That(SdkVersionDefineAudit.FindMismatches(catalog, Pair("a.asmdef", Define("com.unity.test-framework", "1.1.0"))), Is.Empty);
        }

        [Test]
        public void PackageAsmdefs_MatchShippedCatalog()
        {
            Assert.That(SdkVersionDefineAudit.FindMismatches(SdkCatalog.LoadDefault(), AllPackageAsmdefs()), Is.Empty);
        }

        [Test]
        public void ShippedCatalog_EveryEntryHasSha256AndFileName()
        {
            var entries = SdkCatalog.LoadDefault().Entries;
            Assert.That(entries, Is.Not.Empty);
            foreach (var e in entries)
            {
                Assert.That(e.Sha256, Does.Match("^[0-9a-f]{64}$"), e.Id);
                Assert.That(e.FileName, Is.Not.Empty, e.Id);
            }
        }

        static List<KeyValuePair<string, string>> AllPackageAsmdefs()
        {
            var root = PackageInfo.FindForAssembly(typeof(SdkCatalog).Assembly).resolvedPath;
            return Scan(root).Select(p => new KeyValuePair<string, string>(p, File.ReadAllText(p))).ToList();
        }

        // Skips "~" folders (Artifacts~, Samples~) and hidden folders (.git, .claude, .superpowers).
        static IEnumerable<string> Scan(string dir)
        {
            foreach (var file in Directory.GetFiles(dir, "*.asmdef"))
                yield return file;
            foreach (var sub in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(sub);
                if (name.EndsWith("~") || name.StartsWith(".")) continue;
                foreach (var file in Scan(sub))
                    yield return file;
            }
        }
    }
}
