using System;
using UnityEngine;

namespace RippleGame.Ads
{
    [CreateAssetMenu(fileName = "AdsConfiguration", menuName = "TOOL/ADS/Ads Configuration")]
    public sealed class AdsConfiguration : ScriptableObject
    {
        [SerializeField] private string providerId = "admob";
        [SerializeField] private bool enableLogging = true;
        [Tooltip("Banner is shown in all build scenes except these scene names.")]
        [SerializeField] private string[] bannerExcludedSceneNames = { "Loading", "LoadingScene" };
        [SerializeField] private AdMobAdUnitConfiguration test = AdMobAdUnitConfiguration.CreateAndroidTest();
        [SerializeField] private AdMobAdUnitConfiguration production = new AdMobAdUnitConfiguration();

        public string ProviderId => providerId;
        public bool EnableLogging => enableLogging;
        public AdMobAdUnitConfiguration Test => test;
        public AdMobAdUnitConfiguration Production => production;

        public static AdsEnvironment ResolveCompileTimeEnvironment()
        {
#if ADS
    #if ADS_TEST
            return AdsEnvironment.Test;
    #else
            return AdsEnvironment.Production;
    #endif
#else
            return AdsEnvironment.Disabled;
#endif
        }

        public AdMobAdUnitConfiguration GetActiveProfile()
        {
            return ResolveCompileTimeEnvironment() == AdsEnvironment.Test ? test : production;
        }

        public bool ShouldShowBannerInScene(string sceneName, int buildIndex)
        {
            if (buildIndex < 0 || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (bannerExcludedSceneNames == null) return true;

            foreach (var excludedSceneName in bannerExcludedSceneNames)
            {
                if (string.Equals(excludedSceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class AdMobAdUnitConfiguration
    {
        [Header("Android")]
        [SerializeField] private string androidAppId;
        [SerializeField] private string bannerUnitId;
        [SerializeField] private string interstitialUnitId;
        [SerializeField] private string rewardedUnitId;
        [SerializeField] private string appOpenUnitId;

        [Header("Formats")]
        [SerializeField] private bool enableBanner = true;
        [SerializeField] private bool enableInterstitial = true;
        [SerializeField] private bool enableRewarded = true;
        [SerializeField] private bool enableAppOpen = true;
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;

        [Header("Loading")]
        [Min(0f)] [SerializeField] private float appOpenCooldownSeconds = 20f;
        [Min(0f)] [SerializeField] private float appOpenMinimumBackgroundSeconds = 5f;
        [Min(0)] [SerializeField] private int retryMaxAttempts = 3;
        [Min(0.1f)] [SerializeField] private float retryInitialDelaySeconds = 2f;
        [Min(0.1f)] [SerializeField] private float retryMaxDelaySeconds = 30f;

        [Header("Presentation")]
        [Tooltip("Minimum elapsed real time between two interstitial impressions.")]
        [Min(0f)] [SerializeField] private float interstitialCooldownSeconds = 45f;
        [Tooltip("Maximum interstitial impressions in one app session. Set 0 for no cap.")]
        [Min(0)] [SerializeField] private int interstitialMaxImpressionsPerSession = 4;

        // Google demo IDs: https://developers.google.com/admob/unity/test-ads
        public static AdMobAdUnitConfiguration CreateAndroidTest()
        {
            return new AdMobAdUnitConfiguration
            {
                androidAppId = "ca-app-pub-3940256099942544~3347511713",
                bannerUnitId = "ca-app-pub-3940256099942544/9214589741",
                interstitialUnitId = "ca-app-pub-3940256099942544/1033173712",
                rewardedUnitId = "ca-app-pub-3940256099942544/5224354917",
                appOpenUnitId = "ca-app-pub-3940256099942544/9257395921"
            };
        }

        public string AndroidAppId => androidAppId;
        public string BannerUnitId => bannerUnitId;
        public string InterstitialUnitId => interstitialUnitId;
        public string RewardedUnitId => rewardedUnitId;
        public string AppOpenUnitId => appOpenUnitId;
        public bool EnableBanner => enableBanner;
        public bool EnableInterstitial => enableInterstitial;
        public bool EnableRewarded => enableRewarded;
        public bool EnableAppOpen => enableAppOpen;
        public BannerPosition BannerPosition => bannerPosition;
        public float AppOpenCooldownSeconds => appOpenCooldownSeconds;
        public float AppOpenMinimumBackgroundSeconds => appOpenMinimumBackgroundSeconds;
        public int RetryMaxAttempts => retryMaxAttempts;
        public float RetryInitialDelaySeconds => retryInitialDelaySeconds;
        public float RetryMaxDelaySeconds => retryMaxDelaySeconds;
        public float InterstitialCooldownSeconds => interstitialCooldownSeconds;
        public int InterstitialMaxImpressionsPerSession => interstitialMaxImpressionsPerSession;
    }
}
