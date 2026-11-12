using System.IO;
using UnityEditor;
using UnityEngine;

namespace RippleGame.Analytics.Editor
{
    internal static class AnalyticsMenu
    {
        private const string SettingsDirectory = "Assets/Resources/RippleGameAnalytics";
        private const string SettingsPath = SettingsDirectory + "/AnalyticsSettings.asset";

        private static void CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AnalyticsSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(SettingsDirectory);
                settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
