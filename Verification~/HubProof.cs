using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal static class HubProof
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static void Run()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Single(item => item.GetName().Name == "RippleGame.Tools.Editor").GetType("ToolHubWindow", true);
        var version = PlayerSettings.bundleVersion;
        var window = (EditorWindow)ScriptableObject.CreateInstance(type);
        try
        {
            type.GetMethod("CreateGUI").Invoke(window, null);
            Require(window.rootVisualElement.styleSheets.count > 0, "Hub stylesheet");
            Require(window.rootVisualElement.Query<Button>(className: "hub-card").ToList().Count == 9, "Hub cards");
            Require(window.rootVisualElement.Query<Label>().ToList().Any(label => label.text.Contains("RIPPLE Game base by Namdev26")), "author credit");
            var panel = type.GetField("build", Private).GetValue(window);
            var field = panel.GetType().GetField("version", Private);
            field.SetValue(panel, "8.7.6");
            foreach (var id in new[] { "scenes", "build", "textures", "ads", "analytics", "reports" })
            {
                type.GetMethod("Select", Private).Invoke(window, new object[] { id });
                Require(window.rootVisualElement.Q<IMGUIContainer>("inline-" + id) != null, "inline page " + id);
            }
            var snapshot = EditorJsonUtility.ToJson(window);
            EditorJsonUtility.FromJsonOverwrite(snapshot, window);
            panel = type.GetField("build", Private).GetValue(window);
            Require((string)field.GetValue(panel) == "8.7.6", "staged build settings");
            Require(PlayerSettings.bundleVersion == version, "navigation must not apply settings");
        }
        finally { UnityEngine.Object.DestroyImmediate(window); }
    }

    internal static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("Base proof failed: " + name);
    }
}
