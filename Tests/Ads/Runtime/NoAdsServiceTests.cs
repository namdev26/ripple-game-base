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
        }
    }
}
