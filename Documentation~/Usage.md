# Module usage

Author: RIPPLE Game by Namdev26

## Currency

```csharp
using RippleGame.Economy;
using RippleGame.Persistence;

var wallet = new CurrencyWallet(new PlayerPrefsBalanceStore("mygame.currency.coins"));
wallet.BalanceChanged += balance => UpdateCoinLabel(balance);
wallet.TryAdd(50);
if (wallet.TrySpend(10)) UnlockPurchase();
```

The caller owns the key. Reuse an existing game's old key when migrating to keep its balance. Separate keys isolate currencies. Adds require positive amounts and saturate at int.MaxValue; spends reject nonpositive amounts and insufficient funds. Notifications occur after persistence succeeds. Supply another ICurrencyBalanceStore for a different save system. Operations run synchronously on the calling thread; PlayerPrefs calls belong on Unity's main thread.

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

Use the existing `RippleGame.Ads` and `RippleGame.Analytics` APIs. The combined package keeps their assembly and public type names. Project settings are created through TOOL > ADS and TOOL > ANALYTICS. Do not distribute generated project assets as shared source. SDK setup and event conventions are described in [ADS](ads/Setup.md) and [Analytics](analytics/Setup.md).

Install the combined package instead of separately copying the original modules. Their source is under Runtime/Ads, Runtime/Analytics and their Editor folders here.
