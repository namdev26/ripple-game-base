using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class AnalyticsHubPage : ToolHubPage
{
    private const string FirebaseServiceType = "RippleGame.Analytics.FirebaseAnalyticsService, RippleGame.Analytics.Firebase";

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
            var firebase = Type.GetType(FirebaseServiceType, false) != null;
            EditorGUILayout.LabelField("Backend", firebase ? "Firebase" : "Console only");
            if (!firebase)
                EditorGUILayout.HelpBox("Firebase Analytics is not installed. Events print to the Console. Install it from the SDK page.", MessageType.Info);
            GUILayout.Space(18);
            EditorGUILayout.LabelField("Analytics settings", EditorStyles.boldLabel);
            configuration.Draw("RippleGame.Analytics.AnalyticsSettings", "RippleGame.Analytics.Editor.AnalyticsMenu", "CreateSettings");
        }
    }

    public override void Dispose() => configuration.Dispose();
}
