using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RippleGame.Sdk.Editor
{
    /// <summary>
    /// Checks that every asmdef <c>versionDefines</c> item naming a catalog package uses the exact pinned
    /// version (<c>"[" + Version + "]"</c>), so a define never silently survives an SDK bump.
    /// </summary>
    internal static class SdkVersionDefineAudit
    {
        public static IReadOnlyList<string> FindMismatches(SdkCatalog catalog, IEnumerable<KeyValuePair<string, string>> asmdefPathToJson)
        {
            var versionByPackage = new Dictionary<string, string>();
            foreach (var entry in catalog.Entries)
                versionByPackage[entry.PackageName] = entry.Version;

            var messages = new List<string>();
            foreach (var pair in asmdefPathToJson)
            {
                if (!(JObject.Parse(pair.Value)["versionDefines"] is JArray defines)) continue;
                foreach (var define in defines)
                {
                    var name = (string)define["name"];
                    if (name == null || !versionByPackage.TryGetValue(name, out var version)) continue;
                    var expression = (string)define["expression"];
                    var expected = "[" + version + "]";
                    if (expression != expected)
                        messages.Add($"{pair.Key}: versionDefines '{name}' expression '{expression}' must be '{expected}'.");
                }
            }
            return messages;
        }
    }
}
