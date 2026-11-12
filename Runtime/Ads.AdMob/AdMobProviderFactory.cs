using UnityEngine;

namespace RippleGame.Ads.AdMob
{
    /// <summary>Registers the Google Mobile Ads provider. This assembly only compiles with the pinned GMA package.</summary>
    internal sealed class AdMobProviderFactory : IAdsProviderFactory
    {
        internal const string Id = "admob";

        public string ProviderId => Id;

        public IAdsService Create(AdsConfiguration configuration) => new AdMobAdsService(configuration);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register() => AdsProviderRegistry.Register(new AdMobProviderFactory());
    }
}
