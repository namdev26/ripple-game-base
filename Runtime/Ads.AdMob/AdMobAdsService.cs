using System;
using System.Threading;
using System.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RippleGame.Ads.AdMob
{
    /// <summary>
    /// Google Mobile Ads implementation: load, show and retry only. Cooldowns, session caps and app-open
    /// resume rules live in the shared policy layer that wraps this service.
    /// </summary>
    internal sealed class AdMobAdsService : IAdsService
    {
        private readonly AdsConfiguration _configuration;
        private readonly AdMobAdUnitConfiguration _profile;
        private readonly CancellationTokenSource _disposeToken = new CancellationTokenSource();
        // Created in InitializeAsync, after the initialization gate, so remote unit ids apply. Null before that.
        private AdMobBannerHandler _banner;
        private AdMobInterstitialHandler _interstitial;
        private AdMobRewardedHandler _rewarded;
        private AdMobAppOpenHandler _appOpen;

        private Task<AdsResult> _initializationTask;
        private bool _initialized;
        private bool _fullScreenShowing;
        private bool _mrecUnsupportedLogged;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private int _appOpenRetryAttempt;
        private AdPlacement _showingPlacement;

        internal AdMobAdsService(AdsConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _profile = configuration.GetActiveProfile();
        }

        /// <summary>
        /// Resolves unit ids (remote overrides first) and builds the format handlers. Runs once, from InitializeAsync,
        /// which the policy layer calls after the initialization gate. Banner refresh is left to the AdMob SDK, so the
        /// policy refresh values do not apply to this provider.
        /// </summary>
        private void CreateHandlers()
        {
            var bannerId = ResolveUnitId(_profile.EnableBanner, AdFormat.Banner, _profile.BannerUnitId);
            var interstitialId = ResolveUnitId(_profile.EnableInterstitial, AdFormat.Interstitial, _profile.InterstitialUnitId);
            var rewardedId = ResolveUnitId(_profile.EnableRewarded, AdFormat.Rewarded, _profile.RewardedUnitId);
            var appOpenId = ResolveUnitId(_profile.EnableAppOpen, AdFormat.AppOpen, _profile.AppOpenUnitId);

            _banner = bannerId != null ? new AdMobBannerHandler(bannerId) : null;
            _interstitial = interstitialId != null ? new AdMobInterstitialHandler(interstitialId) : null;
            _rewarded = rewardedId != null ? new AdMobRewardedHandler(rewardedId) : null;
            _appOpen = appOpenId != null ? new AdMobAppOpenHandler(appOpenId) : null;

            if (_banner != null) { _banner.Loaded += () => Raise(AdsEventType.Loaded, AdFormat.Banner); _banner.LoadFailed += message => Raise(AdsEventType.Failed, AdFormat.Banner, message: message); }
            if (_interstitial != null) { _interstitial.Loaded += () => { _interstitialRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Interstitial); }; _interstitial.LoadFailed += message => RetryLoad(AdFormat.Interstitial, message); }
            if (_rewarded != null) { _rewarded.Loaded += () => { _rewardedRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Rewarded); }; _rewarded.LoadFailed += message => RetryLoad(AdFormat.Rewarded, message); }
            if (_appOpen != null) { _appOpen.Loaded += () => { _appOpenRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.AppOpen); }; _appOpen.LoadFailed += message => RetryLoad(AdFormat.AppOpen, message); }

            if (_banner != null) _banner.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Banner, revenue: revenue);
            if (_interstitial != null) _interstitial.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Interstitial, _showingPlacement, revenue: revenue);
            if (_rewarded != null) _rewarded.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Rewarded, _showingPlacement, revenue: revenue);
            if (_appOpen != null) _appOpen.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.AppOpen, revenue: revenue);
        }

        private static string ResolveUnitId(bool enabled, AdFormat format, string configured)
        {
            if (!enabled)
            {
                return null;
            }

            var id = AdUnitIdOverrides.Resolve(AdMobProviderFactory.Id, Application.platform, format, configured);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[RippleGame Ads] AdMob {format} is enabled but has no ad unit id; the format is disabled.");
                return null;
            }

            return id;
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
                CreateHandlers();
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
            if (!_initialized)
            {
                return false;
            }

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
            if (!CanShowFullScreen(_interstitial != null && _interstitial.IsReady, AdFormat.Interstitial, placement, completed))
            {
                return;
            }

            _showingPlacement = placement;
            if (!_interstitial.Show(
                    () => Raise(AdsEventType.Shown, AdFormat.Interstitial, placement),
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
            if (!CanShowFullScreen(_appOpen != null && _appOpen.IsReady, AdFormat.AppOpen, AdPlacement.Default, completed))
            {
                return;
            }

            if (!_appOpen.Show(
                    () => Raise(AdsEventType.Shown, AdFormat.AppOpen),
                    () => FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.Closed, completed, LoadAppOpen),
                    error => FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.Failed, completed, LoadAppOpen, error)))
            {
                FinishFullScreen(AdFormat.AppOpen, AdPlacement.Default, AdsResult.NotReady, completed, LoadAppOpen);
            }
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

        private void FinishFullScreen(AdFormat format, AdPlacement placement, AdsResult result, Action<AdsResult> completed, Action reload, string error = null)
        {
            _fullScreenShowing = false;
            try
            {
                Raise(result == AdsResult.Failed ? AdsEventType.ShowFailed : AdsEventType.Closed, format, placement, error);
                completed?.Invoke(result);
            }
            finally
            {
                // A throwing game callback must not leave the format without an ad for the rest of the session.
                if (!_disposeToken.IsCancellationRequested)
                {
                    reload();
                }
            }
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

        /// <summary>Keeps retrying until a load succeeds or the service is disposed, so a format is never left empty.</summary>
        private async void RetryAfterDelay(Action loader, int attempt)
        {
            if (_disposeToken.IsCancellationRequested)
            {
                return;
            }

            var delay = AdsLoadRetry.DelaySeconds(attempt, _profile.RetryMaxAttempts, _profile.RetryInitialDelaySeconds, _profile.RetryMaxDelaySeconds);
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
