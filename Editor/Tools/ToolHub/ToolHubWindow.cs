using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class ToolHubWindow : EditorWindow
{
    [SerializeField] private string selected = "overview";
    [SerializeField] private string query = string.Empty;
    [SerializeField] private SceneSwitcherPanel scenes = new SceneSwitcherPanel();
    [SerializeField] private AndroidBuildPanel build = new AndroidBuildPanel();
    [SerializeField] private TextureHubPage textures = new TextureHubPage();
    [SerializeField] private AdsHubPage ads = new AdsHubPage();
    [SerializeField] private AnalyticsHubPage analytics = new AnalyticsHubPage();
    [SerializeField] private SaveHubPage save = new SaveHubPage();
    [SerializeField] private LocalizationHubPage localization = new LocalizationHubPage();
    [SerializeField] private BuildReportsHubPage reports = new BuildReportsHubPage();
    [SerializeField] private UpdateHubPage update = new UpdateHubPage();
    private ToolHubNavigation navigation;
    private VisualElement body;
    private Label pageTitle;
    private Button popOut;

    [MenuItem("RIPPLE Game/Open Hub", false, 100000)]
    internal static void Open()
    {
        var window = GetWindow<ToolHubWindow>();
        window.titleContent = new GUIContent("TOOL Hub");
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    private ToolHubPage[] Pages => new ToolHubPage[] { scenes, build, textures, ads, analytics, save, localization, reports, update };

    private void OnEnable()
    {
        titleContent = new GUIContent("TOOL Hub");
        minSize = new Vector2(980, 620);
        autoRepaintOnSceneChange = true;
        foreach (var page in Pages) page.Initialize(Repaint);
    }

    private void OnDisable()
    {
        foreach (var page in Pages) page.Dispose();
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.Clear();
        root.AddToClassList("tool-hub");
        root.EnableInClassList("light", !EditorGUIUtility.isProSkin);
        var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        var stylePath = Path.GetDirectoryName(scriptPath).Replace('\\', '/') + "/ToolHub.uss";
        var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
        if (stylesheet != null) root.styleSheets.Add(stylesheet);

        var workspace = new VisualElement();
        workspace.AddToClassList("hub-workspace");
        root.Add(workspace);
        navigation = new ToolHubNavigation(workspace, Select);
        var main = new VisualElement();
        main.AddToClassList("hub-main");
        workspace.Add(main);
        var toolbar = new VisualElement();
        toolbar.AddToClassList("hub-toolbar");
        var search = new ToolbarSearchField { name = "tool-search", value = query };
        search.AddToClassList("hub-search");
        search.tooltip = "Search tools by name or description";
        search.RegisterValueChangedCallback(evt =>
        {
            query = evt.newValue;
            Select("overview");
        });
        toolbar.Add(search);
        main.Add(toolbar);
        var pageHeader = new VisualElement();
        pageHeader.AddToClassList("hub-page-header");
        pageTitle = new Label();
        pageTitle.AddToClassList("hub-page-title");
        pageHeader.Add(pageTitle);
        popOut = new Button(() => ToolHubPageWindow.Open(selected))
        {
            name = "pop-out",
            text = "Pop out",
            tooltip = "Open this tool in its own tab, which you can dock anywhere."
        };
        popOut.AddToClassList("hub-pop-out");
        pageHeader.Add(popOut);
        main.Add(pageHeader);
        body = new VisualElement { name = "tool-content" };
        body.AddToClassList("hub-body");
        main.Add(body);
        Select(selected);
    }

    private void Select(string id)
    {
        var section = ToolHubCatalog.Sections.FirstOrDefault(item => item.Id == id);
        selected = section == null ? "overview" : id;
        navigation.Update(selected, query);
        pageTitle.text = section?.Title ?? "Overview";
        pageTitle.tooltip = section?.Description ?? string.Empty;
        popOut.style.display = section == null ? DisplayStyle.None : DisplayStyle.Flex;
        body.Clear();
        if (section == null)
        {
            body.Add(ToolHubOverview.Create(query, Select));
            return;
        }
        var page = GetPage(selected);
        var content = new IMGUIContainer(() => DrawPage(page)) { name = "inline-" + selected };
        content.AddToClassList("hub-inline-content");
        body.Add(content);
    }

    internal void ShowPage(string id) => Select(id);

    private ToolHubPage GetPage(string id)
    {
        switch (id)
        {
            case "scenes": return scenes;
            case "build": return build;
            case "textures": return textures;
            case "ads": return ads;
            case "analytics": return analytics;
            case "save": return save;
            case "localization": return localization;
            case "update": return update;
            default: return reports;
        }
    }

    internal static void DrawPage(ToolHubPage page)
    {
        var previousWidth = EditorGUIUtility.labelWidth;
        var previousEnabled = GUI.enabled;
        try
        {
            EditorGUIUtility.labelWidth = 210;
            using (new UnityEditor.EditorGUILayout.VerticalScope()) page.Draw();
        }
        finally
        {
            EditorGUIUtility.labelWidth = previousWidth;
            GUI.enabled = previousEnabled;
        }
    }

    private void OnInspectorUpdate() => Repaint();
}
