#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class TextureOptimizerPanel
{
    private const string Android = "Android";
    private static readonly string[] UiNames = { "UI", "Icon", "Icons", "Button", "Buttons", "HUD" };
    private static readonly string[] BackgroundNames = { "Background", "BG", "Environment" };
    private static readonly string[] DataNames = { "Data", "LUT", "Lookup" };

    [SerializeField] private DefaultAsset folder;
    [SerializeField] private int maximumTextureSize = 2048;

    internal void Draw()
    {
        EditorGUILayout.LabelField("Android Texture Optimizer", EditorStyles.boldLabel);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);
        maximumTextureSize = EditorGUILayout.IntPopup("Maximum Texture Size", maximumTextureSize,
            new[] { "256", "512", "1024", "2048", "4096" }, new[] { 256, 512, 1024, 2048, 4096 });

        var path = folder == null ? null : AssetDatabase.GetAssetPath(folder);
        var isFolder = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        if (folder != null && !isFolder)
            EditorGUILayout.HelpBox("Please select a folder inside Assets.", MessageType.Warning);

        GUI.enabled = isFolder;
        if (GUILayout.Button("Optimize Android Textures", GUILayout.Height(36)))
        {
            if (path != "Assets" || EditorUtility.DisplayDialog(
                    "Optimize Entire Assets Folder",
                    "This will modify all supported textures in Assets. Continue?", "Optimize", "Cancel"))
                OptimizeFolder(path);
        }
        GUI.enabled = true;
    }

    private void OptimizeFolder(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Android Texture Optimizer", "No Texture2D assets were found.", "OK");
            return;
        }

        var changed = 0;
        var skipped = 0;
        var cancelled = false;
        AssetDatabase.StartAssetEditing();
        try
        {
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Optimizing Android Textures", path, (float)i / guids.Length))
                {
                    cancelled = true;
                    break;
                }

                if (Optimize(path)) changed++;
                else skipped++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        if (changed > 0) AssetDatabase.Refresh();
        var status = cancelled ? "Cancelled" : "Finished";
        EditorUtility.DisplayDialog("Android Texture Optimizer", $"{status}\nChanged: {changed}\nUnchanged or skipped: {skipped}", "OK");
    }

    private bool Optimize(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || IsUnsupported(importer)) return false;

        importer.GetSourceTextureWidthAndHeight(out var width, out var height);
        if (width <= 0 || height <= 0) return false;

        var size = Mathf.Max(width, height);
        var isSprite = importer.textureType == TextureImporterType.Sprite;
        var format = GetFormat(importer, path, size);
        var settings = importer.GetPlatformTextureSettings(Android);
        var maxSize = Mathf.Min(Mathf.NextPowerOfTwo(size), maximumTextureSize);
        var changed = !settings.overridden || settings.maxTextureSize != maxSize || settings.format != format ||
                      settings.textureCompression != TextureImporterCompression.Compressed ||
                      settings.compressionQuality != 50 || settings.crunchedCompression || importer.mipmapEnabled ||
                      (isSprite && importer.alphaIsTransparency != importer.DoesSourceTextureHaveAlpha());
        if (!changed) return false;

        settings.overridden = true;
        settings.maxTextureSize = maxSize;
        settings.format = format;
        settings.textureCompression = TextureImporterCompression.Compressed;
        settings.compressionQuality = 50;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
        importer.mipmapEnabled = false;
        if (isSprite) importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
        AssetDatabase.WriteImportSettingsIfDirty(path);
        return true;
    }

    private static bool IsUnsupported(TextureImporter importer)
    {
        return importer.textureType == TextureImporterType.NormalMap ||
               importer.textureType == TextureImporterType.Lightmap ||
               importer.textureType == TextureImporterType.SingleChannel;
    }

    private static TextureImporterFormat GetFormat(TextureImporter importer, string path, int size)
    {
        if (importer.textureType == TextureImporterType.Sprite && Contains(path, UiNames))
            return size <= 256 ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_5x5;

        var isBackground = importer.textureType == TextureImporterType.Sprite && Contains(path, BackgroundNames) ||
                           importer.textureType == TextureImporterType.Default && size >= 2048 &&
                           (importer.sRGBTexture && !Contains(path, DataNames));
        if (isBackground)
            return size <= 1024 ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8;

        return size <= 256 ? TextureImporterFormat.ASTC_5x5 : TextureImporterFormat.ASTC_6x6;
    }

    private static bool Contains(string path, string[] names)
    {
        foreach (var name in names)
            if (path.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
}
#endif
