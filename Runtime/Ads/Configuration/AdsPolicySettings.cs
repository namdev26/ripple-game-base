using System;
using System.Collections.Generic;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>Provider-independent presentation rules consumed by the ads policy layer.</summary>
    [Serializable]
    public sealed class AdsPolicySettings
    {
        [SerializeField] private InterstitialPolicySettings interstitial = new InterstitialPolicySettings();
        [SerializeField] private AppOpenPolicySettings appOpen = new AppOpenPolicySettings();
        [Tooltip("When a rewarded ad is not ready, allow the fallback placement to grant the reward.")]
        [SerializeField] private bool rewardedFallbackEnabled;
        [SerializeField] private OrganicPolicySettings organic = new OrganicPolicySettings();
        [Tooltip("Seconds between banner refreshes. 0 keeps the network's own refresh.")]
        [Min(0f)] [SerializeField] private float bannerRefreshSeconds;
        [Tooltip("Seconds between MREC refreshes. 0 keeps the network's own refresh.")]
        [Min(0f)] [SerializeField] private float mrecRefreshSeconds;
        [Tooltip("Pause Time.timeScale and audio while a full-screen ad is showing.")]
        [SerializeField] private bool pauseTimeAndAudio = true;

        public InterstitialPolicySettings Interstitial => interstitial;
        public AppOpenPolicySettings AppOpen => appOpen;
        public bool RewardedFallbackEnabled { get => rewardedFallbackEnabled; set => rewardedFallbackEnabled = value; }
        public OrganicPolicySettings Organic => organic;
        public float BannerRefreshSeconds { get => bannerRefreshSeconds; set => bannerRefreshSeconds = value; }
        public float MrecRefreshSeconds { get => mrecRefreshSeconds; set => mrecRefreshSeconds = value; }
        public bool PauseTimeAndAudio { get => pauseTimeAndAudio; set => pauseTimeAndAudio = value; }

        /// <summary>Deep copy, so runtime overrides never write back into the shared asset.</summary>
        public AdsPolicySettings Clone() => JsonUtility.FromJson<AdsPolicySettings>(JsonUtility.ToJson(this));
    }

    [Serializable]
    public sealed class InterstitialPolicySettings
    {
        [Tooltip("Minimum elapsed real time between two interstitial impressions.")]
        [Min(0f)] [SerializeField] private float cooldownSeconds = 45f;
        [Tooltip("Maximum interstitial impressions in one app session. Set 0 for no cap.")]
        [Min(0)] [SerializeField] private int maxPerSession = 4;
        [Tooltip("Interstitials are suppressed below this level.")]
        [Min(0)] [SerializeField] private int minLevel;
        [Tooltip("Interstitials are suppressed before this app session number (1 = first session).")]
        [Min(1)] [SerializeField] private int startSession = 1;
        [SerializeField] private List<AdPlacement> disabledPlacements = new List<AdPlacement>();

        public float CooldownSeconds { get => cooldownSeconds; set => cooldownSeconds = value; }
        public int MaxPerSession { get => maxPerSession; set => maxPerSession = value; }
        public int MinLevel { get => minLevel; set => minLevel = value; }
        public int StartSession { get => startSession; set => startSession = value; }
        public List<AdPlacement> DisabledPlacements => disabledPlacements;
    }

    [Serializable]
    public sealed class AppOpenPolicySettings
    {
        [SerializeField] private bool enabled = true;
        [Min(0f)] [SerializeField] private float cooldownSeconds = 20f;
        [Min(0f)] [SerializeField] private float minBackgroundSeconds = 5f;

        public bool Enabled { get => enabled; set => enabled = value; }
        public float CooldownSeconds { get => cooldownSeconds; set => cooldownSeconds = value; }
        public float MinBackgroundSeconds { get => minBackgroundSeconds; set => minBackgroundSeconds = value; }
    }

    /// <summary>Which formats may be shown to organic (non-attributed) users.</summary>
    [Serializable]
    public sealed class OrganicPolicySettings
    {
        [SerializeField] private bool banner = true;
        [SerializeField] private bool mrec = true;
        [SerializeField] private bool interstitial = true;
        [SerializeField] private bool appOpen = true;

        public bool Banner { get => banner; set => banner = value; }
        public bool Mrec { get => mrec; set => mrec = value; }
        public bool Interstitial { get => interstitial; set => interstitial = value; }
        public bool AppOpen { get => appOpen; set => appOpen = value; }
    }
}
