using System;
using UnityEngine;

namespace RippleGame.Attribution
{
    /// <summary>
    /// Install attribution: whether the user came from a campaign. The status is saved the first time a provider
    /// reports it and read back on the next launch. Without a provider the status stays Unknown. All members are for
    /// the main thread.
    /// </summary>
    public static class Attribution
    {
        internal const string LogPrefix = "[RippleGame Attribution] ";
        internal const string StatusKey = "ripplegame.attribution.status";
        internal const string MediaSourceKey = "ripplegame.attribution.media_source";
        internal const string CampaignKey = "ripplegame.attribution.campaign";

        private static IAttributionService _service;
        private static AttributionSettings _settings;
        private static AttributionStatus _status;
        private static string _mediaSource;
        private static string _campaign;

        public static AttributionStatus Status => _status;
        public static string MediaSource => _mediaSource;
        public static string Campaign => _campaign;

        /// <summary>Raised when <see cref="Status"/> changes, including when the saved value loads at startup.</summary>
        public static event Action<AttributionStatus> StatusChanged;

        internal static IAttributionService Service => _service;

        internal static AttributionSettings Settings => _settings != null ? _settings : (_settings = AttributionSettings.LoadOrDefault());

        /// <summary>Sends one impression's revenue to the provider. Does nothing without a provider or when LogAdRevenue is off.</summary>
        public static void LogAdRevenue(AttributionAdRevenue revenue)
        {
            var service = _service;
            if (service == null || !Settings.LogAdRevenue) return;

            try
            {
                service.LogAdRevenue(revenue);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Logging ad revenue failed: " + exception.Message);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForTests();

        /// <summary>Called by an SDK-gated assembly to become the attribution provider.</summary>
        internal static void RegisterService(IAttributionService service) => _service = service;

        internal static void SetSettings(AttributionSettings settings) => _settings = settings;

        internal static void ResetForTests()
        {
            // Also keeps state clean when Domain Reload is disabled. Providers re-register at AfterAssembliesLoaded,
            // which runs after this. Saved values are not touched.
            _service = null;
            _settings = null;
            _status = AttributionStatus.Unknown;
            _mediaSource = null;
            _campaign = null;
            StatusChanged = null;
        }

        /// <summary>Reads the saved status. Raises <see cref="StatusChanged"/> when it is not Unknown.</summary>
        internal static void LoadPersisted()
        {
            try
            {
                if (!PlayerPrefs.HasKey(StatusKey)) return;

                var status = (AttributionStatus)PlayerPrefs.GetInt(StatusKey, (int)AttributionStatus.Unknown);
                if (status != AttributionStatus.Organic && status != AttributionStatus.NonOrganic) return;

                Apply(status, PlayerPrefs.GetString(MediaSourceKey, null), PlayerPrefs.GetString(CampaignKey, null));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Could not read the saved attribution: " + exception.Message);
            }
        }

        /// <summary>
        /// Takes a result from the provider. An Unknown result is ignored, so it never overwrites a known status. A known
        /// result is saved, and <see cref="StatusChanged"/> is raised only when the status differs from the current one.
        /// </summary>
        internal static void ReportConversion(AttributionStatus status, string mediaSource, string campaign)
        {
            if (status == AttributionStatus.Unknown) return;

            Persist(status, mediaSource, campaign);
            Apply(status, mediaSource, campaign);
        }

        private static void Apply(AttributionStatus status, string mediaSource, string campaign)
        {
            var changed = status != _status;
            _status = status;
            _mediaSource = mediaSource;
            _campaign = campaign;
            if (!changed) return;

            var handlers = StatusChanged;
            if (handlers == null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<AttributionStatus>)handler)(status);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void Persist(AttributionStatus status, string mediaSource, string campaign)
        {
            try
            {
                PlayerPrefs.SetInt(StatusKey, (int)status);
                SetOrDelete(MediaSourceKey, mediaSource);
                SetOrDelete(CampaignKey, campaign);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Could not save the attribution: " + exception.Message);
            }
        }

        private static void SetOrDelete(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(key);
            else PlayerPrefs.SetString(key, value);
        }
    }
}
