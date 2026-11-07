using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RippleGame.Localization
{
    /// <summary>The game's current language and its translations. Only one language is kept in memory.</summary>
    public static class Localizer
    {
        internal const string FallbackLanguage = "English";

        private static readonly string[] NoLanguages = new string[0];

        private static IReadOnlyList<string> languages = NoLanguages;
        private static ILocalizationSource source;
        private static ILanguagePreference preference;
        private static Dictionary<string, string> entries;

        public static event Action Changed;

        public static bool IsInitialized => entries != null;
        public static string Language { get; private set; }
        public static IReadOnlyList<string> Languages => languages;

        public static void Init(string[] supportedLanguages, LocalizationOptions options = null)
        {
            if (supportedLanguages == null || Array.IndexOf(supportedLanguages, FallbackLanguage) < 0)
                throw new ArgumentException("Supported languages must include " + FallbackLanguage + ".", nameof(supportedLanguages));

            var newSource = options?.Source ?? new ResourcesLocalizationSource();
            var fallback = newSource.ReadOrNull(FallbackLanguage);
            if (fallback == null)
                throw new InvalidOperationException("Localization.txt was not found. A game using Localizer must have it in Resources.");

            var newPreference = options?.Preference ?? new PlayerPrefsLanguagePreference();
            var device = options?.DeviceLanguage ?? Application.systemLanguage.ToString();

            source = newSource;
            preference = newPreference;
            languages = Array.AsReadOnly((string[])supportedLanguages.Clone());
            Language = FallbackLanguage;
            entries = LocalizationFile.Parse(fallback);

            var chosen = Choose(preference.Load(), device);
            if (chosen != FallbackLanguage)
                TryLoad(chosen);

            Changed?.Invoke();
        }

        public static string Get(string key)
        {
            EnsureInitialized();
            if (entries.TryGetValue(key, out var value))
                return value;

#if UNITY_EDITOR
            Debug.LogWarning("[Localization] Missing key '" + key + "' for language " + Language);
#endif
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            var template = Get(key);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                Debug.LogError("[Localization] Bad format for key '" + key + "' in " + Language + ": " + template);
                return template;
            }
        }

        public static void SetLanguage(string language)
        {
            EnsureInitialized();
            if (!Contains(language))
                throw new ArgumentException("Language is not supported: " + language, nameof(language));

            if (language == Language)
                return;

            var previous = Language;
            if (TryLoad(language))
                preference.Save(language);
            else if (previous != FallbackLanguage)
                TryLoad(FallbackLanguage);

            if (Language != previous)
                Changed?.Invoke();
        }

        internal static void Reset()
        {
            languages = NoLanguages;
            source = null;
            preference = null;
            entries = null;
            Language = null;
            Changed = null;
        }

        private static string Choose(string saved, string device)
        {
            if (Contains(saved))
                return saved;
            if (Contains(device))
                return device;
            if ((device == "ChineseSimplified" || device == "ChineseTraditional") && Contains("Chinese"))
                return "Chinese";

            return FallbackLanguage;
        }

        private static bool Contains(string language)
        {
            for (var i = 0; i < languages.Count; i++)
                if (languages[i] == language)
                    return true;

            return false;
        }

        private static bool TryLoad(string language)
        {
            var content = source.ReadOrNull(language);
            if (content == null)
            {
                Debug.LogWarning("[Localization] No translation file for " + language + ". Using " + FallbackLanguage + ".");
                return false;
            }

            entries = LocalizationFile.Parse(content);
            Language = language;
            return true;
        }

        private static void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Call Localizer.Init before using it.");
        }
    }
}
