using System;
using UnityEngine;

namespace RippleGame.Ads
{
    [CreateAssetMenu(fileName = "AdsConfiguration", menuName = "RIPPLE Game/ADS/Ads Configuration")]
    public sealed class AdsConfiguration : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private string providerId = "none";
        [SerializeField] private bool enableLogging = true;
        [Tooltip("Banner is shown in all build scenes except these scene names.")]
        [SerializeField] private string[] bannerExcludedSceneNames = { "Loading", "LoadingScene" };
        [SerializeField] private AdMobAdUnitConfiguration test = AdMobAdUnitConfiguration.CreateAndroidTest();
        [SerializeField] private AdMobAdUnitConfiguration production = new AdMobAdUnitConfiguration();
        [SerializeField] private AdsPolicySettings policy = new AdsPolicySettings();
        [SerializeField] private MaxAdUnitConfiguration maxTest = new MaxAdUnitConfiguration();
        [SerializeField] private MaxAdUnitConfiguration maxProduction = new MaxAdUnitConfiguration();
        [HideInInspector] [SerializeField] private bool policyMigrated;

        public string ProviderId => providerId;
        public bool EnableLogging => enableLogging;
        public AdMobAdUnitConfiguration Test => test;
        public AdMobAdUnitConfiguration Production => production;
        public AdsPolicySettings Policy => policy;
        public MaxAdUnitConfiguration MaxTest => maxTest;
        public MaxAdUnitConfiguration MaxProduction => maxProduction;

        public static AdsEnvironment ResolveCompileTimeEnvironment()
        {
#if ADS_DISABLED
            return AdsEnvironment.Disabled;
#elif ADS_TEST
            return AdsEnvironment.Test;
#else
            return AdsEnvironment.Production;
#endif
        }

        public AdMobAdUnitConfiguration GetActiveProfile()
        {
            return ResolveCompileTimeEnvironment() == AdsEnvironment.Test ? test : production;
        }

        public MaxAdUnitConfiguration GetActiveMaxProfile()
        {
            return ResolveCompileTimeEnvironment() == AdsEnvironment.Test ? maxTest : maxProduction;
        }

        /// <summary>
        /// Banner position from the selected provider's active profile: MAX uses its own profile, "none" has no
        /// banner (null) and every other provider uses the AdMob profile.
        /// </summary>
        public BannerPosition? GetActiveBannerPosition()
        {
            if (string.IsNullOrWhiteSpace(providerId) || string.Equals(providerId, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return string.Equals(providerId, "max", StringComparison.OrdinalIgnoreCase)
                ? GetActiveMaxProfile().BannerPosition
                : GetActiveProfile().BannerPosition;
        }

        private void OnEnable()
        {
            // A freshly created asset has nothing to migrate. Loaded assets already went through
            // OnAfterDeserialize, so this only flips the flag for new instances.
            policyMigrated = true;
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (policyMigrated)
            {
                return;
            }

            // 0.3.1 kept the presentation cooldowns on the Production AdMob profile.
            if (policy == null) policy = new AdsPolicySettings();
            if (production != null)
            {
                policy.Interstitial.CooldownSeconds = production.InterstitialCooldownSeconds;
                policy.Interstitial.MaxPerSession = production.InterstitialMaxImpressionsPerSession;
                policy.AppOpen.CooldownSeconds = production.AppOpenCooldownSeconds;
                policy.AppOpen.MinBackgroundSeconds = production.AppOpenMinimumBackgroundSeconds;
            }

            policyMigrated = true;
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

        [Min(0f)] [HideInInspector] [SerializeField] private float appOpenCooldownSeconds = 20f;
        [Min(0f)] [HideInInspector] [SerializeField] private float appOpenMinimumBackgroundSeconds = 5f;
        [Header("Loading")]
        [Min(0)] [SerializeField] private int retryMaxAttempts = 3;
        [Min(0.1f)] [SerializeField] private float retryInitialDelaySeconds = 2f;
        [Min(0.1f)] [SerializeField] private float retryMaxDelaySeconds = 30f;

        // Legacy 0.3.1 presentation fields, superseded by AdsConfiguration.Policy. Kept serialized so
        // existing assets still deserialize and migrate.
        [Min(0f)] [HideInInspector] [SerializeField] private float interstitialCooldownSeconds = 45f;
        [Min(0)] [HideInInspector] [SerializeField] private int interstitialMaxImpressionsPerSession = 4;

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
