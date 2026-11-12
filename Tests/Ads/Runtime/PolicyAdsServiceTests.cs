using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Ads.Tests
{
    public sealed class PolicyAdsServiceTests
    {
        private FakeAdsService _inner;
        private AdsPolicySettings _settings;
        private float _now;
        private int _session;
        private float _savedTimeScale;
        private bool _savedAudioPause;
        private readonly List<PolicyAdsService> _created = new List<PolicyAdsService>();

        [SetUp]
        public void SetUp()
        {
            Ads.ResetForTests();
            _savedTimeScale = Time.timeScale;
            _savedAudioPause = AudioListener.pause;
            _inner = new FakeAdsService();
            _settings = new AdsPolicySettings();
            _now = 0f;
            _session = 1;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var service in _created) service.Dispose();
            _created.Clear();
            Ads.ResetForTests();
            Time.timeScale = _savedTimeScale;
            AudioListener.pause = _savedAudioPause;
        }

        // ---- Interstitial rules (spec 4.2) ----

        [Test]
        public void Interstitial_WithinCooldown_ReturnsCooldown_AndDoesNotCallInner()
        {
            var policy = Create();
            var first = new Results();
            var second = new Results();
            var third = new Results();

            policy.ShowInterstitial(AdPlacement.LevelComplete, first.Add);
            _inner.Close();
            _now = 10f;
            policy.ShowInterstitial(AdPlacement.LevelComplete, second.Add);

            Assert.That(first.Single, Is.EqualTo(AdsResult.Closed));
            Assert.That(second.Single, Is.EqualTo(AdsResult.Cooldown));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));

            _now = 45f;
            policy.ShowInterstitial(AdPlacement.LevelComplete, third.Add);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));
        }

        [Test]
        public void Interstitial_SessionCapReached_ReturnsCooldown()
        {
            _settings.Interstitial.CooldownSeconds = 0f;
            _settings.Interstitial.MaxPerSession = 2;
            var policy = Create();
            var capped = new Results();

            policy.ShowInterstitial(AdPlacement.Default);
            _inner.Fail(); // a failed show is not an impression and does not count
            ShowAndClose(policy);
            ShowAndClose(policy);
            policy.ShowInterstitial(AdPlacement.Default, capped.Add);

            Assert.That(capped.Single, Is.EqualTo(AdsResult.Cooldown));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(3));
        }

        [Test]
        public void Interstitial_ImpressionIsRecordedWhenProviderReportsShown()
        {
            var policy = Create();
            var second = new Results();

            policy.ShowInterstitial(AdPlacement.Default);
            _inner.Raise(new AdsEvent(AdsEventType.Shown, AdFormat.Interstitial, AdPlacement.Default));
            _now = 30f;
            _inner.Close(); // closing 30 s later must not restart the cooldown from the close time
            _now = 45f;
            policy.ShowInterstitial(AdPlacement.Default, second.Add);

            Assert.That(second.Count, Is.EqualTo(0), "The second show is still pending, so it was not blocked.");
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));
        }

        [Test]
        public void Interstitial_BelowMinLevel_ReturnsDisabled()
        {
            _settings.Interstitial.MinLevel = 3;
            var policy = Create();
            var result = new Results();

            Ads.SetPolicyContext(new AdsPolicyContext(2));
            policy.ShowInterstitial(AdPlacement.LevelComplete, result.Add);

            Assert.That(result.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));

            Ads.SetPolicyContext(new AdsPolicyContext(3));
            policy.ShowInterstitial(AdPlacement.LevelComplete);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
        }

        [Test]
        public void Interstitial_NoContext_IgnoresMinLevel_WarnsOnce()
        {
            _settings.Interstitial.MinLevel = 3;
            _settings.Interstitial.CooldownSeconds = 0f;
            var policy = Create();

            LogAssert.Expect(LogType.Warning, new Regex(@"\[RippleGame Ads\].*SetPolicyContext"));
            ShowAndClose(policy);
            ShowAndClose(policy);

            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Interstitial_BeforeStartSession_ReturnsDisabled()
        {
            _settings.Interstitial.StartSession = 2;
            var policy = Create();
            var result = new Results();

            policy.ShowInterstitial(AdPlacement.Default, result.Add);

            Assert.That(result.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));

            _session = 2;
            policy.ShowInterstitial(AdPlacement.Default);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
        }

        [Test]
        public void Interstitial_DisabledPlacement_ReturnsDisabled()
        {
            _settings.Interstitial.DisabledPlacements.Add(AdPlacement.Pause);
            var policy = Create();
            var result = new Results();

            policy.ShowInterstitial(AdPlacement.Pause, result.Add);

            Assert.That(result.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));

            policy.ShowInterstitial(AdPlacement.LevelComplete);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
        }

        [Test]
        public void Interstitial_OrganicBlocked_ReturnsDisabled()
        {
            _settings.Organic.Interstitial = false;
            var policy = Create();
            var result = new Results();

            policy.SetOrganic(true);
            policy.ShowInterstitial(AdPlacement.Default, result.Add);

            Assert.That(result.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));
            Assert.That(policy.IsReady(AdFormat.Interstitial), Is.False);

            policy.SetOrganic(false);
            policy.ShowInterstitial(AdPlacement.Default);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
        }

        // ---- Entitlements and kill switch ----

        [Test]
        public void RemoveAds_BlocksBannerMrecInterstitialAppOpen_NotRewarded()
        {
            var policy = Create();
            var interstitial = new Results();
            var appOpen = new Results();
            var rewardedResult = new Results();
            var rewards = 0;

            Ads.SetPolicyContext(new AdsPolicyContext(10, AdsEntitlement.RemoveAds));
            policy.ShowBanner(BannerPosition.Bottom);
            policy.ShowMrec(MrecPosition.Center);
            policy.ShowInterstitial(AdPlacement.LevelComplete, interstitial.Add);
            policy.ShowAppOpen(appOpen.Add);

            Assert.That(interstitial.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(appOpen.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowBanner)), Is.EqualTo(0));
            Assert.That(_inner.Count(nameof(IAdsService.ShowMrec)), Is.EqualTo(0));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(0));
            Assert.That(policy.IsReady(AdFormat.Banner), Is.False);
            Assert.That(policy.IsReady(AdFormat.Interstitial), Is.False);

            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, rewardedResult.Add);
            _inner.Reward();
            _inner.Close();

            Assert.That(_inner.Count(nameof(IAdsService.ShowRewarded)), Is.EqualTo(1));
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(rewardedResult.Single, Is.EqualTo(AdsResult.Closed));
        }

        [Test]
        public void FreeRewards_GrantsRewardWithoutCallingInner_ReturnsSuccess()
        {
            _inner.Ready[AdFormat.Rewarded] = false;
            var policy = Create();
            var result = new Results();
            var rewards = 0;

            Ads.SetPolicyContext(new AdsPolicyContext(1, AdsEntitlement.RemoveAdsAndFreeRewards));
            Assert.That(policy.IsReady(AdFormat.Rewarded), Is.True);
            policy.ShowRewarded(AdPlacement.ExtraLife, () => rewards++, result.Add);

            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(result.Single, Is.EqualTo(AdsResult.Success));
            Assert.That(_inner.Calls.Where(c => c.StartsWith("Show")), Is.Empty);
        }

        [Test]
        public void AdsDisabled_RewardedGrantsImmediately_InterstitialDisabled()
        {
            var policy = Create();
            var rewardedResult = new Results();
            var interstitial = new Results();
            var appOpen = new Results();
            var rewards = 0;

            policy.SetAdsEnabled(false);
            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, rewardedResult.Add);
            policy.ShowInterstitial(AdPlacement.LevelComplete, interstitial.Add);
            policy.ShowAppOpen(appOpen.Add);
            policy.ShowBanner(BannerPosition.Bottom);
            policy.ShowMrec(MrecPosition.Center);

            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(rewardedResult.Single, Is.EqualTo(AdsResult.Success));
            Assert.That(interstitial.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(appOpen.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Calls.Where(c => c.StartsWith("Show")), Is.Empty);
            Assert.That(_inner.Count(nameof(IAdsService.HideBanner)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.HideMrec)), Is.EqualTo(1));

            policy.SetAdsEnabled(true);
            policy.ShowInterstitial(AdPlacement.LevelComplete);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
        }

        // ---- Rewarded fallback (spec 4.3) ----

        [Test]
        public void Fallback_Off_RewardedNotReady_ReturnsNotReady()
        {
            _inner.Ready[AdFormat.Rewarded] = false;
            var policy = Create();
            var result = new Results();
            var rewards = 0;

            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, result.Add);

            Assert.That(result.Single, Is.EqualTo(AdsResult.NotReady));
            Assert.That(rewards, Is.EqualTo(0));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));
            Assert.That(policy.IsReady(AdFormat.Rewarded), Is.False);
        }

        [Test]
        public void Fallback_On_InterClosed_GrantsOnce_Success_EventMessageFallback_ResetsCooldown()
        {
            _settings.RewardedFallbackEnabled = true;
            _inner.Ready[AdFormat.Rewarded] = false;
            var policy = Create();
            var events = new List<AdsEvent>();
            policy.EventRaised += events.Add;
            var result = new Results();
            var next = new Results();
            var rewards = 0;

            Assert.That(policy.IsReady(AdFormat.Rewarded), Is.True);
            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, result.Add);

            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.ShowRewarded)), Is.EqualTo(0));
            Assert.That(_inner.LastPlacement, Is.EqualTo(AdPlacement.RewardedFallback));

            _now = 5f;
            _inner.Close();
            _inner.Close();

            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(result.Single, Is.EqualTo(AdsResult.Success));
            var earned = events.Single(e => e.Type == AdsEventType.RewardEarned);
            Assert.That(earned.Message, Is.EqualTo("fallback_interstitial"));
            Assert.That(earned.Placement, Is.EqualTo(AdPlacement.DoubleReward));

            _now = 49f; // 44 s after the fallback closed
            policy.ShowInterstitial(AdPlacement.LevelComplete, next.Add);
            Assert.That(next.Single, Is.EqualTo(AdsResult.Cooldown));
        }

        [Test]
        public void Fallback_On_InterFailed_NoReward_Failed()
        {
            _settings.RewardedFallbackEnabled = true;
            _inner.Ready[AdFormat.Rewarded] = false;
            var policy = Create();
            var events = new List<AdsEvent>();
            policy.EventRaised += events.Add;
            var result = new Results();
            var rewards = 0;

            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, result.Add);
            _inner.Fail();

            Assert.That(rewards, Is.EqualTo(0));
            Assert.That(result.Single, Is.EqualTo(AdsResult.Failed));
            Assert.That(events.Any(e => e.Type == AdsEventType.RewardEarned), Is.False);
        }

        [Test]
        public void Fallback_IgnoresCooldownAndMinLevel_ButRespectsOrganicAndPlacement()
        {
            _settings.RewardedFallbackEnabled = true;
            _settings.Interstitial.MinLevel = 5;
            _settings.Organic.Interstitial = false;
            _inner.Ready[AdFormat.Rewarded] = false;
            Ads.SetPolicyContext(new AdsPolicyContext(1));
            var policy = Create();
            var normal = new Results();
            var first = new Results();
            var second = new Results();
            var organic = new Results();

            policy.ShowInterstitial(AdPlacement.LevelComplete, normal.Add);
            Assert.That(normal.Single, Is.EqualTo(AdsResult.Disabled), "Min level blocks a normal interstitial.");

            policy.ShowRewarded(AdPlacement.DoubleReward, null, first.Add);
            _inner.Close();
            _now = 1f; // well inside the 45 s cooldown the fallback just reset
            policy.ShowRewarded(AdPlacement.DoubleReward, null, second.Add);
            _inner.Close();

            Assert.That(first.Single, Is.EqualTo(AdsResult.Success));
            Assert.That(second.Single, Is.EqualTo(AdsResult.Success));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));

            policy.SetOrganic(true);
            policy.ShowRewarded(AdPlacement.DoubleReward, null, organic.Add);
            Assert.That(organic.Single, Is.EqualTo(AdsResult.NotReady));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));

            var placementSettings = new AdsPolicySettings { RewardedFallbackEnabled = true };
            placementSettings.Interstitial.DisabledPlacements.Add(AdPlacement.RewardedFallback);
            var placementPolicy = Create(placementSettings);
            var placement = new Results();

            placementPolicy.ShowRewarded(AdPlacement.DoubleReward, null, placement.Add);
            Assert.That(placement.Single, Is.EqualTo(AdsResult.NotReady));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));
        }

        [Test]
        public void Fallback_RewardIsNeverCarriedToNextInterstitial()
        {
            _settings.RewardedFallbackEnabled = true;
            _settings.Interstitial.CooldownSeconds = 0f;
            _inner.Ready[AdFormat.Rewarded] = false;
            var policy = Create();
            var fallback = new Results();
            var normal = new Results();
            var rewards = 0;

            policy.ShowRewarded(AdPlacement.DoubleReward, () => rewards++, fallback.Add);
            _inner.Fail();
            policy.ShowInterstitial(AdPlacement.LevelComplete, normal.Add);
            _inner.Close();

            Assert.That(fallback.Single, Is.EqualTo(AdsResult.Failed));
            Assert.That(normal.Single, Is.EqualTo(AdsResult.Closed));
            Assert.That(rewards, Is.EqualTo(0));
        }

        // ---- Full-screen exclusivity, entitlement change, snapshot, pause ----

        [Test]
        public void DoubleShowRewarded_SecondGetsAlreadyShowing_RewardOnce()
        {
            var policy = Create();
            var first = new Results();
            var second = new Results();
            var interstitial = new Results();
            var appOpen = new Results();
            var firstRewards = 0;
            var secondRewards = 0;

            policy.ShowRewarded(AdPlacement.DoubleReward, () => firstRewards++, first.Add);
            policy.ShowRewarded(AdPlacement.DoubleReward, () => secondRewards++, second.Add);
            policy.ShowInterstitial(AdPlacement.Default, interstitial.Add);
            policy.ShowAppOpen(appOpen.Add);

            Assert.That(second.Single, Is.EqualTo(AdsResult.AlreadyShowing));
            Assert.That(interstitial.Single, Is.EqualTo(AdsResult.AlreadyShowing));
            Assert.That(appOpen.Single, Is.EqualTo(AdsResult.AlreadyShowing));
            Assert.That(_inner.Count(nameof(IAdsService.ShowRewarded)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(0));
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(0));

            _inner.Reward();
            _inner.Reward();
            _inner.Close();
            _inner.Reward();

            Assert.That(firstRewards, Is.EqualTo(1));
            Assert.That(secondRewards, Is.EqualTo(0));
            Assert.That(first.Single, Is.EqualTo(AdsResult.Closed));
        }

        [Test]
        public void EntitlementChangedToRemoveAds_HidesBannerAndMrecImmediately()
        {
            var policy = Create();

            policy.ShowBanner(BannerPosition.Bottom);
            policy.ShowMrec(MrecPosition.BottomCenter);
            Ads.SetPolicyContext(new AdsPolicyContext(5, AdsEntitlement.RemoveAds));
            policy.ShowBanner(BannerPosition.Bottom);

            Assert.That(_inner.Count(nameof(IAdsService.ShowBanner)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.ShowMrec)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.HideBanner)), Is.EqualTo(1));
            Assert.That(_inner.Count(nameof(IAdsService.HideMrec)), Is.EqualTo(1));
        }

        [Test]
        public void OverrideAfterSnapshot_IsIgnoredThisSession()
        {
            var policy = Create();

            policy.SetOverride(new AdsPolicyOverride { InterstitialCooldownSeconds = 0f });
            ShowAndClose(policy);
            policy.SetOverride(new AdsPolicyOverride { InterstitialCooldownSeconds = 100f });
            _now = 1f;
            ShowAndClose(policy);

            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(2));
            Assert.That(_settings.Interstitial.CooldownSeconds, Is.EqualTo(45f), "The configuration asset is never modified.");
        }

        [Test]
        public void Snapshot_IsTakenWhenInitializeCompletes()
        {
            var policy = Create();
            var second = new Results();

            var initialization = policy.InitializeAsync();
            Assert.That(initialization.IsCompleted, Is.True);
            Assert.That(initialization.Result, Is.EqualTo(AdsResult.Success));

            policy.SetOverride(new AdsPolicyOverride { InterstitialCooldownSeconds = 0f });
            ShowAndClose(policy);
            policy.ShowInterstitial(AdPlacement.Default, second.Add);

            Assert.That(second.Single, Is.EqualTo(AdsResult.Cooldown));
        }

        [Test]
        public void PauseTimeAndAudio_RestoresPreviousValuesOnClose()
        {
            var policy = Create();

            Time.timeScale = 0.5f;
            AudioListener.pause = false;
            policy.ShowInterstitial(AdPlacement.Default);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(AudioListener.pause, Is.True);
            _inner.Close();
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            Assert.That(AudioListener.pause, Is.False);

            Time.timeScale = 0.7f;
            AudioListener.pause = true;
            policy.ShowRewarded(AdPlacement.DoubleReward, null);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            _inner.Fail();
            Assert.That(Time.timeScale, Is.EqualTo(0.7f));
            Assert.That(AudioListener.pause, Is.True);
        }

        [Test]
        public void PauseTimeAndAudio_Off_LeavesTimeAndAudioAlone()
        {
            _settings.PauseTimeAndAudio = false;
            var policy = Create();

            Time.timeScale = 0.5f;
            AudioListener.pause = false;
            policy.ShowInterstitial(AdPlacement.Default);

            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            Assert.That(AudioListener.pause, Is.False);
            _inner.Close();
        }

        // ---- App open (rules moved from AdMobAdsService) ----

        [Test]
        public void AppOpen_WithinCooldown_ReturnsCooldown()
        {
            var policy = Create();
            var second = new Results();

            policy.ShowAppOpen();
            _inner.Close();
            _now = 5f;
            policy.ShowAppOpen(second.Add);

            Assert.That(second.Single, Is.EqualTo(AdsResult.Cooldown));
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(1));
        }

        [Test]
        public void AppOpen_DisabledInPolicy_OrBlockedForOrganic_ReturnsDisabled()
        {
            _settings.AppOpen.Enabled = false;
            var disabledPolicy = Create();
            var disabled = new Results();
            disabledPolicy.ShowAppOpen(disabled.Add);

            var organicSettings = new AdsPolicySettings();
            organicSettings.Organic.AppOpen = false;
            var organicPolicy = Create(organicSettings);
            organicPolicy.SetOrganic(true);
            var organic = new Results();
            organicPolicy.ShowAppOpen(organic.Add);

            Assert.That(disabled.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(organic.Single, Is.EqualTo(AdsResult.Disabled));
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(0));
        }

        [Test]
        public void AppOpen_OnResume_RespectsMinBackgroundAndCooldown()
        {
            var policy = Create();
            IAdsLifecycleAware lifecycle = policy;

            // Background for 2 s (< 5 s minimum): nothing.
            lifecycle.OnApplicationPauseChanged(true);
            _now = 2f;
            lifecycle.OnApplicationFocusChanged(true);
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(0));

            // Background for 6 s: shows.
            lifecycle.OnApplicationPauseChanged(true);
            _now = 8f;
            lifecycle.OnApplicationFocusChanged(true);
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(1));
            _inner.Close();

            // Back again only 12 s after that ad closed (< 20 s cooldown): nothing.
            lifecycle.OnApplicationPauseChanged(true);
            _now = 20f;
            lifecycle.OnApplicationFocusChanged(true);
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(1));

            // A full-screen ad pauses the app; returning from it must not trigger an app open.
            _now = 100f;
            policy.ShowInterstitial(AdPlacement.Default);
            lifecycle.OnApplicationPauseChanged(true);
            _now = 130f;
            lifecycle.OnApplicationFocusChanged(true);
            _inner.Close();
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(1));

            // Not ready: nothing.
            _inner.Ready[AdFormat.AppOpen] = false;
            lifecycle.OnApplicationPauseChanged(true);
            _now = 200f;
            lifecycle.OnApplicationFocusChanged(true);
            Assert.That(_inner.Count(nameof(IAdsService.ShowAppOpen)), Is.EqualTo(1));
        }

        // ---- Plumbing ----

        [Test]
        public void ProviderEvents_AreForwarded_AndDisposeDisposesProvider()
        {
            var policy = Create();
            var events = new List<AdsEvent>();
            policy.EventRaised += events.Add;

            _inner.Raise(new AdsEvent(AdsEventType.Loaded, AdFormat.Banner, AdPlacement.Default));
            policy.Dispose();

            Assert.That(events.Single().Type, Is.EqualTo(AdsEventType.Loaded));
            Assert.That(_inner.Disposed, Is.True);
        }

        [Test]
        public void OverrideApplyTo_ReturnsNewInstance_WithOnlyPresentFieldsChanged()
        {
            var source = new AdsPolicySettings();
            source.Interstitial.DisabledPlacements.Add(AdPlacement.Replay);
            var value = new AdsPolicyOverride
            {
                InterstitialMaxPerSession = 0,
                AppOpenEnabled = false,
                OrganicInterstitial = false,
                BannerRefreshSeconds = 15f,
                PlacementEnabled = new Dictionary<AdPlacement, bool> { { AdPlacement.Pause, false }, { AdPlacement.Replay, true } }
            };

            var result = value.ApplyTo(source);

            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Interstitial.MaxPerSession, Is.EqualTo(0));
            Assert.That(result.Interstitial.CooldownSeconds, Is.EqualTo(45f));
            Assert.That(result.AppOpen.Enabled, Is.False);
            Assert.That(result.Organic.Interstitial, Is.False);
            Assert.That(result.Organic.Banner, Is.True);
            Assert.That(result.BannerRefreshSeconds, Is.EqualTo(15f));
            Assert.That(result.Interstitial.DisabledPlacements, Is.EquivalentTo(new[] { AdPlacement.Pause }));
            Assert.That(source.Interstitial.DisabledPlacements, Is.EquivalentTo(new[] { AdPlacement.Replay }));
            Assert.That(source.Interstitial.MaxPerSession, Is.EqualTo(4));
        }

        private PolicyAdsService Create(AdsPolicySettings settings = null)
        {
            var policy = new PolicyAdsService(_inner, settings ?? _settings, () => _now, () => _session);
            _created.Add(policy);
            return policy;
        }

        private void ShowAndClose(PolicyAdsService policy)
        {
            var before = _inner.Count(nameof(IAdsService.ShowInterstitial));
            policy.ShowInterstitial(AdPlacement.Default);
            Assert.That(_inner.Count(nameof(IAdsService.ShowInterstitial)), Is.EqualTo(before + 1), "Expected the interstitial to reach the provider.");
            _inner.Close();
        }

        /// <summary>Collects completion results so tests can assert the callback ran exactly once.</summary>
        private sealed class Results
        {
            private readonly List<AdsResult> _values = new List<AdsResult>();
            public void Add(AdsResult result) => _values.Add(result);
            public int Count => _values.Count;

            public AdsResult Single
            {
                get
                {
                    Assert.That(_values, Has.Count.EqualTo(1), "completed must be invoked exactly once.");
                    return _values[0];
                }
            }
        }
    }
}
