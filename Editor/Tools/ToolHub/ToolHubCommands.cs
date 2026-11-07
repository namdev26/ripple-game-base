using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Runs commands from optional modules, which this assembly cannot reference directly.</summary>
internal static class ToolHubCommands
{
    internal static bool HasType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (assembly.GetType(fullName, false) != null) return true;
        return false;
    }

    internal static MethodInfo Find(string typeName, string methodName)
    {
        const BindingFlags anyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName, false);
            if (type != null)
                return type.GetMethod(methodName, anyStatic, null, Type.EmptyTypes, null);
        }

        return null;
    }

    internal static void Run(string typeName, string methodName)
    {
        var method = Find(typeName, methodName);
        if (method == null)
        {
            EditorUtility.DisplayDialog("TOOL Hub", "This command is unavailable. Check that its package is installed.", "OK");
            return;
        }

        try
        {
            method.Invoke(null, null);
        }
        catch (TargetInvocationException exception)
        {
            if (exception.InnerException is ExitGUIException) throw exception.InnerException;
            Debug.LogException(exception.InnerException);
        }
    }
}
