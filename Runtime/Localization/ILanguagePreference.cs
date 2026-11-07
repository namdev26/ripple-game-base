namespace RippleGame.Localization
{
    /// <summary>Remembers the language the player chose. Load returns null when nothing was chosen.</summary>
    public interface ILanguagePreference
    {
        string Load();
        void Save(string language);
    }
}
