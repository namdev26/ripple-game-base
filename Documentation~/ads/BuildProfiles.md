# Android build profiles

`ADS` is the source of truth for whether this package creates an AdMob provider. `ADS_TEST` is valid only with `ADS`.

Use the three build menu commands rather than editing symbols manually. The pre-build guard validates the selected environment and writes that environment's Android App ID to the Google plugin's public settings object.

| Command | Result |
| --- | --- |
| `RIPPLE Game/ADS/Build Android/No Ads` | Removes `ADS` and `ADS_TEST`. Rewarded reports `Disabled` and never grants a reward. |
| `RIPPLE Game/ADS/Build Android/Ads Test` | Adds `ADS;ADS_TEST`; validates the Test profile. |
| `RIPPLE Game/ADS/Build Android/Ads Production` | Adds `ADS`, removes `ADS_TEST`; rejects known Google demo IDs. |

Do a clean device test for every format before shipping a new production configuration. Banner uses one persistent native view across Home and Gameplay: it hides in Loading and is shown again after a full-screen ad closes. Test App Open behavior by backgrounding and foregrounding the app; it has a configurable cooldown and never shows while a different full-screen ad is active.
