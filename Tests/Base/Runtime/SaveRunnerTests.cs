using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RippleGame.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Base.Tests
{
    public sealed class SaveRunnerTests
    {
        [Serializable]
        private sealed class Data
        {
            public int value;
        }

        private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

        private MemorySaveStorage storage;
        private SaveFile save;

        [SetUp]
        public void SetUp()
        {
            storage = new MemorySaveStorage();
            save = new SaveFile("runner", Key, new SaveOptions { Storage = storage, AutoFlushSeconds = 0f });
            save.Section<Data>("data").Update(d => d.value = 1);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            save.Dispose();
            // Destroy is deferred; the next test must not find this runner.
            yield return null;
        }

        private static SaveRunner Runner => UnityEngine.Object.FindAnyObjectByType<SaveRunner>();

        [Test]
        public void Runner_SurvivesSceneChanges()
        {
            Assert.That(Runner, Is.Not.Null);
            Assert.That(Runner.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [Test]
        public void GoingToBackground_Writes()
        {
            Runner.SendMessage("OnApplicationPause", true);
            Assert.That(storage.Exists("runner.sav"), Is.True);
        }

        [Test]
        public void ReturningFromBackground_DoesNotWrite()
        {
            Runner.SendMessage("OnApplicationPause", false);
            Assert.That(storage.Exists("runner.sav"), Is.False);
        }

        [Test]
        public void LosingFocus_Writes()
        {
            Runner.SendMessage("OnApplicationFocus", false);
            Assert.That(storage.Exists("runner.sav"), Is.True);
        }

        [Test]
        public void Quitting_Writes()
        {
            Runner.SendMessage("OnApplicationQuit");
            Assert.That(storage.Exists("runner.sav"), Is.True);
        }

        [UnityTest]
        public IEnumerator TimedWrite_RunsFromUpdate()
        {
            save.Dispose();
            save = new SaveFile("runner", Key, new SaveOptions { Storage = storage, AutoFlushSeconds = 0.01f });
            save.Section<Data>("data").Update(d => d.value = 2);
            var before = storage.ReadOrNull("runner.sav");

            // Headless frames can be far shorter than the interval, so wait on real time.
            var deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && before.SequenceEqual(storage.ReadOrNull("runner.sav")))
                yield return null;

            Assert.That(SaveInspector.TryDecrypt(storage.ReadOrNull("runner.sav"), Key, out var json), Is.True);
            Assert.That(json, Does.Contain("value\\\":2"));
        }

        [UnityTest]
        public IEnumerator Dispose_RemovesRunner()
        {
            save.Dispose();
            yield return null;
            Assert.That(Runner, Is.Null);
        }
    }
}
