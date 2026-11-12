using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RippleGame.Attribution.Tests
{
    public sealed class AttributionTests
    {
        private sealed class FakeService : IAttributionService
        {
            public AttributionSettings InitializedWith;
            public readonly List<AttributionAdRevenue> Revenue = new List<AttributionAdRevenue>();

            public void Initialize(AttributionSettings settings) => InitializedWith = settings;
            public void LogAdRevenue(AttributionAdRevenue revenue) => Revenue.Add(revenue);
        }

        private static readonly string[] Keys =
        {
            Attribution.StatusKey, Attribution.MediaSourceKey, Attribution.CampaignKey
        };

        private readonly List<AttributionStatus> _changes = new List<AttributionStatus>();

        [SetUp]
        public void SetUp()
        {
            DeleteKeys();
            Attribution.ResetForTests();
            _changes.Clear();
            Attribution.StatusChanged += _changes.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Attribution.ResetForTests();
            DeleteKeys();
        }

        private static void DeleteKeys()
        {
            foreach (var key in Keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static AttributionAdRevenue Revenue() =>
            new AttributionAdRevenue(1.5, "USD", "applovin_max", "unit", "AppLovin", "Rewarded", "Default");

        [Test]
        public void Status_StartsUnknown()
        {
            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(Attribution.MediaSource, Is.Null);
            Assert.That(Attribution.Campaign, Is.Null);
        }

        [Test]
        public void ReportConversion_Unknown_IsNotPersisted()
        {
            Attribution.ReportConversion(AttributionStatus.Unknown, "src", "camp");

            Assert.That(PlayerPrefs.HasKey(Attribution.StatusKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(Attribution.MediaSourceKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(Attribution.CampaignKey), Is.False);
            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void ReportConversion_Unknown_DoesNotOverwriteKnownStatus()
        {
            Attribution.ReportConversion(AttributionStatus.Organic, "organic", null);

            Attribution.ReportConversion(AttributionStatus.Unknown, null, null);

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Organic));
            Assert.That(PlayerPrefs.GetInt(Attribution.StatusKey), Is.EqualTo((int)AttributionStatus.Organic));
        }

        [Test]
        public void ReportConversion_PersistsAndRaisesOnlyOnChange()
        {
            Attribution.ReportConversion(AttributionStatus.NonOrganic, "facebook", "c1");

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.NonOrganic));
            Assert.That(Attribution.MediaSource, Is.EqualTo("facebook"));
            Assert.That(Attribution.Campaign, Is.EqualTo("c1"));
            Assert.That(PlayerPrefs.GetInt(Attribution.StatusKey), Is.EqualTo((int)AttributionStatus.NonOrganic));
            Assert.That(PlayerPrefs.GetString(Attribution.MediaSourceKey), Is.EqualTo("facebook"));
            Assert.That(PlayerPrefs.GetString(Attribution.CampaignKey), Is.EqualTo("c1"));
            Assert.That(_changes, Is.EqualTo(new[] { AttributionStatus.NonOrganic }));

            Attribution.ReportConversion(AttributionStatus.NonOrganic, "facebook", "c2");

            Assert.That(_changes, Has.Count.EqualTo(1), "same status must not raise again");
            Assert.That(Attribution.Campaign, Is.EqualTo("c2"));
            Assert.That(PlayerPrefs.GetString(Attribution.CampaignKey), Is.EqualTo("c2"));

            Attribution.ReportConversion(AttributionStatus.Organic, null, null);

            Assert.That(_changes, Is.EqualTo(new[] { AttributionStatus.NonOrganic, AttributionStatus.Organic }));
            Assert.That(PlayerPrefs.HasKey(Attribution.MediaSourceKey), Is.False);
        }

        [Test]
        public void LoadPersisted_RestoresStatusAndRaises()
        {
            PlayerPrefs.SetInt(Attribution.StatusKey, (int)AttributionStatus.Organic);
            PlayerPrefs.SetString(Attribution.MediaSourceKey, "organic");
            PlayerPrefs.SetString(Attribution.CampaignKey, "none");

            Attribution.LoadPersisted();

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Organic));
            Assert.That(Attribution.MediaSource, Is.EqualTo("organic"));
            Assert.That(Attribution.Campaign, Is.EqualTo("none"));
            Assert.That(_changes, Is.EqualTo(new[] { AttributionStatus.Organic }));
        }

        [Test]
        public void LoadPersisted_NothingSaved_StaysUnknown()
        {
            Attribution.LoadPersisted();

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(_changes, Is.Empty);
        }

        [Test]
        public void LoadPersisted_InvalidValue_StaysUnknown()
        {
            PlayerPrefs.SetInt(Attribution.StatusKey, 99);

            Attribution.LoadPersisted();

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Unknown));
        }

        [Test]
        public void LogAdRevenue_NoService_DoesNothing()
        {
            Assert.DoesNotThrow(() => Attribution.LogAdRevenue(Revenue()));
        }

        [Test]
        public void LogAdRevenue_SettingOn_CallsService()
        {
            var service = new FakeService();
            Attribution.RegisterService(service);
            Attribution.SetSettings(AttributionSettings.CreateForTests("key", "id", false, true));

            Attribution.LogAdRevenue(Revenue());

            Assert.That(service.Revenue, Has.Count.EqualTo(1));
            Assert.That(service.Revenue[0].Value, Is.EqualTo(1.5));
        }

        [Test]
        public void LogAdRevenue_SettingOff_DoesNotCallService()
        {
            var service = new FakeService();
            Attribution.RegisterService(service);
            Attribution.SetSettings(AttributionSettings.CreateForTests("key", "id", false, false));

            Attribution.LogAdRevenue(Revenue());

            Assert.That(service.Revenue, Is.Empty);
        }

        [Test]
        public void Bootstrap_LoadsSavedStatusAndInitializesService()
        {
            PlayerPrefs.SetInt(Attribution.StatusKey, (int)AttributionStatus.NonOrganic);
            var service = new FakeService();
            Attribution.RegisterService(service);
            var settings = AttributionSettings.CreateForTests("key", "id", false, true);

            AttributionBootstrap.Initialize(settings);

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.NonOrganic));
            Assert.That(service.InitializedWith, Is.SameAs(settings));
        }

        [Test]
        public void ResetForTests_ClearsStateButKeepsSavedValues()
        {
            Attribution.ReportConversion(AttributionStatus.Organic, "organic", null);

            Attribution.ResetForTests();

            Assert.That(Attribution.Status, Is.EqualTo(AttributionStatus.Unknown));
            Assert.That(PlayerPrefs.HasKey(Attribution.StatusKey), Is.True);
        }
    }
}
