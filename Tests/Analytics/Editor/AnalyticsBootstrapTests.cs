using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Analytics.Tests
{
    public sealed class AnalyticsBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            Analytics.ResetForTests();
            AnalyticsBootstrap.ResetBackendForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Analytics.ResetForTests();
            AnalyticsBootstrap.ResetBackendForTests();
        }

        [Test]
        public void Initialize_NoBackendRegistered_UsesConsole()
        {
            Analytics.LogEvent("queued_event");
            LogAssert.Expect(LogType.Log, new Regex("queued_event"));

            AnalyticsBootstrap.Initialize(AnalyticsSettings.CreateForTests(true, true, true), true);
        }

        [Test]
        public void Initialize_BackendRegistered_UsesBackend()
        {
            AnalyticsSettings received = null;
            var settings = AnalyticsSettings.CreateForTests(true, true, true);
            AnalyticsBootstrap.RegisterBackend(s => received = s);

            AnalyticsBootstrap.Initialize(settings, true);

            Assert.That(received, Is.SameAs(settings));
        }

        [Test]
        public void Initialize_BackendRegisteredButEditor_UsesConsole()
        {
            var called = false;
            AnalyticsBootstrap.RegisterBackend(_ => called = true);
            Analytics.LogEvent("queued_event");
            LogAssert.Expect(LogType.Log, new Regex("queued_event"));

            AnalyticsBootstrap.Initialize(AnalyticsSettings.CreateForTests(true, true, true), false);

            Assert.That(called, Is.False);
        }

        [Test]
        public void Initialize_BackendThrows_FallsBackToConsole()
        {
            AnalyticsBootstrap.RegisterBackend(_ => throw new DllNotFoundException("native lib missing"));
            Analytics.LogEvent("queued_event");
            LogAssert.Expect(LogType.Error, new Regex("RippleGame Analytics.*native lib missing"));
            LogAssert.Expect(LogType.Log, new Regex("queued_event"));

            Assert.DoesNotThrow(() => AnalyticsBootstrap.Initialize(AnalyticsSettings.CreateForTests(true, true, true), true));
        }
    }
}
