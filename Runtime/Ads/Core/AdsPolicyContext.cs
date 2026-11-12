namespace RippleGame.Ads
{
    public enum AdsEntitlement
    {
        None,
        RemoveAds,
        RemoveAdsAndFreeRewards
    }

    /// <summary>Game state the ads policy layer needs; supplied by game code through <c>Ads.SetPolicyContext</c>.</summary>
    public readonly struct AdsPolicyContext
    {
        public AdsPolicyContext(int currentLevel, AdsEntitlement entitlement = AdsEntitlement.None, bool? hasUserConsent = null)
        {
            CurrentLevel = currentLevel;
            Entitlement = entitlement;
            HasUserConsent = hasUserConsent;
        }

        public int CurrentLevel { get; }
        public AdsEntitlement Entitlement { get; }
        /// <summary>Null when consent state is unknown.</summary>
        public bool? HasUserConsent { get; }
    }
}
