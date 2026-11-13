using System;
using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.RemoteConfig
{
    /// <summary>One in-app default value. The value is stored as text and parsed on read.</summary>
    [Serializable]
    public sealed class RemoteConfigDefault
    {
        public string Key;
        public string Value;

        public RemoteConfigDefault() { }

        public RemoteConfigDefault(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>Optional settings asset. Place it at Resources/RippleGameRemoteConfig/RemoteConfigSettings.</summary>
    [CreateAssetMenu(fileName = "RemoteConfigSettings", menuName = "RIPPLE Game/REMOTE CONFIG/Remote Config Settings")]
    public sealed class RemoteConfigSettings : ScriptableObject
    {
        public const string ResourcesPath = "RippleGameRemoteConfig/RemoteConfigSettings";
        public const float DefaultFetchTimeoutSeconds = 5f;
        public const int DefaultMinimumFetchIntervalSeconds = 3600;

        [Tooltip("Longest time the game waits for a fetch. After it the game continues with the last activated or default values.")]
        [SerializeField, Min(0f)] private float fetchTimeoutSeconds = DefaultFetchTimeoutSeconds;

        [Tooltip("Cache lifetime of fetched values. Builds with the ADS_TEST symbol (Ads Test profile) always use 0.")]
        [SerializeField, Min(0)] private int minimumFetchIntervalSeconds = DefaultMinimumFetchIntervalSeconds;

        [Tooltip("In-app values used when the server has no value for a key.")]
        [SerializeField] private List<RemoteConfigDefault> defaults = new List<RemoteConfigDefault>();

        public float FetchTimeoutSeconds => Mathf.Max(0f, fetchTimeoutSeconds);

        public int MinimumFetchIntervalSeconds
        {
            get
            {
#if ADS_TEST
                return 0;
#else
                return Mathf.Max(0, minimumFetchIntervalSeconds);
#endif
            }
        }

        public List<RemoteConfigDefault> Defaults => defaults ?? (defaults = new List<RemoteConfigDefault>());

        internal static RemoteConfigSettings LoadOrDefault()
        {
            var settings = Resources.Load<RemoteConfigSettings>(ResourcesPath);
            return settings != null ? settings : CreateForTests(DefaultFetchTimeoutSeconds, DefaultMinimumFetchIntervalSeconds);
        }

        internal static RemoteConfigSettings CreateForTests(float timeoutSeconds, int minimumIntervalSeconds, params RemoteConfigDefault[] values)
        {
            var settings = CreateInstance<RemoteConfigSettings>();
            settings.hideFlags = HideFlags.DontSave;
            settings.fetchTimeoutSeconds = timeoutSeconds;
            settings.minimumFetchIntervalSeconds = minimumIntervalSeconds;
            settings.defaults = new List<RemoteConfigDefault>(values);
            return settings;
        }

        /// <summary>Finds the first default with this key.</summary>
        internal bool TryGetDefault(string key, out string value)
        {
            foreach (var item in Defaults)
            {
                if (item != null && item.Key == key && item.Value != null)
                {
                    value = item.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>Builds the key/value map handed to the source. Empty keys are skipped and the first duplicate wins.</summary>
        internal Dictionary<string, string> ToDictionary()
        {
            var map = new Dictionary<string, string>();
            foreach (var item in Defaults)
            {
                if (item == null || string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
                if (!map.ContainsKey(item.Key)) map.Add(item.Key, item.Value);
            }

            return map;
        }
    }
}
