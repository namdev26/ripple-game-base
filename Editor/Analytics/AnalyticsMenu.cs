using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RippleGame.Analytics.Editor
{
    internal static class AnalyticsMenu
    {
        private const string SettingsDirectory = "Assets/Resources/RippleGameAnalytics";
        private const string SettingsPath = SettingsDirectory + "/AnalyticsSettings.asset";
        private static readonly NamedBuildTarget[] FirebaseTargets = { NamedBuildTarget.Android, NamedBuildTarget.iOS };

        [MenuItem("TOOL/ANALYTICS/Enable Firebase")]
        private static void EnableFirebase()
        {
            if (!FirebaseDefineUtility.IsFirebaseInstalled())
            {
                throw new InvalidOperationException("Import FirebaseAnalytics.unitypackage before enabling FIREBASE. No symbols were changed.");
            }

            foreach (var target in FirebaseTargets)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, FirebaseDefineUtility.Add(PlayerSettings.GetScriptingDefineSymbols(target)));
            }

            Debug.Log(Analytics.LogPrefix + "FIREBASE enabled for Android and iOS.");
        }

        [MenuItem("TOOL/ANALYTICS/Disable Firebase")]
        private static void DisableFirebase()
        {
            foreach (var target in FirebaseTargets)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, FirebaseDefineUtility.Remove(PlayerSettings.GetScriptingDefineSymbols(target)));
            }

            Debug.Log(Analytics.LogPrefix + "FIREBASE disabled for Android and iOS. Events now print to the Console only.");
        }

        [MenuItem("TOOL/ANALYTICS/Create Analytics Settings")]
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
