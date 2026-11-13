using System.Collections.Generic;

namespace RippleGame.Sdk.Editor
{
    /// <summary>One pinned third-party SDK from <c>SdkCatalog.json</c>.</summary>
    internal sealed class SdkEntry
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public string AsmdefName { get; set; } = string.Empty;
        public IReadOnlyList<string> DependsOn { get; set; } = new string[0];
        public IReadOnlyList<string> LegacyPaths { get; set; } = new string[0];
        public IReadOnlyDictionary<string, string> UnityDependencies { get; set; } = new Dictionary<string, string>();
    }
}
