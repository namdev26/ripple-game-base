using NUnit.Framework;
using UnityEngine;

namespace RippleGame.Ads.Tests
{
    public sealed class AdsBannerPositionTests
    {
        private AdsConfiguration _configuration;

        [TearDown]
        public void TearDown()
        {
            if (_configuration != null) Object.DestroyImmediate(_configuration);
        }

        [Test]
        public void MaxProvider_UsesMaxProfileBannerPosition()
        {
            _configuration = Configuration("max", admobPosition: BannerPosition.Bottom, maxPosition: BannerPosition.Top);

            Assert.That(_configuration.GetActiveBannerPosition(), Is.EqualTo(BannerPosition.Top));
        }

        [Test]
        public void AdMobProvider_UsesAdMobProfileBannerPosition()
        {
            _configuration = Configuration("admob", admobPosition: BannerPosition.Top, maxPosition: BannerPosition.Bottom);

            Assert.That(_configuration.GetActiveBannerPosition(), Is.EqualTo(BannerPosition.Top));
        }

        [Test]
        public void NoneProvider_HasNoBanner()
        {
            _configuration = Configuration("none", admobPosition: BannerPosition.Top, maxPosition: BannerPosition.Top);

            Assert.That(_configuration.GetActiveBannerPosition(), Is.Null);
        }

        private static AdsConfiguration Configuration(string providerId, BannerPosition admobPosition, BannerPosition maxPosition)
        {
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            var admob = "{\"bannerPosition\":" + (int)admobPosition + "}";
            var max = "{\"bannerPosition\":" + (int)maxPosition + "}";
            JsonUtility.FromJsonOverwrite(
                "{\"providerId\":\"" + providerId + "\",\"test\":" + admob + ",\"production\":" + admob +
                ",\"maxTest\":" + max + ",\"maxProduction\":" + max + "}",
                configuration);
            return configuration;
        }
    }
}
