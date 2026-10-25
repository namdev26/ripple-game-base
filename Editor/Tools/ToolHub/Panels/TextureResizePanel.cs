using UnityEditor;
using UnityEngine;

[System.Serializable]
internal sealed class TextureResizePanel
{
    [SerializeField] private DefaultAsset folder;

    internal void Draw()
    {
        EditorGUILayout.LabelField("Resize Canvas to Multiple of 4", EditorStyles.boldLabel);
        folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);
        EditorGUILayout.HelpBox("Drag a folder from Project. PNG and JPG images in its subfolders are included.",
            MessageType.Info);

        var path = folder == null ? null : AssetDatabase.GetAssetPath(folder);
        var validFolder = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) &&
                          (path == "Assets" || path.StartsWith("Assets/", System.StringComparison.Ordinal));
        if (folder != null && !validFolder)
            EditorGUILayout.HelpBox("Please select a folder inside Assets.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!validFolder || TextureResizeToolLauncher.IsRunning))
        {
            if (GUILayout.Button(TextureResizeToolLauncher.IsRunning ? "Processing..." : "Resize Textures",
                    GUILayout.Height(36)))
            {
                if (path != "Assets" || EditorUtility.DisplayDialog("Resize Entire Assets Folder",
                        "This will modify supported images throughout Assets. Continue?", "Resize", "Cancel"))
                    TextureResizeToolLauncher.Run(path);
            }
        }
    }

}
