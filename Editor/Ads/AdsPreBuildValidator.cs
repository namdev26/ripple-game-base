using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RippleGame.Ads.Editor
{
    internal sealed class AdsPreBuildValidator : IPreprocessBuildWithReport
    {
        private const string GoogleDemoPublisherId = "3940256099942544";

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            var defines = AdsBuildProfileUtility.Parse(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android));
            var adsEnabled = defines.Contains("ADS");
            var testEnabled = defines.Contains("ADS_TEST");

            if (testEnabled && !adsEnabled)
            {
                throw new BuildFailedException("ADS_TEST requires ADS.");
            }

            if (!adsEnabled)
            {
                return;
            }

            if (report.summary.platform != BuildTarget.Android)
            {
                throw new BuildFailedException("RippleGame Ads currently supports ADS builds only for Android.");
            }

            if (!AdMobSettingsSynchronizer.IsGoogleMobileAdsInstalled())
            {
                throw new BuildFailedException("ADS is enabled but the official Google Mobile Ads Unity plugin is not installed.");
            }

            var configuration = FindConfiguration();
            if (configuration == null)
            {
                throw new BuildFailedException("ADS is enabled but Assets/Settings/RippleGameAds/AdsConfiguration.asset is missing. Use RIPPLE Game/ADS/Create Ads Configuration.");
            }

            var runtimeReference = AssetDatabase.LoadAssetAtPath<AdsConfigurationReference>("Assets/Resources/RippleGameAds/AdsConfigurationReference.asset");
            if (runtimeReference == null || runtimeReference.Configuration != configuration)
            {
                throw new BuildFailedException("The AdsConfiguration runtime reference is missing or out of date. Run RIPPLE Game/ADS/Create Ads Configuration again.");
            }

            var active = testEnabled ? configuration.Test : configuration.Production;
            ValidateProfile(active, testEnabled ? "Test" : "Production");
            if (testEnabled)
            {
                ValidateTestProfile(configuration.Test, configuration.Production);
            }
            else
            {
                ValidateProductionProfile(configuration.Production);
            }

            AdMobSettingsSynchronizer.SynchronizeAndroidAppId(active.AndroidAppId);
        }

        internal static void ValidateProfile(AdMobAdUnitConfiguration profile, string profileName)
        {
            if (profile == null)
            {
                throw new BuildFailedException($"{profileName} profile is missing.");
            }

            var required = new List<(string Name, string Value)> { ("Android App ID", profile.AndroidAppId) };
            if (profile.EnableBanner) required.Add(("Banner unit ID", profile.BannerUnitId));
            if (profile.EnableInterstitial) required.Add(("Interstitial unit ID", profile.InterstitialUnitId));
            if (profile.EnableRewarded) required.Add(("Rewarded unit ID", profile.RewardedUnitId));
            if (profile.EnableAppOpen) required.Add(("App Open unit ID", profile.AppOpenUnitId));

            var missing = required.Where(item => string.IsNullOrWhiteSpace(item.Value)).Select(item => item.Name).ToArray();
            if (missing.Length > 0)
            {
                throw new BuildFailedException($"{profileName} AdsConfiguration is missing: {string.Join(", ", missing)}.");
            }
        }

        private static void ValidateProductionProfile(AdMobAdUnitConfiguration profile)
        {
            if (AllIds(profile).Any(id => id.Contains(GoogleDemoPublisherId, StringComparison.Ordinal)))
            {
                throw new BuildFailedException("Production AdsConfiguration contains a Google demo ID.");
            }
        }

        private static void ValidateTestProfile(AdMobAdUnitConfiguration test, AdMobAdUnitConfiguration production)
        {
            if (AllIds(test).Intersect(AllIds(production)).Any())
            {
                throw new BuildFailedException("Test AdsConfiguration reuses an ID from Production. Use Google demo/test IDs instead.");
            }
        }

        private static IEnumerable<string> AllIds(AdMobAdUnitConfiguration profile)
        {
            yield return profile.AndroidAppId;
            yield return profile.BannerUnitId;
            yield return profile.InterstitialUnitId;
            yield return profile.RewardedUnitId;
            yield return profile.AppOpenUnitId;
        }

        private static AdsConfiguration FindConfiguration()
        {
            const string canonicalPath = "Assets/Settings/RippleGameAds/AdsConfiguration.asset";
            var canonical = AssetDatabase.LoadAssetAtPath<AdsConfiguration>(canonicalPath);
            if (canonical != null)
            {
                return canonical;
            }

            var matches = AssetDatabase.FindAssets("t:AdsConfiguration")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AdsConfiguration>)
                .Where(config => config != null)
                .ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
