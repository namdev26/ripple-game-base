using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RippleGame.Ads.Editor
{
    [CustomEditor(typeof(AdsConfiguration))]
    internal sealed class AdsConfigurationEditor : UnityEditor.Editor
    {
        private static readonly HashSet<string> AdMobIdFields = new HashSet<string>
            { "androidAppId", "bannerUnitId", "interstitialUnitId", "rewardedUnitId", "appOpenUnitId" };

        private UnityEditor.Editor providerEditor;

        public override void OnInspectorGUI()
        {
            var environment = AdsConfiguration.ResolveCompileTimeEnvironment();
            var message = environment == AdsEnvironment.Disabled
                ? "Current symbols resolve to Disabled. Add ADS for production or ADS;ADS_TEST for test ads."
                : $"Current symbols resolve to {environment}. This profile is selected at compile time.";
            EditorGUILayout.HelpBox(message, environment == AdsEnvironment.Disabled ? MessageType.Warning : MessageType.Info);

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("providerId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogging"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bannerExcludedSceneNames"));
            serializedObject.ApplyModifiedProperties();

            var configuration = (AdsConfiguration)target;
            if (!configuration.IsAdMob) DrawProviderSettings(configuration.ProviderId);

            serializedObject.Update();
            DrawProfile("test", configuration.IsAdMob);
            DrawProfile("production", configuration.IsAdMob);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProviderSettings(string providerId)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField(providerId + " IDs", EditorStyles.boldLabel);
            var settings = serializedObject.FindProperty("providerSettings");
            EditorGUILayout.PropertyField(settings, new GUIContent("Provider Settings"));
            serializedObject.ApplyModifiedProperties();
            if (settings.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox($"Assign the asset that holds the {providerId} app key and ad unit IDs.", MessageType.Warning);
                return;
            }

            CreateCachedEditor(settings.objectReferenceValue, null, ref providerEditor);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.IndentLevelScope()) providerEditor.OnInspectorGUI();
            // The hub's Save button saves only this asset, so save the provider IDs as they are typed.
            if (EditorGUI.EndChangeCheck()) AssetDatabase.SaveAssetIfDirty(settings.objectReferenceValue);
            GUILayout.Space(8);
        }

        // AdMob profiles also hold the format toggles, cooldowns and retries every provider uses; other providers hide only the AdMob IDs.
        private void DrawProfile(string name, bool isAdMob)
        {
            var profile = serializedObject.FindProperty(name);
            if (isAdMob)
            {
                EditorGUILayout.PropertyField(profile, true);
                return;
            }

            profile.isExpanded = EditorGUILayout.Foldout(profile.isExpanded, profile.displayName + " (formats and timing)", true);
            if (!profile.isExpanded) return;
            using (new EditorGUI.IndentLevelScope())
            {
                var end = profile.GetEndProperty();
                var child = profile.Copy();
                for (var enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
                    if (!AdMobIdFields.Contains(child.name)) EditorGUILayout.PropertyField(child, true);
            }
        }

        private void OnDisable()
        {
            if (providerEditor != null) DestroyImmediate(providerEditor);
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
