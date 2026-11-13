using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Single public entry point used by gameplay and UI code.</summary>
    public static class Ads
    {
        private static IAdsService _service = new NoAdsService();

        public static event Action<AdsEvent> EventRaised;

        public static AdsEnvironment Environment => _service.Environment;

        public static Task<AdsResult> InitializeAsync() => _service.InitializeAsync();
        public static bool IsReady(AdFormat format) => _service.IsReady(format);
        public static void ShowBanner(BannerPosition position = BannerPosition.Bottom) => _service.ShowBanner(position);
        public static void HideBanner() => _service.HideBanner();
        public static void DestroyBanner() => _service.DestroyBanner();
        public static void ShowMrec(MrecPosition position = MrecPosition.BottomCenter) => _service.ShowMrec(position);
        public static void HideMrec() => _service.HideMrec();
        public static void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null) => _service.ShowInterstitial(placement, completed);
        public static void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null) => _service.ShowRewarded(placement, rewarded, completed);
        public static void ShowAppOpen(Action<AdsResult> completed = null) => _service.ShowAppOpen(completed);

        /// <summary>Latest game state supplied through <see cref="SetPolicyContext"/>; null until game code sets it.</summary>
        public static AdsPolicyContext? PolicyContext { get; private set; }

        /// <summary>Raised after <see cref="SetPolicyContext"/> stores a new context.</summary>
        internal static event Action PolicyContextChanged;

        /// <summary>Awaited by the policy layer before showing ads (e.g. a consent flow). Null means no wait.</summary>
        internal static Func<Task> InitializationGate;

        public static void SetPolicyContext(AdsPolicyContext context)
        {
            PolicyContext = context;
            PolicyContextChanged?.Invoke();
        }

        private static Task _initializationGateTask;
        private static AdsPolicyOverride _policyOverride;
        private static bool? _adsEnabled;
        private static bool? _isOrganic;

        internal static IAdsService CurrentService => _service;

        /// <summary>The override last handed to <see cref="ApplyPolicyOverride"/>. Lets bridges and tests read what was applied.</summary>
        internal static AdsPolicyOverride CurrentPolicyOverride => _policyOverride;

        /// <summary>
        /// <paramref name="asset"/> with the current remote override applied (a new instance). Providers read banner and
        /// MREC refresh from it when they initialize, which is after the initialization gate.
        /// </summary>
        internal static AdsPolicySettings EffectivePolicy(AdsPolicySettings asset)
        {
            if (_policyOverride != null) return _policyOverride.ApplyTo(asset);
            return asset != null ? asset.Clone() : new AdsPolicySettings();
        }

        /// <summary>The value last handed to <see cref="SetAdsEnabled"/>; null until something sets it.</summary>
        internal static bool? AdsEnabledValue => _adsEnabled;

        /// <summary>The value last handed to <see cref="SetOrganic"/>; null until something sets it.</summary>
        internal static bool? OrganicValue => _isOrganic;

        internal static void Configure(IAdsService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _service.EventRaised -= ForwardEvent;
            _service.Dispose();
            _service = service;
            _service.EventRaised += ForwardEvent;

            if (_service is PolicyAdsService policy)
            {
                if (_policyOverride != null) policy.SetOverride(_policyOverride);
                if (_adsEnabled.HasValue) policy.SetAdsEnabled(_adsEnabled.Value);
                if (_isOrganic.HasValue) policy.SetOrganic(_isOrganic.Value);
            }
        }

        /// <summary>Policy values from remote config. Ignored once this session's rules are fixed.</summary>
        internal static void ApplyPolicyOverride(AdsPolicyOverride value)
        {
            _policyOverride = value;
            (_service as PolicyAdsService)?.SetOverride(value);
        }

        /// <summary>The <c>ads_enabled</c> kill switch. Takes effect immediately.</summary>
        internal static void SetAdsEnabled(bool enabled)
        {
            _adsEnabled = enabled;
            (_service as PolicyAdsService)?.SetAdsEnabled(enabled);
        }

        /// <summary>Attribution result: organic users follow the organic policy switches.</summary>
        internal static void SetOrganic(bool isOrganic)
        {
            _isOrganic = isOrganic;
            (_service as PolicyAdsService)?.SetOrganic(isOrganic);
        }

        /// <summary>
        /// Runs <see cref="InitializationGate"/> once and shares the wait with every caller. Never faults:
        /// a failing gate is logged and ads continue to initialize.
        /// </summary>
        internal static Task WaitForInitializationGateAsync()
        {
            if (_initializationGateTask != null)
            {
                return _initializationGateTask;
            }

            var gate = InitializationGate;
            if (gate == null)
            {
                return Task.CompletedTask;
            }

            Task gateTask;
            try
            {
                gateTask = gate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                gateTask = null;
            }

            _initializationGateTask = gateTask == null ? Task.CompletedTask : ObserveGate(gateTask);
            return _initializationGateTask;
        }

        private static async Task ObserveGate(Task gateTask)
        {
            try
            {
                await gateTask;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForTests();

        /// <summary>Clears every static so state never leaks across play sessions (domain reload off) or tests.</summary>
        internal static void ResetForTests()
        {
            var previous = _service;
            _service = new NoAdsService();
            if (previous != null)
            {
                previous.EventRaised -= ForwardEvent;
                try
                {
                    previous.Dispose();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            PolicyContext = null;
            PolicyContextChanged = null;
            InitializationGate = null;
            _initializationGateTask = null;
            _policyOverride = null;
            _adsEnabled = null;
            _isOrganic = null;
            AdUnitIdOverrides.Clear();
            AdsSession.Reset();
        }

        private static void ForwardEvent(AdsEvent adsEvent) => EventRaised?.Invoke(adsEvent);
    }
}
