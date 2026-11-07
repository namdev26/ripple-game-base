# Hướng dẫn dùng RIPPLE Game Base

Tài liệu này dành cho dev làm game trên package. Bạn chỉ cần làm theo các bước và chép ví dụ, không cần đọc code bên trong package. Toàn bộ ví dụ đều đã được compile thử với bản 0.3.0.

## Mục lục

1. [Package có gì](#1-package-có-gì)
2. [Muốn làm gì thì gọi gì](#2-muốn-làm-gì-thì-gọi-gì)
3. [Cài đặt](#3-cài-đặt)
4. [Dựng game mới từng bước](#4-dựng-game-mới-từng-bước)
5. [Save](#5-save)
6. [Quảng cáo](#6-quảng-cáo)
7. [Analytics](#7-analytics)
8. [Popup](#8-popup)
9. [Chuyển scene và màn hình loading](#9-chuyển-scene-và-màn-hình-loading)
10. [Safe area](#10-safe-area)
11. [TOOL Hub](#11-tool-hub)
12. [Nâng cấp package](#12-nâng-cấp-package)
13. [Lỗi hay gặp](#13-lỗi-hay-gặp)
14. [Giới hạn cần biết](#14-giới-hạn-cần-biết)
15. [Đa ngôn ngữ](#15-đa-ngôn-ngữ)

## 1. Package có gì

| Phần | Làm được gì | Bạn phải làm |
|---|---|---|
| Save | Lưu dữ liệu vào file mã hóa, tự ghi, tự khôi phục khi file hỏng | Tạo key, khai báo class dữ liệu |
| Quảng cáo | Banner, interstitial, rewarded, app open qua AdMob | Cài SDK, điền ID |
| Analytics | Gửi event lên Firebase, chưa có Firebase thì in ra Console | Cài SDK nếu muốn gửi thật |
| Popup | Mỗi lúc hiện một popup, popup mới thay popup cũ | Làm prefab |
| Scene | Chuyển scene, màn hình loading có thanh tiến trình | Thêm scene vào Build Settings |
| Safe area | Tránh tai thỏ và thanh home | Gắn component |
| Đa ngôn ngữ | Đọc file dịch, đổi ngôn ngữ ngay khi đang chạy, sync từ Google Sheet | Làm Sheet, chọn ngôn ngữ, lo font |
| TOOL Hub | Gom các công cụ Editor vào một cửa sổ, kiểm tra bản mới | Mở **RIPPLE Game > Open Hub** |

Package chỉ có phần nền. Luật chơi, thắng thua, hồi sinh, thưởng, coin, level, art, prefab và scene đều do game tự viết.

## 2. Muốn làm gì thì gọi gì

Các ví dụ dưới đây dùng class `GameData` ở [mục 4](#bước-2-tạo-class-gamedata).

| Muốn | Gọi |
|---|---|
| Đọc dữ liệu đã lưu | `GameData.Player.Value.bestScore` |
| Lưu dữ liệu | `GameData.Player.Update(d => d.bestScore = 120)` |
| Ghi xuống file ngay | `GameData.Save.Flush()` |
| Hiện quảng cáo có thưởng | `Ads.ShowRewarded(AdPlacement.Default, () => { ... })` |
| Hiện quảng cáo xen kẽ | `Ads.ShowInterstitial(AdPlacement.Home)` |
| Ẩn hoặc hiện banner | `Ads.HideBanner()`, `Ads.ShowBanner()` |
| Gửi event | `Analytics.LogEvent("shop_open")` |
| Hiện popup | `popups.Show(settingsPrefab)` |
| Chuyển scene | `SceneLoader.Load("Home")` |
| Lấy chữ đã dịch | `Localizer.Get("s_home")` |
| Đổi ngôn ngữ | `Localizer.SetLanguage("Vietnamese")` |

## 3. Cài đặt

Máy cần có Unity 6000.0 trở lên và [Git](https://git-scm.com/).

1. Mở **Window > Package Manager**.
2. Bấm **+ > Install package from git URL**.
3. Dán địa chỉ sau rồi bấm **Install**:

```text
https://github.com/namdev26/ripple-game-base.git#v0.3.0
```

Hoặc thêm một dòng vào `dependencies` trong `Packages/manifest.json`:

```json
"com.ripplegame.base": "https://github.com/namdev26/ripple-game-base.git#v0.3.0"
```

Sau đó commit cả `Packages/manifest.json` và `Packages/packages-lock.json` để cả team dùng cùng một bản.

**Xem code mẫu:** trong Package Manager, chọn RIPPLE Game Base, mở tab **Samples** và bấm **Import** ở mục **Basic Integration**.

**Project đang có bản copy cũ của các tool này:** phải xóa bản cũ trước, vì trùng tên assembly sẽ lỗi compile. Xem [Migration.md](Migration.md).

## 4. Dựng game mới từng bước

### Bước 1: Tạo key save

Mở **RIPPLE Game > Open Hub > Save**, giữ thư mục `Assets` hoặc chọn thư mục khác, rồi bấm **Create GameSaveKey.cs**. Commit file này cùng game.

> Mỗi game một key. Sau khi phát hành **không được đổi key**, vì đổi hoặc mất key thì người chơi mất save.

### Bước 2: Tạo class GameData

Đây là nơi duy nhất mở save. Cả game dùng chung class này.

```csharp
using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerData
{
    public int bestScore;
    public bool soundOn = true;
    public List<string> ownedSkins = new List<string>();
}
```

```csharp
using RippleGame.Save;
using UnityEngine;

public static class GameData
{
    public static readonly SaveFile Save = OpenSave();
    public static readonly SaveSection<PlayerData> Player = Save.Section<PlayerData>("player");

    private static SaveFile OpenSave()
    {
        var save = new SaveFile("main", GameSaveKey.Value);
        save.ProblemRaised += problem => Debug.LogWarning("Save problem: " + problem);
        return save;
    }
}
```

### Bước 3: Viết game

Luật chơi, thắng thua, thưởng, coin và level do game tự viết theo cách phù hợp, rồi lưu vào Save. Base không bắt game implement interface nào cho các phần này. Dự án TOOL có ví dụ: `SnakeProgress` và `SnakeLevelFlow`.

### Bước 4: Bật quảng cáo và analytics khi cần

Làm theo [mục 6](#6-quảng-cáo) và [mục 7](#7-analytics). Chưa cài thì game vẫn chạy: quảng cáo báo không sẵn sàng, event in ra Console.

## 5. Save

### Đọc và ghi

```csharp
var best = GameData.Player.Value.bestScore;

GameData.Player.Update(data => data.bestScore = 120);

GameData.Player.Update(data =>
{
    data.soundOn = false;
    data.ownedSkins.Add("red");
});
```

Luôn đổi dữ liệu qua `Update`. Sửa thẳng `Value` thì save không biết có thay đổi.

### Khi nào dữ liệu được ghi xuống file

Bạn không cần tự gọi lệnh lưu. Dữ liệu được ghi vào các lúc sau:

| Thời điểm | Ghi chú |
|---|---|
| Tối đa 5 giây sau khi đổi | Đổi bằng `new SaveOptions { AutoFlushSeconds = 10 }`, đặt `0` để tắt |
| App xuống nền, mất focus hoặc thoát | Tự động |
| Khi gọi `GameData.Save.Flush()` | Dùng ngay sau việc quan trọng như mua hàng |

Nhiều lần `Update` liên tiếp chỉ ghi một lần.

### Thêm dữ liệu mới

Tạo thêm section với một tên mới. Mỗi section là một class `[Serializable]` có constructor mặc định.

```csharp
using System;

[Serializable]
public sealed class SettingsData
{
    public float musicVolume = 1f;
}
```

```csharp
var settings = GameData.Save.Section<SettingsData>("settings");
settings.Update(data => data.musicVolume = 0.5f);
```

### Đổi cấu trúc dữ liệu giữa các bản game

- **Thêm field mới:** không cần làm gì. Save cũ nhận giá trị khởi tạo của field đó.
- **Đổi tên, đổi kiểu hoặc đổi ý nghĩa field:** tăng version và viết hàm chuyển đổi.

```csharp
var settings = GameData.Save.Section<SettingsData>("settings", 2, (oldVersion, oldJson) =>
{
    if (oldVersion == 1) return oldJson.Replace("\"volume\"", "\"musicVolume\"");
    return oldJson;
});
```

Save cũ có version thấp hơn mà thiếu hàm chuyển đổi sẽ báo lỗi ngay khi gọi `Section`, nên bạn sẽ thấy lúc phát triển.

### Khi save gặp sự cố

Mọi sự cố đều được ghi log và báo qua `ProblemRaised`, đã đăng ký sẵn trong `GameData`.

| Sự cố | Ý nghĩa | Nên làm |
|---|---|---|
| `RecoveredFromBackup` | File chính hỏng, đã đọc từ bản dự phòng | Thường không cần làm gì |
| `DataLost` | Không còn bản đọc được, dữ liệu về mặc định, file lỗi được giữ lại | Điều tra ngay, hay gặp nhất là do đổi key. Trả lại key đúng thì save tự khôi phục |
| `WriteFailed` | Không ghi được, thường do đầy bộ nhớ, sẽ tự thử lại | Có thể báo người chơi |
| `NewerVersion` | Save do bản game mới hơn tạo, phiên này không ghi | Nhắc cập nhật game |
| `ReadFailed` | Tạm thời không đọc được file, phiên này không ghi | Nhắc mở lại game |

### Xem và xóa save khi debug

- **Xem nội dung:** mở **RIPPLE Game > Open Hub > Save**, chọn file, dán key rồi bấm **Decrypt**.
- **Vị trí file:** thư mục `Application.persistentDataPath`, tên `main.sav` nếu đặt tên `main`.
- **Chơi lại từ đầu:** xóa các file `main.*` trong thư mục đó khi không chạy game.

### Chuyển dữ liệu cũ từ PlayerPrefs

Chép một lần và dùng một section để đánh dấu đã chép:

```csharp
using System;

[Serializable]
public sealed class LegacyImport
{
    public bool done;
}
```

```csharp
var import = GameData.Save.Section<LegacyImport>("legacy_import");
if (!import.Value.done)
{
    GameData.Player.Update(data => data.bestScore = PlayerPrefs.GetInt("old.score.key", 0));
    import.Update(data => data.done = true);
    GameData.Save.Flush();
}
```

Chi tiết hơn nằm ở [Save.md](Save.md).

## 6. Quảng cáo

### Cài đặt lần đầu

1. Cài [Google Mobile Ads Unity plugin](https://developers.google.com/admob/unity/quick-start) vào project.
2. Mở menu Settings của plugin một lần để nó tạo file cấu hình.
3. Chọn **RIPPLE Game > ADS > Create Ads Configuration**. File tạo ra đã có sẵn ID test của Google cho Android.
4. Mở scene khởi động của game, chọn **RIPPLE Game > ADS > Scene Setup > Add Ads Object to Active Scene**, rồi lưu scene.
5. Chọn **RIPPLE Game > ADS > Configure Android > Ads Test** và build thử lên máy.

Khi sắp phát hành, điền ID thật vào mục `Production` trong file cấu hình, rồi dùng **Ads Production**.

Các lệnh này cũng có trong **RIPPLE Game > Open Hub > ADS**.

### Chọn chế độ build

Đừng tự sửa define symbol, hãy dùng menu **RIPPLE Game > ADS > Configure Android** để cấu hình hoặc **RIPPLE Game > ADS > Build Android** để build luôn:

| Chế độ | Kết quả |
|---|---|
| **No Ads** | Tắt quảng cáo. Rewarded báo `Disabled` và không bao giờ cho thưởng |
| **Ads Test** | Dùng ID test. Dùng khi phát triển |
| **Ads Production** | Dùng ID thật, từ chối build nếu còn ID demo của Google |

Không bấm vào quảng cáo thật khi test.

### Banner

Banner tự hiện ở mọi scene trong Build Settings, trừ các scene trong ô `Banner Excluded Scene Names` của file cấu hình (mặc định là `Loading` và `LoadingScene`). Thường bạn không cần gọi gì. Nếu muốn tự điều khiển:

```csharp
Ads.ShowBanner(BannerPosition.Bottom);
Ads.HideBanner();
```

### Quảng cáo có thưởng (rewarded)

Hàm thứ nhất chỉ chạy khi người chơi **thực sự nhận thưởng**. Hàm thứ hai luôn chạy khi quảng cáo kết thúc, kể cả khi không hiện được.

```csharp
Ads.ShowRewarded(
    AdPlacement.Default,
    rewarded: () => GiveReward(),
    completed: result => ResumeGame());
```

Chỉ phát thưởng trong hàm `rewarded`, đừng dựa vào `result`.

Muốn làm mờ nút khi chưa có quảng cáo:

```csharp
watchButton.interactable = Ads.IsReady(AdFormat.Rewarded);
```

### Quảng cáo xen kẽ (interstitial)

```csharp
Ads.ShowInterstitial(AdPlacement.Home, result => ResumeGame());
```

Luôn tiếp tục game trong `completed`, vì quảng cáo có thể không hiện. Mặc định mỗi lần cách nhau ít nhất 45 giây và tối đa 4 lần mỗi phiên chơi. Đổi hai số này trong file cấu hình.

### Quảng cáo khi mở app (app open)

Tự hiện khi người chơi quay lại game sau khi rời đi ít nhất 5 giây, cách nhau ít nhất 20 giây. Bạn không cần gọi gì. Đổi hai số này trong file cấu hình, hoặc tắt bằng ô `Enable App Open`.

### Giá trị của `result`

| Giá trị | Ý nghĩa |
|---|---|
| `Closed` | Quảng cáo đã hiện và người chơi đã đóng |
| `Cooldown` | Chưa đủ thời gian chờ, hoặc đã đủ số lần trong phiên |
| `NotReady` | Quảng cáo chưa tải xong, hoặc SDK chưa khởi tạo xong |
| `AlreadyShowing` | Đang có quảng cáo toàn màn hình khác |
| `Failed` | SDK báo lỗi khi hiện |
| `Disabled` | Đang build No Ads |

### Vị trí quảng cáo (`AdPlacement`)

`Default`, `Home`, `LevelStart`, `LevelComplete`, `Retry`, `DoubleReward`, `ExtraLife`. Đây là nhãn để xem báo cáo trên analytics, không đổi cách hiện quảng cáo.

### Theo dõi mọi sự kiện quảng cáo

Hiếm khi cần, vì analytics đã tự ghi các event này:

```csharp
Ads.EventRaised += adsEvent => Debug.Log(adsEvent.Type + " " + adsEvent.Format);
```

## 7. Analytics

Không cần đặt object nào vào scene. Chưa có Firebase thì event in ra Console, nên bạn dùng được ngay.

### Gửi event

```csharp
GameEvents.TutorialBegin("move");
GameEvents.TutorialStep("move", 2);
GameEvents.TutorialComplete("move");

GameEvents.EarnCurrency("coin", 50, source: "level_reward");
GameEvents.SpendCurrency("coin", 200, itemName: "skin_red");

GameEvents.ScreenView("Home");

Analytics.LogEvent("shop_open", new AnalyticsParams().Add("tab", "skins"));
Analytics.SetUserProperty("ab_group", "B");
```

### Event đã tự gửi, đừng gửi lại

| Event | Tự gửi khi |
|---|---|
| `ad_impression`, `ad_show`, `ad_show_fail`, `ad_reward_earned` | Dùng module quảng cáo |

Muốn thống kê tiền trong game thì tự gọi `GameEvents.EarnCurrency` và `GameEvents.SpendCurrency`.

Event level do game tự gọi:

```csharp
GameEvents.LevelStart(5);
GameEvents.LevelComplete(5, score: 1200);
GameEvents.LevelFail(5, reason: "hit_wall");
```

### Bật Firebase

1. Tạo project trên [Firebase Console](https://console.firebase.google.com) và thêm app Android có package name trùng với game.
2. Tải `google-services.json` vào `Assets/`.
3. Tải [Firebase Unity SDK](https://firebase.google.com/download/unity) và import **chỉ** `FirebaseAnalytics.unitypackage`.
4. Chọn **RIPPLE Game > ANALYTICS > Enable Firebase**.

Muốn quay về chỉ in Console thì chọn **Disable Firebase**.

### Cần nhớ

- **Tên event và param:** tối đa 40 ký tự, chỉ gồm chữ, số và `_`, bắt đầu bằng chữ. Sai luật thì event bị bỏ và có cảnh báo trong Editor.
- **Doanh thu quảng cáo:** nếu đã link AdMob với Firebase, hãy tắt `Log Ad Revenue` trong **RIPPLE Game > ANALYTICS > Create Analytics Settings**, nếu không doanh thu bị đếm hai lần.
- **Kiểm tra trên máy:** chạy `adb shell setprop debug.firebase.analytics.app <package.name>` rồi xem Firebase Console > DebugView. Báo cáo thường thì chờ khoảng 24 giờ.

Danh sách đầy đủ event và cách chờ người dùng đồng ý (consent) nằm ở [analytics/Setup.md](analytics/Setup.md).

## 8. Popup

1. Gắn `PopupHost` vào Canvas.
2. Làm prefab popup có component kế thừa `PopupView`, hoặc dùng luôn `PopupView`.
3. Kéo prefab vào script của bạn và gọi `Show`.

```csharp
using RippleGame.UI;
using UnityEngine;

public sealed class SettingsButton : MonoBehaviour
{
    [SerializeField] private PopupHost popups;
    [SerializeField] private PopupView settingsPrefab;

    public void OpenSettings() => popups.Show(settingsPrefab);
    public void CloseSettings() => popups.CloseActive();
}
```

- Mỗi lúc chỉ có một popup, popup mới tự đóng popup cũ.
- `Show` trả về bản vừa tạo, dùng nó để truyền dữ liệu: `host.Show(winPrefab).Initialize(score)`.
- Nút đóng trong prefab có thể gắn thẳng vào hàm `Close()` của `PopupView`.
- Muốn popup nằm dưới một object khác, kéo object đó vào ô `Container` của `PopupHost`.

## 9. Chuyển scene và màn hình loading

Scene phải có trong **Build Settings**, nếu không sẽ báo lỗi kèm tên scene.

```csharp
SceneLoader.Load("Home");
```

Màn hình loading có thanh tiến trình:

```csharp
using RippleGame.Scenes;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressBar;

    private void Start()
    {
        StartCoroutine(SceneLoader.LoadRoutine("Home", minSeconds: 1f, onProgress: value => progressBar.fillAmount = value));
    }
}
```

`minSeconds` là thời gian tối thiểu màn hình loading được giữ lại.

## 10. Safe area

Thêm component `RippleGame.UI.SafeArea` vào một RectTransform phủ toàn màn hình nằm dưới Canvas, rồi đặt nội dung vào bên trong. Nội dung sẽ tránh tai thỏ và thanh home.

## 11. TOOL Hub

Mở **RIPPLE Game > Open Hub**:

| Trang | Dùng để |
|---|---|
| Scenes | Xem và chuyển nhanh giữa các scene |
| Android Build | Version, package name, icon, keystore và các cài đặt Android |
| Textures | Chỉnh kích thước ảnh về bội số của 4, tối ưu texture cho Android |
| ADS | Sửa cấu hình quảng cáo, thêm Ads object vào scene, chọn chế độ build |
| Analytics | Bật hoặc tắt Firebase, sửa cài đặt analytics |
| Save | Tạo key save, giải mã file save để xem |
| Localization | Sync file dịch từ Google Sheet |
| Build Reports | Xem kết quả, dung lượng và thời gian của lần build gần nhất |
| Update | Kiểm tra và cài bản package mới |

Các trang Localization, Save và Update cũng mở trực tiếp được từ menu **RIPPLE Game**.

Ngoài ra, chuột phải một ScriptableObject trong Inspector rồi chọn **Export as Remote Config JSON** để xuất ra JSON.

## 12. Nâng cấp package

Unity không tự cập nhật package. Package chỉ thông báo, còn bạn quyết định khi nào nâng:

1. Khi mở Editor, nếu có bản mới thì Console hiện dòng `RIPPLE Game Base v0.3.0 is available`.
2. Mở **RIPPLE Game > Open Hub > Update**. Trang này hiện bản đang dùng và bản mới nhất.
3. Bấm **View changes** để đọc thay đổi. Commit công việc đang làm.
4. Bấm **Update to v0.3.0** rồi xác nhận. Unity tự tải bản mới và compile lại.
5. Commit `Packages/manifest.json` và `Packages/packages-lock.json`.

**Quay lại bản cũ:** revert hai file đó bằng git.

**Nút Update bị mờ:** đang chạy game hoặc đang compile, hoặc project dùng package từ ổ đĩa. Khi đó sửa tay `Packages/manifest.json` thành Git URL có tag mới.

**Cập nhật không ảnh hưởng dữ liệu của game:** file cấu hình quảng cáo, key save, prefab và keystore nằm trong project của bạn, nên không bị ghi đè.

**Người phát hành bản mới:** sửa `version` trong `package.json` để khớp với tag, ghi vào `CHANGELOG.md`, rồi tạo tag dạng `vX.Y.Z` và push. Trang Update chỉ coi các tag dạng này là bản mới, nên tag kiểu `v0.3.0-beta` không được báo cho project khác.

## 13. Lỗi hay gặp

| Hiện tượng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| Nút x2 mờ, không có popup hồi sinh | Rewarded chưa tải xong, đang ở chế độ No Ads, hoặc scene khởi động thiếu Ads object | Chọn **Ads Test**, chạy [cài đặt quảng cáo](#cài-đặt-lần-đầu) lại, đợi quảng cáo tải |
| Bấm Next mà không có interstitial | Đang trong thời gian chờ 45 giây hoặc đã đủ 4 lần | Bình thường, đổi trong file cấu hình nếu cần |
| Banner không hiện | Scene không có trong Build Settings, hoặc nằm trong danh sách loại trừ | Kiểm tra hai chỗ đó |
| Build Production bị từ chối | Còn ID demo của Google hoặc thiếu ID | Điền ID thật trong mục `Production` |
| Lỗi trùng assembly khi cài | Project còn bản copy cũ của Ads, Analytics hoặc tool | Xóa bản cũ, xem [Migration.md](Migration.md) |
| Báo lỗi tên save đã mở | Tạo hai `SaveFile` trùng tên | Chỉ mở trong `GameData`. Nếu tạo trong scene thì phải `Dispose` khi rời scene |
| `DataLost` sau khi cập nhật game | Key bị đổi | Trả lại key cũ, save tự khôi phục |
| Dữ liệu không được lưu | Sửa thẳng `Value` thay vì dùng `Update` | Đổi sang `Update` |
| Lỗi khi load scene | Scene chưa có trong Build Settings | Thêm vào Build Settings |
| Không thấy event trên Firebase | Chưa bật Firebase, hoặc báo cáo chưa kịp cập nhật | Chọn **Enable Firebase**, xem DebugView |
| Trang Update báo `Could not check for updates` | Mất mạng, máy chưa có Git, hoặc không có quyền đọc repo | Kiểm tra mạng và lệnh `git` trong terminal |

## 14. Giới hạn cần biết

- **Đổi hoặc mất key là mất save.** Lỡ phát hành nhầm key thì trả lại key cũ sẽ khôi phục được, nhưng mất hẳn key thì không.
- **Mã hóa chỉ chặn việc sửa file thông thường.** Người có kinh nghiệm vẫn lấy được key từ bản build. IAP và bảng xếp hạng thật vẫn cần server xác thực.
- **Save không lưu trực tiếp `Dictionary`.** Dùng `List` của một class `[Serializable]`. Class dữ liệu không được là `MonoBehaviour` hay `ScriptableObject`.
- **Quảng cáo hiện chỉ có cấu hình ID cho Android.** Save nhắm tới Android và iOS, WebGL chưa làm, chưa có cloud save.
- **Quảng cáo mạng thật và chạy trên điện thoại chưa được kiểm tra đầy đủ trong bản này.** Hãy test trên máy thật trước khi phát hành. Xem [Verification.md](Verification.md).

## 15. Đa ngôn ngữ

Mỗi ngôn ngữ là một file trong `Assets/Resources`: `Localization.txt` cho tiếng Anh và `Localization-<Ngôn ngữ>.txt` cho ngôn ngữ khác. Game tự giữ các file này.

### Bước 1: Tạo Google Sheet

| KEY_lang | English | Vietnamese |
|---|---|---|
| s_home | HOME | TRANG CHỦ |
| s_coin_value | COIN: {0} | XU: {0} |

Dòng đầu là tên cột, cột `English` là bắt buộc, thêm ngôn ngữ chỉ cần thêm cột. Mẫu Sheet cũ của studio (hai ô đầu của dòng 1 để trống) vẫn dùng được. Ô để trống sẽ lấy tiếng Anh.

**Tự dịch bằng Google Translate:** dòng `country_code` chứa mã ngôn ngữ của từng cột (`vi`, `ja`, `zh-CN`...). Dán công thức này vào ô tiếng Việt của dòng key đầu tiên rồi kéo sang các cột khác và xuống các dòng dưới, ví dụ với dòng mã ngôn ngữ là dòng 2 và key đầu ở dòng 4:

```text
=IF($B4="";"";GOOGLETRANSLATE($B4;"en";C$2))
```

Sheet dùng locale Anh thì đổi dấu `;` thành `,`. Từ đó chỉ cần điền cột `English`. Muốn sửa một bản dịch thì gõ đè lên ô đó. Bản dịch máy của từ đơn thường chưa chuẩn (`Home` có thể thành "nhà"), nên hãy xem lại trước khi phát hành. Nếu ô đang báo lỗi như `#ERROR!` thì công cụ sync dừng và không ghi file nào.

### Bước 2: Sync vào project

1. Bật chia sẻ Sheet cho **bất kỳ ai có link**.
2. Mở **RIPPLE Game > Open Hub > Localization**, dán link rồi bấm **Sync**.
3. Đọc báo cáo, rồi commit các file dịch cùng `ProjectSettings/RippleGameLocalization.json`.

Sheet là nguồn chính, nên key đã xóa trên Sheet cũng bị xóa khỏi file, và báo cáo sẽ liệt kê các key đó. Nếu Sheet có lỗi (key trùng, không có key nào, `{0}` của một ngôn ngữ khác tiếng Anh, chưa chia sẻ) thì không file nào bị ghi.

### Bước 3: Khởi tạo khi game chạy

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void InitLanguages() => Localizer.Init(new[] { "English", "Vietnamese" });
```

Chỉ khai báo ngôn ngữ mà font của game hiển thị được. Game chọn ngôn ngữ theo thứ tự: lựa chọn đã lưu, ngôn ngữ máy nếu có trong danh sách, rồi tiếng Anh.

### Bước 4: Gắn vào UI

- **Chữ cố định:** thêm component `LocalizedText` vào object TextMeshPro rồi nhập key. Chữ tự đổi theo ngôn ngữ, kể cả popup đang ẩn.
- **Chữ có số:** dùng `Localizer.Format` và cập nhật lại khi đổi ngôn ngữ:

```csharp
private void OnEnable() { Localizer.Changed += Refresh; Refresh(); }
private void OnDisable() => Localizer.Changed -= Refresh;
private void Refresh() => coinLabel.text = Localizer.Format("s_coin_value", coins);
```

- **Nút chọn ngôn ngữ:** game tự làm, rồi gọi `Localizer.SetLanguage("Vietnamese")`. Chỉ lựa chọn này mới được lưu.

### Giới hạn

- **Game tự lo font.** Tiếng Trung, Nhật, Hàn, Thái và tiếng Việt thường cần thêm fallback font. Thiếu font thì chữ hiện thành ô vuông.
- **Tiếng Ả Rập chưa được xử lý chữ.** Công cụ sync ghi cột `Arabic` như mọi cột khác, nhưng TextMeshPro không tự nối chữ và đảo chiều, nên có font rồi chữ vẫn rời và ngược. Game cần thêm bước xử lý riêng nếu phát hành tiếng Ả Rập.
- **Hindi cần kiểm tra thực tế,** vì TextMeshPro có thể không ghép đúng chữ Devanagari.
- **`LocalizedText` chỉ dùng được với TextMeshPro,** không dùng được với `Text` cũ.
- **Nội dung quảng cáo và tên app trên điện thoại** không đổi theo ngôn ngữ.
