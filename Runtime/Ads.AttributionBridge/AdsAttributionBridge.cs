using RippleGame.Attribution;
using UnityEngine;
using AdsApi = RippleGame.Ads.Ads;
using AttributionApi = RippleGame.Attribution.Attribution;

namespace RippleGame.Ads.AttributionBridge
{
    /// <summary>
    /// Connects Ads and Attribution. Paid ad events go to Attribution as revenue, and the attribution status feeds the
    /// organic rules of the Ads policy layer (Unknown counts as not organic). Everything runs on the main thread.
    /// </summary>
    internal static class AdsAttributionBridge
    {
        private const double MicrosPerUnit = 1_000_000.0;

        internal static bool IsInstalled { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForTests();

        internal static void ResetForTests()
        {
            // Also keeps state clean when Domain Reload is disabled.
            if (IsInstalled)
            {
                AdsApi.EventRaised -= OnAdsEvent;
                AttributionApi.StatusChanged -= OnStatusChanged;
            }

            IsInstalled = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Install()
        {
            if (IsInstalled) return;
            IsInstalled = true;

            AdsApi.EventRaised += OnAdsEvent;
            AttributionApi.StatusChanged += OnStatusChanged;
            ApplyOrganic();
        }

        /// <summary>Forwards a Paid event as revenue in currency units. The value is passed on as Ads raised it.</summary>
        internal static void Handle(AdsEvent adsEvent)
        {
            if (adsEvent.Type != AdsEventType.Paid || !adsEvent.Revenue.HasValue) return;

            var revenue = adsEvent.Revenue.Value;
            AttributionApi.LogAdRevenue(new AttributionAdRevenue(
                revenue.ValueMicros / MicrosPerUnit,
                revenue.CurrencyCode,
                revenue.Platform,
                revenue.AdUnitId,
                revenue.AdSourceName,
                adsEvent.Format.ToString(),
                adsEvent.Placement.ToString()));
        }

        private static void OnAdsEvent(AdsEvent adsEvent) => Handle(adsEvent);

        private static void OnStatusChanged(AttributionStatus status) => ApplyOrganic();

        private static void ApplyOrganic() => AdsApi.SetOrganic(AttributionApi.Status == AttributionStatus.Organic);
    }
}
