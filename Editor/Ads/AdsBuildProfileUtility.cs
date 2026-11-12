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
        internal const string DisabledSymbol = "ADS_DISABLED";
        internal const string TestSymbol = "ADS_TEST";
        private const string PendingBuildProfileKey = "RippleGameAds.PendingAndroidBuildProfile";

        [InitializeOnLoadMethod]
        private static void ResumeRequestedBuildAfterCompilation()
        {
            EditorApplication.delayCall += TryRunPendingBuild;
        }

        private static void BuildNoAds() => Build(AndroidAdsBuildProfile.NoAds);

        private static void BuildAdsTest() => Build(AndroidAdsBuildProfile.AdsTest);

        private static void BuildAdsProduction() => Build(AndroidAdsBuildProfile.AdsProduction);

        private static void ConfigureNoAds() => ApplyProfile(AndroidAdsBuildProfile.NoAds);

        private static void ConfigureAdsTest() => ApplyProfile(AndroidAdsBuildProfile.AdsTest);

        private static void ConfigureAdsProduction() => ApplyProfile(AndroidAdsBuildProfile.AdsProduction);

        internal static void ApplyProfile(AndroidAdsBuildProfile profile)
        {
            var current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, ApplyProfile(current, profile));
            Debug.Log($"[RippleGame Ads] Android symbols configured for {profile}.");
        }

        /// <summary>
        /// Returns <paramref name="defineSymbols"/> with the profile applied. The legacy <c>ADS</c> symbol is always
        /// removed; NoAds adds <c>ADS_DISABLED</c>, AdsTest adds <c>ADS_TEST</c> and AdsProduction adds neither.
        /// </summary>
        internal static string ApplyProfile(string defineSymbols, AndroidAdsBuildProfile profile)
        {
            var symbols = Parse(defineSymbols ?? string.Empty);
            symbols.Remove("ADS");
            symbols.Remove(DisabledSymbol);
            symbols.Remove(TestSymbol);

            if (profile == AndroidAdsBuildProfile.NoAds)
            {
                symbols.Add(DisabledSymbol);
            }
            else if (profile == AndroidAdsBuildProfile.AdsTest)
            {
                symbols.Add(TestSymbol);
            }

            return string.Join(";", symbols.OrderBy(symbol => symbol, StringComparer.Ordinal));
        }

        /// <summary>Mirrors <see cref="AdsConfiguration.ResolveCompileTimeEnvironment"/> for an arbitrary symbol set.</summary>
        internal static AdsEnvironment ResolveEnvironment(ISet<string> symbols)
        {
            if (symbols.Contains(DisabledSymbol)) return AdsEnvironment.Disabled;
            return symbols.Contains(TestSymbol) ? AdsEnvironment.Test : AdsEnvironment.Production;
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
