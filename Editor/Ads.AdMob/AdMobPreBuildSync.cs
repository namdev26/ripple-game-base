using System;
using RippleGame.Ads.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RippleGame.Ads.AdMob.Editor
{
    /// <summary>
    /// Copies the active profile's Android app id into the Google Mobile Ads settings before an Android build.
    /// Runs after <c>AdsPreBuildValidator</c>, so the ids are already validated.
    /// </summary>
    internal sealed class AdMobPreBuildSync : IPreprocessBuildWithReport
    {
        private const string ProviderId = "admob";

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            var configuration = AdsConfigurationLocator.Find();
            if (configuration == null || !string.Equals(configuration.ProviderId, ProviderId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var defines = AdsBuildProfileUtility.Parse(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android));
            var environment = AdsBuildProfileUtility.ResolveEnvironment(defines);
            if (environment == AdsEnvironment.Disabled)
            {
                return;
            }

            var profile = environment == AdsEnvironment.Test ? configuration.Test : configuration.Production;
            AdMobSettingsSynchronizer.SynchronizeAndroidAppId(profile.AndroidAppId);
        }
    }
}
