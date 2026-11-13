using System;
using System.Collections.Generic;

namespace RippleGame.Attribution
{
    /// <summary>Reads an AppsFlyer conversion data dictionary into an attribution result. SDK-free.</summary>
    public static class ConversionDataParser
    {
        private const string StatusField = "af_status";
        private const string MediaSourceField = "media_source";
        private const string CampaignField = "campaign";

        /// <summary>
        /// <c>af_status</c> "Organic" gives Organic and "Non-organic" gives NonOrganic (case-insensitive); anything else
        /// is Unknown. Some platforms wrap the fields in a "data" or "payload" object, which is read when the top level
        /// has no <c>af_status</c>.
        /// </summary>
        public static (AttributionStatus status, string mediaSource, string campaign) Parse(IDictionary<string, object> data)
        {
            var fields = Locate(data);
            if (fields == null) return (AttributionStatus.Unknown, null, null);

            return (ParseStatus(Text(fields, StatusField)), Text(fields, MediaSourceField), Text(fields, CampaignField));
        }

        private static IDictionary<string, object> Locate(IDictionary<string, object> data)
        {
            if (data == null) return null;
            if (data.ContainsKey(StatusField)) return data;

            foreach (var wrapper in new[] { "data", "payload" })
            {
                if (data.TryGetValue(wrapper, out var inner) && inner is IDictionary<string, object> nested && nested.ContainsKey(StatusField))
                {
                    return nested;
                }
            }

            return data;
        }

        private static AttributionStatus ParseStatus(string value)
        {
            if (string.Equals(value, "Organic", StringComparison.OrdinalIgnoreCase)) return AttributionStatus.Organic;
            if (string.Equals(value, "Non-organic", StringComparison.OrdinalIgnoreCase)) return AttributionStatus.NonOrganic;
            return AttributionStatus.Unknown;
        }

        private static string Text(IDictionary<string, object> fields, string key) =>
            fields.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
    }
}
