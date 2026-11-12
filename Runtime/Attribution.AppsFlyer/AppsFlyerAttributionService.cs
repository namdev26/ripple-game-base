using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;
using AppsFlyerApi = AppsFlyerSDK.AppsFlyer;

namespace RippleGame.Attribution.AppsFlyer
{
    /// <summary>AppsFlyer attribution provider. This is the only file that knows the AppsFlyer SDK API besides the host.</summary>
    internal sealed class AppsFlyerAttributionService : IAttributionService
    {
        private const string PlatformMax = "applovin_max";
        private const string PlatformAdMob = "admob";

        private bool _started;
        private bool _warnedPlatform;

        /// <summary>Registers this provider. The Editor has no AppsFlyer backend, so it is skipped there.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            if (Application.isEditor) return;
            Attribution.RegisterService(new AppsFlyerAttributionService());
        }

        public void Initialize(AttributionSettings settings)
        {
            if (string.IsNullOrEmpty(settings.DevKey))
            {
                Debug.LogWarning(Attribution.LogPrefix + "AppsFlyer dev key is empty. Attribution is off.");
                return;
            }

            var host = AppsFlyerHost.Create(settings.DebugLog);
            AppsFlyerApi.setIsDebug(settings.DebugLog);
            AppsFlyerApi.initSDK(settings.DevKey, settings.IosAppId, host);
            AppsFlyerApi.startSDK();
            _started = true;
        }

        public void LogAdRevenue(AttributionAdRevenue revenue)
        {
            if (!_started) return;

            if (!TryGetMediationNetwork(revenue.Platform, out var network))
            {
                if (!_warnedPlatform)
                {
                    _warnedPlatform = true;
                    Debug.LogWarning(Attribution.LogPrefix + "AppsFlyer ad revenue skipped: unknown ad platform '" + revenue.Platform + "'.");
                }

                return;
            }

            if (string.IsNullOrEmpty(revenue.Currency)) return;

            var monetization = string.IsNullOrEmpty(revenue.AdSource) ? revenue.Platform : revenue.AdSource;
            var data = new AFAdRevenueData(monetization, network, revenue.Currency, revenue.Value);

            var extra = new Dictionary<string, string>();
            Add(extra, AdRevenueScheme.AD_UNIT, revenue.AdUnitId);
            Add(extra, AdRevenueScheme.AD_TYPE, revenue.Format);
            Add(extra, AdRevenueScheme.PLACEMENT, revenue.Placement);

            AppsFlyerApi.logAdRevenue(data, extra);
        }

        internal static bool TryGetMediationNetwork(string platform, out MediationNetwork network)
        {
            switch (platform)
            {
                case PlatformMax:
                    network = MediationNetwork.ApplovinMax;
                    return true;
                case PlatformAdMob:
                    network = MediationNetwork.GoogleAdMob;
                    return true;
                default:
                    network = default;
                    return false;
            }
        }

        private static void Add(Dictionary<string, string> map, string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) map[key] = value;
        }
    }
}
