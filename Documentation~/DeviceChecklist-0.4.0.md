# Android device checklist - 0.4.0

Run this on a real Android device with a Development build. Use a test device registered with the ad network, and never tap live production ads. Write `pass`, `fail` or `not run` in each Result cell, with a note for any failure. A line that was not run stays `not run`.

Setup before you start:

- SDKs installed from RIPPLE Game > Open Hub > SDK. The SDK page shows every SDK you use as **Installed**.
- Build with the **Ads Test** profile (`ADS_TEST`) so Remote Config fetches at once.
- `AdsConfiguration` filled for the provider under test. For MAX, use a real SDK key and real ad units with a test device.
- `google-services.json` under `Assets/`; Attribution Settings with a dev key (see [SDK setup](sdk/Setup.md)).
- `adb logcat -s Unity` open, and `adb shell setprop debug.firebase.analytics.app <package.name>` set for the Firebase DebugView.

Device, Android version, build date and tester:

## A. AppLovin MAX (`providerId` = `max`)

| # | Step | Expected | Result |
| --- | --- | --- | --- |
| A1 | Cold start, wait on the first scene | Log shows MAX SDK initialized; no error; `Ads.IsReady` turns true for interstitial and rewarded | |
| A2 | Banner | Banner shows at the configured position, hides in the Loading scene, returns after a full-screen ad closes | |
| A3 | MREC: `Ads.ShowMrec(MrecPosition.Center)`, then `Ads.HideMrec()` | MREC shows at the position and hides again | |
| A4 | Interstitial | Shows at a placement, game is paused (time and audio) and resumes after close | |
| A5 | Rewarded | Shows, reward callback runs only after the reward is earned, game resumes | |
| A6 | App open: background the app for more than the minimum seconds, then return | App open ad shows once; no ad when returning sooner than the minimum | |
| A7 | Rewarded fallback: turn it on, make rewarded not ready (airplane mode after init, or an exhausted unit), press the rewarded button | A ready interstitial shows, the reward is granted when it closes, result is `Success`, cooldown restarts | |
| A8 | Fallback with a failing interstitial | No reward, result is `Failed` | |
| A9 | Cooldown: show an interstitial, request another at once | Second request returns `Cooldown`; it shows again after the cooldown | |
| A10 | Session cap: reach `max_per_session` interstitials | Next request returns `Cooldown`; a new app launch resets the count | |
| A11 | Organic block: set `organic.interstitial` to false and use an organic install (or a build before AppsFlyer reports) | Interstitial does not show for an organic user, and still shows for a non-organic user | |
| A12 | `ads_policy` override: in Firebase set `{"interstitial":{"cooldown_seconds":90}}` and publish | Current session keeps its rules. After a full app restart, the interstitial cooldown is 90 seconds | |
| A13 | `ads_policy` cleared: publish `{}` and restart twice | Cooldown returns to the AdsConfiguration value | |
| A14 | `ads_enabled` = false and restart | No ad shows; rewarded grants its reward at once with `Success` | |
| A15 | `ads_unit_ids` override with a different valid interstitial ID, restart | Log or network shows the new unit loading | |
| A16 | Revenue in the AppsFlyer debug view | An ad revenue event for each paid impression, mediation network AppLovin MAX | |
| A17 | Revenue in Firebase DebugView | `ad_impression` with `ad_platform` = `AppLovin`, `value`, `currency` | |

## B. AdMob (`providerId` = `admob`)

| # | Step | Expected | Result |
| --- | --- | --- | --- |
| B1 | Cold start | AdMob initialized; no error; interstitial and rewarded become ready | |
| B2 | Banner | Banner shows at the configured position, hides in the Loading scene, returns after a full-screen ad closes | |
| B3 | MREC: `Ads.ShowMrec(...)` | Nothing shows, one warning in the log (MREC is not supported on AdMob in 0.4.0) | |
| B4 | Interstitial | Shows, game paused and resumed | |
| B5 | Rewarded | Reward callback runs only after the reward is earned | |
| B6 | App open: background longer than the minimum, return | App open shows once; no ad on a quick return | |
| B7 | Rewarded fallback with rewarded not ready | A ready interstitial shows and the reward is granted when it closes | |
| B8 | Cooldown: two interstitial requests in a row | Second returns `Cooldown` | |
| B9 | Organic block with `organic.interstitial` false | Blocked for organic users only | |
| B10 | `ads_policy` override on the next session (as A12) | New value applies after a full restart, not before | |
| B11 | `ads_enabled` = false | No ad; rewarded grants at once | |
| B12 | Revenue in the AppsFlyer debug view | Ad revenue event, mediation network Google AdMob | |
| B13 | Revenue in Firebase DebugView | `ad_impression` with `ad_platform` = `AdMob`. If AdMob is linked to Firebase, `Log Ad Revenue` is off and none is expected | |

## C. No SDK and No Ads

| # | Step | Expected | Result |
| --- | --- | --- | --- |
| C1 | Build with the **No Ads** profile (`ADS_DISABLED`) | No ad shows; rewarded reports `Disabled` and never grants a reward | |
| C2 | Build with `providerId` = `none` | Game runs; ads report no provider; no build error | |
