using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Ads.Tests
{
    public sealed class AdsProviderFactoryTests
    {
        private const string FakeProviderId = "policy-tests-fake";

        private AdsConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            Ads.ResetForTests();
            AdsProviderRegistry.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Ads.ResetForTests();
            if (_configuration != null) Object.DestroyImmediate(_configuration);
        }

        [Test]
        public void Registry_ReportsRegisteredProviders_UntilCleared()
        {
            Assert.That(AdsProviderRegistry.IsRegistered(FakeProviderId), Is.False);

            AdsProviderRegistry.Register(new FakeFactory(new FakeAdsService()));
            Assert.That(AdsProviderRegistry.IsRegistered(FakeProviderId), Is.True);
            Assert.That(AdsProviderRegistry.IsRegistered(FakeProviderId.ToUpperInvariant()), Is.True);

            AdsProviderRegistry.ClearForTests();
            Assert.That(AdsProviderRegistry.IsRegistered(FakeProviderId), Is.False);
            Assert.That(AdsProviderRegistry.IsRegistered(null), Is.False);
        }

        [Test]
        public void NoneProvider_ReturnsNoAdsService()
        {
            _configuration = Configuration("none");

            Assert.That(AdsProviderFactory.Create(_configuration), Is.InstanceOf<NoAdsService>());
        }

        [Test]
        public void UnknownProvider_LogsError_AndReturnsNoAdsService()
        {
            _configuration = Configuration("admob");

            LogAssert.Expect(LogType.Error, "[RippleGame Ads] No ads provider is registered for 'admob'. Install its SDK from RIPPLE Game > Open Hub > SDK.");
            var service = AdsProviderFactory.Create(_configuration);

            Assert.That(service, Is.InstanceOf<NoAdsService>());
        }

        [Test]
        public void RegisteredProvider_IsWrappedInPolicyLayer()
        {
            var fake = new FakeAdsService();
            AdsProviderRegistry.Register(new FakeFactory(fake));
            _configuration = Configuration(FakeProviderId);
            _configuration.Policy.Interstitial.MinLevel = 0;

            var service = AdsProviderFactory.Create(_configuration);

            Assert.That(service, Is.InstanceOf<PolicyAdsService>());
            Assert.That(AdsSession.Number, Is.GreaterThanOrEqualTo(1), "Creating a provider begins the ads session.");
            service.ShowInterstitial(AdPlacement.Default);
            Assert.That(fake.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
            fake.Close();
            service.Dispose();
        }

        [Test]
        public void BufferedSettings_AreAppliedWhenPolicyIsConfigured()
        {
            var fake = new FakeAdsService();
            var policy = new PolicyAdsService(fake, new AdsPolicySettings(), () => 0f, () => 1);
            var rewards = 0;
            AdsResult? interstitial = null;

            Ads.SetAdsEnabled(false);
            Ads.ApplyPolicyOverride(new AdsPolicyOverride { InterstitialCooldownSeconds = 0f });
            Ads.Configure(policy);

            Ads.ShowRewarded(AdPlacement.DoubleReward, () => rewards++);
            Ads.ShowInterstitial(AdPlacement.Default, r => interstitial = r);
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(interstitial, Is.EqualTo(AdsResult.Disabled));

            Ads.SetAdsEnabled(true);
            Ads.ShowInterstitial(AdPlacement.Default);
            fake.Close();
            Ads.ShowInterstitial(AdPlacement.Default);
            Assert.That(fake.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2), "The buffered 0 s cooldown override applied.");
            fake.Close();
        }

        [Test]
        public void BufferedOrganic_IsAppliedWhenPolicyIsConfigured()
        {
            var fake = new FakeAdsService();
            var settings = new AdsPolicySettings();
            settings.Organic.Interstitial = false;
            AdsResult? result = null;

            Ads.SetOrganic(true);
            Ads.Configure(new PolicyAdsService(fake, settings, () => 0f, () => 1));
            Ads.ShowInterstitial(AdPlacement.Default, r => result = r);

            Assert.That(result, Is.EqualTo(AdsResult.Disabled));
        }

        [Test]
        public void ResetForTests_ClearsPolicyState()
        {
            Ads.SetPolicyContext(new AdsPolicyContext(3));
            Ads.InitializationGate = () => Task.CompletedTask;
            AdUnitIdOverrides.Set("max", "android", AdFormat.Banner, "remote");

            Ads.ResetForTests();

            Assert.That(Ads.PolicyContext, Is.Null);
            Assert.That(Ads.InitializationGate, Is.Null);
            Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Banner, "asset"), Is.EqualTo("asset"));
            Assert.That(AdsSession.Number, Is.EqualTo(0));
            Assert.That(Ads.CurrentService, Is.InstanceOf<NoAdsService>());
        }

        [Test]
        public void Session_IncrementsOncePerProcess_AndPersists()
        {
            var store = new Dictionary<string, int> { { "ripplegame.ads.session", 4 } };
            int Read(string key) => store.TryGetValue(key, out var value) ? value : 0;
            void Write(string key, int value) => store[key] = value;

            AdsSession.Begin(Read, Write);
            AdsSession.Begin(Read, Write);

            Assert.That(AdsSession.Number, Is.EqualTo(5));
            Assert.That(store["ripplegame.ads.session"], Is.EqualTo(5));

            AdsSession.Reset();
            AdsSession.Begin(Read, Write);
            Assert.That(AdsSession.Number, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator InitializeAsync_WaitsForInitializationGate()
        {
            var fake = new FakeAdsService();
            var gate = new TaskCompletionSource<bool>();
            var gateCalls = 0;
            Ads.InitializationGate = () => { gateCalls++; return gate.Task; };
            var policy = new PolicyAdsService(fake, new AdsPolicySettings(), () => 0f, () => 1, Ads.WaitForInitializationGateAsync);

            var initialization = policy.InitializeAsync();
            var bootstrapperWait = Ads.WaitForInitializationGateAsync();
            yield return null;

            Assert.That(initialization.IsCompleted, Is.False);
            Assert.That(fake.Count(nameof(IAdsService.InitializeAsync)), Is.EqualTo(0));

            gate.SetResult(true);
            for (var frame = 0; frame < 10 && !initialization.IsCompleted; frame++) yield return null;

            Assert.That(initialization.IsCompleted, Is.True);
            Assert.That(initialization.Result, Is.EqualTo(AdsResult.Success));
            Assert.That(fake.Count(nameof(IAdsService.InitializeAsync)), Is.EqualTo(1));
            Assert.That(bootstrapperWait.IsCompleted, Is.True);
            Assert.That(gateCalls, Is.EqualTo(1), "The gate is invoked once and shared.");
            policy.Dispose();
        }

        [UnityTest]
        public IEnumerator InitializeAsync_FaultedGate_StillInitializesProvider()
        {
            var fake = new FakeAdsService();
            Ads.InitializationGate = () => Task.FromException(new System.InvalidOperationException("gate failed"));
            var policy = new PolicyAdsService(fake, new AdsPolicySettings(), () => 0f, () => 1, Ads.WaitForInitializationGateAsync);

            LogAssert.Expect(LogType.Exception, new Regex("gate failed"));
            var initialization = policy.InitializeAsync();
            for (var frame = 0; frame < 10 && !initialization.IsCompleted; frame++) yield return null;

            Assert.That(initialization.Result, Is.EqualTo(AdsResult.Success));
            Assert.That(fake.Count(nameof(IAdsService.InitializeAsync)), Is.EqualTo(1));
            policy.Dispose();
        }

        private static AdsConfiguration Configuration(string providerId)
        {
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            JsonUtility.FromJsonOverwrite("{\"providerId\":\"" + providerId + "\"}", configuration);
            return configuration;
        }

        private sealed class FakeFactory : IAdsProviderFactory
        {
            private readonly IAdsService _service;
            public FakeFactory(IAdsService service) => _service = service;
            public string ProviderId => FakeProviderId;
            public IAdsService Create(AdsConfiguration configuration) => _service;
        }
    }
}
