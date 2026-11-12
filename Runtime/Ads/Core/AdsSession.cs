using System;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>App session counter used by the interstitial <c>startSession</c> rule. 1 is the first session.</summary>
    internal static class AdsSession
    {
        internal const string PlayerPrefsKey = "ripplegame.ads.session";

        private static bool _begun;

        /// <summary>Current session number; 0 until <see cref="Begin"/> runs.</summary>
        internal static int Number { get; private set; }

        /// <summary>Increments the stored counter once per process. Later calls do nothing.</summary>
        internal static void Begin(Func<string, int> read, Action<string, int> write)
        {
            if (_begun)
            {
                return;
            }

            _begun = true;
            Number = Math.Max(0, read(PlayerPrefsKey)) + 1;
            write(PlayerPrefsKey, Number);
        }

        internal static void BeginWithPlayerPrefs()
        {
            Begin(key => PlayerPrefs.GetInt(key, 0), (key, value) =>
            {
                PlayerPrefs.SetInt(key, value);
                PlayerPrefs.Save();
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            _begun = false;
            Number = 0;
        }
    }
}
