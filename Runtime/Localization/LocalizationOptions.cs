namespace RippleGame.Localization
{
    public sealed class LocalizationOptions
    {
        /// <summary>Defaults to files in Resources.</summary>
        public ILocalizationSource Source;

        /// <summary>Defaults to the PlayerPrefs key "Language".</summary>
        public ILanguagePreference Preference;

        /// <summary>Defaults to Application.systemLanguage.</summary>
        public string DeviceLanguage;
    }
}
