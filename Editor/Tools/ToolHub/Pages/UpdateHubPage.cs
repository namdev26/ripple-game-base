using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class UpdateHubPage : ToolHubPage
{
    [NonSerialized] private Action repaint;
    [NonSerialized] private PackageUpdateStatus status;
    [NonSerialized] private bool checkStarted;
    [NonSerialized] private bool busy;
    [NonSerialized] private string installError;

    internal override void Initialize(Action repaint) => this.repaint = repaint;

    internal override void Draw()
    {
        if (!checkStarted) Check();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("RIPPLE Game Base", EditorStyles.boldLabel);
            if (status != null)
            {
                EditorGUILayout.LabelField("Installed", status.InstalledVersion + (status.InstalledFromGit ? " (Git)" : " (from disk)"));
                EditorGUILayout.LabelField("Latest release", status.Latest?.Tag ?? "-");
            }

            GUILayout.Space(8);
            DrawMessage();
            GUILayout.Space(8);
            DrawActions();
        }
    }

    private void DrawMessage()
    {
        if (busy) EditorGUILayout.HelpBox("Working...", MessageType.None);
        else if (!string.IsNullOrEmpty(installError)) EditorGUILayout.HelpBox("Update failed: " + installError, MessageType.Error);
        else if (status == null) return;
        else if (!string.IsNullOrEmpty(status.Error)) EditorGUILayout.HelpBox("Could not check for updates: " + status.Error, MessageType.Warning);
        else if (status.Latest == null) EditorGUILayout.HelpBox("The repository has no releases yet.", MessageType.Info);
        else if (!status.UpdateAvailable) EditorGUILayout.HelpBox("You have the latest version.", MessageType.Info);
        else if (status.InstalledFromGit) EditorGUILayout.HelpBox("Version " + status.Latest.Tag + " is available. Read the changes first, then commit your work before updating.", MessageType.Warning);
        else EditorGUILayout.HelpBox("Version " + status.Latest.Tag + " is available, but this project uses the package from disk. To update, point Packages/manifest.json at " + PackageUpdateLogic.InstallUrl(status.Source, status.Latest.Tag), MessageType.Warning);
    }

    private void DrawActions()
    {
        using (new EditorGUI.DisabledScope(busy))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Check now", GUILayout.Height(28))) Check();
            if (status == null || !status.UpdateAvailable) return;

            var changelog = PackageUpdateLogic.ChangelogUrl(status.Source, status.Latest.Tag);
            if (changelog != null && GUILayout.Button("View changes", GUILayout.Height(28))) Application.OpenURL(changelog);

            using (new EditorGUI.DisabledScope(!status.InstalledFromGit || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            {
                if (GUILayout.Button("Update to " + status.Latest.Tag, GUILayout.Height(28))) Install();
            }
        }
    }

    private async void Check()
    {
        checkStarted = true;
        busy = true;
        installError = null;
        status = await PackageUpdates.CheckAsync();
        busy = false;
        repaint?.Invoke();
    }

    private void Install()
    {
        var tag = status.Latest.Tag;
        if (!EditorUtility.DisplayDialog("Update RIPPLE Game Base",
                "Update from " + status.InstalledVersion + " to " + tag + "?\n\nCommit your work first. If the new version breaks your code, revert Packages/manifest.json and Packages/packages-lock.json to go back.",
                "Update", "Cancel")) return;

        busy = true;
        installError = null;
        PackageUpdates.Install(status, error =>
        {
            busy = false;
            installError = error;
            repaint?.Invoke();
        });
    }
}
