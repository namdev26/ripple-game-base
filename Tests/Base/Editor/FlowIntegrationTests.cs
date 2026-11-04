using NUnit.Framework;
using RippleGame.Flow;
using RippleGame.FlowIntegration;
using UnityEngine;

namespace RippleGame.Base.Tests
{
    public sealed class FlowIntegrationTests
    {
        private const int TestLevel = 987654;
        private const string AttemptKey = "rg_analytics_attempt_987654";

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(AttemptKey);

        [Test]
        public void DisabledAds_AreNotReadyAndReportNotEarned()
        {
            var ads = new AdsLevelFlowAds();
            Assert.That(ads.IsRewardedReady, Is.False);

            bool? earned = null;
            ads.ShowRewarded(LevelAdPurpose.DoubleReward, value => earned = value);
            Assert.That(earned, Is.False);
        }

        [Test]
        public void DisabledAds_StillCompleteInterstitial()
        {
            var completed = false;
            new AdsLevelFlowAds().ShowInterstitial(() => completed = true);
            Assert.That(completed, Is.True);
        }

        [Test]
        public void LevelStarted_ReachesGameEvents()
        {
            PlayerPrefs.DeleteKey(AttemptKey);
            new AnalyticsLevelFlowEvents().LevelStarted(TestLevel);
            Assert.That(PlayerPrefs.GetInt(AttemptKey, 0), Is.EqualTo(1));
        }

        [Test]
        public void CompletedAndFailed_DoNotThrow()
        {
            var events = new AnalyticsLevelFlowEvents();
            Assert.DoesNotThrow(() => events.LevelCompleted(TestLevel, 5));
            Assert.DoesNotThrow(() => events.LevelFailed(TestLevel, "wall"));
        }
    }
}
