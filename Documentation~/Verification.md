# Verification - 0.4.0, 2026-11-12

Unity Editor: 6000.0.50f1 on Windows.

- Four fresh scratch projects, one per SDK combination (not copies of `ripple-verify`), each importing the package from a local path with `com.unity.test-framework` 1.5.1 and `com.unity.ugui` 2.0.0. Hub Setup itself was not run: the SDK tarballs were copied into `Packages/RippleSdks/` and the manifest entries `file:RippleSdks/<file>` were written by hand to emulate it. In the four-SDK project, `SdkStatusResolver` returned Installed for max, appsflyer, firebase-remote-config, firebase-analytics, firebase-app and the External Dependency Manager. Every combination imported with 0 `error CS`:
  - none: no `RippleGame.Ads.AdMob*`, `Ads.Max`, `Firebase`, `Analytics.Firebase`, `RemoteConfig.Firebase` or `Attribution.AppsFlyer` DLL in `Library/ScriptAssemblies`.
  - admob: adds `RippleGame.Ads.AdMob.dll` and `RippleGame.Ads.AdMob.Editor.dll` only.
  - max: adds `RippleGame.Ads.Max.dll` only.
  - max + appsflyer + firebase-remote-config + firebase-analytics: adds `Ads.Max`, `RippleGame.Firebase`, `Analytics.Firebase`, `RemoteConfig.Firebase` and `Attribution.AppsFlyer`.
- Test suites, run in the none and the four-SDK combinations with the same result in both: EditMode 309 passed, 6 skipped without a graphics device (the pop-out window tests), 0 failed. PlayMode 177 passed, 0 failed. The first PlayMode run failed one test in both the none and the four-SDK projects (a delayed TMP importer window logged "No graphic device is available"). The TextMesh Pro essentials were then copied in from `ripple-verify` and PlayMode re-run: all 177 passed.
- Wrong version, in a fifth scratch project (`max` combination): `max` pointed at a tarball whose package version is 8.5.0. Unity resolved 8.5.0, imported with 0 `error CS`, and built no `RippleGame.Ads.Max.dll`. `SdkStatusResolver` returned `WrongVersion`. A batchmode Android build with `providerId` `max` failed in `AdsPreBuildValidator` (the `ProviderBuildCheck` message, "SDK is not installed at the pinned version"), and `SdkPreBuildValidator` also failed it ("installed at ... 8.5.0 but this RIPPLE Game Base version needs 8.5.1").
- Not run: Android device; the Hub's live download from the `ripple-game-sdks` release (the release does not exist yet, so it is checked at publish time); the Hub SDK page (GUI) was not opened, and WrongVersion was confirmed via `SdkStatusResolver`, the page's data source; `DeviceChecklist-0.4.0.md` with real MAX keys.

# Verification - 0.3.1, 2026-11-07

Unity Editor: 6000.0.71f1 on Windows.

- EditMode suite: 236 passed, 5 skipped without a graphics device, 0 failed. PlayMode suite: 31 passed, 0 failed. `Tools~/Check-Package.ps1` passed.
- The sheet that failed in 0.3.0 (blank key cell, `English` in the next cell) built all its language files when passed through the fixed code.
- Not run: Android device.

# Verification - 0.3.0, 2026-11-07

Unity Editor: 6000.0.71f1 on Windows.

- EditMode suite: 234 passed, 5 skipped without a graphics device, 0 failed. The 8 pop-out tests passed in a run with a graphics device. PlayMode suite: 31 passed, 0 failed. `Tools~/Check-Package.ps1` passed.
- Import from a local path into an empty project with Unity's default packages and into one whose manifest has only this package: no compile errors. Without `com.unity.ugui`, `RippleGame.Base.Localization.UI` is not built.
- TOOL sample without the removed modules: 51 EditMode tests passed. In Play Mode with Google test ads: a real wall collision opened revive, decline, retry, a watched revive continued the level, the next collision went to lose, a win saved coins and level at once, x2 added the reward once and moved to the next level, next on the last level returned home, lose home returned home, and coins and levels survived a restart. The win itself was triggered from a script instead of by eating fruit.
- TOOL Hub: all pages opened with no console errors. All 9 pages popped out into their own windows and stayed open across a script reload. Docking them by hand was not run. The `RIPPLE Game` menu has only Open Hub, and the Hub commands were checked to resolve their methods.
- Not run: Android device, and an install of this version from its Git tag before release.

# Verification - 0.2.1, 2026-11-07

Unity Editor: 6000.0.71f1 on Windows.

- EditMode suite: 270 passed, 0 failed. PlayMode suite: 31 passed, 0 failed. `Tools~/Check-Package.ps1` passed.
- Real sync from a Google Sheet with 17 languages and `GOOGLETRANSLATE` formulas: all files written, with no broken placeholders or line breaks. Sheet formulas showing `#ERROR!` were observed in that Sheet, and sync now stops on them (covered by tests).
- TOOL sample in Play Mode: all 17 languages selected, no raw keys, no game errors in the console. Fonts for Japanese, Korean, Thai, Arabic, Chinese and Hindi are not installed there, and Arabic has no letter shaping.
- Not run: Android device, and a new clone on another machine.

# Verification - 0.2.0 localization, 2026-11-07

Unity Editor: 6000.0.71f1 on Windows.

## Completed

- EditMode suite: 262 passed, 0 failed. PlayMode suite: 31 passed, 0 failed. Both include every earlier test.
- Import into a project whose manifest has only this package and no `com.unity.ugui`: no compile errors, and `RippleGame.Base.Localization.UI` is not built. `Tools~/Check-Package.ps1` passed. The new sample compiled in a project with `com.unity.ugui`.
- Real sync from a studio Google Sheet: 15 language files written, Arabic and Unknown skipped. Compared with the previous files of 4 languages, differences were the dropped `KEY_lang` row, a trimmed `s_review` key and translations updated on the Sheet. A link to a Sheet that does not exist reported 404 and changed no files.
- TOOL sample in Play Mode: the language button changes Home text at once, the choice survives a restart, Vietnamese text shows all diacritics on Home and in the gameplay score, and all 4 popups show their Vietnamese text with no missing glyphs or overflow. Win, Lose, Revive and Reward popups were instantiated directly, not reached by playing a level.
- Coverage scan of the 3 scenes and 4 popups: every text is a `LocalizedText`, written by a script through `Localizer`, a symbol, or a `RewardPopup` placeholder.
- The renamed menu was read from the native menu bar: `Tools | RIPPLE Game | Window | Help`, with `Open Hub` first inside it.

## Not run

- Android device: first launch language detection. No device was used.
- A Sheet that exists but is not shared. The HTML response case is covered by a test only.
- Fonts for Chinese, Japanese, Korean, Thai and Hindi are not tested, because the TOOL sample does not declare those languages.

## Known issues outside this change

- The symbols `⚙ ◀ ▶` show as squares in the TOOL sample because its fonts have no such glyphs.

# Verification - 0.2.0, 2026-11-05

Unity Editor: 6000.0.71f1 on Windows.

## Completed

- EditMode suite: 197 passed, 0 failed. PlayMode suite: 24 passed, 0 failed. Both include every 0.1.0 test.
- Save writes were interrupted at each mutation, with partial files, and stayed readable. Tampering, truncation, wrong keys, restored keys, read errors, write errors and version migration are covered.
- Git install from a local repository at the release candidate commit into an empty project without SDKs: source `git`, 14 RippleGame assemblies compiled, no errors.
- Windows Mono player with managed stripping at High: save smoke test passed, covering encryption, reopen, wallet and level stores on a private type, and tamper recovery. `Flush` of a 10 KB document averaged 3.77 ms and peaked at 8.13 ms on the development PC.
- Android IL2CPP APK built from the TOOL sample with ADS enabled and managed stripping at Low: `libil2cpp.so` present, no Mono runtime, and AesManaged, HMACSHA256, RNGCryptoServiceProvider and the Save types present in the metadata.
- TOOL sample in Play Mode with Google test ads: loading, locked levels, win, double reward, next with interstitial, revive when earned, revive not earned, decline, retry, home, last level, persistence across runs and one-time coin import from PlayerPrefs.
- TOOL Hub Save page opened without errors. Key file creation is covered by tests.
- Update from a local Git repository in an empty project: `v0.2.0` found `v0.2.1` and ignored `v0.3.0-beta`, then the install step moved the manifest and lockfile to the `v0.2.1` commit. The project reopened on 0.2.1 with no compile errors and no further update offered. The page layout was checked with sample data in the Editor. The Update button's click and confirmation dialog were not exercised, because batch mode cancels dialogs.
- Every C# example in the developer guide compiled in a project with the package and no SDKs.
- An independent review found eight issues in save recovery, read errors, late rewards and documentation. All were fixed, with regression tests where the code can run without the ad SDK.

## Not run

- Android or iOS device. No device was connected, so IL2CPP runtime behaviour, a real app kill and `Flush` time on a phone are unverified. The 16 ms target has only been measured on a PC.
- Real ad networks. The wait for a reward that arrives after the close event ran only through the Google placeholder ads, which never deliver a late reward.
- Install from the GitHub URL. The `v0.2.0` tag has not been created or pushed.
- Windows IL2CPP, because the machine has no Visual Studio C++ toolchain.

# Verification - 2026-10-25

Unity Editor: 6000.0.71f1 on Windows. External Google Mobile Ads and Firebase SDKs were not installed in the verification projects; ADS and FIREBASE symbols were disabled.

## Completed

- Independent embedded package import into a fresh project without Unity Test Framework: passed. Test assemblies are excluded using a package-version define until Test Framework is installed.
- Ten SDK-independent currency checks: passed. Cases cover persisted values, invalid amounts, exact/insufficient spends, overflow saturation, corrupt negative storage, failed persistence and notifications.
- EditMode suite: 68 passed, 0 failed, 0 skipped. Includes original Ads/Analytics tests and new caller-key persistence/isolation checks.
- PlayMode suite in an isolated project: 13 passed, 0 failed, 0 skipped. Includes popup replacement, invalid/inactive prefabs, external containers, OnEnable/OnDisable callbacks that open or close views, and the existing Ads runtime tests.
- Regression proof: inactive-prefab and reentrant-close tests were observed failing before the lifecycle correction, then passed in the complete suite.
- Native Hub structural/serialization checks: stylesheet, six overview cards, six inline containers, author credit, staged build values, and no settings application from navigation passed.
- Actual Unity Git dependency install from an isolated local Git tag reported PackageSource.Git and version 0.1.0.
- Update from a local verification tag at 0.1.0 to a 0.1.1 fixture reported PackageSource.Git/version 0.1.1. Consumer ADS/Analytics asset hashes and GUIDs, authored logging settings, and persisted currency balance were unchanged. The fixture is not a public release tag.
- Source boundary audit: 80 runtime/editor C# files contain no original game class/save-key/scene dependencies or non-demo ad IDs. Source GUIDs are unique; exports contain no scenes, prefabs, configuration assets, binary SDKs or signing files.
- Source file write access was checked from the normal Windows developer account. The deliverable clone and its Git directory belong to that account; no global Git safe.directory exception is required.
- Independent code review found popup activation/ownership issues. Five regression cases were reproduced failing and corrected before release, including initialization before OnEnable callbacks.

## Scope limits

Hub checks construct its native visual tree and containers; they do not verify the actual rendered Editor layout or scrolling. The original game's authored scenes/prefabs were not migrated or tested in Play Mode. No Android/iOS build, device SDK ad flow, Firebase cloud ingestion, purchases or production configuration was tested.

Google/Firebase SDK setup, consent policy and per-game configuration remain responsibilities of each consuming project. See the module setup guides before enabling their symbols.
