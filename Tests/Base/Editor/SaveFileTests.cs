using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RippleGame.Save;

namespace RippleGame.Base.Tests
{
    [Serializable]
    internal sealed class PlayerData
    {
        public int score;
        public string name = "default";
    }

    [Serializable]
    internal sealed class OtherData
    {
        public int level;
    }

    internal sealed class FlakyStorage : ISaveStorage
    {
        private readonly ISaveStorage inner;
        private int remaining = -1;

        public FlakyStorage(ISaveStorage inner) => this.inner = inner;

        /// <summary>The mutation with this zero-based index, and every later one, fails.</summary>
        public void FailFrom(int mutationIndex) => remaining = mutationIndex;
        public void Heal() => remaining = -1;

        private bool ShouldFail()
        {
            if (remaining < 0) return false;
            if (remaining == 0) return true;
            remaining--;
            return false;
        }

        public bool Exists(string file) => inner.Exists(file);
        public byte[] ReadOrNull(string file) => inner.ReadOrNull(file);

        public void Write(string file, byte[] data)
        {
            if (ShouldFail())
            {
                // A killed process can leave a partial file behind.
                inner.Write(file, data.Take(data.Length / 2).ToArray());
                throw new IOException("Simulated write failure.");
            }
            inner.Write(file, data);
        }

        public void Move(string from, string to)
        {
            if (ShouldFail()) throw new IOException("Simulated move failure.");
            inner.Move(from, to);
        }

        public void Delete(string file)
        {
            if (ShouldFail()) throw new IOException("Simulated delete failure.");
            inner.Delete(file);
        }
    }

    public sealed class SaveFileTests
    {
        private const string Name = "main";
        private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());
        private static readonly string OtherKey = Convert.ToBase64String(Enumerable.Range(101, 32).Select(i => (byte)i).ToArray());

        private MemorySaveStorage storage;

        [SetUp]
        public void SetUp() => storage = new MemorySaveStorage();

        private SaveFile Open(ISaveStorage target = null, string key = null, float autoFlush = 0f) =>
            new SaveFile(Name, key ?? Key, new SaveOptions { Storage = target ?? storage, AutoFlushSeconds = autoFlush });

        private void WriteScore(int score, ISaveStorage target = null)
        {
            using (var save = Open(target))
            {
                save.Section<PlayerData>("player").Update(d => d.score = score);
                Assert.That(save.Flush(), Is.True);
            }
        }

        private int ReadScore(out List<SaveProblem> problems, ISaveStorage target = null, string key = null)
        {
            var seen = new List<SaveProblem>();
            using (var save = Open(target, key))
            {
                save.ProblemRaised += seen.Add;
                var score = save.Section<PlayerData>("player").Value.score;
                problems = seen;
                return score;
            }
        }

        private void FlipByte(string file, int index)
        {
            var data = storage.ReadOrNull(file);
            data[index] ^= 0xFF;
            storage.Write(file, data);
        }

        [Test]
        public void Section_WithoutSavedData_ReturnsDefaults()
        {
            using (var save = Open())
                Assert.That(save.Section<PlayerData>("player").Value.name, Is.EqualTo("default"));
        }

        [Test]
        public void Flush_ThenReopen_RestoresEverySection()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.score = 42);
                save.Section<OtherData>("other").Update(d => d.level = 7);
                save.Flush();
            }

            using (var save = Open())
            {
                Assert.That(save.Section<PlayerData>("player").Value.score, Is.EqualTo(42));
                Assert.That(save.Section<OtherData>("other").Value.level, Is.EqualTo(7));
            }
        }

        [Test]
        public void Section_SameKeyAndType_ReturnsSameObject()
        {
            using (var save = Open())
                Assert.That(save.Section<PlayerData>("player"), Is.SameAs(save.Section<PlayerData>("player")));
        }

        [Test]
        public void Section_SameKeyDifferentType_Throws()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player");
                Assert.Throws<InvalidOperationException>(() => save.Section<OtherData>("player"));
            }
        }

        [Test]
        public void StoredBytes_DoNotContainPlainText()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.name = "visible-secret");
                save.Flush();
            }

            var text = Encoding.UTF8.GetString(storage.ReadOrNull("main.sav"));
            Assert.That(text, Does.Not.Contain("visible-secret"));
            Assert.That(text, Does.Not.Contain("player"));
        }

        [Test]
        public void EachWrite_UsesNewInitializationVector()
        {
            WriteScore(1);
            var first = storage.ReadOrNull("main.sav").Skip(5).Take(16).ToArray();
            WriteScore(2);
            var second = storage.ReadOrNull("main.sav").Skip(5).Take(16).ToArray();
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Flush_WithoutChanges_DoesNotRewrite()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.score = 1);
                save.Flush();
                var written = storage.ReadOrNull("main.sav");
                Assert.That(save.Flush(), Is.True);
                Assert.That(storage.ReadOrNull("main.sav"), Is.EqualTo(written));
            }
        }

        [Test]
        public void Flush_AfterDirectValueEdit_IsWritten()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Value.score = 9;
                save.Flush();
            }
            Assert.That(ReadScore(out _), Is.EqualTo(9));
        }

        [Test]
        public void Flush_WithNoSections_WritesNothing()
        {
            using (var save = Open())
                Assert.That(save.Flush(), Is.True);
            Assert.That(storage.Exists("main.sav"), Is.False);
        }

        [Test]
        public void Tick_WritesOnlyAfterIntervalWhenDirty()
        {
            using (var save = Open(autoFlush: 5f))
            {
                var section = save.Section<PlayerData>("player");
                save.Tick(10f);
                Assert.That(storage.Exists("main.sav"), Is.False, "Nothing changed yet.");

                section.Update(d => d.score = 1);
                save.Tick(4f);
                Assert.That(storage.Exists("main.sav"), Is.False, "Interval not reached.");
                save.Tick(1f);
                Assert.That(storage.Exists("main.sav"), Is.True);
            }
        }

        [Test]
        public void Tick_WithAutoFlushDisabled_DoesNotWrite()
        {
            using (var save = Open(autoFlush: 0f))
            {
                save.Section<PlayerData>("player").Update(d => d.score = 1);
                save.Tick(1000f);
                Assert.That(storage.Exists("main.sav"), Is.False);
            }
        }

        [Test]
        public void Dispose_WritesPendingChanges()
        {
            using (var save = Open())
                save.Section<PlayerData>("player").Update(d => d.score = 3);
            Assert.That(ReadScore(out _), Is.EqualTo(3));
        }

        [TestCase(5)]
        [TestCase(40)]
        [TestCase(-1)]
        public void TamperedFile_IsRejected(int index)
        {
            WriteScore(5);
            var data = storage.ReadOrNull("main.sav");
            FlipByte("main.sav", index < 0 ? data.Length + index : index);
            Assert.That(ReadScore(out var problems), Is.EqualTo(0));
            Assert.That(problems, Does.Contain(SaveProblem.DataLost));
        }

        [Test]
        public void TruncatedFile_IsRejected()
        {
            WriteScore(5);
            storage.Write("main.sav", storage.ReadOrNull("main.sav").Take(20).ToArray());
            Assert.That(ReadScore(out var problems), Is.EqualTo(0));
            Assert.That(problems, Does.Contain(SaveProblem.DataLost));
        }

        [Test]
        public void CorruptMain_RecoversFromBackup()
        {
            WriteScore(1);
            WriteScore(2);
            FlipByte("main.sav", 30);
            Assert.That(ReadScore(out var problems), Is.EqualTo(1));
            Assert.That(problems, Is.EqualTo(new[] { SaveProblem.RecoveredFromBackup }));
        }

        [Test]
        public void MissingMain_RecoversFromTemporaryFile()
        {
            WriteScore(1);
            storage.Move("main.sav", "main.tmp");
            Assert.That(ReadScore(out var problems), Is.EqualTo(1));
            Assert.That(problems, Is.EqualTo(new[] { SaveProblem.RecoveredFromBackup }));
        }

        [Test]
        public void Recovery_RewritesHealthyMainFile()
        {
            WriteScore(1);
            WriteScore(2);
            FlipByte("main.sav", 30);
            ReadScore(out _);
            Assert.That(ReadScore(out var problems), Is.EqualTo(1));
            Assert.That(problems, Is.Empty);
        }

        [Test]
        public void AllFilesCorrupt_KeepsCopyAndReportsDataLost()
        {
            WriteScore(1);
            WriteScore(2);
            FlipByte("main.sav", 30);
            FlipByte("main.bak", 30);
            Assert.That(ReadScore(out var problems), Is.EqualTo(0));
            Assert.That(problems, Is.EqualTo(new[] { SaveProblem.DataLost }));
            Assert.That(storage.Exists("main.corrupt"), Is.True);
        }

        [Test]
        public void OnlyCorruptBackupExists_KeepsCopyAndReportsDataLost()
        {
            WriteScore(1);
            storage.Move("main.sav", "main.bak");
            FlipByte("main.bak", 30);
            ReadScore(out var problems);
            Assert.That(problems, Is.EqualTo(new[] { SaveProblem.DataLost }));
            Assert.That(storage.Exists("main.corrupt"), Is.True);
        }

        [Test]
        public void WrongKey_DoesNotDestroyRealSave()
        {
            WriteScore(5);
            Assert.That(ReadScore(out var problems, key: OtherKey), Is.EqualTo(0));
            Assert.That(problems, Does.Contain(SaveProblem.DataLost));

            storage.Delete("main.sav");
            storage.Move("main.corrupt", "main.sav");
            Assert.That(ReadScore(out _), Is.EqualTo(5));
        }

        [Test]
        public void ProblemHandler_AddedAfterConstruction_StillReceivesLoadProblems()
        {
            WriteScore(5);
            FlipByte("main.sav", 30);
            var seen = new List<SaveProblem>();
            using (var save = Open())
            {
                save.ProblemRaised += seen.Add;
                save.Section<PlayerData>("player");
            }
            Assert.That(seen, Is.Not.Empty);
        }

        [Test]
        public void InterruptedWriteAtAnyStep_LeavesReadableSave()
        {
            for (var failAt = 0; failAt < 6; failAt++)
            {
                storage = new MemorySaveStorage();
                var flaky = new FlakyStorage(storage);
                WriteScore(1, flaky);
                WriteScore(2, flaky);

                using (var save = Open(flaky))
                {
                    flaky.FailFrom(failAt);
                    save.Section<PlayerData>("player").Update(d => d.score = 3);
                    save.Flush();
                }

                flaky.Heal();
                var score = ReadScore(out _, flaky);
                Assert.That(score == 2 || score == 3, Is.True, "Read " + score + " after failure at mutation " + failAt);
            }
        }

        [Test]
        public void WriteFailure_ReportsAndRetriesLater()
        {
            var flaky = new FlakyStorage(storage);
            var problems = new List<SaveProblem>();
            using (var save = Open(flaky, autoFlush: 5f))
            {
                save.ProblemRaised += problems.Add;
                var section = save.Section<PlayerData>("player");
                section.Update(d => d.score = 8);

                flaky.FailFrom(0);
                Assert.That(save.Flush(), Is.False);
                Assert.That(problems, Is.EqualTo(new[] { SaveProblem.WriteFailed }));

                flaky.Heal();
                save.Tick(5f);
            }
            Assert.That(ReadScore(out _), Is.EqualTo(8));
        }

        [Test]
        public void OlderSection_IsMigrated()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.score = 10);
                save.Flush();
            }

            int? from = null;
            using (var save = Open())
            {
                var section = save.Section<OtherData>("player", 2, (version, json) =>
                {
                    from = version;
                    return "{\"level\":99}";
                });
                Assert.That(section.Value.level, Is.EqualTo(99));
            }
            Assert.That(from, Is.EqualTo(1));

            using (var save = Open())
                Assert.That(save.Section<OtherData>("player", 2).Value.level, Is.EqualTo(99), "Migration result is persisted.");
        }

        [Test]
        public void OlderSection_WithoutMigration_Throws()
        {
            WriteScore(1);
            var save = Open();
            try
            {
                Assert.Throws<InvalidOperationException>(() => save.Section<PlayerData>("player", 2));
            }
            finally
            {
                save.Dispose();
            }
        }

        [Test]
        public void NewerSection_SwitchesToReadOnlyAndKeepsFile()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player", 3).Update(d => d.score = 77);
                save.Flush();
            }
            var original = storage.ReadOrNull("main.sav");

            var problems = new List<SaveProblem>();
            using (var save = Open())
            {
                save.ProblemRaised += problems.Add;
                var section = save.Section<PlayerData>("player", 2);
                Assert.That(section.Value.score, Is.EqualTo(0));
                Assert.That(save.IsReadOnly, Is.True);
                section.Update(d => d.score = 1);
                Assert.That(save.Flush(), Is.False);
            }

            Assert.That(problems, Is.EqualTo(new[] { SaveProblem.NewerVersion }));
            Assert.That(storage.ReadOrNull("main.sav"), Is.EqualTo(original));
        }

        [Test]
        public void UndeclaredSections_ArePreserved()
        {
            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.score = 1);
                save.Section<OtherData>("other").Update(d => d.level = 2);
                save.Flush();
            }

            using (var save = Open())
            {
                save.Section<PlayerData>("player").Update(d => d.score = 3);
                save.Flush();
            }

            using (var save = Open())
                Assert.That(save.Section<OtherData>("other").Value.level, Is.EqualTo(2));
        }

        [Test]
        public void OpeningSameNameTwice_Throws_UntilDisposed()
        {
            var first = Open();
            Assert.Throws<InvalidOperationException>(() => Open());
            first.Dispose();
            Open().Dispose();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not base64!")]
        [TestCase("AAAA")]
        public void InvalidKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => new SaveFile(Name, key, new SaveOptions { Storage = storage }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("../evil")]
        [TestCase("a/b")]
        public void InvalidName_Throws(string name)
        {
            Assert.Throws<ArgumentException>(() => new SaveFile(name, Key, new SaveOptions { Storage = storage }));
        }

        [Test]
        public void Inspector_DecryptsOnlyWithRightKey()
        {
            WriteScore(12);
            var file = storage.ReadOrNull("main.sav");
            Assert.That(SaveInspector.TryDecrypt(file, Key, out var json), Is.True);
            Assert.That(json, Does.Contain("score"));
            Assert.That(SaveInspector.TryDecrypt(file, OtherKey, out _), Is.False);
            Assert.That(SaveInspector.TryDecrypt(file, "bad key", out _), Is.False);
        }

        [Test]
        public void Inspector_GeneratedKeysAreValidAndUnique()
        {
            var a = SaveInspector.GenerateKey();
            var b = SaveInspector.GenerateKey();
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(Convert.FromBase64String(a).Length, Is.EqualTo(32));
            using (new SaveFile(Name, a, new SaveOptions { Storage = storage })) { }
        }

        [Test]
        public void FileStorage_RoundTripsOnDisk()
        {
            var directory = Path.Combine(Path.GetTempPath(), "RippleSaveTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var disk = new FileSaveStorage(directory);
                WriteScore(21, disk);
                WriteScore(22, disk);
                Assert.That(ReadScore(out var problems, disk), Is.EqualTo(22));
                Assert.That(problems, Is.Empty);
                Assert.That(File.Exists(Path.Combine(directory, "main.sav")), Is.True);
                Assert.That(File.Exists(Path.Combine(directory, "main.bak")), Is.True);
                Assert.That(File.Exists(Path.Combine(directory, "main.tmp")), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
