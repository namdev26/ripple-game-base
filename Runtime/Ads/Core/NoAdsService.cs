using System;
using System.Threading.Tasks;

namespace RippleGame.Ads
{
    /// <summary>Safe fallback used whenever the ADS symbol is absent.</summary>
    internal sealed class NoAdsService : IAdsService
    {
        public event Action<AdsEvent> EventRaised;
        public AdsEnvironment Environment => AdsEnvironment.Disabled;

        public Task<AdsResult> InitializeAsync() => Task.FromResult(AdsResult.Disabled);
        public bool IsReady(AdFormat format) => false;
        public void ShowBanner(BannerPosition position) => RaiseDisabled(AdFormat.Banner, AdPlacement.Default);
        public void HideBanner() { }
        public void DestroyBanner() { }
        public void ShowMrec(MrecPosition position) => RaiseDisabled(AdFormat.Mrec, AdPlacement.Default);
        public void HideMrec() { }

        public void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null)
        {
            RaiseDisabled(AdFormat.Interstitial, placement);
            completed?.Invoke(AdsResult.Disabled);
        }

        public void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null)
        {
            // Deliberately never invoke rewarded: a no-ads build must not grant rewards.
            RaiseDisabled(AdFormat.Rewarded, placement);
            completed?.Invoke(AdsResult.Disabled);
        }

        public void ShowAppOpen(Action<AdsResult> completed = null)
        {
            RaiseDisabled(AdFormat.AppOpen, AdPlacement.Default);
            completed?.Invoke(AdsResult.Disabled);
        }

        public void Dispose() { }

        private void RaiseDisabled(AdFormat format, AdPlacement placement)
        {
            EventRaised?.Invoke(new AdsEvent(AdsEventType.Failed, format, placement, "Advertising is disabled for this build."));
        }
    }
}
