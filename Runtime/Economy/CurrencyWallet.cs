using System;

namespace RippleGame.Economy
{
    /// <summary>Currency operations independent of scenes, UI and the persistence provider.</summary>
    public sealed class CurrencyWallet
    {
        private readonly ICurrencyBalanceStore store;
        public int Balance => Math.Max(0, store.Load());
        public event Action<int> BalanceChanged;

        public CurrencyWallet(ICurrencyBalanceStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool TryAdd(int amount)
        {
            if (amount <= 0) return false;
            Persist((int)Math.Min((long)Balance + amount, int.MaxValue));
            return true;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            var current = Balance;
            if (amount > current) return false;
            Persist(current - amount);
            return true;
        }

        private void Persist(int balance)
        {
            store.Save(balance);
            BalanceChanged?.Invoke(balance);
        }
    }
}
