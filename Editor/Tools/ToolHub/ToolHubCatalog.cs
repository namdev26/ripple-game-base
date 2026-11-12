using System;
using System.Collections.Generic;

internal sealed class ToolHubSection
{
    internal string Id { get; }
    internal string Title { get; }
    internal string Description { get; }
    internal string Icon { get; }

    internal ToolHubSection(string id, string title, string description, string icon)
    {
        Id = id;
        Title = title;
        Description = description;
        Icon = icon;
    }
}

internal static class ToolHubCatalog
{
    internal static readonly ToolHubSection[] Sections =
    {
        new ToolHubSection("scenes", "Scenes", "Browse and switch project scenes, grouped by build order.", "SceneAsset Icon"),
        new ToolHubSection("build", "Android Build", "Version, identity, icons, signing and Android settings.", "BuildSettings.Android"),
        new ToolHubSection("textures", "Textures", "Resize image canvases and optimize Android texture imports.", "Texture Icon"),
        new ToolHubSection("ads", "ADS", "Edit ad configuration, scene setup and Android build profiles.", "SettingsIcon"),
        new ToolHubSection("analytics", "Analytics", "Edit event collection settings and enable Firebase integration.", "Profiler.NetworkMessages"),
        new ToolHubSection("save", "Save", "Create the game's save key and inspect encrypted save files.", "SaveAs"),
        new ToolHubSection("localization", "Localization", "Sync the game's translations from a Google Sheet.", "FilterByLabel"),
        new ToolHubSection("reports", "Build Reports", "Inspect the latest build result, size, duration and output.", "UnityEditor.ConsoleWindow"),
        new ToolHubSection("update", "Update", "Check for a new package version and install it.", "Refresh"),
        new ToolHubSection("sdk", "SDK", "Install, fix or remove the optional third-party SDKs.", "Package Manager")
    };

    internal static ToolHubPage CreatePage(string id)
    {
        switch (id)
        {
            case "scenes": return new SceneSwitcherPanel();
            case "build": return new AndroidBuildPanel();
            case "textures": return new TextureHubPage();
            case "ads": return new AdsHubPage();
            case "analytics": return new AnalyticsHubPage();
            case "save": return new SaveHubPage();
            case "localization": return new LocalizationHubPage();
            case "reports": return new BuildReportsHubPage();
            case "update": return new UpdateHubPage();
            case "sdk": return new SdkHubPage();
            default: return null;
        }
    }

    internal static IEnumerable<ToolHubSection> Search(string query)
    {
        query = (query ?? string.Empty).Trim();
        foreach (var section in Sections)
            if (query.Length == 0 || section.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                section.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                yield return section;
    }
}
