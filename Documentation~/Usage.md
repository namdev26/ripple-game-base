# Module usage

Author: RIPPLE Game by Namdev26

## Start a new game

1. Install the package from its Git URL.
2. Create your save key in **RIPPLE Game > Open Hub > Save**.
3. Keep one `SaveFile` for the whole game.
4. Write your own rules, results, rewards, coins and levels. The package has no gameplay code.

## Save

See [Save](Save.md).

## Scenes

```csharp
using RippleGame.Scenes;

SceneLoader.Load("Home");
StartCoroutine(SceneLoader.LoadRoutine("Home", minSeconds: 1f, onProgress: value => bar.fillAmount = value));
```

A scene missing from Build Settings throws an error naming it.

## Safe area

Add `RippleGame.UI.SafeArea` to a full-screen RectTransform under your Canvas to keep its content away from notches and home bars.

## Popup UI

```csharp
using RippleGame.UI;
using UnityEngine;

public sealed class SettingsButton : MonoBehaviour
{
    [SerializeField] private PopupHost popups;
    [SerializeField] private PopupView settingsPrefab;
    public void OpenSettings() => popups.Show(settingsPrefab);
}
```

Attach PopupHost to your Canvas/container and drag your own prefab into the caller's field. A custom view can derive from PopupView and call Close or the protected InvokeAndClose helper. Closing deactivates the view immediately, then destroys it at frame end. Showing a valid replacement closes the previous popup and activates the new view, including an inactive prefab. The host stages instances inactive before taking ownership, so OnEnable sees the correct current view. If a lifecycle callback opens a newer popup, that newer request owns the host and the superseded returned view is already closing. No Resources folder, prefab naming convention, ad action or reward formula is imposed. PopupHost.Container can point to an explicit transform.

## ADS and Analytics

Use the existing `RippleGame.Ads` and `RippleGame.Analytics` APIs. The combined package keeps their assembly and public type names. Project settings are created in RIPPLE Game > Open Hub, on the ADS and Analytics pages. Do not distribute generated project assets as shared source. SDK setup and event conventions are described in [ADS](ads/Setup.md) and [Analytics](analytics/Setup.md).

Install the combined package instead of separately copying the original modules. Their source is under Runtime/Ads, Runtime/Analytics and their Editor folders here.
