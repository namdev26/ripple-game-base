using System;
using UnityEditor;

internal static class AndroidSerializedSettings
{
    internal static SerializedObject Open() =>
        new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));

    internal static SerializedProperty Require(SerializedObject settings, string name)
    {
        return settings.FindProperty(name) ??
               throw new NotSupportedException($"This Unity version does not expose Player Settings property '{name}'.");
    }
}
