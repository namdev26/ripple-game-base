using UnityEngine;

namespace RippleGame.Ads.Max
{
    /// <summary>Registers the AppLovin MAX provider. This assembly only compiles with the pinned MAX package.</summary>
    internal sealed class MaxProviderFactory : IAdsProviderFactory
    {
        internal const string Id = "max";

        public string ProviderId => Id;

        public IAdsService Create(AdsConfiguration configuration) => new MaxAdsService(configuration);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register() => AdsProviderRegistry.Register(new MaxProviderFactory());
    }
}
