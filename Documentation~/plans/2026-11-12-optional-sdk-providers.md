# 0.4.0 Optional SDK Providers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship ripple-game-base 0.4.0, where AdMob, AppLovin MAX, Firebase (Analytics, Remote Config) and AppsFlyer are optional SDK providers that compile only against the exact pinned SDK versions. TOOL Hub installs those versions with one button. Ads gain MREC, shared show rules and rewarded fallback.

**Architecture:** SDK-free core assemblies (`Ads`, `Analytics`, `RemoteConfig`, `Attribution`) always compile. Each SDK provider lives in its own assembly gated by asmdef `versionDefines` on its exact UPM package version, and self-registers into the core at `AfterAssembliesLoaded`. Small SDK-free bridge assemblies connect modules. An editor `SdkCatalog.json` pins versions and drives the Hub SDK page, the build validator and a repo audit.

**Tech Stack:** Unity 6000.0, C#, NUnit via Unity Test Framework, UPM tarballs, Newtonsoft JSON (`com.unity.nuget.newtonsoft-json` 3.2.2), PowerShell for `Tools~` scripts.

**Spec:** `Documentation~/specs/2026-11-12-optional-sdk-providers-design.md`

## Global Constraints

- Unity floor stays `"unity": "6000.0"`. Package version becomes `0.4.0`.
- Add the dependency `"com.unity.nuget.newtonsoft-json": "3.2.2"` to `package.json`.
- Remove the scripting defines `ADS` and `FIREBASE` from all code. Keep `ADS_TEST` (selects the Test profile and the Remote Config test fetch interval). Add `ADS_DISABLED`, which the No Ads build profile sets to force `NoAdsService`.
- No SDK keys, ad unit IDs or dev keys in code. Only Google's published AdMob demo IDs (`ca-app-pub-3940256099942544`) are allowed.
- Pinned SDKs (copied from the spec): EDM `1.2.186`; Firebase app, analytics, remote-config, crashlytics and messaging `13.1.0`; AppLovin MAX `8.5.1`; AppsFlyer `6.17.9`; Google Mobile Ads version chosen in Task 1.
- Every provider asmdef's `versionDefines` expression is exactly `[<catalog version>]`.
- Do not add Odin, DOTween Pro, UniRx or UniTask.
- Every new `.cs` and `.asmdef` file needs a `.meta` with a unique GUID (`Tools~/Check-Package.ps1` enforces this). Generate metas by letting Unity import the file in the verification project, then copy them into the repo.
- Existing enum integer values must not change. New enum members are appended at the end.
- Work on branch `feat/0.4.0-sdk-providers`. Commit per task. Never push, create repos or tag without asking the user first.

## Review Focus

1. **Double show:** the game calls `ShowRewarded` twice quickly, or calls it while an interstitial is up. It gets `AlreadyShowing`, and the reward is granted at most once. Tested in Task 6.
2. **Wrong-type Remote Config JSON**, such as `"cooldown_seconds":"abc"` or `"placements":5`. Only that field is ignored; the rest of the override still applies. Tested in Task 11.
3. **Messy `manifest.json`:** unrelated entries, `scopedRegistries`, `testables` and a missing `Packages/RippleSdks` folder are preserved or created. A download that fails or is cut off leaves no file and leaves the manifest unchanged. Tested in Task 3.
4. **Offline first launch with no cache:** Remote Config stays on defaults, reports ready after the timeout, and ads still initialize. Tested in Task 10 and Task 11.
5. **Entitlement bought mid-session**, for example `RemoveAds` while a banner and an MREC are visible. Both are hidden at once, and later banner calls do nothing without throwing. Tested in Task 6.

---

## Verification project and test command (used by every task)

A Unity consumer project outside the repo, created in Task 0: `E:\_NamGame\ripple-verify`. Run tests in it with:

```bash
"/e/Unity Editor/6000.0.50f1/Editor/Unity.exe" -batchmode -nographics -projectPath "E:/_NamGame/ripple-verify" -runTests -testPlatform EditMode -testResults "E:/_NamGame/ripple-verify/editmode.xml" -logFile "E:/_NamGame/ripple-verify/editmode.log"
```

- PlayMode: the same command with `-testPlatform PlayMode` and output files `playmode.xml` / `playmode.log`.
- One class: add `-testFilter <Fully.Qualified.ClassName>`.
- Pass means exit code `0` and `result="Passed"` on the root `test-run` element of the XML.
- `-nographics` skips 5 graphics tests, the same as earlier releases.

---

### Task 0: Workspace preparation

**Files:**
- Move: `Base Somia/` → `E:\_NamGame\_somia-source\` (outside the repo). **Ask the user before moving.**
- Create: `E:\_NamGame\ripple-verify\` (a Unity project, not in the repo)

- [ ] **Step 1:** Ask the user for approval, then move `Base Somia` out of the repo. Run `git status --short`. Expected: no `Base Somia` entry.
- [ ] **Step 2:** `git switch -c feat/0.4.0-sdk-providers`
- [ ] **Step 3:** Create an empty Unity 6000.0.50f1 project at `E:\_NamGame\ripple-verify` with `-batchmode -createProject ... -quit`. Set the `Packages/manifest.json` dependencies to `"com.ripplegame.base": "file:../../ripple-game-base"`, `"com.unity.test-framework": "1.5.1"`, `"com.unity.ugui": "2.0.0"`, `"com.unity.nuget.newtonsoft-json": "3.2.2"`, with `"testables": ["com.ripplegame.base"]`.
- [ ] **Step 4:** Run EditMode and PlayMode on the untouched `main` code. Expected: the 0.3.1 baseline of 236 passed and 5 skipped in EditMode, and 31 passed in PlayMode. Record the numbers. If they differ, stop and report.

No commit (nothing in the repo changed).

---

### Task 1: SDK sourcing and the catalog file

Get each pinned SDK as a UPM tarball from its official source, and record facts that later tasks rely on.

**Files:**
- Create: `Editor/Sdk/SdkCatalog.json`
- Create: `E:\_NamGame\ripple-game-sdks\` (a local folder, not pushed yet) containing the `.tgz` files, `LICENSES/<id>.txt` and `sdks.json`

**Interfaces:**
- Produces: `SdkCatalog.json`, an array `entries` of `{ "id", "displayName", "packageName", "version", "fileName", "url", "sha256", "dependsOn": [], "legacyPaths": [], "asmdefName" }`.
  - Ids: `edm`, `firebase-app`, `firebase-analytics`, `firebase-remote-config`, `firebase-crashlytics`, `firebase-messaging`, `admob`, `max`, `appsflyer`.
  - `url` is `https://github.com/namdev26/ripple-game-sdks/releases/download/sdks-0.4.0/<fileName>`, or the vendor's official URL when its license forbids redistribution.
  - `asmdefName` is the SDK's own assembly that provider asmdefs must reference. Use an empty string when the SDK ships only precompiled DLLs.

- [ ] **Step 1:** Firebase and EDM: download the official 13.1.0 and 1.2.186 `.tgz` from Google's Unity archive. Compute SHA-256 and compare it with the old project's copies in `E:\_NamGame\_somia-source\GooglePackages`. A mismatch is reported to the user, not silently accepted.
- [ ] **Step 2:** AppLovin MAX 8.5.1: get the official UPM package (AppLovin's UPM distribution or the Integration Manager's UPM migration) and pack it to `.tgz`. Record `packageName`, which is expected to be `com.applovin.mediation.ads`, and the asmdef name of its scripts.
- [ ] **Step 3:** AppsFlyer 6.17.9: get the official `#upm` distribution at the `6.17.9` tag and pack it to `.tgz`. Record the package name and asmdef name. Make sure its `package.json` does not depend on EDM.
- [ ] **Step 4:** Google Mobile Ads: choose the newest stable release whose EDM requirement is satisfied by 1.2.186 and which supports Unity 6000.0. Get its official `.tgz`. Write the chosen version into the spec's table in section 2.1.
- [ ] **Step 5:** Read each SDK's license. If a license forbids redistribution, set that entry's `url` to the vendor's official URL. Note the decision in `sdks.json`.
- [ ] **Step 6:** Fill in the dependencies:
  - every Firebase entry depends on `firebase-app` and `edm`;
  - `admob` and `max` depend on `edm`;
  - `appsflyer` depends on `edm` only if Step 3 found such a dependency.
- [ ] **Step 7:** Fill in `legacyPaths`:
  - `max`: `Assets/MaxSdk`
  - `firebase-*`: `Assets/Firebase`
  - `edm`: `Assets/ExternalDependencyManager`
  - `appsflyer`: `Assets/AppsFlyer`
  - `admob`: `Assets/GoogleMobileAds`
- [ ] **Step 8:** In a scratch copy of the verification project, add every `.tgz` with `file:` references. Open Unity in batchmode and confirm there are no compile errors. Report the result.
- [ ] **Step 9:** Commit `Editor/Sdk/SdkCatalog.json` and the spec edit with the message `chore: pin the 0.4.0 SDK set`.

---

### Task 2: SDK catalog model and version-define audit

**Files:**
- Create: `Editor/Sdk/RippleGame.Sdk.Editor.asmdef` (Editor only; references `Unity.Newtonsoft.Json`)
- Create: `Editor/Sdk/SdkCatalog.cs`, `Editor/Sdk/SdkEntry.cs`, `Editor/Sdk/SdkVersionDefineAudit.cs`, `Editor/Sdk/AssemblyInfo.cs`
- Create: `Tests/Sdk/Editor/RippleGame.Sdk.Editor.Tests.asmdef` (same `RIPPLEGAME_TEST_FRAMEWORK` pattern as `Tests/Ads/Editor`)
- Test: `Tests/Sdk/Editor/SdkCatalogTests.cs`

**Interfaces:**
- Produces:
  - `internal sealed class SdkEntry` with string properties `Id`, `DisplayName`, `PackageName`, `Version`, `FileName`, `Url`, `Sha256`, `AsmdefName`, and `IReadOnlyList<string> DependsOn` and `IReadOnlyList<string> LegacyPaths`.
  - `internal sealed class SdkCatalog`:
    - `static SdkCatalog LoadDefault()`, which reads `Editor/Sdk/SdkCatalog.json` through `PackageInfo.FindForAssembly`;
    - `static SdkCatalog Parse(string json)`;
    - `IReadOnlyList<SdkEntry> Entries`;
    - `SdkEntry Find(string id)`;
    - `IReadOnlyList<SdkEntry> InstallOrder(string id)`: dependencies first, no duplicates, throws `InvalidOperationException` on a cycle;
    - `IReadOnlyList<SdkEntry> DependentsOf(string id)`.
  - `internal static class SdkVersionDefineAudit`: `static IReadOnlyList<string> FindMismatches(SdkCatalog catalog, IEnumerable<KeyValuePair<string, string>> asmdefPathToJson)`. It returns one message per `versionDefines` item whose `name` is a catalog `PackageName` and whose `expression` is not `"[" + Version + "]"`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void InstallOrder_PutsDependenciesFirst()
{   var c = SdkCatalog.Parse(Fixture);   // edm <- firebase-app <- firebase-remote-config
    Assert.That(c.InstallOrder("firebase-remote-config").Select(e => e.Id),
        Is.EqualTo(new[] { "edm", "firebase-app", "firebase-remote-config" })); }
[Test] public void InstallOrder_Cycle_Throws() => Assert.Throws<InvalidOperationException>(() => SdkCatalog.Parse(CycleFixture).InstallOrder("a"));
[Test] public void DependentsOf_Edm_ListsDirectAndIndirect() { /* edm -> firebase-app, firebase-remote-config */ }
[Test] public void Audit_FlagsExpressionThatIsNotExactVersion()
{   var asmdef = "{\"versionDefines\":[{\"name\":\"com.applovin.mediation.ads\",\"expression\":\"8.5.0\",\"define\":\"X\"}]}";
    Assert.That(SdkVersionDefineAudit.FindMismatches(MaxCatalog, Pair("a.asmdef", asmdef)), Has.Count.EqualTo(1)); }
[Test] public void Audit_IgnoresNonCatalogPackages() { /* com.unity.test-framework 1.1.0 -> no mismatch */ }
[Test] public void PackageAsmdefs_MatchShippedCatalog()
{   // Scan every *.asmdef under the package root (PackageInfo.FindForAssembly) and audit against LoadDefault().
    Assert.That(SdkVersionDefineAudit.FindMismatches(SdkCatalog.LoadDefault(), AllPackageAsmdefs()), Is.Empty); }
[Test] public void ShippedCatalog_EveryEntryHasSha256AndFileName() { /* 64 hex chars, non-empty fileName */ }
```

- [ ] **Step 2:** Run with `-testFilter RippleGame.Sdk.Editor.Tests.SdkCatalogTests`. Expected: compile failure, because the types do not exist yet.
- [ ] **Step 3:** Implement the types listed under Interfaces. Parse with Newtonsoft `JObject`.
- [ ] **Step 4:** Run again. Expected: PASS.
- [ ] **Step 5:** Commit with `feat: add sdk catalog and version define audit`.

---

### Task 3: Manifest editing, status detection and installer

**Files:**
- Create: `Editor/Sdk/SdkManifestEditor.cs`, `Editor/Sdk/SdkStatus.cs`, `Editor/Sdk/SdkInstaller.cs`, `Editor/Sdk/ISdkDownloader.cs`, `Editor/Sdk/UnityWebRequestSdkDownloader.cs`, `Editor/Sdk/SdkHash.cs`
- Test: `Tests/Sdk/Editor/SdkManifestEditorTests.cs`, `Tests/Sdk/Editor/SdkInstallerTests.cs`

**Interfaces:**
- Consumes: `SdkCatalog` and `SdkEntry` from Task 2.
- Produces:
  - `internal static class SdkManifestEditor`:
    - `string SetDependency(string manifestJson, string packageName, string reference)`
    - `string RemoveDependency(string manifestJson, string packageName)`
    - `string GetDependency(string manifestJson, string packageName)` (returns null when absent)
    - Output is indented with 2 spaces, and every other top-level key is kept in its original order.
  - `internal enum SdkState { NotInstalled, Installed, WrongVersion, Legacy }`
  - `internal sealed class SdkStatusInfo { SdkState State; string InstalledReference; IReadOnlyList<string> FoundLegacyPaths; }`
  - `internal static class SdkStatusResolver`: `SdkStatusInfo Resolve(SdkEntry entry, string manifestJson, string projectRoot)`. The state is `Installed` only when the dependency equals `"file:RippleSdks/" + FileName`. `Legacy` takes priority whenever any `LegacyPaths` folder exists under `projectRoot`.
  - `internal interface ISdkDownloader { Task DownloadAsync(string url, string destinationPath); }`
  - `internal static class SdkHash`: `string Sha256Hex(string filePath)` (lowercase).
  - `internal sealed class SdkInstaller`:
    - constructor `(SdkCatalog catalog, string projectRoot, ISdkDownloader downloader)`
    - `Task<SdkOperationResult> InstallAsync(string id)`: walks `InstallOrder`, skips entries already `Installed`, downloads to `Packages/RippleSdks/<FileName>.part`, verifies SHA-256, renames to the final name, and writes the manifest once at the end. A `WrongVersion` entry is replaced: its dependency is overwritten, and an old `file:RippleSdks/...` tarball it pointed to is deleted;
    - `Task<SdkOperationResult> RemoveAsync(string id)`: fails with a message listing installed dependents;
    - `SdkOperationResult { bool Success; string Message; }`
    - It does not call `Client.Resolve()`. The Hub page does that after a successful operation.
  - `internal sealed class SdkPreBuildValidator : IPreprocessBuildWithReport` (callbackOrder 0) uses `internal static string Check(SdkCatalog catalog, string manifestJson, string projectRoot)`. It returns `"SDK '<DisplayName>' is installed at <InstalledReference> but this RIPPLE Game Base version needs <Version>. Open RIPPLE Game > Open Hub > SDK and press Fix."` for the first `WrongVersion` entry, otherwise null. The build fails when the result is not null.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void SetDependency_KeepsOtherKeysAndOrder()
{   var m = "{\"dependencies\":{\"a\":\"1.0.0\"},\"scopedRegistries\":[],\"testables\":[\"x\"]}";
    var r = SdkManifestEditor.SetDependency(m, "com.applovin.mediation.ads", "file:RippleSdks/max.tgz");
    Assert.That(JObject.Parse(r).Properties().Select(p => p.Name), Is.EqualTo(new[] { "dependencies", "scopedRegistries", "testables" }));
    Assert.That(SdkManifestEditor.GetDependency(r, "a"), Is.EqualTo("1.0.0")); }
[Test] public void Resolve_SemverReference_IsWrongVersion() { /* "8.5.0" -> WrongVersion, InstalledReference "8.5.0" */ }
[Test] public void Resolve_LegacyFolderPresent_IsLegacy() { /* create temp Assets/MaxSdk */ }
[Test] public async Task Install_ShaMismatch_DeletesFileAndLeavesManifest() { /* fake downloader writes wrong bytes */ }
[Test] public async Task Install_DownloadThrows_NoPartFileAndManifestUnchanged() { /* Review Focus 3 */ }
[Test] public async Task Install_CreatesRippleSdksFolderAndDependenciesFirst() { /* fake downloader records order: edm, firebase-app, firebase-remote-config */ }
[Test] public async Task Remove_WithInstalledDependent_Fails() { /* remove edm while max installed -> Success false, Message contains "max" */ }
[Test] public async Task Install_WrongVersion_ReplacesReferenceAndDeletesOldTarball() { }
[Test] public void PreBuildCheck_WrongVersion_ReturnsFixMessage() { /* max "8.5.0" -> message contains "8.5.1" and "Fix" */ }
[Test] public void PreBuildCheck_AllInstalledOrAbsent_ReturnsNull() { }
```

  Tests use a temporary project root under `Path.GetTempPath()` and a fake `ISdkDownloader` that writes fixture bytes. The SHA-256 values in the fixtures are computed from those bytes.
- [ ] **Step 2:** Run the two classes. Expected: compile failure.
- [ ] **Step 3:** Implement the members listed under Interfaces. `UnityWebRequestSdkDownloader` uses `UnityWebRequest.Get` with a `DownloadHandlerFile` pointing at the destination path, and throws on a non-success result.
- [ ] **Step 4:** Run again. Expected: PASS.
- [ ] **Step 5:** Commit with `feat: install pinned sdk tarballs into the manifest`.

---

### Task 4: Hub SDK page

**Files:**
- Create: `Editor/Tools/ToolHub/Pages/SdkHubPage.cs`
- Modify: `Editor/Tools/RippleGame.Tools.Editor.asmdef` (add a reference to `RippleGame.Sdk.Editor`), `Editor/Tools/ToolHub/ToolHubCatalog.cs` (add case `"sdk"` → `new SdkHubPage()`, and add the section entry), `Editor/Tools/ToolHub/ToolHubWindow.cs` (add a serialized `sdk` field and include it in `Pages`)
- Modify tests: `Tests/Base/Editor/ToolHubLayoutTests.cs`, `ToolHubMenuTests.cs`, `ToolHubPopOutTests.cs` (page count goes from 9 to 10; the new page has an id and pops out)

**Interfaces:**
- Consumes: `SdkCatalog.LoadDefault()`, `SdkStatusResolver.Resolve`, `SdkInstaller` from Tasks 2 and 3.
- Produces: the `[Serializable] internal sealed class SdkHubPage : ToolHubPage` page with id `"sdk"`. Each catalog entry is one row showing `DisplayName`, the pinned version and the state.
  - Buttons: **Setup** (`NotInstalled`), **Fix** (`WrongVersion`), **Remove** (`Installed`).
  - `Legacy` rows show the found paths and the text "Remove these folders by hand, then press Setup." with no button.
  - After a successful operation the page calls `UnityEditor.PackageManager.Client.Resolve()`. Buttons are disabled while busy, compiling or in play mode.

- [ ] **Step 1:** Update the three ToolHub test files so they expect the `sdk` page. Run them. Expected: FAIL.
- [ ] **Step 2:** Implement `SdkHubPage` following the `UpdateHubPage` style (IMGUI, `busy` flag, `HelpBox` for errors), and register it.
- [ ] **Step 3:** Run the ToolHub tests. Expected: PASS.
- [ ] **Step 4:** Manual check in `ripple-verify`: open the Hub and the SDK page. All rows show NotInstalled. Press Setup on `edm`. The manifest gains `file:RippleSdks/...` and Unity resolves without errors. Remove it again.
- [ ] **Step 5:** Commit with `feat: add sdk page to tool hub`.

---

### Task 5: Ads core API and configuration

**Files:**
- Modify: `Runtime/Ads/Core/AdsContracts.cs`, `Runtime/Ads/Core/Ads.cs`, `Runtime/Ads/Core/NoAdsService.cs`, `Runtime/Ads/Configuration/AdsConfiguration.cs`, `Runtime/Ads/AssemblyInfo.cs`
- Create: `Runtime/Ads/Core/AdsPolicyContext.cs`, `Runtime/Ads/Configuration/AdsPolicySettings.cs`, `Runtime/Ads/Configuration/MaxAdUnitConfiguration.cs`, `Runtime/Ads/Core/AdUnitIdOverrides.cs`
- Test: `Tests/Ads/Runtime/AdsEventTests.cs` (extend), `Tests/Ads/Runtime/AdsConfigurationMigrationTests.cs`, `Tests/Ads/Runtime/NoAdsServiceTests.cs` (extend)

**Interfaces:**
- Produces, all in namespace `RippleGame.Ads`:
  - Enums:
    - `AdFormat` gains `Mrec` (value 4).
    - `AdPlacement` gains `Pause` (7), `Replay` (8) and `RewardedFallback` (9).
    - New `public enum MrecPosition { TopCenter, Center, BottomCenter }`.
  - `AdRevenue`:
    - new constructor `(long valueMicros, string currencyCode, int precision, string adUnitId, string adSourceName, string platform)`;
    - the old 5-argument constructor stays and sets `platform = PlatformAdMob`;
    - `public const string PlatformAdMob = "admob"`, `PlatformMax = "applovin_max"`;
    - property `string Platform`.
  - `IAdsService` gains `void ShowMrec(MrecPosition position); void HideMrec();`
  - `public enum AdsEntitlement { None, RemoveAds, RemoveAdsAndFreeRewards }`
  - `public readonly struct AdsPolicyContext(int currentLevel, AdsEntitlement entitlement = AdsEntitlement.None, bool? hasUserConsent = null)` with matching properties.
  - `Ads`:
    - `ShowMrec(MrecPosition position = MrecPosition.BottomCenter)`, `HideMrec()`;
    - `SetPolicyContext(AdsPolicyContext context)`;
    - `public static AdsPolicyContext? PolicyContext { get; }`;
    - `internal static event Action PolicyContextChanged`;
    - `internal static Func<Task> InitializationGate` (null means no wait).
  - `[Serializable] public sealed class AdsPolicySettings`, with Inspector defaults equal to the spec's values:
    - `InterstitialPolicySettings Interstitial`: `CooldownSeconds` 45, `MaxPerSession` 4, `MinLevel` 0, `StartSession` 1, `List<AdPlacement> DisabledPlacements` (empty);
    - `AppOpenPolicySettings AppOpen`: `Enabled` true, `CooldownSeconds` 20, `MinBackgroundSeconds` 5;
    - `bool RewardedFallbackEnabled` false;
    - `OrganicPolicySettings Organic`: `Banner`, `Mrec`, `Interstitial`, `AppOpen`, all true;
    - `float BannerRefreshSeconds` 0, `float MrecRefreshSeconds` 0, `bool PauseTimeAndAudio` true;
    - `AdsPolicySettings Clone()`.
  - `[Serializable] public sealed class MaxAdUnitConfiguration`:
    - `string SdkKey`;
    - `MaxPlatformUnitIds Android, Ios`, where `MaxPlatformUnitIds` holds `Banner`, `Interstitial`, `Rewarded`, `AppOpen`, `Mrec`;
    - `bool EnableBanner, EnableInterstitial, EnableRewarded, EnableAppOpen, EnableMrec`;
    - `BannerPosition BannerPosition`.
  - `AdsConfiguration`:
    - new fields `policy`, `maxTest`, `maxProduction`;
    - `AdsPolicySettings Policy`, `MaxAdUnitConfiguration GetActiveMaxProfile()`;
    - `providerId` default becomes `"none"`;
    - implements `ISerializationCallbackReceiver`. While a hidden `[SerializeField] bool policyMigrated` is false, `OnAfterDeserialize` copies the Production AdMob profile's `interstitialCooldownSeconds`, `interstitialMaxImpressionsPerSession`, `appOpenCooldownSeconds` and `appOpenMinimumBackgroundSeconds` into `policy`, then sets `policyMigrated = true`. The old fields stay serialized but are `[HideInInspector]`.
  - `public static class AdUnitIdOverrides`:
    - `string Resolve(string providerId, RuntimePlatform platform, AdFormat format, string fallback)` returns the override when it is non-empty, otherwise the fallback;
    - `internal static void Set(string providerId, string platformKey, AdFormat format, string id)`, where `platformKey` is `"android"` or `"ios"`;
    - `internal static void Clear()`.
  - `AssemblyInfo.cs` adds `InternalsVisibleTo` for `RippleGame.Ads.AdMob`, `RippleGame.Ads.Max`, `RippleGame.Ads.RemoteConfigBridge`, `RippleGame.Ads.AttributionBridge` and their test assemblies.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void ExistingPlacementValues_AreUnchanged() { Assert.That((int)AdPlacement.ExtraLife, Is.EqualTo(6)); Assert.That((int)AdPlacement.Pause, Is.EqualTo(7)); }
[Test] public void FiveArgRevenue_DefaultsToAdMobPlatform() { Assert.That(new AdRevenue(1, "USD", 1, "u", "s").Platform, Is.EqualTo("admob")); }
[Test] public void LegacyCooldowns_MigrateIntoPolicyOnce()
{   // Deserialize a JSON snapshot of a 0.3.1 AdsConfiguration with interstitialCooldownSeconds 60, appOpenCooldownSeconds 25.
    var c = LoadLegacy(); Assert.That(c.Policy.Interstitial.CooldownSeconds, Is.EqualTo(60f));
    c.Policy.Interstitial.CooldownSeconds = 30; Reserialize(c); Assert.That(c.Policy.Interstitial.CooldownSeconds, Is.EqualTo(30f)); }
[Test] public void NewConfiguration_ProviderIsNone_AndPolicyHasSpecDefaults() { /* 45, 4, 0, 1, fallback false, organic all true */ }
[Test] public void NoAdsService_ShowMrec_RaisesFailedForMrec() { }
[Test] public void UnitIdOverride_EmptyFallsBack() { }
```

- [ ] **Step 2:** Run the Ads runtime tests. Expected: compile failure.
- [ ] **Step 3:** Implement. Update every existing `IAdsService` implementer (`NoAdsService`, `AdMobAdsService`) so the code compiles. In `AdMobAdsService`, `ShowMrec` logs `"[RippleGame Ads] MREC is not supported by the AdMob provider in 0.4.0."` once and raises `ShowFailed`.
- [ ] **Step 4:** Run all EditMode and PlayMode tests. Expected: PASS, with the baseline count plus the new tests.
- [ ] **Step 5:** Commit with `feat!: add mrec, policy settings and policy context to ads api`.

---

### Task 6: Policy layer (`PolicyAdsService`)

**Files:**
- Create: `Runtime/Ads/Core/PolicyAdsService.cs`, `Runtime/Ads/Core/AdsSession.cs`, `Runtime/Ads/Core/AdsPolicyOverride.cs`, `Runtime/Ads/Core/FullScreenPause.cs`
- Modify: `Runtime/Ads/Core/AdsProviderFactory.cs`, `Runtime/Ads/Core/Ads.cs`, `Runtime/Ads/Core/AdsBootstrapper.cs`
- Test: `Tests/Ads/Runtime/PolicyAdsServiceTests.cs`, `Tests/Ads/Runtime/FakeAdsService.cs`

**Interfaces:**
- Consumes: Task 5 types.
- Produces:
  - `public sealed class AdsPolicyOverride`: nullable fields mirroring `AdsPolicySettings` (`float? InterstitialCooldownSeconds`, `int? InterstitialMaxPerSession`, `int? InterstitialMinLevel`, `int? InterstitialStartSession`, `Dictionary<AdPlacement,bool> PlacementEnabled`, `bool? AppOpenEnabled`, `float? AppOpenCooldownSeconds`, `float? AppOpenMinBackgroundSeconds`, `bool? RewardedFallbackEnabled`, `bool? OrganicBanner`, `bool? OrganicMrec`, `bool? OrganicInterstitial`, `bool? OrganicAppOpen`, `float? BannerRefreshSeconds`, `float? MrecRefreshSeconds`) and `AdsPolicySettings ApplyTo(AdsPolicySettings source)`, which returns a new instance.
  - `internal static class AdsSession`: `int Number { get; }` and `static void Begin(Func<string,int> read, Action<string,int> write)`. PlayerPrefs key `"ripplegame.ads.session"`. It increments once per process and is reset at `SubsystemRegistration`.
  - `internal sealed class PolicyAdsService : IAdsService, IAdsLifecycleAware`:
    - constructor `(IAdsService inner, AdsPolicySettings settings, Func<float> realtime, Func<int> sessionNumber)`;
    - `internal void SetOverride(AdsPolicyOverride value)` is ignored after the snapshot is taken;
    - `internal void SetAdsEnabled(bool enabled)` takes effect immediately;
    - `internal void SetOrganic(bool isOrganic)`;
    - `AppOpenCooldownSeconds` and `AppOpenMinimumBackgroundSeconds` show-on-resume logic moves here from `AdMobAdsService`.
  - `Ads` gains `internal static void ApplyPolicyOverride(AdsPolicyOverride o)`, `internal static void SetAdsEnabled(bool enabled)` and `internal static void SetOrganic(bool isOrganic)`. These forward to the current `PolicyAdsService`, or buffer until one exists.
  - `AdsProviderFactory.Create(AdsConfiguration)`:
    - `ADS_DISABLED` defined, or `providerId == "none"` → `NoAdsService`;
    - registered provider → `new PolicyAdsService(registered, configuration.Policy, ...)`;
    - unknown provider → `Debug.LogError("[RippleGame Ads] No ads provider is registered for '<id>'. Install its SDK from RIPPLE Game > Open Hub > SDK.")` and `NoAdsService`.
  - `AdsBootstrapper` drops `#if ADS`, awaits `Ads.InitializationGate` when it is set, then calls `InitializeAsync`.

Rules the tests pin, all from spec 4.2 and 4.3:

- [ ] **Step 1: Write the failing tests** with `FakeAdsService` (settable `IsReady` per format, records calls, lets the test trigger close/fail/reward callbacks) and a controllable clock:

```csharp
[Test] public void Interstitial_WithinCooldown_ReturnsCooldown_AndDoesNotCallInner()
[Test] public void Interstitial_SessionCapReached_ReturnsCooldown()
[Test] public void Interstitial_BelowMinLevel_ReturnsDisabled()               // context level 2, MinLevel 3
[Test] public void Interstitial_NoContext_IgnoresMinLevel_WarnsOnce()          // LogAssert.Expect once across two calls
[Test] public void Interstitial_BeforeStartSession_ReturnsDisabled()
[Test] public void Interstitial_DisabledPlacement_ReturnsDisabled()
[Test] public void Interstitial_OrganicBlocked_ReturnsDisabled()
[Test] public void RemoveAds_BlocksBannerMrecInterstitialAppOpen_NotRewarded()
[Test] public void FreeRewards_GrantsRewardWithoutCallingInner_ReturnsSuccess()
[Test] public void AdsDisabled_RewardedGrantsImmediately_InterstitialDisabled()
[Test] public void Fallback_Off_RewardedNotReady_ReturnsNotReady()
[Test] public void Fallback_On_InterClosed_GrantsOnce_Success_EventMessageFallback_ResetsCooldown()
[Test] public void Fallback_On_InterFailed_NoReward_Failed()
[Test] public void Fallback_IgnoresCooldownAndMinLevel_ButRespectsOrganicAndPlacement()
[Test] public void Fallback_RewardIsNeverCarriedToNextInterstitial()           // fail, then a normal inter closes -> no reward callback
[Test] public void DoubleShowRewarded_SecondGetsAlreadyShowing_RewardOnce()    // Review Focus 1
[Test] public void EntitlementChangedToRemoveAds_HidesBannerAndMrecImmediately() // Review Focus 5
[Test] public void OverrideAfterSnapshot_IsIgnoredThisSession()
[Test] public void PauseTimeAndAudio_RestoresPreviousValuesOnClose()           // timeScale 0.5 before -> 0.5 after
```

- [ ] **Step 2:** Run `-testFilter RippleGame.Ads.Tests.PolicyAdsServiceTests`. Expected: compile failure.
- [ ] **Step 3:** Implement. The snapshot is taken at the first `ShowInterstitial`, `ShowRewarded` or `ShowAppOpen` call, or when `InitializeAsync` completes, whichever comes first. `FullScreenPause` saves `Time.timeScale` and `AudioListener.pause` on show and restores them on close or fail.
- [ ] **Step 4:** Run all Ads tests. Expected: PASS.
- [ ] **Step 5:** Commit with `feat: enforce shared ad rules in a policy layer`.

---

### Task 7: AdMob as a version-gated provider assembly; editor build profiles

**Files:**
- Move: `Runtime/Ads/AdMob/*` → `Runtime/Ads.AdMob/` and add `RippleGame.Ads.AdMob.asmdef`. It references `RippleGame.Ads.Runtime` plus the GMA asmdef from Task 1 if one exists. `versionDefines`: `{ name: "<GMA packageName>", expression: "[<GMA version>]", define: "RIPPLEGAME_SDK_ADMOB" }`; `defineConstraints`: `["RIPPLEGAME_SDK_ADMOB"]`.
- Create: `Runtime/Ads.AdMob/AdMobProviderFactory.cs`
- Modify: `Runtime/Ads.AdMob/AdMobAdsService.cs` (remove `#if ADS`, the cooldown, session cap and app-open resume logic, and `IAdsLifecycleAware`; keep load/show/retry; set `platform = PlatformAdMob` on revenue), `Runtime/Ads.AdMob/AdMobRevenue.cs`
- Move: `Editor/Ads/AdMobSettingsSynchronizer.cs` → `Editor/Ads.AdMob/` with `RippleGame.Ads.AdMob.Editor.asmdef` (Editor only, same versionDefines). Add `AdMobPreBuildSync : IPreprocessBuildWithReport` (callbackOrder 1), which syncs the Android app id when `providerId == "admob"`.
- Modify: `Editor/Ads/AdsBuildProfileUtility.cs`, `Editor/Ads/AdsPreBuildValidator.cs`, `Editor/Ads/AdsConfigurationEditor.cs`, `Editor/Tools/ToolHub/Pages/AdsHubPage.cs`, `Editor/Ads/RippleGame.Ads.Editor.asmdef` (reference `RippleGame.Sdk.Editor`)
- Test: `Tests/Ads/Editor/AdsPreBuildValidatorTests.cs` (rewrite), `Tests/Ads/Editor/AdsBuildProfileTests.cs`

**Interfaces:**
- Produces:
  - `internal sealed class AdMobProviderFactory : IAdsProviderFactory` with `ProviderId = "admob"`. It registers at `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]`.
  - `AdsProviderRegistry` gains `internal static void ClearForTests()` and `public static bool IsRegistered(string providerId)`.
  - `AdsBuildProfileUtility.ApplyProfile(AndroidAdsBuildProfile)` is rewritten:
    - always removes the legacy `ADS` symbol;
    - `NoAds` → adds `ADS_DISABLED` and removes `ADS_TEST`;
    - `AdsTest` → adds `ADS_TEST` and removes `ADS_DISABLED`;
    - `AdsProduction` → removes both.
  - `internal static class ProviderBuildCheck`: `string Validate(string providerId, Func<string,bool> providerCompiled, Func<string,SdkStatusInfo> sdkStatus, BuildTarget target)` returns null when OK, or the exact error message.
    - Provider compiled-ness is found by type name: `"RippleGame.Ads.AdMob.AdMobProviderFactory, RippleGame.Ads.AdMob"` and `"RippleGame.Ads.Max.MaxProviderFactory, RippleGame.Ads.Max"`.
    - Error when the provider is not compiled: `"Ads provider '<id>' is selected but its SDK is not installed at the pinned version. Open RIPPLE Game > Open Hub > SDK and press Setup or Fix."`
    - AdMob still allows only Android. MAX allows Android and iOS.
  - `AdsPreBuildValidator` calls `ProviderBuildCheck` and then the existing per-profile ID validation for the chosen provider. Existing ID rules stay; MAX gets the same "Production must not be empty" rule for enabled formats.
  - `AdsHubPage` shows the provider, the build profile (No Ads / Ads Test / Ads Production from `ADS_DISABLED`/`ADS_TEST`) and whether the provider's SDK is compiled.

- [ ] **Step 1: Write the failing tests:**
  - `ApplyProfile_RemovesLegacyAdsSymbol`;
  - `ApplyProfile_NoAds_SetsAdsDisabled`;
  - `Validate_MaxSelectedNotCompiled_ReturnsSetupMessage`;
  - `Validate_NoneSelected_Passes`;
  - `Validate_AdMobOnIos_Fails`;
  - `Validate_MaxOnIos_Passes`;
  - the existing ID-rule tests, ported to the new entry points.
- [ ] **Step 2:** Run the Ads editor tests. Expected: FAIL.
- [ ] **Step 3:** Implement the moves and changes. Delete `#if ADS` everywhere. `grep -rn "#if ADS\b\|\"ADS\"" Runtime Editor` must find only the legacy-symbol removal in `AdsBuildProfileUtility`.
- [ ] **Step 4:** Run all EditMode and PlayMode tests in `ripple-verify` (no SDK installed). Expected: PASS, and the `RippleGame.Ads.AdMob` assembly is not built (check `Library/ScriptAssemblies` has no `RippleGame.Ads.AdMob.dll`).
- [ ] **Step 5:** Commit with `feat!: gate the admob provider on its pinned package`.

---

### Task 8: AppLovin MAX provider

**Files:**
- Create: `Runtime/Ads.Max/RippleGame.Ads.Max.asmdef`. It references `RippleGame.Ads.Runtime` and the MAX asmdef recorded in Task 1. `versionDefines`: `{ name: "com.applovin.mediation.ads", expression: "[8.5.1]", define: "RIPPLEGAME_SDK_MAX" }`; constraints `["RIPPLEGAME_SDK_MAX"]`.
- Create: `Runtime/Ads.Max/MaxProviderFactory.cs`, `MaxAdsService.cs`, `MaxBannerHandler.cs`, `MaxMrecHandler.cs`, `MaxInterstitialHandler.cs`, `MaxRewardedHandler.cs`, `MaxAppOpenHandler.cs`, `MaxRevenue.cs`
- Create: `Runtime/Ads/Core/MaxRevenueConversion.cs` (SDK-free, so it can be tested)
- Test: `Tests/Ads/Runtime/MaxRevenueConversionTests.cs`

**Interfaces:**
- Consumes: `IAdsService`, `AdUnitIdOverrides.Resolve("max", ...)`, `Ads.PolicyContext` (consent), `AdsConfiguration.GetActiveMaxProfile()`, `AdsPolicySettings.BannerRefreshSeconds`/`MrecRefreshSeconds`.
- Produces:
  - `internal sealed class MaxProviderFactory : IAdsProviderFactory` with `ProviderId = "max"`, registered at `AfterAssembliesLoaded`.
  - `internal static class MaxRevenueConversion`: `static long ToMicros(double usd)` (`Math.Round(usd * 1_000_000)`, negative or NaN → 0) and `static AdRevenue Create(double usd, string adUnitId, string networkName)` with currency `"USD"`, precision 0 and `Platform = AdRevenue.PlatformMax`.
  - `MaxAdsService : IAdsService` behaviour:
    - `InitializeAsync` sets consent when `Ads.PolicyContext?.HasUserConsent` has a value, then sets the SDK key and initializes. It completes `Success` on `OnSdkInitializedEvent`, then loads enabled formats.
    - Load failures retry with the existing `RetryMaxAttempts`, `RetryInitialDelaySeconds` and `RetryMaxDelaySeconds` backoff from the AdMob profile, which is reused for MAX.
    - When `Application.internetReachability` goes from `NotReachable` to reachable, it reloads every enabled format that is not ready.
    - Banner and MREC: if the refresh value is greater than 0, stop SDK auto-refresh and call the SDK's manual load every N seconds while visible.
    - Revenue for every format raises `Paid` with `MaxRevenueConversion.Create(...)`.

- [ ] **Step 1:** Write `ToMicros_RoundsAndClamps` (`0.0012345` → `1235`, `-1` → `0`, `double.NaN` → `0`) and `Create_SetsMaxPlatformAndUsd`. Run them. Expected: compile failure.
- [ ] **Step 2:** Implement `MaxRevenueConversion`. Run. Expected: PASS.
- [ ] **Step 3:** Implement the MAX provider files. `MaxAdsService` follows the `AdMobAdsService` structure: handlers per format, `MainThreadDispatcher` for SDK callbacks, `Raise(...)` for events.
- [ ] **Step 4:** Compile check: in a copy of `ripple-verify`, use the Hub SDK page to Setup `max`. Expected: `RippleGame.Ads.Max.dll` is built with no compile errors. Then remove `max` and confirm the project still compiles without it.
- [ ] **Step 5:** Commit with `feat: add applovin max provider`.

---

### Task 9: Firebase bootstrap and the Analytics.Firebase split

**Files:**
- Create: `Runtime/Firebase/RippleGame.Firebase.asmdef` (versionDefines `com.google.firebase.app` `[13.1.0]` → `RIPPLEGAME_SDK_FIREBASE_APP`) and `Runtime/Firebase/FirebaseBootstrap.cs`
- Move: `Runtime/Analytics/Firebase/FirebaseAnalyticsService.cs` → `Runtime/Analytics.Firebase/` with `RippleGame.Analytics.Firebase.asmdef`, which references `RippleGame.Analytics.Runtime` and `RippleGame.Firebase`. Its versionDefines cover `com.google.firebase.analytics` `[13.1.0]` → `RIPPLEGAME_SDK_FIREBASE_ANALYTICS` and `com.google.firebase.app` `[13.1.0]` → `RIPPLEGAME_SDK_FIREBASE_APP`, and both are constraints.
- Modify: `Runtime/Analytics/AnalyticsBootstrap.cs`, `Runtime/Analytics/AssemblyInfo.cs`, `Editor/Analytics/AnalyticsMenu.cs`, `Editor/Analytics/AnalyticsPreBuildValidator.cs`, `Editor/Tools/ToolHub/Pages/AnalyticsHubPage.cs`
- Delete: `Editor/Analytics/FirebaseDefineUtility.cs` and its tests in `Tests/Analytics/Editor/FirebaseEditorToolsTests.cs` (keep any tests there that do not use the define)
- Test: `Tests/Analytics/Editor/AnalyticsBootstrapTests.cs`

**Interfaces:**
- Produces:
  - `public static class FirebaseBootstrap` (namespace `RippleGame.Firebase`): `static Task<bool> EnsureReadyAsync()`. It runs `FirebaseApp.CheckAndFixDependenciesAsync()` once, returns true on `DependencyStatus.Available`, and logs `"[RippleGame Firebase] Firebase unavailable: <reason>"` otherwise.
  - `AnalyticsBootstrap`:
    - `internal static void RegisterBackend(Action<AnalyticsSettings> initializer)`;
    - `Initialize` uses the registered backend when one exists and the code is not running in the Editor, otherwise the Console service;
    - `#if FIREBASE` is removed.
  - `FirebaseAnalyticsService` registers itself through `RegisterBackend` at `AfterAssembliesLoaded`, and awaits `FirebaseBootstrap.EnsureReadyAsync()` instead of calling `CheckAndFixDependenciesAsync` itself.
  - `AnalyticsMenu` drops Enable/Disable Firebase. `AnalyticsHubPage` shows "Firebase" when the type `RippleGame.Analytics.FirebaseAnalyticsService, RippleGame.Analytics.Firebase` exists, otherwise "Console only", with a hint to use the SDK page. `AnalyticsPreBuildValidator` runs its config-file check only when that type exists.

- [ ] **Step 1:** Write `Initialize_NoBackendRegistered_UsesConsole` and `Initialize_BackendThrows_FallsBackToConsole` (port of the existing `StartService` test). Run them. Expected: FAIL.
- [ ] **Step 2:** Implement. `grep -rn "FIREBASE" Runtime Editor` returns only `RIPPLEGAME_SDK_FIREBASE_*` asmdef defines.
- [ ] **Step 3:** Run all tests. Expected: PASS. Compile check with `firebase-analytics` set up through the Hub: both Firebase assemblies are built.
- [ ] **Step 4:** Commit with `feat!: gate firebase analytics on its pinned package`.

---

### Task 10: Remote Config core and Firebase provider

**Files:**
- Create: `Runtime/RemoteConfig/RippleGame.RemoteConfig.asmdef` (references `Unity.Newtonsoft.Json`), `RemoteConfig.cs`, `IRemoteConfigSource.cs`, `RemoteConfigSettings.cs`, `RemoteConfigValueParser.cs`, `RemoteConfigBootstrap.cs`, `RemoteConfigNetworkWatcher.cs`, `AssemblyInfo.cs`
- Create: `Runtime/RemoteConfig.Firebase/RippleGame.RemoteConfig.Firebase.asmdef` (versionDefines `com.google.firebase.remote-config` `[13.1.0]` and `com.google.firebase.app` `[13.1.0]`, both constraints) and `FirebaseRemoteConfigSource.cs`
- Create: `Editor/RemoteConfig/RemoteConfigMenu.cs` (Create Settings, like `AnalyticsMenu`), inside the existing analytics editor asmdef or a new `RippleGame.RemoteConfig.Editor.asmdef`
- Test: `Tests/RemoteConfig/RippleGame.RemoteConfig.Tests.asmdef`, `RemoteConfigTests.cs`, `RemoteConfigValueParserTests.cs`

**Interfaces:**
- Produces, in namespace `RippleGame.RemoteConfig`:
  - `public interface IRemoteConfigSource`:
    - `Task<bool> FetchAndActivateAsync(TimeSpan timeout, TimeSpan minimumInterval)`;
    - `bool TryGetString(string key, out string value)`;
    - `void SetDefaults(IReadOnlyDictionary<string,string> defaults)`.
  - `public static class RemoteConfig`:
    - `bool IsReady`, `event Action Activated`;
    - `T Get<T>(string key, T defaultValue)`;
    - `T GetJson<T>(string key, T defaultValue)`;
    - `Task WaitUntilReadyAsync(float timeoutSeconds)`, which completes on ready or on timeout, whichever comes first;
    - `internal static void RegisterSource(IRemoteConfigSource source)`, `internal static void ResetForTests()`;
    - lookup order is source value, then settings default, then `defaultValue`.
  - `public sealed class RemoteConfigSettings : ScriptableObject` at Resources path `RippleGameRemoteConfig/RemoteConfigSettings`:
    - `float FetchTimeoutSeconds` = 5;
    - `int MinimumFetchIntervalSeconds` = 3600, but 0 when `ADS_TEST` is defined;
    - `List<RemoteConfigDefault> Defaults`, where each item is `{ string Key; string Value; }`.
  - `internal static class RemoteConfigValueParser`: `static bool TryParse<T>(string raw, out T value)` for int, long, float, double, bool and string, using `CultureInfo.InvariantCulture`. bool accepts `true`/`false` (case-insensitive), `1` and `0`.
  - `RemoteConfigBootstrap` runs at `BeforeSceneLoad`:
    - no source registered → ready at once;
    - source registered → `FetchAndActivateAsync(timeout)`; ready on success, failure or timeout, and `Activated` is raised only on success;
    - if it never succeeded, a hidden `DontDestroyOnLoad` `RemoteConfigNetworkWatcher` retries once each time reachability goes from `NotReachable` to reachable.
  - Parse failures log `"[RippleGame RemoteConfig] Key '<key>' is not a valid <Type>. Using the default."` once per key.
  - `FirebaseRemoteConfigSource` awaits `FirebaseBootstrap.EnsureReadyAsync()` and uses `FirebaseRemoteConfig.DefaultInstance` with `FetchAsync(minimumInterval)`, then `ActivateAsync()` raced against a `Task.Delay(timeout)`. It registers at `AfterAssembliesLoaded`.

- [ ] **Step 1: Write the failing tests** with a `FakeRemoteConfigSource` (configurable result, delay and values):

```csharp
[Test] public void Get_SourceValueWins_ThenSettingsDefault_ThenArgument()
[Test] public void Get_InvalidInt_ReturnsDefault_WarnsOncePerKey()
[Test] public void Parser_FloatUsesInvariantCulture()            // "1.5" parses with CurrentCulture = de-DE
[Test] public void Parser_BoolAcceptsOneAndZero()
[UnityTest] public IEnumerator FetchTimeout_StillBecomesReady_NoActivatedEvent()   // Review Focus 4
[UnityTest] public IEnumerator FetchSuccess_RaisesActivatedOnce()
[Test] public void NoSource_IsReadyImmediately()
[UnityTest] public IEnumerator WaitUntilReady_ReturnsAfterTimeoutWhenNeverReady()
```

- [ ] **Step 2:** Run. Expected: compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run all tests. Expected: PASS. Compile check with `firebase-remote-config` set up through the Hub.
- [ ] **Step 5:** Commit with `feat: add remote config module with firebase provider`.

---

### Task 11: Ads ↔ Remote Config bridge

**Files:**
- Create: `Runtime/Ads.RemoteConfigBridge/RippleGame.Ads.RemoteConfigBridge.asmdef` (references Ads runtime, RemoteConfig and `Unity.Newtonsoft.Json`; no SDK), `AdsRemoteConfigKeys.cs`, `AdsPolicyJson.cs`, `AdUnitIdsJson.cs`, `AdsRemoteConfigBridge.cs`
- Test: `Tests/Ads.RemoteConfigBridge/RippleGame.Ads.RemoteConfigBridge.Tests.asmdef`, `AdsPolicyJsonTests.cs`, `AdsRemoteConfigBridgeTests.cs`

**Interfaces:**
- Consumes: `RemoteConfig` (Task 10); `Ads.ApplyPolicyOverride`, `Ads.SetAdsEnabled`, `Ads.InitializationGate`, `AdUnitIdOverrides.Set` (Tasks 5 and 6).
- Produces:
  - `internal static class AdsRemoteConfigKeys`: `AdsEnabled = "ads_enabled"`, `AdsPolicy = "ads_policy"`, `AdsUnitIds = "ads_unit_ids"`.
  - `internal static class AdsPolicyJson`: `static AdsPolicyOverride Parse(string json)`.
    - It reads the exact paths from spec 5.3: `interstitial.cooldown_seconds|max_per_session|min_level|start_session|placements.<snake_case AdPlacement>`, `app_open.enabled|cooldown_seconds|min_background_seconds`, `rewarded_fallback.enabled`, `organic.banner|mrec|interstitial|app_open`, `banner.refresh_seconds` and `mrec.refresh_seconds`.
    - A field with the wrong JSON type is skipped. Unknown fields are ignored.
    - Invalid JSON returns null and logs a warning.
  - `internal static class AdUnitIdsJson`: `static IEnumerable<(string provider, string platform, AdFormat format, string id)> Parse(string json)`. Format keys are `banner`, `interstitial`, `rewarded`, `app_open` and `mrec`; empty strings are skipped.
  - `internal static class AdsRemoteConfigBridge`, installed at `AfterAssembliesLoaded`:
    1. apply the cached override from PlayerPrefs key `"ripplegame.ads.policy_override_cache"` (raw JSON) if one exists;
    2. set `Ads.InitializationGate = () => RemoteConfig.WaitUntilReadyAsync(settings.FetchTimeoutSeconds)`;
    3. on `RemoteConfig.Activated`, and once when ready if no fetch ever succeeds: read `ads_enabled` → `Ads.SetAdsEnabled`; read `ads_policy`, and if it parses, cache it and `Ads.ApplyPolicyOverride` (ignored if the snapshot was already taken); read `ads_unit_ids` → `AdUnitIdOverrides.Set` for each entry.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void Parse_OnlyPresentFieldsAreSet()                 // {"interstitial":{"min_level":3}} -> MinLevel 3, CooldownSeconds null
[Test] public void Parse_WrongTypeFieldSkipped_OthersApplied()     // "cooldown_seconds":"abc", "min_level":3 -> Review Focus 2
[Test] public void Parse_PlacementsUseSnakeCase()                  // "level_complete":false, "rewarded_fallback":false
[Test] public void Parse_InvalidJson_ReturnsNull_Warns()
[Test] public void UnitIds_SkipsEmptyAndUnknownFormats()
[Test] public void Bridge_CachedOverrideAppliedBeforeFetch()       // fake RemoteConfig source never answers
[Test] public void Bridge_AdsEnabledFalse_DisablesAds()
[UnityTest] public IEnumerator Bridge_GateCompletesAfterTimeoutWhenSourceNeverAnswers() // Review Focus 4: Ads.InitializationGate finishes within FetchTimeoutSeconds + 1 s
```

- [ ] **Step 2:** Run. Expected: compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run all tests. Expected: PASS.
- [ ] **Step 5:** Commit with `feat: drive ad rules and unit ids from remote config`.

---

### Task 12: Attribution core, AppsFlyer provider and Ads ↔ Attribution bridge

**Files:**
- Create: `Runtime/Attribution/RippleGame.Attribution.asmdef`, `Attribution.cs`, `AttributionStatus.cs`, `IAttributionService.cs`, `AttributionAdRevenue.cs`, `AttributionSettings.cs`, `ConversionDataParser.cs`, `AttributionBootstrap.cs`, `AssemblyInfo.cs`
- Create: `Runtime/Attribution.AppsFlyer/RippleGame.Attribution.AppsFlyer.asmdef` (versionDefines on the AppsFlyer package name from Task 1 `[6.17.9]` → `RIPPLEGAME_SDK_APPSFLYER`; references the AppsFlyer asmdef from Task 1), `AppsFlyerAttributionService.cs`, `AppsFlyerHost.cs`
- Create: `Runtime/Ads.AttributionBridge/RippleGame.Ads.AttributionBridge.asmdef` (Ads runtime and Attribution; no SDK) and `AdsAttributionBridge.cs`
- Create: `Editor/Attribution/AttributionMenu.cs` (Create Settings)
- Modify: `Runtime/AnalyticsAdsBridge/AdsAnalyticsBridge.cs` (replace the `AdPlatform` constant with `adsEvent.Revenue.Value.Platform`)
- Test: `Tests/Attribution/RippleGame.Attribution.Tests.asmdef`, `AttributionTests.cs`, `ConversionDataParserTests.cs`; `Tests/Ads.AttributionBridge/RippleGame.Ads.AttributionBridge.Tests.asmdef`, `AdsAttributionBridgeTests.cs`; `Tests/Analytics/AdsBridge/` (extend)

**Interfaces:**
- Produces, in namespace `RippleGame.Attribution`:
  - `public enum AttributionStatus { Unknown, Organic, NonOrganic }`
  - `public readonly struct AttributionAdRevenue(double value, string currency, string platform, string adUnitId, string adSource, string format, string placement)`
  - `public interface IAttributionService { void Initialize(AttributionSettings settings); void LogAdRevenue(AttributionAdRevenue revenue); }`
  - `public static class Attribution`:
    - `AttributionStatus Status`, `string MediaSource`, `string Campaign`, `event Action<AttributionStatus> StatusChanged`;
    - `void LogAdRevenue(AttributionAdRevenue revenue)`, which does nothing when there is no service or when `LogAdRevenue` is off in settings;
    - `internal static void RegisterService(IAttributionService s)`;
    - `internal static void ReportConversion(AttributionStatus s, string mediaSource, string campaign)`. This persists to PlayerPrefs keys `"ripplegame.attribution.status"`, `".media_source"` and `".campaign"` only when `s != Unknown`, and raises `StatusChanged` only when the status changes;
    - the persisted values load at `BeforeSceneLoad`.
  - `public sealed class AttributionSettings : ScriptableObject` at Resources path `RippleGameAttribution/AttributionSettings`: `string DevKey`, `string IosAppId`, `bool DebugLog` (false), `bool LogAdRevenue` (true).
  - `public static class ConversionDataParser`: `static (AttributionStatus status, string mediaSource, string campaign) Parse(IDictionary<string, object> data)`. `af_status` `"Organic"` → Organic, `"Non-organic"` → NonOrganic (both case-insensitive), anything else → Unknown.
  - `AppsFlyerAttributionService`: on `Initialize`, an empty `DevKey` logs `"[RippleGame Attribution] AppsFlyer dev key is empty. Attribution is off."` and returns. Otherwise it creates the `AppsFlyerHost` MonoBehaviour (`DontDestroyOnLoad`, implements the SDK's conversion-data interface), calls `initSDK(devKey, iosAppId, host)` and `startSDK()`. Conversion success goes to `ConversionDataParser` and then `Attribution.ReportConversion`. Revenue uses the SDK's ad revenue API with mediation network `ApplovinMax` when the platform is `applovin_max`, and `GoogleAdMob` when it is `admob`.
  - `AdsAttributionBridge`, installed at `AfterAssembliesLoaded`:
    - `Ads.EventRaised` `Paid` → `Attribution.LogAdRevenue(new AttributionAdRevenue(micros / 1_000_000.0, currency, platform, unitId, source, format.ToString(), placement.ToString()))`;
    - on install and on every `StatusChanged` → `Ads.SetOrganic(Attribution.Status == AttributionStatus.Organic)`, so Unknown counts as non-organic.

- [ ] **Step 1: Write the failing tests**

```csharp
[Test] public void Parse_OrganicCaseInsensitive() / Parse_NonOrganic() / Parse_MissingStatus_Unknown()
[Test] public void ReportConversion_Unknown_IsNotPersisted()
[Test] public void ReportConversion_PersistsAndRaisesOnlyOnChange()
[Test] public void LogAdRevenue_SettingOff_DoesNotCallService()
[Test] public void Bridge_PaidEvent_ConvertsMicrosToUnits()          // 1_500_000 micros -> 1.5
[Test] public void Bridge_UnknownStatus_SetsNonOrganic()
[Test] public void AnalyticsBridge_UsesRevenuePlatform()            // applovin_max appears in ad_impression params
```

- [ ] **Step 2:** Run. Expected: compile failure.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run all tests. Expected: PASS. Compile check with `appsflyer` set up through the Hub.
- [ ] **Step 5:** Commit with `feat: add attribution module with appsflyer provider`.

---

### Task 13: Package guard, version, docs

**Files:**
- Modify: `Tools~/Check-Package.ps1`, `package.json`, `CHANGELOG.md`, `README.md` (modules table), `Documentation~/Migration.md`, `Documentation~/HuongDan.md`, `Documentation~/ads/Setup.md`, `Documentation~/analytics/Setup.md`
- Create: `Documentation~/sdk/Setup.md` (SDK page, Remote Config keys with the JSON example from spec 5.3, AppsFlyer settings), `Documentation~/DeviceChecklist-0.4.0.md`

- [ ] **Step 1:** In `Check-Package.ps1`, add two checks:
  - parse `Editor/Sdk/SdkCatalog.json` and every `*.asmdef`, and throw `'versionDefines do not match SdkCatalog: <file>'` when an expression for a catalog package is not `[version]`;
  - forbid matches of `[A-Za-z0-9_-]{86}` inside string literals in `Runtime`/`Editor` `.cs` files (MAX SDK key shape) and the literal AppsFlyer dev key shape `"[A-Za-z0-9]{22}"`. Throw `'Possible SDK key in <file>'`.

  Run the script. Expected: `RIPPLE_PACKAGE_BOUNDARY_PASS ...`.
- [ ] **Step 2:** `package.json`: set `"version": "0.4.0"`, add the Newtonsoft dependency, update `description` and `keywords` (`max`, `remote-config`, `appsflyer`).
- [ ] **Step 3:** In `CHANGELOG.md`, add `## 0.4.0` with **Breaking** lines exactly as spec section 7 lists them, plus feature lines for MAX, MREC, show rules, rewarded fallback, Remote Config, AppsFlyer and the SDK page.
- [ ] **Step 4:** In `Migration.md` and `HuongDan.md`, add a 0.3.x → 0.4.0 section:
  1. remove the `ADS`/`FIREBASE` symbols, or apply a build profile, which removes `ADS`;
  2. remove `Assets/GoogleMobileAds`, `Assets/Firebase`, `Assets/ExternalDependencyManager` imports;
  3. open the SDK page and Setup the SDKs the game uses;
  4. set `providerId`;
  5. add `Ads.SetPolicyContext` calls.
- [ ] **Step 5:** `DeviceChecklist-0.4.0.md` covers both providers:
  - init;
  - banner, MREC, inter, rewarded and app-open show;
  - fallback;
  - cooldown;
  - organic block;
  - `ads_policy` override on the next session;
  - revenue visible in the AppsFlyer and Firebase debug views.

  Each line has a result column.
- [ ] **Step 6:** Commit with `docs: describe 0.4.0 sdk setup and migration`.

---

### Task 14: Full verification in Unity

**Files:**
- Modify: `Documentation~/Verification.md` (add `# Verification - 0.4.0, <date>` at the top)

- [ ] **Step 1:** Run all EditMode and PlayMode tests in `ripple-verify` with no SDKs. Expected: all pass. Record the counts.
- [ ] **Step 2:** Using the Hub SDK page in copies of `ripple-verify`, build these combinations. Each must import with zero compile errors (`grep -c "error CS" Editor.log` = 0) and show the expected `Library/ScriptAssemblies` DLLs:
  - (a) none;
  - (b) admob;
  - (c) max;
  - (d) max + appsflyer + firebase-remote-config + firebase-analytics.

  Run the test suites in (d) too.
- [ ] **Step 3:** Wrong-version check: point the `max` manifest entry at an 8.5.0 tarball or semver. Expected: the Hub row shows WrongVersion, `RippleGame.Ads.Max.dll` is not built, and an Android build with `providerId = "max"` fails with the `ProviderBuildCheck` message.
- [ ] **Step 4:** Ask the user to run `DeviceChecklist-0.4.0.md` on an Android device with their own MAX keys. Record results, or "Not run".
- [ ] **Step 5:** Write the Verification entry: numbers, combinations, what was not run.
- [ ] **Step 6:** Commit with `docs: record 0.4.0 verification`.

---

### Task 15: Publish (each step needs the user's explicit yes)

- [ ] **Step 1:** Ask the user. Then create the public repo `namdev26/ripple-game-sdks` with `gh repo create`, push the local folder from Task 1 with a README that explains it holds pinned SDK tarballs for ripple-game-base, and create release `sdks-0.4.0` with the `.tgz` files as assets.
- [ ] **Step 2:** Re-run Task 4 Step 4 against the real release URLs: Setup `edm` downloads and the SHA-256 matches.
- [ ] **Step 3:** Ask the user. Push `feat/0.4.0-sdk-providers` and open a PR to `main`.
- [ ] **Step 4:** After merge and the user's yes, tag `v0.4.0` on `main` and push the tag.
- [ ] **Step 5:** Ask the user whether to delete `E:\_NamGame\_somia-source`.
