using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>
    /// Kept in Resources so the reusable bootstrapper can find the project-specific configuration.
    /// The referenced AdsConfiguration remains in Assets/Settings/RippleGameAds.
    /// </summary>
    public sealed class AdsConfigurationReference : ScriptableObject
    {
        [SerializeField] private AdsConfiguration configuration;

        public AdsConfiguration Configuration => configuration;

#if UNITY_EDITOR
        internal void SetConfiguration(AdsConfiguration value) => configuration = value;
#endif
    }
}
