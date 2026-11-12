using System.IO;
using UnityEditor;
using UnityEngine;

namespace RippleGame.RemoteConfig.Editor
{
    internal static class RemoteConfigMenu
    {
        private const string SettingsDirectory = "Assets/Resources/RippleGameRemoteConfig";
        private const string SettingsPath = SettingsDirectory + "/RemoteConfigSettings.asset";

        private static void CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<RemoteConfigSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(SettingsDirectory);
                settings = ScriptableObject.CreateInstance<RemoteConfigSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
