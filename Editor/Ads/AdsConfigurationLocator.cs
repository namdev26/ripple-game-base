using System.Linq;
using UnityEditor;

namespace RippleGame.Ads.Editor
{
    internal static class AdsConfigurationLocator
    {
        private const string CanonicalPath = "Assets/Settings/RippleGameAds/AdsConfiguration.asset";

        /// <summary>The canonical AdsConfiguration asset, or the only one in the project, or null.</summary>
        internal static AdsConfiguration Find()
        {
            var canonical = AssetDatabase.LoadAssetAtPath<AdsConfiguration>(CanonicalPath);
            if (canonical != null)
            {
                return canonical;
            }

            var matches = AssetDatabase.FindAssets("t:AdsConfiguration")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AdsConfiguration>)
                .Where(config => config != null)
                .ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
    }
}
