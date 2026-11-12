using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;

namespace RippleGame.Sdk.Editor
{
    /// <summary>Editor-side model of <c>Editor/Sdk/SdkCatalog.json</c>.</summary>
    internal sealed class SdkCatalog
    {
        const string CatalogRelativePath = "Editor/Sdk/SdkCatalog.json";

        readonly List<SdkEntry> entries;

        SdkCatalog(List<SdkEntry> entries) => this.entries = entries;

        public IReadOnlyList<SdkEntry> Entries => entries;

        public static SdkCatalog LoadDefault()
        {
            var info = PackageInfo.FindForAssembly(typeof(SdkCatalog).Assembly);
            if (info == null)
                throw new InvalidOperationException("Cannot locate the ripple-game-base package for the SDK catalog.");
            return Parse(File.ReadAllText(Path.Combine(info.resolvedPath, CatalogRelativePath)));
        }

        public static SdkCatalog Parse(string json)
        {
            var root = JObject.Parse(json);
            var list = new List<SdkEntry>();
            foreach (var token in (JArray)root["entries"] ?? new JArray())
            {
                var unityDependencies = new Dictionary<string, string>();
                if (token["unityDependencies"] is JObject deps)
                    foreach (var p in deps.Properties())
                        unityDependencies[p.Name] = (string)p.Value;

                list.Add(new SdkEntry
                {
                    Id = (string)token["id"] ?? string.Empty,
                    DisplayName = (string)token["displayName"] ?? string.Empty,
                    PackageName = (string)token["packageName"] ?? string.Empty,
                    Version = (string)token["version"] ?? string.Empty,
                    FileName = (string)token["fileName"] ?? string.Empty,
                    Url = (string)token["url"] ?? string.Empty,
                    Sha256 = (string)token["sha256"] ?? string.Empty,
                    AsmdefName = (string)token["asmdefName"] ?? string.Empty,
                    DependsOn = Strings(token["dependsOn"]),
                    LegacyPaths = Strings(token["legacyPaths"]),
                    UnityDependencies = unityDependencies,
                });
            }
            return new SdkCatalog(list);
        }

        public SdkEntry Find(string id) => entries.FirstOrDefault(e => e.Id == id);

        /// <summary>Dependencies first, then the entry itself, no duplicates.</summary>
        public IReadOnlyList<SdkEntry> InstallOrder(string id)
        {
            var result = new List<SdkEntry>();
            Visit(id, new HashSet<string>(), result);
            return result;
        }

        /// <summary>Every entry that depends on <paramref name="id"/>, directly or indirectly.</summary>
        public IReadOnlyList<SdkEntry> DependentsOf(string id)
        {
            return entries
                .Where(e => e.Id != id && IsReachable(e.Id, id, new HashSet<string>()))
                .ToList();
        }

        bool IsReachable(string from, string target, HashSet<string> seen)
        {
            if (!seen.Add(from)) return false;
            var entry = Find(from);
            if (entry == null) return false;
            foreach (var dep in entry.DependsOn)
                if (dep == target || IsReachable(dep, target, seen)) return true;
            return false;
        }

        void Visit(string id, HashSet<string> path, List<SdkEntry> result)
        {
            var entry = Find(id) ?? throw new InvalidOperationException($"Unknown SDK id '{id}'.");
            if (result.Contains(entry)) return;
            if (!path.Add(id))
                throw new InvalidOperationException($"SDK dependency cycle through '{id}'.");
            foreach (var dep in entry.DependsOn)
                Visit(dep, path, result);
            path.Remove(id);
            result.Add(entry);
        }

        static IReadOnlyList<string> Strings(JToken token)
        {
            return token is JArray array
                ? array.Select(t => (string)t).ToList()
                : new List<string>();
        }
    }
}
