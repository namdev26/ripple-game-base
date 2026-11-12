using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[Serializable]
internal sealed class AdsHubPage : ToolHubPage
{
    private const string ProfileCommands = "RippleGame.Ads.Editor.AdsBuildProfileUtility";

    [SerializeField] private ToolHubAssetInspector configuration = new ToolHubAssetInspector();
    [SerializeField] private Vector2 scroll;
    [NonSerialized] private ScriptableObject providerAsset;
    [NonSerialized] private double lastProviderSearch;

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            if (!ToolHubCommands.HasType("RippleGame.Ads.AdsConfiguration"))
            {
                EditorGUILayout.HelpBox("Install com.ripplegame.ads to configure ads in this project.", MessageType.Info);
                return;
            }
            DrawProvider();
            EditorGUILayout.LabelField("Android build profiles", EditorStyles.boldLabel);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';');
            var profile = Array.IndexOf(symbols, "ADS_DISABLED") >= 0 ? "No Ads" :
                Array.IndexOf(symbols, "ADS_TEST") >= 0 ? "Ads Test" : "Ads Production";
            EditorGUILayout.LabelField("Current Android profile", profile);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
            {
                DrawProfile("No Ads");
                DrawProfile("Ads Test");
                DrawProfile("Ads Production");
                GUILayout.Space(8);
                if (GUILayout.Button("Add Ads Object to Active Scene", GUILayout.Height(28)))
                    ToolHubCommands.Run("RippleGame.Ads.Editor.AdsSceneInstaller", "AddAdsObjectToActiveScene");
            }
            GUILayout.Space(18);
            EditorGUILayout.LabelField("Ad configuration", EditorStyles.boldLabel);
            configuration.Draw("RippleGame.Ads.AdsConfiguration", "RippleGame.Ads.Editor.AdsConfigurationAssetMenu", "Create");
        }
    }

    private void DrawProvider()
    {
        var providerId = ReadProviderId();
        EditorGUILayout.LabelField("Ads provider", providerId);
        if (string.IsNullOrEmpty(providerId) || providerId == "none") return;

        var factoryType = providerId == "admob" ? "RippleGame.Ads.AdMob.AdMobProviderFactory" :
            providerId == "max" ? "RippleGame.Ads.Max.MaxProviderFactory" : null;
        if (factoryType == null) return;
        var compiled = ToolHubCommands.HasType(factoryType);
        EditorGUILayout.LabelField("Provider SDK", compiled ? "Compiled" : "Not installed at the pinned version");
        if (!compiled)
            EditorGUILayout.HelpBox("Open the SDK page and press Setup or Fix to install the " + providerId + " SDK.", MessageType.Warning);
    }

    private string ReadProviderId()
    {
        if (providerAsset == null && EditorApplication.timeSinceStartup - lastProviderSearch > 1.0)
        {
            lastProviderSearch = EditorApplication.timeSinceStartup;
            foreach (var guid in AssetDatabase.FindAssets("t:AdsConfiguration"))
            {
                providerAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (providerAsset != null && providerAsset.GetType().FullName == "RippleGame.Ads.AdsConfiguration") break;
                providerAsset = null;
            }
        }

        if (providerAsset == null) return "none (no configuration asset)";
        using (var serialized = new SerializedObject(providerAsset))
        {
            var property = serialized.FindProperty("providerId");
            return property == null ? "none" : property.stringValue;
        }
    }

    private static void DrawProfile(string name)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(name, GUILayout.MinWidth(100));
            if (GUILayout.Button(new GUIContent("Configure", "Change the Android profile."), GUILayout.Width(100)))
                ToolHubCommands.Run(ProfileCommands, "Configure" + name.Replace(" ", string.Empty));
            if (GUILayout.Button(new GUIContent("Build", "Choose an output path and start a build."), GUILayout.Width(80)))
                ToolHubCommands.Run(ProfileCommands, "Build" + name.Replace(" ", string.Empty));
        }
    }

    public override void Dispose() => configuration.Dispose();
}
