using UnityEngine;

namespace RippleGame.Localization
{
    /// <summary>Reads Resources/Localization.txt for English and Resources/Localization-Name.txt for the rest.</summary>
    public sealed class ResourcesLocalizationSource : ILocalizationSource
    {
        public string ReadOrNull(string language)
        {
            var path = language == Localizer.FallbackLanguage ? "Localization" : "Localization-" + language;
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
                return null;

            var content = asset.text;
            Resources.UnloadAsset(asset);
            return content;
        }
    }
}
