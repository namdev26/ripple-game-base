using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RippleGame.Ads.RemoteConfigBridge
{
    /// <summary>
    /// Reads the <c>ads_unit_ids</c> JSON: <c>{"max":{"android":{"interstitial":"id"}},"admob":{"ios":{"banner":"id"}}}</c>.
    /// Empty ids, ids that are not strings and unknown formats are skipped.
    /// </summary>
    internal static class AdUnitIdsJson
    {
        private static readonly Dictionary<string, AdFormat> FormatsByName = new Dictionary<string, AdFormat>
        {
            { "banner", AdFormat.Banner },
            { "interstitial", AdFormat.Interstitial },
            { "rewarded", AdFormat.Rewarded },
            { "app_open", AdFormat.AppOpen },
            { "mrec", AdFormat.Mrec }
        };

        /// <summary>Provider and platform come back lower-case. Invalid JSON logs a warning and yields nothing.</summary>
        internal static IEnumerable<(string provider, string platform, AdFormat format, string id)> Parse(string json)
        {
            TryParse(json, out var result);
            return result;
        }

        /// <summary>
        /// False (with a warning) when <paramref name="json"/> is not a JSON object. A valid object with no usable ids,
        /// such as <c>{}</c>, returns true and an empty list.
        /// </summary>
        internal static bool TryParse(string json, out List<(string provider, string platform, AdFormat format, string id)> result)
        {
            result = new List<(string provider, string platform, AdFormat format, string id)>();

            JObject root;
            try
            {
                root = string.IsNullOrWhiteSpace(json) ? null : JToken.Parse(json) as JObject;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(AdsPolicyJson.LogPrefix + "ads_unit_ids is not valid JSON: " + exception.Message);
                return false;
            }

            if (root == null)
            {
                Debug.LogWarning(AdsPolicyJson.LogPrefix + "ads_unit_ids is not a JSON object.");
                return false;
            }

            foreach (var provider in root.Properties())
            {
                if (!(provider.Value is JObject platforms)) continue;

                foreach (var platform in platforms.Properties())
                {
                    if (!(platform.Value is JObject formats)) continue;

                    foreach (var format in formats.Properties())
                    {
                        if (format.Value.Type != JTokenType.String) continue;
                        if (!FormatsByName.TryGetValue(format.Name.ToLowerInvariant(), out var adFormat)) continue;

                        var id = format.Value.Value<string>();
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        result.Add((provider.Name.ToLowerInvariant(), platform.Name.ToLowerInvariant(), adFormat, id.Trim()));
                    }
                }
            }

            return true;
        }
    }
}
