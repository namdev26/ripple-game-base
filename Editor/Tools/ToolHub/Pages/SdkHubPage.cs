using System;
using System.Collections.Generic;
using System.IO;
using RippleGame.Sdk.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

[Serializable]
internal sealed class SdkHubPage : ToolHubPage
{
    private const double RefreshSeconds = 1.0;

    [NonSerialized] private Action repaint;
    [NonSerialized] private SdkCatalog catalog;
    [NonSerialized] private SdkInstaller installer;
    [NonSerialized] private List<KeyValuePair<SdkEntry, SdkStatusInfo>> rows = new List<KeyValuePair<SdkEntry, SdkStatusInfo>>();
    [NonSerialized] private double lastRefresh;
    [NonSerialized] private bool busy;
    [NonSerialized] private string error;
    [NonSerialized] private string info;

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    internal override void Initialize(Action repaint)
    {
        this.repaint = repaint;
        Refresh();
    }

    internal override void Draw()
    {
        if (Event.current.type == EventType.Layout && !busy && EditorApplication.timeSinceStartup - lastRefresh > RefreshSeconds)
            Refresh();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Optional SDKs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Each SDK is installed from a pinned, checked tarball into Packages/RippleSdks.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(8);
            DrawMessage();
            foreach (var row in rows) DrawRow(row.Key, row.Value);
        }
    }

    private void DrawMessage()
    {
        if (busy) EditorGUILayout.HelpBox("Working...", MessageType.None);
        else if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        else if (!string.IsNullOrEmpty(info)) EditorGUILayout.HelpBox(info, MessageType.Info);
    }

    private void DrawRow(SdkEntry entry, SdkStatusInfo status)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(entry.DisplayName + "  " + entry.Version, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(Describe(entry, status), EditorStyles.wordWrappedMiniLabel);
                if (status.State == SdkState.Legacy)
                {
                    foreach (var path in status.FoundLegacyPaths) EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("Remove these folders by hand, then press Setup.", EditorStyles.wordWrappedMiniLabel);
                }
            }

            var label = ButtonLabel(status.State);
            if (label == null) return;

            using (new EditorGUI.DisabledScope(busy || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(label, GUILayout.Width(90), GUILayout.Height(24))) Run(entry, status.State);
            }
        }
    }

    private static string Describe(SdkEntry entry, SdkStatusInfo status)
    {
        switch (status.State)
        {
            case SdkState.Installed: return "Installed";
            case SdkState.WrongVersion: return "Wrong version (" + status.InstalledReference + "), expected " + entry.Version;
            case SdkState.Legacy: return "Old copy found in Assets";
            default: return "Not installed";
        }
    }

    private static string ButtonLabel(SdkState state)
    {
        switch (state)
        {
            case SdkState.NotInstalled: return "Setup";
            case SdkState.WrongVersion: return "Fix";
            case SdkState.Installed: return "Remove";
            default: return null;
        }
    }

    private async void Run(SdkEntry entry, SdkState state)
    {
        busy = true;
        error = null;
        info = null;
        SdkOperationResult result;
        try
        {
            var current = Installer();
            result = state == SdkState.Installed ? await current.RemoveAsync(entry.Id) : await current.InstallAsync(entry.Id);
        }
        catch (Exception exception)
        {
            result = SdkOperationResult.Fail(exception.Message);
        }

        busy = false;
        if (result.Success)
        {
            info = result.Message;
            Client.Resolve();
        }
        else
        {
            error = result.Message;
        }

        Refresh();
        repaint?.Invoke();
    }

    private SdkInstaller Installer()
    {
        if (installer == null) installer = new SdkInstaller(Catalog(), ProjectRoot, new UnityWebRequestSdkDownloader());
        return installer;
    }

    private SdkCatalog Catalog()
    {
        if (catalog == null) catalog = SdkCatalog.LoadDefault();
        return catalog;
    }

    private void Refresh()
    {
        lastRefresh = EditorApplication.timeSinceStartup;
        try
        {
            var manifestPath = Path.Combine(ProjectRoot, "Packages", "manifest.json");
            var manifest = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : "{}";
            var fresh = new List<KeyValuePair<SdkEntry, SdkStatusInfo>>();
            foreach (var entry in Catalog().Entries)
                fresh.Add(new KeyValuePair<SdkEntry, SdkStatusInfo>(entry, SdkStatusResolver.Resolve(entry, manifest, ProjectRoot)));
            rows = fresh;
        }
        catch (Exception exception)
        {
            error = "Could not read the SDK list: " + exception.Message;
        }
    }
}
