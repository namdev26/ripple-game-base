using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RippleGame.Ads;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Analytics.AdsBridge.Tests
{
    public sealed class AdsAnalyticsBridgeTests
    {
        private sealed class Recorder : IAnalyticsService
        {
            public readonly List<(string Name, List<AnalyticsParam> Params)> Events = new List<(string, List<AnalyticsParam>)>();
            public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters) => Events.Add((name, parameters.ToList()));
            public void SetUserProperty(string name, string value) { }
            public void SetUserId(string id) { }
            public void SetCollectionEnabled(bool enabled) { }

            public AnalyticsParam Param(string name) => Events.Single().Params.Single(p => p.Name == name);
            public bool Has(string name) => Events.Single().Params.Any(p => p.Name == name);
        }

        private Recorder _recorder;

        [SetUp]
        public void SetUp()
        {
            RippleGame.Analytics.Analytics.ResetForTests();
            _recorder = new Recorder();
            RippleGame.Analytics.Analytics.SetService(_recorder);
        }

        [TearDown]
        public void TearDown()
        {
            RippleGame.Analytics.Analytics.ResetForTests();
            AdsAnalyticsBridge.Reset();
        }

        private static AdsEvent Paid(string currency) =>
            new AdsEvent(AdsEventType.Paid, AdFormat.Rewarded, AdPlacement.DoubleReward,
                revenue: new AdRevenue(1500, currency, 1, "ca-app-pub-unit", "AdMob Network"));

        [Test]
        public void Paid_LogsAdImpressionWithRevenue()
        {
            AdsAnalyticsBridge.Handle(Paid("USD"), true);

            Assert.That(_recorder.Events.Single().Name, Is.EqualTo("ad_impression"));
            Assert.That(_recorder.Param("value").DoubleValue, Is.EqualTo(0.0015).Within(1e-12));
            Assert.That(_recorder.Param("currency").StringValue, Is.EqualTo("USD"));
            Assert.That(_recorder.Param("ad_format").StringValue, Is.EqualTo("rewarded"));
            Assert.That(_recorder.Param("placement").StringValue, Is.EqualTo("double_reward"));
            Assert.That(_recorder.Param("ad_platform").StringValue, Is.EqualTo("AdMob"));
            Assert.That(_recorder.Param("ad_source").StringValue, Is.EqualTo("AdMob Network"));
            Assert.That(_recorder.Param("ad_unit_name").StringValue, Is.EqualTo("ca-app-pub-unit"));
        }

        [Test]
        public void Paid_LogAdRevenueOff_LogsNothing()
        {
            AdsAnalyticsBridge.Handle(Paid("USD"), false);

            Assert.That(_recorder.Events, Is.Empty);
        }

        [Test]
        public void Paid_EmptyCurrency_Skipped()
        {
            LogAssert.Expect(LogType.Warning, new Regex("RippleGame Analytics"));

            AdsAnalyticsBridge.Handle(Paid(""), true);

            Assert.That(_recorder.Events, Is.Empty);
        }

        [Test]
        public void Shown_MapsToAdShow()
        {
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.Shown, AdFormat.Interstitial, AdPlacement.LevelComplete), true);

            Assert.That(_recorder.Events.Single().Name, Is.EqualTo("ad_show"));
            Assert.That(_recorder.Param("ad_format").StringValue, Is.EqualTo("interstitial"));
            Assert.That(_recorder.Param("placement").StringValue, Is.EqualTo("level_complete"));
        }

        [Test]
        public void ShowFailed_MapsToAdShowFail()
        {
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.ShowFailed, AdFormat.AppOpen, AdPlacement.Default, "Ad is not ready."), true);

            Assert.That(_recorder.Events.Single().Name, Is.EqualTo("ad_show_fail"));
            Assert.That(_recorder.Param("ad_format").StringValue, Is.EqualTo("app_open"));
            Assert.That(_recorder.Param("placement").StringValue, Is.EqualTo("default"));
            Assert.That(_recorder.Param("error").StringValue, Is.EqualTo("Ad is not ready."));
        }

        [Test]
        public void RewardEarned_MapsToAdReward()
        {
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.RewardEarned, AdFormat.Rewarded, AdPlacement.ExtraLife), true);

            Assert.That(_recorder.Events.Single().Name, Is.EqualTo("ad_reward_earned"));
            Assert.That(_recorder.Param("placement").StringValue, Is.EqualTo("extra_life"));
            Assert.That(_recorder.Has("ad_format"), Is.False);
        }

        [Test]
        public void Loaded_And_Failed_Ignored()
        {
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.Loaded, AdFormat.Rewarded, AdPlacement.Default), true);
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.Failed, AdFormat.Rewarded, AdPlacement.Default, "no fill"), true);
            AdsAnalyticsBridge.Handle(new AdsEvent(AdsEventType.Closed, AdFormat.Rewarded, AdPlacement.Default), true);

            Assert.That(_recorder.Events, Is.Empty);
        }

        [TestCase("AppOpen", "app_open")]
        [TestCase("LevelComplete", "level_complete")]
        [TestCase("Default", "default")]
        public void ToSnakeCase_Cases(string input, string expected)
        {
            Assert.That(AdsAnalyticsBridge.ToSnakeCase(input), Is.EqualTo(expected));
        }

        [Test]
        public void Install_Twice_SubscribesOnce()
        {
            AdsAnalyticsBridge.Reset();

            AdsAnalyticsBridge.Install();
            AdsAnalyticsBridge.Install();

            Assert.That(AdsAnalyticsBridge.IsInstalled, Is.True);
            Assert.That(AdsAnalyticsBridge.InstallCount, Is.EqualTo(1));
        }
    }
}
