using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class ToolHubAssetInspector : IDisposable
{
    [SerializeField] private ScriptableObject asset;
    [NonSerialized] private UnityEditor.Editor inspector;
    [NonSerialized] private bool searched;

    internal bool Draw(string typeName, string createMenu)
    {
        var type = FindType(typeName);
        if (type == null)
        {
            EditorGUILayout.HelpBox("Install the optional package to edit its configuration here.", MessageType.Info);
            return false;
        }
        if (!searched)
        {
            FindAsset(type);
            searched = true;
        }
        var selected = EditorGUILayout.ObjectField("Configuration", asset, type, false) as ScriptableObject;
        if (selected != asset)
        {
            DisposeInspector();
            asset = selected;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(asset == null ? "Create Configuration" : "Select in Project"))
            {
                if (asset == null)
                {
                    ToolHubCommands.Run(createMenu);
                    searched = false;
                }
                else EditorGUIUtility.PingObject(asset);
            }
            if (GUILayout.Button("Find Existing")) FindAsset(type);
            using (new EditorGUI.DisabledScope(asset == null))
                if (GUILayout.Button("Save Configuration")) AssetDatabase.SaveAssetIfDirty(asset);
        }
        if (asset == null)
        {
            EditorGUILayout.HelpBox("Choose an existing asset or create a configuration for this project.", MessageType.Info);
            return true;
        }
        UnityEditor.Editor.CreateCachedEditor(asset, null, ref inspector);
        GUILayout.Space(12);
        inspector.OnInspectorGUI();
        return true;
    }

    private void FindAsset(Type type)
    {
        if (asset != null) return;
        foreach (var guid in AssetDatabase.FindAssets("t:" + type.Name))
        {
            asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type) as ScriptableObject;
            if (asset != null) break;
        }
    }

    private static Type FindType(string name)
    {
        foreach (var type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            if (type.FullName == name) return type;
        return null;
    }

    private void DisposeInspector()
    {
        if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
        inspector = null;
    }

    public void Dispose()
    {
        DisposeInspector();
        searched = false;
    }
}
