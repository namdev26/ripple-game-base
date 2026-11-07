namespace RippleGame.Localization
{
    /// <summary>Where translation files come from. Returns null when a language has no file.</summary>
    public interface ILocalizationSource
    {
        string ReadOrNull(string language);
    }
}
