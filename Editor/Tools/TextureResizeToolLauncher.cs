using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class TextureResizeToolLauncher
{
    internal static bool IsRunning { get; private set; }

    internal static void Run(string folderPath)
    {
        if (IsRunning || !AssetDatabase.IsValidFolder(folderPath) ||
            (folderPath != "Assets" && !folderPath.StartsWith("Assets/", StringComparison.Ordinal))) return;

        var root = Path.GetFullPath(folderPath);
        var backupRoot = Path.GetFullPath(Path.Combine("Library", "RippleGameTools", "TextureResizeBackups",
            DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")));
        var resized = 0;
        var skipped = 0;
        var errors = 0;
        var cancelled = false;
        IsRunning = true;
        try
        {
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var extension = Path.GetExtension(file);
                if (!IsImage(extension) || IsLegacyBackup(file, root)) continue;
                if (EditorUtility.DisplayCancelableProgressBar("Resizing Texture Canvases", file,
                        (float)i / files.Length))
                {
                    cancelled = true;
                    break;
                }
                try
                {
                    var output = TextureCanvasImageProcessor.Resize(File.ReadAllBytes(file),
                        extension.Equals(".png", StringComparison.OrdinalIgnoreCase));
                    if (output == null) { skipped++; continue; }
                    var relative = file.Substring(root.TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                    var backup = Path.Combine(backupRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(file, backup, false);
                    File.WriteAllBytes(file, output);
                    resized++;
                }
                catch (Exception exception)
                {
                    errors++;
                    Debug.LogError($"[Texture Resize] {file}: {exception.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            errors++;
            Debug.LogException(exception);
        }
        finally
        {
            IsRunning = false;
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        var summary = $"{(cancelled ? "Cancelled" : "Finished")}\nResized: {resized}\nSkipped: {skipped}\nErrors: {errors}";
        if (Directory.Exists(backupRoot)) summary += "\nOriginal image backups: " + backupRoot;
        Debug.Log("[Texture Resize] " + summary);
        EditorUtility.DisplayDialog("Texture Resize", summary, "OK");
    }

    private static bool IsImage(string extension) => extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegacyBackup(string file, string root)
    {
        var relative = file.Substring(root.Length).Replace('\\', '/');
        return relative.IndexOf("/_texture_backup/", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
