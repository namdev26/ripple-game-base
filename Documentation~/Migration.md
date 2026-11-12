# Migration and rollback

## New consuming project

Install the tagged Git package. Create new project configurations from RIPPLE Game > Open Hub. Assign this project's own popup prefabs, identifiers, scenes and save keys. Install vendor SDKs only when needed, from RIPPLE Game > Open Hub > SDK. The `ADS` and `FIREBASE` symbols no longer exist; an SDK is active when its pinned package is installed.

## Project already containing old TOOL code

1. Commit or back up the consuming project first, including current package lockfile and project-owned assets.
2. Identify old Scene Switch, Texture, Build and Hub scripts, plus the separate com.ripplegame.ads/com.ripplegame.analytics packages. Move their sources and metas outside the active Assets/Packages trees as one reversible backup before installing the combined package; do not keep duplicate copies imported.
3. Keep all project-owned configuration assets, original game scripts, levels, prefabs, scenes and assets in place. Preserve their GUIDs. Never edit scene/prefab YAML to repair a migration.
4. Install the combined Git package. Its existing shared scripts retain their source GUIDs and Ads/Analytics assembly names. The included bridge detects com.ripplegame.base.
5. For a legacy game popup base, a compatibility adapter can inherit RippleGame.UI.PopupView. Keep game-specific routing/reward rules in game code and supply prefabs to PopupHost. This package does not ship the original game's popup subclasses or prefabs.
6. Compile and test scene/prefab bindings, saves, popup actions and SDK flows in that project before discarding its backup.

Do not install the package alongside imported old implementations without performing the migration above. From 0.2.0 the TOOL sample runs on this package, so its Snake scripts show a complete migration.

## Moving to 0.3.0

0.3.0 removes `Flow`, `FlowIntegration`, `Progression`, `Economy` and `Persistence`, and the `RIPPLE Game` menu keeps only **Open Hub**. A game using those modules stays on tag `v0.2.1` until it has done the steps below.

1. Move the code into the game. Copy it from tag `v0.2.1`, or write less, as `SnakeProgress` and `SnakeLevelFlow` do in the TOOL project.
2. Remove `RippleGame.Base.Flow`, `RippleGame.Base.FlowIntegration`, `RippleGame.Base.Progression`, `RippleGame.Base.Economy` and `RippleGame.Base.Persistence` from the game's asmdefs.
3. Keep player data readable. Which rule applies depends on how the game stored it:
   - **`save.BalanceStore(key)` or `save.LevelStore(key)`:** read the same section key at version 1, with a `[Serializable]` class that has an `int` field named exactly `value`.
   - **`PlayerPrefsBalanceStore(key)`:** this is a plain int in PlayerPrefs, not a save section. Keep reading it with `PlayerPrefs.GetInt(key)`, or copy it once into your save. Reading a new save section instead shows players 0 coins.
4. Test with a save from the current release before publishing.

## Moving to 0.4.0

0.4.0 removes the `ADS` and `FIREBASE` define symbols. Third-party SDKs are now installed from RIPPLE Game > Open Hub > SDK at the version this package pins, and each provider assembly compiles only with that exact version. Back up the project and commit first, then follow these steps in order.

1. **Fix the define symbols.** In Player Settings > Scripting Define Symbols, remove `ADS` and `FIREBASE` for every platform, or apply a build profile in RIPPLE Game > Open Hub > ADS (a profile removes `ADS` by itself).
   - **No Ads games.** A 0.3.x game without the `ADS` symbol used to mean "No Ads". In 0.4.0 no symbol means **Production**, and the old configuration asset carries `providerId` `admob`, so such a game would now build with AdMob. Apply the **No Ads** profile (it sets `ADS_DISABLED`), or set `providerId` to `none`.
   - **Non-Android builds of an AdMob game.** The No Ads profile writes the Android symbols only. For iOS or any other platform that must not show ads, add `ADS_DISABLED` for that platform by hand. The AdMob provider builds on Android only.
2. **Remove the old SDK copies from `Assets`.** Delete `Assets/GoogleMobileAds`, `Assets/Firebase`, `Assets/ExternalDependencyManager`, and if present `Assets/MaxSdk` and `Assets/AppsFlyer`, along with their `.meta` files. The Hub refuses Setup while these folders exist, and lists the ones it finds. Keep your `google-services.json` and `GoogleService-Info.plist`.
3. **Install the SDKs the game uses.** Open RIPPLE Game > Open Hub > SDK and press **Setup** on each: Google Mobile Ads for AdMob, AppLovin MAX, Firebase Analytics, Firebase Remote Config, AppsFlyer. Wait for the compile to finish. Firebase Analytics no longer needs the **Enable Firebase** button, which is gone. See [SDK setup](sdk/Setup.md).
4. **Set `providerId`.** In the project's `AdsConfiguration`, choose `none`, `admob` or `max`. Existing assets keep `admob`. For `max`, fill the Max Test and Max Production profiles. Open the ADS page to confirm that the provider shows **Compiled**.
5. **Tell Ads about the game.** Call `Ads.SetPolicyContext(new AdsPolicyContext(currentLevel, entitlement, hasUserConsent))` at start and whenever the level, a purchase or the consent answer changes. Without it `min_level` is ignored. See [SDK setup](sdk/Setup.md#game-code-for-the-ads-rules).
6. **Check custom code.** A class that implements `IAdsService` must add `ShowMrec(MrecPosition)` and `HideMrec()`. Code that builds an `AdRevenue` can pass the new `platform` argument. A `switch` over `AdPlacement` should handle `Pause`, `Replay` and `RewardedFallback`.
7. **Optional.** Create Remote Config and Attribution settings if the game uses them (the paths are in [SDK setup](sdk/Setup.md)). The old cooldown values in `AdsConfiguration` move into the new show rules by themselves, so no configuration is lost.
8. Build once with Ads Test and run the [device checklist](DeviceChecklist-0.4.0.md) before a release.

To roll back, return the package tag to `v0.3.1`, restore the symbols and the old SDK folders from your backup, and remove the `Packages/RippleSdks` folder and its manifest entries.

## Update or roll back a Git installation

Update the revision suffix to a new immutable release tag and resolve packages. Keep configuration assets and save keys unchanged. Commit the manifest/lockfile after validation.

To roll back, restore the previous manifest/lockfile revision or previous tag, then resolve. If rolling back the initial migration, remove the combined dependency and restore the backed-up old sources/metas to their original paths. Never install both generations at once. Do not delete project-owned configuration or player saves as part of a source rollback.

Git cache files are not shared development source. Make code changes in an editable clone or embedded development copy, then release a new tag. Never put credentials in a Git URL or commit vendor Firebase files, ad ID assets or signing files into this repository.
