using System;
using RippleGame.Economy;

internal static class WalletChecks
{
    private static int checks;

    private static void Main()
    {
        var store = new MemoryStore { Value = 17 };
        var wallet = new CurrencyWallet(store);
        var notifications = 0;
        var notifiedBalance = -1;
        wallet.BalanceChanged += balance => { notifications++; notifiedBalance = balance; };
        Check(wallet.Balance == 17, "load persisted balance");
        Check(!wallet.TryAdd(0) && !wallet.TryAdd(-3) && store.Saves == 0, "reject invalid adds without writes");
        Check(wallet.TryAdd(5) && wallet.Balance == 22 && notifications == 1 && notifiedBalance == 22, "add and notify persisted value");
        Check(!wallet.TrySpend(23) && !wallet.TrySpend(0) && !wallet.TrySpend(-1) && store.Saves == 1, "reject invalid spend without writes");
        Check(wallet.TrySpend(7) && wallet.Balance == 15 && notifications == 2, "spend and notify");
        Check(wallet.TrySpend(15) && wallet.Balance == 0, "spend exact balance");
        store.Value = int.MaxValue - 2;
        Check(wallet.TryAdd(10) && wallet.Balance == int.MaxValue, "saturate addition without overflow");
        store.Value = -7;
        Check(wallet.Balance == 0, "corrupted negative balance is not spendable");
        store.Value = 5;
        store.Fail = true;
        var before = notifications;
        var threw = false;
        try { wallet.TryAdd(2); } catch (InvalidOperationException) { threw = true; }
        Check(threw && notifications == before && store.Value == 5, "failed persistence never publishes a false balance");
        threw = false;
        try { new CurrencyWallet(null); } catch (ArgumentNullException) { threw = true; }
        Check(threw, "require a storage implementation");
        Console.WriteLine("RIPPLE_WALLET_CHECKS_PASS checks=" + checks);
    }

    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception(name);
    }

    private sealed class MemoryStore : ICurrencyBalanceStore
    {
        internal int Value;
        internal int Saves;
        internal bool Fail;
        public int Load() => Value;
        public void Save(int value)
        {
            if (Fail) throw new InvalidOperationException("Simulated persistence failure.");
            Value = value;
            Saves++;
        }
    }
}
