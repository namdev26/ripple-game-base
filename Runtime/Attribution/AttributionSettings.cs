using UnityEngine;

namespace RippleGame.Attribution
{
    /// <summary>Optional settings asset. Place it at Resources/RippleGameAttribution/AttributionSettings.</summary>
    [CreateAssetMenu(fileName = "AttributionSettings", menuName = "RIPPLE Game/ATTRIBUTION/Attribution Settings")]
    public sealed class AttributionSettings : ScriptableObject
    {
        public const string ResourcesPath = "RippleGameAttribution/AttributionSettings";

        [Tooltip("AppsFlyer dev key. Attribution stays off while this is empty.")]
        [SerializeField] private string devKey = string.Empty;

        [Tooltip("App Store app id (numbers only). Needed on iOS.")]
        [SerializeField] private string iosAppId = string.Empty;

        [Tooltip("Turn on the attribution SDK's debug log. Leave off in release builds.")]
        [SerializeField] private bool debugLog;

        [Tooltip("Send ad impression revenue to the attribution provider.")]
        [SerializeField] private bool logAdRevenue = true;

        public string DevKey => devKey ?? string.Empty;
        public string IosAppId => iosAppId ?? string.Empty;
        public bool DebugLog => debugLog;
        public bool LogAdRevenue => logAdRevenue;

        internal static AttributionSettings LoadOrDefault()
        {
            var settings = Resources.Load<AttributionSettings>(ResourcesPath);
            return settings != null ? settings : CreateForTests(string.Empty, string.Empty, false, true);
        }

        internal static AttributionSettings CreateForTests(string devKey, string iosAppId, bool debugLog, bool logAdRevenue)
        {
            var settings = CreateInstance<AttributionSettings>();
            settings.hideFlags = HideFlags.DontSave;
            settings.devKey = devKey;
            settings.iosAppId = iosAppId;
            settings.debugLog = debugLog;
            settings.logAdRevenue = logAdRevenue;
            return settings;
        }
    }
}
