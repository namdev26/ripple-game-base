using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RippleGame.Sdk.Editor
{
    internal enum SdkState { NotInstalled, Installed, WrongVersion, Legacy }

    internal sealed class SdkStatusInfo
    {
        public SdkState State { get; set; }

        /// <summary>The manifest value for the entry's package, or null when it is not listed.</summary>
        public string InstalledReference { get; set; }

        /// <summary>Catalog legacy paths that exist under the project root.</summary>
        public IReadOnlyList<string> FoundLegacyPaths { get; set; } = new string[0];
    }

    internal static class SdkStatusResolver
    {
        /// <summary>Prefix of a tarball reference, relative to the <c>Packages</c> folder.</summary>
        public const string TarballReferencePrefix = "file:RippleSdks/";

        public static string ExpectedReference(SdkEntry entry) => TarballReferencePrefix + entry.FileName;

        public static SdkStatusInfo Resolve(SdkEntry entry, string manifestJson, string projectRoot)
        {
            var reference = SdkManifestEditor.GetDependency(manifestJson, entry.PackageName);
            var legacy = entry.LegacyPaths
                .Where(p => Directory.Exists(Path.Combine(projectRoot, p)))
                .ToList();

            SdkState state;
            if (legacy.Count > 0) state = SdkState.Legacy;
            else if (reference == null) state = SdkState.NotInstalled;
            else if (reference == ExpectedReference(entry)) state = SdkState.Installed;
            else state = SdkState.WrongVersion;

            return new SdkStatusInfo { State = state, InstalledReference = reference, FoundLegacyPaths = legacy };
        }
    }
}
