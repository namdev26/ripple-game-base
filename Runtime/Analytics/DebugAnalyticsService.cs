using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RippleGame.Analytics
{
    /// <summary>Used in the Editor and whenever Firebase is not available. Prints calls to the Console.</summary>
    internal sealed class DebugAnalyticsService : IAnalyticsService
    {
        private readonly bool _logToConsole;

        internal DebugAnalyticsService(bool logToConsole) => _logToConsole = logToConsole;

        public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters) => Print(Format(name, parameters));
        public void SetUserProperty(string name, string value) => Print($"{Analytics.LogPrefix}user_property {name}={value}");
        public void SetUserId(string id) => Print($"{Analytics.LogPrefix}user_id {id}");
        public void SetCollectionEnabled(bool enabled) => Print($"{Analytics.LogPrefix}collection_enabled {enabled}");

        internal static string Format(string name, IReadOnlyList<AnalyticsParam> parameters)
        {
            var builder = new StringBuilder(Analytics.LogPrefix).Append(name);
            if (parameters == null || parameters.Count == 0) return builder.ToString();

            builder.Append(" {");
            for (var i = 0; i < parameters.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(parameters[i].Name).Append('=').Append(parameters[i].ToString());
            }

            return builder.Append('}').ToString();
        }

        private void Print(string message)
        {
            if (_logToConsole) Debug.Log(message);
        }
    }
}
