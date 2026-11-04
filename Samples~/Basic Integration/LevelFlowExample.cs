using RippleGame.Economy;
using RippleGame.Flow;
using RippleGame.FlowIntegration;
using RippleGame.Persistence;
using RippleGame.Progression;
using RippleGame.Save;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    /// <summary>Connects everything for one gameplay scene. No game rule lives in the base package.</summary>
    public sealed class LevelFlowExample : MonoBehaviour
    {
        [SerializeField] private string saveKey;
        [SerializeField] private string homeScene = "Home";
        [SerializeField] private int levelCount = 10;
        [SerializeField] private ExampleGameplay gameplay;
        [SerializeField] private ExampleView view;

        private SaveFile save;
        private LevelFlow flow;

        private void Start()
        {
            save = new SaveFile("example_flow", saveKey);
            var progress = new LevelProgress(save.LevelStore("levels"));

            flow = new LevelFlow(new LevelFlowConfig
            {
                Gameplay = gameplay,
                View = view,
                Progress = progress,
                Wallet = new CurrencyWallet(save.BalanceStore("coins")),
                RewardPolicy = outcome => outcome.Score * 10,
                Ads = new AdsLevelFlowAds(),
                Events = new AnalyticsLevelFlowEvents(),
                LevelCount = levelCount,
                Commit = () => save.Flush(),
                ExitToHome = () => RippleGame.Scenes.SceneLoader.Load(homeScene)
            });

            flow.Start(Mathf.Min(progress.HighestCompleted + 1, levelCount));
        }

        private void OnDestroy()
        {
            flow?.Dispose();
            save?.Dispose();
        }
    }
}
