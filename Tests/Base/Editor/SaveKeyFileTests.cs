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
    }
}
