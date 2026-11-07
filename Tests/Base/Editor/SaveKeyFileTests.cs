using System;
using System.IO;
using NUnit.Framework;

namespace RippleGame.Base.Tests
{
    public sealed class SaveKeyFileTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "RippleKeyTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        [Test]
        public void Render_ContainsKeyAndClass()
        {
            var source = SaveKeyFile.Render("abc+/=");
            Assert.That(source, Does.Contain("static class GameSaveKey"));
            Assert.That(source, Does.Contain("public const string Value = \"abc+/=\";"));
        }

        [Test]
        public void Write_CreatesFileNamedAfterClass()
        {
            var path = SaveKeyFile.Write(directory, "abc");
            Assert.That(Path.GetFileName(path), Is.EqualTo("GameSaveKey.cs"));
            Assert.That(File.ReadAllText(path), Does.Contain("\"abc\""));
        }

        [Test]
        public void Write_NeverOverwritesExistingKey()
        {
            var path = SaveKeyFile.Write(directory, "first");
            Assert.Throws<IOException>(() => SaveKeyFile.Write(directory, "second"));
            Assert.That(File.ReadAllText(path), Does.Contain("\"first\""));
        }

        [Test]
        public void WriteNew_NoKeyYet_Creates()
        {
            var path = SaveKeyFile.WriteNew(directory, directory, "abc");

            Assert.That(File.ReadAllText(path), Does.Contain("\"abc\""));
        }

        [Test]
        public void WriteNew_KeyInAnotherFolder_RefusesAndNamesIt()
        {
            var other = Directory.CreateDirectory(Path.Combine(directory, "Scripts", "Game")).FullName;
            var existing = SaveKeyFile.Write(other, "real");

            var error = Assert.Throws<IOException>(() => SaveKeyFile.WriteNew(directory, directory, "second"));

            Assert.That(error.Message, Does.Contain("GameSaveKey.cs").And.Contain("Game"));
            Assert.That(File.Exists(Path.Combine(directory, "GameSaveKey.cs")), Is.False);
            Assert.That(File.ReadAllText(existing), Does.Contain("\"real\""));
        }

        [Test]
        public void WriteNew_KeyInParentFolder_Refuses()
        {
            SaveKeyFile.Write(directory, "real");
            var child = Directory.CreateDirectory(Path.Combine(directory, "Child")).FullName;

            Assert.Throws<IOException>(() => SaveKeyFile.WriteNew(directory, child, "second"));
            Assert.That(File.Exists(Path.Combine(child, "GameSaveKey.cs")), Is.False);
        }
    }
}
