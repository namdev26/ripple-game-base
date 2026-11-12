using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>
    /// Ad unit ids supplied at runtime (for example by a remote config bridge). Providers resolve each
    /// configured id through <see cref="Resolve"/>, so an empty or missing override falls back to the asset value.
    /// </summary>
    public static class AdUnitIdOverrides
    {
        private static readonly Dictionary<string, string> Overrides = new Dictionary<string, string>();

        public static string Resolve(string providerId, RuntimePlatform platform, AdFormat format, string fallback)
        {
            var platformKey = ToPlatformKey(platform);
            if (platformKey != null && Overrides.TryGetValue(Key(providerId, platformKey, format), out var id) && !string.IsNullOrEmpty(id))
            {
                return id;
            }

            return fallback;
        }

        /// <param name="platformKey">"android" or "ios".</param>
        internal static void Set(string providerId, string platformKey, AdFormat format, string id)
        {
            Overrides[Key(providerId, platformKey, format)] = id;
        }

        internal static void Clear() => Overrides.Clear();

        private static string ToPlatformKey(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android: return "android";
                case RuntimePlatform.IPhonePlayer: return "ios";
                default: return null;
            }
        }

        private static string Key(string providerId, string platformKey, AdFormat format)
        {
            return $"{providerId?.ToLowerInvariant()}|{platformKey?.ToLowerInvariant()}|{(int)format}";
        }
    }
}
