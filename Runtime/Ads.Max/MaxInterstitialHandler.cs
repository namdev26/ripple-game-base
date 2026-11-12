using System;

namespace RippleGame.Ads.Max
{
    internal sealed class MaxInterstitialHandler : IDisposable
    {
        private readonly string _unitId;
        private bool _loading;
        private bool _showing;
        private Action _opened;
        private Action _closed;
        private Action<string> _failed;

        internal MaxInterstitialHandler(string unitId)
        {
            _unitId = unitId;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnAdDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHidden;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        }

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;
        internal bool IsReady => MaxSdk.IsInterstitialReady(_unitId);
        internal bool IsLoading => _loading;

        internal void Load()
        {
            if (_loading || IsReady)
            {
                return;
            }

            _loading = true;
            MaxSdk.LoadInterstitial(_unitId);
        }

        /// <summary>Returns false when no ad is ready; otherwise exactly one of closed or failed runs later.</summary>
        internal bool Show(Action opened, Action closed, Action<string> failed)
        {
            if (_showing || !IsReady)
            {
                return false;
            }

            _showing = true;
            _opened = opened;
            _closed = closed;
            _failed = failed;
            MaxSdk.ShowInterstitial(_unitId);
            return true;
        }

        public void Dispose()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnAdDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnAdHidden;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
            CompleteFailed("The ads service was disposed while the interstitial was showing.");
        }

        private void OnAdLoaded(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                _loading = false;
                Loaded?.Invoke();
            });
        }

        private void OnAdLoadFailed(string unitId, MaxSdkBase.ErrorInfo error)
        {
            if (unitId != _unitId) return;
            var message = MaxRevenue.Describe(error);
            MainThreadDispatcher.Post(() =>
            {
                _loading = false;
                LoadFailed?.Invoke(message);
            });
        }

        private void OnAdDisplayed(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                var opened = _opened;
                _opened = null;
                opened?.Invoke();
            });
        }

        private void OnAdDisplayFailed(string unitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            var message = MaxRevenue.Describe(error);
            MainThreadDispatcher.Post(() => CompleteFailed(message));
        }

        private void OnAdHidden(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                var closed = _closed;
                ClearShowCallbacks();
                closed?.Invoke();
            });
        }

        private void OnAdRevenuePaid(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            var revenue = MaxRevenue.From(_unitId, info);
            MainThreadDispatcher.Post(() => Paid?.Invoke(revenue));
        }

        private void CompleteFailed(string message)
        {
            var failed = _failed;
            ClearShowCallbacks();
            failed?.Invoke(message);
        }

        private void ClearShowCallbacks()
        {
            _showing = false;
            _opened = null;
            _closed = null;
            _failed = null;
        }
    }
}
