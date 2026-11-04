# Migration and rollback

## New consuming project

Install the tagged Git package. Create new project configurations from the TOOL menus. Assign this project's own popup prefabs, identifiers, scenes and save keys. Install vendor SDKs only when needed; do not enable ADS or FIREBASE before their SDKs are present.

## Project already containing old TOOL code

1. Commit or back up the consuming project first, including current package lockfile and project-owned assets.
2. Identify old Scene Switch, Texture, Build and Hub scripts, plus the separate com.ripplegame.ads/com.ripplegame.analytics packages. Move their sources and metas outside the active Assets/Packages trees as one reversible backup before installing the combined package; do not keep duplicate copies imported.
3. Keep all project-owned configuration assets, original game scripts, levels, prefabs, scenes and assets in place. Preserve their GUIDs. Never edit scene/prefab YAML to repair a migration.
4. Install the combined Git package. Its existing shared scripts retain their source GUIDs and Ads/Analytics assembly names. The included bridge detects com.ripplegame.base.
5. For a legacy game popup base, a compatibility adapter can inherit RippleGame.UI.PopupView. Keep game-specific routing/reward rules in game code and supply prefabs to PopupHost. This package does not ship the original game's popup subclasses or prefabs.
6. For a legacy wallet, construct CurrencyWallet with a PlayerPrefsBalanceStore using that game's EXACT existing balance key. Adapt the old TryAddCoins API in game code if necessary; no automatic save migration runs.
7. Compile and test scene/prefab bindings, saves, popup actions and SDK flows in that project before discarding its backup.

Do not install the package alongside imported old implementations without performing the migration above. From 0.2.0 the TOOL sample runs on this package, so its Snake scripts show a complete migration.

## Update or roll back a Git installation

Update the revision suffix to a new immutable release tag and resolve packages. Keep configuration assets and save keys unchanged. Commit the manifest/lockfile after validation.

To roll back, restore the previous manifest/lockfile revision or previous tag, then resolve. If rolling back the initial migration, remove the combined dependency and restore the backed-up old sources/metas to their original paths. Never install both generations at once. Do not delete project-owned configuration or player saves as part of a source rollback.

Git cache files are not shared development source. Make code changes in an editable clone or embedded development copy, then release a new tag. Never put credentials in a Git URL or commit vendor Firebase files, ad ID assets or signing files into this repository.
