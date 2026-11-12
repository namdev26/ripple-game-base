using System.IO;
using UnityEditor;
using UnityEngine;

namespace RippleGame.Attribution.Editor
{
    internal static class AttributionMenu
    {
        private const string SettingsDirectory = "Assets/Resources/RippleGameAttribution";
        private const string SettingsPath = SettingsDirectory + "/AttributionSettings.asset";

        private static void CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AttributionSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(SettingsDirectory);
                settings = ScriptableObject.CreateInstance<AttributionSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
