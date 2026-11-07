using System.IO;
using UnityEditor;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    [CustomEditor(typeof(AdsConfiguration))]
    internal sealed class AdsConfigurationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var environment = AdsConfiguration.ResolveCompileTimeEnvironment();
            var message = environment == AdsEnvironment.Disabled
                ? "Current symbols resolve to Disabled. Add ADS for production or ADS;ADS_TEST for test ads."
                : $"Current symbols resolve to {environment}. This profile is selected at compile time.";
            EditorGUILayout.HelpBox(message, environment == AdsEnvironment.Disabled ? MessageType.Warning : MessageType.Info);
            DrawDefaultInspector();
        }
    }

    internal static class AdsConfigurationAssetMenu
    {
        private const string DirectoryPath = "Assets/Settings/RippleGameAds";
        private const string AssetPath = DirectoryPath + "/AdsConfiguration.asset";
        private const string ReferenceDirectoryPath = "Assets/Resources/RippleGameAds";
        private const string ReferenceAssetPath = ReferenceDirectoryPath + "/AdsConfigurationReference.asset";

        private static void Create()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AdsConfiguration>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                EnsureRuntimeReference(existing);
                AssetDatabase.SaveAssets();
                return;
            }

            Directory.CreateDirectory(DirectoryPath);
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            AssetDatabase.CreateAsset(configuration, AssetPath);
            EnsureRuntimeReference(configuration);
            AssetDatabase.SaveAssets();
            Selection.activeObject = configuration;
            EditorGUIUtility.PingObject(configuration);
        }

        private static void EnsureRuntimeReference(AdsConfiguration configuration)
        {
            Directory.CreateDirectory(ReferenceDirectoryPath);
            var reference = AssetDatabase.LoadAssetAtPath<AdsConfigurationReference>(ReferenceAssetPath);
            if (reference == null)
            {
                reference = ScriptableObject.CreateInstance<AdsConfigurationReference>();
                AssetDatabase.CreateAsset(reference, ReferenceAssetPath);
            }

            reference.SetConfiguration(configuration);
            EditorUtility.SetDirty(reference);
        }
    }
}
