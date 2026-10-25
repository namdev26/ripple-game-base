using System;
using System.Collections.Generic;
using System.Linq;

namespace RippleGame.Analytics.Tests
{
    /// <summary>Records every call so tests can assert on what reached the service.</summary>
    internal sealed class FakeAnalyticsService : IAnalyticsService
    {
        internal sealed class Call
        {
            public string Kind;
            public string Name;
            public string Value;
            public List<AnalyticsParam> Parameters;

            public AnalyticsParam Param(string name) => Parameters.Single(p => p.Name == name);
            public bool Has(string name) => Parameters != null && Parameters.Any(p => p.Name == name);
        }

        public readonly List<Call> Calls = new List<Call>();
        public bool Throw;

        public IEnumerable<Call> Events => Calls.Where(c => c.Kind == "event");
        public Call Event(string name) => Events.Single(c => c.Name == name);

        public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters)
        {
            Record(new Call { Kind = "event", Name = name, Parameters = parameters.ToList() });
        }

        public void SetUserProperty(string name, string value) => Record(new Call { Kind = "property", Name = name, Value = value });
        public void SetUserId(string id) => Record(new Call { Kind = "user_id", Value = id });
        public void SetCollectionEnabled(bool enabled) => Record(new Call { Kind = "collection", Value = enabled.ToString() });

        private void Record(Call call)
        {
            if (Throw) throw new InvalidOperationException("fake failure");
            Calls.Add(call);
        }
    }
}
