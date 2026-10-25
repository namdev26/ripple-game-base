using System;
using System.Linq;

namespace RippleGame.Analytics.Editor
{
    /// <summary>Adds or removes the FIREBASE scripting define in a ';'-separated symbol list.</summary>
    internal static class FirebaseDefineUtility
    {
        internal const string Symbol = "FIREBASE";

        internal static string Add(string symbols) => Join(Parse(symbols).Append(Symbol));

        internal static string Remove(string symbols) => Join(Parse(symbols).Where(symbol => symbol != Symbol));

        internal static bool Contains(string symbols) => Parse(symbols).Contains(Symbol);

        internal static bool IsFirebaseInstalled() =>
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType("Firebase.Analytics.FirebaseAnalytics", false) != null);

        private static System.Collections.Generic.IEnumerable<string> Parse(string symbols) =>
            (symbols ?? string.Empty).Split(';').Select(symbol => symbol.Trim()).Where(symbol => symbol.Length > 0);

        private static string Join(System.Collections.Generic.IEnumerable<string> symbols) =>
            string.Join(";", symbols.Distinct().OrderBy(symbol => symbol, StringComparer.Ordinal));
    }
}
