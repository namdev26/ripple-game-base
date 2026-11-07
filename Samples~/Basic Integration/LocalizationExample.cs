using RippleGame.Localization;
using TMPro;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    /// <summary>Text with numbers from code. For fixed text, add LocalizedText to the TextMeshPro object instead.</summary>
    public sealed class LocalizationExample : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinLabel;
        private int coins = 120;

        // Declare only the languages your fonts can show. English is required.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitLanguages() => Localizer.Init(new[] { "English", "Vietnamese" });

        private void OnEnable()
        {
            Localizer.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => Localizer.Changed -= Refresh;

        // Connect this to a language button.
        public void UseVietnamese() => Localizer.SetLanguage("Vietnamese");

        private void Refresh() => coinLabel.text = Localizer.Format("s_coin_value", coins);
    }
}
