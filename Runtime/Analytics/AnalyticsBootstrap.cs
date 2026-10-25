using System;
using UnityEngine;

namespace RippleGame.Analytics
{
    /// <summary>Starts analytics before the first scene loads. No scene object is needed.</summary>
    internal static class AnalyticsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Keeps state clean when Domain Reload is disabled in Enter Play Mode Options.
            Analytics.ResetForTests();
            GameEvents.ResetForTests();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var settings = AnalyticsSettings.LoadOrDefault();
            Analytics.SetSettings(settings);

#if FIREBASE && !UNITY_EDITOR
            StartService(settings, FirebaseAnalyticsService.Initialize);
#else
            StartService(settings, StartConsoleService);
#endif
        }

        /// <summary>Runs a backend initializer; any synchronous failure falls back to the Console service.</summary>
        internal static void StartService(AnalyticsSettings settings, Action<AnalyticsSettings> initializer)
        {
            try
            {
                initializer(settings);
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
