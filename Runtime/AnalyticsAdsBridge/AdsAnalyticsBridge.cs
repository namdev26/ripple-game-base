using System.Text;
using RippleGame.Ads;
using UnityEngine;
using Names = RippleGame.Analytics.AnalyticsNames;

namespace RippleGame.Analytics.AdsBridge
{
    /// <summary>
    /// Logs ad_impression / ad_show / ad_show_fail / ad_reward_earned from RippleGame Ads events.
    /// Compiled only when com.ripplegame.ads 0.2.0+ is in the project.
    /// </summary>
    internal static class AdsAnalyticsBridge
    {
        private const string AdPlatform = "AdMob";
        private const double MicrosPerUnit = 1_000_000.0;

        internal static bool IsInstalled { get; private set; }
        internal static int InstallCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            if (IsInstalled) RippleGame.Ads.Ads.EventRaised -= OnAdsEvent;
            IsInstalled = false;
            InstallCount = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Install()
        {
            if (IsInstalled) return;
            RippleGame.Ads.Ads.EventRaised += OnAdsEvent;
            IsInstalled = true;
            InstallCount++;
        }

        internal static void Handle(AdsEvent adsEvent, bool logAdRevenue)
        {
            switch (adsEvent.Type)
            {
                case AdsEventType.Paid:
                    LogImpression(adsEvent, logAdRevenue);
                    break;
                case AdsEventType.Shown:
                    Analytics.LogEvent(Names.Events.AdShow, FormatAndPlacement(adsEvent));
                    break;
                case AdsEventType.ShowFailed:
                    Analytics.LogEvent(Names.Events.AdShowFail, FormatAndPlacement(adsEvent).Add(Names.Params.Error, adsEvent.Message));
                    break;
                case AdsEventType.RewardEarned:
                    Analytics.LogEvent(Names.Events.AdReward, new AnalyticsParams().Add(Names.Params.Placement, ToSnakeCase(adsEvent.Placement.ToString())));
                    break;
            }
        }

        internal static string ToSnakeCase(string pascal)
        {
            if (string.IsNullOrEmpty(pascal)) return pascal;
            var builder = new StringBuilder(pascal.Length + 4);
            for (var i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c) && i > 0) builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static void OnAdsEvent(AdsEvent adsEvent) => Handle(adsEvent, Analytics.Settings.LogAdRevenue);

        private static void LogImpression(AdsEvent adsEvent, bool logAdRevenue)
        {
            if (!logAdRevenue || !adsEvent.Revenue.HasValue) return;

            var revenue = adsEvent.Revenue.Value;
            if (string.IsNullOrEmpty(revenue.CurrencyCode))
            {
                if (Debug.isDebugBuild) Debug.LogWarning(Analytics.LogPrefix + "ad_impression skipped: ad network returned no currency code.");
                return;
            }

            Analytics.LogEvent(Names.Events.AdImpression, new AnalyticsParams()
                .Add(Names.Params.AdPlatform, AdPlatform)
                .Add(Names.Params.AdSource, revenue.AdSourceName)
                .Add(Names.Params.AdFormat, ToSnakeCase(adsEvent.Format.ToString()))
                .Add(Names.Params.AdUnitName, revenue.AdUnitId)
                .Add(Names.Params.Value, revenue.ValueMicros / MicrosPerUnit)
                .Add(Names.Params.Currency, revenue.CurrencyCode)
                .Add(Names.Params.Placement, ToSnakeCase(adsEvent.Placement.ToString())));
        }

        private static AnalyticsParams FormatAndPlacement(AdsEvent adsEvent) =>
            new AnalyticsParams()
                .Add(Names.Params.AdFormat, ToSnakeCase(adsEvent.Format.ToString()))
                .Add(Names.Params.Placement, ToSnakeCase(adsEvent.Placement.ToString()));
    }
}
