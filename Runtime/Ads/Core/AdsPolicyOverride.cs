using System.Collections.Generic;

namespace RippleGame.Ads
{
    /// <summary>
    /// Runtime changes to <see cref="AdsPolicySettings"/> (for example from remote config). Only fields that
    /// have a value replace the configured value; everything else keeps the asset value.
    /// </summary>
    public sealed class AdsPolicyOverride
    {
        public float? InterstitialCooldownSeconds;
        public int? InterstitialMaxPerSession;
        public int? InterstitialMinLevel;
        public int? InterstitialStartSession;
        /// <summary>Per-placement switch for interstitials. false disables the placement, true enables it.</summary>
        public Dictionary<AdPlacement, bool> PlacementEnabled;
        public bool? AppOpenEnabled;
        public float? AppOpenCooldownSeconds;
        public float? AppOpenMinBackgroundSeconds;
        public bool? RewardedFallbackEnabled;
        public bool? OrganicBanner;
        public bool? OrganicMrec;
        public bool? OrganicInterstitial;
        public bool? OrganicAppOpen;
        public float? BannerRefreshSeconds;
        public float? MrecRefreshSeconds;

        /// <summary>Returns a new settings instance; <paramref name="source"/> is never modified.</summary>
        public AdsPolicySettings ApplyTo(AdsPolicySettings source)
        {
            var result = source != null ? source.Clone() : new AdsPolicySettings();

            if (InterstitialCooldownSeconds.HasValue) result.Interstitial.CooldownSeconds = InterstitialCooldownSeconds.Value;
            if (InterstitialMaxPerSession.HasValue) result.Interstitial.MaxPerSession = InterstitialMaxPerSession.Value;
            if (InterstitialMinLevel.HasValue) result.Interstitial.MinLevel = InterstitialMinLevel.Value;
            if (InterstitialStartSession.HasValue) result.Interstitial.StartSession = InterstitialStartSession.Value;
            if (AppOpenEnabled.HasValue) result.AppOpen.Enabled = AppOpenEnabled.Value;
            if (AppOpenCooldownSeconds.HasValue) result.AppOpen.CooldownSeconds = AppOpenCooldownSeconds.Value;
            if (AppOpenMinBackgroundSeconds.HasValue) result.AppOpen.MinBackgroundSeconds = AppOpenMinBackgroundSeconds.Value;
            if (RewardedFallbackEnabled.HasValue) result.RewardedFallbackEnabled = RewardedFallbackEnabled.Value;
            if (OrganicBanner.HasValue) result.Organic.Banner = OrganicBanner.Value;
            if (OrganicMrec.HasValue) result.Organic.Mrec = OrganicMrec.Value;
            if (OrganicInterstitial.HasValue) result.Organic.Interstitial = OrganicInterstitial.Value;
            if (OrganicAppOpen.HasValue) result.Organic.AppOpen = OrganicAppOpen.Value;
            if (BannerRefreshSeconds.HasValue) result.BannerRefreshSeconds = BannerRefreshSeconds.Value;
            if (MrecRefreshSeconds.HasValue) result.MrecRefreshSeconds = MrecRefreshSeconds.Value;

            if (PlacementEnabled != null)
            {
                var disabled = result.Interstitial.DisabledPlacements;
                foreach (var entry in PlacementEnabled)
                {
                    disabled.RemoveAll(placement => placement == entry.Key);
                    if (!entry.Value)
                    {
                        disabled.Add(entry.Key);
                    }
                }
            }

            return result;
        }
    }
}
