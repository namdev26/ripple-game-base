using System;
using RippleGame.Sdk.Editor;
using UnityEditor;

namespace RippleGame.Ads.Editor
{
    /// <summary>Checks that the selected ads provider's SDK is compiled in and supports the build target.</summary>
    internal static class ProviderBuildCheck
    {
        internal const string AdMobTypeName = "RippleGame.Ads.AdMob.AdMobProviderFactory, RippleGame.Ads.AdMob";
        internal const string MaxTypeName = "RippleGame.Ads.Max.MaxProviderFactory, RippleGame.Ads.Max";

        /// <summary>The assembly-qualified factory type of a built-in provider, or null for "none" and unknown providers.</summary>
        internal static string FactoryTypeName(string providerId)
        {
            if (string.Equals(providerId, "admob", StringComparison.OrdinalIgnoreCase)) return AdMobTypeName;
            if (string.Equals(providerId, "max", StringComparison.OrdinalIgnoreCase)) return MaxTypeName;
            return null;
        }

        /// <summary>True when the provider's assembly was compiled, which happens only with its pinned SDK installed.</summary>
        internal static bool IsCompiled(string assemblyQualifiedTypeName) => Type.GetType(assemblyQualifiedTypeName, false) != null;

        /// <summary>
        /// Returns null when the provider can build for <paramref name="target"/>, or the error message otherwise.
        /// <paramref name="providerCompiled"/> receives the factory's assembly-qualified type name and
        /// <paramref name="sdkStatus"/> the SDK catalog id; the status is reserved for callers that want to
        /// word the fix hint by state, the error text stays the same.
        /// </summary>
        internal static string Validate(
            string providerId,
            Func<string, bool> providerCompiled,
            Func<string, SdkStatusInfo> sdkStatus,
            BuildTarget target)
        {
            if (string.IsNullOrWhiteSpace(providerId) || string.Equals(providerId, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var typeName = FactoryTypeName(providerId);
            if (typeName == null)
            {
                // A third-party provider registers itself at runtime; there is nothing to verify here.
                return null;
            }

            if (!providerCompiled(typeName))
            {
                return $"Ads provider '{providerId}' is selected but its SDK is not installed at the pinned version. " +
                       "Open RIPPLE Game > Open Hub > SDK and press Setup or Fix. " +
                       "If this game should not show ads, apply the No Ads build profile or set providerId to none.";
            }

            if (typeName == AdMobTypeName && target != BuildTarget.Android)
            {
                return "RippleGame Ads currently supports the AdMob provider only for Android builds.";
            }

            if (typeName == MaxTypeName && target != BuildTarget.Android && target != BuildTarget.iOS)
            {
                return "RippleGame Ads supports the MAX provider only for Android and iOS builds.";
            }

            return null;
        }
    }
}
