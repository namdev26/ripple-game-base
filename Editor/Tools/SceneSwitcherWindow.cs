using UnityEditor;
using UnityEngine;

public class SceneSwitcherWindow : EditorWindow
{
    [SerializeField] private SceneSwitcherPanel panel = new SceneSwitcherPanel();
    [MenuItem("RIPPLE Game/Scene Switch")]
    private static void Open() => GetWindow<SceneSwitcherWindow>("Scene Switch");
    private void OnEnable()
    {
        titleContent = new GUIContent("Scene Switch");
        autoRepaintOnSceneChange = true;
        panel.Initialize(Repaint);
    }
    private void OnGUI() => panel.Draw();
    private void OnDisable() => panel.Dispose();
    private void OnInspectorUpdate() => Repaint();
}
