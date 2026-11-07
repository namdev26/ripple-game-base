using TMPro;
using UnityEngine;

namespace RippleGame.Localization.UI
{
    /// <summary>Shows the translation of a key on this TextMeshPro text and follows language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                if (isActiveAndEnabled)
                    Refresh();
            }
        }

        private void OnEnable()
        {
            Localizer.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => Localizer.Changed -= Refresh;

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(key) && Localizer.IsInitialized)
                GetComponent<TMP_Text>().text = Localizer.Get(key);
        }
    }
}
