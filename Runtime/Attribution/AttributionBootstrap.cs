using System;
using UnityEngine;

namespace RippleGame.Attribution
{
    /// <summary>Starts attribution before the first scene loads: loads the saved status, then starts the provider if one registered.</summary>
    internal static class AttributionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => Initialize(AttributionSettings.LoadOrDefault());

        internal static void Initialize(AttributionSettings settings)
        {
            Attribution.SetSettings(settings);
            Attribution.LoadPersisted();

            var service = Attribution.Service;
            if (service == null) return;

            try
            {
                service.Initialize(settings);
            }
            catch (Exception exception)
            {
                Debug.LogError(Attribution.LogPrefix + "Attribution init failed: " + exception.Message);
            }
        }
    }
}
