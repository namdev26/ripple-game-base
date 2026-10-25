using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RippleGame.Analytics.Editor
{
    /// <summary>Stops a FIREBASE build that is missing the Firebase config file for its platform.</summary>
    internal sealed class AnalyticsPreBuildValidator : IPreprocessBuildWithReport
    {
        internal const string AndroidConfigFile = "google-services.json";
        internal const string IosConfigFile = "GoogleService-Info.plist";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target)));
            var error = GetMissingConfigError(target, symbols, ConfigExists);
            if (error != null) throw new BuildFailedException(error);
        }

        internal static string GetMissingConfigError(BuildTarget target, string defineSymbols, Func<string, bool> configExists)
        {
            if (!FirebaseDefineUtility.Contains(defineSymbols)) return null;

            var fileName = target == BuildTarget.Android ? AndroidConfigFile : target == BuildTarget.iOS ? IosConfigFile : null;
            if (fileName == null || configExists(fileName)) return null;

            return $"FIREBASE is enabled but {fileName} was not found under Assets/. Download it from the Firebase console, " +
                   "or use TOOL/ANALYTICS/Disable Firebase.";
        }

        private static bool ConfigExists(string fileName) =>
            Directory.GetFiles("Assets", fileName, SearchOption.AllDirectories).Length > 0;
    }
}
