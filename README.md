# RIPPLE Game Base

**RIPPLE Game by Namdev26**

A reusable Unity 6 UPM package. Runtime modules and editor tools are isolated by assembly. No original game scenes, prefabs, levels, art, live configuration or third-party SDK distributions are included.

## Install from Git

In Unity, open **Window > Package Manager > + > Install package from Git URL**, then enter:

```text
https://github.com/namdev26/ripple-game-base.git#v0.1.0
```

The repository is private. Authenticate Git on your machine with an authorized GitHub account; never put passwords or tokens in the URL or project manifest. Unity and command-line Git use your machine's Git authentication, which can be separate from a browser login.

You can also add this entry to the consuming project's existing `Packages/manifest.json` dependencies, preserving its other entries:

```json
"com.ripplegame.base": "https://github.com/namdev26/ripple-game-base.git#v0.1.0"
```

Use Unity 6000.0 or newer. Remove or migrate previous copies of these tools and the separate RippleGame Ads/Analytics packages before installing; duplicate assembly names cannot coexist. See [Migration](Documentation~/Migration.md).

## Modules

| Folder | Assembly / purpose |
| --- | --- |
| Runtime/Economy | `RippleGame.Base.Economy`: pure C# currency operations; no Unity dependency |
| Runtime/Persistence | `RippleGame.Base.Persistence`: PlayerPrefs adapter with a caller-owned key |
| Runtime/UI | `RippleGame.Base.UI`: caller-supplied popup prefabs, one active view |
| Runtime/Ads | Existing `RippleGame.Ads.Runtime` API and optional AdMob provider |
| Runtime/Analytics | Existing `RippleGame.Analytics.Runtime` API with Console fallback |
| Runtime/AnalyticsAdsBridge | Optional bridge between the included Ads and Analytics modules |
| Editor/Tools | `TOOL > Open Hub`, Scene Switch, Android Build, Texture and JSON export tools |
| Editor/Ads, Editor/Analytics | Project configuration and build validation |

Open **TOOL > Open Hub** for settings and actions in one window. Install the official Google Mobile Ads and Firebase SDKs separately before enabling their integrations. SDK-disabled imports compile and the Analytics Console fallback works without those SDKs.

## Project-owned configuration

ADS and Analytics menus create assets under the consuming project's Assets folder. Keep production IDs, Firebase project files, scene policies, keystores, passwords, prefabs and save keys in that game. A package update does not recreate or overwrite these assets.

The base does not impose a reward formula, game flow, level schema or scene name. Supply those from your game. ADS defaults exclude generic loading scene names; edit the new project's configuration to match its own scene policy.

## Update and edit

For a released version such as `v0.1.1`, replace the Git revision suffix with that tag and let Package Manager resolve it. Commit the consuming project's manifest and lockfile. Tags pin versions; upstream pushes do not silently change a tagged install.

Shared source is edited in this repository's normal writable clone. Unity's Git package cache is a consumer copy. For development, use **Install package from disk** and select this clone's package.json, or make an embedded development copy. After editing and testing, increment package.json, update CHANGELOG.md, commit and create a new tag. Return consumers to a Git dependency to receive future releases.

Source-only ZIP fallback: run `Tools~/Export-Source.ps1`; the result is under ignored `Artifacts~`. Verification: `Tools~/Check-Wallet.ps1` runs SDK-free wallet checks with the .NET SDK. Add `com.ripplegame.base` to the consuming project's `testables` and install Unity Test Framework to run the included EditMode/PlayMode tests.

See [Usage](Documentation~/Usage.md), [Migration](Documentation~/Migration.md) and [Changelog](CHANGELOG.md).
