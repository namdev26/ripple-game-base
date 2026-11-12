using System.Collections.Generic;
using NUnit.Framework;
using RippleGame.Attribution;
using UnityEngine;
using AttributionApi = RippleGame.Attribution.Attribution;

namespace RippleGame.Ads.AttributionBridge.Tests
{
    public sealed class AdsAttributionBridgeTests
    {
        private sealed class FakeService : IAttributionService
        {
            public readonly List<AttributionAdRevenue> Revenue = new List<AttributionAdRevenue>();

            public void Initialize(AttributionSettings settings) { }
            public void LogAdRevenue(AttributionAdRevenue revenue) => Revenue.Add(revenue);
        }

        private static readonly string[] Keys =
        {
            "ripplegame.attribution.status", "ripplegame.attribution.media_source", "ripplegame.attribution.campaign"
        };

        private FakeService _service;

        [SetUp]
        public void SetUp()
        {
            DeleteKeys();
            ResetAll();
            _service = new FakeService();
            AttributionApi.RegisterService(_service);
        }

        [TearDown]
        public void TearDown()
        {
            ResetAll();
            DeleteKeys();
        }

        private static void ResetAll()
        {
            AdsAttributionBridge.ResetForTests();
            AttributionApi.ResetForTests();
            Ads.ResetForTests();
        }

        private static void DeleteKeys()
        {
            foreach (var key in Keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        [Test]
        public void Bridge_PaidEvent_ConvertsMicrosToUnits()
        {
            var revenue = new AdRevenue(1_500_000, "EUR", 2, "unit-1", "Meta", AdRevenue.PlatformMax);

            AdsAttributionBridge.Handle(new AdsEvent(AdsEventType.Paid, AdFormat.Rewarded, AdPlacement.DoubleReward, revenue: revenue));

            Assert.That(_service.Revenue, Has.Count.EqualTo(1));
            var sent = _service.Revenue[0];
            Assert.That(sent.Value, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(sent.Currency, Is.EqualTo("EUR"));
            Assert.That(sent.Platform, Is.EqualTo("applovin_max"));
            Assert.That(sent.AdUnitId, Is.EqualTo("unit-1"));
            Assert.That(sent.AdSource, Is.EqualTo("Meta"));
            Assert.That(sent.Format, Is.EqualTo("Rewarded"));
            Assert.That(sent.Placement, Is.EqualTo("DoubleReward"));
        }

        [Test]
        public void Bridge_NegativeRevenue_IsForwardedAsRaised()
        {
            var revenue = new AdRevenue(-250_000, "USD", 0, "unit", "src", AdRevenue.PlatformMax);

            AdsAttributionBridge.Handle(new AdsEvent(AdsEventType.Paid, AdFormat.Banner, AdPlacement.Default, revenue: revenue));

            Assert.That(_service.Revenue[0].Value, Is.EqualTo(-0.25).Within(1e-12));
        }

        [Test]
        public void Bridge_PaidWithoutRevenue_Ignored()
        {
            AdsAttributionBridge.Handle(new AdsEvent(AdsEventType.Paid, AdFormat.Banner, AdPlacement.Default));

            Assert.That(_service.Revenue, Is.Empty);
        }

        [Test]
        public void Bridge_NonPaidEvent_Ignored()
        {
            var revenue = new AdRevenue(1_000_000, "USD", 0, "unit", "src");

            AdsAttributionBridge.Handle(new AdsEvent(AdsEventType.Shown, AdFormat.Banner, AdPlacement.Default, revenue: revenue));

            Assert.That(_service.Revenue, Is.Empty);
        }

        [Test]
        public void Bridge_UnknownStatus_SetsNonOrganic()
        {
            AdsAttributionBridge.Install();

            Assert.That(AttributionApi.Status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(Ads.OrganicValue, Is.EqualTo(false));
        }

        [Test]
        public void Bridge_StatusChanges_FeedOrganicRule()
        {
            AdsAttributionBridge.Install();

            AttributionApi.ReportConversion(AttributionStatus.Organic, "organic", null);
            Assert.That(Ads.OrganicValue, Is.EqualTo(true));

            AttributionApi.ReportConversion(AttributionStatus.NonOrganic, "facebook", "c1");
            Assert.That(Ads.OrganicValue, Is.EqualTo(false));
        }

        [Test]
        public void Bridge_Install_UsesAlreadyKnownStatus()
        {
            AttributionApi.ReportConversion(AttributionStatus.Organic, "organic", null);

            AdsAttributionBridge.Install();

            Assert.That(Ads.OrganicValue, Is.EqualTo(true));
        }

        [Test]
        public void Bridge_InstallTwice_DoesNotDuplicate()
        {
            AdsAttributionBridge.Install();
            AdsAttributionBridge.Install();

            Assert.That(AdsAttributionBridge.IsInstalled, Is.True);
        }

        [Test]
        public void Bridge_AfterReset_StopsFollowingStatus()
        {
            AdsAttributionBridge.Install();
            AdsAttributionBridge.ResetForTests();
            Ads.ResetForTests();

            AttributionApi.ReportConversion(AttributionStatus.Organic, "organic", null);

            Assert.That(Ads.OrganicValue, Is.Null);
        }
    }
}
