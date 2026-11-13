using System.Collections.Generic;
using NUnit.Framework;

namespace RippleGame.Ads.Editor.Tests
{
    public sealed class AdsBuildProfileTests
    {
        [Test]
        public void ApplyProfile_RemovesLegacyAdsSymbol()
        {
            foreach (var profile in new[] { AndroidAdsBuildProfile.NoAds, AndroidAdsBuildProfile.AdsTest, AndroidAdsBuildProfile.AdsProduction })
            {
                var result = AdsBuildProfileUtility.ApplyProfile("ADS;ADS_TEST;ZETA", profile);

                Assert.That(Symbols(result), Does.Not.Contain("ADS"), profile.ToString());
                Assert.That(Symbols(result), Does.Contain("ZETA"), profile.ToString());
            }
        }

        [Test]
        public void ApplyProfile_NoAds_SetsAdsDisabled()
        {
            var result = AdsBuildProfileUtility.ApplyProfile("ADS;ADS_TEST;ZETA", AndroidAdsBuildProfile.NoAds);

            Assert.That(result, Is.EqualTo("ADS_DISABLED;ZETA"));
        }

        [Test]
        public void ApplyProfile_AdsTest_SetsAdsTestAndClearsDisabled()
        {
            var result = AdsBuildProfileUtility.ApplyProfile("ADS_DISABLED;ZETA", AndroidAdsBuildProfile.AdsTest);

            Assert.That(result, Is.EqualTo("ADS_TEST;ZETA"));
        }

        [Test]
        public void ApplyProfile_AdsProduction_ClearsBothProfileSymbols()
        {
            var result = AdsBuildProfileUtility.ApplyProfile("ADS_DISABLED;ADS_TEST;ZETA", AndroidAdsBuildProfile.AdsProduction);

            Assert.That(result, Is.EqualTo("ZETA"));
        }

        [Test]
        public void ApplyProfile_IsIdempotent()
        {
            var once = AdsBuildProfileUtility.ApplyProfile("A", AndroidAdsBuildProfile.NoAds);

            Assert.That(AdsBuildProfileUtility.ApplyProfile(once, AndroidAdsBuildProfile.NoAds), Is.EqualTo(once));
            Assert.That(AdsBuildProfileUtility.ApplyProfile(string.Empty, AndroidAdsBuildProfile.AdsProduction), Is.Empty);
        }

        [Test]
        public void ResolveEnvironment_DisabledWinsOverTest()
        {
            Assert.That(AdsBuildProfileUtility.ResolveEnvironment(Symbols("ADS_DISABLED;ADS_TEST")), Is.EqualTo(AdsEnvironment.Disabled));
            Assert.That(AdsBuildProfileUtility.ResolveEnvironment(Symbols("ADS_TEST")), Is.EqualTo(AdsEnvironment.Test));
            Assert.That(AdsBuildProfileUtility.ResolveEnvironment(Symbols("OTHER")), Is.EqualTo(AdsEnvironment.Production));
        }

        private static HashSet<string> Symbols(string defineSymbols) => AdsBuildProfileUtility.Parse(defineSymbols);
    }
}
