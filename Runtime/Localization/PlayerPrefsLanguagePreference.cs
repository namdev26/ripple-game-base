using UnityEngine;

namespace RippleGame.Localization
{
    public sealed class PlayerPrefsLanguagePreference : ILanguagePreference
    {
        private const string Key = "Language";

        public string Load() => PlayerPrefs.HasKey(Key) ? PlayerPrefs.GetString(Key) : null;

        public void Save(string language)
        {
            PlayerPrefs.SetString(Key, language);
            PlayerPrefs.Save();
        }
    }
}
