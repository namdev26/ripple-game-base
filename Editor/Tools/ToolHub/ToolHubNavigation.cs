using System;
using UnityEditor;
using UnityEngine.UIElements;

internal sealed class ToolHubNavigation
{
    private readonly VisualElement list;
    private readonly Action<string> select;

    internal ToolHubNavigation(VisualElement parent, Action<string> select)
    {
        this.select = select;
        var rail = new VisualElement { name = "navigation" };
        rail.AddToClassList("hub-navigation");
        var brand = new Label("RIPPLE Game base by Namdev26");
        brand.AddToClassList("hub-brand");
        rail.Add(brand);
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.AddToClassList("hub-navigation-scroll");
        list = scroll.contentContainer;
        rail.Add(scroll);
        parent.Add(rail);
    }

    internal void Update(string selected, string query)
    {
        list.Clear();
        AddButton("overview", "Overview", "Grid.BoxTool", selected);
        var count = 0;
        foreach (var section in ToolHubCatalog.Search(query))
        {
            AddButton(section.Id, section.Title, section.Icon, selected);
            count++;
        }
        if (count == 0) list.Add(new Label("No matching tools."));
    }

    private void AddButton(string id, string title, string icon, string selected)
    {
        var button = new Button(() => select(id)) { name = "nav-" + id, tooltip = title };
        button.AddToClassList("hub-nav-item");
        button.EnableInClassList("selected", selected == id);
        var image = new Image { image = EditorGUIUtility.IconContent(icon).image };
        image.AddToClassList("hub-nav-icon");
        button.Add(image);
        button.Add(new Label(title));
        list.Add(button);
    }
}
