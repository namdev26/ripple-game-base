# Setup

1. Install the combined `com.ripplegame.base` UPM package from its tagged Git URL.
2. Install the official Google Mobile Ads Unity plugin in the host project.
3. Open the Google plugin's Settings menu once, so it creates its settings asset.
4. Run **RIPPLE Game > ADS > Create Ads Configuration**.
5. New configurations already contain Google Android demo IDs in the editable Test profile. Fill Production IDs before a production build. Existing configurations are preserved. Disabled formats do not require an ID.
6. Open your game's startup scene, then select **RIPPLE Game > ADS > Scene Setup > Add Ads Object to Active Scene** and save that scene.
7. Select **RIPPLE Game > ADS > Configure Android > Ads Test** and make a device build.

During development, use Google's test IDs or configure a test device. Do not click live production ads while testing.

The `[RippleGame Ads]` GameObject is saved in the startup scene chosen by the consuming game. It uses `DontDestroyOnLoad`, so it loads configuration once and stays available across that game's scene transitions.

Banner routing includes all build scenes automatically, including scenes added later. `Banner Excluded Scene Names` in the configuration defaults to `Loading` and `LoadingScene`. Scenes outside the build do not show an automatic banner.
