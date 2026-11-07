using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>One Hub page in its own window, which can be docked as a tab. Unity keeps it in the layout.</summary>
internal sealed class ToolHubPageWindow : EditorWindow
{
    [SerializeField] private string pageId;
    [SerializeReference] private ToolHubPage page;
    private GUIStyle padding;

    internal string PageId => pageId;

    internal static void Open(string id)
    {
        if (ToolHubCatalog.CreatePage(id) == null)
            return;

        var existing = Resources.FindObjectsOfTypeAll<ToolHubPageWindow>().FirstOrDefault(window => window.pageId == id);
        if (existing != null)
        {
            existing.Show();
            existing.Focus();
            return;
        }

        var created = CreateInstance<ToolHubPageWindow>();
        created.pageId = id;
        created.Prepare();
        created.Show();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(pageId))
            Prepare();
    }

    private void OnDisable() => page?.Dispose();

    private void Prepare()
    {
        if (page == null)
            page = ToolHubCatalog.CreatePage(pageId);

        var section = ToolHubCatalog.Sections.First(item => item.Id == pageId);
        titleContent = new GUIContent(section.Title, EditorGUIUtility.IconContent(section.Icon).image);
        minSize = new Vector2(320, 240);
        autoRepaintOnSceneChange = true;
        page.Initialize(Repaint);
    }

    private void OnGUI()
    {
        if (page == null)
            return;

        if (padding == null)
            padding = new GUIStyle { padding = new RectOffset(8, 8, 8, 8) };

        using (new EditorGUILayout.VerticalScope(padding))
            ToolHubWindow.DrawPage(page);
    }

    private void OnInspectorUpdate() => Repaint();
}
