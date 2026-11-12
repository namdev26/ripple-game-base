using NUnit.Framework;
using RippleGame.Sdk.Editor;
using UnityEditor;
using UnityEngine;

namespace RippleGame.Ads.Editor.Tests
{
    public sealed class AdsPreBuildValidatorTests
    {
        private const string SetupMessage =
            "Ads provider '{0}' is selected but its SDK is not installed at the pinned version. Open RIPPLE Game > Open Hub > SDK and press Setup or Fix.";

        private AdsConfiguration _configuration;

        [TearDown]
        public void TearDown()
        {
            if (_configuration != null) Object.DestroyImmediate(_configuration);
        }

        // ProviderBuildCheck

        [Test]
        public void Validate_MaxSelectedNotCompiled_ReturnsSetupMessage()
        {
            var error = ProviderBuildCheck.Validate("max", _ => false, Status, BuildTarget.Android);

            Assert.That(error, Is.EqualTo(string.Format(SetupMessage, "max")));
        }

        [Test]
        public void Validate_AdMobSelectedNotCompiled_ReturnsSetupMessage()
        {
            var error = ProviderBuildCheck.Validate("admob", _ => false, Status, BuildTarget.Android);

            Assert.That(error, Is.EqualTo(string.Format(SetupMessage, "admob")));
        }

        [Test]
        public void Validate_LooksUpProvidersByTheirFactoryTypeName()
        {
            string asked = null;
            ProviderBuildCheck.Validate("max", name => { asked = name; return true; }, Status, BuildTarget.Android);
            Assert.That(asked, Is.EqualTo("RippleGame.Ads.Max.MaxProviderFactory, RippleGame.Ads.Max"));

            ProviderBuildCheck.Validate("admob", name => { asked = name; return true; }, Status, BuildTarget.Android);
            Assert.That(asked, Is.EqualTo("RippleGame.Ads.AdMob.AdMobProviderFactory, RippleGame.Ads.AdMob"));
        }

        [Test]
        public void Validate_NoneSelected_Passes()
        {
            Assert.That(ProviderBuildCheck.Validate("none", _ => false, Status, BuildTarget.StandaloneWindows64), Is.Null);
            Assert.That(ProviderBuildCheck.Validate(null, _ => false, Status, BuildTarget.iOS), Is.Null);
        }

        [Test]
        public void Validate_UnknownProvider_Passes()
        {
            Assert.That(ProviderBuildCheck.Validate("custom-network", _ => false, Status, BuildTarget.Android), Is.Null);
        }

        [Test]
        public void Validate_AdMobOnAndroid_Passes()
        {
            Assert.That(ProviderBuildCheck.Validate("admob", _ => true, Status, BuildTarget.Android), Is.Null);
        }

        [Test]
        public void Validate_AdMobOnIos_Fails()
        {
            Assert.That(ProviderBuildCheck.Validate("admob", _ => true, Status, BuildTarget.iOS), Is.Not.Null.And.Contains("AdMob"));
        }

        [Test]
        public void Validate_MaxOnIos_Passes()
        {
            Assert.That(ProviderBuildCheck.Validate("max", _ => true, Status, BuildTarget.iOS), Is.Null);
        }

        [Test]
        public void Validate_MaxOnDesktop_Fails()
        {
            Assert.That(ProviderBuildCheck.Validate("max", _ => true, Status, BuildTarget.StandaloneWindows64), Is.Not.Null.And.Contains("MAX"));
        }

        // Per-profile ID rules

        [Test]
        public void AdMob_EmptyProfile_IsRejected()
        {
            var error = AdsPreBuildValidator.ValidateAdMobProfile(new AdMobAdUnitConfiguration(), "Test");

            Assert.That(error, Does.StartWith("Test AdsConfiguration is missing: Android App ID"));
        }

        [Test]
        public void AdMob_MissingProfile_IsRejected()
        {
            Assert.That(AdsPreBuildValidator.ValidateAdMobProfile(null, "Production"), Is.EqualTo("Production profile is missing."));
        }

        [Test]
        public void AdMob_ProductionWithGoogleDemoId_IsRejected()
        {
            _configuration = Configuration(
                "{\"providerId\":\"admob\",\"production\":{\"androidAppId\":\"ca-app-pub-3940256099942544~1\",\"bannerUnitId\":\"b\"," +
                "\"interstitialUnitId\":\"i\",\"rewardedUnitId\":\"r\",\"appOpenUnitId\":\"a\"}}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => true, Status);

            Assert.That(error, Is.EqualTo("Production AdsConfiguration contains a Google demo ID."));
        }

        [Test]
        public void AdMob_TestReusingProductionId_IsRejected()
        {
            _configuration = Configuration(
                "{\"providerId\":\"admob\",\"test\":{\"androidAppId\":\"app\",\"bannerUnitId\":\"shared\",\"interstitialUnitId\":\"i1\"," +
                "\"rewardedUnitId\":\"r1\",\"appOpenUnitId\":\"a1\"},\"production\":{\"androidAppId\":\"app2\",\"bannerUnitId\":\"shared\"," +
                "\"interstitialUnitId\":\"i2\",\"rewardedUnitId\":\"r2\",\"appOpenUnitId\":\"a2\"}}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Test, BuildTarget.Android, _ => true, Status);

            Assert.That(error, Is.EqualTo("Test AdsConfiguration reuses an ID from Production. Use Google demo/test IDs instead."));
        }

        [Test]
        public void AdMob_TestWithDemoIdsAndBlankDisabledFormats_Passes()
        {
            // Disabled formats may be blank in both profiles without counting as a shared ID.
            _configuration = Configuration(
                "{\"providerId\":\"admob\",\"test\":{\"androidAppId\":\"app\",\"enableBanner\":false,\"enableInterstitial\":true," +
                "\"interstitialUnitId\":\"i1\",\"enableRewarded\":false,\"enableAppOpen\":false}," +
                "\"production\":{\"androidAppId\":\"app2\",\"enableBanner\":false,\"enableInterstitial\":true,\"interstitialUnitId\":\"i2\"," +
                "\"enableRewarded\":false,\"enableAppOpen\":false}}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Test, BuildTarget.Android, _ => true, Status);

            Assert.That(error, Is.Null);
        }

        [Test]
        public void AdMob_ValidProduction_Passes()
        {
            _configuration = Configuration(
                "{\"providerId\":\"admob\",\"production\":{\"androidAppId\":\"app\",\"bannerUnitId\":\"b\",\"interstitialUnitId\":\"i\"," +
                "\"rewardedUnitId\":\"r\",\"appOpenUnitId\":\"a\"}}");

            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => true, Status), Is.Null);
        }

        [Test]
        public void Max_ProductionWithEmptyEnabledFormats_IsRejected()
        {
            _configuration = Configuration("{\"providerId\":\"max\",\"maxProduction\":{\"sdkKey\":\"key\",\"android\":{\"banner\":\"b\"}}}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => true, Status);

            Assert.That(error, Is.EqualTo("Production AdsConfiguration (MAX) is missing: Interstitial unit ID, Rewarded unit ID, App Open unit ID, MREC unit ID."));
        }

        [Test]
        public void Max_ProductionChecksTheTargetPlatformIds()
        {
            _configuration = Configuration(
                "{\"providerId\":\"max\",\"maxProduction\":{\"sdkKey\":\"key\",\"enableMrec\":false,\"android\":{\"banner\":\"b\",\"interstitial\":\"i\"," +
                "\"rewarded\":\"r\",\"appOpen\":\"a\"}}}");

            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => true, Status), Is.Null);
            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.iOS, _ => true, Status),
                Does.StartWith("Production AdsConfiguration (MAX) is missing: Banner unit ID"));
        }

        [Test]
        public void Max_ProductionWithoutSdkKey_IsRejected()
        {
            _configuration = Configuration(
                "{\"providerId\":\"max\",\"maxProduction\":{\"enableBanner\":false,\"enableInterstitial\":false,\"enableRewarded\":false," +
                "\"enableAppOpen\":false,\"enableMrec\":false}}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => true, Status);

            Assert.That(error, Is.EqualTo("Production AdsConfiguration (MAX) is missing: MAX SDK key."));
        }

        [Test]
        public void Max_TestProfileMayBeEmpty()
        {
            _configuration = Configuration("{\"providerId\":\"max\"}");

            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Test, BuildTarget.Android, _ => true, Status), Is.Null);
        }

        // Entry point

        [Test]
        public void GetBuildError_NoAdsProfile_SkipsProviderChecks()
        {
            _configuration = Configuration("{\"providerId\":\"max\"}");

            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Disabled, BuildTarget.StandaloneWindows64, _ => false, Status), Is.Null);
        }

        [Test]
        public void GetBuildError_NoneProvider_SkipsIdChecks()
        {
            _configuration = Configuration("{\"providerId\":\"none\"}");

            Assert.That(AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => false, Status), Is.Null);
        }

        [Test]
        public void GetBuildError_NoConfiguration_Passes()
        {
            Assert.That(AdsPreBuildValidator.GetBuildError(null, AdsEnvironment.Production, BuildTarget.Android, _ => false, Status), Is.Null);
        }

        [Test]
        public void GetBuildError_ProviderNotCompiled_ReportsSetupBeforeIdRules()
        {
            _configuration = Configuration("{\"providerId\":\"admob\"}");

            var error = AdsPreBuildValidator.GetBuildError(_configuration, AdsEnvironment.Production, BuildTarget.Android, _ => false, Status);

            Assert.That(error, Is.EqualTo(string.Format(SetupMessage, "admob")));
        }

        [Test]
        public void ResolveCompileTimeEnvironment_FollowsTheProfileSymbols()
        {
#if ADS_DISABLED
            Assert.That(AdsConfiguration.ResolveCompileTimeEnvironment(), Is.EqualTo(AdsEnvironment.Disabled));
#elif ADS_TEST
            Assert.That(AdsConfiguration.ResolveCompileTimeEnvironment(), Is.EqualTo(AdsEnvironment.Test));
#else
            Assert.That(AdsConfiguration.ResolveCompileTimeEnvironment(), Is.EqualTo(AdsEnvironment.Production));
#endif
        }

        private static SdkStatusInfo Status(string sdkId) => new SdkStatusInfo { State = SdkState.Installed };

        private static AdsConfiguration Configuration(string json)
        {
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            JsonUtility.FromJsonOverwrite(json, configuration);
            return configuration;
        }
    }
}
