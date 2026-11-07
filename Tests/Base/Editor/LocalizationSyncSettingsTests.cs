using System;
using System.IO;
using NUnit.Framework;

namespace RippleGame.Base.Tests
{
    public sealed class LocalizationSyncSettingsTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "RippleSyncSettings_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "ProjectSettings"));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(root, true);

        [Test]
        public void Load_NoFile_ReturnsEmpty()
        {
            Assert.That(LocalizationSyncSettings.Load(root), Is.Empty);
        }

        [Test]
        public void Save_ThenLoad_ReturnsUrl()
        {
            LocalizationSyncSettings.Save(root, "https://docs.google.com/spreadsheets/d/ABC/edit");

            Assert.That(LocalizationSyncSettings.Load(root), Is.EqualTo("https://docs.google.com/spreadsheets/d/ABC/edit"));
            Assert.That(File.Exists(Path.Combine(root, "ProjectSettings", "RippleGameLocalization.json")), Is.True);
        }

        [Test]
        public void Load_DamagedFile_ReturnsEmpty()
        {
            File.WriteAllText(Path.Combine(root, "ProjectSettings", "RippleGameLocalization.json"), "{not json");

            Assert.That(LocalizationSyncSettings.Load(root), Is.Empty);
        }
    }
}
