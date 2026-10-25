namespace RippleGame.Economy
{
    /// <summary>Stores one currency balance; the consuming game owns its identity.</summary>
    public interface ICurrencyBalanceStore
    {
        int Load();
        void Save(int balance);
    }
}
