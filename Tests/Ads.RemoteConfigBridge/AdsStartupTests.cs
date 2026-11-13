using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RippleGame.RemoteConfig;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using RemoteConfigApi = RippleGame.RemoteConfig.RemoteConfig;
using RemoteConfigSettings = RippleGame.RemoteConfig.RemoteConfigSettings;

namespace RippleGame.Ads.RemoteConfigBridge.Tests
{
    /// <summary>
    /// Runs the real startup path: AdsBootstrapper → AdsProviderFactory → PolicyAdsService (gated by the Remote Config
    /// bridge) → a fake provider registered like the real ones. Spec 6.1 and final review C1/I1/I5.
    /// </summary>
    public sealed class AdsStartupTests
    {
        private const string ProviderId = "startup-test-fake";
        private const string AssetUnitId = "asset-interstitial";

        private static readonly string[] SavedKeys =
        {
            AdsRemoteConfigBridge.PolicyCacheKey,
            AdsRemoteConfigBridge.UnitIdsCacheKey
        };

        private readonly Dictionary<string, string> _savedPrefs = new Dictionary<string, string>();
        private readonly List<Scene> _createdScenes = new List<Scene>();
        private Func<NetworkReachability> _savedReachability;
        private RecordingProvider _provider;
        private AdsConfiguration _configuration;
        private GameObject _bootstrapperObject;
        private Scene _originalScene;

        [SetUp]
        public void SetUp()
        {
            foreach (var key in SavedKeys)
            {
                if (PlayerPrefs.HasKey(key)) _savedPrefs[key] = PlayerPrefs.GetString(key);
                PlayerPrefs.DeleteKey(key);
            }

            _savedReachability = RemoteConfigNetworkWatcher.ReachabilityProvider;
            RemoteConfigNetworkWatcher.ReachabilityProvider = () => NetworkReachability.NotReachable;
            _originalScene = SceneManager.GetActiveScene();

            ResetAll();
            _provider = new RecordingProvider();
            AdsProviderRegistry.Register(new RecordingFactory(_provider));
            _configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            JsonUtility.FromJsonOverwrite("{\"providerId\":\"" + ProviderId + "\"}", _configuration);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrapperObject != null) Object.Destroy(_bootstrapperObject);
            if (_originalScene.IsValid() && _originalScene.isLoaded) SceneManager.SetActiveScene(_originalScene);
            foreach (var scene in _createdScenes)
            {
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var unload = SceneManager.UnloadSceneAsync(scene);
                while (unload != null && !unload.isDone) yield return null;
            }

            _createdScenes.Clear();
            yield return null; // Destroy is deferred; the next test must not find this bootstrapper.

            ResetAll();
            if (_configuration != null) Object.Destroy(_configuration);
            RemoteConfigNetworkWatcher.ReachabilityProvider = _savedReachability;

            foreach (var key in SavedKeys)
            {
                if (_savedPrefs.TryGetValue(key, out var value)) PlayerPrefs.SetString(key, value);
                else PlayerPrefs.DeleteKey(key);
            }

            _savedPrefs.Clear();
            PlayerPrefs.Save();
        }

        private static void ResetAll()
        {
            AdsRemoteConfigBridge.ResetForTests();
            Ads.ResetForTests();
            RemoteConfigApi.ResetForTests();
            RemoteConfigBootstrap.ResetForTests();
        }

        // ---- C1 / I1: values from Remote Config reach the provider when it initializes ----

        [UnityTest]
        public IEnumerator Startup_FetchedUnitIdsAndRefresh_ReachProviderAtInitialize()
        {
            const string unitIds = "{\"" + ProviderId + "\":{\"android\":{\"interstitial\":\"remote-interstitial\"}}}";
            var source = new DelayedSource { Delay = TimeSpan.FromSeconds(0.3) };
            source.ValuesAfterFetch[AdsRemoteConfigKeys.AdsUnitIds] = unitIds;
            source.ValuesAfterFetch[AdsRemoteConfigKeys.AdsPolicy] = "{\"banner\":{\"refresh_seconds\":15}}";
            RemoteConfigApi.RegisterSource(source);
            var settings = RemoteConfigSettings.CreateForTests(5f, 0);
            RemoteConfigApi.SetSettings(settings);
            AdsRemoteConfigBridge.Install();
            _ = RemoteConfigBootstrap.Run(settings);

            BootAds();
            Assert.That(_provider.UnitIdAtCreate, Is.EqualTo(AssetUnitId), "the provider is created before Remote Config answers");

            yield return WaitUntil(() => _provider.InitializeCalls > 0, "provider InitializeAsync");

            Assert.That(source.Fetched, Is.True);
            Assert.That(_provider.UnitIdAtInitialize, Is.EqualTo("remote-interstitial"), "ads_unit_ids applied before the gate released");
            Assert.That(_provider.BannerRefreshAtInitialize, Is.EqualTo(15f), "ads_policy refresh applied before the gate released");
            Assert.That(PlayerPrefs.GetString(AdsRemoteConfigBridge.UnitIdsCacheKey), Is.EqualTo(unitIds), "fetched ids are cached for the next session");
        }

        [UnityTest]
        public IEnumerator Startup_FetchTimesOut_DefaultUnitIdsAreAppliedInsideTheGate()
        {
            RemoteConfigApi.RegisterSource(new DelayedSource { NeverAnswers = true });
            var settings = RemoteConfigSettings.CreateForTests(0.3f, 0,
                new RemoteConfigDefault(AdsRemoteConfigKeys.AdsUnitIds, "{\"" + ProviderId + "\":{\"android\":{\"interstitial\":\"default-interstitial\"}}}"));
            RemoteConfigApi.SetSettings(settings);
            AdsRemoteConfigBridge.Install();
            _ = RemoteConfigBootstrap.Run(settings);

            BootAds();
            yield return WaitUntil(() => _provider.InitializeCalls > 0, "provider InitializeAsync");

            Assert.That(_provider.UnitIdAtInitialize, Is.EqualTo("default-interstitial"));
            Assert.That(PlayerPrefs.HasKey(AdsRemoteConfigBridge.UnitIdsCacheKey), Is.False, "defaults never replace the cache");
        }

        [UnityTest]
        public IEnumerator Startup_CachedUnitIds_ReachProviderWhenRemoteConfigNeverAnswers()
        {
            PlayerPrefs.SetString(AdsRemoteConfigBridge.UnitIdsCacheKey, "{\"" + ProviderId + "\":{\"android\":{\"interstitial\":\"cached-interstitial\"}}}");
            RemoteConfigApi.RegisterSource(new DelayedSource { NeverAnswers = true });
            var settings = RemoteConfigSettings.CreateForTests(0.3f, 0);
            RemoteConfigApi.SetSettings(settings);
            AdsRemoteConfigBridge.Install();
            _ = RemoteConfigBootstrap.Run(settings);

            BootAds();
            yield return WaitUntil(() => _provider.InitializeCalls > 0, "provider InitializeAsync");

            Assert.That(_provider.UnitIdAtCreate, Is.EqualTo("cached-interstitial"), "the cache is applied at install, before any provider exists");
            Assert.That(_provider.UnitIdAtInitialize, Is.EqualTo("cached-interstitial"));
        }

        // ---- I5: Ads keeps working across scene changes (spec 6.1) ----

        [UnityTest]
        public IEnumerator Ads_KeepsWorkingAcrossSceneChanges()
        {
            BootAds();
            yield return WaitUntil(() => _provider.InitializeCalls > 0, "provider InitializeAsync");
            var service = Ads.CurrentService;
            Assert.That(service, Is.InstanceOf<PolicyAdsService>());

            var rewards = 0;
            var results = new List<AdsResult>();
            Ads.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, results.Add);
            Assert.That(_provider.Count(nameof(IAdsService.ShowRewarded)), Is.EqualTo(1));

            // Scenes made at runtime are not in the build list, so the banner router hides the banner in them.
            var hidesBefore = _provider.Count(nameof(IAdsService.HideBanner));
            var sceneA = SceneManager.CreateScene("AdsStartupTests_A");
            _createdScenes.Add(sceneA);
            SceneManager.SetActiveScene(sceneA);
            yield return WaitUntil(() => _provider.Count(nameof(IAdsService.HideBanner)) > hidesBefore, "banner routed for scene A");

            hidesBefore = _provider.Count(nameof(IAdsService.HideBanner));
            var sceneB = SceneManager.CreateScene("AdsStartupTests_B");
            _createdScenes.Add(sceneB);
            SceneManager.SetActiveScene(sceneB);
            var unload = SceneManager.UnloadSceneAsync(sceneA);
            while (!unload.isDone) yield return null;
            yield return WaitUntil(() => _provider.Count(nameof(IAdsService.HideBanner)) > hidesBefore, "banner routed for scene B");

            Assert.That(_bootstrapperObject != null, Is.True, "the bootstrapper survives scene changes");
            Assert.That(_bootstrapperObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(Ads.CurrentService, Is.SameAs(service), "the same service instance serves every scene");
            Assert.That(_provider.InitializeCalls, Is.EqualTo(1));
            Assert.That(_provider.Creates, Is.EqualTo(1));

            Ads.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, results.Add);
            Ads.ShowInterstitial(AdPlacement.LevelComplete, results.Add);
            Assert.That(_provider.Count(nameof(IAdsService.ShowRewarded)), Is.EqualTo(2), "show calls still reach the provider");
            Assert.That(_provider.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
            Assert.That(rewards, Is.EqualTo(2));
            Assert.That(results, Is.EqualTo(new[] { AdsResult.Closed, AdsResult.Closed, AdsResult.Closed }));

            // A second bootstrapper (the Loading scene loaded again) is destroyed and does not replace the service.
            var duplicate = new GameObject("[RippleGame Ads] duplicate");
            duplicate.SetActive(false);
            duplicate.AddComponent<AdsSceneBannerRouter>();
            duplicate.AddComponent<AdsBootstrapper>().Configuration = _configuration;
            duplicate.SetActive(true);
            yield return null;
            Assert.That(duplicate == null, Is.True);
            Assert.That(Ads.CurrentService, Is.SameAs(service));
            Assert.That(_provider.Creates, Is.EqualTo(1));
        }

        // ---- Helpers ----

        private void BootAds()
        {
            _configuration.Policy.Interstitial.CooldownSeconds = 0f;
            _configuration.Policy.Interstitial.MinLevel = 0;
            _configuration.Policy.Interstitial.StartSession = 1;
            _bootstrapperObject = new GameObject("[RippleGame Ads]");
            _bootstrapperObject.SetActive(false);
            _bootstrapperObject.AddComponent<AdsSceneBannerRouter>();
            _bootstrapperObject.AddComponent<AdsBootstrapper>().Configuration = _configuration;
            _bootstrapperObject.SetActive(true); // Awake: create the provider, then wait for the gate and initialize
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what, float seconds = 5f)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, "timed out waiting for: " + what);
        }

        private sealed class RecordingFactory : IAdsProviderFactory
        {
            private readonly RecordingProvider _provider;
            public RecordingFactory(RecordingProvider provider) => _provider = provider;
            public string ProviderId => AdsStartupTests.ProviderId;

            public IAdsService Create(AdsConfiguration configuration)
            {
                _provider.Creates++;
                _provider.UnitIdAtCreate = ResolveInterstitial();
                return _provider;
            }
        }

        // Resolves like the real providers do; the platform is fixed because the editor platform has no overrides.
        private static string ResolveInterstitial() =>
            AdUnitIdOverrides.Resolve(ProviderId, RuntimePlatform.Android, AdFormat.Interstitial, AssetUnitId);

        /// <summary>Provider double that records what it would read at InitializeAsync. Full-screen shows close at once.</summary>
        private sealed class RecordingProvider : IAdsService
        {
            private readonly List<string> _calls = new List<string>();

            public event Action<AdsEvent> EventRaised { add { } remove { } }
            public AdsEnvironment Environment => AdsEnvironment.Test;
            public int Creates { get; set; }
            public int InitializeCalls { get; private set; }
            public string UnitIdAtCreate { get; set; }
            public string UnitIdAtInitialize { get; private set; }
            public float BannerRefreshAtInitialize { get; private set; } = -1f;

            public int Count(string call) => _calls.FindAll(c => c == call).Count;

            public Task<AdsResult> InitializeAsync()
            {
                InitializeCalls++;
                UnitIdAtInitialize = ResolveInterstitial();
                BannerRefreshAtInitialize = Ads.EffectivePolicy(new AdsPolicySettings()).BannerRefreshSeconds;
                return Task.FromResult(AdsResult.Success);
            }

            public bool IsReady(AdFormat format) => InitializeCalls > 0;
            public void ShowBanner(BannerPosition position) => _calls.Add(nameof(ShowBanner));
            public void HideBanner() => _calls.Add(nameof(HideBanner));
            public void DestroyBanner() => _calls.Add(nameof(DestroyBanner));
            public void ShowMrec(MrecPosition position) => _calls.Add(nameof(ShowMrec));
            public void HideMrec() => _calls.Add(nameof(HideMrec));

            public void ShowInterstitial(AdPlacement placement, Action<AdsResult> completed = null)
            {
                _calls.Add(nameof(ShowInterstitial));
                completed?.Invoke(AdsResult.Closed);
            }

            public void ShowRewarded(AdPlacement placement, Action rewarded, Action<AdsResult> completed = null)
            {
                _calls.Add(nameof(ShowRewarded));
                rewarded?.Invoke();
                completed?.Invoke(AdsResult.Closed);
            }

            public void ShowAppOpen(Action<AdsResult> completed = null)
            {
                _calls.Add(nameof(ShowAppOpen));
                completed?.Invoke(AdsResult.Closed);
            }

            public void Dispose() { }
        }

        /// <summary>Remote Config source that answers after a delay, or never.</summary>
        private sealed class DelayedSource : IRemoteConfigSource
        {
            public TimeSpan Delay { get; set; }
            public bool NeverAnswers { get; set; }
            public bool Fetched { get; private set; }
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
            public Dictionary<string, string> ValuesAfterFetch { get; } = new Dictionary<string, string>();

            public async Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval)
            {
                if (NeverAnswers) return await new TaskCompletionSource<bool>().Task;
                await Task.Delay(Delay);
                foreach (var pair in ValuesAfterFetch) Values[pair.Key] = pair.Value;
                Fetched = true;
                return true;
            }

            public bool TryGetString(string key, out string value) => Values.TryGetValue(key, out value);

            public void SetDefaults(IReadOnlyDictionary<string, string> defaults) { }
        }
    }
}
