using System;
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
                ? "Current symbols resolve to Disabled (ADS_DISABLED). Use the Ads Test or Ads Production profile in RIPPLE Game > Open Hub > ADS to turn ads on."
                : $"Current symbols resolve to {environment}. This profile is selected at compile time.";
            EditorGUILayout.HelpBox(message, environment == AdsEnvironment.Disabled ? MessageType.Warning : MessageType.Info);

            var providerId = ((AdsConfiguration)target).ProviderId;
            var factoryType = ProviderBuildCheck.FactoryTypeName(providerId);
            if (factoryType != null && !ProviderBuildCheck.IsCompiled(factoryType))
            {
                EditorGUILayout.HelpBox(
                    $"Ads provider '{providerId}' is selected but its SDK is not installed at the pinned version. Open RIPPLE Game > Open Hub > SDK and press Setup or Fix.",
                    MessageType.Warning);
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("providerId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogging"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bannerExcludedSceneNames"));
            serializedObject.ApplyModifiedProperties();

            // Only the selected provider's IDs are drawn, so a LevelPlay or MAX game never sees AdMob IDs.
            providerId = ((AdsConfiguration)target).ProviderId;
            if (IsProvider(providerId, "none") || string.IsNullOrWhiteSpace(providerId))
            {
                EditorGUILayout.HelpBox("Provider is none: no ads are shown and no IDs are needed.", MessageType.Info);
                return;
            }

            if (IsProvider(providerId, "admob"))
            {
                DrawProperties("test", "production");
            }
            else if (IsProvider(providerId, "max"))
            {
                DrawProperties("maxTest", "maxProduction");
            }
            else
            {
                DrawProviderSettings(providerId);
                DrawProfileWithoutAdMobIds("test");
                DrawProfileWithoutAdMobIds("production");
            }

            DrawProperties("policy");
        }

        private static bool IsProvider(string providerId, string id) => string.Equals(providerId, id, StringComparison.OrdinalIgnoreCase);

        private void DrawProperties(params string[] names)
        {
            serializedObject.Update();
            foreach (var name in names) EditorGUILayout.PropertyField(serializedObject.FindProperty(name), true);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProviderSettings(string providerId)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField(providerId + " IDs", EditorStyles.boldLabel);
            DrawProperties("providerSettings");
            var settings = serializedObject.FindProperty("providerSettings").objectReferenceValue;
            if (settings == null)
            {
                EditorGUILayout.HelpBox($"Assign the asset that holds the {providerId} app key and ad unit IDs.", MessageType.Warning);
                return;
            }

            CreateCachedEditor(settings, null, ref providerEditor);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.IndentLevelScope()) providerEditor.OnInspectorGUI();
            // The Hub's Save button saves only this configuration, so save the provider IDs as they are typed.
            if (EditorGUI.EndChangeCheck()) AssetDatabase.SaveAssetIfDirty(settings);
            GUILayout.Space(8);
        }

        // A game-registered provider still reads format toggles, banner position and retries from the AdMob profiles.
        private void DrawProfileWithoutAdMobIds(string name)
        {
            serializedObject.Update();
            var profile = serializedObject.FindProperty(name);
            profile.isExpanded = EditorGUILayout.Foldout(profile.isExpanded, profile.displayName + " (formats and loading)", true);
            if (profile.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var end = profile.GetEndProperty();
                    var child = profile.Copy();
                    for (var enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false)
                        if (!AdMobIdFields.Contains(child.name)) EditorGUILayout.PropertyField(child, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
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
