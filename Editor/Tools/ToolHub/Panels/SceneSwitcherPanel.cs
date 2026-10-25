using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
internal sealed class SceneSwitcherPanel : ToolHubPage
{
    [NonSerialized] private Action requestRepaint;
    [SerializeField] private Vector2 scrollPosition;
    private readonly List<string> buildScenePaths = new List<string>();
    private readonly List<string> otherScenePaths = new List<string>();

    internal override void Initialize(Action repaint)
    {
        Dispose();
        requestRepaint = repaint;
        EditorApplication.projectChanged += RefreshScenes;
        EditorBuildSettings.sceneListChanged += RefreshScenes;
        RefreshScenes();
    }

    public override void Dispose()
    {
        EditorApplication.projectChanged -= RefreshScenes;
        EditorBuildSettings.sceneListChanged -= RefreshScenes;
        requestRepaint = null;
    }

    private void RefreshScenes()
    {
        buildScenePaths.Clear();
        otherScenePaths.Clear();
        var buildPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path) &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null &&
                buildPaths.Add(scene.path))
            {
                buildScenePaths.Add(scene.path);
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!buildPaths.Contains(path))
                otherScenePaths.Add(path);
        }

        otherScenePaths.Sort(StringComparer.OrdinalIgnoreCase);
        requestRepaint?.Invoke();
    }

    internal override void Draw()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("In Build", EditorStyles.boldLabel);
        foreach (string path in buildScenePaths)
        {
            DrawSceneButton(path);
        }
        if (otherScenePaths.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Not In Build", EditorStyles.boldLabel);
            foreach (string path in otherScenePaths)
            {
                DrawSceneButton(path);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawSceneButton(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        Color originalColor = GUI.backgroundColor;
        if (SceneManager.GetActiveScene().path == path)
            GUI.backgroundColor = Color.green;

        bool clicked = GUILayout.Button(new GUIContent(name, path),
            new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter },
            GUILayout.Height(32));
        GUI.backgroundColor = originalColor;

        if (clicked && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}
