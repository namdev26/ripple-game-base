using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RippleGame.Sdk.Editor
{
    /// <summary>Fails a build when a catalog SDK sits in the manifest at a version this package was not built against.</summary>
    internal sealed class SdkPreBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return;

            string message;
            try
            {
                message = Check(SdkCatalog.LoadDefault(), File.ReadAllText(manifestPath), projectRoot);
            }
            catch (Exception e)
            {
                // A broken check must not block a build the user cannot fix from here.
                Debug.LogWarning($"[RIPPLE SDK] Skipped the SDK version check: {e.Message}");
                return;
            }

            if (message != null)
                throw new BuildFailedException(message);
        }

        /// <summary>The fix message for the first catalog entry in <see cref="SdkState.WrongVersion"/>, otherwise null.</summary>
        internal static string Check(SdkCatalog catalog, string manifestJson, string projectRoot)
        {
            foreach (var entry in catalog.Entries)
            {
                var status = SdkStatusResolver.Resolve(entry, manifestJson, projectRoot);
                if (status.State != SdkState.WrongVersion) continue;
                var name = string.IsNullOrEmpty(entry.DisplayName) ? entry.Id : entry.DisplayName;
                const string fix = "Open RIPPLE Game > Open Hub > SDK and press Fix.";
                var reference = status.InstalledReference ?? string.Empty;

                // A pinned tarball of another version names the version it holds; any other reference (registry semver,
                // git URL, another file path) is described as what it is.
                if (reference.StartsWith(SdkStatusResolver.TarballReferencePrefix, StringComparison.Ordinal))
                {
                    var installedFile = reference.Substring(SdkStatusResolver.TarballReferencePrefix.Length);
                    return $"SDK '{name}' is installed from the tarball {installedFile} but this RIPPLE Game Base version needs " +
                           $"{entry.FileName} ({entry.Version}). {fix}";
                }

                return $"SDK '{name}' is installed from '{reference}' instead of the pinned tarball {entry.FileName} ({entry.Version}). {fix}";
            }
            return null;
        }
    }
}
