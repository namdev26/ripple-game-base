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
    [SerializeField] private BuildReportsHubPage reports = new BuildReportsHubPage();
    [SerializeField] private UpdateHubPage update = new UpdateHubPage();
    private ToolHubNavigation navigation;
    private VisualElement body;
    private Label pageTitle;
    private Label pageDescription;

    [MenuItem("TOOL/Open Hub", false, -1000)]
    internal static void Open()
    {
        var window = GetWindow<ToolHubWindow>();
        window.titleContent = new GUIContent("TOOL Hub");
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    private ToolHubPage[] Pages => new ToolHubPage[] { scenes, build, textures, ads, analytics, save, reports, update };

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

        var header = new VisualElement();
        header.AddToClassList("hub-header");
        var heading = new Label("TOOL Hub");
        heading.AddToClassList("hub-heading");
        header.Add(heading);
        var project = new Label(Application.productName + "  /  Unity " + Application.unityVersion);
        project.AddToClassList("hub-project");
        header.Add(project);
        root.Add(header);
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
        pageDescription = new Label();
        pageDescription.AddToClassList("hub-description");
        pageHeader.Add(pageTitle);
        pageHeader.Add(pageDescription);
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
        pageDescription.text = section?.Description ?? "Every workspace, one place to manage your project.";
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
            case "update": return update;
            default: return reports;
        }
    }

    private static void DrawPage(ToolHubPage page)
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
