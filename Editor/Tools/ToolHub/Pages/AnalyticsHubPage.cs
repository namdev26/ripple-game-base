using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[Serializable]
internal sealed class AnalyticsHubPage : ToolHubPage
{
    [SerializeField] private ToolHubAssetInspector configuration = new ToolHubAssetInspector();
    [SerializeField] private Vector2 scroll;

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            if (!ToolHubCommands.HasType("RippleGame.Analytics.AnalyticsSettings"))
            {
                EditorGUILayout.HelpBox("Install com.ripplegame.analytics to configure analytics in this project.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Firebase integration", EditorStyles.boldLabel);
            DrawTarget("Android", NamedBuildTarget.Android);
            DrawTarget("iOS", NamedBuildTarget.iOS);
            var installed = ToolHubCommands.HasType("Firebase.Analytics.FirebaseAnalytics");
            if (!installed)
                EditorGUILayout.HelpBox("Firebase SDK is not installed. Events print to the Console.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!installed))
                    if (GUILayout.Button("Enable Firebase", GUILayout.Height(28)))
                        ToolHubCommands.Run("RIPPLE Game/ANALYTICS/Enable Firebase");
                if (GUILayout.Button("Disable Firebase", GUILayout.Height(28)))
                    ToolHubCommands.Run("RIPPLE Game/ANALYTICS/Disable Firebase");
            }
            GUILayout.Space(18);
            EditorGUILayout.LabelField("Analytics settings", EditorStyles.boldLabel);
            configuration.Draw("RippleGame.Analytics.AnalyticsSettings", "RIPPLE Game/ANALYTICS/Create Analytics Settings");
        }
    }

    private static void DrawTarget(string label, NamedBuildTarget target)
    {
        var symbols = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
        EditorGUILayout.LabelField(label, Array.IndexOf(symbols, "FIREBASE") >= 0 ? "Firebase enabled" : "Console only");
    }

    public override void Dispose() => configuration.Dispose();
}
