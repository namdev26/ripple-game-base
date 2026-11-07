using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed partial class AndroidBuildPanel : ToolHubPage
{
    [NonSerialized] private string status;
    [SerializeField] private string version;
    [SerializeField] private bool buildAppBundle;
    [SerializeField] private bool initialized;
    [SerializeField] private Vector2 scroll;

    internal override void Initialize(Action repaint)
    {
        if (!initialized) ReadCurrentSettings();
        if (!panelsInitialized)
        {
            ReadPanels();
            RecommendedDefaults();
        }
    }

    internal override void Draw()
    {
        var previousLabelWidth = EditorGUIUtility.labelWidth;
        try
        {
            EditorGUIUtility.labelWidth = 230;
            DrawSettings();
        }
        finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Current Version", PlayerSettings.bundleVersion);
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUILayout.HorizontalScope())
        {
            version = EditorGUILayout.TextField(new GUIContent("Version", "Display version, for example 1.0.1."), version);
            var canIncrement = GameVersionSettings.TryIncrementVersion(version, out var nextVersion);
            using (new EditorGUI.DisabledScope(!canIncrement))
            {
                if (GUILayout.Button(new GUIContent("+1 Version", "1.0 -> 1.0.1 -> 1.0.2. Needs a numeric version."), GUILayout.Width(100))) version = nextVersion;
            }
        }
        buildAppBundle = EditorGUILayout.ToggleLeft("Build App Bundle (AAB)", buildAppBundle);

        DrawPanels();
        EditorGUILayout.EndScrollView();

        var valid = ValidateAll(out var error);
        if (!valid) EditorGUILayout.HelpBox(error, MessageType.Error);

        EditorGUILayout.LabelField("Changes are saved only when you press Apply.", EditorStyles.miniLabel);
        using (new EditorGUI.DisabledScope(!valid || EditorApplication.isCompiling ||
                                           EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer))
        {
            if (GUILayout.Button(new GUIContent("Apply", "Changes here are saved only when you press Apply."), GUILayout.Height(36))) Apply();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Recommended Defaults", "Keeps your names, icons, Color Space, Input Handling, signing and Gradle templates."))) RecommendedDefaults();
            if (GUILayout.Button("Reload Current Settings"))
            {
                ReadCurrentSettings();
                ReadPanels();
            }
        }
    }

    private void ReadCurrentSettings()
    {
        version = PlayerSettings.bundleVersion;
        buildAppBundle = EditorUserBuildSettings.buildAppBundle;
        initialized = true;
    }

    private void Apply()
    {
        try
        {
            if (!ValidateAll(out var error)) throw new InvalidOperationException(error);
            ApplyPanels();
            GameVersionSettings.Apply(version, buildAppBundle);
            configuration.ApplyInputHandling();
            AssetDatabase.SaveAssets();
            ReadCurrentSettings();
            ReadPanels();
            status = $"Applied: {version}";
            Debug.Log($"<color=#56C8FF><b>[Game Version]</b></color> Applied " +
                      $"<color=#7FE07F><b>version {version}</b></color>, " +
                      $"<color=#FFD166><b>Android code {PlayerSettings.Android.bundleVersionCode}</b></color>, " +
                      $"<color=#C792EA><b>format {(buildAppBundle ? "AAB" : "APK")}</b></color>.\n" +
                      $"<color=#80CBC4><b>Company: {identity.companyName}</b></color> | " +
                      $"<color=#FFCB6B><b>Product: {identity.productName}</b></color> | " +
                      $"<color=#82AAFF><b>Package: {identity.packageName}</b></color>");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Game Version", exception.Message, "OK");
        }
    }
}
