using System;
using UnityEngine;

namespace RippleGame.Analytics
{
    /// <summary>Starts analytics before the first scene loads. No scene object is needed.</summary>
    internal static class AnalyticsBootstrap
    {
        private static Action<AnalyticsSettings> _backend;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Keeps state clean when Domain Reload is disabled in Enter Play Mode Options.
            // Backends re-register at AfterAssembliesLoaded, which runs after this.
            _backend = null;
            Analytics.ResetForTests();
            GameEvents.ResetForTests();
        }

        /// <summary>Called by an SDK-gated assembly (e.g. RippleGame.Analytics.Firebase) to become the analytics backend.</summary>
        internal static void RegisterBackend(Action<AnalyticsSettings> initializer) => _backend = initializer;

        internal static void ResetBackendForTests() => _backend = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // The Editor always uses the Console service, even when a backend SDK is installed.
            Initialize(AnalyticsSettings.LoadOrDefault(), !Application.isEditor);
        }

        /// <summary>Uses the registered backend when allowed; any synchronous failure falls back to the Console service.</summary>
        internal static void Initialize(AnalyticsSettings settings, bool allowBackend)
        {
            Analytics.SetSettings(settings);

            var backend = allowBackend ? _backend : null;
            if (backend == null)
            {
                StartConsoleService(settings);
                return;
            }

            try
            {
                backend(settings);
            }
            catch (Exception exception)
            {
                Debug.LogError(Analytics.LogPrefix + "Analytics init failed, using Console only: " + exception.Message);
                StartConsoleService(settings);
            }
        }

        private static void StartConsoleService(AnalyticsSettings settings)
        {
            Analytics.SetService(new DebugAnalyticsService(settings.EnableConsoleLog));
            Analytics.SetCollectionEnabled(settings.CollectionEnabled);
        }
    }
}
