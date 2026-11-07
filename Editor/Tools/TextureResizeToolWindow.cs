using UnityEditor;
using UnityEngine;

internal sealed class TextureResizeToolWindow : EditorWindow
{
    [SerializeField] private TextureResizePanel panel = new TextureResizePanel();
    [MenuItem("RIPPLE Game/Texture/Resize Canvas to Multiple of 4")]
    private static void Open() => GetWindow<TextureResizeToolWindow>("Texture Canvas Resize");
    private void OnGUI() => panel.Draw();
    private void OnInspectorUpdate() => Repaint();
}
