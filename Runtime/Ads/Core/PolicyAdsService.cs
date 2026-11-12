using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>
    /// Wraps a provider and owns every show rule: entitlements, the ads kill switch, organic limits,
    /// interstitial cooldown / session cap / min level / start session / placements, app open on resume,
    /// rewarded → interstitial fallback, one full-screen ad at a time, and pausing time and audio.
    /// The provider only loads and shows.
    /// </summary>
    internal sealed class PolicyAdsService : IAdsService, IAdsLifecycleAware
    {
        internal const string FallbackRewardMessage = "fallback_interstitial";

        private enum ShowKind
        {
            None,
            Interstitial,
            Rewarded,
            FallbackInterstitial,
            AppOpen
        }

        private readonly IAdsService _inner;
        private readonly AdsPolicySettings _settings;
        private readonly Func<float> _realtime;
        private readonly Func<int> _sessionNumber;
        private readonly Func<Task> _initializationGate;
        private readonly FullScreenPause _pause = new FullScreenPause();

        private AdsPolicyOverride _override;
        private AdsPolicySettings _snapshot;
        private AdsPolicySettings _preview;
        private Task<AdsResult> _initializationTask;
        private bool _adsEnabled = true;
        private bool _isOrganic;
        private bool _minLevelWarningLogged;
        private bool _disposed;

        private ShowKind _showing;
        private bool _impressionRecorded;
        private int _interstitialImpressions;
        private float _lastInterstitialTime = float.NegativeInfinity;
        private float _lastAppOpenTime = float.NegativeInfinity;
        private float _lastFullScreenClosedTime = float.NegativeInfinity;
        private bool _backgrounded;
        private float _backgroundedAt;

        /// <param name="initializationGate">Awaited before the provider initializes (e.g. remote config). Optional.</param>
        internal PolicyAdsService(IAdsService inner, AdsPolicySettings settings, Func<float> realtime, Func<int> sessionNumber, Func<Task> initializationGate = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _settings = settings ?? new AdsPolicySettings();
            _realtime = realtime ?? throw new ArgumentNullException(nameof(realtime));
            _sessionNumber = sessionNumber ?? throw new ArgumentNullException(nameof(sessionNumber));
            _initializationGate = initializationGate;

            _inner.EventRaised += OnInnerEvent;
            Ads.PolicyContextChanged += OnPolicyContextChanged;
        }

        public event Action<AdsEvent> EventRaised;

        public AdsEnvironment Environment => _inner.Environment;

        private float Now => _realtime();

        /// <summary>Rules in force: the session snapshot once taken, otherwise what the snapshot would be now.</summary>
        private AdsPolicySettings Current => _snapshot ?? (_preview ??= BuildSettings());

        private bool AdsRemoved
        {
            get
            {
                var entitlement = Ads.PolicyContext?.Entitlement ?? AdsEntitlement.None;
                return entitlement == AdsEntitlement.RemoveAds || entitlement == AdsEntitlement.RemoveAdsAndFreeRewards;
            }
        }

        private bool GrantsFreeRewards => !_adsEnabled || Ads.PolicyContext?.Entitlement == AdsEntitlement.RemoveAdsAndFreeRewards;

        // ---- Runtime inputs ----

        /// <summary>Applies until the session snapshot is taken; later values wait for the next session.</summary>
        internal void SetOverride(AdsPolicyOverride value)
        {
            if (_snapshot != null)
            {
                Debug.Log("[RippleGame Ads] Ads policy override arrived after this session's rules were fixed; it applies from the next session.");
                return;
            }

            _override = value;
            _preview = null;
            HideBlockedBannerAndMrec();
        }

        /// <summary>Kill switch. false: no ad shows and rewarded grants its reward at once. Takes effect immediately.</summary>
        internal void SetAdsEnabled(bool enabled)
        {
            _adsEnabled = enabled;
            HideBlockedBannerAndMrec();
        }

        internal void SetOrganic(bool isOrganic)
        {
            _isOrganic = isOrganic;
            HideBlockedBannerAndMrec();
        }

        // ---- IAdsService ----

        public Task<AdsResult> InitializeAsync() => _initializationTask ??= InitializeCoreAsync();

        private async Task<AdsResult> InitializeCoreAsync()
        {
            try
            {
                var gate = _initializationGate?.Invoke();
                if (gate != null)
                {
                    try
                    {
                        await gate;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                return await _inner.InitializeAsync();
            }
            finally
            {
                TakeSnapshot();
            }
        }

        public bool IsReady(AdFormat format)
        {
            var policy = Current;
            switch (format)
            {
                case AdFormat.Rewarded:
                    return GrantsFreeRewards || _inner.IsReady(AdFormat.Rewarded) || CanShowFallback(policy);
                case AdFormat.Interstitial:
                    return _adsEnabled && !AdsRemoved && OrganicAllows(policy.Organic.Interstitial) && _inner.IsReady(format);
                case AdFormat.AppOpen:
                    return _adsEnabled && !AdsRemoved && policy.AppOpen.Enabled && OrganicAllows(policy.Organic.AppOpen) && _inner.IsReady(format);
                case AdFormat.Mrec:
                    return CanShowMrec(policy) && _inner.IsReady(format);
                default:
                    return CanShowBanner(policy) && _inner.IsReady(format);
            }
        }

        public void ShowBanner(BannerPosition position)
        {
            if (CanShowBanner(Current))
            {
                _inner.ShowBanner(position);
            }
        }

        public void HideBanner() => _inner.HideBanner();
        public void DestroyBanner() => _inner.DestroyBanner();

        public void ShowMrec(MrecPosition position)
        {
            if (CanShowMrec(Current))
            {
                _inner.ShowMrec(position);
            }
        }

        public void HideMrec() => _inner.HideMrec();

        public void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null)
        {
            var policy = TakeSnapshot();
            var blocked = CheckInterstitial(policy, placement);
            if (blocked.HasValue)
            {
                completed?.Invoke(blocked.Value);
                return;
            }

            var finish = BeginFullScreen(ShowKind.Interstitial, policy, completed);
            Run(() => _inner.ShowInterstitial(placement, finish), finish);
        }

        public void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null)
        {
            var policy = TakeSnapshot();
            if (GrantsFreeRewards)
            {
                rewarded?.Invoke();
                completed?.Invoke(AdsResult.Success);
                return;
            }

            if (_showing != ShowKind.None)
            {
                completed?.Invoke(AdsResult.AlreadyShowing);
                return;
            }

            if (!_inner.IsReady(AdFormat.Rewarded) && CanShowFallback(policy))
            {
                ShowFallbackInterstitial(policy, placement, rewarded, completed);
                return;
            }

            // The provider may report the reward more than once (or after close); grant it once per show.
            var granted = false;
            void GrantOnce()
            {
                if (granted) return;
                granted = true;
                rewarded?.Invoke();
            }

            var finish = BeginFullScreen(ShowKind.Rewarded, policy, completed);
            Run(() => _inner.ShowRewarded(placement, GrantOnce, finish), finish);
        }

        public void ShowAppOpen(Action<AdsResult> completed = null)
        {
            var policy = TakeSnapshot();
            AdsResult? blocked = null;
            if (!_adsEnabled || AdsRemoved || !policy.AppOpen.Enabled || !OrganicAllows(policy.Organic.AppOpen))
            {
                blocked = AdsResult.Disabled;
            }
            else if (_showing != ShowKind.None)
            {
                blocked = AdsResult.AlreadyShowing;
            }
            else if (Now - _lastAppOpenTime < policy.AppOpen.CooldownSeconds)
            {
                blocked = AdsResult.Cooldown;
            }

            if (blocked.HasValue)
            {
                completed?.Invoke(blocked.Value);
                return;
            }

            var finish = BeginFullScreen(ShowKind.AppOpen, policy, completed);
            Run(() => _inner.ShowAppOpen(finish), finish);
        }

        // ---- App open on resume ----

        public void OnApplicationPauseChanged(bool isPaused)
        {
            // A full-screen ad also pauses the app on Android; that is not the player leaving.
            if (!isPaused || _showing != ShowKind.None)
            {
                return;
            }

            _backgrounded = true;
            _backgroundedAt = Now;
        }

        public void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus || !_backgrounded)
            {
                return;
            }

            _backgrounded = false;
            var policy = Current;
            var now = Now;
            if (now - _backgroundedAt < policy.AppOpen.MinBackgroundSeconds ||
                _showing != ShowKind.None ||
                now - _lastFullScreenClosedTime < policy.AppOpen.CooldownSeconds ||
                !_inner.IsReady(AdFormat.AppOpen))
            {
                return;
            }

            ShowAppOpen();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Ads.PolicyContextChanged -= OnPolicyContextChanged;
            _inner.EventRaised -= OnInnerEvent;
            _pause.End();
            _inner.Dispose();
        }

        // ---- Rules ----

        private AdsResult? CheckInterstitial(AdsPolicySettings policy, AdPlacement placement)
        {
            var rules = policy.Interstitial;
            if (!_adsEnabled || AdsRemoved || !OrganicAllows(policy.Organic.Interstitial) ||
                rules.DisabledPlacements.Contains(placement) ||
                _sessionNumber() < rules.StartSession ||
                IsBelowMinLevel(rules.MinLevel))
            {
                return AdsResult.Disabled;
            }

            if (_showing != ShowKind.None)
            {
                return AdsResult.AlreadyShowing;
            }

            if ((rules.MaxPerSession > 0 && _interstitialImpressions >= rules.MaxPerSession) ||
                Now - _lastInterstitialTime < rules.CooldownSeconds)
            {
                return AdsResult.Cooldown;
            }

            return null;
        }

        private bool IsBelowMinLevel(int minLevel)
        {
            if (minLevel <= 0)
            {
                return false;
            }

            var context = Ads.PolicyContext;
            if (!context.HasValue)
            {
                if (!_minLevelWarningLogged)
                {
                    _minLevelWarningLogged = true;
                    Debug.LogWarning("[RippleGame Ads] Interstitial minLevel is set, but Ads.SetPolicyContext was never called; minLevel is ignored.");
                }

                return false;
            }

            return context.Value.CurrentLevel < minLevel;
        }

        /// <summary>Spec 4.3: no cooldown or min level check; organic and the RewardedFallback placement still apply.</summary>
        private bool CanShowFallback(AdsPolicySettings policy)
        {
            return policy.RewardedFallbackEnabled &&
                   _adsEnabled &&
                   !AdsRemoved &&
                   OrganicAllows(policy.Organic.Interstitial) &&
                   !policy.Interstitial.DisabledPlacements.Contains(AdPlacement.RewardedFallback) &&
                   _inner.IsReady(AdFormat.Interstitial);
        }

        private void ShowFallbackInterstitial(AdsPolicySettings policy, AdPlacement placement, Action rewarded, Action<AdsResult> completed)
        {
            // The reward lives only in this closure, so it can never be paid out by a later show.
            var finish = BeginFullScreen(ShowKind.FallbackInterstitial, policy, result =>
            {
                if (result != AdsResult.Closed && result != AdsResult.Success)
                {
                    completed?.Invoke(AdsResult.Failed);
                    return;
                }

                _lastInterstitialTime = Now;
                rewarded?.Invoke();
                Raise(AdsEventType.RewardEarned, AdFormat.Rewarded, placement, FallbackRewardMessage);
                completed?.Invoke(AdsResult.Success);
            });
            Run(() => _inner.ShowInterstitial(AdPlacement.RewardedFallback, finish), finish);
        }

        private bool CanShowBanner(AdsPolicySettings policy) => _adsEnabled && !AdsRemoved && OrganicAllows(policy.Organic.Banner);
        private bool CanShowMrec(AdsPolicySettings policy) => _adsEnabled && !AdsRemoved && OrganicAllows(policy.Organic.Mrec);
        private bool OrganicAllows(bool allowedForOrganic) => !_isOrganic || allowedForOrganic;

        private void HideBlockedBannerAndMrec()
        {
            if (_disposed)
            {
                return;
            }

            var policy = Current;
            if (!CanShowBanner(policy)) _inner.HideBanner();
            if (!CanShowMrec(policy)) _inner.HideMrec();
        }

        private void OnPolicyContextChanged() => HideBlockedBannerAndMrec();

        // ---- Settings snapshot ----

        private AdsPolicySettings TakeSnapshot()
        {
            if (_snapshot == null)
            {
                _snapshot = _preview ?? BuildSettings();
                _preview = null;
            }

            return _snapshot;
        }

        private AdsPolicySettings BuildSettings() => _override != null ? _override.ApplyTo(_settings) : _settings.Clone();

        // ---- Full-screen bookkeeping ----

        /// <summary>Marks a full-screen ad as showing and returns its single-use completion.</summary>
        private Action<AdsResult> BeginFullScreen(ShowKind kind, AdsPolicySettings policy, Action<AdsResult> onFinished)
        {
            _showing = kind;
            _impressionRecorded = false;
            if (policy.PauseTimeAndAudio)
            {
                _pause.Begin();
            }

            var finished = false;
            return result =>
            {
                if (finished) return;
                finished = true;
                EndFullScreen(kind, result);
                onFinished?.Invoke(result);
            };
        }

        private void EndFullScreen(ShowKind kind, AdsResult result)
        {
            if (result == AdsResult.Closed || result == AdsResult.Success)
            {
                RecordImpression(kind);
            }

            _showing = ShowKind.None;
            _lastFullScreenClosedTime = Now;
            _pause.End();
        }

        /// <summary>Starts cooldowns at the impression: the provider's Shown event, or the close if it sends none.</summary>
        private void RecordImpression(ShowKind kind)
        {
            if (_impressionRecorded || _showing != kind)
            {
                return;
            }

            _impressionRecorded = true;
            if (kind == ShowKind.Interstitial)
            {
                _interstitialImpressions++;
                _lastInterstitialTime = Now;
            }
            else if (kind == ShowKind.AppOpen)
            {
                _lastAppOpenTime = Now;
            }
        }

        private static void Run(Action show, Action<AdsResult> finish)
        {
            try
            {
                show();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                finish(AdsResult.Failed);
            }
        }

        private void OnInnerEvent(AdsEvent adsEvent)
        {
            if (adsEvent.Type == AdsEventType.Shown)
            {
                if (adsEvent.Format == AdFormat.Interstitial) RecordImpression(ShowKind.Interstitial);
                else if (adsEvent.Format == AdFormat.AppOpen) RecordImpression(ShowKind.AppOpen);
            }

            EventRaised?.Invoke(adsEvent);
        }

        private void Raise(AdsEventType type, AdFormat format, AdPlacement placement, string message)
        {
            EventRaised?.Invoke(new AdsEvent(type, format, placement, message));
        }
    }
}
