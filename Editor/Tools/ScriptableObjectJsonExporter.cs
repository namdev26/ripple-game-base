#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RippleGame.RemoteConfig.Editor
{
    /// <summary>
    /// Exports the serialized data of a ScriptableObject as a remote-config JSON file.
    /// </summary>
    internal static class ScriptableObjectJsonExporter
    {
        private const string MenuPath = "Tools/Remote Config/Export Selected ScriptableObject as JSON";
        private const string OutputFolder = "Assets/RemoteConfig/Generated";

        [MenuItem(MenuPath, false, 100)]
        private static void ExportSelectedAsset()
        {
            if (!TryGetSelectedAsset(out var asset))
            {
                return;
            }

            Export(asset);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateExportSelectedAsset()
        {
            return TryGetSelectedAsset(out _);
        }

        [MenuItem("CONTEXT/ScriptableObject/Export as Remote Config JSON")]
        private static void ExportContext(MenuCommand command)
        {
            if (command.context is ScriptableObject asset)
            {
                Export(asset);
            }
        }

        private static bool TryGetSelectedAsset(out ScriptableObject asset)
        {
            asset = Selection.activeObject as ScriptableObject;
            if (asset == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset));
        }

        private static void Export(ScriptableObject asset)
        {
            var assetPath = BuildOutputAssetPath(asset);
            var absolutePath = ToAbsoluteProjectPath(assetPath);
            var json = JsonUtility.ToJson(asset, true);

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            Debug.Log($"Remote config JSON exported: {assetPath}", asset);
        }

        private static string BuildOutputAssetPath(ScriptableObject asset)
        {
            var fileName = SanitizeFileName(asset.name);
            return $"{OutputFolder}/{fileName}.json";
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var relativePath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, relativePath);
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "RemoteConfig";
            }

            var invalidCharacters = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Trim());
            foreach (var invalidCharacter in invalidCharacters)
            {
                builder.Replace(invalidCharacter, '_');
            }

            return builder.ToString();
        }
    }
}
#endif
