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
        public void SetUp() => Analytics.ResetForTests();

        [TearDown]
        public void TearDown() => Analytics.ResetForTests();

        [Test]
        public void StartService_InitializerThrows_FallsBackToConsoleAndFlushesQueue()
        {
            Analytics.LogEvent("queued_event");
            LogAssert.Expect(LogType.Error, new Regex("RippleGame Analytics.*native lib missing"));
            LogAssert.Expect(LogType.Log, new Regex("queued_event"));

            Assert.DoesNotThrow(() => AnalyticsBootstrap.StartService(
                AnalyticsSettings.CreateForTests(true, true, true),
                _ => throw new DllNotFoundException("native lib missing")));
        }
    }
}
