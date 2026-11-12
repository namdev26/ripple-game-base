using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RippleGame.Firebase;
using UnityEngine;
using FirebaseConfig = Firebase.RemoteConfig.FirebaseRemoteConfig;
using FirebaseValueSource = Firebase.RemoteConfig.ValueSource;

namespace RippleGame.RemoteConfig
{
    /// <summary>Firebase Remote Config backend. This is the only file that knows the Firebase Remote Config API.</summary>
    internal sealed class FirebaseRemoteConfigSource : IRemoteConfigSource
    {
        private IReadOnlyDictionary<string, string> _pendingDefaults;
        private bool _firebaseReady;
        private bool _defaultsApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register() => RemoteConfig.RegisterSource(new FirebaseRemoteConfigSource());

        public void SetDefaults(IReadOnlyDictionary<string, string> defaults) => _pendingDefaults = defaults;

        public async Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval)
        {
            try
            {
                var work = FetchCoreAsync(minimumInterval);
                var finished = await Task.WhenAny(work, Task.Delay(timeout));
                if (finished != work)
                {
                    // The fetch may still finish later and activate on its own; this call stops waiting for it.
                    _ = work.ContinueWith(task => _ = task.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    return false;
                }

                return await work;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Firebase fetch failed: " + exception.Message);
                return false;
            }
        }

        public bool TryGetString(string key, out string value)
        {
            value = null;
            if (!_firebaseReady) return false;

            try
            {
                var configValue = FirebaseConfig.DefaultInstance.GetValue(key);
                if (configValue.Source == FirebaseValueSource.StaticValue) return false;

                value = configValue.StringValue;
                return value != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Firebase read of '" + key + "' failed: " + exception.Message);
                return false;
            }
        }

        private async Task<bool> FetchCoreAsync(TimeSpan minimumInterval)
        {
            // FirebaseBootstrap already logged why Firebase is unavailable. It retries on the next call.
            if (!await FirebaseBootstrap.EnsureReadyAsync()) return false;

            var instance = FirebaseConfig.DefaultInstance;
            _firebaseReady = true;

            if (!_defaultsApplied)
            {
                if (_pendingDefaults != null && _pendingDefaults.Count > 0)
                {
                    var converted = new Dictionary<string, object>();
                    foreach (var pair in _pendingDefaults) converted[pair.Key] = pair.Value;
                    await instance.SetDefaultsAsync(converted);
                }

                _defaultsApplied = true;
            }

            await instance.FetchAsync(minimumInterval);
            await instance.ActivateAsync();
            return true;
        }
    }
}
