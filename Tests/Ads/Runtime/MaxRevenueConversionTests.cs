using NUnit.Framework;

namespace RippleGame.Ads.Tests
{
    public sealed class MaxRevenueConversionTests
    {
        [Test]
        public void ToMicros_RoundsAndClamps()
        {
            Assert.That(MaxRevenueConversion.ToMicros(0.0012345), Is.EqualTo(1235L));
            Assert.That(MaxRevenueConversion.ToMicros(-1), Is.EqualTo(0L));
            Assert.That(MaxRevenueConversion.ToMicros(double.NaN), Is.EqualTo(0L));
        }

        [Test]
        public void Create_SetsMaxPlatformAndUsd()
        {
            var revenue = MaxRevenueConversion.Create(0.5, "unit-1", "AppLovin");

            Assert.That(revenue.ValueMicros, Is.EqualTo(500000L));
            Assert.That(revenue.CurrencyCode, Is.EqualTo("USD"));
            Assert.That(revenue.Precision, Is.EqualTo(0));
            Assert.That(revenue.AdUnitId, Is.EqualTo("unit-1"));
            Assert.That(revenue.AdSourceName, Is.EqualTo("AppLovin"));
            Assert.That(revenue.Platform, Is.EqualTo(AdRevenue.PlatformMax));
        }
    }
}
