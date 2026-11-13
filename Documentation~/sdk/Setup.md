# Optional SDKs (0.4.0)

RIPPLE Game Base never ships a third-party SDK. It pins one set of SDK versions, and **RIPPLE Game > Open Hub > SDK** installs exactly that set. A game picks the SDKs it uses. With none installed, the whole package still compiles, ads report no provider, analytics print to the Console, Remote Config returns its defaults and attribution stays `Unknown`.

## Pinned set

| SDK | UPM package | Version | Source of the tarball |
| --- | --- | --- | --- |
| External Dependency Manager | `com.google.external-dependency-manager` | 1.2.186 | `ripple-game-sdks` release |
| Firebase App | `com.google.firebase.app` | 13.1.0 | `ripple-game-sdks` release |
| Firebase Analytics | `com.google.firebase.analytics` | 13.1.0 | `ripple-game-sdks` release |
| Firebase Remote Config | `com.google.firebase.remote-config` | 13.1.0 | `ripple-game-sdks` release |
| Firebase Crashlytics | `com.google.firebase.crashlytics` | 13.1.0 | `ripple-game-sdks` release |
| Firebase Cloud Messaging | `com.google.firebase.messaging` | 13.1.0 | `ripple-game-sdks` release |
| Google Mobile Ads (AdMob) | `com.google.ads.mobile` | 11.0.0 | `ripple-game-sdks` release |
| AppLovin MAX | `com.applovin.mediation.ads` | 8.5.1 | AppLovin's official registry (`unity.packages.applovin.com`). Its license does not allow redistribution, so it is not in `ripple-game-sdks` |
| AppsFlyer | `appsflyer-unity-plugin` | 6.17.900 | `ripple-game-sdks` release |

Every download is checked against the SHA-256 in `Editor/Sdk/SdkCatalog.json`. One External Dependency Manager serves all SDKs. Crashlytics and Cloud Messaging are only installed at the right version; the package has no code for them.

## The SDK page

Each SDK has one row with one of these states:

| State | Meaning | Button |
| --- | --- | --- |
| Not installed | Nothing in `Packages/manifest.json` | **Setup** |
| Installed | The pinned version is installed | **Remove** |
| Wrong version | Shows the installed reference and the expected version | **Fix** |
| Old copy found in Assets | An old `.unitypackage` import of this SDK is still in `Assets`. Shows the marker paths found (see below) | none |

- **Setup** downloads the tarball into `Packages/RippleSdks/`, checks its SHA-256, writes `"<package>": "file:RippleSdks/<file>.tgz"` into `Packages/manifest.json`, and sets up missing dependencies in the same step (for example External Dependency Manager, and `com.unity.ugui` 2.0.0 for MAX). When a checksum is wrong the download is deleted and the manifest is not touched.
- **Fix** brings a wrong version to the pinned one.
- **Remove** takes the entry out of the manifest and deletes the tarball. It is refused while another installed SDK needs this one (remove that SDK first). Dependencies that Setup added, such as External Dependency Manager and `com.unity.ugui`, stay in the project.
- The Hub never deletes old copies under `Assets`. **Delete the old plugin files by hand** (with their `.meta` files), then press Setup. Setup is refused for the SDK being installed when that SDK, or an SDK in its dependency chain (for example External Dependency Manager for MAX), still has an old import. Old imports of unrelated SDKs do not block it.

An old import is recognised by marker paths that only a `.unitypackage` import creates. The UPM packages write some files under the same roots, and those are not old copies: `Assets/MaxSdk/Resources/AppLovinSettings.asset`, `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`, `Assets/GoogleMobileAds/link.xml` (written at build), `Assets/GeneratedLocalRepo`, `Assets/Plugins/Android`. A `google-services.json` kept in `Assets/Firebase` is not an old copy either.

| SDK | Markers (any one of them means an old import) |
| --- | --- |
| External Dependency Manager | `Assets/ExternalDependencyManager/Editor` |
| Firebase (every Firebase row) | `Assets/Firebase/Plugins`, `Assets/Firebase/Editor` |
| Google Mobile Ads | `Assets/GoogleMobileAds/Editor`, `Assets/GoogleMobileAds/GoogleMobileAds.dll`, `Assets/GoogleMobileAds/Api`, `Assets/GoogleMobileAds/Platforms` |
| AppLovin MAX | `Assets/MaxSdk/Scripts`, `Assets/MaxSdk/AppLovin` |
| AppsFlyer | `Assets/AppsFlyer/Editor`, `Assets/AppsFlyer/Plugins`, `Assets/AppsFlyer/AppsFlyer.cs` |

When a marker is found, delete that plugin's whole old import: for MAX everything under `Assets/MaxSdk` except `Resources/AppLovinSettings.asset`, for Google Mobile Ads everything under `Assets/GoogleMobileAds` except `Resources/GoogleMobileAdsSettings.asset`, and the `Assets/Firebase`, `Assets/ExternalDependencyManager` (and `Assets/PlayServicesResolver`) and `Assets/AppsFlyer` folders. Keep `google-services.json` and `GoogleService-Info.plist`.

## Guards against a wrong version

1. **Compile.** Each provider assembly builds only with its exact pinned version. A missing or different version turns that assembly off.
2. **Build.** Before a build, a validator stops it when the selected `providerId` has no compiled assembly, or when an installed SDK has the wrong version. The message says to open the SDK page.
3. **Repository.** `Tools~/Check-Package.ps1` and the tests check that every asmdef `versionDefines` entry equals the version in `SdkCatalog.json`.

## Ads providers

`providerId` in the project's `AdsConfiguration` is `none`, `admob` or `max`. A new configuration starts at `none`. Create it from **RIPPLE Game > Open Hub > ADS > Create Configuration**, then see [Ads setup](../ads/Setup.md).

### AppLovin MAX

1. SDK page: **Setup** on AppLovin MAX (it also adds `com.unity.ugui` 2.0.0 and External Dependency Manager).
2. In the `AdsConfiguration`, set `providerId` to `max` and fill the **Max Test** and **Max Production** profiles: the SDK key and the unit IDs for banner, interstitial, rewarded, app open and MREC, separately for Android and iOS. A Production build is refused while the SDK key or an enabled format's ID is empty.
3. `MaxSdk.SetSdkKey` is deprecated in 8.5.1, but the package still forwards the profile's key to it. **Also enter the same SDK key in AppLovin's Integration Manager** (AppLovin > Integration Manager). AppLovin Quality Service reads the key from there.
4. MREC works on MAX: `Ads.ShowMrec(MrecPosition.Center)` and `Ads.HideMrec()`. AdMob has no MREC in 0.4.0 and logs one warning.

### AdMob

1. SDK page: **Setup** on Google Mobile Ads.
2. Open **Assets > Google Mobile Ads > Settings** once so the plugin creates its settings asset.
3. Set `providerId` to `admob`. New configurations hold Google's Android demo IDs in the Test profile. Fill the Production profile before a production build.
4. AdMob builds are Android only. For a build on another platform, set `ADS_DISABLED` for that platform, because the **No Ads** profile writes the Android symbols only.

## Firebase

1. SDK page: **Setup** on Firebase Analytics and/or Firebase Remote Config. Firebase App and External Dependency Manager come with them.
2. Put `google-services.json` (Android) or `GoogleService-Info.plist` (iOS) under `Assets/`. A build is refused when Firebase Analytics is installed and the file is missing.
3. Firebase starts once, shared by Analytics and Remote Config. If it cannot start, the log shows one Firebase error and the game continues without it.

Analytics events are described in [Analytics setup](../analytics/Setup.md). The `ad_impression` event uses `ad_platform` `AdMob` or `AppLovin`.

## Remote Config

The module works without an SDK (defaults only). The Firebase backend needs Firebase Remote Config from the SDK page.

Create the optional settings asset with **Assets > Create > RIPPLE Game > REMOTE CONFIG > Remote Config Settings**. It **must** live at `Assets/Resources/RippleGameRemoteConfig/RemoteConfigSettings.asset`; the package loads it from `Resources/RippleGameRemoteConfig/RemoteConfigSettings`.

| Field | Default | Meaning |
| --- | --- | --- |
| Fetch Timeout Seconds | 5 | Longest wait for a fetch. After it the game continues with the last activated values, or with the defaults |
| Minimum Fetch Interval Seconds | 3600 | Cache lifetime. Builds with the `ADS_TEST` symbol (Ads Test profile) always use 0 |
| Defaults | empty | In-app key/value pairs used when the server has no value |

```csharp
using RippleGame.RemoteConfig;

var speed = RemoteConfig.Get<float>("player_speed", 3f);
var enabled = RemoteConfig.Get<bool>("feature_x", false);
var tuning = RemoteConfig.GetJson<MyTuning>("tuning", new MyTuning());

RemoteConfig.Activated += () => Debug.Log("New values are active.");
```

`Get<T>` supports int, long, float, double, bool and string. A value of the wrong type returns the default and logs one warning per key. A fetch that fails or times out still sets `RemoteConfig.IsReady`, using the last activated values or the defaults. Firebase Remote Config also runs in the Unity Editor. Without Firebase's `google-services-desktop.json` it logs one Firebase error, and the game still runs on the defaults.

### Ad keys

| Key | Type | Default | Effect |
| --- | --- | --- | --- |
| `ads_enabled` | bool | true | `false` shows no ad at all, and rewarded calls its reward callback at once and returns `Success`. Applies immediately |
| `ads_policy` | JSON | none | Overrides show rules. Only the fields present are changed; unknown fields are ignored |
| `ads_unit_ids` | JSON | none | Overrides unit IDs per provider and platform |

`ads_policy` example (every field is optional):

```json
{
  "interstitial": { "cooldown_seconds": 60, "max_per_session": 0, "min_level": 3, "start_session": 2,
                    "placements": { "replay": false, "pause": false } },
  "app_open": { "enabled": true, "cooldown_seconds": 30, "min_background_seconds": 5 },
  "rewarded_fallback": { "enabled": true },
  "organic": { "banner": true, "mrec": true, "interstitial": false, "app_open": true },
  "banner": { "refresh_seconds": 15 },
  "mrec": { "refresh_seconds": 15 }
}
```

- Integer fields (`max_per_session`, `min_level`, `start_session`) accept integer tokens only; `15.0` is skipped. Float fields (`cooldown_seconds`, `min_background_seconds`, `refresh_seconds`) accept an integer or a decimal. A field with the wrong type is skipped.
- Placement names are the `AdPlacement` names in snake_case: `default`, `home`, `level_start`, `level_complete`, `retry`, `double_reward`, `extra_life`, `pause`, `replay`, `rewarded_fallback`.
- `banner.refresh_seconds` and `mrec.refresh_seconds` apply to MAX only, and are read when MAX initializes. AdMob ignores them: the AdMob SDK refreshes its banner by itself.
- The rules are fixed once per session, at the first ad decision. A policy that arrives later applies from the next session. The last good policy is cached on the device and applied at the start of the next session.
- A missing or blank `ads_policy` keeps the cached override. To clear a cached override, publish `{}`.

`ads_unit_ids` example. Providers are `max` and `admob`, platforms `android` and `ios`, formats `banner`, `interstitial`, `rewarded`, `app_open` and `mrec`:

```json
{ "max": { "android": { "interstitial": "your-unit-id" } }, "admob": { "android": { "banner": "your-unit-id" } } }
```

Empty strings are ignored. Both providers read their IDs when they initialize. Ads waits for Remote Config for at most the fetch timeout before initializing, so fetched IDs (or the settings defaults, when the fetch fails or times out) are in place first. The last good `ads_unit_ids` is cached on the device and applied at the start of the next session, so IDs that arrive after the provider initialized apply from the next session. A missing or blank key, or a value that is not a JSON object, keeps the cache; publish `{}` to clear it.

## Attribution and AppsFlyer

1. SDK page: **Setup** on AppsFlyer.
2. Create the settings asset with **Assets > Create > RIPPLE Game > ATTRIBUTION > Attribution Settings**. It **must** live at `Assets/Resources/RippleGameAttribution/AttributionSettings.asset`; the package loads it from `Resources/RippleGameAttribution/AttributionSettings`.

| Field | Default | Meaning |
| --- | --- | --- |
| Dev Key | empty | AppsFlyer dev key. Attribution stays off while it is empty |
| Ios App Id | empty | App Store app id, digits only. Needed on iOS |
| Debug Log | off | AppsFlyer SDK debug log. Leave off in release builds |
| Log Ad Revenue | on | Send every ad impression's revenue to AppsFlyer |

Keep the dev key in the game project's asset, never in the package.

- `Attribution.Status` is `Unknown`, `Organic` or `NonOrganic`, with `MediaSource`, `Campaign` and the `StatusChanged` event. The first known status is saved and read back on the next launch.
- AppsFlyer runs on devices only. The Editor does not start it, so status stays `Unknown` there.
- With Ads installed, a bridge sends each paid impression to AppsFlyer (MAX as `applovin_max`, AdMob as `admob`) and feeds the status into the organic rules of `ads_policy`. `Unknown` counts as not organic, so ads show until AppsFlyer says the user is organic.

## Game code for the ads rules

Ads rules that depend on the game need the game to tell Ads its state:

```csharp
using RippleGame.Ads;

Ads.SetPolicyContext(new AdsPolicyContext(currentLevel: 12, entitlement: AdsEntitlement.None, hasUserConsent: true));
```

Call it at start and whenever the level, entitlement or consent changes. Without it, `min_level` is ignored with one warning. `RemoveAds` blocks banner, MREC, interstitial and app open; `RemoveAdsAndFreeRewards` also makes rewarded grant at once without an ad. A `hasUserConsent` value is passed to MAX before it initializes.
