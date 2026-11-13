# Changelog

## 0.4.0

- **Breaking:** the `ADS` and `FIREBASE` define symbols are gone. Code, the Hub and the build guard no longer read them. Remove them from Player Settings. `ADS_TEST` still selects the Test profile, and `ADS_DISABLED` is the No Ads profile. The Analytics page no longer has **Enable Firebase** or **Disable Firebase**.
- **Breaking:** a game that was on 0.3.x without the `ADS` symbol (the old No Ads setup) now builds as Production with `providerId` `admob`, because old configuration assets carry `admob`. Apply the **No Ads** build profile in RIPPLE Game > Open Hub > ADS (it sets `ADS_DISABLED`) or set `providerId` to `none`. A non-Android build of an AdMob game also needs `ADS_DISABLED` for that platform, because the profile writes Android symbols only.
- **Breaking:** AdMob and Firebase Analytics need their SDK installed from RIPPLE Game > Open Hub > SDK. Old `.unitypackage` imports (`Assets/GoogleMobileAds`, `Assets/Firebase`, `Assets/ExternalDependencyManager`, `Assets/MaxSdk`, `Assets/AppsFlyer`) must be removed by hand. The Hub recognises them by marker paths only an import creates (such as `Assets/GoogleMobileAds/Editor`, `Assets/MaxSdk/Scripts`, `Assets/Firebase/Plugins`) and refuses Setup for the SDK being installed while that SDK or one in its dependency chain still has them. Settings files the UPM packages create (`Assets/MaxSdk/Resources/AppLovinSettings.asset`, `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`) are not old copies.
- **Breaking:** `IAdsService` has two new members, `ShowMrec(MrecPosition)` and `HideMrec()`. A custom provider must implement them.
- **Breaking:** `AdRevenue` has a new `Platform` property (`admob` or `applovin_max`).
- **Breaking:** `AdPlacement` has three new values: `Pause`, `Replay` and `RewardedFallback`.

- **SDK page** in TOOL Hub (RIPPLE Game > Open Hub > SDK). It installs the pinned SDK set from checked tarballs into `Packages/RippleSdks` (Setup, Fix, Remove) and lists old copies under `Assets`. Every file is SHA-256 checked. Pinned: External Dependency Manager 1.2.186, Firebase app, analytics, remote-config, crashlytics and messaging 13.1.0, AppLovin MAX 8.5.1, AppsFlyer 6.17.900, Google Mobile Ads 11.0.0. A wrong version turns the matching assembly off and stops the build.
- **AppLovin MAX** ads provider next to AdMob. A game picks `none`, `admob` or `max` in `providerId`. Revenue is reported in micros with `Platform` `applovin_max`.
- **MREC** format on MAX: `Ads.ShowMrec(MrecPosition)` and `Ads.HideMrec()`.
- **Show rules** in the core, shared by both providers: interstitial cooldown, cap per session, `minLevel`, `startSession` and per-placement switches; app open rules; organic switches; banner and MREC refresh; time and audio pause. Games feed level, entitlement and consent through `Ads.SetPolicyContext`. Old 0.3.1 cooldown values move into the new rules by themselves.
- **Rewarded fallback:** with the option on, a rewarded request shows a ready interstitial when no rewarded ad is ready.
- **Remote Config** module (`RippleGame.RemoteConfig`) with a Firebase backend: `RemoteConfig.Get<T>`, `GetJson<T>`, `IsReady`, `Activated`. The keys `ads_enabled`, `ads_policy` and `ads_unit_ids` control Ads. Both providers read unit IDs and banner/MREC refresh when they initialize, after Remote Config; the last good `ads_policy` and `ads_unit_ids` are cached for the next session. AdMob ignores `refresh_seconds`. See [SDK setup](Documentation~/sdk/Setup.md).
- Ad loads that fail keep retrying at `Retry Max Delay Seconds` after the backoff ramp, instead of stopping after `Retry Max Attempts`.
- A game callback (`rewarded` or `completed`) that throws is logged and no longer skips the completion or the reload of the next ad.
- MAX revenue callbacks with no revenue (MAX reports -1) no longer raise a 0-micros `Paid` event.
- **Attribution** module (`RippleGame.Attribution`) with an AppsFlyer provider: `Attribution.Status` (`Unknown`, `Organic`, `NonOrganic`), ad revenue reporting and an Ads bridge that feeds the organic rules.
- `ad_impression` keeps `ad_platform` `AdMob` for AdMob and uses `AppLovin` for MAX.
- Newtonsoft Json (`com.unity.nuget.newtonsoft-json` 3.2.2) is now a package dependency.
- `Tools~/Check-Package.ps1` checks that every asmdef `versionDefines` entry matches `SdkCatalog.json` and refuses string literals shaped like a MAX SDK key or an AppsFlyer dev key.
- [Device checklist](Documentation~/DeviceChecklist-0.4.0.md) for the Android test of both providers.

Upgrading from 0.3.x: follow [Migration](Documentation~/Migration.md).

## 0.3.1

- Localization sync accepts a Sheet whose first header row has a blank key cell and `English` in the next cell.

## 0.3.0

- **Breaking:** removed `Flow`, `FlowIntegration`, `Progression`, `Economy` and `Persistence`. Rules, results, rewards, coins and levels now belong to each game. A game using them keeps tag `v0.2.1` or moves that code into the game. See [Migration](Documentation~/Migration.md).
- The `RIPPLE Game` menu now has only **Open Hub**, and every tool opens from TOOL Hub. The separate Scene Switch, Texture and Android build windows are removed. Scripts calling the old menu paths must change.
- TOOL Hub is simpler: less text, compact overview, help moved to tooltips. Every action works as before.
- Every TOOL Hub page has a **Pop out** button that opens it in its own dockable tab.
- The TOOL Hub Save page refuses to create a second save key anywhere under `Assets`, which used to break compilation with a duplicate class.

## 0.2.1

- Localization sync now writes every language column, including `Arabic` and `Unknown`. Arabic text is written as is, without letter shaping.
- Localization sync stops when a cell shows a formula error such as `#ERROR!`, so translations made with `GOOGLETRANSLATE` cannot put error text in a game.

## 0.2.0

- Encrypted save file with typed sections, batched writes, atomic replacement, backup recovery, version migration and app lifecycle writes.
- Level flow state machine: win, lose, revive, rewards, ads and analytics order, with gameplay and screens supplied through interfaces.
- Level progression, and wallet and progress stores on a save file.
- Scene loader with loading progress, and a safe area component.
- TOOL Hub Save page: create a game's key and inspect encrypted save files.
- TOOL Hub Update page: finds new release tags, opens the changes and installs the new version on request. The Editor logs a notice once per launch when one is available.
- Developer guide in Vietnamese covering every module.
- Localization: one translation file per language, language choice from the saved value or device, and switching while the game runs.
- `LocalizedText` component for TextMeshPro, which follows language changes.
- TOOL Hub Localization page: sync translation files from a Google Sheet. A Sheet with errors changes no files.
- The `TOOL` menu is now `RIPPLE Game` and sits next to `Window`. Scripts calling `TOOL/...` menu paths must use the new name.
- API from 0.1.0 is unchanged.

## 0.1.0

- Independent root UPM package for Git installation and versioned updates.
- Modular Ads, Analytics and their bridge, preserving existing APIs and assembly names.
- Integrated TOOL Hub with RIPPLE Game by Namdev26 credit; reusable Scene, Build, Texture and JSON export utilities.
- Generic popup host/lifecycle and currency wallet with a caller-owned persistence key.
- Source-only samples, tests, release export, and installation/migration documentation.
