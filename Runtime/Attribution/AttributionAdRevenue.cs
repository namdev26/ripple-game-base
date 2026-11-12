namespace RippleGame.Attribution
{
    /// <summary>One ad impression's revenue, in currency units (not micros), handed to the attribution provider.</summary>
    public readonly struct AttributionAdRevenue
    {
        public AttributionAdRevenue(double value, string currency, string platform, string adUnitId, string adSource, string format, string placement)
        {
            Value = value;
            Currency = currency;
            Platform = platform;
            AdUnitId = adUnitId;
            AdSource = adSource;
            Format = format;
            Placement = placement;
        }

        /// <summary>Revenue in units of <see cref="Currency"/>.</summary>
        public double Value { get; }
        public string Currency { get; }
        /// <summary>Mediation platform that reported the revenue, e.g. "applovin_max" or "admob".</summary>
        public string Platform { get; }
        public string AdUnitId { get; }
        /// <summary>Ad network that served the impression.</summary>
        public string AdSource { get; }
        public string Format { get; }
        public string Placement { get; }
    }
}
