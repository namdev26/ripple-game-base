using System;
using System.Collections.Generic;
using RippleGame.RemoteConfig;
using UnityEngine;
using RemoteConfigApi = RippleGame.RemoteConfig.RemoteConfig;

namespace RippleGame.Ads.RemoteConfigBridge
{
    /// <summary>
    /// Connects Remote Config to the Ads policy layer: applies <c>ads_enabled</c>, <c>ads_policy</c> and
    /// <c>ads_unit_ids</c>, caches the policy override and the unit ids for the next session, and makes Ads wait for
    /// Remote Config (bounded by its fetch timeout) before the provider initializes. Everything runs on the main thread.
    /// </summary>
    internal static class AdsRemoteConfigBridge
    {
        internal const string PolicyCacheKey = "ripplegame.ads.policy_override_cache";
        internal const string UnitIdsCacheKey = "ripplegame.ads.unit_ids_cache";

        private const float GateGraceSeconds = 0.5f;

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
            ApplyCachedUnitIds();

            // Providers read unit ids and refresh values when they initialize, which is right after this gate. Applying the
            // ready-path values inside the gate guarantees they are in place first. Remote Config marks itself ready when
            // its own fetch timeout ends, and it starts before the first scene, so it is ready before this wait ends; the
            // grace only keeps the two equal timers from racing. The timeout is read when the gate runs, so a settings
            // asset registered after Install is honored.
            var epoch = _epoch;
            Ads.InitializationGate = async () =>
            {
                await RemoteConfigApi.WaitUntilReadyAsync(RemoteConfigApi.Settings.FetchTimeoutSeconds + GateGraceSeconds);
                if (epoch == _epoch && RemoteConfigApi.IsReady && !_activatedSeen) ApplyValues(fromActivation: false);
            };

            RemoteConfigApi.Activated += OnActivated;
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
        /// Last good <c>ads_unit_ids</c> from an earlier session, so ids that arrived after the provider initialized
        /// take effect now. Applied at install, before anything can initialize a provider.
        /// </summary>
        private static void ApplyCachedUnitIds()
        {
            string cached;
            try
            {
                cached = PlayerPrefs.GetString(UnitIdsCacheKey, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(AdsPolicyJson.LogPrefix + "Could not read the cached ads unit ids: " + exception.Message);
                return;
            }

            if (string.IsNullOrEmpty(cached)) return;

            if (!AdUnitIdsJson.TryParse(cached, out var entries))
            {
                PlayerPrefs.DeleteKey(UnitIdsCacheKey);
                return;
            }

            SetUnitIds(entries);
        }

        // Activated can fire more than once (overlapping attempts, a late fetch after the timeout), so applying is idempotent.
        private static void OnActivated()
        {
            _activatedSeen = true;
            ApplyValues(fromActivation: true);
        }

        /// <param name="fromActivation">
        /// True for fetched values: the policy is applied and cached, and unit ids are cached. False when Remote Config
        /// became ready without a successful fetch (timeout, offline, no SDK): Activated is not raised then, so the
        /// values <c>Get</c> returns (the settings defaults) apply for this session only. Defaults never replace a cache.
        /// </param>
        private static void ApplyValues(bool fromActivation)
        {
            ApplyAdsEnabled();
            if (fromActivation) ApplyPolicy();
            ApplyUnitIds(fromActivation);
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

        private static void ApplyUnitIds(bool cache)
        {
            // A missing or blank key keeps the cache (as for ads_policy); publish {} to clear it.
            var raw = RemoteConfigApi.Get<string>(AdsRemoteConfigKeys.AdsUnitIds, null);
            if (string.IsNullOrWhiteSpace(raw)) return;

            if (!AdUnitIdsJson.TryParse(raw, out var entries)) return;

            // Providers read their ids when they initialize (after the gate). Ids that arrive later are cached and
            // applied at the start of the next session.
            if (cache)
            {
                PlayerPrefs.SetString(UnitIdsCacheKey, raw);
                PlayerPrefs.Save();
            }

            SetUnitIds(entries);
        }

        // Last write wins per (provider, platform, format).
        private static void SetUnitIds(IEnumerable<(string provider, string platform, AdFormat format, string id)> entries)
        {
            foreach (var entry in entries)
            {
                AdUnitIdOverrides.Set(entry.provider, entry.platform, entry.format, entry.id);
            }
        }
    }
}
