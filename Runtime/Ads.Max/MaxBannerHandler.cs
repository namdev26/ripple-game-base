using System;

namespace RippleGame.Ads.Max
{
    /// <summary>
    /// One MAX banner view. With a refresh interval above 0 the SDK auto-refresh is stopped and
    /// <see cref="Tick"/> loads a new banner every interval while the banner is visible.
    /// </summary>
    internal sealed class MaxBannerHandler : IDisposable
    {
        private readonly string _unitId;
        private readonly float _refreshSeconds;
        private bool _created;
        private bool _visible;
        private bool _loaded;
        private BannerPosition _position;
        private float _nextRefreshAt;

        internal MaxBannerHandler(string unitId, float refreshSeconds)
        {
            _unitId = unitId;
            _refreshSeconds = refreshSeconds;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        }

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;

        /// <summary>True when the banner view exists but has no ad yet (for example after going offline).</summary>
        internal bool NeedsReload => _created && !_loaded;

        internal void Show(BannerPosition position, float now)
        {
            if (!_created)
            {
                MaxSdk.CreateBanner(_unitId, new MaxSdkBase.AdViewConfiguration(ToAdViewPosition(position)));
                if (_refreshSeconds > 0f)
                {
                    MaxSdk.StopBannerAutoRefresh(_unitId);
                }

                _created = true;
            }
            else if (position != _position)
            {
                MaxSdk.UpdateBannerPosition(_unitId, ToAdViewPosition(position));
            }

            _position = position;
            MaxSdk.ShowBanner(_unitId);
            if (!_visible)
            {
                _nextRefreshAt = now + _refreshSeconds;
            }

            _visible = true;
        }

        internal void Hide()
        {
            if (!_created)
            {
                return;
            }

            MaxSdk.HideBanner(_unitId);
            _visible = false;
        }

        internal void Destroy()
        {
            if (!_created)
            {
                return;
            }

            MaxSdk.DestroyBanner(_unitId);
            _created = false;
            _visible = false;
            _loaded = false;
        }

        internal void Reload()
        {
            if (_created)
            {
                MaxSdk.LoadBanner(_unitId);
            }
        }

        /// <summary>Manual refresh; does nothing when the SDK refreshes on its own (interval 0).</summary>
        internal void Tick(float now)
        {
            if (_refreshSeconds <= 0f || !_visible || now < _nextRefreshAt)
            {
                return;
            }

            _nextRefreshAt = now + _refreshSeconds;
            MaxSdk.LoadBanner(_unitId);
        }

        public void Dispose()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
            Destroy();
        }

        private void OnAdLoaded(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            MainThreadDispatcher.Post(() =>
            {
                if (!_created) return;
                _loaded = true;
                Loaded?.Invoke();
            });
        }

        private void OnAdLoadFailed(string unitId, MaxSdkBase.ErrorInfo error)
        {
            if (unitId != _unitId) return;
            var message = MaxRevenue.Describe(error);
            MainThreadDispatcher.Post(() => LoadFailed?.Invoke(message));
        }

        private void OnAdRevenuePaid(string unitId, MaxSdkBase.AdInfo info)
        {
            if (unitId != _unitId) return;
            if (!MaxRevenue.TryFrom(_unitId, info, out var revenue)) return;
            MainThreadDispatcher.Post(() => Paid?.Invoke(revenue));
        }

        private static MaxSdkBase.AdViewPosition ToAdViewPosition(BannerPosition position)
        {
            return position == BannerPosition.Top ? MaxSdkBase.AdViewPosition.TopCenter : MaxSdkBase.AdViewPosition.BottomCenter;
        }
    }
}
