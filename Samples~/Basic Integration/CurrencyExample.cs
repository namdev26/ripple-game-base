using RippleGame.Economy;
using RippleGame.Persistence;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    public sealed class CurrencyExample : MonoBehaviour
    {
        [SerializeField] private string saveKey = "example.currency.coins";
        public CurrencyWallet Wallet { get; private set; }
        private void Awake() => Wallet = new CurrencyWallet(new PlayerPrefsBalanceStore(saveKey));
    }
}
