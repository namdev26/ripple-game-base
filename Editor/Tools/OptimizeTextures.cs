using UnityEditor;
using UnityEngine;

public sealed class OptimizeTextures : EditorWindow
{
    [SerializeField] private TextureOptimizerPanel panel = new TextureOptimizerPanel();
    [MenuItem("TOOL/Texture/Android Optimizer")]
    private static void Open() => GetWindow<OptimizeTextures>("Android Texture Optimizer");
    private void OnGUI() => panel.Draw();
}
