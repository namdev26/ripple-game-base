using NUnit.Framework;
using RippleGame.Analytics.Editor;
using UnityEditor;

namespace RippleGame.Analytics.Tests
{
    public sealed class FirebaseDefineUtilityTests
    {
        [Test]
        public void Add_KeepsOthersAndSorts()
        {
            Assert.That(FirebaseDefineUtility.Add("ADS;ADS_TEST"), Is.EqualTo("ADS;ADS_TEST;FIREBASE"));
            Assert.That(FirebaseDefineUtility.Add("ZETA;ADS"), Is.EqualTo("ADS;FIREBASE;ZETA"));
        }

        [Test]
        public void Add_Twice_NoDuplicate()
        {
            Assert.That(FirebaseDefineUtility.Add(FirebaseDefineUtility.Add("ADS")), Is.EqualTo("ADS;FIREBASE"));
        }

        [Test]
        public void Add_Empty_ReturnsOnlyFirebase()
        {
            Assert.That(FirebaseDefineUtility.Add(""), Is.EqualTo("FIREBASE"));
            Assert.That(FirebaseDefineUtility.Add(null), Is.EqualTo("FIREBASE"));
        }

        [Test]
        public void Remove_KeepsOthers()
        {
            Assert.That(FirebaseDefineUtility.Remove("ADS;ADS_TEST;FIREBASE"), Is.EqualTo("ADS;ADS_TEST"));
            Assert.That(FirebaseDefineUtility.Remove("FIREBASE"), Is.EqualTo(string.Empty));
        }
    }

    public sealed class AnalyticsPreBuildValidatorTests
    {
        [Test]
        public void Validator_NoFirebaseDefine_Ok()
        {
            Assert.That(AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, "ADS;ADS_TEST", _ => false), Is.Null);
        }

        [Test]
        public void Validator_AndroidMissingJson_Error()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, "ADS;FIREBASE", _ => false);

            Assert.That(error, Does.Contain("google-services.json"));
        }

        [Test]
        public void Validator_iOSMissingPlist_Error()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.iOS, "FIREBASE", _ => false);

            Assert.That(error, Does.Contain("GoogleService-Info.plist"));
        }

        [Test]
        public void Validator_AndroidWithJson_Ok()
        {
            var error = AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, "FIREBASE", file => file == "google-services.json");

            Assert.That(error, Is.Null);
        }

        [Test]
        public void Validator_FirebaseSubstringSymbol_IsNotFirebase()
        {
            Assert.That(AnalyticsPreBuildValidator.GetMissingConfigError(BuildTarget.Android, "FIREBASE_OFF", _ => false), Is.Null);
        }
    }
}
