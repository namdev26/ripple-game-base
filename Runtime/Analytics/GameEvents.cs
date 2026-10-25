using System;
using System.Collections.Generic;
using System.Globalization;
using Names = RippleGame.Analytics.AnalyticsNames;

namespace RippleGame.Analytics
{
    /// <summary>Standard game events. Call only the ones your game needs.</summary>
    public static class GameEvents
    {
        private const string AttemptKeyPrefix = "rg_analytics_attempt_";
        private const string MaxLevelKey = "rg_analytics_max_level";

        private static readonly Dictionary<int, double> LevelStartTimes = new Dictionary<int, double>();
        private static readonly Dictionary<int, string> LevelNames = new Dictionary<int, string>();
        private static IAnalyticsClock _clock = new RealtimeClock();
        private static IAnalyticsStore _store = new PlayerPrefsStore();

        /// <summary>Logs level_start. Attempt count per level is tracked automatically.</summary>
        public static void LevelStart(int level, string levelName = null, string mode = null)
        {
            var attempt = _store.GetInt(AttemptKeyPrefix + level, 0) + 1;
            _store.SetInt(AttemptKeyPrefix + level, attempt);
            LevelStartTimes[level] = _clock.Now;
            LevelNames[level] = levelName;

            Analytics.LogEvent(Names.Events.LevelStart, LevelParams(level, levelName)
                .Add(Names.Params.Attempt, attempt)
                .Add(Names.Params.Mode, mode));
        }

        /// <summary>Logs level_complete with attempt and duration since <see cref="LevelStart"/>; raises max_level.</summary>
        public static void LevelComplete(int level, long? score = null)
        {
            var parameters = LevelEndParams(level);
            if (score.HasValue) parameters.Add(Names.Params.Score, score.Value);
            Analytics.LogEvent(Names.Events.LevelComplete, parameters);

            if (level > _store.GetInt(MaxLevelKey, 0))
            {
                _store.SetInt(MaxLevelKey, level);
                Analytics.SetUserProperty(Names.UserProperties.MaxLevel, level.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Logs level_fail with attempt and duration since <see cref="LevelStart"/>.</summary>
        public static void LevelFail(int level, string reason = null)
        {
            Analytics.LogEvent(Names.Events.LevelFail, LevelEndParams(level).Add(Names.Params.FailReason, reason));
        }

        public static void TutorialBegin(string tutorialId) =>
            Analytics.LogEvent(Names.Events.TutorialBegin, new AnalyticsParams().Add(Names.Params.TutorialId, tutorialId));

        public static void TutorialStep(string tutorialId, int step) =>
            Analytics.LogEvent(Names.Events.TutorialStep, new AnalyticsParams()
                .Add(Names.Params.TutorialId, tutorialId)
                .Add(Names.Params.Step, step));

        public static void TutorialComplete(string tutorialId) =>
            Analytics.LogEvent(Names.Events.TutorialComplete, new AnalyticsParams().Add(Names.Params.TutorialId, tutorialId));

        public static void EarnCurrency(string currency, long amount, string source) =>
            Analytics.LogEvent(Names.Events.EarnVirtualCurrency, new AnalyticsParams()
                .Add(Names.Params.VirtualCurrencyName, currency)
                .Add(Names.Params.Value, amount)
                .Add(Names.Params.Source, source));

        public static void SpendCurrency(string currency, long amount, string itemName) =>
            Analytics.LogEvent(Names.Events.SpendVirtualCurrency, new AnalyticsParams()
                .Add(Names.Params.VirtualCurrencyName, currency)
                .Add(Names.Params.Value, amount)
                .Add(Names.Params.ItemName, itemName));

        public static void ScreenView(string screenName, string screenClass = null) =>
            Analytics.LogEvent(Names.Events.ScreenView, new AnalyticsParams()
                .Add(Names.Params.ScreenName, screenName)
                .Add(Names.Params.ScreenClass, string.IsNullOrEmpty(screenClass) ? screenName : screenClass));

        internal static void SetDependencies(IAnalyticsClock clock, IAnalyticsStore store)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        internal static void ResetForTests()
        {
            LevelStartTimes.Clear();
            LevelNames.Clear();
            _clock = new RealtimeClock();
            _store = new PlayerPrefsStore();
        }

        private static AnalyticsParams LevelParams(int level, string levelName) =>
            new AnalyticsParams()
                .Add(Names.Params.Level, level)
                .Add(Names.Params.LevelName, string.IsNullOrEmpty(levelName) ? "level_" + level.ToString(CultureInfo.InvariantCulture) : levelName);

        private static AnalyticsParams LevelEndParams(int level)
        {
            var duration = 0L;
            if (LevelStartTimes.TryGetValue(level, out var startedAt))
            {
                duration = Math.Max(0L, (long)Math.Round(_clock.Now - startedAt));
                LevelStartTimes.Remove(level);
            }

            LevelNames.TryGetValue(level, out var levelName);
            return LevelParams(level, levelName)
                .Add(Names.Params.Attempt, _store.GetInt(AttemptKeyPrefix + level, 0))
                .Add(Names.Params.DurationSec, duration);
        }
    }
}
