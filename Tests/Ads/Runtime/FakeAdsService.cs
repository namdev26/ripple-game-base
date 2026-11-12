using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RippleGame.Ads.Tests
{
    /// <summary>
    /// Provider double for policy tests. Records every call, reports readiness per format and holds the
    /// callbacks of the last full-screen show so a test decides when it rewards, closes or fails.
    /// </summary>
    internal sealed class FakeAdsService : IAdsService
    {
        private readonly List<string> _calls = new List<string>();

        public event Action<AdsEvent> EventRaised;

        public Dictionary<AdFormat, bool> Ready { get; } = new Dictionary<AdFormat, bool>
        {
            { AdFormat.Banner, true },
            { AdFormat.Interstitial, true },
            { AdFormat.Rewarded, true },
            { AdFormat.AppOpen, true },
            { AdFormat.Mrec, true }
        };

        public IReadOnlyList<string> Calls => _calls;
        public AdPlacement LastPlacement { get; private set; }
        public bool Disposed { get; private set; }
        public AdsEnvironment Environment => AdsEnvironment.Test;

        private Action<AdsResult> _pendingCompleted;
        private Action _pendingReward;

        public int Count(string call) => _calls.Count(c => c == call);

        public Task<AdsResult> InitializeAsync()
        {
            _calls.Add(nameof(InitializeAsync));
            return Task.FromResult(AdsResult.Success);
        }

        public bool IsReady(AdFormat format) => Ready.TryGetValue(format, out var ready) && ready;

        public void ShowBanner(BannerPosition position) => _calls.Add(nameof(ShowBanner));
        public void HideBanner() => _calls.Add(nameof(HideBanner));
        public void DestroyBanner() => _calls.Add(nameof(DestroyBanner));
        public void ShowMrec(MrecPosition position) => _calls.Add(nameof(ShowMrec));
        public void HideMrec() => _calls.Add(nameof(HideMrec));

        public void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null)
        {
            _calls.Add(nameof(ShowInterstitial));
            Hold(AdFormat.Interstitial, placement, null, completed);
        }

        public void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null)
        {
            _calls.Add(nameof(ShowRewarded));
            Hold(AdFormat.Rewarded, placement, rewarded, completed);
        }

        public void ShowAppOpen(Action<AdsResult> completed = null)
        {
            _calls.Add(nameof(ShowAppOpen));
            Hold(AdFormat.AppOpen, AdPlacement.Default, null, completed);
        }

        /// <summary>Invokes the reward callback of the last rewarded show (it stays callable after close).</summary>
        public void Reward() => _pendingReward?.Invoke();

        /// <summary>Completes the pending show with <paramref name="result"/>.</summary>
        public void Close(AdsResult result = AdsResult.Closed)
        {
            var completed = _pendingCompleted;
            _pendingCompleted = null;
            completed?.Invoke(result);
        }

        public void Fail() => Close(AdsResult.Failed);

        public void Raise(AdsEvent adsEvent) => EventRaised?.Invoke(adsEvent);

        public void Dispose() => Disposed = true;

        private void Hold(AdFormat format, AdPlacement placement, Action rewarded, Action<AdsResult> completed)
        {
            LastPlacement = placement;
            if (!IsReady(format))
            {
                completed?.Invoke(AdsResult.NotReady);
                return;
            }

            _pendingReward = rewarded;
            _pendingCompleted = completed;
        }
    }
}
