using System;
using UnityEngine;

namespace RippleGame.RemoteConfig
{
    /// <summary>
    /// Hidden object that waits for the network to come back after a fetch that did not succeed. It calls back once
    /// each time reachability goes from NotReachable to reachable, and is destroyed after a later fetch succeeds.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class RemoteConfigNetworkWatcher : MonoBehaviour
    {
        private const float PollIntervalSeconds = 1f;

        /// <summary>Replaced by tests to simulate losing and regaining the network.</summary>
        internal static Func<NetworkReachability> ReachabilityProvider = () => Application.internetReachability;

        private static RemoteConfigNetworkWatcher _instance;

        private Action _onRestored;
        private bool _wasReachable;
        private float _nextPollTime;

        internal static RemoteConfigNetworkWatcher Instance => _instance;

        internal static bool Exists => _instance != null;

        /// <summary>Creates the watcher, or reuses the existing one and replaces its callback.</summary>
        internal static RemoteConfigNetworkWatcher Create(Action onRestored)
        {
            if (_instance != null)
            {
                _instance._onRestored = onRestored;
                return _instance;
            }

            var host = new GameObject("RippleGame RemoteConfig Network Watcher") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            var watcher = host.AddComponent<RemoteConfigNetworkWatcher>();
            watcher._onRestored = onRestored;
            watcher._wasReachable = IsReachable();
            _instance = watcher;
            return watcher;
        }

        internal static void DestroyInstance()
        {
            var watcher = _instance;
            _instance = null;
            if (watcher == null) return;

            if (Application.isPlaying) Destroy(watcher.gameObject);
            else DestroyImmediate(watcher.gameObject);
        }

        internal static void ResetForTests() => DestroyInstance();

        /// <summary>Checks reachability once and calls back when it just returned.</summary>
        internal void Poll()
        {
            var reachable = IsReachable();
            var restored = reachable && !_wasReachable;
            _wasReachable = reachable;
            if (!restored) return;

            try
            {
                _onRestored?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError(RemoteConfig.LogPrefix + "Reconnect retry failed: " + exception);
            }
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            if (now < _nextPollTime) return;
            _nextPollTime = now + PollIntervalSeconds;
            Poll();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

#if UNITY_EDITOR
        // A HideAndDontSave object would otherwise outlive Play Mode.
        private void OnApplicationQuit()
        {
            if (_instance == this) _instance = null;
            DestroyImmediate(gameObject);
        }
#endif

        private static bool IsReachable() => ReachabilityProvider() != NetworkReachability.NotReachable;
    }
}
