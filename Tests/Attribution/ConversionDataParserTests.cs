using System.Collections.Generic;
using NUnit.Framework;

namespace RippleGame.Attribution.Tests
{
    public sealed class ConversionDataParserTests
    {
        [TestCase("Organic")]
        [TestCase("organic")]
        [TestCase("ORGANIC")]
        public void Parse_OrganicCaseInsensitive(string value)
        {
            var result = ConversionDataParser.Parse(new Dictionary<string, object> { { "af_status", value } });

            Assert.That(result.status, Is.EqualTo(AttributionStatus.Organic));
        }

        [TestCase("Non-organic")]
        [TestCase("non-organic")]
        [TestCase("NON-ORGANIC")]
        public void Parse_NonOrganic(string value)
        {
            var result = ConversionDataParser.Parse(new Dictionary<string, object>
            {
                { "af_status", value },
                { "media_source", "googleadwords_int" },
                { "campaign", "spring" }
            });

            Assert.That(result.status, Is.EqualTo(AttributionStatus.NonOrganic));
            Assert.That(result.mediaSource, Is.EqualTo("googleadwords_int"));
            Assert.That(result.campaign, Is.EqualTo("spring"));
        }

        [Test]
        public void Parse_MissingStatus_Unknown()
        {
            var result = ConversionDataParser.Parse(new Dictionary<string, object>());

            Assert.That(result.status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(result.mediaSource, Is.Null);
            Assert.That(result.campaign, Is.Null);
        }

        [Test]
        public void Parse_Null_Unknown()
        {
            Assert.That(ConversionDataParser.Parse(null).status, Is.EqualTo(AttributionStatus.Unknown));
        }

        [Test]
        public void Parse_UnrecognizedStatus_Unknown()
        {
            var result = ConversionDataParser.Parse(new Dictionary<string, object> { { "af_status", "Something" } });

            Assert.That(result.status, Is.EqualTo(AttributionStatus.Unknown));
        }

        [Test]
        public void Parse_NonStringStatus_Unknown()
        {
            var result = ConversionDataParser.Parse(new Dictionary<string, object> { { "af_status", 5 } });

            Assert.That(result.status, Is.EqualTo(AttributionStatus.Unknown));
        }

        [TestCase("data")]
        [TestCase("payload")]
        public void Parse_StatusInsideWrapper_IsRead(string wrapper)
        {
            var inner = new Dictionary<string, object>
            {
                { "af_status", "Non-organic" },
                { "media_source", "facebook" },
                { "campaign", "c1" }
            };

            var result = ConversionDataParser.Parse(new Dictionary<string, object> { { "status", "success" }, { wrapper, inner } });

            Assert.That(result.status, Is.EqualTo(AttributionStatus.NonOrganic));
            Assert.That(result.mediaSource, Is.EqualTo("facebook"));
            Assert.That(result.campaign, Is.EqualTo("c1"));
        }
    }
}
