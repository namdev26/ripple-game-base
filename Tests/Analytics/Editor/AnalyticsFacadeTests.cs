using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Analytics.Tests
{
    public sealed class AnalyticsFacadeTests
    {
        [SetUp]
        public void SetUp() => Analytics.ResetForTests();

        [TearDown]
        public void TearDown() => Analytics.ResetForTests();

        [Test]
        public void Facade_CallsBeforeServiceSet_AreFlushedInOrder()
        {
            var fake = new FakeAnalyticsService();

            Analytics.LogEvent("a");
            Analytics.SetUserProperty("p", "1");
            Analytics.LogEvent("b");
            Analytics.SetService(fake);

            Assert.That(fake.Calls.Select(c => c.Name), Is.EqualTo(new[] { "a", "p", "b" }));
        }

        [Test]
        public void Facade_AfterServiceSet_ForwardsDirectly()
        {
            var fake = new FakeAnalyticsService();
            Analytics.SetService(fake);

            Analytics.LogEvent("level_start", new AnalyticsParams().Add("level", 1L));
            Analytics.SetUserId("u1");

            Assert.That(fake.Event("level_start").Param("level").LongValue, Is.EqualTo(1L));
            Assert.That(fake.Calls.Last().Kind, Is.EqualTo("user_id"));
        }

        [Test]
        public void Facade_QueueOverflow_DropsOldest()
        {
            var fake = new FakeAnalyticsService();
            LogAssert.ignoreFailingMessages = true;

            for (var i = 0; i < 105; i++) Analytics.LogEvent("e" + i);
            Analytics.SetService(fake);

            Assert.That(fake.Calls.Count, Is.EqualTo(Analytics.MaxQueuedCalls));
            Assert.That(fake.Calls.First().Name, Is.EqualTo("e5"));
            Assert.That(fake.Calls.Last().Name, Is.EqualTo("e104"));
        }

        [Test]
        public void Facade_InvalidEvent_NeverReachesService()
        {
            var fake = new FakeAnalyticsService();
            Analytics.SetService(fake);
            LogAssert.Expect(LogType.Warning, new Regex("RippleGame Analytics"));

            Analytics.LogEvent("bad-name");

            Assert.That(fake.Calls, Is.Empty);
        }

        [Test]
        public void Facade_ServiceThrows_DoesNotPropagate()
        {
            var fake = new FakeAnalyticsService { Throw = true };
            Analytics.SetService(fake);
            LogAssert.Expect(LogType.Warning, new Regex("RippleGame Analytics"));

            Assert.DoesNotThrow(() => Analytics.LogEvent("ok"));
        }

        [Test]
        public void Facade_Settings_NeverNull()
        {
            Assert.That(Analytics.Settings, Is.Not.Null);
            Assert.That(Analytics.Settings.LogAdRevenue, Is.True);
        }

        [Test]
        public void DebugFormat_PrintsNameAndParams()
        {
            var parameters = new AnalyticsParams().Add("level", 5L).Add("score", 1200L).ToList();

            var text = DebugAnalyticsService.Format("level_complete", parameters);

            Assert.That(text, Is.EqualTo("[RippleGame Analytics] level_complete {level=5, score=1200}"));
        }
    }
}
