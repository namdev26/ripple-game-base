using NUnit.Framework;
using UnityEngine;

namespace RippleGame.Ads.Tests
{
    public sealed class AdsConfigurationMigrationTests
    {
        // Shape of a 0.3.1 asset: legacy cooldown fields on the AdMob profiles, no policy block, not yet migrated.
        // policyMigrated is explicit because CreateInstance already ran OnEnable, which marks a new instance migrated.
        private const string LegacyJson =
            "{\"providerId\":\"admob\",\"policyMigrated\":false,\"production\":{" +
            "\"interstitialCooldownSeconds\":60,\"interstitialMaxImpressionsPerSession\":6," +
            "\"appOpenCooldownSeconds\":25,\"appOpenMinimumBackgroundSeconds\":9}}";

        private AdsConfiguration _configuration;
        private AdsConfiguration _reloaded;

        [TearDown]
        public void TearDown()
        {
            if (_configuration != null) Object.DestroyImmediate(_configuration);
            if (_reloaded != null) Object.DestroyImmediate(_reloaded);
        }

        [Test]
        public void LegacyCooldowns_MigrateIntoPolicyOnce()
        {
            _configuration = LoadLegacy();

            Assert.That(_configuration.Policy.Interstitial.CooldownSeconds, Is.EqualTo(60f));
            Assert.That(_configuration.Policy.Interstitial.MaxPerSession, Is.EqualTo(6));
            Assert.That(_configuration.Policy.AppOpen.CooldownSeconds, Is.EqualTo(25f));
            Assert.That(_configuration.Policy.AppOpen.MinBackgroundSeconds, Is.EqualTo(9f));

            _configuration.Policy.Interstitial.CooldownSeconds = 30f;
            _reloaded = Reserialize(_configuration);

            Assert.That(_reloaded.Policy.Interstitial.CooldownSeconds, Is.EqualTo(30f));
            Assert.That(_reloaded.Policy.AppOpen.CooldownSeconds, Is.EqualTo(25f));
        }

        [Test]
        public void LegacyFields_StayDeserializable()
        {
            _configuration = LoadLegacy();

            Assert.That(_configuration.Production.InterstitialCooldownSeconds, Is.EqualTo(60f));
            Assert.That(_configuration.Production.AppOpenMinimumBackgroundSeconds, Is.EqualTo(9f));
        }

        [Test]
        public void NewConfiguration_ProviderIsNone_AndPolicyHasSpecDefaults()
        {
            _configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            var policy = _configuration.Policy;

            Assert.That(_configuration.ProviderId, Is.EqualTo("none"));
            Assert.That(policy.Interstitial.CooldownSeconds, Is.EqualTo(45f));
            Assert.That(policy.Interstitial.MaxPerSession, Is.EqualTo(4));
            Assert.That(policy.Interstitial.MinLevel, Is.EqualTo(0));
            Assert.That(policy.Interstitial.StartSession, Is.EqualTo(1));
            Assert.That(policy.Interstitial.DisabledPlacements, Is.Empty);
            Assert.That(policy.AppOpen.Enabled, Is.True);
            Assert.That(policy.AppOpen.CooldownSeconds, Is.EqualTo(20f));
            Assert.That(policy.AppOpen.MinBackgroundSeconds, Is.EqualTo(5f));
            Assert.That(policy.RewardedFallbackEnabled, Is.False);
            Assert.That(policy.Organic.Banner, Is.True);
            Assert.That(policy.Organic.Mrec, Is.True);
            Assert.That(policy.Organic.Interstitial, Is.True);
            Assert.That(policy.Organic.AppOpen, Is.True);
            Assert.That(policy.BannerRefreshSeconds, Is.EqualTo(0f));
            Assert.That(policy.MrecRefreshSeconds, Is.EqualTo(0f));
            Assert.That(policy.PauseTimeAndAudio, Is.True);
        }

        [Test]
        public void NewConfiguration_EditsSurviveReload()
        {
            _configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            _configuration.Policy.Interstitial.CooldownSeconds = 12f;

            _reloaded = Reserialize(_configuration);

            Assert.That(_reloaded.Policy.Interstitial.CooldownSeconds, Is.EqualTo(12f));
        }

        [Test]
        public void PolicyClone_IsIndependent()
        {
            _configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            _configuration.Policy.Interstitial.DisabledPlacements.Add(AdPlacement.Retry);

            var clone = _configuration.Policy.Clone();
            clone.Interstitial.DisabledPlacements.Add(AdPlacement.Pause);
            clone.Interstitial.CooldownSeconds = 99f;

            Assert.That(_configuration.Policy.Interstitial.DisabledPlacements, Is.EqualTo(new[] { AdPlacement.Retry }));
            Assert.That(_configuration.Policy.Interstitial.CooldownSeconds, Is.EqualTo(45f));
            Assert.That(clone.Interstitial.DisabledPlacements, Has.Count.EqualTo(2));
        }

        [Test]
        public void MaxProfile_FollowsCompileTimeEnvironment()
        {
            _configuration = ScriptableObject.CreateInstance<AdsConfiguration>();

            var expected = AdsConfiguration.ResolveCompileTimeEnvironment() == AdsEnvironment.Test
                ? _configuration.MaxTest
                : _configuration.MaxProduction;
            Assert.That(_configuration.GetActiveMaxProfile(), Is.SameAs(expected));
            Assert.That(_configuration.MaxTest.EnableBanner, Is.True);
        }

        [Test]
        public void UnitIdOverride_EmptyFallsBack()
        {
            AdUnitIdOverrides.Clear();
            try
            {
                Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Banner, "fallback"), Is.EqualTo("fallback"));

                AdUnitIdOverrides.Set("max", "android", AdFormat.Banner, "remote");
                AdUnitIdOverrides.Set("max", "ios", AdFormat.Banner, "");

                Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Banner, "fallback"), Is.EqualTo("remote"));
                Assert.That(AdUnitIdOverrides.Resolve("MAX", RuntimePlatform.Android, AdFormat.Banner, "fallback"), Is.EqualTo("remote"));
                Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.IPhonePlayer, AdFormat.Banner, "fallback"), Is.EqualTo("fallback"));
                Assert.That(AdUnitIdOverrides.Resolve("max", RuntimePlatform.Android, AdFormat.Rewarded, "fallback"), Is.EqualTo("fallback"));
                Assert.That(AdUnitIdOverrides.Resolve("admob", RuntimePlatform.Android, AdFormat.Banner, "fallback"), Is.EqualTo("fallback"));
            }
            finally
            {
                AdUnitIdOverrides.Clear();
            }
        }

        private static AdsConfiguration LoadLegacy()
        {
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            JsonUtility.FromJsonOverwrite(LegacyJson, configuration);
            return configuration;
        }

        private static AdsConfiguration Reserialize(AdsConfiguration source)
        {
            var copy = ScriptableObject.CreateInstance<AdsConfiguration>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
            return copy;
        }
    }
}
