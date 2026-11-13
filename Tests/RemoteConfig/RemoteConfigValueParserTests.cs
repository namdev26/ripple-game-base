using System.Globalization;
using NUnit.Framework;

namespace RippleGame.RemoteConfig.Tests
{
    public sealed class RemoteConfigValueParserTests
    {
        private CultureInfo _savedCulture;

        [SetUp]
        public void SetUp() => _savedCulture = CultureInfo.CurrentCulture;

        [TearDown]
        public void TearDown() => CultureInfo.CurrentCulture = _savedCulture;

        [Test]
        public void Parser_FloatUsesInvariantCulture()
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.That(RemoteConfigValueParser.TryParse<float>("1.5", out var single), Is.True);
            Assert.That(single, Is.EqualTo(1.5f));
            Assert.That(RemoteConfigValueParser.TryParse<double>("2.25", out var number), Is.True);
            Assert.That(number, Is.EqualTo(2.25d));
            Assert.That(RemoteConfigValueParser.TryParse<float>("1,5", out _), Is.False, "a comma is not a decimal separator");
        }

        [Test]
        public void Parser_BoolAcceptsOneAndZero()
        {
            Assert.That(RemoteConfigValueParser.TryParse<bool>("1", out var on), Is.True);
            Assert.That(on, Is.True);
            Assert.That(RemoteConfigValueParser.TryParse<bool>("0", out var off), Is.True);
            Assert.That(off, Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<bool>("TRUE", out var upper), Is.True);
            Assert.That(upper, Is.True);
            Assert.That(RemoteConfigValueParser.TryParse<bool>("False", out var mixed), Is.True);
            Assert.That(mixed, Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<bool>("yes", out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<bool>("2", out _), Is.False);
        }

        [Test]
        public void Parser_IntAndLong()
        {
            Assert.That(RemoteConfigValueParser.TryParse<int>("-42", out var small), Is.True);
            Assert.That(small, Is.EqualTo(-42));
            Assert.That(RemoteConfigValueParser.TryParse<long>("9000000000", out var big), Is.True);
            Assert.That(big, Is.EqualTo(9000000000L));
            Assert.That(RemoteConfigValueParser.TryParse<int>("9000000000", out _), Is.False, "overflow");
            Assert.That(RemoteConfigValueParser.TryParse<int>("1.5", out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<int>("", out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<int>("abc", out _), Is.False);
        }

        [Test]
        public void Parser_String_KeepsAnyText()
        {
            Assert.That(RemoteConfigValueParser.TryParse<string>("", out var empty), Is.True);
            Assert.That(empty, Is.EqualTo(""));
            Assert.That(RemoteConfigValueParser.TryParse<string>("{\"a\":1}", out var json), Is.True);
            Assert.That(json, Is.EqualTo("{\"a\":1}"));
        }

        [Test]
        public void Parser_NullText_IsInvalid()
        {
            Assert.That(RemoteConfigValueParser.TryParse<string>(null, out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<int>(null, out _), Is.False);
        }

        [Test]
        public void Parser_FloatRejectsNanAndInfinity()
        {
            Assert.That(RemoteConfigValueParser.TryParse<float>("NaN", out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<double>("Infinity", out _), Is.False);
            Assert.That(RemoteConfigValueParser.TryParse<float>("1e99", out _), Is.False, "overflow to infinity");
        }

        [Test]
        public void Parser_UnsupportedType_IsInvalid()
        {
            Assert.That(RemoteConfigValueParser.TryParse<System.DateTime>("2026-01-01", out _), Is.False);
        }
    }
}
