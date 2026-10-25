using System;
using RippleGame.Economy;
using UnityEngine;

namespace RippleGame.Persistence
{
    /// <summary>Local integer storage. Supply a key owned by the consuming game.</summary>
    public sealed class PlayerPrefsBalanceStore : ICurrencyBalanceStore
    {
        private readonly string key;

        public PlayerPrefsBalanceStore(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A save key is required.", nameof(key));
            this.key = key;
        }

        public int Load() => Math.Max(0, PlayerPrefs.GetInt(key, 0));

        public void Save(int balance)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            PlayerPrefs.SetInt(key, balance);
            PlayerPrefs.Save();
        }
    }
}
