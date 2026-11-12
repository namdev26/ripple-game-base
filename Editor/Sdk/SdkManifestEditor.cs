using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RippleGame.Sdk.Editor
{
    /// <summary>
    /// Edits the <c>dependencies</c> object of <c>Packages/manifest.json</c> as text-in / text-out. Every other
    /// top-level key keeps its content and position; output is indented with 2 spaces and "\n" line endings.
    /// </summary>
    internal static class SdkManifestEditor
    {
        const string DependenciesKey = "dependencies";

        public static string SetDependency(string manifestJson, string packageName, string reference)
        {
            var root = Parse(manifestJson);
            var dependencies = Dependencies(root, create: true);
            dependencies[packageName] = reference;
            return Serialize(root);
        }

        public static string RemoveDependency(string manifestJson, string packageName)
        {
            var root = Parse(manifestJson);
            var dependencies = Dependencies(root, create: false);
            if (dependencies == null || !dependencies.Remove(packageName))
                return manifestJson;
            return Serialize(root);
        }

        /// <summary>The dependency value, or null when the package is not listed.</summary>
        public static string GetDependency(string manifestJson, string packageName)
        {
            var dependencies = Dependencies(Parse(manifestJson), create: false);
            return dependencies != null && dependencies.TryGetValue(packageName, out var token) ? (string)token : null;
        }

        static JObject Dependencies(JObject root, bool create)
        {
            var token = root[DependenciesKey];
            if (token == null || token.Type == JTokenType.Null)
            {
                if (!create) return null;
                var created = new JObject();
                root[DependenciesKey] = created;
                return created;
            }
            if (token is JObject dependencies) return dependencies;
            throw new InvalidOperationException("manifest.json: \"dependencies\" is not an object.");
        }

        static JObject Parse(string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader);
        }

        static string Serialize(JObject root)
        {
            var text = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.Indented, Indentation = 2, IndentChar = ' ' })
                root.WriteTo(writer);
            return text.ToString();
        }
    }
}
