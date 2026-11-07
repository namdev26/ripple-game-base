using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.Localization
{
    /// <summary>Reads one language file: a header line, then key,value lines.</summary>
    internal static class LocalizationFile
    {
        internal static Dictionary<string, string> Parse(string content)
        {
            var entries = new Dictionary<string, string>();
            var lines = content.TrimStart('﻿').Split('\n');
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                var comma = line.IndexOf(',');
                if (comma <= 0)
                    continue;

                var key = line.Substring(0, comma);
#if UNITY_EDITOR
                if (entries.ContainsKey(key))
                    Debug.LogWarning("[Localization] Duplicate key: " + key);
#endif
                entries[key] = ReadValue(line.Substring(comma + 1));
            }

            return entries;
        }

        private static string ReadValue(string raw)
        {
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                raw = raw.Substring(1, raw.Length - 2).Replace("\"\"", "\"");

            return raw.Replace("\\n", "\n");
        }
    }
}
