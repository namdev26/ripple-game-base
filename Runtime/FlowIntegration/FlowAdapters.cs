using System;
using RippleGame.Ads;
using RippleGame.Analytics;
using RippleGame.Flow;
using AdsApi = RippleGame.Ads.Ads;

namespace RippleGame.FlowIntegration
{
    /// <summary>Plays level flow ads through the package's Ads module.</summary>
    public sealed class AdsLevelFlowAds : ILevelFlowAds
    {
        public bool IsRewardedReady => AdsApi.IsReady(AdFormat.Rewarded);

        public void ShowRewarded(LevelAdPurpose purpose, Action<bool> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var placement = purpose == LevelAdPurpose.Revive ? AdPlacement.ExtraLife : AdPlacement.DoubleReward;
            var earned = false;
            AdsApi.ShowRewarded(placement, () => earned = true, _ => completed(earned));
        }

        public void ShowInterstitial(Action completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            AdsApi.ShowInterstitial(AdPlacement.LevelComplete, _ => completed());
        }
    }

    /// <summary>Reports level flow events through the package's Analytics module.</summary>
    public sealed class AnalyticsLevelFlowEvents : ILevelFlowEvents
    {
        public void LevelStarted(int level) => GameEvents.LevelStart(level);
        public void LevelCompleted(int level, int score) => GameEvents.LevelComplete(level, score);
        public void LevelFailed(int level, string reason) => GameEvents.LevelFail(level, reason);
    }
}
