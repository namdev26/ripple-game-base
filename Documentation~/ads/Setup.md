# Setup

1. Install the combined `com.ripplegame.base` UPM package from its tagged Git URL.
2. Open **RIPPLE Game > Open Hub > SDK** and press **Setup** on the SDK of the provider you want: **Google Mobile Ads** for AdMob, or **AppLovin MAX**. See [SDK setup](../sdk/Setup.md). Remove old copies under `Assets` first (`Assets/GoogleMobileAds`, `Assets/MaxSdk`, `Assets/Firebase`, `Assets/ExternalDependencyManager`).
3. For AdMob, open **Assets > Google Mobile Ads > Settings** once, so the plugin creates its settings asset.
4. Open **RIPPLE Game > Open Hub > ADS** and press **Create Configuration**.
5. In the configuration, set `providerId` to `admob` or `max` (a new configuration starts at `none`, which shows no ads). The ADS page shows whether that provider's SDK is compiled.
   - **AdMob:** new configurations already contain Google Android demo IDs in the editable Test profile. Fill the Production IDs before a production build.
   - **MAX:** fill the **Max Test** and **Max Production** profiles with the SDK key and the unit IDs per format and platform. Also enter the SDK key in AppLovin's Integration Manager.
   - Disabled formats do not require an ID. Existing configurations are preserved.
6. Open your game's startup scene, then press **Add Ads Object to Active Scene** on the ADS page and save that scene.
7. Press **Configure** on the **Ads Test** row and make a device build.
8. Call `Ads.SetPolicyContext` from game code so level, entitlement and consent rules work. See [SDK setup](../sdk/Setup.md#game-code-for-the-ads-rules).

During development, use test IDs or configure a test device. Do not click live production ads while testing.

The `[RippleGame Ads]` GameObject is saved in the startup scene chosen by the consuming game. It uses `DontDestroyOnLoad`, so it loads configuration once and stays available across that game's scene transitions.

Banner routing includes all build scenes automatically, including scenes added later. `Banner Excluded Scene Names` in the configuration defaults to `Loading` and `LoadingScene`. Scenes outside the build do not show an automatic banner.

## Show rules

The rules live in the `Policy` block of `AdsConfiguration` and apply to both providers. Remote Config can override them (key `ads_policy`, see [SDK setup](../sdk/Setup.md#remote-config)).

| Rule | Default |
| --- | --- |
| Interstitial cooldown | 45 seconds |
| Interstitial cap per session | 4 (0 means no cap) |
| Interstitial minimum level | 0 (off) |
| Interstitial first session | 1 |
| Interstitial per placement | all on |
| App open | on, 20 second cooldown, 5 seconds in the background |
| Rewarded fallback | off |
| Organic users see banner, MREC, interstitial, app open | all on |
| Banner and MREC refresh | 0 (the network refreshes) |
| Pause time and audio during full-screen ads | on |

Rewarded fallback shows a ready interstitial, and grants the reward when it closes, if no rewarded ad is ready. Placements are `Default`, `Home`, `LevelStart`, `LevelComplete`, `Retry`, `DoubleReward`, `ExtraLife`, `Pause`, `Replay` and `RewardedFallback`. MREC (`Ads.ShowMrec`, `Ads.HideMrec`) works with MAX.
