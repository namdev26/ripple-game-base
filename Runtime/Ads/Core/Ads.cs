using System;
using System.Threading.Tasks;

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

        internal static IAdsService CurrentService => _service;

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
        }

        private static void ForwardEvent(AdsEvent adsEvent) => EventRaised?.Invoke(adsEvent);
    }
}
