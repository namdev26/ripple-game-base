using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RippleGame.Ads.RemoteConfigBridge.Tests
{
    public sealed class AdsPolicyJsonTests
    {
        [Test]
        public void Parse_OnlyPresentFieldsAreSet()
        {
            var result = AdsPolicyJson.Parse("{\"interstitial\":{\"min_level\":3}}");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.InterstitialMinLevel, Is.EqualTo(3));
            Assert.That(result.InterstitialCooldownSeconds, Is.Null);
            Assert.That(result.InterstitialMaxPerSession, Is.Null);
            Assert.That(result.InterstitialStartSession, Is.Null);
            Assert.That(result.PlacementEnabled, Is.Null);
            Assert.That(result.AppOpenEnabled, Is.Null);
            Assert.That(result.RewardedFallbackEnabled, Is.Null);
            Assert.That(result.OrganicBanner, Is.Null);
            Assert.That(result.BannerRefreshSeconds, Is.Null);
        }

        [Test]
        public void Parse_WrongTypeFieldSkipped_OthersApplied()
        {
            var result = AdsPolicyJson.Parse(
                "{\"interstitial\":{\"cooldown_seconds\":\"abc\",\"min_level\":3,\"max_per_session\":true,\"start_session\":2.5}," +
                "\"app_open\":{\"enabled\":\"yes\",\"cooldown_seconds\":30}," +
                "\"banner\":5,\"mrec\":{\"refresh_seconds\":15}}");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.InterstitialCooldownSeconds, Is.Null, "string where a number is expected");
            Assert.That(result.InterstitialMaxPerSession, Is.Null, "bool where an int is expected");
            Assert.That(result.InterstitialStartSession, Is.Null, "fraction where an int is expected");
            Assert.That(result.InterstitialMinLevel, Is.EqualTo(3));
            Assert.That(result.AppOpenEnabled, Is.Null, "string where a bool is expected");
            Assert.That(result.AppOpenCooldownSeconds, Is.EqualTo(30f));
            Assert.That(result.BannerRefreshSeconds, Is.Null, "a number where a section is expected");
            Assert.That(result.MrecRefreshSeconds, Is.EqualTo(15f));
        }

        [Test]
        public void Parse_FullSpecExample()
        {
            const string json = "{\"interstitial\":{\"cooldown_seconds\":60,\"max_per_session\":0,\"min_level\":3,\"start_session\":2," +
                                "\"placements\":{\"replay\":false,\"pause\":false}}," +
                                "\"app_open\":{\"enabled\":true,\"cooldown_seconds\":30,\"min_background_seconds\":5}," +
                                "\"rewarded_fallback\":{\"enabled\":true}," +
                                "\"organic\":{\"banner\":true,\"mrec\":true,\"interstitial\":false,\"app_open\":true}," +
                                "\"banner\":{\"refresh_seconds\":15},\"mrec\":{\"refresh_seconds\":20.5},\"future_section\":{\"x\":1}}";

            var result = AdsPolicyJson.Parse(json);

            Assert.That(result.InterstitialCooldownSeconds, Is.EqualTo(60f));
            Assert.That(result.InterstitialMaxPerSession, Is.EqualTo(0));
            Assert.That(result.InterstitialMinLevel, Is.EqualTo(3));
            Assert.That(result.InterstitialStartSession, Is.EqualTo(2));
            Assert.That(result.PlacementEnabled[AdPlacement.Replay], Is.False);
            Assert.That(result.PlacementEnabled[AdPlacement.Pause], Is.False);
            Assert.That(result.AppOpenEnabled, Is.True);
            Assert.That(result.AppOpenCooldownSeconds, Is.EqualTo(30f));
            Assert.That(result.AppOpenMinBackgroundSeconds, Is.EqualTo(5f));
            Assert.That(result.RewardedFallbackEnabled, Is.True);
            Assert.That(result.OrganicBanner, Is.True);
            Assert.That(result.OrganicMrec, Is.True);
            Assert.That(result.OrganicInterstitial, Is.False);
            Assert.That(result.OrganicAppOpen, Is.True);
            Assert.That(result.BannerRefreshSeconds, Is.EqualTo(15f));
            Assert.That(result.MrecRefreshSeconds, Is.EqualTo(20.5f));
        }

        [Test]
        public void Parse_PlacementsUseSnakeCase()
        {
            var result = AdsPolicyJson.Parse(
                "{\"interstitial\":{\"placements\":{\"level_complete\":false,\"rewarded_fallback\":false,\"home\":true,\"level_start\":\"no\",\"unknown_place\":false}}}");

            Assert.That(result.PlacementEnabled.Count, Is.EqualTo(3));
            Assert.That(result.PlacementEnabled[AdPlacement.LevelComplete], Is.False);
            Assert.That(result.PlacementEnabled[AdPlacement.RewardedFallback], Is.False);
            Assert.That(result.PlacementEnabled[AdPlacement.Home], Is.True);
            Assert.That(result.PlacementEnabled.ContainsKey(AdPlacement.LevelStart), Is.False, "wrong type is skipped");
        }

        [Test]
        public void Parse_EveryPlacementHasASnakeCaseName()
        {
            var expected = new[] { "default", "home", "level_start", "level_complete", "retry", "double_reward", "extra_life", "pause", "replay", "rewarded_fallback" };
            var actual = ((AdPlacement[])System.Enum.GetValues(typeof(AdPlacement))).Select(p => AdsPolicyJson.ToSnakeCase(p.ToString()));

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_InvalidJson_ReturnsNull_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[RippleGame Ads RemoteConfig\] ads_policy is not valid JSON"));

            Assert.That(AdsPolicyJson.Parse("{not json"), Is.Null);
        }

        [Test]
        public void Parse_NotAnObject_ReturnsNull_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[RippleGame Ads RemoteConfig\] ads_policy is not a JSON object"));

            Assert.That(AdsPolicyJson.Parse("[1,2]"), Is.Null);
        }

        [Test]
        public void Parse_EmptyObject_ReturnsEmptyOverride()
        {
            var result = AdsPolicyJson.Parse("{}");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.InterstitialCooldownSeconds, Is.Null);
        }

        [Test]
        public void UnitIds_SkipsEmptyAndUnknownFormats()
        {
            const string json = "{\"max\":{\"android\":{\"interstitial\":\"max-inter\",\"rewarded\":\"\",\"native\":\"x\",\"app_open\":\"max-open\"}}," +
                                "\"AdMob\":{\"iOS\":{\"banner\":\"admob-banner\",\"mrec\":\"admob-mrec\",\"interstitial\":42}},\"junk\":5}";

            var result = AdUnitIdsJson.Parse(json).ToList();

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Does.Contain(("max", "android", AdFormat.Interstitial, "max-inter")));
            Assert.That(result, Does.Contain(("max", "android", AdFormat.AppOpen, "max-open")));
            Assert.That(result, Does.Contain(("admob", "ios", AdFormat.Banner, "admob-banner")));
            Assert.That(result, Does.Contain(("admob", "ios", AdFormat.Mrec, "admob-mrec")));
        }

        [Test]
        public void UnitIds_InvalidJson_YieldsNothing_Warns()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"ads_unit_ids is not valid JSON"));

            Assert.That(AdUnitIdsJson.Parse("{oops").Any(), Is.False);
        }
    }
}
