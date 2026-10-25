using System;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class TextureHubPage : ToolHubPage
{
    [SerializeField] private TextureResizePanel resize = new TextureResizePanel();
    [SerializeField] private TextureOptimizerPanel optimizer = new TextureOptimizerPanel();
    [SerializeField] private Vector2 scroll;

    internal override void Draw()
    {
        using (var view = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = view.scrollPosition;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) resize.Draw();
            GUILayout.Space(18);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) optimizer.Draw();
        }
    }
}
