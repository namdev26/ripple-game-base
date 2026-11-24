# Setup

1. Install the combined `com.ripplegame.base` UPM package from its tagged Git URL.
2. Install the official Google Mobile Ads Unity plugin in the host project.
3. Open the Google plugin's Settings menu once, so it creates its settings asset.
4. Open **RIPPLE Game > Open Hub > ADS** and press **Create Configuration**.
5. New configurations already contain Google Android demo IDs in the editable Test profile. Fill Production IDs before a production build. Existing configurations are preserved. Disabled formats do not require an ID.
6. Open your game's startup scene, then press **Add Ads Object to Active Scene** on the ADS page and save that scene.
7. Press **Configure** on the **Ads Test** row and make a device build.

During development, use Google's test IDs or configure a test device. Do not click live production ads while testing.

The `[RippleGame Ads]` GameObject is saved in the startup scene chosen by the consuming game. It uses `DontDestroyOnLoad`, so it loads configuration once and stays available across that game's scene transitions.

Banner routing includes all build scenes automatically, including scenes added later. `Banner Excluded Scene Names` in the configuration defaults to `Loading` and `LoadingScene`. Scenes outside the build do not show an automatic banner.

## Another provider

A game can add its own provider (for example LevelPlay) by registering an `IAdsProviderFactory` with `AdsProviderRegistry` and setting **Provider Id** to that provider's id. Put the provider's app key and ad unit IDs in a ScriptableObject of the game and assign it to **Provider Settings**. The provider reads it from `configuration.ProviderSettings`. The ADS page then hides the AdMob IDs and shows the provider's IDs. Format toggles, cooldowns and retries still come from the Test and Production profiles.
