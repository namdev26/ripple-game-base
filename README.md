# RIPPLE Game Base

**RIPPLE Game by Namdev26**

A reusable Unity 6 UPM package. Runtime modules and editor tools are isolated by assembly. No original game scenes, prefabs, levels, art, live configuration or third-party SDK distributions are included; the SDK page downloads the optional SDKs on request.

## Install from Git

In Unity, open **Window > Package Manager > + > Install package from Git URL**, then enter:

```text
https://github.com/namdev26/ripple-game-base.git#v0.4.0
```

Git must be installed on the machine. Never put passwords or tokens in the URL or project manifest.

You can also add this entry to the consuming project's existing `Packages/manifest.json` dependencies, preserving its other entries:

```json
"com.ripplegame.base": "https://github.com/namdev26/ripple-game-base.git#v0.4.0"
```

Use Unity 6000.0 or newer. Remove or migrate previous copies of these tools and the separate RippleGame Ads/Analytics packages before installing; duplicate assembly names cannot coexist. Upgrading from 0.3.x changes the `ADS` and `FIREBASE` symbols and the SDK install: see [Migration](Documentation~/Migration.md).

## Modules

| Folder | Assembly / purpose |
| --- | --- |
| Runtime/Save | `RippleGame.Base.Save`: encrypted save file with typed sections, atomic writes and recovery. See [Save](Documentation~/Save.md) |
| Runtime/Scenes | `RippleGame.Base.Scenes`: load scenes by a caller-supplied name, with loading progress |
| Runtime/Localization | `RippleGame.Base.Localization`: translation files, language choice and runtime switching |
| Runtime/LocalizationUI | `RippleGame.Base.Localization.UI`: `LocalizedText` for TextMeshPro, compiled only when `com.unity.ugui` is installed |
| Runtime/UI | `RippleGame.Base.UI`: caller-supplied popup prefabs, one active view, safe area |
| Runtime/Ads | `RippleGame.Ads.Runtime`: the `Ads` API, show rules (cooldown, session cap, min level, organic, entitlement, rewarded fallback) and provider registry. Needs no SDK |
| Runtime/Ads.AdMob, Runtime/Ads.Max | `RippleGame.Ads.AdMob` and `RippleGame.Ads.Max` (with MREC): ad providers, compiled only with Google Mobile Ads 11.0.0 or AppLovin MAX 8.5.1 installed. See [SDK setup](Documentation~/sdk/Setup.md) |
| Runtime/Analytics, Runtime/Analytics.Firebase | `RippleGame.Analytics.Runtime` API with Console fallback, and a Firebase backend compiled only with Firebase Analytics 13.1.0 |
| Runtime/AnalyticsAdsBridge | Optional bridge between the included Ads and Analytics modules |
| Runtime/Firebase | `RippleGame.Firebase`: starts Firebase once for Analytics and Remote Config |
| Runtime/RemoteConfig, Runtime/RemoteConfig.Firebase | `RippleGame.RemoteConfig`: typed `RemoteConfig.Get<T>` with defaults, and a Firebase backend (Firebase Remote Config 13.1.0) |
| Runtime/Ads.RemoteConfigBridge | Applies the `ads_enabled`, `ads_policy` and `ads_unit_ids` keys to Ads |
| Runtime/Attribution, Runtime/Attribution.AppsFlyer | `RippleGame.Attribution`: organic status and ad revenue reporting, with an AppsFlyer 6.17.900 provider |
| Runtime/Ads.AttributionBridge | Sends ad revenue to Attribution and feeds the organic status into the Ads rules |
| Editor/Tools | `RIPPLE Game > Open Hub`, Scene Switch, Android Build, Texture, Save, package update and JSON export tools |
| Editor/Sdk | The Hub **SDK** page: installs, fixes and removes the pinned optional SDKs, and blocks a build with a wrong SDK version |
| Editor/Ads, Editor/Analytics | Project configuration and build validation |

Open **RIPPLE Game > Open Hub** for settings and actions in one window. The third-party SDKs (Google Mobile Ads, AppLovin MAX, Firebase, AppsFlyer) are optional and are not part of this package. The **SDK** page installs the exact versions this release was built against, from checked tarballs, with one button each. A provider's code compiles only when its pinned SDK is installed, so a game with no SDK still compiles, and the Analytics Console fallback and Remote Config defaults work. There are no `ADS` or `FIREBASE` define symbols any more. See [SDK setup](Documentation~/sdk/Setup.md).

## Project-owned configuration

ADS, Analytics, Remote Config and Attribution menus create assets under the consuming project's Assets folder. Keep production IDs, Firebase project files, scene policies, keystores, passwords, prefabs and save keys in that game. A package update does not recreate or overwrite these assets.

The base does not impose a reward formula, game flow, level schema or scene name. Supply those from your game. ADS defaults exclude generic loading scene names; edit the new project's configuration to match its own scene policy.

## Update and edit

When a new version is released, the Editor logs a notice and **RIPPLE Game > Open Hub > Update** offers it. You can also replace the Git revision suffix by hand and let Package Manager resolve it. Commit the consuming project's manifest and lockfile afterwards. Tags pin versions; upstream pushes do not silently change a tagged install.

Shared source is edited in this repository's normal writable clone. Unity's Git package cache is a consumer copy. For development, use **Install package from disk** and select this clone's package.json, or make an embedded development copy. After editing and testing, increment package.json, update CHANGELOG.md, commit and create a new tag. Return consumers to a Git dependency to receive future releases.

Source-only ZIP fallback: run `Tools~/Export-Source.ps1`; the result is under ignored `Artifacts~`. Add `com.ripplegame.base` to the consuming project's `testables` and install Unity Test Framework to run the included EditMode/PlayMode tests.

See the [developer guide in Vietnamese](Documentation~/HuongDan.md), [SDK setup](Documentation~/sdk/Setup.md), [Save](Documentation~/Save.md), [Usage](Documentation~/Usage.md), [Migration](Documentation~/Migration.md), [Device checklist](Documentation~/DeviceChecklist-0.4.0.md), [Verification](Documentation~/Verification.md) and [Changelog](CHANGELOG.md).
