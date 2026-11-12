using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RippleGame.Sdk.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    internal sealed class AdsPreBuildValidator : IPreprocessBuildWithReport
    {
        private const string GoogleDemoPublisherId = "3940256099942544";

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            var defines = AdsBuildProfileUtility.Parse(PlayerSettings.GetScriptingDefineSymbols(target));
            var environment = AdsBuildProfileUtility.ResolveEnvironment(defines);
            var configuration = AdsConfigurationLocator.Find();

            var error = GetBuildError(
                configuration,
                environment,
                report.summary.platform,
                ProviderBuildCheck.IsCompiled,
                SdkStatusOf);
            if (error != null)
            {
                throw new BuildFailedException(error);
            }

            if (environment == AdsEnvironment.Disabled || configuration == null || IsNone(configuration.ProviderId))
            {
                return;
            }

            var runtimeReference = AssetDatabase.LoadAssetAtPath<AdsConfigurationReference>("Assets/Resources/RippleGameAds/AdsConfigurationReference.asset");
            if (runtimeReference == null || runtimeReference.Configuration != configuration)
            {
                throw new BuildFailedException("The AdsConfiguration runtime reference is missing or out of date. Use Create Configuration in RIPPLE Game > Open Hub > ADS again.");
            }
        }

        /// <summary>
        /// Returns null when the build can proceed, or the first error. A NoAds build, a project without an
        /// AdsConfiguration and the "none" provider all skip every provider check.
        /// </summary>
        internal static string GetBuildError(
            AdsConfiguration configuration,
            AdsEnvironment environment,
            BuildTarget target,
            Func<string, bool> providerCompiled,
            Func<string, SdkStatusInfo> sdkStatus)
        {
            if (environment == AdsEnvironment.Disabled || configuration == null)
            {
                return null;
            }

            var providerId = configuration.ProviderId;
            var providerError = ProviderBuildCheck.Validate(providerId, providerCompiled, sdkStatus, target);
            if (providerError != null || IsNone(providerId))
            {
                return providerError;
            }

            if (string.Equals(providerId, "admob", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateAdMob(configuration, environment);
            }

            if (string.Equals(providerId, "max", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateMax(configuration, environment, target);
            }

            return null;
        }

        internal static string ValidateAdMob(AdsConfiguration configuration, AdsEnvironment environment)
        {
            var testing = environment == AdsEnvironment.Test;
            var profileName = testing ? "Test" : "Production";
            var error = ValidateAdMobProfile(testing ? configuration.Test : configuration.Production, profileName);
            if (error != null)
            {
                return error;
            }

            if (testing)
            {
                var production = new HashSet<string>(AllIds(configuration.Production).Where(id => !string.IsNullOrWhiteSpace(id)));
                return AllIds(configuration.Test).Any(id => !string.IsNullOrWhiteSpace(id) && production.Contains(id))
                    ? "Test AdsConfiguration reuses an ID from Production. Use Google demo/test IDs instead."
                    : null;
            }

            return AllIds(configuration.Production).Any(id => id.Contains(GoogleDemoPublisherId, StringComparison.Ordinal))
                ? "Production AdsConfiguration contains a Google demo ID."
                : null;
        }

        internal static string ValidateAdMobProfile(AdMobAdUnitConfiguration profile, string profileName)
        {
            if (profile == null)
            {
                return $"{profileName} profile is missing.";
            }

            var required = new List<(string Name, string Value)> { ("Android App ID", profile.AndroidAppId) };
            if (profile.EnableBanner) required.Add(("Banner unit ID", profile.BannerUnitId));
            if (profile.EnableInterstitial) required.Add(("Interstitial unit ID", profile.InterstitialUnitId));
            if (profile.EnableRewarded) required.Add(("Rewarded unit ID", profile.RewardedUnitId));
            if (profile.EnableAppOpen) required.Add(("App Open unit ID", profile.AppOpenUnitId));

            var missing = required.Where(item => string.IsNullOrWhiteSpace(item.Value)).Select(item => item.Name).ToArray();
            return missing.Length > 0
                ? $"{profileName} AdsConfiguration is missing: {string.Join(", ", missing)}."
                : null;
        }

        /// <summary>The Production MAX profile must have an SDK key and a unit ID for every enabled format on the target platform.</summary>
        internal static string ValidateMax(AdsConfiguration configuration, AdsEnvironment environment, BuildTarget target)
        {
            if (environment != AdsEnvironment.Production)
            {
                return null;
            }

            var profile = configuration.MaxProduction;
            if (profile == null)
            {
                return "Production profile is missing.";
            }

            var ids = target == BuildTarget.iOS ? profile.Ios : profile.Android;
            ids = ids ?? new MaxPlatformUnitIds();
            var required = new List<(string Name, string Value)> { ("MAX SDK key", profile.SdkKey) };
            if (profile.EnableBanner) required.Add(("Banner unit ID", ids.Banner));
            if (profile.EnableInterstitial) required.Add(("Interstitial unit ID", ids.Interstitial));
            if (profile.EnableRewarded) required.Add(("Rewarded unit ID", ids.Rewarded));
            if (profile.EnableAppOpen) required.Add(("App Open unit ID", ids.AppOpen));
            if (profile.EnableMrec) required.Add(("MREC unit ID", ids.Mrec));

            var missing = required.Where(item => string.IsNullOrWhiteSpace(item.Value)).Select(item => item.Name).ToArray();
            return missing.Length > 0
                ? $"Production AdsConfiguration (MAX) is missing: {string.Join(", ", missing)}."
                : null;
        }

        private static bool IsNone(string providerId)
        {
            return string.IsNullOrWhiteSpace(providerId) || string.Equals(providerId, "none", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> AllIds(AdMobAdUnitConfiguration profile)
        {
            yield return profile.AndroidAppId ?? string.Empty;
            yield return profile.BannerUnitId ?? string.Empty;
            yield return profile.InterstitialUnitId ?? string.Empty;
            yield return profile.RewardedUnitId ?? string.Empty;
            yield return profile.AppOpenUnitId ?? string.Empty;
        }

        private static SdkStatusInfo SdkStatusOf(string sdkId)
        {
            try
            {
                var entry = SdkCatalog.LoadDefault().Find(sdkId);
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
                return entry == null || !File.Exists(manifestPath)
                    ? null
                    : SdkStatusResolver.Resolve(entry, File.ReadAllText(manifestPath), projectRoot);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
