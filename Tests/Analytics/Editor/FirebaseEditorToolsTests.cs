using NUnit.Framework;
using RippleGame.Analytics.Editor;
using UnityEditor;

namespace RippleGame.Analytics.Tests
{
    public sealed class AnalyticsPreBuildValidatorTests
    {
        [Test]
        public void Validator_FirebaseNotInstalled_Ok()
        {
            Assert.That(AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, false, _ => false), Is.Null);
        }

        [Test]
        public void Validator_AndroidMissingJson_Error()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, true, _ => false);

            Assert.That(error, Does.Contain("google-services.json"));
        }

        [Test]
        public void Validator_iOSMissingPlist_Error()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.iOS, true, _ => false);

            Assert.That(error, Does.Contain("GoogleService-Info.plist"));
        }

        [Test]
        public void Validator_AndroidWithJson_Ok()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, true, file => file == "google-services.json");

            Assert.That(error, Is.Null);
        }
    }
}
