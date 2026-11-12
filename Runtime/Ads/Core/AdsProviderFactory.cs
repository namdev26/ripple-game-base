using System;
using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Provider packages register a factory before AdsBootstrapper initializes.</summary>
    public static class AdsProviderRegistry
    {
        private static readonly Dictionary<string, IAdsProviderFactory> Factories = new Dictionary<string, IAdsProviderFactory>(StringComparer.OrdinalIgnoreCase);

        public static void Register(IAdsProviderFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (string.IsNullOrWhiteSpace(factory.ProviderId)) throw new ArgumentException("ProviderId cannot be empty.", nameof(factory));
            Factories[factory.ProviderId] = factory;
        }

        public static bool IsRegistered(string providerId)
        {
            return !string.IsNullOrEmpty(providerId) && Factories.ContainsKey(providerId);
        }

        internal static void ClearForTests() => Factories.Clear();

        internal static bool TryCreate(string providerId, AdsConfiguration configuration, out IAdsService service)
        {
            if (!string.IsNullOrEmpty(providerId) && Factories.TryGetValue(providerId, out var factory))
            {
                service = factory.Create(configuration);
                return service != null;
            }

            service = null;
            return false;
        }
    }

    internal static class AdsProviderFactory
    {
        /// <summary>
        /// Returns the registered provider for <see cref="AdsConfiguration.ProviderId"/> wrapped in the policy
        /// layer, or <see cref="NoAdsService"/> for "none", ADS_DISABLED builds and unknown providers.
        /// </summary>
        internal static IAdsService Create(AdsConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

#if ADS_DISABLED
            return new NoAdsService();
#else
            var providerId = configuration.ProviderId;
            if (string.Equals(providerId, "none", StringComparison.OrdinalIgnoreCase))
            {
                return new NoAdsService();
            }

            if (AdsProviderRegistry.TryCreate(providerId, configuration, out var registeredService))
            {
                AdsSession.BeginWithPlayerPrefs();
                return new PolicyAdsService(
                    registeredService,
                    configuration.Policy,
                    () => Time.realtimeSinceStartup,
                    () => AdsSession.Number,
                    Ads.WaitForInitializationGateAsync);
            }

            Debug.LogError($"[RippleGame Ads] No ads provider is registered for '{providerId}'. Install its SDK from RIPPLE Game > Open Hub > SDK.");
            return new NoAdsService();
#endif
        }
    }
}
