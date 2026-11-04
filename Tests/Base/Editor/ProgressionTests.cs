using System;
using System.Linq;
using NUnit.Framework;
using RippleGame.Economy;
using RippleGame.Persistence;
using RippleGame.Progression;
using RippleGame.Save;

namespace RippleGame.Base.Tests
{
    public sealed class ProgressionTests
    {
        private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

        private sealed class FakeStore : ILevelProgressStore
        {
            public int Value;
            public int Saves;
            public int LoadHighestCompleted() => Value;
            public void SaveHighestCompleted(int level)
            {
                Value = level;
                Saves++;
            }
        }

        [Test]
        public void FirstLevel_IsAlwaysUnlocked()
        {
            var progress = new LevelProgress(new FakeStore());
            Assert.That(progress.HighestCompleted, Is.EqualTo(0));
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.IsUnlocked(0), Is.False);
        }

        [Test]
        public void Completing_UnlocksOnlyNextLevel()
        {
            var progress = new LevelProgress(new FakeStore());
            progress.MarkCompleted(3);
            Assert.That(progress.HighestCompleted, Is.EqualTo(3));
            Assert.That(progress.IsUnlocked(4), Is.True);
            Assert.That(progress.IsUnlocked(5), Is.False);
        }

        [Test]
        public void ReplayingOlderLevel_DoesNotLowerProgress()
        {
            var store = new FakeStore { Value = 5 };
            var progress = new LevelProgress(store);
            progress.MarkCompleted(2);
            Assert.That(progress.HighestCompleted, Is.EqualTo(5));
            Assert.That(store.Saves, Is.EqualTo(0));
        }

        [Test]
        public void CorruptNegativeStore_IsTreatedAsNoProgress()
        {
            Assert.That(new LevelProgress(new FakeStore { Value = -4 }).HighestCompleted, Is.EqualTo(0));
        }

        [Test]
        public void CompletingInvalidLevel_Throws()
        {
            var progress = new LevelProgress(new FakeStore());
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.MarkCompleted(0));
        }

        [Test]
        public void MissingStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelProgress(null));
        }

        [Test]
        public void WalletAndProgress_PersistTogetherInOneSaveFile()
        {
            var storage = new MemorySaveStorage();
            var options = new SaveOptions { Storage = storage, AutoFlushSeconds = 0f };

            using (var save = new SaveFile("game", Key, options))
            {
                new CurrencyWallet(save.BalanceStore("coins")).TryAdd(70);
                new LevelProgress(save.LevelStore("levels")).MarkCompleted(4);
                Assert.That(save.Flush(), Is.True);
            }

            using (var save = new SaveFile("game", Key, options))
            {
                Assert.That(new CurrencyWallet(save.BalanceStore("coins")).Balance, Is.EqualTo(70));
                Assert.That(new LevelProgress(save.LevelStore("levels")).HighestCompleted, Is.EqualTo(4));
            }
        }

        [Test]
        public void SeparateCurrencyKeys_AreIsolated()
        {
            var options = new SaveOptions { Storage = new MemorySaveStorage(), AutoFlushSeconds = 0f };
            using (var save = new SaveFile("game", Key, options))
            {
                var coins = new CurrencyWallet(save.BalanceStore("coins"));
                var gems = new CurrencyWallet(save.BalanceStore("gems"));
                coins.TryAdd(5);
                Assert.That(gems.Balance, Is.EqualTo(0));
            }
        }
    }
}
