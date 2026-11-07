using System;
using UnityEditor;
using UnityEngine.UIElements;

internal static class ToolHubOverview
{
    internal static VisualElement Create(string query, Action<string> select)
    {
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.AddToClassList("hub-overview");
        var cards = new VisualElement();
        cards.AddToClassList("hub-cards");
        var count = 0;
        foreach (var section in ToolHubCatalog.Search(query))
        {
            cards.Add(CreateCard(section, select));
            count++;
        }
        scroll.Add(cards);
        if (count == 0)
        {
            var empty = new Label("No matching tools.");
            empty.AddToClassList("hub-empty");
            scroll.Add(empty);
        }
        return scroll;
    }

    private static Button CreateCard(ToolHubSection section, Action<string> select)
    {
        var card = new Button(() => select(section.Id)) { name = "card-" + section.Id, tooltip = section.Description };
        card.AddToClassList("hub-card");
        var icon = new Image { image = EditorGUIUtility.IconContent(section.Icon).image };
        icon.AddToClassList("hub-card-icon");
        card.Add(icon);
        var title = new Label(section.Title);
        title.AddToClassList("hub-card-title");
        card.Add(title);
        return card;
    }
}
