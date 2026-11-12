using System;
using RippleGame.RemoteConfig;
using UnityEngine;
using RemoteConfigApi = RippleGame.RemoteConfig.RemoteConfig;

namespace RippleGame.Ads.RemoteConfigBridge
{
    /// <summary>
    /// Connects Remote Config to the Ads policy layer: applies <c>ads_enabled</c>, <c>ads_policy</c> and
    /// <c>ads_unit_ids</c>, caches the policy override for the next session, and makes Ads wait for Remote Config
    /// (bounded by its fetch timeout) before the provider initializes. Everything runs on the main thread.
    /// </summary>
    internal static class AdsRemoteConfigBridge
    {
        internal const string PolicyCacheKey = "ripplegame.ads.policy_override_cache";

        // Readiness is already bounded by the fetch timeout; this only keeps the wait from being unbounded.
        private const float ReadyWaitSeconds = 86400f;

        private static bool _installed;
        private static bool _activatedSeen;
        private static int _epoch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForTests();

        internal static void ResetForTests()
        {
            // Also keeps state clean when Domain Reload is disabled.
            if (_installed) RemoteConfigApi.Activated -= OnActivated;
            _installed = false;
            _activatedSeen = false;
            _epoch++;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Install()
        {
            if (_installed) return;
            _installed = true;
            _activatedSeen = false;

            ApplyCachedPolicy();

            // The timeout is read when the gate runs, so a settings asset registered after Install is honored.
            Ads.InitializationGate = () => RemoteConfigApi.WaitUntilReadyAsync(RemoteConfigApi.Settings.FetchTimeoutSeconds);

            RemoteConfigApi.Activated += OnActivated;
            ApplyWhenReadyAsync(_epoch, RemoteConfigApi.Generation);
        }

        /// <summary>Last good <c>ads_policy</c> from an earlier session. It applies before any fetch can finish.</summary>
        private static void ApplyCachedPolicy()
        {
            string cached;
            try
            {
                cached = PlayerPrefs.GetString(PolicyCacheKey, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(AdsPolicyJson.LogPrefix + "Could not read the cached ads policy: " + exception.Message);
                return;
            }

            if (string.IsNullOrEmpty(cached)) return;

            var parsed = AdsPolicyJson.Parse(cached);
            if (parsed == null)
            {
                PlayerPrefs.DeleteKey(PolicyCacheKey);
                return;
            }

            Ads.ApplyPolicyOverride(parsed);
        }

        /// <summary>
        /// Remote Config can become ready without a successful fetch (timeout, offline, no SDK). Activated is not raised
        /// then, so apply what <c>Get</c> returns (the settings defaults). The cached policy stays untouched: defaults
        /// must not replace it.
        /// </summary>
        private static async void ApplyWhenReadyAsync(int epoch, int generation)
        {
            try
            {
                await RemoteConfigApi.WaitUntilReadyAsync(ReadyWaitSeconds);

                if (epoch != _epoch || generation != RemoteConfigApi.Generation) return;
                if (!RemoteConfigApi.IsReady || _activatedSeen) return;

                ApplyValues(includePolicy: false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // Activated can fire more than once (overlapping attempts, a late fetch after the timeout), so applying is idempotent.
        private static void OnActivated()
        {
            _activatedSeen = true;
            ApplyValues(includePolicy: true);
        }

        private static void ApplyValues(bool includePolicy)
        {
            ApplyAdsEnabled();
            if (includePolicy) ApplyPolicy();
            ApplyUnitIds();
        }

        private static void ApplyAdsEnabled()
        {
            // An absent key leaves ads on (spec 5.3: default true) instead of calling SetAdsEnabled.
            var raw = RemoteConfigApi.Get<string>(AdsRemoteConfigKeys.AdsEnabled, null);
            if (string.IsNullOrWhiteSpace(raw)) return;

            if (!RemoteConfigValueParser.TryParse<bool>(raw, out var enabled))
            {
                Debug.LogWarning(AdsPolicyJson.LogPrefix + "ads_enabled is not a valid bool. Ignoring it.");
                return;
            }

            Ads.SetAdsEnabled(enabled);
        }

        private static void ApplyPolicy()
        {
            var raw = RemoteConfigApi.Get<string>(AdsRemoteConfigKeys.AdsPolicy, null);
            if (string.IsNullOrWhiteSpace(raw)) return;

            var parsed = AdsPolicyJson.Parse(raw);
            if (parsed == null) return;

            // Only a policy that parses replaces the cache. Ads ignores the override if this session's rules are already fixed.
            PlayerPrefs.SetString(PolicyCacheKey, raw);
            PlayerPrefs.Save();
            Ads.ApplyPolicyOverride(parsed);
        }

        private static void ApplyUnitIds()
        {
            var raw = RemoteConfigApi.Get<string>(AdsRemoteConfigKeys.AdsUnitIds, null);
            if (string.IsNullOrWhiteSpace(raw)) return;

            // Last write wins per (provider, platform, format). Providers read their ids when they initialize, so ids that
            // arrive after that take effect from the next session.
            foreach (var entry in AdUnitIdsJson.Parse(raw))
            {
                AdUnitIdOverrides.Set(entry.provider, entry.platform, entry.format, entry.id);
            }
        }
    }
}
