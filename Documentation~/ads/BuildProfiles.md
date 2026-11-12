# Android build profiles

Two symbols select the environment, and neither is required: `ADS_TEST` selects the Test profile and `ADS_DISABLED` turns ads off. With neither symbol the build is Production. The old `ADS` symbol no longer exists (0.4.0); the profile commands remove it from a project that still has it.

Which provider runs comes from `providerId` in the project's `AdsConfiguration` (`none`, `admob` or `max`), and that provider's SDK must be installed from RIPPLE Game > Open Hub > SDK at the pinned version. A build stops with an explanation when the selected provider's SDK is missing or has the wrong version. If a game should not show ads, apply No Ads or set `providerId` to `none`.

Use the three **Build** buttons in RIPPLE Game > Open Hub > ADS rather than editing symbols manually. The pre-build guard validates the selected environment. For AdMob it also writes that environment's Android App ID to the Google plugin's public settings object. For MAX, a Production build needs the SDK key and a unit ID for every enabled format.

| Command | Result |
| --- | --- |
| **No Ads** | Removes `ADS` and `ADS_TEST`, adds `ADS_DISABLED`. No provider checks run. Rewarded reports `Disabled` and never grants a reward. |
| **Ads Test** | Removes `ADS` and `ADS_DISABLED`, adds `ADS_TEST`; validates the Test profile. Remote Config also fetches with no cache in this profile. |
| **Ads Production** | Removes `ADS`, `ADS_TEST` and `ADS_DISABLED`; rejects known Google demo IDs and, for MAX, empty keys or unit IDs. |

The commands write the **Android** symbols only. The AdMob provider builds on Android only, so for iOS or another platform that must not show ads, add `ADS_DISABLED` for that platform in Player Settings. MAX builds on Android and iOS.

A game that was on 0.3.x without the `ADS` symbol (the old No Ads setup) now builds as Production. Apply **No Ads** or set `providerId` to `none`, see [Migration](../Migration.md).

Do a clean device test for every format before shipping a new production configuration, using the [device checklist](../DeviceChecklist-0.4.0.md). Banner uses one persistent native view across Home and Gameplay: it hides in Loading and is shown again after a full-screen ad closes. Test App Open behavior by backgrounding and foregrounding the app; it has a configurable cooldown and never shows while a different full-screen ad is active.
