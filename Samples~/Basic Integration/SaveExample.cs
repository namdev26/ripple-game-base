using System;
using RippleGame.Economy;
using RippleGame.Persistence;
using RippleGame.Progression;
using RippleGame.Save;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    [Serializable]
    public sealed class PlayerSaveData
    {
        public int bestScore;
        public bool soundOn = true;
    }

    /// <summary>Create your own key from TOOL Hub > Save and pass GameSaveKey.Value instead of this field.</summary>
    public sealed class SaveExample : MonoBehaviour
    {
        [SerializeField] private string saveKey;
        private SaveFile save;

        public SaveSection<PlayerSaveData> Player { get; private set; }
        public CurrencyWallet Wallet { get; private set; }
        public LevelProgress Progress { get; private set; }

        private void Awake()
        {
            save = new SaveFile("example", saveKey);
            save.ProblemRaised += problem => Debug.LogWarning("Save problem: " + problem);

            Player = save.Section<PlayerSaveData>("player");
            Wallet = new CurrencyWallet(save.BalanceStore("coins"));
            Progress = new LevelProgress(save.LevelStore("levels"));
        }

        public void RecordScore(int score)
        {
            if (score > Player.Value.bestScore) Player.Update(data => data.bestScore = score);
        }

        private void OnDestroy() => save?.Dispose();
    }
}
