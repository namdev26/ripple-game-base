using UnityEditor;
using UnityEngine;

internal sealed partial class GameVersionSettingsWindow : EditorWindow
{
    [SerializeField] private AndroidBuildPanel panel = new AndroidBuildPanel();
    [MenuItem("RIPPLE Game/Build/Version Settings")]
    [MenuItem("RIPPLE Game/Build/Android Settings")]
    private static void Open() => GetWindow<GameVersionSettingsWindow>("Android Build Settings");
    private void OnEnable()
    {
        minSize = new Vector2(560, 520);
        panel.Initialize(Repaint);
    }
    private void OnGUI() => panel.Draw();
    private void OnDisable() => panel.Dispose();
}
