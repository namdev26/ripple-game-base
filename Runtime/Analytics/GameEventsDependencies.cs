using UnityEngine;

namespace RippleGame.Analytics
{
    internal interface IAnalyticsClock
    {
        double Now { get; }
    }

    internal interface IAnalyticsStore
    {
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
    }

    internal sealed class RealtimeClock : IAnalyticsClock
    {
        public double Now => Time.realtimeSinceStartupAsDouble;
    }

    /// <summary>PlayerPrefs is saved by Unity on pause/quit, so no explicit Save() per event.</summary>
    internal sealed class PlayerPrefsStore : IAnalyticsStore
    {
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    }
}
