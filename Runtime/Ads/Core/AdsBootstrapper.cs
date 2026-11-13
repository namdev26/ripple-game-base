using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Place once in the first scene in the build. It persists through scene changes.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class AdsBootstrapper : MonoBehaviour
    {
        private const string ConfigurationReferenceResourcePath = "RippleGameAds/AdsConfigurationReference";
        private static AdsBootstrapper _instance;

        [SerializeField] private AdsConfiguration configuration;

        /// <summary>Tests set the configuration on an inactive object before Awake runs.</summary>
        internal AdsConfiguration Configuration
        {
            set => configuration = value;
        }

        private async void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            MainThreadDispatcher.EnsureInitialized();

            if (configuration == null)
            {
                var reference = Resources.Load<AdsConfigurationReference>(ConfigurationReferenceResourcePath);
                configuration = reference != null ? reference.Configuration : null;
            }

            if (configuration == null)
            {
                Debug.LogError("[RippleGame Ads] No AdsConfigurationReference was found at Resources/RippleGameAds/AdsConfigurationReference.");
                Ads.Configure(new NoAdsService());
            }
            else
            {
                Ads.Configure(AdsProviderFactory.Create(configuration));
            }

            var bannerRouter = GetComponent<AdsSceneBannerRouter>();
            if (bannerRouter == null)
            {
                Debug.LogError("[RippleGame Ads] Add AdsSceneBannerRouter to the same [RippleGame Ads] GameObject in the Loading scene.");
            }
            else
            {
                bannerRouter.Configure(configuration);
            }

            // Wait for anything that must finish before the provider starts (consent, remote config).
            // The policy layer shares this same wait, so an early InitializeAsync from elsewhere is gated too.
            await Ads.WaitForInitializationGateAsync();
            await Ads.InitializeAsync();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (Ads.CurrentService is IAdsLifecycleAware lifecycleAware)
            {
                lifecycleAware.OnApplicationFocusChanged(hasFocus);
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (Ads.CurrentService is IAdsLifecycleAware lifecycleAware)
            {
                lifecycleAware.OnApplicationPauseChanged(isPaused);
            }
        }
    }
}
