using System.Collections.Generic;

namespace RippleGame.Analytics
{
    /// <summary>Backend that receives already validated calls from <see cref="Analytics"/>.</summary>
    public interface IAnalyticsService
    {
        void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters);
        void SetUserProperty(string name, string value);
        void SetUserId(string id);
        void SetCollectionEnabled(bool enabled);
    }
}
