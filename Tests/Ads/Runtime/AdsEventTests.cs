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

        [Test]
        public void ExistingPlacementValues_AreUnchanged()
        {
            Assert.That((int)AdPlacement.ExtraLife, Is.EqualTo(6));
            Assert.That((int)AdPlacement.Pause, Is.EqualTo(7));
            Assert.That((int)AdPlacement.Replay, Is.EqualTo(8));
            Assert.That((int)AdPlacement.RewardedFallback, Is.EqualTo(9));
            Assert.That((int)AdFormat.AppOpen, Is.EqualTo(3));
            Assert.That((int)AdFormat.Mrec, Is.EqualTo(4));
        }

        [Test]
        public void FiveArgRevenue_DefaultsToAdMobPlatform()
        {
            Assert.That(new AdRevenue(1, "USD", 1, "u", "s").Platform, Is.EqualTo("admob"));
        }

        [Test]
        public void SixArgRevenue_CarriesPlatform()
        {
            var revenue = new AdRevenue(1, "USD", 1, "u", "s", AdRevenue.PlatformMax);

            Assert.That(revenue.Platform, Is.EqualTo("applovin_max"));
        }

        [Test]
        public void PolicyContext_DefaultsToNoEntitlementAndUnknownConsent()
        {
            var context = new AdsPolicyContext(7);

            Assert.That(context.CurrentLevel, Is.EqualTo(7));
            Assert.That(context.Entitlement, Is.EqualTo(AdsEntitlement.None));
            Assert.That(context.HasUserConsent, Is.Null);
        }
    }
}
