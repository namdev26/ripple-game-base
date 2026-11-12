using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RippleGame.Sdk.Editor.Tests
{
    public sealed class SdkManifestEditorTests
    {
        const string MaxCatalog = @"{ ""entries"": [
  { ""id"": ""edm"", ""displayName"": ""External Dependency Manager"", ""packageName"": ""com.google.edm"", ""version"": ""1.0.0"",
    ""fileName"": ""com.google.edm-1.0.0.tgz"", ""dependsOn"": [], ""legacyPaths"": [""Assets/ExternalDependencyManager""] },
  { ""id"": ""max"", ""displayName"": ""AppLovin MAX"", ""packageName"": ""com.applovin.mediation.ads"", ""version"": ""8.5.1"",
    ""fileName"": ""com.applovin.mediation.ads-8.5.1.tgz"", ""dependsOn"": [""edm""], ""legacyPaths"": [""Assets/MaxSdk""] }
] }";

        string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "ripple-sdk-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        static string Manifest(string dependencies) => "{\"dependencies\":{" + dependencies + "}}";

        // --- SdkManifestEditor ---

        [Test]
        public void SetDependency_KeepsOtherKeysAndOrder()
        {
            var m = "{\"dependencies\":{\"a\":\"1.0.0\"},\"scopedRegistries\":[],\"testables\":[\"x\"]}";
            var r = SdkManifestEditor.SetDependency(m, "com.applovin.mediation.ads", "file:RippleSdks/max.tgz");
            Assert.That(JObject.Parse(r).Properties().Select(p => p.Name), Is.EqualTo(new[] { "dependencies", "scopedRegistries", "testables" }));
            Assert.That(SdkManifestEditor.GetDependency(r, "a"), Is.EqualTo("1.0.0"));
            Assert.That(SdkManifestEditor.GetDependency(r, "com.applovin.mediation.ads"), Is.EqualTo("file:RippleSdks/max.tgz"));
        }

        [Test]
        public void SetDependency_ExistingKey_ReplacesValueInPlace()
        {
            var r = SdkManifestEditor.SetDependency(Manifest("\"a\":\"1\",\"b\":\"2\",\"c\":\"3\""), "b", "9");
            Assert.That(((JObject)JObject.Parse(r)["dependencies"]).Properties().Select(p => p.Name), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(SdkManifestEditor.GetDependency(r, "b"), Is.EqualTo("9"));
        }

        [Test]
        public void SetDependency_NoDependenciesObject_CreatesIt()
        {
            var r = SdkManifestEditor.SetDependency("{\"testables\":[]}", "a", "1");
            Assert.That(SdkManifestEditor.GetDependency(r, "a"), Is.EqualTo("1"));
        }

        [Test]
        public void Output_IsIndentedWithTwoSpaces()
        {
            var r = SdkManifestEditor.SetDependency(Manifest("\"a\":\"1\""), "b", "2");
            Assert.That(r, Does.Contain("\n  \"dependencies\": {\n    \"a\": \"1\""));
        }

        [Test]
        public void Output_KeepsDateLikeStringsUntouched()
        {
            var r = SdkManifestEditor.SetDependency("{\"dependencies\":{},\"note\":\"2020-01-01T00:00:00Z\"}", "a", "1");
            Assert.That(r, Does.Contain("\"note\": \"2020-01-01T00:00:00Z\""));
        }

        [Test]
        public void RemoveDependency_RemovesOnlyThatKey()
        {
            var r = SdkManifestEditor.RemoveDependency(Manifest("\"a\":\"1\",\"b\":\"2\""), "a");
            Assert.That(SdkManifestEditor.GetDependency(r, "a"), Is.Null);
            Assert.That(SdkManifestEditor.GetDependency(r, "b"), Is.EqualTo("2"));
        }

        [Test]
        public void GetDependency_Absent_ReturnsNull()
        {
            Assert.That(SdkManifestEditor.GetDependency(Manifest("\"a\":\"1\""), "zzz"), Is.Null);
            Assert.That(SdkManifestEditor.GetDependency("{}", "a"), Is.Null);
        }

        // --- SdkStatusResolver ---

        [Test]
        public void Resolve_NoDependency_IsNotInstalled()
        {
            var info = SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), Manifest(""), root);
            Assert.That(info.State, Is.EqualTo(SdkState.NotInstalled));
            Assert.That(info.InstalledReference, Is.Null);
        }

        [Test]
        public void Resolve_ExactTarballReference_IsInstalled()
        {
            var m = Manifest("\"com.applovin.mediation.ads\":\"file:RippleSdks/com.applovin.mediation.ads-8.5.1.tgz\"");
            var info = SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), m, root);
            Assert.That(info.State, Is.EqualTo(SdkState.Installed));
        }

        [Test]
        public void Resolve_SemverReference_IsWrongVersion()
        {
            var info = SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), Manifest("\"com.applovin.mediation.ads\":\"8.5.0\""), root);
            Assert.That(info.State, Is.EqualTo(SdkState.WrongVersion));
            Assert.That(info.InstalledReference, Is.EqualTo("8.5.0"));
        }

        [Test]
        public void Resolve_OtherTarball_IsWrongVersion()
        {
            var m = Manifest("\"com.applovin.mediation.ads\":\"file:RippleSdks/com.applovin.mediation.ads-8.5.0.tgz\"");
            Assert.That(SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), m, root).State, Is.EqualTo(SdkState.WrongVersion));
        }

        [Test]
        public void Resolve_LegacyFolderPresent_IsLegacy()
        {
            Directory.CreateDirectory(Path.Combine(root, "Assets", "MaxSdk"));
            var info = SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), Manifest(""), root);
            Assert.That(info.State, Is.EqualTo(SdkState.Legacy));
            Assert.That(info.FoundLegacyPaths, Is.EqualTo(new[] { "Assets/MaxSdk" }));
        }

        [Test]
        public void Resolve_LegacyFolderPresent_WinsOverInstalledReference()
        {
            Directory.CreateDirectory(Path.Combine(root, "Assets", "MaxSdk"));
            var m = Manifest("\"com.applovin.mediation.ads\":\"file:RippleSdks/com.applovin.mediation.ads-8.5.1.tgz\"");
            Assert.That(SdkStatusResolver.Resolve(SdkCatalog.Parse(MaxCatalog).Find("max"), m, root).State, Is.EqualTo(SdkState.Legacy));
        }

        // --- SdkPreBuildValidator ---

        [Test]
        public void PreBuildCheck_WrongVersion_ReturnsFixMessage()
        {
            var message = SdkPreBuildValidator.Check(SdkCatalog.Parse(MaxCatalog), Manifest("\"com.applovin.mediation.ads\":\"8.5.0\""), root);
            Assert.That(message, Is.EqualTo(
                "SDK 'AppLovin MAX' is installed at 8.5.0 but this RIPPLE Game Base version needs 8.5.1. " +
                "Open RIPPLE Game > Open Hub > SDK and press Fix."));
        }

        [Test]
        public void PreBuildCheck_AllInstalledOrAbsent_ReturnsNull()
        {
            var catalog = SdkCatalog.Parse(MaxCatalog);
            Assert.That(SdkPreBuildValidator.Check(catalog, Manifest(""), root), Is.Null);
            var installed = Manifest(
                "\"com.google.edm\":\"file:RippleSdks/com.google.edm-1.0.0.tgz\"," +
                "\"com.applovin.mediation.ads\":\"file:RippleSdks/com.applovin.mediation.ads-8.5.1.tgz\"");
            Assert.That(SdkPreBuildValidator.Check(catalog, installed, root), Is.Null);
        }
    }
}
