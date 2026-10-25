using System;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

[Serializable]
internal sealed class AndroidIconSettings
{
    public Texture2D defaultIcon;
    public Texture2D background;
    public Texture2D foreground;
    [SerializeField] private bool defaultChanged;
    [SerializeField] private bool backgroundChanged;
    [SerializeField] private bool foregroundChanged;

    internal void Read()
    {
        var defaults = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
        defaultIcon = defaults.Length > 0 ? defaults[0] : null;
        var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        background = adaptive.Length > 0 ? adaptive[0].GetTexture(0) : null;
        foreground = adaptive.Length > 0 ? adaptive[0].GetTexture(1) : null;
        defaultChanged = backgroundChanged = foregroundChanged = false;
    }

    internal void Draw()
    {
        EditorGUI.BeginChangeCheck();
        defaultIcon = (Texture2D)EditorGUILayout.ObjectField("Default Icon", defaultIcon, typeof(Texture2D), false);
        defaultChanged |= EditorGUI.EndChangeCheck();
        EditorGUI.BeginChangeCheck();
        background = (Texture2D)EditorGUILayout.ObjectField("Adaptive Background", background, typeof(Texture2D), false);
        backgroundChanged |= EditorGUI.EndChangeCheck();
        EditorGUI.BeginChangeCheck();
        foreground = (Texture2D)EditorGUILayout.ObjectField("Adaptive Foreground", foreground, typeof(Texture2D), false);
        foregroundChanged |= EditorGUI.EndChangeCheck();
        EditorGUILayout.HelpBox("Drag textures from Project. Changing an adaptive layer applies it to all Android icon sizes. " +
                                "Untouched icon sizes and other layers are preserved.", MessageType.Info);
    }

    internal void Apply()
    {
        if (defaultChanged)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { defaultIcon }, IconKind.Any);
        if (!backgroundChanged && !foregroundChanged) return;
        var adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        foreach (var icon in adaptive)
        {
            if (backgroundChanged) icon.SetTexture(background, 0);
            if (foregroundChanged) icon.SetTexture(foreground, 1);
        }
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, adaptive);
    }
}
