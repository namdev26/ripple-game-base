using System;
using System.IO;
using RippleGame.Save;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class SaveHubPage : ToolHubPage
{
    [SerializeField] private string keyFolder = "Assets";
    [SerializeField] private string saveFilePath = string.Empty;
    [SerializeField] private Vector2 scroll;
    // Keys and decrypted data stay out of serialized window state, which lives on disk.
    [NonSerialized] private string inspectKey = string.Empty;
    [NonSerialized] private string decrypted = string.Empty;
    [NonSerialized] private string message = string.Empty;
    [NonSerialized] private MessageType messageType = MessageType.None;

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawKeyCreation();
            GUILayout.Space(18);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawInspector();
        }
    }

    private void DrawKeyCreation()
    {
        EditorGUILayout.LabelField("Save key", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Never change this key after release, or players lose their saves.", MessageType.Warning);
        keyFolder = EditorGUILayout.TextField("Folder", keyFolder);
        if (GUILayout.Button("Create " + SaveKeyFile.FileName, GUILayout.Height(28))) CreateKey();
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
    }

    private void DrawInspector()
    {
        EditorGUILayout.LabelField("Inspect a save file", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            saveFilePath = EditorGUILayout.TextField("File", saveFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var chosen = EditorUtility.OpenFilePanel("Choose a save file", Application.persistentDataPath, string.Empty);
                if (!string.IsNullOrEmpty(chosen)) saveFilePath = chosen;
            }
        }

        inspectKey = EditorGUILayout.PasswordField("Key", inspectKey);
        if (GUILayout.Button("Decrypt", GUILayout.Height(28))) Decrypt();
        if (string.IsNullOrEmpty(decrypted)) return;
        using (new EditorGUI.DisabledScope(false))
            EditorGUILayout.SelectableLabel(decrypted, EditorStyles.textArea, GUILayout.MinHeight(220));
    }

    private void CreateKey()
    {
        try
        {
            if (!keyFolder.Replace('\\', '/').StartsWith("Assets", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(keyFolder))
                throw new IOException("Choose an existing folder inside Assets.");

            var path = SaveKeyFile.Write(keyFolder, SaveInspector.GenerateKey());
            AssetDatabase.ImportAsset(path.Replace('\\', '/'));
            message = "Created " + path + ". Use it with: new SaveFile(\"main\", GameSaveKey.Value)";
            messageType = MessageType.Info;
        }
        catch (IOException exception)
        {
            message = exception.Message;
            messageType = MessageType.Error;
        }
    }

    private void Decrypt()
    {
        decrypted = string.Empty;
        try
        {
            if (!File.Exists(saveFilePath))
            {
                EditorUtility.DisplayDialog("Inspect a save file", "That file does not exist.", "OK");
                return;
            }

            if (!SaveInspector.TryDecrypt(File.ReadAllBytes(saveFilePath), inspectKey, out var json))
            {
                EditorUtility.DisplayDialog("Inspect a save file", "The key is wrong or the file is damaged.", "OK");
                return;
            }

            decrypted = json;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            EditorUtility.DisplayDialog("Inspect a save file", exception.Message, "OK");
        }
    }
}
