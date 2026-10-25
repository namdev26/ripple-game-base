using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RippleGame.Analytics.Tests
{
    public sealed class AnalyticsEventValidatorTests
    {
        [Test]
        public void Validator_ValidEvent_PassesUnchanged()
        {
            var warnings = new List<string>();

            var ok = AnalyticsEventValidator.TrySanitizeEvent("level_start", new AnalyticsParams().Add("level", 5L), out var output, warnings);

            Assert.That(ok, Is.True);
            Assert.That(output.Count, Is.EqualTo(1));
            Assert.That(output[0].Name, Is.EqualTo("level"));
            Assert.That(output[0].LongValue, Is.EqualTo(5L));
            Assert.That(warnings, Is.Empty);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("1level")]
        [TestCase("level-start")]
        [TestCase("firebase_x")]
        [TestCase("google_x")]
        [TestCase("ga_x")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void Validator_InvalidNames_Dropped(string name)
        {
            var warnings = new List<string>();

            var ok = AnalyticsEventValidator.TrySanitizeEvent(name, new AnalyticsParams(), out _, warnings);

            Assert.That(ok, Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [TestCase("ad_reward")]
        [TestCase("ad_click")]
        [TestCase("error")]
        [TestCase("first_open")]
        [TestCase("session_start")]
        [TestCase("user_engagement")]
        [TestCase("app_update")]
        [TestCase("in_app_purchase")]
        public void Validator_ReservedEventNames_Dropped(string name)
        {
            var warnings = new List<string>();

            var ok = AnalyticsEventValidator.TrySanitizeEvent(name, null, out _, warnings);

            Assert.That(ok, Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Validator_NonAsciiName_Dropped_NonAsciiValueKept()
        {
            var warnings = new List<string>();
            Assert.That(AnalyticsEventValidator.TrySanitizeEvent("màn_1", null, out _, warnings), Is.False);

            warnings.Clear();
            var ok = AnalyticsEventValidator.TrySanitizeEvent("ok", new AnalyticsParams().Add("name", new string('ộ', 120)), out var output, warnings);

            Assert.That(ok, Is.True);
            Assert.That(output[0].StringValue, Is.EqualTo(new string('ộ', 100)));
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Validator_InvalidParamName_DroppedOthersKept()
        {
            var warnings = new List<string>();

            var ok = AnalyticsEventValidator.TrySanitizeEvent("ok", new AnalyticsParams().Add("bad-name", 1L).Add("good", 2L), out var output, warnings);

            Assert.That(ok, Is.True);
            Assert.That(output.Select(p => p.Name), Is.EqualTo(new[] { "good" }));
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Validator_MoreThan25Params_KeepsFirst25()
        {
            var parameters = new AnalyticsParams();
            for (var i = 0; i < 30; i++) parameters.Add("p" + i, (long)i);
            var warnings = new List<string>();

            AnalyticsEventValidator.TrySanitizeEvent("ok", parameters, out var output, warnings);

            Assert.That(output.Select(p => p.Name), Is.EqualTo(Enumerable.Range(0, 25).Select(i => "p" + i)));
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void UserProperty_Rules()
        {
            var warnings = new List<string>();
            Assert.That(AnalyticsEventValidator.TrySanitizeUserProperty(new string('a', 25), "x", out _, warnings), Is.False);

            Assert.That(AnalyticsEventValidator.TrySanitizeUserProperty("ab_group", new string('b', 40), out var truncated, warnings), Is.True);
            Assert.That(truncated.Length, Is.EqualTo(36));

            Assert.That(AnalyticsEventValidator.TrySanitizeUserProperty("ab_group", null, out var cleared, warnings), Is.True);
            Assert.That(cleared, Is.Null);
        }

        [Test]
        public void Params_EmptyStringIgnored()
        {
            var parameters = new AnalyticsParams().Add("a", "").Add("b", (string)null).Add("c", 1L);

            Assert.That(parameters.Count, Is.EqualTo(1));
        }
    }
}
