using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

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
        public void ProviderSettings_AreRequiredOnlyForProvidersOtherThanAdMob()
        {
            var configuration = ScriptableObject.CreateInstance<AdsConfiguration>();
            var settings = ScriptableObject.CreateInstance<AdsConfiguration>();
            try
            {
                Assert.DoesNotThrow(() => AdsPreBuildValidator.ValidateProviderSettings(configuration));

                var serialized = new SerializedObject(configuration);
                serialized.FindProperty("providerId").stringValue = "levelplay";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(configuration.IsAdMob, Is.False);
                Assert.Throws<BuildFailedException>(() => AdsPreBuildValidator.ValidateProviderSettings(configuration));

                serialized.FindProperty("providerSettings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.DoesNotThrow(() => AdsPreBuildValidator.ValidateProviderSettings(configuration));
            }
            finally
            {
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(settings);
            }
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
