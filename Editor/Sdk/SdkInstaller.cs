using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RippleGame.Sdk.Editor
{
    internal sealed class SdkOperationResult
    {
        public bool Success { get; }
        public string Message { get; }

        SdkOperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static SdkOperationResult Ok(string message) => new SdkOperationResult(true, message);
        public static SdkOperationResult Fail(string message) => new SdkOperationResult(false, message);
    }

    /// <summary>
    /// Installs and removes the pinned SDK tarballs listed in <see cref="SdkCatalog"/> by copying them into
    /// <c>Packages/RippleSdks</c> and pointing <c>Packages/manifest.json</c> at them. It never calls
    /// <c>Client.Resolve()</c>; the caller does that after a successful operation.
    /// </summary>
    internal sealed class SdkInstaller
    {
        static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        readonly SdkCatalog catalog;
        readonly string projectRoot;
        readonly ISdkDownloader downloader;

        public SdkInstaller(SdkCatalog catalog, string projectRoot, ISdkDownloader downloader)
        {
            this.catalog = catalog;
            this.projectRoot = projectRoot;
            this.downloader = downloader;
        }

        string ManifestPath => Path.Combine(projectRoot, "Packages", "manifest.json");
        string SdkFolder => Path.Combine(projectRoot, "Packages", "RippleSdks");

        /// <summary>
        /// Installs <paramref name="id"/> and its dependencies (dependencies first). Everything is downloaded and
        /// verified before any file is committed or the manifest is written, so a failure leaves the manifest
        /// byte-identical and no <c>.part</c> or half-installed tarball behind.
        /// </summary>
        public async Task<SdkOperationResult> InstallAsync(string id)
        {
            var partFiles = new List<string>();
            var committedFiles = new List<string>();
            var createdFolder = false;
            try
            {
                if (!File.Exists(ManifestPath))
                    return SdkOperationResult.Fail("Packages/manifest.json was not found in the project.");
                if (catalog.Find(id) == null)
                    return SdkOperationResult.Fail($"Unknown SDK id '{id}'.");

                var original = File.ReadAllText(ManifestPath);
                var order = catalog.InstallOrder(id);

                var toInstall = new List<SdkEntry>();
                var staleTarballs = new List<string>();
                foreach (var entry in order)
                {
                    var status = SdkStatusResolver.Resolve(entry, original, projectRoot);
                    if (status.State == SdkState.Legacy)
                        return SdkOperationResult.Fail(
                            $"{Label(entry)} has an old .unitypackage copy in Assets ({string.Join(", ", status.FoundLegacyPaths)}). " +
                            "Delete that plugin's old files by hand first, then install again.");
                    if (status.State == SdkState.Installed) continue;
                    toInstall.Add(entry);
                    if (status.State == SdkState.WrongVersion)
                    {
                        var oldFile = TarballFileName(status.InstalledReference);
                        if (oldFile != null && oldFile != entry.FileName) staleTarballs.Add(oldFile);
                    }
                }

                // 1. Download and verify everything into .part files.
                if (toInstall.Count > 0)
                {
                    createdFolder = !Directory.Exists(SdkFolder);
                    Directory.CreateDirectory(SdkFolder);
                }
                foreach (var entry in toInstall)
                {
                    var part = Path.Combine(SdkFolder, entry.FileName + ".part");
                    File.Delete(part);
                    partFiles.Add(part);
                    await downloader.DownloadAsync(entry.Url, part);
                    if (!File.Exists(part))
                        throw new IOException($"The downloader did not produce a file for {entry.Url}.");
                    var actual = SdkHash.Sha256Hex(part);
                    if (!string.Equals(actual, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        Cleanup(partFiles, committedFiles, createdFolder);
                        return SdkOperationResult.Fail(
                            $"SHA-256 mismatch for {Label(entry)}: expected {entry.Sha256}, got {actual}. The download was discarded.");
                    }
                }

                // 2. Compute the new manifest in memory (including the Unity packages the SDKs need).
                var manifest = original;
                foreach (var entry in toInstall)
                    manifest = SdkManifestEditor.SetDependency(manifest, entry.PackageName, SdkStatusResolver.ExpectedReference(entry));
                foreach (var entry in order)
                    foreach (var unityDependency in entry.UnityDependencies)
                        if (SdkManifestEditor.GetDependency(manifest, unityDependency.Key) == null)
                            manifest = SdkManifestEditor.SetDependency(manifest, unityDependency.Key, unityDependency.Value);

                if (toInstall.Count == 0 && manifest == original)
                    return SdkOperationResult.Ok($"{Label(catalog.Find(id))} is already installed.");

                // 3. Commit: move tarballs into place, then write the manifest once.
                foreach (var part in partFiles.ToList())
                {
                    var final = part.Substring(0, part.Length - ".part".Length);
                    File.Delete(final);
                    File.Move(part, final);
                    partFiles.Remove(part);
                    committedFiles.Add(final);
                }
                if (manifest != original)
                    File.WriteAllText(ManifestPath, manifest, Utf8NoBom);

                // 4. The manifest no longer points at replaced tarballs; delete them.
                foreach (var stale in staleTarballs.Distinct())
                    TryDelete(Path.Combine(SdkFolder, stale));

                return SdkOperationResult.Ok(toInstall.Count == 0
                    ? $"{Label(catalog.Find(id))} is already installed."
                    : "Installed " + string.Join(", ", toInstall.Select(e => $"{Label(e)} {e.Version}")) + ".");
            }
            catch (Exception e)
            {
                Cleanup(partFiles, committedFiles, createdFolder);
                return SdkOperationResult.Fail($"Install of '{id}' failed: {e.Message}");
            }
        }

        /// <summary>Removes <paramref name="id"/> from the manifest and deletes its tarball. Unity packages stay.</summary>
        public Task<SdkOperationResult> RemoveAsync(string id)
        {
            try
            {
                var entry = catalog.Find(id);
                if (entry == null)
                    return Task.FromResult(SdkOperationResult.Fail($"Unknown SDK id '{id}'."));
                if (!File.Exists(ManifestPath))
                    return Task.FromResult(SdkOperationResult.Fail("Packages/manifest.json was not found in the project."));

                var original = File.ReadAllText(ManifestPath);
                var blockers = catalog.DependentsOf(id)
                    .Where(d => SdkManifestEditor.GetDependency(original, d.PackageName) != null)
                    .ToList();
                if (blockers.Count > 0)
                    return Task.FromResult(SdkOperationResult.Fail(
                        $"Cannot remove {Label(entry)}: still required by installed {string.Join(", ", blockers.Select(Label))}. Remove those first."));

                var reference = SdkManifestEditor.GetDependency(original, entry.PackageName);
                if (reference == null)
                    return Task.FromResult(SdkOperationResult.Ok($"{Label(entry)} is not installed."));

                File.WriteAllText(ManifestPath, SdkManifestEditor.RemoveDependency(original, entry.PackageName), Utf8NoBom);

                var tarball = TarballFileName(reference);
                if (tarball != null) TryDelete(Path.Combine(SdkFolder, tarball));
                return Task.FromResult(SdkOperationResult.Ok($"Removed {Label(entry)}."));
            }
            catch (Exception e)
            {
                return Task.FromResult(SdkOperationResult.Fail($"Remove of '{id}' failed: {e.Message}"));
            }
        }

        static string Label(SdkEntry entry) =>
            string.IsNullOrEmpty(entry.DisplayName) ? entry.Id : $"{entry.DisplayName} ({entry.Id})";

        /// <summary>The tarball file name inside <c>Packages/RippleSdks</c>, or null if the reference is not one of ours.</summary>
        static string TarballFileName(string reference)
        {
            if (reference == null || !reference.StartsWith(SdkStatusResolver.TarballReferencePrefix, StringComparison.Ordinal))
                return null;
            var name = Path.GetFileName(reference.Substring(SdkStatusResolver.TarballReferencePrefix.Length));
            return string.IsNullOrEmpty(name) ? null : name;
        }

        void Cleanup(List<string> partFiles, List<string> committedFiles, bool createdFolder)
        {
            foreach (var file in partFiles.Concat(committedFiles))
                TryDelete(file);
            partFiles.Clear();
            committedFiles.Clear();
            if (createdFolder)
            {
                try
                {
                    if (Directory.Exists(SdkFolder) && !Directory.EnumerateFileSystemEntries(SdkFolder).Any())
                        Directory.Delete(SdkFolder);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
