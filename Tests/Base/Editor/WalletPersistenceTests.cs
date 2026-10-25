using System;
using NUnit.Framework;
using RippleGame.Economy;
using RippleGame.Persistence;
using UnityEngine;

namespace RippleGame.Base.Tests
{
    public sealed class WalletPersistenceTests
    {
        private string key;

        [SetUp] public void SetUp() => key = "ripple.test.wallet." + Guid.NewGuid().ToString("N");
        [TearDown] public void TearDown()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + ".other");
            PlayerPrefs.Save();
        }

        [Test]
        public void SuppliedKeysIsolateWalletsAndSurviveReconstruction()
        {
            var first = new CurrencyWallet(new PlayerPrefsBalanceStore(key));
            var other = new CurrencyWallet(new PlayerPrefsBalanceStore(key + ".other"));
            Assert.That(first.TryAdd(25), Is.True);
            Assert.That(first.TrySpend(9), Is.True);
            Assert.That(new CurrencyWallet(new PlayerPrefsBalanceStore(key)).Balance, Is.EqualTo(16));
            Assert.That(other.Balance, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void EmptyKeyIsRejected(string suppliedKey)
        {
            Assert.Throws<ArgumentException>(() => new PlayerPrefsBalanceStore(suppliedKey));
        }

        [Test]
        public void InvalidBalanceDoesNotOverwriteExistingSave()
        {
            var store = new PlayerPrefsBalanceStore(key);
            store.Save(12);
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Save(-1));
            Assert.That(store.Load(), Is.EqualTo(12));
        }
    }
}
