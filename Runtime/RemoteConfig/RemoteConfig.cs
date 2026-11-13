using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace RippleGame.RemoteConfig
{
    /// <summary>
    /// Typed access to server configuration. Values come from the registered source, then from the defaults in
    /// <see cref="RemoteConfigSettings"/>, then from the argument. Without an SDK there is no source and only the
    /// defaults are used. All members are for the main thread.
    /// </summary>
    public static class RemoteConfig
    {
        internal const string LogPrefix = "[RippleGame RemoteConfig] ";

        private static IRemoteConfigSource _source;
        private static RemoteConfigSettings _settings;
        private static bool _isReady;
        private static int _generation;
        private static TaskCompletionSource<bool> _readyGate = NewGate();
        private static readonly HashSet<string> WarnedKeys = new HashSet<string>();

        /// <summary>True once the first fetch ended: it succeeded, failed, timed out, or there is no source at all.</summary>
        public static bool IsReady => _isReady;

        /// <summary>Raised after a fetch succeeded and its values were activated. Never raised for a failure or a timeout.</summary>
        public static event Action Activated;

        /// <summary>Counts resets, so work that started before a reset can tell it is stale.</summary>
        internal static int Generation => _generation;

        internal static IRemoteConfigSource Source => _source;

        internal static RemoteConfigSettings Settings => _settings != null ? _settings : (_settings = RemoteConfigSettings.LoadOrDefault());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForTests();

        /// <summary>Called by an SDK-gated assembly to become the Remote Config backend.</summary>
        internal static void RegisterSource(IRemoteConfigSource source) => _source = source;

        internal static void SetSettings(RemoteConfigSettings settings) => _settings = settings;

        internal static void ResetForTests()
        {
            // Also keeps state clean when Domain Reload is disabled. Sources re-register at AfterAssembliesLoaded,
            // which runs after this.
            _generation++;
            _source = null;
            _settings = null;
            _isReady = false;
            _readyGate = NewGate();
            WarnedKeys.Clear();
            Activated = null;
            RemoteConfigBootstrap.ResetForTests();
            RemoteConfigNetworkWatcher.ResetForTests();
        }

        internal static void MarkReady()
        {
            if (_isReady) return;
            _isReady = true;
            _readyGate.TrySetResult(true);
        }

        internal static void RaiseActivated()
        {
            var handlers = Activated;
            if (handlers == null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception exception)
                {
                    Debug.LogError(LogPrefix + "Activated handler failed: " + exception);
                }
            }
        }

        /// <summary>Reads an int, long, float, double, bool or string. A value that does not parse is skipped with one warning per key.</summary>
        public static T Get<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

            for (var stage = 0; stage < 2; stage++)
            {
                if (!TryGetRaw(key, stage, out var raw)) continue;
                if (RemoteConfigValueParser.TryParse<T>(raw, out var value)) return value;
                WarnInvalid(key, typeof(T));
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads a JSON object into <typeparamref name="T"/>. Fields missing from the JSON keep the value the type
        /// gives them, unknown fields are ignored, and invalid JSON is skipped with one warning per key.
        /// </summary>
        public static T GetJson<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;

            for (var stage = 0; stage < 2; stage++)
            {
                if (!TryGetRaw(key, stage, out var raw)) continue;

                try
                {
                    var value = JsonConvert.DeserializeObject<T>(raw);
                    if (value != null) return value;
                }
                catch (Exception)
                {
                    WarnInvalid(key, typeof(T));
                }
            }

            return defaultValue;
        }

        /// <summary>Completes when Remote Config is ready or after <paramref name="timeoutSeconds"/>, whichever comes first. Never faults.</summary>
        public static async Task WaitUntilReadyAsync(float timeoutSeconds)
        {
            if (_isReady || !(timeoutSeconds > 0f)) return;
            await RemoteConfigBootstrap.CompletedBeforeAsync(_readyGate.Task, TimeSpan.FromSeconds(Math.Min(timeoutSeconds, 86400f)));
        }

        private static bool TryGetRaw(string key, int stage, out string raw)
        {
            raw = null;
            if (stage == 0)
            {
                var source = _source;
                if (source == null) return false;

                try
                {
                    return source.TryGetString(key, out raw) && raw != null;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(LogPrefix + "Reading key '" + key + "' failed: " + exception.Message);
                    raw = null;
                    return false;
                }
            }

            return Settings.TryGetDefault(key, out raw);
        }

        private static void WarnInvalid(string key, Type type)
        {
            if (!WarnedKeys.Add(key)) return;
            Debug.LogWarning(LogPrefix + "Key '" + key + "' is not a valid " + RemoteConfigValueParser.TypeName(type) + ". Using the default.");
        }

        private static TaskCompletionSource<bool> NewGate() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
