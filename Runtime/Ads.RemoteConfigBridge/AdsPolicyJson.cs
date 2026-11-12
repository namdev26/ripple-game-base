using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RippleGame.Ads.RemoteConfigBridge
{
    /// <summary>
    /// Reads the <c>ads_policy</c> JSON (spec 5.3) into an <see cref="AdsPolicyOverride"/>. Only fields that are
    /// present with the right JSON type are set. A field with the wrong type is skipped, unknown fields are ignored.
    /// </summary>
    internal static class AdsPolicyJson
    {
        internal const string LogPrefix = "[RippleGame Ads RemoteConfig] ";

        private static readonly Dictionary<string, AdPlacement> PlacementsByName = BuildPlacementMap();

        /// <summary>Returns null (and logs a warning) when <paramref name="json"/> is not a JSON object.</summary>
        internal static AdsPolicyOverride Parse(string json)
        {
            JObject root;
            try
            {
                root = string.IsNullOrWhiteSpace(json) ? null : JToken.Parse(json) as JObject;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "ads_policy is not valid JSON: " + exception.Message);
                return null;
            }

            if (root == null)
            {
                Debug.LogWarning(LogPrefix + "ads_policy is not a JSON object.");
                return null;
            }

            var result = new AdsPolicyOverride();

            var interstitial = Section(root, "interstitial");
            if (interstitial != null)
            {
                result.InterstitialCooldownSeconds = Float(interstitial, "cooldown_seconds");
                result.InterstitialMaxPerSession = Int(interstitial, "max_per_session");
                result.InterstitialMinLevel = Int(interstitial, "min_level");
                result.InterstitialStartSession = Int(interstitial, "start_session");
                result.PlacementEnabled = Placements(Section(interstitial, "placements"));
            }

            var appOpen = Section(root, "app_open");
            if (appOpen != null)
            {
                result.AppOpenEnabled = Bool(appOpen, "enabled");
                result.AppOpenCooldownSeconds = Float(appOpen, "cooldown_seconds");
                result.AppOpenMinBackgroundSeconds = Float(appOpen, "min_background_seconds");
            }

            var fallback = Section(root, "rewarded_fallback");
            if (fallback != null) result.RewardedFallbackEnabled = Bool(fallback, "enabled");

            var organic = Section(root, "organic");
            if (organic != null)
            {
                result.OrganicBanner = Bool(organic, "banner");
                result.OrganicMrec = Bool(organic, "mrec");
                result.OrganicInterstitial = Bool(organic, "interstitial");
                result.OrganicAppOpen = Bool(organic, "app_open");
            }

            var banner = Section(root, "banner");
            if (banner != null) result.BannerRefreshSeconds = Float(banner, "refresh_seconds");

            var mrec = Section(root, "mrec");
            if (mrec != null) result.MrecRefreshSeconds = Float(mrec, "refresh_seconds");

            return result;
        }

        /// <summary>PascalCase to snake_case, matching the AdPlacement names used in the JSON.</summary>
        internal static string ToSnakeCase(string pascal)
        {
            var builder = new StringBuilder(pascal.Length + 4);
            for (var i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c) && i > 0) builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        private static Dictionary<string, AdPlacement> BuildPlacementMap()
        {
            var map = new Dictionary<string, AdPlacement>();
            foreach (AdPlacement placement in Enum.GetValues(typeof(AdPlacement)))
            {
                map[ToSnakeCase(placement.ToString())] = placement;
            }

            return map;
        }

        private static Dictionary<AdPlacement, bool> Placements(JObject section)
        {
            if (section == null) return null;

            Dictionary<AdPlacement, bool> result = null;
            foreach (var property in section.Properties())
            {
                if (property.Value.Type != JTokenType.Boolean) continue;
                if (!PlacementsByName.TryGetValue(property.Name, out var placement)) continue;

                result = result ?? new Dictionary<AdPlacement, bool>();
                result[placement] = property.Value.Value<bool>();
            }

            return result;
        }

        private static JObject Section(JObject parent, string name)
        {
            return parent.TryGetValue(name, StringComparison.Ordinal, out var token) ? token as JObject : null;
        }

        private static bool? Bool(JObject section, string name)
        {
            if (!section.TryGetValue(name, StringComparison.Ordinal, out var token) || token.Type != JTokenType.Boolean) return null;
            return token.Value<bool>();
        }

        private static float? Float(JObject section, string name)
        {
            if (!section.TryGetValue(name, StringComparison.Ordinal, out var token)) return null;
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float) return null;

            var value = token.Value<double>();
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > float.MaxValue) return null;
            return (float)value;
        }

        private static int? Int(JObject section, string name)
        {
            if (!section.TryGetValue(name, StringComparison.Ordinal, out var token) || token.Type != JTokenType.Integer) return null;

            try
            {
                var value = token.Value<long>();
                return value < int.MinValue || value > int.MaxValue ? (int?)null : (int)value;
            }
            catch (OverflowException)
            {
                return null;
            }
        }
    }
}
