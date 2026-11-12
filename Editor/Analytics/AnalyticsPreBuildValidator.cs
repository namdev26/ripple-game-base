using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RippleGame.Analytics.Editor
{
    /// <summary>Stops a build that has Firebase Analytics installed but is missing the Firebase config file for its platform.</summary>
    internal sealed class AnalyticsPreBuildValidator : IPreprocessBuildWithReport
    {
        internal const string AndroidConfigFile = "google-services.json";
        internal const string IosConfigFile = "GoogleService-Info.plist";
        internal const string FirebaseServiceType = "RippleGame.Analytics.FirebaseAnalyticsService, RippleGame.Analytics.Firebase";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var error = GetMissingConfigError(report.summary.platform, IsFirebaseInstalled(), ConfigExists);
            if (error != null) throw new BuildFailedException(error);
        }

        internal static bool IsFirebaseInstalled() => Type.GetType(FirebaseServiceType, false) != null;

        internal static string GetMissingConfigError(BuildTarget target, bool firebaseInstalled, Func<string, bool> configExists)
        {
            if (!firebaseInstalled) return null;

            var fileName = target == BuildTarget.Android ? AndroidConfigFile : target == BuildTarget.iOS ? IosConfigFile : null;
            if (fileName == null || configExists(fileName)) return null;

            return $"Firebase Analytics is installed but {fileName} was not found under Assets/. Download it from the Firebase console, " +
                   "or remove Firebase Analytics in RIPPLE Game > Open Hub > SDK.";
        }

        private static bool ConfigExists(string fileName) =>
            Directory.GetFiles("Assets", fileName, SearchOption.AllDirectories).Length > 0;
    }
}
