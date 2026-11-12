using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RippleGame.RemoteConfig;
using UnityEngine;
using UnityEngine.TestTools;
using RemoteConfigApi = RippleGame.RemoteConfig.RemoteConfig;
using RemoteConfigSettings = RippleGame.RemoteConfig.RemoteConfigSettings;

namespace RippleGame.Ads.RemoteConfigBridge.Tests
{
    public sealed class AdsRemoteConfigBridgeTests
    {
        private const string CacheKey = AdsRemoteConfigBridge.PolicyCacheKey;

        /// <summary>Remote Config source whose fetch never resolves, like a dead connection.</summary>
        private sealed class SilentSource : IRemoteConfigSource
        {
            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

            public Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval) => new TaskCompletionSource<bool>().Task;

            public bool TryGetString(string key, out string value) => Values.TryGetValue(key, out value);

            public void SetDefaults(IReadOnlyDictionary<string, string> defaults) { }
        }

        private SilentSource _source;
        private string _savedCache;
        private Func<NetworkReachability> _savedReachability;

        [SetUp]
        public void SetUp()
        {
            _savedCache = PlayerPrefs.HasKey(CacheKey) ? PlayerPrefs.GetString(CacheKey) : null;
            PlayerPrefs.DeleteKey(CacheKey);
            _savedReachability = RemoteConfigNetworkWatcher.ReachabilityProvider;
            RemoteConfigNetworkWatcher.ReachabilityProvider = () => NetworkReachability.NotReachable;

            ResetAll();
            _source = new SilentSource();
            RemoteConfigApi.RegisterSource(_source);
        }

        [TearDown]
        public void TearDown()
        {
            ResetAll();
            RemoteConfigNetworkWatcher.ReachabilityProvider = _savedReachability;

            if (_savedCache != null) PlayerPrefs.SetString(CacheKey, _savedCache);
            else PlayerPrefs.DeleteKey(CacheKey);
            PlayerPrefs.Save();
        }

        private static void ResetAll()
        {
            AdsRemoteConfigBridge.ResetForTests();
            Ads.ResetForTests();
            RemoteConfigApi.ResetForTests();
        }

        // ---- Cache ----

        [Test]
        public void Bridge_CachedOverrideAppliedBeforeFetch()
        {
            PlayerPrefs.SetString(CacheKey, "{\"interstitial\":{\"min_level\":4,\"cooldown_seconds\":\"bad\"}}");

            AdsRemoteConfigBridge.Install();

            Assert.That(RemoteConfigApi.IsReady, Is.False, "the source never answered");
            Assert.That(Ads.CurrentPolicyOverride, Is.Not.Null);
            Assert.That(Ads.CurrentPolicyOverride.InterstitialMinLevel, Is.EqualTo(4));
            Assert.That(Ads.CurrentPolicyOverride.InterstitialCooldownSeconds, Is.Null);
        }

        [Test]
        public void Bridge_NoCache_AppliesNothingAtInstall()
        {
            AdsRemoteConfigBridge.Install();

            Assert.That(Ads.CurrentPolicyOverride, Is.Null);
            Assert.That(Ads.AdsEnabledValue, Is.Null);
        }

        [Test]
        public void Bridge_CorruptCache_IsDropped()
        {
            PlayerPrefs.SetString(CacheKey, "{broken");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"ads_policy is not valid JSON"));

            AdsRemoteConfigBridge.Install();

            Assert.That(Ads.CurrentPolicyOverride, Is.Null);
            Assert.That(PlayerPrefs.HasKey(CacheKey), Is.False);
        }

        [Test]
        public void Bridge_Activated_PolicyIsAppliedAndCached()
        {
            const string json = "{\"interstitial\":{\"cooldown_seconds\":60,\"min_level\":3}}";
            _source.Values[AdsRemoteConfigKeys.AdsPolicy] = json;
            AdsRemoteConfigBridge.Install();

            RemoteConfigApi.RaiseActivated();

            Assert.That(Ads.CurrentPolicyOverride.InterstitialCooldownSeconds, Is.EqualTo(60f));
            Assert.That(Ads.CurrentPolicyOverride.InterstitialMinLevel, Is.EqualTo(3));
            Assert.That(PlayerPrefs.GetString(CacheKey), Is.EqualTo(json));
        }

        [Test]
        public void Bridge_Activated_InvalidPolicy_KeepsCacheAndCurrentOverride()
        {
            const string cached = "{\"interstitial\":{\"min_level\":4}}";
            PlayerPrefs.SetString(CacheKey, cached);
            _source.Values[AdsRemoteConfigKeys.AdsPolicy] = "{not json";
            AdsRemoteConfigBridge.Install();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"ads_policy is not valid JSON"));

            RemoteConfigApi.RaiseActivated();

            Assert.That(PlayerPrefs.GetString(CacheKey), Is.EqualTo(cached));
            Assert.That(Ads.CurrentPolicyOverride.InterstitialMinLevel, Is.EqualTo(4));
        }

        [Test]
        public void Bridge_Activated_PolicyKeyAbsent_KeepsCache()
        {
            const string cached = "{\"interstitial\":{\"min_level\":4}}";
            PlayerPrefs.SetString(CacheKey, cached);
            AdsRemoteConfigBridge.Install();

            RemoteConfigApi.RaiseActivated();

            Assert.That(PlayerPrefs.GetString(CacheKey), Is.EqualTo(cached));
            Assert.That(Ads.CurrentPolicyOverride.InterstitialMinLevel, Is.EqualTo(4));
        }

        // ---- ads_enabled ----

        [Test]
        public void Bridge_AdsEnabledFalse_DisablesAds()
        {
            _source.Values[AdsRemoteConfigKeys.AdsEnabled] = "false";
            AdsRemoteConfigBridge.Install();

            RemoteConfigApi.RaiseActivated();

            Assert.That(Ads.AdsEnabledValue, Is.False);
        }

        [Test]
        public void Bridge_AdsEnabledAbsent_DoesNotCallSetAdsEnabled()
        {
            AdsRemoteConfigBridge.Install();

            RemoteConfigApi.RaiseActivated();

            Assert.That(Ads.AdsEnabledValue, Is.Null);
        }

        [Test]
        public void Bridge_AdsEnabledTrueAfterFalse_ReEnables()
        {
            _source.Values[AdsRemoteConfigKeys.AdsEnabled] = "false";
            AdsRemoteConfigBridge.Install();
            RemoteConfigApi.RaiseActivated();

            _source.Values[AdsRemoteConfigKeys.AdsEnabled] = "true";
            RemoteConfigApi.RaiseActivated();

            Assert.That(Ads.AdsEnabledValue, Is.True);
        }

        // ---- ads_unit_ids ----

        [Test]
        public void Bridge_Activated_UnitIdsAreSet_LastWriteWins_AndRepeatIsHarmless()
        {
            _source.Values[AdsRemoteConfigKeys.AdsUnitIds] = "{\"max\":{\"android\":{\"interstitial\":\"first\",\"banner\":\"\"}}}";
            AdsRemoteConfigBridge.Install();

            RemoteConfigApi.RaiseActivated();
            RemoteConfigApi.RaiseActivated();
            Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Interstitial, "asset"), Is.EqualTo("first"));
            Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Banner, "asset"), Is.EqualTo("asset"), "empty ids are skipped");

            _source.Values[AdsRemoteConfigKeys.AdsUnitIds] = "{\"max\":{\"android\":{\"interstitial\":\"second\"}}}";
            RemoteConfigApi.RaiseActivated();
            Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Interstitial, "asset"), Is.EqualTo("second"));
        }

        // ---- Ready without a successful fetch ----

        [UnityTest]
        public IEnumerator Bridge_ReadyWithoutActivated_AppliesDefaults_ButNeverTouchesPolicyCache()
        {
            RemoteConfigApi.ResetForTests(); // no source: ready at once, Activated never raised
            const string cached = "{\"interstitial\":{\"min_level\":4}}";
            PlayerPrefs.SetString(CacheKey, cached);
            var settings = RemoteConfigSettings.CreateForTests(1f, 0,
                new RemoteConfigDefault(AdsRemoteConfigKeys.AdsEnabled, "false"),
                new RemoteConfigDefault(AdsRemoteConfigKeys.AdsPolicy, "{\"interstitial\":{\"min_level\":99}}"),
                new RemoteConfigDefault(AdsRemoteConfigKeys.AdsUnitIds, "{\"max\":{\"ios\":{\"rewarded\":\"offline-id\"}}}"));
            RemoteConfigApi.SetSettings(settings);
            AdsRemoteConfigBridge.Install();

            _ = RemoteConfigBootstrap.Run(settings);
            yield return WaitUntil(() => Ads.AdsEnabledValue.HasValue, "ads_enabled applied from defaults");

            Assert.That(Ads.AdsEnabledValue, Is.False);
            Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.IPhonePlayer, AdFormat.Rewarded, "asset"), Is.EqualTo("offline-id"));
            Assert.That(PlayerPrefs.GetString(CacheKey), Is.EqualTo(cached), "defaults never replace the cached policy");
            Assert.That(Ads.CurrentPolicyOverride.InterstitialMinLevel, Is.EqualTo(4), "the cached policy stays in force");
        }

        // ---- Initialization gate (Review Focus 4) ----

        [UnityTest]
        public IEnumerator Bridge_GateCompletesAfterTimeoutWhenSourceNeverAnswers()
        {
            const float timeout = 0.4f;
            var settings = RemoteConfigSettings.CreateForTests(timeout, 0);
            RemoteConfigApi.SetSettings(settings);
            AdsRemoteConfigBridge.Install();
            Assert.That(Ads.InitializationGate, Is.Not.Null);

            _ = RemoteConfigBootstrap.Run(settings);
            var start = Time.realtimeSinceStartup;
            var gate = Ads.WaitForInitializationGateAsync();
            var limit = timeout + 1f;
            while (!gate.IsCompleted && Time.realtimeSinceStartup - start < limit + 1f) yield return null;

            var elapsed = Time.realtimeSinceStartup - start;
            Assert.That(gate.IsCompleted, Is.True, "the gate must not wait for a source that never answers");
            Assert.That(gate.IsFaulted, Is.False);
            Assert.That(elapsed, Is.LessThanOrEqualTo(limit), "FetchTimeoutSeconds + 1 s");
            Assert.That(RemoteConfigApi.IsReady, Is.True);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what, float seconds = 3f)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, "timed out waiting for: " + what);
        }
    }
}
