using RippleGame.Localization;
using UnityEngine;

namespace RippleGame.Base.Samples
{
    /// <summary>Text with numbers from code. For fixed text, add LocalizedText to the TextMeshPro object instead.</summary>
    public sealed class LocalizationExample : MonoBehaviour
    {
        private int coins = 120;

        private void Awake()
        {
            // A real game calls Init once at startup. Declare only the languages your fonts can show.
            if (!Localizer.IsInitialized)
                Localizer.Init(new[] { "English", "Vietnamese" });
        }

        private void OnEnable()
        {
            Localizer.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => Localizer.Changed -= Refresh;

        // Connect this to a language button.
        public void UseVietnamese() => Localizer.SetLanguage("Vietnamese");

        // Show this on your own label. With TextMeshPro: coinLabel.text = ...
        private void Refresh() => Debug.Log(Localizer.Format("s_coin_value", coins));
    }
}
