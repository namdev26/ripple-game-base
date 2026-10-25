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
