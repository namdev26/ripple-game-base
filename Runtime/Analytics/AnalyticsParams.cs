using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace RippleGame.Analytics
{
    public enum AnalyticsParamKind
    {
        String,
        Long,
        Double
    }

    public readonly struct AnalyticsParam
    {
        private AnalyticsParam(string name, AnalyticsParamKind kind, string stringValue, long longValue, double doubleValue)
        {
            Name = name;
            Kind = kind;
            StringValue = stringValue;
            LongValue = longValue;
            DoubleValue = doubleValue;
        }

        public string Name { get; }
        public AnalyticsParamKind Kind { get; }
        public string StringValue { get; }
        public long LongValue { get; }
        public double DoubleValue { get; }

        internal static AnalyticsParam String(string name, string value) => new AnalyticsParam(name, AnalyticsParamKind.String, value, 0, 0);
        internal static AnalyticsParam Long(string name, long value) => new AnalyticsParam(name, AnalyticsParamKind.Long, null, value, 0);
        internal static AnalyticsParam Double(string name, double value) => new AnalyticsParam(name, AnalyticsParamKind.Double, null, 0, value);

        internal AnalyticsParam WithStringValue(string value) => String(Name, value);

        public override string ToString()
        {
            switch (Kind)
            {
                case AnalyticsParamKind.Long: return LongValue.ToString(CultureInfo.InvariantCulture);
                case AnalyticsParamKind.Double: return DoubleValue.ToString(CultureInfo.InvariantCulture);
                default: return StringValue;
            }
        }
    }

    /// <summary>Fluent builder for event parameters. Empty string values are skipped.</summary>
    public sealed class AnalyticsParams : IEnumerable<AnalyticsParam>
    {
        private readonly List<AnalyticsParam> _items = new List<AnalyticsParam>();

        public int Count => _items.Count;

        public AnalyticsParams Add(string name, string value)
        {
            if (!string.IsNullOrEmpty(value)) _items.Add(AnalyticsParam.String(name, value));
            return this;
        }

        public AnalyticsParams Add(string name, long value)
        {
            _items.Add(AnalyticsParam.Long(name, value));
            return this;
        }

        public AnalyticsParams Add(string name, double value)
        {
            _items.Add(AnalyticsParam.Double(name, value));
            return this;
        }

        public IEnumerator<AnalyticsParam> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
