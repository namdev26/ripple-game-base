namespace RippleGame.Analytics
{
    /// <summary>Standard event, param and user property names. Firebase recommended names where they exist.</summary>
    public static class AnalyticsNames
    {
        public static class Events
        {
            public const string LevelStart = "level_start";
            public const string LevelComplete = "level_complete";
            public const string LevelFail = "level_fail";
            public const string TutorialBegin = "tutorial_begin";
            public const string TutorialStep = "tutorial_step";
            public const string TutorialComplete = "tutorial_complete";
            public const string EarnVirtualCurrency = "earn_virtual_currency";
            public const string SpendVirtualCurrency = "spend_virtual_currency";
            public const string ScreenView = "screen_view";
            public const string AdImpression = "ad_impression";
            public const string AdShow = "ad_show";
            public const string AdShowFail = "ad_show_fail";
            // "ad_reward" is reserved by Firebase.
            public const string AdReward = "ad_reward_earned";
        }

        public static class Params
        {
            public const string Level = "level";
            public const string LevelName = "level_name";
            public const string Attempt = "attempt";
            public const string Mode = "mode";
            public const string DurationSec = "duration_sec";
            public const string Score = "score";
            public const string FailReason = "fail_reason";
            public const string TutorialId = "tutorial_id";
            public const string Step = "step";
            public const string VirtualCurrencyName = "virtual_currency_name";
            public const string Value = "value";
            public const string Source = "source";
            public const string ItemName = "item_name";
            public const string ScreenName = "screen_name";
            public const string ScreenClass = "screen_class";
            public const string AdPlatform = "ad_platform";
            public const string AdSource = "ad_source";
            public const string AdFormat = "ad_format";
            public const string AdUnitName = "ad_unit_name";
            public const string Currency = "currency";
            public const string Placement = "placement";
            public const string Error = "error";
        }

        public static class UserProperties
        {
            public const string MaxLevel = "max_level";
        }
    }
}
