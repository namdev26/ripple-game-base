using System;
using System.Collections.Generic;

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

        internal static bool TryCreate(string providerId, AdsConfiguration configuration, out IAdsService service)
        {
            if (Factories.TryGetValue(providerId, out var factory))
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
        internal static IAdsService Create(AdsConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (AdsProviderRegistry.TryCreate(configuration.ProviderId, configuration, out var registeredService))
            {
                return registeredService;
            }

            if (string.Equals(configuration.ProviderId, "none", StringComparison.OrdinalIgnoreCase))
            {
                return new NoAdsService();
            }

#if ADS
            if (string.Equals(configuration.ProviderId, "admob", StringComparison.OrdinalIgnoreCase))
            {
                return new AdMobAdsService(configuration);
            }

            throw new InvalidOperationException($"No ads provider is registered for '{configuration.ProviderId}'.");
#else
            return new NoAdsService();
#endif
        }
    }
}
