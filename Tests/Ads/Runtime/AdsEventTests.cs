using NUnit.Framework;

namespace RippleGame.Ads.Tests
{
    public sealed class AdsEventTests
    {
        [Test]
        public void AdsEvent_CarriesRevenue()
        {
            var adsEvent = new AdsEvent(AdsEventType.Paid, AdFormat.Rewarded, AdPlacement.DoubleReward,
                revenue: new AdRevenue(1500, "USD", 1, "unit", "AdMob Network"));

            Assert.That(adsEvent.Revenue.HasValue, Is.True);
            Assert.That(adsEvent.Revenue.Value.ValueMicros, Is.EqualTo(1500));
            Assert.That(adsEvent.Revenue.Value.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(adsEvent.Revenue.Value.AdSourceName, Is.EqualTo("AdMob Network"));
        }

        [Test]
        public void AdsEvent_DefaultRevenue_IsNull()
        {
            var adsEvent = new AdsEvent(AdsEventType.Shown, AdFormat.Interstitial, AdPlacement.LevelComplete);

            Assert.That(adsEvent.Revenue.HasValue, Is.False);
        }

        [Test]
        public void ExistingEventTypeValues_AreUnchanged()
        {
            Assert.That((int)AdsEventType.RewardEarned, Is.EqualTo(4));
            Assert.That((int)AdsEventType.Paid, Is.EqualTo(5));
            Assert.That((int)AdsEventType.ShowFailed, Is.EqualTo(6));
        }
    }
}
