using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RippleGame.Sdk.Editor.Tests
{
    public sealed class SdkInstallerTests
    {
        const string RefPrefix = "file:RippleSdks/";

        sealed class FakeDownloader : ISdkDownloader
        {
            public readonly Dictionary<string, byte[]> Content = new Dictionary<string, byte[]>();
            public readonly List<string> Requested = new List<string>();
            public string ThrowOnUrl;

            public Task DownloadAsync(string url, string destinationPath)
            {
                Requested.Add(url);
                if (url == ThrowOnUrl) throw new IOException("network down");
                File.WriteAllBytes(destinationPath, Content[url]);
                return Task.CompletedTask;
            }
        }

        string root;
        string manifestPath;
        FakeDownloader downloader;
        SdkCatalog catalog;

        static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

        static string Sha(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }

        static JObject EntryJson(string id, string package, string version, string[] dependsOn, byte[] content, JObject unityDependencies = null) =>
            new JObject
            {
                ["id"] = id,
                ["displayName"] = "Name " + id,
                ["packageName"] = package,
                ["version"] = version,
                ["fileName"] = package + "-" + version + ".tgz",
                ["url"] = "https://example.invalid/" + package + "-" + version + ".tgz",
                ["sha256"] = Sha(content),
                ["dependsOn"] = new JArray(dependsOn),
                ["legacyPaths"] = new JArray("Assets/" + id),
                ["unityDependencies"] = unityDependencies ?? new JObject(),
            };

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "ripple-sdk-tests-" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(root, "Packages"));
            manifestPath = Path.Combine(root, "Packages", "manifest.json");
            WriteManifest("{\"dependencies\":{\"com.unity.modules.ai\":\"1.0.0\"},\"scopedRegistries\":[]}");

            downloader = new FakeDownloader();
            var entries = new JArray();
            void Add(string id, string package, string version, string[] dependsOn, string content, JObject unityDependencies = null)
            {
                var json = EntryJson(id, package, version, dependsOn, Bytes(content), unityDependencies);
                entries.Add(json);
                downloader.Content[(string)json["url"]] = Bytes(content);
            }
            Add("edm", "com.google.edm", "1.0.0", new string[0], "edm-bytes");
            Add("firebase-app", "com.google.firebase.app", "2.0.0", new[] { "edm" }, "app-bytes");
            Add("firebase-remote-config", "com.google.firebase.remote-config", "2.0.0", new[] { "firebase-app", "edm" }, "rc-bytes");
            Add("max", "com.applovin.mediation.ads", "8.5.1", new[] { "edm" }, "max-bytes", new JObject { ["com.unity.ugui"] = "2.0.0" });
            catalog = SdkCatalog.Parse(new JObject { ["entries"] = entries }.ToString());
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(manifestPath)) File.SetAttributes(manifestPath, FileAttributes.Normal);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        SdkInstaller NewInstaller() => new SdkInstaller(catalog, root, downloader);
        void WriteManifest(string json) => File.WriteAllText(manifestPath, json);
        string ReadManifest() => File.ReadAllText(manifestPath);
        string SdkFolder => Path.Combine(root, "Packages", "RippleSdks");
        string Dependency(string package) => SdkManifestEditor.GetDependency(ReadManifest(), package);
        string[] FilesInSdkFolder(string pattern = "*") =>
            Directory.Exists(SdkFolder) ? Directory.GetFiles(SdkFolder, pattern).Select(Path.GetFileName).OrderBy(n => n).ToArray() : new string[0];

        [Test]
        public async Task Install_CreatesRippleSdksFolderAndDependenciesFirst()
        {
            Assert.That(Directory.Exists(SdkFolder), Is.False);

            var result = await NewInstaller().InstallAsync("firebase-remote-config");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(downloader.Requested, Is.EqualTo(new[]
            {
                "https://example.invalid/com.google.edm-1.0.0.tgz",
                "https://example.invalid/com.google.firebase.app-2.0.0.tgz",
                "https://example.invalid/com.google.firebase.remote-config-2.0.0.tgz",
            }));
            Assert.That(FilesInSdkFolder(), Is.EqualTo(new[]
            {
                "com.google.edm-1.0.0.tgz", "com.google.firebase.app-2.0.0.tgz", "com.google.firebase.remote-config-2.0.0.tgz",
            }));
            Assert.That(Dependency("com.google.edm"), Is.EqualTo(RefPrefix + "com.google.edm-1.0.0.tgz"));
            Assert.That(Dependency("com.google.firebase.remote-config"), Is.EqualTo(RefPrefix + "com.google.firebase.remote-config-2.0.0.tgz"));
            Assert.That(Dependency("com.unity.modules.ai"), Is.EqualTo("1.0.0"));
            Assert.That(JObject.Parse(ReadManifest()).Properties().Select(p => p.Name), Is.EqualTo(new[] { "dependencies", "scopedRegistries" }));
        }

        [Test]
        public async Task Install_SkipsEntriesAlreadyInstalled()
        {
            WriteManifest("{\"dependencies\":{\"com.google.edm\":\"" + RefPrefix + "com.google.edm-1.0.0.tgz\"}}");

            var result = await NewInstaller().InstallAsync("firebase-app");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(downloader.Requested, Is.EqualTo(new[] { "https://example.invalid/com.google.firebase.app-2.0.0.tgz" }));
        }

        [Test]
        public async Task Install_EverythingAlreadyInstalled_SucceedsWithoutTouchingManifest()
        {
            WriteManifest("{\"dependencies\":{\"com.google.edm\":\"" + RefPrefix + "com.google.edm-1.0.0.tgz\"}}");
            var before = File.ReadAllBytes(manifestPath);

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(downloader.Requested, Is.Empty);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public async Task Install_ShaMismatch_DeletesFileAndLeavesManifest()
        {
            var before = File.ReadAllBytes(manifestPath);
            downloader.Content["https://example.invalid/com.google.edm-1.0.0.tgz"] = Bytes("tampered");

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("SHA-256"));
            Assert.That(FilesInSdkFolder(), Is.Empty);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public async Task Install_DownloadThrows_NoPartFileAndManifestUnchanged()
        {
            var before = File.ReadAllBytes(manifestPath);
            // edm downloads fine, then firebase-app fails: nothing from edm may survive either.
            downloader.ThrowOnUrl = "https://example.invalid/com.google.firebase.app-2.0.0.tgz";

            var result = await NewInstaller().InstallAsync("firebase-app");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("network down"));
            Assert.That(FilesInSdkFolder("*.part"), Is.Empty);
            Assert.That(FilesInSdkFolder(), Is.Empty);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public async Task Install_ManifestWriteFails_RollsBackTarballsAndLeavesManifest()
        {
            var before = File.ReadAllBytes(manifestPath);
            File.SetAttributes(manifestPath, FileAttributes.ReadOnly);

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.False);
            Assert.That(FilesInSdkFolder("*.part"), Is.Empty);
            Assert.That(FilesInSdkFolder(), Is.Empty);
            File.SetAttributes(manifestPath, FileAttributes.Normal);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public async Task Install_WrongVersion_ReplacesReferenceAndDeletesOldTarball()
        {
            Directory.CreateDirectory(SdkFolder);
            var oldTarball = Path.Combine(SdkFolder, "com.google.edm-0.9.0.tgz");
            File.WriteAllText(oldTarball, "old");
            WriteManifest("{\"dependencies\":{\"com.google.edm\":\"" + RefPrefix + "com.google.edm-0.9.0.tgz\"}}");

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(Dependency("com.google.edm"), Is.EqualTo(RefPrefix + "com.google.edm-1.0.0.tgz"));
            Assert.That(File.Exists(oldTarball), Is.False);
            Assert.That(FilesInSdkFolder(), Is.EqualTo(new[] { "com.google.edm-1.0.0.tgz" }));
        }

        [Test]
        public async Task Install_WrongVersionSemver_OverwritesDependency()
        {
            WriteManifest("{\"dependencies\":{\"com.google.edm\":\"0.9.0\"}}");

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(Dependency("com.google.edm"), Is.EqualTo(RefPrefix + "com.google.edm-1.0.0.tgz"));
        }

        [Test]
        public async Task Install_AddsMissingUnityDependency_KeepsExistingVersion()
        {
            var first = await NewInstaller().InstallAsync("max");
            Assert.That(first.Success, Is.True, first.Message);
            Assert.That(Dependency("com.unity.ugui"), Is.EqualTo("2.0.0"));

            // Reinstall from scratch with a newer ugui already present: it must be left alone.
            WriteManifest("{\"dependencies\":{\"com.unity.ugui\":\"2.1.0\"}}");
            Directory.Delete(SdkFolder, true);

            var second = await NewInstaller().InstallAsync("max");
            Assert.That(second.Success, Is.True, second.Message);
            Assert.That(Dependency("com.unity.ugui"), Is.EqualTo("2.1.0"));
            Assert.That(Dependency("com.applovin.mediation.ads"), Is.EqualTo(RefPrefix + "com.applovin.mediation.ads-8.5.1.tgz"));
        }

        [Test]
        public async Task Install_LegacyFolderPresent_FailsWithoutDownloading()
        {
            Directory.CreateDirectory(Path.Combine(root, "Assets", "edm"));
            var before = File.ReadAllBytes(manifestPath);

            var result = await NewInstaller().InstallAsync("edm");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Assets/edm"));
            Assert.That(downloader.Requested, Is.Empty);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public async Task Install_UnknownId_Fails()
        {
            var result = await NewInstaller().InstallAsync("nope");
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("nope"));
        }

        [Test]
        public async Task Remove_WithInstalledDependent_Fails()
        {
            await NewInstaller().InstallAsync("max");
            var before = File.ReadAllBytes(manifestPath);

            var result = await NewInstaller().RemoveAsync("edm");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("max"));
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
            Assert.That(FilesInSdkFolder(), Does.Contain("com.google.edm-1.0.0.tgz"));
        }

        [Test]
        public async Task Remove_Installed_RemovesDependencyAndTarball_KeepsUnityDependency()
        {
            await NewInstaller().InstallAsync("max");

            var result = await NewInstaller().RemoveAsync("max");

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(Dependency("com.applovin.mediation.ads"), Is.Null);
            Assert.That(Dependency("com.unity.ugui"), Is.EqualTo("2.0.0"));
            Assert.That(Dependency("com.google.edm"), Is.Not.Null);
            Assert.That(FilesInSdkFolder(), Is.EqualTo(new[] { "com.google.edm-1.0.0.tgz" }));
        }

        [Test]
        public async Task Remove_NotInstalled_SucceedsWithoutChange()
        {
            var before = File.ReadAllBytes(manifestPath);
            var result = await NewInstaller().RemoveAsync("max");
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(before));
        }

        [Test]
        public void SdkHash_ReturnsLowercaseSha256()
        {
            var path = Path.Combine(root, "x.bin");
            File.WriteAllBytes(path, Bytes("abc"));
            Assert.That(SdkHash.Sha256Hex(path), Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }
    }
}
