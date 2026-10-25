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

#if ADS
            if (configuration == null)
            {
                Debug.LogError("[RippleGame Ads] ADS is enabled, but no AdsConfigurationReference was found at Resources/RippleGameAds/AdsConfigurationReference.");
                Ads.Configure(new NoAdsService());
            }
            else
            {
                Ads.Configure(AdsProviderFactory.Create(configuration));
            }
#else
            Ads.Configure(new NoAdsService());
#endif

            var bannerRouter = GetComponent<AdsSceneBannerRouter>();
            if (bannerRouter == null)
            {
                Debug.LogError("[RippleGame Ads] Add AdsSceneBannerRouter to the same [RippleGame Ads] GameObject in the Loading scene.");
            }
            else
            {
                bannerRouter.Configure(configuration);
            }

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
