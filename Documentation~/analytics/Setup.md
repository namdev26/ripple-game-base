# Setup RippleGame Analytics

## 1. Đưa package vào project

Cài package kết hợp `com.ripplegame.base` bằng Git URL có tag. Package đã chứa module Analytics, Ads và cầu nối giữa hai module; không cài thêm bản package rời trùng assembly.

Tới đây game đã chạy được: event chỉ in ra Console, chưa gửi lên Firebase.

## 2. Tạo app trên Firebase (mỗi game một lần)

1. Vào https://console.firebase.google.com, tạo project và thêm app Android có package name trùng `Player Settings > Package Name`.
2. Tải `google-services.json` và bỏ vào `Assets/`. Với iOS thì tải `GoogleService-Info.plist`.

## 3. Cài Firebase Analytics từ Hub

Mở **RIPPLE Game > Open Hub > SDK** và bấm **Setup** ở dòng **Firebase Analytics**. Hub tự tải đúng bản Firebase mà package đã ghim (13.1.0), kiểm tra SHA-256, thêm vào `Packages/manifest.json` và cài luôn Firebase App cùng External Dependency Manager. Không import `.unitypackage` vào `Assets`.

- Nếu project còn bản import `.unitypackage` cũ của Firebase hoặc External Dependency Manager (Hub nhận ra qua `Assets/Firebase/Plugins`, `Assets/Firebase/Editor` hoặc `Assets/ExternalDependencyManager/Editor`), Hub từ chối Setup. Xóa bằng tay thư mục `Assets/Firebase` và `Assets/ExternalDependencyManager` (cả file `.meta`, giữ lại `google-services.json`), rồi bấm Setup lại.
- Không còn define `FIREBASE` và không còn nút **Enable Firebase** hay **Disable Firebase**. Cài đúng bản SDK là Firebase tự bật, gỡ SDK (nút **Remove**) là quay lại chế độ chỉ log Console. Trang **Analytics** hiện `Backend: Firebase` hoặc `Console only` để bạn kiểm tra.
- Cài sai bản thì phần code Firebase không compile và build bị chặn. Bấm **Fix** ở dòng SDK.

## 4. Build

Nếu đã cài Firebase Analytics mà thiếu `google-services.json` (Android) hoặc `GoogleService-Info.plist` (iOS) trong `Assets/`, lúc build sẽ báo lỗi. Trong Editor, analytics luôn dùng Console dù đã cài Firebase.

Firebase chỉ được khởi tạo một lần và dùng chung cho Analytics và Remote Config. Cài thêm Remote Config thì xem [sdk/Setup.md](../sdk/Setup.md).

## 5. Settings (không bắt buộc)

Nút **Create Configuration** trong trang Analytics tạo file `Assets/Resources/RippleGameAnalytics/AnalyticsSettings.asset`:

| Ô | Mặc định | Ý nghĩa |
|---|---|---|
| Enable Console Log | bật | In mọi event ra Console |
| Log Ad Revenue | bật | Log `ad_impression` có doanh thu. **Tắt nếu đã link AdMob với Firebase**, vì khi đó AdMob tự log và doanh thu sẽ bị đếm 2 lần |
| Collection Enabled | bật | Trạng thái thu thập sau khi Firebase khởi tạo xong |

### Chờ người dùng đồng ý (consent)

Firebase SDK bắt đầu gửi `first_open`/`session_start` ngay khi app mở, **trước** khi settings kịp áp dụng. Muốn chặn hẳn cho tới khi người dùng đồng ý thì phải tắt ở mức native:

- Android, trong `Assets/Plugins/Android/AndroidManifest.xml`, bên trong `<application>`:
  ```xml
  <meta-data android:name="firebase_analytics_collection_enabled" android:value="false" />
  ```
- iOS, trong Info.plist: `FIREBASE_ANALYTICS_COLLECTION_ENABLED` = `NO`.

Khi người dùng đồng ý thì gọi `Analytics.SetCollectionEnabled(true)`.

Không cần kéo object nào vào scene, vì analytics tự khởi tạo trước khi scene đầu tiên load.

## Cách dùng

```csharp
using RippleGame.Analytics;

GameEvents.LevelStart(5);                         // attempt và duration được tính tự động
GameEvents.LevelComplete(5, score: 1200);
GameEvents.LevelFail(5, reason: "hit_wall");

GameEvents.TutorialBegin("move");
GameEvents.TutorialStep("move", 2);
GameEvents.TutorialComplete("move");

GameEvents.EarnCurrency("coin", 50, source: "level_reward");
GameEvents.SpendCurrency("coin", 200, itemName: "skin_red");

GameEvents.ScreenView("Home");

// Event riêng của từng game
Analytics.LogEvent("shop_open", new AnalyticsParams().Add("tab", "skins"));
Analytics.SetUserProperty("ab_group", "B");
```

Chỉ gọi những event game cần. Hàm nào không gọi thì không có event đó.

Event quảng cáo (`ad_impression`, `ad_show`, `ad_show_fail`, `ad_reward_earned`) được log tự động qua module Ads đi kèm khi game sử dụng quảng cáo, cả với AdMob lẫn AppLovin MAX. Với `ad_impression`, `ad_platform` là `AdMob` hoặc `AppLovin` tùy provider đang chạy.

## Danh sách event

| Event | Param |
|---|---|
| `level_start` | `level`, `level_name`, `attempt`, `mode`? |
| `level_complete` | `level`, `level_name`, `attempt`, `duration_sec`, `score`? |
| `level_fail` | `level`, `level_name`, `attempt`, `duration_sec`, `fail_reason`? |
| `tutorial_begin` / `tutorial_complete` | `tutorial_id` |
| `tutorial_step` | `tutorial_id`, `step` |
| `earn_virtual_currency` | `virtual_currency_name`, `value`, `source` |
| `spend_virtual_currency` | `virtual_currency_name`, `value`, `item_name` |
| `screen_view` | `screen_name`, `screen_class` |
| `ad_impression` | `ad_platform` (`AdMob` hoặc `AppLovin`), `ad_source`, `ad_format`, `ad_unit_name`, `value`, `currency`, `placement` |
| `ad_show` | `ad_format`, `placement` |
| `ad_show_fail` | `ad_format`, `placement`, `error` |
| `ad_reward_earned` | `placement` (không dùng `ad_reward` vì Firebase giữ tên này) |

User property `max_level` được cập nhật khi gọi `LevelComplete`.

Tên event và param phải theo luật Firebase: tối đa 40 ký tự, chỉ gồm chữ, số và `_`, bắt đầu bằng chữ. Chuỗi giá trị tối đa 100 ký tự, mỗi event tối đa 25 param. Event sai luật sẽ bị bỏ và hiện warning trong Editor.

## Kiểm tra

- **Editor:** Console hiện `[RippleGame Analytics] level_complete {level=5, level_name=level_5, attempt=2, duration_sec=48, score=1200}`.
- **Máy Android:** chạy `adb shell setprop debug.firebase.analytics.app <package.name>`, mở app, rồi xem Firebase Console > DebugView. Báo cáo thường phải chờ khoảng 24 giờ.
