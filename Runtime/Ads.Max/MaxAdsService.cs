using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RippleGame.Ads.Max
{
    /// <summary>
    /// AppLovin MAX implementation: load, show and retry only. Cooldowns, session caps and app-open
    /// resume rules live in the shared policy layer that wraps this service.
    /// </summary>
    internal sealed class MaxAdsService : IAdsService
    {
        private const float ReachabilityPollSeconds = 1f;

        private readonly AdsConfiguration _configuration;
        private readonly MaxAdUnitConfiguration _profile;
        // MAX reuses the retry settings of the active AdMob profile.
        private readonly AdMobAdUnitConfiguration _retry;
        private readonly CancellationTokenSource _disposeToken = new CancellationTokenSource();
        // Created in InitializeAsync, after the initialization gate, so remote unit ids and policy apply. Null before that.
        private MaxBannerHandler _banner;
        private MaxMrecHandler _mrec;
        private MaxInterstitialHandler _interstitial;
        private MaxRewardedHandler _rewarded;
        private MaxAppOpenHandler _appOpen;

        private Task<AdsResult> _initializationTask;
        private Action<MaxSdkBase.SdkConfiguration> _sdkInitializedHandler;
        private MaxUpdateHook _updateHook;
        private bool _initialized;
        private bool _disposed;
        private bool _fullScreenShowing;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private int _appOpenRetryAttempt;
        private AdPlacement _showingPlacement;
        private AdFormat _showingFormat;
        private NetworkReachability _lastReachability;
        private float _nextReachabilityPollAt;

        internal MaxAdsService(AdsConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _profile = configuration.GetActiveMaxProfile();
            _retry = configuration.GetActiveProfile();
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

                var consent = Ads.PolicyContext?.HasUserConsent;
                if (consent.HasValue)
                {
                    MaxSdk.SetHasUserConsent(consent.Value);
                }

#if !UNITY_EDITOR
                if (!string.IsNullOrEmpty(_profile.SdkKey))
                {
                    // Deprecated in favour of the Integration Manager key, but still forwarded to the native SDK in 8.5.1,
                    // which lets each AdsConfiguration profile carry its own key. The editor stub only logs a warning.
#pragma warning disable CS0618
                    MaxSdk.SetSdkKey(_profile.SdkKey);
#pragma warning restore CS0618
                }
#endif

                _sdkInitializedHandler = sdkConfiguration => MainThreadDispatcher.Post(() =>
                {
                    MaxSdkCallbacks.OnSdkInitializedEvent -= _sdkInitializedHandler;
                    if (_disposed)
                    {
                        completion.TrySetResult(AdsResult.Failed);
                        return;
                    }

                    _initialized = true;
                    StartUpdateHook();
                    LoadInitialAds();
                    completion.TrySetResult(AdsResult.Success);
                });
                MaxSdkCallbacks.OnSdkInitializedEvent += _sdkInitializedHandler;
                MaxSdk.InitializeSdk();
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
                AdFormat.Mrec => _mrec != null,
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

            _banner.Show(position, Now);
        }

        public void HideBanner() => _banner?.Hide();
        public void DestroyBanner() => _banner?.Destroy();

        public void ShowMrec(MrecPosition position)
        {
            if (!_initialized || _mrec == null)
            {
                Raise(AdsEventType.ShowFailed, AdFormat.Mrec, message: "MREC is disabled or the SDK is not initialized.");
                return;
            }

            _mrec.Show(position, Now);
        }

        public void HideMrec() => _mrec?.Hide();

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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _disposeToken.Cancel();
            if (_sdkInitializedHandler != null)
            {
                MaxSdkCallbacks.OnSdkInitializedEvent -= _sdkInitializedHandler;
            }

            _updateHook?.Release();
            _updateHook = null;
            _banner?.Dispose();
            _mrec?.Dispose();
            _interstitial?.Dispose();
            _rewarded?.Dispose();
            _appOpen?.Dispose();
            _disposeToken.Dispose();
        }

        private static float Now => Time.realtimeSinceStartup;

        /// <summary>
        /// Resolves unit ids (remote overrides first) and the effective banner/MREC refresh, then builds the format
        /// handlers. Runs once, from InitializeAsync, which the policy layer calls after the initialization gate.
        /// </summary>
        private void CreateHandlers()
        {
            var policy = Ads.EffectivePolicy(_configuration.Policy);
#if UNITY_IOS
            var ids = _profile.Ios;
#else
            var ids = _profile.Android;
#endif

            var bannerId = ResolveUnitId(_profile.EnableBanner, AdFormat.Banner, ids.Banner);
            var mrecId = ResolveUnitId(_profile.EnableMrec, AdFormat.Mrec, ids.Mrec);
            var interstitialId = ResolveUnitId(_profile.EnableInterstitial, AdFormat.Interstitial, ids.Interstitial);
            var rewardedId = ResolveUnitId(_profile.EnableRewarded, AdFormat.Rewarded, ids.Rewarded);
            var appOpenId = ResolveUnitId(_profile.EnableAppOpen, AdFormat.AppOpen, ids.AppOpen);

            _banner = bannerId != null ? new MaxBannerHandler(bannerId, policy.BannerRefreshSeconds) : null;
            _mrec = mrecId != null ? new MaxMrecHandler(mrecId, policy.MrecRefreshSeconds) : null;
            _interstitial = interstitialId != null ? new MaxInterstitialHandler(interstitialId) : null;
            _rewarded = rewardedId != null ? new MaxRewardedHandler(rewardedId) : null;
            _appOpen = appOpenId != null ? new MaxAppOpenHandler(appOpenId) : null;

            if (_banner != null) { _banner.Loaded += () => Raise(AdsEventType.Loaded, AdFormat.Banner); _banner.LoadFailed += message => Raise(AdsEventType.Failed, AdFormat.Banner, message: message); }
            if (_mrec != null) { _mrec.Loaded += () => Raise(AdsEventType.Loaded, AdFormat.Mrec); _mrec.LoadFailed += message => Raise(AdsEventType.Failed, AdFormat.Mrec, message: message); }
            if (_interstitial != null) { _interstitial.Loaded += () => { _interstitialRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Interstitial); }; _interstitial.LoadFailed += message => RetryLoad(AdFormat.Interstitial, message); }
            if (_rewarded != null) { _rewarded.Loaded += () => { _rewardedRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.Rewarded); }; _rewarded.LoadFailed += message => RetryLoad(AdFormat.Rewarded, message); }
            if (_appOpen != null) { _appOpen.Loaded += () => { _appOpenRetryAttempt = 0; Raise(AdsEventType.Loaded, AdFormat.AppOpen); }; _appOpen.LoadFailed += message => RetryLoad(AdFormat.AppOpen, message); }

            if (_banner != null) _banner.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Banner, revenue: revenue);
            if (_mrec != null) _mrec.Paid += revenue => Raise(AdsEventType.Paid, AdFormat.Mrec, revenue: revenue);
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

            var id = AdUnitIdOverrides.Resolve(MaxProviderFactory.Id, Application.platform, format, configured);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[RippleGame Ads] MAX {format} is enabled but has no ad unit id for this platform; the format is disabled.");
                return null;
            }

            return id;
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
            _showingFormat = format;
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
                if (!_disposed)
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
            if (_disposed)
            {
                return;
            }

            var delay = AdsLoadRetry.DelaySeconds(attempt, _retry.RetryMaxAttempts, _retry.RetryInitialDelaySeconds, _retry.RetryMaxDelaySeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), _disposeToken.Token);
                MainThreadDispatcher.Post(() =>
                {
                    if (!_disposed)
                    {
                        loader();
                    }
                });
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private void StartUpdateHook()
        {
            if (_updateHook != null)
            {
                return;
            }

            _lastReachability = Application.internetReachability;
            _nextReachabilityPollAt = Now + ReachabilityPollSeconds;
            _updateHook = MaxUpdateHook.Create();
            _updateHook.Ticked += Tick;
        }

        private void Tick()
        {
            if (_disposed)
            {
                return;
            }

            var now = Now;
            _banner?.Tick(now);
            _mrec?.Tick(now);

            if (now < _nextReachabilityPollAt)
            {
                return;
            }

            _nextReachabilityPollAt = now + ReachabilityPollSeconds;
            var reachability = Application.internetReachability;
            var reconnected = _lastReachability == NetworkReachability.NotReachable && reachability != NetworkReachability.NotReachable;
            _lastReachability = reachability;
            if (reconnected)
            {
                ReloadMissingAds();
            }
        }

        /// <summary>Back online: reload every enabled format without an ad, starting a fresh backoff sequence.</summary>
        private void ReloadMissingAds()
        {
            if (_banner != null && _banner.NeedsReload) _banner.Reload();
            if (_mrec != null && _mrec.NeedsReload) _mrec.Reload();

            // A showing full-screen format reloads itself when it closes.
            if (_interstitial != null && !IsShowing(AdFormat.Interstitial) && !_interstitial.IsReady && !_interstitial.IsLoading)
            {
                _interstitialRetryAttempt = 0;
                _interstitial.Load();
            }

            if (_rewarded != null && !IsShowing(AdFormat.Rewarded) && !_rewarded.IsReady && !_rewarded.IsLoading)
            {
                _rewardedRetryAttempt = 0;
                _rewarded.Load();
            }

            if (_appOpen != null && !IsShowing(AdFormat.AppOpen) && !_appOpen.IsReady && !_appOpen.IsLoading)
            {
                _appOpenRetryAttempt = 0;
                _appOpen.Load();
            }
        }

        private bool IsShowing(AdFormat format) => _fullScreenShowing && _showingFormat == format;

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
