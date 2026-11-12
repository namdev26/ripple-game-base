#if ADS
using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Google Mobile Ads implementation. This is the only runtime file that knows the SDK API.</summary>
    internal sealed class AdMobAdsService : IAdsService, IAdsLifecycleAware
    {
        private readonly AdsConfiguration _configuration;
        private readonly AdMobAdUnitConfiguration _profile;
        private readonly CancellationTokenSource _disposeToken = new CancellationTokenSource();
        private readonly AdMobBannerHandler _banner;
        private readonly AdMobInterstitialHandler _interstitial;
        private readonly AdMobRewardedHandler _rewarded;
        private readonly AdMobAppOpenHandler _appOpen;

        private Task<AdsResult> _initializationTask;
        private bool _initialized;
        private bool _fullScreenShowing;
        private bool _mrecUnsupportedLogged;
        private bool _appWasBackgrounded;
        private float _appBackgroundedAt;
        private float _lastAppOpenShowTime = float.NegativeInfinity;
        private float _lastFullScreenClosedTime = float.NegativeInfinity;
        private float _lastInterstitialShowTime = float.NegativeInfinity;
        private int _interstitialImpressionsThisSession;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private int _appOpenRetryAttempt;
        private AdPlacement _showingPlacement;

        internal AdMobAdsService(AdsConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _profile = configuration.GetActiveProfile();
            _banner = _profile.EnableBanner ? new AdMobBannerHandler(_profile.BannerUnitId) : null;
            _interstitial = _profile.EnableInterstitial ? new AdMobInterstitialHandler(_profile.InterstitialUnitId) : null;
            _rewarded = _profile.EnableRewarded ? new AdMobRewardedHandler(_profile.RewardedUnitId) : null;
            _appOpen = _profile.EnableAppOpen ? new AdMobAppOpenHandler(_profile.AppOpenUnitId) : null;

            if (_banner != null) { _banner.Loaded += () => Raise(AdsEventType.Loaded, AdFormat.Banner); _banner.LoadFailed += message => Raise(AdsEventType.Failed, AdFormat.Banner, message: message); }
            if (_interstitial != null) { _interstitial.Loaded += () => { _interstitialRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Interstitial); }; _interstitial.LoadFailed += message => RetryLoad(AdFormat.Interstitial, message); }
            if (_rewarded != null) { _rewarded.Loaded += () => { _rewardedRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Rewarded); }; _rewarded.LoadFailed += message => RetryLoad(AdFormat.Rewarded, message); }
            if (_appOpen != null) { _appOpen.Loaded += () => { _appOpenRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.AppOpen); }; _appOpen.LoadFailed += message => RetryLoad(AdFormat.AppOpen, message); }

            if (_banner != null) _banner.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Banner, revenue: revenue);
            if (_interstitial != null) _interstitial.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Interstitial, _showingPlacement, revenue: revenue);
            if (_rewarded != null) _rewarded.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Rewarded, _showingPlacement, revenue: revenue);
            if (_appOpen != null) _appOpen.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.AppOpen, revenue: revenue);
        }

        public event Action<AdsEvent> EventRaised;
        public AdsEnvironment Environment => AdsConfiguration.ResolveCompileTimeEnvironment();

        public Task<AdsResult> InitializeAsync()
        {
            if (_initializationTask != null)
            {
                return _initializationTask;
            }

            var completion = new TaskCompletionSource<AdsResult>();
            _initializationTask = completion.Task;
            try
            {
                MobileAds.Initialize(status => MainThreadDispatcher.Post(() =>
                {
                    _initialized = status != null;
                    if (!_initialized)
                    {
                        completion.TrySetResult(AdsResult.Failed);
                        return;
                    }

                    LoadInitialAds();
                    completion.TrySetResult(AdsResult.Success);
                }));
            }
            catch (Exception exception)
            {
                Raise(AdsEventType.Failed, AdFormat.AppOpen, message: exception.ToString());
                completion.TrySetResult(AdsResult.Failed);
            }
            return _initializationTask;
        }

        public bool IsReady(AdFormat format)
        {
            return format switch
            {
                AdFormat.Interstitial => _interstitial != null && _interstitial.IsReady,
                AdFormat.Rewarded => _rewarded != null && _rewarded.IsReady,
                AdFormat.AppOpen => _appOpen != null && _appOpen.IsReady,
                AdFormat.Mrec => false,
                _ => _banner != null
            };
        }

        public void ShowBanner(BannerPosition position)
        {
            if (!_initialized || _banner == null)
            {
                Raise(AdsEventType.ShowFailed, AdFormat.Banner, message: "Banner is disabled or the SDK is not initialized.");
                return;
            }

            _banner.Show(position);
        }

        public void HideBanner() => _banner?.Hide();
        public void DestroyBanner() => _banner?.Destroy();

        public void ShowMrec(MrecPosition position)
        {
            if (!_mrecUnsupportedLogged)
            {
                _mrecUnsupportedLogged = true;
                Debug.LogWarning("[RippleGame Ads] MREC is not supported by the AdMob provider in 0.4.0.");
            }

            Raise(AdsEventType.ShowFailed, AdFormat.Mrec, message: "MREC is not supported by the AdMob provider.");
        }

        public void HideMrec() { }

        public void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null)
        {
            if (!CanShowInterstitial(completed))
            {
                return;
            }

            if (!CanShowFullScreen(_interstitial != null && _interstitial.IsReady, AdFormat.Interstitial, placement, completed))
            {
                return;
            }

            _showingPlacement = placement;
            if (!_interstitial.Show(
                    () =>
                    {
                        RecordInterstitialImpression();
                        Raise(AdsEventType.Shown, AdFormat.Interstitial, placement);
                    },
                    () => FinishFullScreen(AdFormat.Interstitial, placement, AdsResult.Closed, completed, LoadInterstitial),
                    error => FinishFullScreen(AdFormat.Interstitial, placement, AdsResult.Failed, completed, LoadInterstitial, error)))
            {
                FinishFullScreen(AdFormat.Interstitial, placement, AdsResult.NotReady, completed, LoadInterstitial);
            }
        }

        public void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null)
        {
            if (!CanShowFullScreen(_rewarded != null && _rewarded.IsReady, AdFormat.Rewarded, placement, completed))
            {
                return;
            }

            _showingPlacement = placement;
            if (!_rewarded.Show(
                    () => Raise(AdsEventType.Shown, AdFormat.Rewarded, placement),
                    () =>
                    {
                        Raise(AdsEventType.RewardEarned, AdFormat.Rewarded, placement);
                        rewarded?.Invoke();
                    },
                    () => FinishFullScreen(AdFormat.Rewarded, placement, AdsResult.Closed, completed, LoadRewarded),
                    error => FinishFullScreen(AdFormat.Rewarded, placement, AdsResult.Failed, completed, LoadRewarded, error)))
            {
                FinishFullScreen(AdFormat.Rewarded, placement, AdsResult.NotReady, completed, LoadRewarded);
            }
        }

        public void ShowAppOpen(Action<AdsResult> completed = null)
        {
            if (Time.realtimeSinceStartup - _lastAppOpenShowTime < _profile.AppOpenCooldownSeconds)
            {
                completed?.Invoke(AdsResult.Cooldown);
                return;
            }

            if (!CanShowFullScreen(_appOpen != null && _appOpen.IsReady, AdFormat.AppOpen, AdPlacement.Default, completed))
            {
                return;
            }

            _lastAppOpenShowTime = Time.realtimeSinceStartup;
            if (!_appOpen.Show(
                    () => Raise(AdsEventType.Shown, AdFormat.AppOpen),
                    () => FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.Closed, completed, LoadAppOpen),
                    error => FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.Failed, completed, LoadAppOpen, error)))
            {
                FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.NotReady, completed, LoadAppOpen);
            }
        }

        public void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                return;

            var wasBackgrounded = _appWasBackgrounded;
            _appWasBackgrounded = false;
            if (!wasBackgrounded ||
                Time.realtimeSinceStartup - _appBackgroundedAt < _profile.AppOpenMinimumBackgroundSeconds ||
                !_initialized ||
                _fullScreenShowing ||
                _appOpen == null ||
                !_appOpen.IsReady ||
                Time.realtimeSinceStartup - _lastFullScreenClosedTime < _profile.AppOpenCooldownSeconds)
            {
                return;
            }

            ShowAppOpen();
        }

        public void OnApplicationPauseChanged(bool isPaused)
        {
            if (!isPaused)
                return;

            _appWasBackgrounded = true;
            _appBackgroundedAt = Time.realtimeSinceStartup;
        }

        public void Dispose()
        {
            _disposeToken.Cancel();
            _banner?.Dispose();
            _interstitial?.Dispose();
            _rewarded?.Dispose();
            _appOpen?.Dispose();
            _disposeToken.Dispose();
        }

        private bool CanShowFullScreen(bool isReady, AdFormat format, AdPlacement placement, Action<AdsResult> completed)
        {
            if (!_initialized)
            {
                completed?.Invoke(AdsResult.NotReady);
                return false;
            }

            if (_fullScreenShowing)
            {
                completed?.Invoke(AdsResult.AlreadyShowing);
                return false;
            }

            if (!isReady)
            {
                Raise(AdsEventType.ShowFailed, format, placement, "Ad is not ready.");
                completed?.Invoke(AdsResult.NotReady);
                return false;
            }

            _fullScreenShowing = true;
            return true;
        }

        private bool CanShowInterstitial(Action<AdsResult> completed)
        {
            var isSessionCapReached = _profile.InterstitialMaxImpressionsPerSession > 0 &&
                                      _interstitialImpressionsThisSession >= _profile.InterstitialMaxImpressionsPerSession;
            var isCoolingDown = Time.realtimeSinceStartup - _lastInterstitialShowTime < _profile.InterstitialCooldownSeconds;
            if (!isSessionCapReached && !isCoolingDown)
            {
                return true;
            }

            completed?.Invoke(AdsResult.Cooldown);
            return false;
        }

        private void RecordInterstitialImpression()
        {
            _lastInterstitialShowTime = Time.realtimeSinceStartup;
            _interstitialImpressionsThisSession++;
        }

        private void FinishFullScreen(AdFormat format, AdPlacement placement, AdsResult result, Action<AdsResult> completed, Action reload, string error = null)
        {
            _fullScreenShowing = false;
            _lastFullScreenClosedTime = Time.realtimeSinceStartup;
            Raise(result == AdsResult.Failed ? AdsEventType.ShowFailed : AdsEventType.Closed, format, placement, error);
            completed?.Invoke(result);
            reload();
        }

        private void LoadInitialAds()
        {
            LoadInterstitial();
            LoadRewarded();
            LoadAppOpen();
        }

        private void LoadInterstitial() => _interstitial?.Load();
        private void LoadRewarded() => _rewarded?.Load();
        private void LoadAppOpen() => _appOpen?.Load();

        private void RetryLoad(AdFormat format, string error)
        {
            Raise(AdsEventType.Failed, format, message: error);
            switch (format)
            {
                case AdFormat.Interstitial:
                    RetryAfterDelay(LoadInterstitial, ++_interstitialRetryAttempt);
                    break;
                case AdFormat.Rewarded:
                    RetryAfterDelay(LoadRewarded, ++_rewardedRetryAttempt);
                    break;
                case AdFormat.AppOpen:
                    RetryAfterDelay(LoadAppOpen, ++_appOpenRetryAttempt);
                    break;
            }
        }

        private async void RetryAfterDelay(Action loader, int attempt)
        {
            if (attempt > _profile.RetryMaxAttempts || _disposeToken.IsCancellationRequested)
            {
                return;
            }

            var delay = Mathf.Min(_profile.RetryInitialDelaySeconds * Mathf.Pow(2f, attempt - 1), _profile.RetryMaxDelaySeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), _disposeToken.Token);
                MainThreadDispatcher.Post(() =>
                {
                    if (!_disposeToken.IsCancellationRequested)
                    {
                        loader();
                    }
                });
            }
            catch (OperationCanceledException) { }
        }

        private void Raise(AdsEventType type, AdFormat format, AdPlacement placement = AdPlacement.Default, string message = null, AdRevenue? revenue = null)
        {
            if (_configuration.EnableLogging)
            {
                var revenueText = revenue.HasValue ? $" {revenue.Value.ValueMicros} micros {revenue.Value.CurrencyCode}" : string.Empty;
                Debug.Log($"[RippleGame Ads] {type} {format} ({placement}) {message}{revenueText}");
            }

            EventRaised?.Invoke(new AdsEvent(type, format, placement, message, revenue));
        }
    }
}
#endif
