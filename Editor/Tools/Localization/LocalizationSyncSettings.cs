using System;
using System.IO;
using UnityEngine;

/// <summary>Keeps the game's Sheet link in ProjectSettings so the whole team shares it through git.</summary>
internal static class LocalizationSyncSettings
{
    [Serializable]
    private sealed class Data
    {
        public string sheetUrl = string.Empty;
    }

    internal static string Load(string projectRoot)
    {
        var path = PathIn(projectRoot);
        if (!File.Exists(path))
            return string.Empty;

        try
        {
            return JsonUtility.FromJson<Data>(File.ReadAllText(path))?.sheetUrl ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    internal static void Save(string projectRoot, string sheetUrl)
    {
        File.WriteAllText(PathIn(projectRoot), JsonUtility.ToJson(new Data { sheetUrl = sheetUrl }, true));
    }

    private static string PathIn(string projectRoot)
    {
        return Path.Combine(projectRoot, "ProjectSettings", "RippleGameLocalization.json");
    }
}
