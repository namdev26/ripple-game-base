using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace RippleGame.Base.Tests
{
    public sealed class PackageUpdateTests
    {
        [Test]
        public void Latest_ComparesVersionsAsNumbers()
        {
            var release = PackageUpdateLogic.ParseLatest("a\trefs/tags/v0.9.0\nb\trefs/tags/v0.10.0\nc\trefs/tags/v0.1.0\n");
            Assert.That(release.Tag, Is.EqualTo("v0.10.0"));
        }

        [Test]
        public void Latest_IgnoresPrereleasesAndOtherTags()
        {
            var output = "a\trefs/tags/v0.2.0\r\nb\trefs/tags/v9.0.0-beta\r\nc\trefs/tags/nightly\r\nd\trefs/tags/v8.0.0^{}\r\ne\trefs/heads/v7.0.0\r\n";
            Assert.That(PackageUpdateLogic.ParseLatest(output).Tag, Is.EqualTo("v0.2.0"));
        }

        [Test]
        public void Latest_AcceptsTagWithoutPrefix()
        {
            Assert.That(PackageUpdateLogic.ParseLatest("a\trefs/tags/1.2.3\n").Tag, Is.EqualTo("1.2.3"));
        }

        [Test]
        public void Latest_IsNullWithoutReleases()
        {
            Assert.That(PackageUpdateLogic.ParseLatest(string.Empty), Is.Null);
            Assert.That(PackageUpdateLogic.ParseLatest("a\trefs/tags/nightly\n"), Is.Null);
        }

        [TestCase("0.2.0", "a\trefs/tags/v0.3.0", true)]
        [TestCase("0.2.0", "a\trefs/tags/v0.10.0", true)]
        [TestCase("0.2.0", "a\trefs/tags/v0.2.0", false)]
        [TestCase("0.3.0", "a\trefs/tags/v0.2.0", false)]
        [TestCase("not-a-version", "a\trefs/tags/v0.2.0", false)]
        public void IsNewer_ComparesInstalledVersion(string installed, string output, bool expected)
        {
            Assert.That(PackageUpdateLogic.IsNewer(installed, PackageUpdateLogic.ParseLatest(output)), Is.EqualTo(expected));
        }

        [Test]
        public void IsNewer_IsFalseWithoutRelease()
        {
            Assert.That(PackageUpdateLogic.IsNewer("0.2.0", null), Is.False);
        }

        [TestCase("com.ripplegame.base@https://github.com/a/b.git#v0.2.0", "https://github.com/a/b.git#v0.2.0")]
        [TestCase("com.ripplegame.base@git@github.com:a/b.git#v0.2.0", "git@github.com:a/b.git#v0.2.0")]
        public void SourceOf_KeepsEverythingAfterPackageName(string packageId, string expected)
        {
            Assert.That(PackageUpdateLogic.SourceOf(packageId), Is.EqualTo(expected));
        }

        [TestCase("https://github.com/a/b.git#v0.2.0", "https://github.com/a/b.git")]
        [TestCase("https://github.com/a/b.git", "https://github.com/a/b.git")]
        [TestCase("git@github.com:a/b.git#main", "git@github.com:a/b.git")]
        [TestCase("git+ssh://git@github.com/a/b.git?path=/sub#main", "ssh://git@github.com/a/b.git")]
        [TestCase("git+file:///D:/repo/b#v1.0.0", "file:///D:/repo/b")]
        public void RemoteUrl_IsWhatGitCanRead(string source, string expected)
        {
            Assert.That(PackageUpdateLogic.RemoteUrl(source), Is.EqualTo(expected));
        }

        [TestCase("https://github.com/a/b.git#v0.2.0", "https://github.com/a/b.git#v0.3.0")]
        [TestCase("https://github.com/a/b.git", "https://github.com/a/b.git#v0.3.0")]
        [TestCase("git+ssh://git@github.com/a/b.git?path=/sub#main", "git+ssh://git@github.com/a/b.git?path=/sub#v0.3.0")]
        public void InstallUrl_PinsTheTag(string source, string expected)
        {
            Assert.That(PackageUpdateLogic.InstallUrl(source, "v0.3.0"), Is.EqualTo(expected));
        }

        [TestCase("https://github.com/a/b.git#v0.2.0", "https://github.com/a/b/blob/v0.3.0/CHANGELOG.md")]
        [TestCase("https://github.com/a/b", "https://github.com/a/b/blob/v0.3.0/CHANGELOG.md")]
        [TestCase("git@github.com:a/b.git#v0.2.0", null)]
        [TestCase("git+file:///D:/repo/b#v0.2.0", null)]
        public void ChangelogUrl_OnlyForWebRepositories(string source, string expected)
        {
            Assert.That(PackageUpdateLogic.ChangelogUrl(source, "v0.3.0"), Is.EqualTo(expected));
        }

        [Test]
        public void FetchLatest_ReadsLightweightAndAnnotatedTagsFromRealRepository()
        {
            var repository = Path.Combine(Path.GetTempPath(), "RippleUpdateTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(repository);
            try
            {
                Git(repository, "init");
                Git(repository, "-c user.name=t -c user.email=t@t -c commit.gpgsign=false commit --allow-empty -m x");
                Git(repository, "tag v0.2.0");
                Git(repository, "-c user.name=t -c user.email=t@t -c tag.gpgsign=false tag -a v0.3.0 -m x");
                Git(repository, "tag nightly");

                var release = PackageUpdateGit.FetchLatest(repository);

                Assert.That(release.Tag, Is.EqualTo("v0.3.0"));
            }
            finally
            {
                DeleteRepository(repository);
            }
        }

        [Test]
        public void FetchLatest_ReportsUnreadableRepository()
        {
            var missing = Path.Combine(Path.GetTempPath(), "RippleUpdateMissing_" + Guid.NewGuid().ToString("N"));
            Assert.Throws<IOException>(() => PackageUpdateGit.FetchLatest(missing));
        }

        private static void Git(string directory, string arguments)
        {
            var info = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = directory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using (var process = Process.Start(info))
            {
                var error = process.StandardError.ReadToEndAsync();
                process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                Assert.That(process.ExitCode, Is.EqualTo(0), "git " + arguments + ": " + error.Result);
            }
        }

        private static void DeleteRepository(string directory)
        {
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(directory, true);
        }
    }
}
