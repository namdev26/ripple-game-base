using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RippleGame.RemoteConfig
{
    /// <summary>
    /// Starts Remote Config before the first scene loads. Remote Config always ends up ready: after a successful
    /// fetch, after a failure, after a timeout, or at once when no source is registered. Activated is raised only
    /// for a successful fetch.
    /// </summary>
    internal static class RemoteConfigBootstrap
    {
        private static bool _retrying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => _ = Run(RemoteConfigSettings.LoadOrDefault());

        internal static void ResetForTests() => _retrying = false;

        /// <summary>Runs the startup. The returned task completes when the first attempt ended and never faults.</summary>
        internal static Task Run(RemoteConfigSettings settings)
        {
            settings = settings != null ? settings : RemoteConfigSettings.LoadOrDefault();
            RemoteConfig.SetSettings(settings);

            var source = RemoteConfig.Source;
            if (source == null)
            {
                RemoteConfig.MarkReady();
                return Task.CompletedTask;
            }

            return FirstFetchAsync(source, settings, RemoteConfig.Generation);
        }

        private static async Task FirstFetchAsync(IRemoteConfigSource source, RemoteConfigSettings settings, int generation)
        {
            var success = false;
            try
            {
                ApplyDefaults(source, settings);
                success = await AttemptAsync(source, settings, generation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Startup fetch failed: " + exception.Message);
            }

            if (generation != RemoteConfig.Generation) return;

            RemoteConfig.MarkReady();
            if (success)
            {
                RemoteConfig.RaiseActivated();
                return;
            }

            try
            {
                RemoteConfigNetworkWatcher.Create(() => _ = RetryAsync(source, settings, generation));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Could not watch the network for a retry: " + exception.Message);
            }
        }

        private static async Task RetryAsync(IRemoteConfigSource source, RemoteConfigSettings settings, int generation)
        {
            if (_retrying || generation != RemoteConfig.Generation) return;

            _retrying = true;
            try
            {
                var success = await AttemptAsync(source, settings, generation);
                if (!success || generation != RemoteConfig.Generation) return;

                RemoteConfigNetworkWatcher.DestroyInstance();
                RemoteConfig.RaiseActivated();
            }
            finally
            {
                if (generation == RemoteConfig.Generation) _retrying = false;
            }
        }

        private static void ApplyDefaults(IRemoteConfigSource source, RemoteConfigSettings settings)
        {
            var defaults = settings.ToDictionary();
            if (defaults.Count == 0) return;

            try
            {
                source.SetDefaults(defaults);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Setting defaults on the source failed: " + exception.Message);
            }
        }

        /// <summary>One fetch bounded by the settings timeout. True only when the source reports a successful fetch.</summary>
        private static async Task<bool> AttemptAsync(IRemoteConfigSource source, RemoteConfigSettings settings, int generation)
        {
            var timeout = TimeSpan.FromSeconds(settings.FetchTimeoutSeconds);
            var interval = TimeSpan.FromSeconds(settings.MinimumFetchIntervalSeconds);

            try
            {
                var fetch = source.FetchAndActivateAsync(timeout, interval);
                if (fetch == null) return false;

                if (!await CompletedBeforeAsync(fetch, timeout))
                {
                    Debug.LogWarning(RemoteConfig.LogPrefix + "Fetch timed out. Using the last activated or default values.");
                    _ = ObserveLateFetchAsync(fetch, generation);
                    return false;
                }

                return await fetch;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "Fetch failed: " + exception.Message + " Using the last activated or default values.");
                return false;
            }
        }

        /// <summary>
        /// Keeps watching a fetch that missed its timeout. If it still succeeds, its values are live, so the watcher is
        /// no longer needed and Activated is raised once. A late failure only logs. Runs on the main thread because it
        /// awaits with the captured Unity context.
        /// </summary>
        private static async Task ObserveLateFetchAsync(Task<bool> fetch, int generation)
        {
            try
            {
                var success = await fetch;
                if (generation != RemoteConfig.Generation) return;

                if (!success)
                {
                    Debug.LogWarning(RemoteConfig.LogPrefix + "A fetch that missed its timeout also failed.");
                    return;
                }

                RemoteConfigNetworkWatcher.DestroyInstance();
                RemoteConfig.RaiseActivated();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(RemoteConfig.LogPrefix + "A fetch that missed its timeout also failed: " + exception.Message);
            }
        }

        /// <summary>True when <paramref name="task"/> finished before <paramref name="timeout"/> elapsed.</summary>
        internal static async Task<bool> CompletedBeforeAsync(Task task, TimeSpan timeout)
        {
            if (task.IsCompleted) return true;

            using (var cancel = new CancellationTokenSource())
            {
                var delay = Task.Delay(timeout, cancel.Token);
                var first = await Task.WhenAny(task, delay);
                cancel.Cancel();
                return first == task;
            }
        }
    }
}
