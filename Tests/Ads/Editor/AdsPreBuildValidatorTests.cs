using NUnit.Framework;
using UnityEditor.Build;

namespace RippleGame.Ads.Editor.Tests
{
    public sealed class AdsPreBuildValidatorTests
    {
        [Test]
        public void EmptyProfile_IsRejectedByValidation()
        {
            Assert.Throws<BuildFailedException>(() => AdsPreBuildValidator.ValidateProfile(new AdMobAdUnitConfiguration(), "Test"));
        }

        [Test]
        public void NoAdsCompilation_ResolvesDisabledEnvironment()
        {
#if ADS
            Assert.Ignore("This test verifies the no-ADS compilation path.");
#else
            Assert.That(AdsConfiguration.ResolveCompileTimeEnvironment(), Is.EqualTo(AdsEnvironment.Disabled));
#endif
        }
    }
}
