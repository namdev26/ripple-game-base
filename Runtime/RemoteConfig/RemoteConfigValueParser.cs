using System;
using System.Globalization;

namespace RippleGame.RemoteConfig
{
    /// <summary>Turns raw Remote Config text into typed values. Always culture invariant.</summary>
    internal static class RemoteConfigValueParser
    {
        /// <summary>Parses int, long, float, double, bool and string. Anything else, or text that is not valid, returns false.</summary>
        internal static bool TryParse<T>(string raw, out T value)
        {
            value = default;
            if (raw == null) return false;

            var type = typeof(T);
            if (type == typeof(string))
            {
                value = (T)(object)raw;
                return true;
            }

            if (type == typeof(int))
            {
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return false;
                value = (T)(object)result;
                return true;
            }

            if (type == typeof(long))
            {
                if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return false;
                value = (T)(object)result;
                return true;
            }

            if (type == typeof(float))
            {
                if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || float.IsNaN(result) || float.IsInfinity(result)) return false;
                value = (T)(object)result;
                return true;
            }

            if (type == typeof(double))
            {
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result) || double.IsInfinity(result)) return false;
                value = (T)(object)result;
                return true;
            }

            if (type == typeof(bool))
            {
                var text = raw.Trim();
                if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(object)true;
                    return true;
                }

                if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    value = (T)(object)false;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Short name of a type for log messages.</summary>
        internal static string TypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            return type.Name;
        }
    }
}
