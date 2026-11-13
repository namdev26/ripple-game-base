using System;
using System.Threading.Tasks;

namespace RippleGame.Ads
{
    public enum AdsEnvironment
    {
        Disabled,
        Test,
        Production
    }

    public enum AdFormat
    {
        Banner,
        Interstitial,
        Rewarded,
        AppOpen,
        Mrec
    }

    public enum BannerPosition
    {
        Top,
        Bottom
    }

    public enum MrecPosition
    {
        TopCenter,
        Center,
        BottomCenter
    }

    public enum AdPlacement
    {
        Default,
        Home,
        LevelStart,
        LevelComplete,
        Retry,
        DoubleReward,
        ExtraLife,
        Pause,
        Replay,
        RewardedFallback
    }

    public enum AdsResult
    {
        Success,
        Closed,
        NotReady,
        Disabled,
        Failed,
        Cooldown,
        AlreadyShowing
    }

    public enum AdsEventType
    {
        Loaded,
        Failed,
        Shown,
        Closed,
        RewardEarned,
        /// <summary>An impression earned revenue. <see cref="AdsEvent.Revenue"/> is set.</summary>
        Paid,
        /// <summary>A show request failed (not ready or SDK error). Load failures use <see cref="Failed"/>.</summary>
        ShowFailed
    }

    /// <summary>Impression revenue reported by the ad network.</summary>
    public readonly struct AdRevenue
    {
        public const string PlatformAdMob = "admob";
        public const string PlatformMax = "applovin_max";

        public AdRevenue(long valueMicros, string currencyCode, int precision, string adUnitId, string adSourceName)
            : this(valueMicros, currencyCode, precision, adUnitId, adSourceName, PlatformAdMob)
        {
        }

        public AdRevenue(long valueMicros, string currencyCode, int precision, string adUnitId, string adSourceName, string platform)
        {
            ValueMicros = valueMicros;
            CurrencyCode = currencyCode;
            Precision = precision;
            AdUnitId = adUnitId;
            AdSourceName = adSourceName;
            Platform = platform;
        }

        /// <summary>Revenue in micros of <see cref="CurrencyCode"/> (1,000,000 = 1 unit).</summary>
        public long ValueMicros { get; }
        public string CurrencyCode { get; }
        public int Precision { get; }
        public string AdUnitId { get; }
        public string AdSourceName { get; }
        /// <summary>Mediation platform that reported the revenue, e.g. <see cref="PlatformAdMob"/>.</summary>
        public string Platform { get; }
    }

    public readonly struct AdsEvent
    {
        public AdsEvent(AdsEventType type, AdFormat format, AdPlacement placement, string message = null, AdRevenue? revenue = null)
        {
            Type = type;
            Format = format;
            Placement = placement;
            Message = message;
            Revenue = revenue;
        }

        public AdsEventType Type { get; }
        public AdFormat Format { get; }
        public AdPlacement Placement { get; }
        public string Message { get; }
        public AdRevenue? Revenue { get; }
    }

    /// <summary>
    /// The provider contract. Implementations must raise <see cref="EventRaised"/> and invoke every
    /// completion and reward callback on the Unity main thread, marshalling SDK callbacks that arrive on
    /// other threads (see <c>MainThreadDispatcher</c>).
    /// </summary>
    public interface IAdsService : IDisposable
    {
        event Action<AdsEvent> EventRaised;

        AdsEnvironment Environment { get; }
        Task<AdsResult> InitializeAsync();
        bool IsReady(AdFormat format);
        void ShowBanner(BannerPosition position);
        void HideBanner();
        void DestroyBanner();
        void ShowMrec(MrecPosition position);
        void HideMrec();
        void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null);
        void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null);
        void ShowAppOpen(Action<AdsResult> completed = null);
    }

    /// <summary>Implemented by optional provider packages to extend the package without changing game code.</summary>
    public interface IAdsProviderFactory
    {
        string ProviderId { get; }
        IAdsService Create(AdsConfiguration configuration);
    }

    internal interface IAdsLifecycleAware
    {
        void OnApplicationFocusChanged(bool hasFocus);
        void OnApplicationPauseChanged(bool isPaused);
    }
}
