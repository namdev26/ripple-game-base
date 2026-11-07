using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    internal enum AndroidAdsBuildProfile
    {
        NoAds,
        AdsTest,
        AdsProduction
    }

    internal static class AdsBuildProfileUtility
    {
        private const string PendingBuildProfileKey = "RippleGameAds.PendingAndroidBuildProfile";

        [InitializeOnLoadMethod]
        private static void ResumeRequestedBuildAfterCompilation()
        {
            EditorApplication.delayCall += TryRunPendingBuild;
        }

        [MenuItem("RIPPLE Game/ADS/Build Android/No Ads")]
        private static void BuildNoAds() => Build(AndroidAdsBuildProfile.NoAds);

        [MenuItem("RIPPLE Game/ADS/Build Android/Ads Test")]
        private static void BuildAdsTest() => Build(AndroidAdsBuildProfile.AdsTest);

        [MenuItem("RIPPLE Game/ADS/Build Android/Ads Production")]
        private static void BuildAdsProduction() => Build(AndroidAdsBuildProfile.AdsProduction);

        [MenuItem("RIPPLE Game/ADS/Configure Android/No Ads")]
        private static void ConfigureNoAds() => ApplyProfile(AndroidAdsBuildProfile.NoAds);

        [MenuItem("RIPPLE Game/ADS/Configure Android/Ads Test")]
        private static void ConfigureAdsTest() => ApplyProfile(AndroidAdsBuildProfile.AdsTest);

        [MenuItem("RIPPLE Game/ADS/Configure Android/Ads Production")]
        private static void ConfigureAdsProduction() => ApplyProfile(AndroidAdsBuildProfile.AdsProduction);

        internal static void ApplyProfile(AndroidAdsBuildProfile profile)
        {
            if (profile != AndroidAdsBuildProfile.NoAds && !AdMobSettingsSynchronizer.IsGoogleMobileAdsInstalled())
            {
                throw new InvalidOperationException(
                    "Install the official Google Mobile Ads Unity plugin before enabling ADS. No symbols were changed.");
            }

            var symbols = Parse(PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android));
            symbols.Remove("ADS");
            symbols.Remove("ADS_TEST");

            if (profile != AndroidAdsBuildProfile.NoAds)
            {
                symbols.Add("ADS");
            }

            if (profile == AndroidAdsBuildProfile.AdsTest)
            {
                symbols.Add("ADS_TEST");
            }

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Join(";", symbols.OrderBy(symbol => symbol, StringComparer.Ordinal)));
            Debug.Log($"[RippleGame Ads] Android symbols configured for {profile}.");
        }

        private static void Build(AndroidAdsBuildProfile profile)
        {
            ApplyProfile(profile);
            SessionState.SetString(PendingBuildProfileKey, profile.ToString());
            EditorApplication.delayCall += TryRunPendingBuild;
        }

        private static void TryRunPendingBuild()
        {
            var pending = SessionState.GetString(PendingBuildProfileKey, string.Empty);
            if (string.IsNullOrWhiteSpace(pending))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRunPendingBuild;
                return;
            }

            SessionState.EraseString(PendingBuildProfileKey);
            BuildNow((AndroidAdsBuildProfile)Enum.Parse(typeof(AndroidAdsBuildProfile), pending));
        }

        private static void BuildNow(AndroidAdsBuildProfile profile)
        {
            var extension = EditorUserBuildSettings.buildAppBundle ? "aab" : "apk";
            var path = EditorUtility.SaveFilePanel("Build Android", "Builds", Application.productName, extension);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes exist in Build Settings.");
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            }
        }

        internal static HashSet<string> Parse(string defineSymbols)
        {
            return new HashSet<string>(defineSymbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        }
    }
}
