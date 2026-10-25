using UnityEditor;
using UnityEngine;

internal sealed partial class AndroidBuildPanel
{
    [SerializeField] private AndroidIdentitySettings identity = new AndroidIdentitySettings();
    [SerializeField] private AndroidIconSettings icons = new AndroidIconSettings();
    [SerializeField] private AndroidRenderingSettings rendering = new AndroidRenderingSettings();
    [SerializeField] private AndroidConfigurationSettings configuration = new AndroidConfigurationSettings();
    [SerializeField] private AndroidOptimizationSettings optimization = new AndroidOptimizationSettings();
    [SerializeField] private AndroidPublishingSettings publishing = new AndroidPublishingSettings();
    [SerializeField] private AndroidGradleSettings gradle = new AndroidGradleSettings();
    [SerializeField] private bool panelsInitialized;
    [SerializeField] private bool identityOpen = true, iconsOpen, renderingOpen, configurationOpen,
        optimizationOpen, publishingOpen, gradleOpen;

    private void ReadPanels()
    {
        identity.Read();
        icons.Read();
        rendering.Read();
        configuration.Read();
        optimization.Read();
        publishing.Read();
        gradle.Read();
        panelsInitialized = true;
    }

    private void RecommendedDefaults()
    {
        identity.overridePackageName = true;
        rendering.Recommended();
        configuration.Recommended();
        optimization.Recommended();
        publishing.minifyRelease = false;
        buildAppBundle = true;
    }

    private void DrawPanels()
    {
        identityOpen = EditorGUILayout.Foldout(identityOpen, "General / Identification", true);
        if (identityOpen) identity.Draw();
        iconsOpen = EditorGUILayout.Foldout(iconsOpen, "Icons", true);
        if (iconsOpen) icons.Draw();
        renderingOpen = EditorGUILayout.Foldout(renderingOpen, "Resolution / Rendering", true);
        if (renderingOpen) rendering.Draw();
        configurationOpen = EditorGUILayout.Foldout(configurationOpen, "Android Configuration", true);
        if (configurationOpen) configuration.Draw();
        optimizationOpen = EditorGUILayout.Foldout(optimizationOpen, "Optimization", true);
        if (optimizationOpen) optimization.Draw();
        publishingOpen = EditorGUILayout.Foldout(publishingOpen, "Publishing / Signing", true);
        if (publishingOpen) publishing.Draw();
        gradleOpen = EditorGUILayout.Foldout(gradleOpen, "Custom Gradle Templates", true);
        if (gradleOpen) gradle.Draw();
    }

    private bool ValidateAll(out string error)
    {
        if (!GameVersionSettings.TryValidate(version, out error) || !identity.Validate(out error) ||
            !rendering.Validate(out error) || !configuration.Validate(out error) ||
            !publishing.Validate(out error) || !gradle.Validate(out error)) return false;
        try
        {
            GameVersionSettings.GetVersionCodeForApply(PlayerSettings.bundleVersion, version,
                PlayerSettings.Android.bundleVersionCode);
            return true;
        }
        catch (System.Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private void ApplyPanels()
    {
        gradle.ApplyTemplateFiles();
        identity.Apply();
        icons.Apply();
        rendering.Apply();
        configuration.Apply();
        optimization.Apply();
        publishing.Apply();
        gradle.Apply();
    }

    public override void Dispose() => publishing.ClearPasswords();
}
