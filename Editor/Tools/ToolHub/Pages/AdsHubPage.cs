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
            EditorGUILayout.LabelField("Android build profiles", EditorStyles.boldLabel);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';');
            var profile = Array.IndexOf(symbols, "ADS") < 0 ? "No Ads" :
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

    private static void DrawProfile(string name)
    {
        var available = name == "No Ads" || ToolHubCommands.HasType("GoogleMobileAds.Api.MobileAds");
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(name, GUILayout.MinWidth(100));
            using (new EditorGUI.DisabledScope(!available))
            {
                if (GUILayout.Button(new GUIContent("Configure", "Change the Android profile."), GUILayout.Width(100)))
                    ToolHubCommands.Run(ProfileCommands, "Configure" + name.Replace(" ", string.Empty));
                if (GUILayout.Button(new GUIContent("Build", "Choose an output path and start a build."), GUILayout.Width(80)))
                    ToolHubCommands.Run(ProfileCommands, "Build" + name.Replace(" ", string.Empty));
            }
        }
        if (!available) EditorGUILayout.LabelField("Google Mobile Ads SDK is required for " + name + ".", EditorStyles.miniLabel);
    }

    public override void Dispose() => configuration.Dispose();
}
