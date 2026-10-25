using System;
using UnityEditor;
using UnityEditor.Build;

[Serializable]
internal sealed class AndroidOptimizationSettings
{
    public ManagedStrippingLevel stripping;
    public bool stripEngineCode;
    public bool optimizeMeshData;

    internal void Read()
    {
        stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android);
        stripEngineCode = PlayerSettings.stripEngineCode;
        optimizeMeshData = PlayerSettings.stripUnusedMeshComponents;
    }

    internal void Recommended()
    {
        stripping = ManagedStrippingLevel.Low;
        stripEngineCode = optimizeMeshData = true;
    }

    internal void Draw()
    {
        stripping = (ManagedStrippingLevel)EditorGUILayout.EnumPopup("Managed Stripping Level", stripping);
        stripEngineCode = EditorGUILayout.Toggle("Strip Engine Code", stripEngineCode);
        optimizeMeshData = EditorGUILayout.Toggle("Optimize Mesh Data", optimizeMeshData);
    }

    internal void Apply()
    {
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, stripping);
        PlayerSettings.stripEngineCode = stripEngineCode;
        PlayerSettings.stripUnusedMeshComponents = optimizeMeshData;
    }
}
