using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class AndroidGradleSettings
{
    private static readonly string[] Properties = { "useCustomMainGradleTemplate", "useCustomLauncherGradleManifest",
        "useCustomBaseGradleTemplate", "useCustomGradlePropertiesTemplate", "useCustomGradleSettingsTemplate" };
    private static readonly string[] Files = { "mainTemplate.gradle", "launcherTemplate.gradle",
        "baseProjectTemplate.gradle", "gradleTemplate.properties", "settingsTemplate.gradle" };
    [SerializeField] private bool[] enabled = new bool[5];

    internal void Read()
    {
        for (var i = 0; i < Files.Length; i++)
            enabled[i] = File.Exists(Target(Files[i]));
    }

    internal void Draw()
    {
        for (var i = 0; i < Files.Length; i++) enabled[i] = EditorGUILayout.Toggle(Files[i], enabled[i]);
        EditorGUILayout.HelpBox("Enable only templates required by your plugins. Apply restores disabled templates " +
            "before copying Unity defaults. Turning a template off keeps it as a .DISABLED backup.", MessageType.Info);
    }

    internal bool Validate(out string error)
    {
        error = null;
        for (var i = 0; i < Files.Length; i++)
        {
            var target = Target(Files[i]);
            var backup = target + ".DISABLED";
            if (!enabled[i])
            {
                if (!File.Exists(target) || !File.Exists(backup)) continue;
                error = $"Both {target} and its .DISABLED backup exist. Resolve the duplicate before disabling this template.";
                return false;
            }
            if (File.Exists(target) || File.Exists(backup) || File.Exists(Source(Files[i]))) continue;
            error = $"Unity's Android template {Files[i]} is missing. Check Android Build Support installation.";
            return false;
        }
        return true;
    }

    internal void ApplyTemplateFiles()
    {
        for (var i = 0; i < Files.Length; i++)
        {
            var target = Target(Files[i]);
            var backup = target + ".DISABLED";
            if (!enabled[i])
            {
                if (File.Exists(target)) MoveTemplate(target, backup);
                continue;
            }
            if (File.Exists(target)) continue;
            if (File.Exists(backup))
            {
                MoveTemplate(backup, target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(Source(Files[i]), target, false);
            File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
            AssetDatabase.ImportAsset(target);
        }
    }

    private static void MoveTemplate(string source, string destination)
    {
        if (File.Exists(destination)) throw new IOException($"Template destination already exists: {destination}");
        AssetDatabase.ImportAsset(source);
        var error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
    }

    private static string Target(string file) => "Assets/Plugins/Android/" + file;

    internal void Apply()
    {
        using (var settings = AndroidSerializedSettings.Open())
        {
            for (var i = 0; i < Properties.Length; i++)
                AndroidSerializedSettings.Require(settings, Properties[i]).boolValue = enabled[i];
            settings.ApplyModifiedProperties();
        }
    }

    private static string Source(string file) => Path.Combine(EditorApplication.applicationContentsPath,
        "PlaybackEngines", "AndroidPlayer", "Tools", "GradleTemplates", file);
}
