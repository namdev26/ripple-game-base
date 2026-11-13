using System;
using UnityEngine;

namespace RippleGame.Ads
{
    /// <summary>AppLovin MAX settings for one environment (test or production).</summary>
    [Serializable]
    public sealed class MaxAdUnitConfiguration
    {
        [SerializeField] private string sdkKey;
        [Header("Android")]
        [SerializeField] private MaxPlatformUnitIds android = new MaxPlatformUnitIds();
        [Header("iOS")]
        [SerializeField] private MaxPlatformUnitIds ios = new MaxPlatformUnitIds();

        [Header("Formats")]
        [SerializeField] private bool enableBanner = true;
        [SerializeField] private bool enableInterstitial = true;
        [SerializeField] private bool enableRewarded = true;
        [SerializeField] private bool enableAppOpen = true;
        [SerializeField] private bool enableMrec = true;
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;

        public string SdkKey => sdkKey;
        public MaxPlatformUnitIds Android => android;
        public MaxPlatformUnitIds Ios => ios;
        public bool EnableBanner => enableBanner;
        public bool EnableInterstitial => enableInterstitial;
        public bool EnableRewarded => enableRewarded;
        public bool EnableAppOpen => enableAppOpen;
        public bool EnableMrec => enableMrec;
        public BannerPosition BannerPosition => bannerPosition;
    }

    [Serializable]
    public sealed class MaxPlatformUnitIds
    {
        [SerializeField] private string banner;
        [SerializeField] private string interstitial;
        [SerializeField] private string rewarded;
        [SerializeField] private string appOpen;
        [SerializeField] private string mrec;

        public string Banner => banner;
        public string Interstitial => interstitial;
        public string Rewarded => rewarded;
        public string AppOpen => appOpen;
        public string Mrec => mrec;
    }
}
