using UnityEngine;

namespace RippleGame.Analytics
{
    /// <summary>Optional settings asset. Place it at Resources/RippleGameAnalytics/AnalyticsSettings.</summary>
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "TOOL/ANALYTICS/Analytics Settings")]
    public sealed class AnalyticsSettings : ScriptableObject
    {
        public const string ResourcesPath = "RippleGameAnalytics/AnalyticsSettings";

        [Tooltip("Print every event to the Console.")]
        [SerializeField] private bool enableConsoleLog = true;

        [Tooltip("Log ad_impression with revenue. Turn off if AdMob is linked to Firebase, otherwise revenue is counted twice.")]
        [SerializeField] private bool logAdRevenue = true;

        [Tooltip("Firebase collection state applied after init. This alone does not block data sent at app start (first_open, session_start); for consent gating also set firebase_analytics_collection_enabled=false in AndroidManifest / FIREBASE_ANALYTICS_COLLECTION_ENABLED=NO in Info.plist. See Setup.md.")]
        [SerializeField] private bool collectionEnabled = true;

        public bool EnableConsoleLog => enableConsoleLog;
        public bool LogAdRevenue => logAdRevenue;
        public bool CollectionEnabled => collectionEnabled;

        internal static AnalyticsSettings LoadOrDefault()
        {
            var settings = Resources.Load<AnalyticsSettings>(ResourcesPath);
            return settings != null ? settings : CreateDefault();
        }

        internal static AnalyticsSettings CreateDefault() => CreateForTests(true, true, true);

        internal static AnalyticsSettings CreateForTests(bool consoleLog, bool adRevenue, bool collection)
        {
            var settings = CreateInstance<AnalyticsSettings>();
            settings.hideFlags = HideFlags.DontSave;
            settings.enableConsoleLog = consoleLog;
            settings.logAdRevenue = adRevenue;
            settings.collectionEnabled = collection;
            return settings;
        }
    }
}
