using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
internal sealed class AndroidRenderingSettings
{
    public UIOrientation orientation;
    public bool outsideSafeArea;
    public ColorSpace colorSpace;
    public bool autoGraphicsApi;
    public bool openGles3;
    public bool vulkan;
    public bool preferVulkan;
    public bool multithreaded;

    internal void Read()
    {
        orientation = PlayerSettings.defaultInterfaceOrientation;
        outsideSafeArea = PlayerSettings.Android.renderOutsideSafeArea;
        colorSpace = PlayerSettings.colorSpace;
        autoGraphicsApi = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        openGles3 = Array.IndexOf(apis, GraphicsDeviceType.OpenGLES3) >= 0;
        vulkan = Array.IndexOf(apis, GraphicsDeviceType.Vulkan) >= 0;
        preferVulkan = apis.Length > 0 && apis[0] == GraphicsDeviceType.Vulkan;
        multithreaded = PlayerSettings.GetMobileMTRendering(NamedBuildTarget.Android);
    }

    internal void Recommended()
    {
        orientation = UIOrientation.Portrait;
        autoGraphicsApi = false;
        openGles3 = true;
        vulkan = preferVulkan = false;
        multithreaded = true;
    }

    internal void Draw()
    {
        orientation = (UIOrientation)EditorGUILayout.EnumPopup("Default Orientation", orientation);
        outsideSafeArea = EditorGUILayout.Toggle("Render Outside Safe Area", outsideSafeArea);
        colorSpace = (ColorSpace)EditorGUILayout.EnumPopup("Color Space", colorSpace);
        autoGraphicsApi = EditorGUILayout.Toggle("Auto Graphics API", autoGraphicsApi);
        using (new EditorGUI.DisabledScope(autoGraphicsApi))
        {
            openGles3 = EditorGUILayout.Toggle("OpenGLES3", openGles3);
            vulkan = EditorGUILayout.Toggle("Vulkan", vulkan);
            if (openGles3 && vulkan) preferVulkan = EditorGUILayout.Toggle("Prefer Vulkan First", preferVulkan);
        }
        multithreaded = EditorGUILayout.Toggle("Multithreaded Rendering", multithreaded);
    }

    internal bool Validate(out string error)
    {
        error = !autoGraphicsApi && !openGles3 && !vulkan ? "Choose at least one Android Graphics API." : null;
        return error == null;
    }

    internal void Apply()
    {
        PlayerSettings.defaultInterfaceOrientation = orientation;
        PlayerSettings.Android.renderOutsideSafeArea = outsideSafeArea;
        PlayerSettings.colorSpace = colorSpace;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, autoGraphicsApi);
        if (!autoGraphicsApi)
        {
            var apis = openGles3 && vulkan
                ? (preferVulkan ? new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 }
                    : new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan })
                : new[] { vulkan ? GraphicsDeviceType.Vulkan : GraphicsDeviceType.OpenGLES3 };
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
        }
        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, multithreaded);
    }
}
