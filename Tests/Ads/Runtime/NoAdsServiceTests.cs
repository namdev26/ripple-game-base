using NUnit.Framework;

namespace RippleGame.Ads.Tests
{
    public sealed class NoAdsServiceTests
    {
        [Test]
        public void RewardedInNoAdsBuild_NeverGrantsReward_AndReturnsDisabled()
        {
            var service = new NoAdsService();
            var rewarded = false;
            AdsResult? result = null;

            service.ShowRewarded(AdPlacement.DoubleReward, () => rewarded = true, value => result = value);

            Assert.That(rewarded, Is.False);
            Assert.That(result, Is.EqualTo(AdsResult.Disabled));
        }

        [Test]
        public void NoAdsService_IsNeverReady()
        {
            var service = new NoAdsService();

            Assert.That(service.IsReady(AdFormat.Banner), Is.False);
            Assert.That(service.IsReady(AdFormat.Interstitial), Is.False);
            Assert.That(service.IsReady(AdFormat.Rewarded), Is.False);
            Assert.That(service.IsReady(AdFormat.AppOpen), Is.False);
            Assert.That(service.IsReady(AdFormat.Mrec), Is.False);
        }

        [Test]
        public void NoAdsService_ShowMrec_RaisesFailedForMrec()
        {
            var service = new NoAdsService();
            AdsEvent? raised = null;
            service.EventRaised += e => raised = e;

            service.ShowMrec(MrecPosition.Center);
            service.HideMrec();

            Assert.That(raised.HasValue, Is.True);
            Assert.That(raised.Value.Type, Is.EqualTo(AdsEventType.Failed));
            Assert.That(raised.Value.Format, Is.EqualTo(AdFormat.Mrec));
        }
    }
}
