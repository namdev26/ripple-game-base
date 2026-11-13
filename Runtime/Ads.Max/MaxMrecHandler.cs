using System;

namespace RippleGame.Ads.Max
{
    /// <summary>
    /// One MAX MREC view. With a refresh interval above 0 the SDK auto-refresh is stopped and
    /// <see cref="Tick"/> loads a new MREC every interval while it is visible.
    /// </summary>
    internal sealed class MaxMrecHandler : IDisposable
    {
        private readonly string _unitId;
        private readonly float _refreshSeconds;
        private bool _created;
        private bool _visible;
        private bool _loaded;
        private MrecPosition _position;
        private float _nextRefreshAt;

        internal MaxMrecHandler(string unitId, float refreshSeconds)
        {
            _unitId = unitId;
            _refreshSeconds = refreshSeconds;
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        }

        internal event Action Loaded;
        internal event Action<string> LoadFailed;
        internal event Action<AdRevenue> Paid;

        /// <summary>True when the MREC view exists but has no ad yet (for example after going offline).</summary>
        internal bool NeedsReload => _created && !_loaded;

        internal void Show(MrecPosition position, float now)
        {
            if (!_created)
            {
                MaxSdk.CreateMRec(_unitId, new MaxSdkBase.AdViewConfiguration(ToAdViewPosition(position)));
                if (_refreshSeconds > 0f)
                {
                    MaxSdk.StopMRecAutoRefresh(_unitId);
                }

                _created = true;
            }
            else if (position != _position)
            {
                MaxSdk.UpdateMRecPosition(_unitId, ToAdViewPosition(position));
            }

            _position = position;
            MaxSdk.ShowMRec(_unitId);
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

            MaxSdk.HideMRec(_unitId);
            _visible = false;
        }

        internal void Destroy()
        {
            if (!_created)
            {
                return;
            }

            MaxSdk.DestroyMRec(_unitId);
            _created = false;
            _visible = false;
            _loaded = false;
        }

        internal void Reload()
        {
            if (_created)
            {
                MaxSdk.LoadMRec(_unitId);
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
            MaxSdk.LoadMRec(_unitId);
        }

        public void Dispose()
        {
            MaxSdkCallbacks.MRec.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
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

        private static MaxSdkBase.AdViewPosition ToAdViewPosition(MrecPosition position)
        {
            switch (position)
            {
                case MrecPosition.TopCenter: return MaxSdkBase.AdViewPosition.TopCenter;
                case MrecPosition.Center: return MaxSdkBase.AdViewPosition.Centered;
                default: return MaxSdkBase.AdViewPosition.BottomCenter;
            }
        }
    }
}
