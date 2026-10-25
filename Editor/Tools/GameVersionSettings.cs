using System;
using UnityEditor;

internal static class GameVersionSettings
{
    private const int MaximumAndroidVersionCode = 2100000000;

    internal static bool TryIncrementVersion(string version, out string nextVersion)
    {
        nextVersion = null;
        if (!Version.TryParse(version?.Trim(), out var parsed) || parsed.Revision >= 0 ||
            parsed.Build == int.MaxValue)
            return false;

        var patch = parsed.Build < 0 ? 1 : parsed.Build + 1;
        nextVersion = $"{parsed.Major}.{parsed.Minor}.{patch}";
        return true;
    }

    internal static bool TryValidate(string version, out string error)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            error = "Version cannot be empty. Example: 1.0.1";
            return false;
        }

        foreach (var character in version)
        {
            if (!char.IsControl(character)) continue;
            error = "Version cannot contain control characters or line breaks.";
            return false;
        }

        error = null;
        return true;
    }

    internal static void Apply(string version, bool buildAppBundle)
    {
        if (!TryValidate(version, out var error))
            throw new ArgumentException(error);

        var versionCode = GetVersionCodeForApply(PlayerSettings.bundleVersion, version,
            PlayerSettings.Android.bundleVersionCode);
        PlayerSettings.bundleVersion = version.Trim();
        PlayerSettings.Android.bundleVersionCode = versionCode;
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        AssetDatabase.SaveAssets();
    }

    internal static int GetVersionCodeForApply(string currentVersion, string nextVersion, int currentCode)
    {
        if (currentCode > MaximumAndroidVersionCode)
            throw new InvalidOperationException("Android version code exceeds Google Play's maximum.");
        if (string.Equals(currentVersion?.Trim(), nextVersion?.Trim(), StringComparison.Ordinal))
            return Math.Max(1, currentCode);
        if (currentCode == MaximumAndroidVersionCode)
            throw new InvalidOperationException("The project has reached Google Play's maximum Android version code.");
        return Math.Max(0, currentCode) + 1;
    }
}
