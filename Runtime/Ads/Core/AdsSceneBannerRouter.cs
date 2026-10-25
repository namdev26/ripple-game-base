using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RippleGame.Ads
{
    /// <summary>Routes a persistent banner across build scenes, excluding configured scene names.</summary>
    [DisallowMultipleComponent]
    public sealed class AdsSceneBannerRouter : MonoBehaviour
    {
        private AdsConfiguration _configuration;
        private int _routeVersion;
        private Coroutine _routeCoroutine;

        internal void Configure(AdsConfiguration configuration)
        {
            _configuration = configuration;
            Route(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Ads.EventRaised += OnAdsEvent;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Ads.EventRaised -= OnAdsEvent;
            if (_routeCoroutine != null)
            {
                StopCoroutine(_routeCoroutine);
                _routeCoroutine = null;
            }
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            Route(nextScene);
        }

        private void OnAdsEvent(AdsEvent adsEvent)
        {
            if (adsEvent.Type != AdsEventType.Closed ||
                (adsEvent.Format != AdFormat.AppOpen && adsEvent.Format != AdFormat.Interstitial && adsEvent.Format != AdFormat.Rewarded))
            {
                return;
            }

            // A full-screen ad can temporarily cover or hide the native banner. Restore visibility
            // without creating another BannerView or issuing another banner request.
            Route(SceneManager.GetActiveScene());
        }

        private void Route(Scene scene)
        {
            var routeVersion = ++_routeVersion;
            if (_routeCoroutine != null)
            {
                StopCoroutine(_routeCoroutine);
            }

            _routeCoroutine = StartCoroutine(RouteWhenSceneIsReady(scene, routeVersion));
        }

        private IEnumerator RouteWhenSceneIsReady(Scene scene, int routeVersion)
        {
            var sceneName = scene.name;
            if (_configuration == null || !_configuration.ShouldShowBannerInScene(sceneName, scene.buildIndex))
            {
                Log($"Hiding banner in scene '{sceneName}'.");
                Ads.HideBanner();
                yield break;
            }

            // Wait one frame so a single-mode scene replacement is fully active before calling the native SDK.
            yield return null;

            Task<AdsResult> initialization = Ads.InitializeAsync();
            while (!initialization.IsCompleted)
            {
                yield return null;
            }

            if (routeVersion != _routeVersion || initialization.IsFaulted || initialization.IsCanceled || initialization.Result != AdsResult.Success || SceneManager.GetActiveScene().name != sceneName)
            {
                yield break;
            }

            Log($"Showing persistent banner in scene '{sceneName}'.");
            Ads.ShowBanner(_configuration.GetActiveProfile().BannerPosition);
            _routeCoroutine = null;
        }

        private void Log(string message)
        {
            if (_configuration != null && _configuration.EnableLogging)
            {
                Debug.Log($"[RippleGame Ads] {message}");
            }
        }
    }
}
