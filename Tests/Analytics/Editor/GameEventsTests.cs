using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RippleGame.Analytics.Tests
{
    public sealed class GameEventsTests
    {
        private sealed class FakeClock : IAnalyticsClock
        {
            public double Now { get; set; }
        }

        private sealed class MemoryStore : IAnalyticsStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
            public int GetInt(string key, int fallback) => _values.TryGetValue(key, out var value) ? value : fallback;
            public void SetInt(string key, int value) => _values[key] = value;
        }

        private FakeClock _clock;
        private FakeAnalyticsService _fake;

        [SetUp]
        public void SetUp()
        {
            Analytics.ResetForTests();
            GameEvents.ResetForTests();
            _clock = new FakeClock();
            GameEvents.SetDependencies(_clock, new MemoryStore());
            _fake = new FakeAnalyticsService();
            Analytics.SetService(_fake);
        }

        [TearDown]
        public void TearDown()
        {
            Analytics.ResetForTests();
            GameEvents.ResetForTests();
        }

        [Test]
        public void LevelStart_LogsLevelNameDefaultAndAttempt()
        {
            GameEvents.LevelStart(3);
            GameEvents.LevelStart(3);

            var second = _fake.Events.Last();
            Assert.That(second.Name, Is.EqualTo("level_start"));
            Assert.That(second.Param("level").LongValue, Is.EqualTo(3));
            Assert.That(second.Param("level_name").StringValue, Is.EqualTo("level_3"));
            Assert.That(second.Param("attempt").LongValue, Is.EqualTo(2));
            Assert.That(second.Has("mode"), Is.False);
        }

        [Test]
        public void LevelStart_WithNameAndMode_UsesThem()
        {
            GameEvents.LevelStart(5, "forest_2", "hard");

            var start = _fake.Event("level_start");
            Assert.That(start.Param("level_name").StringValue, Is.EqualTo("forest_2"));
            Assert.That(start.Param("mode").StringValue, Is.EqualTo("hard"));
        }

        [Test]
        public void LevelComplete_UsesNameFromStart()
        {
            GameEvents.LevelStart(5, "forest_2");

            GameEvents.LevelComplete(5);

            Assert.That(_fake.Event("level_complete").Param("level_name").StringValue, Is.EqualTo("forest_2"));
        }

        [Test]
        public void LevelComplete_LogsDurationAndScore()
        {
            _clock.Now = 10;
            GameEvents.LevelStart(1);
            _clock.Now = 58.4;

            GameEvents.LevelComplete(1, 1200);

            var complete = _fake.Event("level_complete");
            Assert.That(complete.Param("duration_sec").LongValue, Is.EqualTo(48));
            Assert.That(complete.Param("attempt").LongValue, Is.EqualTo(1));
            Assert.That(complete.Param("score").LongValue, Is.EqualTo(1200));
            Assert.That(complete.Param("level_name").StringValue, Is.EqualTo("level_1"));
        }

        [Test]
        public void LevelComplete_WithoutStart_LogsZeroDuration()
        {
            _clock.Now = 99;

            GameEvents.LevelComplete(7);

            var complete = _fake.Event("level_complete");
            Assert.That(complete.Param("duration_sec").LongValue, Is.EqualTo(0));
            Assert.That(complete.Has("score"), Is.False);
        }

        [Test]
        public void LevelFail_NullReason_OmitsParam()
        {
            GameEvents.LevelStart(2);

            GameEvents.LevelFail(2);

            Assert.That(_fake.Event("level_fail").Has("fail_reason"), Is.False);
        }

        [Test]
        public void LevelFail_WithReason_LogsFailReason()
        {
            GameEvents.LevelStart(2);

            GameEvents.LevelFail(2, "hit_wall");

            Assert.That(_fake.Event("level_fail").Param("fail_reason").StringValue, Is.EqualTo("hit_wall"));
        }

        [Test]
        public void LevelComplete_MaxLevel_OnlyIncreases()
        {
            GameEvents.LevelComplete(5);
            GameEvents.LevelComplete(3);

            var properties = _fake.Calls.Where(c => c.Kind == "property" && c.Name == "max_level").ToList();
            Assert.That(properties.Count, Is.EqualTo(1));
            Assert.That(properties[0].Value, Is.EqualTo("5"));
        }

        [Test]
        public void Tutorial_Events()
        {
            GameEvents.TutorialBegin("move");
            GameEvents.TutorialStep("move", 2);
            GameEvents.TutorialComplete("move");

            Assert.That(_fake.Events.Select(e => e.Name), Is.EqualTo(new[] { "tutorial_begin", "tutorial_step", "tutorial_complete" }));
            Assert.That(_fake.Events.All(e => e.Param("tutorial_id").StringValue == "move"), Is.True);
            Assert.That(_fake.Event("tutorial_step").Param("step").LongValue, Is.EqualTo(2));
        }

        [Test]
        public void Currency_Events()
        {
            GameEvents.EarnCurrency("coin", 50, "level_reward");
            GameEvents.SpendCurrency("coin", 200, "skin_red");

            var earn = _fake.Event("earn_virtual_currency");
            Assert.That(earn.Param("virtual_currency_name").StringValue, Is.EqualTo("coin"));
            Assert.That(earn.Param("value").LongValue, Is.EqualTo(50));
            Assert.That(earn.Param("source").StringValue, Is.EqualTo("level_reward"));

            var spend = _fake.Event("spend_virtual_currency");
            Assert.That(spend.Param("value").LongValue, Is.EqualTo(200));
            Assert.That(spend.Param("item_name").StringValue, Is.EqualTo("skin_red"));
        }

        [Test]
        public void ScreenView_DefaultsClassToName()
        {
            GameEvents.ScreenView("Home");

            var view = _fake.Event("screen_view");
            Assert.That(view.Param("screen_name").StringValue, Is.EqualTo("Home"));
            Assert.That(view.Param("screen_class").StringValue, Is.EqualTo("Home"));
        }
    }
}
