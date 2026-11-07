using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
internal sealed class LocalizationHubPage : ToolHubPage
{
    [SerializeField] private Vector2 scroll;
    [NonSerialized] private string sheetUrl = string.Empty;
    [NonSerialized] private string report = string.Empty;
    [NonSerialized] private MessageType reportType = MessageType.None;
    [NonSerialized] private UnityWebRequest request;
    [NonSerialized] private UnityWebRequestAsyncOperation download;
    [NonSerialized] private Action repaint;

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    private static string ResourcesFolder => Path.Combine(Application.dataPath, "Resources");

    internal override void Initialize(Action repaint)
    {
        this.repaint = repaint;
        sheetUrl = LocalizationSyncSettings.Load(ProjectRoot);
    }

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawSync();
        }
    }

    public override void Dispose() => Stop();

    private void DrawSync()
    {
        EditorGUILayout.LabelField("Sync from Google Sheet", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The Sheet is the source of truth: Assets/Resources/Localization*.txt are replaced with its content. " +
            "Share the Sheet with anyone who has the link, then commit the updated files.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(request != null))
        {
            sheetUrl = EditorGUILayout.TextField("Sheet link", sheetUrl);
            if (GUILayout.Button(request != null ? "Syncing..." : "Sync", GUILayout.Height(28)))
                StartSync();
        }

        if (!string.IsNullOrEmpty(report))
            EditorGUILayout.HelpBox(report, reportType);
    }

    internal void StartSync()
    {
        if (request != null)
            return;

        string url;
        try
        {
            url = SheetUrl.ToCsvExport(sheetUrl);
        }
        catch (ArgumentException exception)
        {
            Show(exception.Message, MessageType.Error);
            return;
        }

        LocalizationSyncSettings.Save(ProjectRoot, sheetUrl.Trim());
        Show("Downloading the Sheet...", MessageType.Info);
        request = UnityWebRequest.Get(url);
        download = request.SendWebRequest();
        EditorApplication.update += Poll;
    }

    private void Poll()
    {
        if (!download.isDone)
            return;

        var succeeded = request.result == UnityWebRequest.Result.Success;
        var error = request.error;
        var text = succeeded ? Encoding.UTF8.GetString(request.downloadHandler.data) : null;
        Stop();

        if (succeeded)
            Apply(text);
        else
            Show("Download failed: " + error + ". Check the link and that the Sheet is shared.", MessageType.Error);
    }

    private void Apply(string text)
    {
        var sheet = LocalizationSheet.Build(text);
        if (!sheet.Ok)
        {
            Show("No files were changed.\n" + string.Join("\n", sheet.Errors), MessageType.Error);
            return;
        }

        try
        {
            var written = LocalizationFileWriter.Write(ResourcesFolder, sheet.Files);
            if (written.Written.Count > 0)
                AssetDatabase.Refresh();

            Show(Describe(sheet, written), MessageType.Info);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Show("Writing failed, some files may be updated. Check git and sync again.\n" + exception.Message, MessageType.Error);
        }
    }

    private static string Describe(LocalizationSheetResult sheet, LocalizationWriteReport written)
    {
        var lines = new StringBuilder();
        lines.AppendLine("Updated: " + (written.Written.Count > 0 ? string.Join(", ", written.Written) : "none"));
        lines.AppendLine("Unchanged: " + (written.Unchanged.Count > 0 ? string.Join(", ", written.Unchanged) : "none"));
        if (sheet.SkippedColumns.Count > 0)
            lines.AppendLine("Skipped columns: " + string.Join(", ", sheet.SkippedColumns));
        foreach (var removed in written.RemovedKeys.OrderBy(entry => entry.Key))
            lines.AppendLine("Keys no longer on the Sheet (" + removed.Key + "): " + string.Join(", ", removed.Value));

        return lines.ToString().TrimEnd();
    }

    private void Show(string message, MessageType type)
    {
        report = message;
        reportType = type;
        repaint?.Invoke();
    }

    private void Stop()
    {
        EditorApplication.update -= Poll;
        request?.Dispose();
        request = null;
        download = null;
    }
}
