using System;
using UnityEditor;

internal static class ToolHubCommands
{
    internal static bool HasType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (assembly.GetType(fullName, false) != null) return true;
        return false;
    }

    internal static void Run(string menuPath)
    {
        if (!EditorApplication.ExecuteMenuItem(menuPath))
            EditorUtility.DisplayDialog("TOOL Hub", "This command is unavailable. Check that its package is installed.\n\n" + menuPath, "OK");
    }
}
