using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RippleGame.Analytics
{
    /// <summary>Applies Firebase Analytics naming and size limits before anything reaches the SDK.</summary>
    internal static class AnalyticsEventValidator
    {
        internal const int MaxNameLength = 40;
        internal const int MaxParams = 25;
        internal const int MaxStringValueLength = 100;
        internal const int MaxUserPropertyNameLength = 24;
        internal const int MaxUserPropertyValueLength = 36;

        private static readonly Regex NamePattern = new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
        private static readonly string[] ReservedPrefixes = { "firebase_", "google_", "ga_" };

        // Firebase drops events with these names and logs an error event instead.
        private static readonly HashSet<string> ReservedEventNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "ad_activeview", "ad_click", "ad_exposure", "ad_query", "ad_reward", "adunit_exposure",
            "app_background", "app_clear_data", "app_exception", "app_remove", "app_store_refund",
            "app_store_subscription_cancel", "app_store_subscription_convert", "app_store_subscription_renew",
            "app_update", "app_upgrade", "dynamic_link_app_open", "dynamic_link_app_update", "dynamic_link_first_open",
            "error", "first_open", "first_visit", "in_app_purchase", "notification_dismiss", "notification_foreground",
            "notification_open", "notification_receive", "os_update", "session_start", "session_start_with_rollout",
            "user_engagement"
        };

        internal static bool TrySanitizeEvent(string name, IEnumerable<AnalyticsParam> input, out List<AnalyticsParam> output, List<string> warnings)
        {
            output = new List<AnalyticsParam>();
            if (!IsValidName(name, MaxNameLength))
            {
                warnings.Add($"Event '{name}' dropped: names need 1-{MaxNameLength} chars of A-Z, a-z, 0-9, _ starting with a letter and no firebase_/google_/ga_ prefix.");
                return false;
            }

            if (ReservedEventNames.Contains(name))
            {
                warnings.Add($"Event '{name}' dropped: the name is reserved by Firebase.");
                return false;
            }

            if (input == null) return true;

            var dropped = 0;
            foreach (var parameter in input)
            {
                if (!IsValidName(parameter.Name, MaxNameLength))
                {
                    warnings.Add($"Event '{name}': param '{parameter.Name}' dropped (invalid name).");
                    continue;
                }

                if (output.Count == MaxParams)
                {
                    dropped++;
                    continue;
                }

                if (parameter.Kind == AnalyticsParamKind.String && parameter.StringValue != null && parameter.StringValue.Length > MaxStringValueLength)
                {
                    warnings.Add($"Event '{name}': param '{parameter.Name}' truncated to {MaxStringValueLength} chars.");
                    output.Add(parameter.WithStringValue(parameter.StringValue.Substring(0, MaxStringValueLength)));
                    continue;
                }

                output.Add(parameter);
            }

            if (dropped > 0)
            {
                warnings.Add($"Event '{name}': {dropped} params dropped (max {MaxParams}).");
            }

            return true;
        }

        internal static bool TrySanitizeUserProperty(string name, string value, out string sanitizedValue, List<string> warnings)
        {
            sanitizedValue = value;
            if (!IsValidName(name, MaxUserPropertyNameLength))
            {
                warnings.Add($"User property '{name}' dropped: names need 1-{MaxUserPropertyNameLength} chars of A-Z, a-z, 0-9, _ starting with a letter.");
                return false;
            }

            if (value != null && value.Length > MaxUserPropertyValueLength)
            {
                warnings.Add($"User property '{name}' truncated to {MaxUserPropertyValueLength} chars.");
                sanitizedValue = value.Substring(0, MaxUserPropertyValueLength);
            }

            return true;
        }

        private static bool IsValidName(string name, int maxLength)
        {
            if (string.IsNullOrEmpty(name) || name.Length > maxLength || !NamePattern.IsMatch(name)) return false;
            foreach (var prefix in ReservedPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            }

            return true;
        }
    }
}
