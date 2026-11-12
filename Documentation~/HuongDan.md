# Hướng dẫn dùng RIPPLE Game Base

Tài liệu này dành cho dev làm game trên package. Bạn chỉ cần làm theo các bước và chép ví dụ, không cần đọc code bên trong package.

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
16. [SDK tùy chọn](#16-sdk-tùy-chọn)
17. [Remote Config](#17-remote-config)
18. [Attribution và AppsFlyer](#18-attribution-và-appsflyer)
19. [Nâng lên 0.4.0 từ 0.3.x](#19-nâng-lên-040-từ-03x)

## 1. Package có gì

| Phần | Làm được gì | Bạn phải làm |
|---|---|---|
| Save | Lưu dữ liệu vào file mã hóa, tự ghi, tự khôi phục khi file hỏng | Tạo key, khai báo class dữ liệu |
| Quảng cáo | Banner, MREC, interstitial, rewarded, app open qua AdMob hoặc AppLovin MAX, kèm luật hiện quảng cáo dùng chung | Cài SDK ở trang SDK, điền ID, gọi `Ads.SetPolicyContext` |
| Analytics | Gửi event lên Firebase, chưa có Firebase thì in ra Console | Cài Firebase Analytics ở trang SDK nếu muốn gửi thật |
| Remote Config | Đọc giá trị từ Firebase, đổi luật quảng cáo không cần build lại | Cài Firebase Remote Config, đặt key trên Firebase |
| Attribution | Biết người chơi organic hay đến từ quảng cáo, báo doanh thu quảng cáo cho AppsFlyer | Cài AppsFlyer, nhập dev key |
| Popup | Mỗi lúc hiện một popup, popup mới thay popup cũ | Làm prefab |
| Scene | Chuyển scene, màn hình loading có thanh tiến trình | Thêm scene vào Build Settings |
| Safe area | Tránh tai thỏ và thanh home | Gắn component |
| Đa ngôn ngữ | Đọc file dịch, đổi ngôn ngữ ngay khi đang chạy, sync từ Google Sheet | Làm Sheet, chọn ngôn ngữ, lo font |
| TOOL Hub | Gom các công cụ Editor vào một cửa sổ, cài đúng bản SDK, kiểm tra bản mới | Mở **RIPPLE Game > Open Hub** |

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
| Hiện hoặc ẩn MREC (chỉ MAX) | `Ads.ShowMrec(MrecPosition.Center)`, `Ads.HideMrec()` |
| Báo level, gói bỏ quảng cáo, đồng ý dữ liệu cho Ads | `Ads.SetPolicyContext(new AdsPolicyContext(level))` |
| Đọc giá trị Remote Config | `RemoteConfig.Get<int>("key", 0)` |
| Biết người chơi organic hay không | `Attribution.Status` |
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
https://github.com/namdev26/ripple-game-base.git#v0.4.0
```

Hoặc thêm một dòng vào `dependencies` trong `Packages/manifest.json`:

```json
"com.ripplegame.base": "https://github.com/namdev26/ripple-game-base.git#v0.4.0"
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

Cài SDK cần dùng ở trang SDK ([mục 16](#16-sdk-tùy-chọn)), rồi làm theo [mục 6](#6-quảng-cáo) và [mục 7](#7-analytics). Chưa cài thì game vẫn chạy: quảng cáo báo không sẵn sàng, event in ra Console.

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

1. Mở **RIPPLE Game > Open Hub > SDK** và bấm **Setup** ở SDK của nhà cung cấp quảng cáo bạn chọn: **Google Mobile Ads** (AdMob) hoặc **AppLovin MAX**. Chi tiết ở [mục 16](#16-sdk-tùy-chọn).
2. Với AdMob: mở **Assets > Google Mobile Ads > Settings** một lần để plugin tạo file cấu hình.
3. Mở **RIPPLE Game > Open Hub > ADS** rồi bấm **Create Configuration**. Với AdMob, file tạo ra đã có sẵn ID test của Google cho Android.
4. Trong file cấu hình, chọn `providerId`: `none` (không quảng cáo, là giá trị ban đầu), `admob` hoặc `max`. Trang ADS báo SDK của provider đó đã compile (`Compiled`) hay chưa.
   - **AdMob:** điền ID thật vào mục `Production` khi sắp phát hành.
   - **MAX:** điền SDK key và ID của từng loại quảng cáo (banner, interstitial, rewarded, app open, MREC), riêng cho Android và iOS, vào hai mục **Max Test** và **Max Production**. Nhập thêm SDK key này vào **AppLovin > Integration Manager**, vì AppLovin Quality Service đọc key ở đó. `MaxSdk.SetSdkKey` đã bị AppLovin đánh dấu cũ ở bản 8.5.1 nhưng package vẫn truyền key giúp bạn.
5. Mở scene khởi động của game, bấm **Add Ads Object to Active Scene** trong trang ADS, rồi lưu scene.
6. Bấm **Configure** ở dòng **Ads Test** và build thử lên máy.
7. Trong code game, gọi `Ads.SetPolicyContext` (xem mục [Báo trạng thái game cho Ads](#báo-trạng-thái-game-cho-ads)).

### Chọn chế độ build

Đừng tự sửa define symbol, hãy dùng nút **Configure** để cấu hình hoặc **Build** để build luôn trong trang ADS của Hub:

| Chế độ | Kết quả |
|---|---|
| **No Ads** | Tắt quảng cáo (define `ADS_DISABLED`). Rewarded báo `Disabled` và không bao giờ cho thưởng |
| **Ads Test** | Dùng ID test (define `ADS_TEST`). Dùng khi phát triển |
| **Ads Production** | Dùng ID thật, từ chối build nếu còn ID demo của Google hoặc thiếu SDK key, ID của MAX |

Không còn define `ADS`. Không có define nào nghĩa là **Production**. Ba nút trên chỉ ghi define cho **Android**. AdMob chỉ build cho Android, nên game dùng AdMob mà còn build iOS hoặc nền tảng khác không muốn quảng cáo thì tự thêm `ADS_DISABLED` cho nền tảng đó trong Player Settings.

Nếu `providerId` chọn provider mà SDK của nó chưa cài hoặc sai bản, build dừng và báo rõ. Game không muốn quảng cáo thì chọn **No Ads** hoặc đặt `providerId` là `none`.

Không bấm vào quảng cáo thật khi test.

### Banner

Banner tự hiện ở mọi scene trong Build Settings, trừ các scene trong ô `Banner Excluded Scene Names` của file cấu hình (mặc định là `Loading` và `LoadingScene`). Thường bạn không cần gọi gì. Nếu muốn tự điều khiển:

```csharp
Ads.ShowBanner(BannerPosition.Bottom);
Ads.HideBanner();
```

### MREC (hình chữ nhật 300x250)

Chỉ có với MAX. AdMob bản này không hỗ trợ MREC, gọi sẽ chỉ có một cảnh báo trong log.

```csharp
Ads.ShowMrec(MrecPosition.Center);   // TopCenter, Center hoặc BottomCenter
Ads.HideMrec();
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

Luôn tiếp tục game trong `completed`, vì quảng cáo có thể không hiện. Mặc định mỗi lần cách nhau ít nhất 45 giây và tối đa 4 lần mỗi phiên chơi. Đổi các số này trong mục `Policy` của file cấu hình, hoặc từ xa bằng Remote Config ([mục 17](#17-remote-config)).

### Quảng cáo khi mở app (app open)

Tự hiện khi người chơi quay lại game sau khi rời đi ít nhất 5 giây, cách nhau ít nhất 20 giây. Bạn không cần gọi gì. Đổi hai số này trong mục `Policy > App Open` của file cấu hình, hoặc tắt bằng ô `Enable App Open`.

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

`Default`, `Home`, `LevelStart`, `LevelComplete`, `Retry`, `DoubleReward`, `ExtraLife`, `Pause`, `Replay`, `RewardedFallback`. Đây là nhãn để xem báo cáo trên analytics. Riêng với interstitial, mỗi vị trí còn có thể bật hoặc tắt riêng trong luật hiện quảng cáo.

### Luật hiện quảng cáo

Mọi luật nằm trong lõi nên AdMob và MAX giống nhau. Chúng nằm ở mục `Policy` của `AdsConfiguration`:

| Luật | Mặc định |
|---|---|
| Interstitial: thời gian chờ giữa hai lần | 45 giây |
| Interstitial: tối đa mỗi phiên | 4 (số 0 là không giới hạn) |
| Interstitial: chỉ từ level | 0 (tắt) |
| Interstitial: chỉ từ phiên chơi thứ | 1 |
| Interstitial: tắt theo vị trí | không tắt vị trí nào |
| App open | bật, chờ 20 giây, rời app ít nhất 5 giây |
| Rewarded fallback | tắt |
| Người chơi organic vẫn thấy banner, MREC, interstitial, app open | cả bốn đều bật |
| Banner, MREC tự đổi quảng cáo sau | 0 giây (để mạng quảng cáo tự làm) |
| Dừng thời gian và âm thanh khi quảng cáo toàn màn hình hiện | bật, trả lại như cũ khi đóng |

Số phiên chơi tăng một lần mỗi lần mở app. Các luật được chốt một lần mỗi phiên, vào lúc cần dùng đầu tiên.

**Rewarded fallback:** khi bật, người chơi bấm xem quảng cáo có thưởng mà rewarded chưa sẵn sàng nhưng interstitial đã sẵn sàng, thì hiện interstitial. Đóng bình thường thì người chơi nhận thưởng và kết quả là `Success`. Interstitial lỗi thì không thưởng và kết quả là `Failed`. Kiểu hiện này bỏ qua thời gian chờ và level tối thiểu.

#### Báo trạng thái game cho Ads

Một số luật cần biết game đang ở đâu. Hãy gọi hàm này lúc mở game và mỗi khi level, quyền lợi hoặc việc đồng ý dữ liệu thay đổi:

```csharp
Ads.SetPolicyContext(new AdsPolicyContext(
    currentLevel: 12,
    entitlement: AdsEntitlement.None,
    hasUserConsent: true));
```

- `entitlement`: `None`; `RemoveAds` (chặn banner, MREC, interstitial và app open, ví dụ người chơi đã mua bỏ quảng cáo); `RemoveAdsAndFreeRewards` (như trên và rewarded cho thưởng ngay, không hiện quảng cáo).
- `hasUserConsent`: `true`, `false`, hoặc `null` nếu chưa hỏi. Giá trị này được truyền cho MAX trước khi khởi tạo.
- Không gọi hàm này thì luật level tối thiểu bị bỏ qua và log một cảnh báo.

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
3. Mở **RIPPLE Game > Open Hub > SDK** rồi bấm **Setup** ở dòng **Firebase Analytics**. Hub tải đúng bản (13.1.0), cài kèm Firebase App và External Dependency Manager. Nếu còn thư mục cũ `Assets/Firebase` thì xóa nó bằng tay trước.
4. Mở trang **Analytics**: dòng `Backend` phải hiện `Firebase`.

Không còn define `FIREBASE` và không còn nút **Enable Firebase** hay **Disable Firebase**. Muốn quay về chỉ in Console thì bấm **Remove** ở dòng Firebase Analytics trên trang SDK. Trong Editor, event luôn chỉ in ra Console.

### Cần nhớ

- **Tên event và param:** tối đa 40 ký tự, chỉ gồm chữ, số và `_`, bắt đầu bằng chữ. Sai luật thì event bị bỏ và có cảnh báo trong Editor.
- **Doanh thu quảng cáo:** event `ad_impression` có `ad_platform` là `AdMob` hoặc `AppLovin`. Nếu đã link AdMob với Firebase, hãy tắt `Log Ad Revenue` trong **RIPPLE Game > Open Hub > Analytics**, nếu không doanh thu bị đếm hai lần.
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
| ADS | Sửa cấu hình quảng cáo, xem provider đang chọn, thêm Ads object vào scene, chọn chế độ build |
| Analytics | Xem analytics đang chạy bằng Firebase hay chỉ Console, sửa cài đặt analytics |
| SDK | Cài, sửa hoặc gỡ các SDK tùy chọn (AdMob, MAX, Firebase, AppsFlyer) đúng bản package yêu cầu |
| Save | Tạo key save, giải mã file save để xem |
| Localization | Sync file dịch từ Google Sheet |
| Build Reports | Xem kết quả, dung lượng và thời gian của lần build gần nhất |
| Update | Kiểm tra và cài bản package mới |

Bấm **Pop out** ở đầu mỗi trang để tách công cụ đó thành tab riêng, rồi kéo gắn cạnh Inspector hay Console. Unity tự nhớ vị trí trong layout.

Ngoài ra, chuột phải một ScriptableObject trong Inspector rồi chọn **Export as Remote Config JSON** để xuất ra JSON.

## 12. Nâng cấp package

Unity không tự cập nhật package. Package chỉ thông báo, còn bạn quyết định khi nào nâng:

1. Khi mở Editor, nếu có bản mới thì Console hiện dòng `RIPPLE Game Base v0.4.0 is available`.
2. Mở **RIPPLE Game > Open Hub > Update**. Trang này hiện bản đang dùng và bản mới nhất.
3. Bấm **View changes** để đọc thay đổi. Commit công việc đang làm.
4. Bấm **Update to v0.4.0** rồi xác nhận. Unity tự tải bản mới và compile lại.
5. Commit `Packages/manifest.json` và `Packages/packages-lock.json`.

**Quay lại bản cũ:** revert hai file đó bằng git.

**Lên 0.4.0 từ 0.3.x:** bản này bỏ define `ADS` và `FIREBASE`, và SDK phải cài qua trang SDK. Làm theo [mục 19](#19-nâng-lên-040-từ-03x) trước khi cập nhật.

**Lên 0.3.0 từ bản cũ hơn:** bản này bỏ Coin, Mở khóa level và Level flow. Game đang dùng các phần đó phải làm theo [Migration.md](Migration.md) trước khi cập nhật.

**Nút Update bị mờ:** đang chạy game hoặc đang compile, hoặc project dùng package từ ổ đĩa. Khi đó sửa tay `Packages/manifest.json` thành Git URL có tag mới.

**Cập nhật không ảnh hưởng dữ liệu của game:** file cấu hình quảng cáo, key save, prefab và keystore nằm trong project của bạn, nên không bị ghi đè.

**Người phát hành bản mới:** sửa `version` trong `package.json` để khớp với tag, ghi vào `CHANGELOG.md`, rồi tạo tag dạng `vX.Y.Z` và push. Trang Update chỉ coi các tag dạng này là bản mới, nên tag kiểu `v0.4.0-beta` không được báo cho project khác.

## 13. Lỗi hay gặp

| Hiện tượng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| Quảng cáo có thưởng báo chưa sẵn sàng | Rewarded chưa tải xong, đang ở chế độ No Ads, `providerId` là `none`, hoặc scene khởi động thiếu Ads object | Chọn **Ads Test**, chạy [cài đặt quảng cáo](#cài-đặt-lần-đầu) lại, đợi quảng cáo tải |
| Gọi interstitial mà không thấy hiện | Đang trong thời gian chờ 45 giây hoặc đã đủ 4 lần | Bình thường, đổi trong file cấu hình nếu cần |
| Banner không hiện | Scene không có trong Build Settings, hoặc nằm trong danh sách loại trừ | Kiểm tra hai chỗ đó |
| Build Production bị từ chối | Còn ID demo của Google, hoặc thiếu ID, thiếu SDK key của MAX | Điền ID thật trong mục `Production` (hoặc `Max Production`) |
| Build báo provider đã chọn nhưng chưa cài SDK đúng bản | SDK chưa cài, sai bản, hoặc còn thư mục cũ trong `Assets` | Mở trang SDK, xóa thư mục cũ bằng tay nếu Hub liệt kê, rồi bấm **Setup** hoặc **Fix**. Game không cần quảng cáo thì chọn **No Ads** hoặc `providerId` là `none` |
| Game cũ không có quảng cáo bỗng build ra AdMob | Bản 0.3.x dùng "không có define `ADS`" làm No Ads, nay đó là Production | Chọn **No Ads** (`ADS_DISABLED`) hoặc đặt `providerId` là `none` |
| Hub không cho Setup SDK | Còn thư mục cũ như `Assets/MaxSdk`, `Assets/Firebase` | Xóa bằng tay các thư mục Hub liệt kê, cả file `.meta` |
| Console báo lỗi Firebase trong Editor khi dùng Remote Config | Thiếu `google-services-desktop.json` cho Editor | Bỏ qua được: game vẫn chạy bằng giá trị mặc định |
| Lỗi trùng assembly khi cài | Project còn bản copy cũ của Ads, Analytics hoặc tool | Xóa bản cũ, xem [Migration.md](Migration.md) |
| Báo lỗi tên save đã mở | Tạo hai `SaveFile` trùng tên | Chỉ mở trong `GameData`. Nếu tạo trong scene thì phải `Dispose` khi rời scene |
| `DataLost` sau khi cập nhật game | Key bị đổi | Trả lại key cũ, save tự khôi phục |
| Dữ liệu không được lưu | Sửa thẳng `Value` thay vì dùng `Update` | Đổi sang `Update` |
| Lỗi khi load scene | Scene chưa có trong Build Settings | Thêm vào Build Settings |
| Không thấy event trên Firebase | Chưa cài Firebase Analytics, thiếu `google-services.json`, hoặc báo cáo chưa kịp cập nhật | Cài ở trang SDK (trang Analytics phải hiện `Backend: Firebase`), xem DebugView |
| Trang Update báo `Could not check for updates` | Mất mạng, máy chưa có Git, hoặc không có quyền đọc repo | Kiểm tra mạng và lệnh `git` trong terminal |

## 14. Giới hạn cần biết

- **Đổi hoặc mất key là mất save.** Lỡ phát hành nhầm key thì trả lại key cũ sẽ khôi phục được, nhưng mất hẳn key thì không.
- **Mã hóa chỉ chặn việc sửa file thông thường.** Người có kinh nghiệm vẫn lấy được key từ bản build. IAP và bảng xếp hạng thật vẫn cần server xác thực.
- **Save không lưu trực tiếp `Dictionary`.** Dùng `List` của một class `[Serializable]`. Class dữ liệu không được là `MonoBehaviour` hay `ScriptableObject`.
- **AdMob chỉ chạy trên Android.** MAX chạy trên Android và iOS. MREC chỉ có với MAX. Save nhắm tới Android và iOS, WebGL chưa làm, chưa có cloud save.
- **Quảng cáo mạng thật và chạy trên điện thoại chưa được kiểm tra đầy đủ trong bản này.** Hãy test trên máy thật trước khi phát hành, theo [DeviceChecklist-0.4.0.md](DeviceChecklist-0.4.0.md). Xem thêm [Verification.md](Verification.md).

## 15. Đa ngôn ngữ

Mỗi ngôn ngữ là một file trong `Assets/Resources`: `Localization.txt` cho tiếng Anh và `Localization-<Ngôn ngữ>.txt` cho ngôn ngữ khác. Game tự giữ các file này.

### Bước 1: Tạo Google Sheet

| KEY_lang | English | Vietnamese |
|---|---|---|
| s_home | HOME | TRANG CHỦ |
| s_coin_value | COIN: {0} | XU: {0} |

Dòng đầu là tên cột, cột `English` là bắt buộc, thêm ngôn ngữ chỉ cần thêm cột. Mẫu Sheet cũ của studio vẫn dùng được: ô đầu của dòng 1 để trống, ô thứ hai để trống hoặc ghi `English`. Ô để trống sẽ lấy tiếng Anh.

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

## 16. SDK tùy chọn

Package không chứa SDK của bên thứ ba. Mỗi bản package ghim một bộ phiên bản SDK, và trang **SDK** cài đúng bộ đó chỉ bằng một nút. Game cần SDK nào thì cài SDK đó. Không cài SDK nào thì game vẫn compile và chạy: quảng cáo báo không có provider, analytics in ra Console, Remote Config trả về giá trị mặc định.

### Bộ phiên bản đang ghim

| SDK | Phiên bản |
|---|---|
| External Dependency Manager | 1.2.186 |
| Firebase (app, analytics, remote-config, crashlytics, messaging) | 13.1.0 |
| AppLovin MAX | 8.5.1 (cần `com.unity.ugui` 2.0.0, nút Setup tự thêm) |
| AppsFlyer | 6.17.900 |
| Google Mobile Ads (AdMob) | 11.0.0 |

Crashlytics và Messaging chỉ được cài đúng bản, package chưa có code riêng cho hai SDK này.

### Cách dùng trang SDK

Mở **RIPPLE Game > Open Hub > SDK**. Mỗi SDK có một dòng với một trong bốn trạng thái:

| Trạng thái | Ý nghĩa | Nút |
|---|---|---|
| Not installed | Chưa cài | **Setup** |
| Installed | Đúng bản | **Remove** |
| Wrong version | Sai bản, dòng chữ hiện bản đang cài và bản yêu cầu | **Fix** |
| Old copy found in Assets | Còn bản cài kiểu cũ trong `Assets` | không có nút |

- **Setup** tải file về `Packages/RippleSdks/`, kiểm tra SHA-256, ghi vào `Packages/manifest.json`, rồi tự cài các SDK phụ thuộc còn thiếu. SHA-256 sai thì file tải về bị xóa và manifest không bị đụng tới.
- **Remove** gỡ khỏi manifest và xóa file. SDK nào còn được SDK khác cần thì không bị gỡ.
- AppLovin MAX được tải từ kho chính thức của AppLovin vì giấy phép của họ không cho phát lại. Các SDK còn lại tải từ bản phát hành của repo `ripple-game-sdks`. Mọi file đều được kiểm tra SHA-256.
- **Bản cài kiểu cũ phải xóa bằng tay:** `Assets/MaxSdk`, `Assets/Firebase`, `Assets/ExternalDependencyManager`, `Assets/AppsFlyer`, `Assets/GoogleMobileAds` (cả file `.meta`). Hub chỉ liệt kê đường dẫn và từ chối Setup khi chúng còn tồn tại.

### Nếu cài sai bản

Phần code của SDK đó sẽ không compile, trang SDK báo lệch, và build bị chặn kèm lời nhắc mở trang SDK bấm **Fix**.

## 17. Remote Config

Remote Config đọc giá trị từ Firebase, nhờ đó đổi luật quảng cáo hoặc số liệu game không cần build lại. Cần cài **Firebase Remote Config** ở trang SDK. Chưa cài thì mọi hàm đọc trả về giá trị mặc định và báo sẵn sàng ngay.

### Tạo file cài đặt (không bắt buộc)

Chọn **Assets > Create > RIPPLE Game > REMOTE CONFIG > Remote Config Settings**. File **phải nằm đúng** ở `Assets/Resources/RippleGameRemoteConfig/RemoteConfigSettings.asset`, nếu để chỗ khác package không tìm thấy.

| Ô | Mặc định | Ý nghĩa |
|---|---|---|
| Fetch Timeout Seconds | 5 | Chờ fetch tối đa bao lâu, quá thì chạy tiếp bằng giá trị đã lưu hoặc mặc định |
| Minimum Fetch Interval Seconds | 3600 | Thời gian giữ cache. Build Ads Test luôn dùng 0 |
| Defaults | trống | Danh sách key và giá trị dùng khi server chưa có key đó |

### Đọc giá trị

```csharp
using RippleGame.RemoteConfig;

int lives = RemoteConfig.Get<int>("start_lives", 3);
bool saleOn = RemoteConfig.Get<bool>("sale_on", false);
var tuning = RemoteConfig.GetJson<MyTuning>("tuning", new MyTuning());

RemoteConfig.Activated += () => Debug.Log("Đã có giá trị mới");
```

`Get` đọc được int, long, float, double, bool và string. Đọc sai kiểu thì trả về giá trị mặc định và log một cảnh báo cho mỗi key. Fetch lỗi hoặc quá giờ thì `RemoteConfig.IsReady` vẫn lên `true`, dùng giá trị đã lưu lần trước hoặc mặc định. Remote Config cũng chạy trong Editor. Thiếu `google-services-desktop.json` thì Console báo một lỗi Firebase, nhưng game vẫn chạy bằng giá trị mặc định.

### Ba key điều khiển quảng cáo

| Key | Kiểu | Mặc định | Tác dụng |
|---|---|---|---|
| `ads_enabled` | bool | true | `false`: không hiện quảng cáo nào, rewarded cho thưởng ngay và trả `Success`. Có hiệu lực ngay |
| `ads_policy` | JSON | không có | Ghi đè luật hiện quảng cáo. Chỉ trường nào có mặt mới đổi, trường lạ bị bỏ qua |
| `ads_unit_ids` | JSON | không có | Ghi đè ID quảng cáo theo provider và nền tảng |

Ví dụ `ads_policy` (trường nào cũng không bắt buộc):

```json
{
  "interstitial": { "cooldown_seconds": 60, "max_per_session": 0, "min_level": 3, "start_session": 2,
                    "placements": { "replay": false, "pause": false } },
  "app_open": { "enabled": true, "cooldown_seconds": 30, "min_background_seconds": 5 },
  "rewarded_fallback": { "enabled": true },
  "organic": { "banner": true, "mrec": true, "interstitial": false, "app_open": true },
  "banner": { "refresh_seconds": 15 },
  "mrec": { "refresh_seconds": 15 }
}
```

- Trường số nguyên (`max_per_session`, `min_level`, `start_session`) chỉ nhận số nguyên, ghi `15.0` thì bị bỏ qua. Trường số thực (`cooldown_seconds`, `min_background_seconds`, `refresh_seconds`) nhận số nguyên hoặc số thập phân. Trường sai kiểu bị bỏ qua.
- Tên vị trí là tên `AdPlacement` viết kiểu snake_case: `default`, `home`, `level_start`, `level_complete`, `retry`, `double_reward`, `extra_life`, `pause`, `replay`, `rewarded_fallback`.
- **Khi nào có hiệu lực:** luật được chốt một lần mỗi phiên, lúc có quyết định quảng cáo đầu tiên. `ads_policy` về muộn hơn lúc đó thì áp từ phiên sau. Bản hợp lệ gần nhất được lưu trên máy và dùng ngay từ đầu phiên kế tiếp.
- **Key trống hoặc thiếu:** `ads_policy` thiếu hoặc để trống thì giữ nguyên bản đã lưu. Muốn xóa bản đã lưu, hãy đặt giá trị là `{}`.

`ads_unit_ids` ví dụ (provider là `max` hoặc `admob`, nền tảng `android` hoặc `ios`, loại quảng cáo `banner`, `interstitial`, `rewarded`, `app_open`, `mrec`):

```json
{ "max": { "android": { "interstitial": "id-cua-ban" } }, "admob": { "android": { "banner": "id-cua-ban" } } }
```

Chuỗi rỗng bị bỏ qua. ID được chốt lúc provider khởi tạo. Ads chờ Remote Config tối đa bằng `Fetch Timeout Seconds` rồi mới khởi tạo, nên ID về muộn hơn sẽ áp từ phiên sau.

Key riêng của game thì game tự đọc bằng `RemoteConfig.Get`.

## 18. Attribution và AppsFlyer

Attribution cho biết người chơi là organic (tự tìm đến) hay đến từ quảng cáo, và báo doanh thu quảng cáo cho AppsFlyer.

1. Mở trang SDK và bấm **Setup** ở dòng **AppsFlyer**.
2. Chọn **Assets > Create > RIPPLE Game > ATTRIBUTION > Attribution Settings**. File **phải nằm đúng** ở `Assets/Resources/RippleGameAttribution/AttributionSettings.asset`.
3. Điền các ô:

| Ô | Mặc định | Ý nghĩa |
|---|---|---|
| Dev Key | trống | Dev key của AppsFlyer. Để trống thì attribution tắt |
| Ios App Id | trống | App id trên App Store, chỉ gồm số. Cần cho iOS |
| Debug Log | tắt | Bật log gỡ lỗi của AppsFlyer. Tắt khi phát hành |
| Log Ad Revenue | bật | Gửi doanh thu của từng lần hiện quảng cáo cho AppsFlyer |

Dev key để trong file của game, không bao giờ để trong package.

```csharp
using RippleGame.Attribution;

if (Attribution.Status == AttributionStatus.Organic) { /* ... */ }
Attribution.StatusChanged += status => Debug.Log(status);
```

- `Attribution.Status` là `Unknown`, `Organic` hoặc `NonOrganic`, kèm `MediaSource` và `Campaign`. Lần đầu biết thì trạng thái được lưu lại và đọc lại ở các lần mở sau.
- AppsFlyer chỉ chạy trên máy thật, Editor không khởi động nó nên trạng thái luôn là `Unknown`.
- Khi có cả Ads, doanh thu mỗi lần hiện quảng cáo tự được gửi cho AppsFlyer (MAX và AdMob đều có), và trạng thái organic được đưa vào luật `organic` của quảng cáo. `Unknown` được coi là chưa organic, nên quảng cáo vẫn hiện cho tới khi AppsFlyer báo người đó organic.

## 19. Nâng lên 0.4.0 từ 0.3.x

Bản 0.4.0 bỏ define `ADS` và `FIREBASE`, và SDK phải cài qua trang SDK. Commit hoặc backup project trước, rồi làm lần lượt:

1. **Xử lý define.** Vào Player Settings > Scripting Define Symbols, xóa `ADS` và `FIREBASE` ở mọi nền tảng, hoặc bấm một chế độ ở trang ADS (chế độ nào cũng tự bỏ `ADS`).
   - **Game không quảng cáo (No Ads cũ):** ở 0.3.x "không có define `ADS`" nghĩa là No Ads. Ở 0.4.0 "không có define" nghĩa là **Production**, và file cấu hình cũ có `providerId` là `admob`, nên game sẽ build ra AdMob. Hãy bấm **No Ads** (đặt `ADS_DISABLED`) hoặc đặt `providerId` là `none`.
   - **Build iOS hoặc nền tảng khác của game dùng AdMob:** nút No Ads chỉ ghi define cho Android, nên tự thêm `ADS_DISABLED` cho nền tảng đó nếu không muốn quảng cáo.
2. **Xóa SDK cũ trong `Assets`:** `Assets/GoogleMobileAds`, `Assets/Firebase`, `Assets/ExternalDependencyManager`, và nếu có thì `Assets/MaxSdk`, `Assets/AppsFlyer` (cả file `.meta`). Giữ lại `google-services.json` và `GoogleService-Info.plist`. Hub từ chối Setup khi các thư mục này còn.
3. **Cài SDK game cần:** mở **RIPPLE Game > Open Hub > SDK**, bấm **Setup** từng SDK (Google Mobile Ads, AppLovin MAX, Firebase Analytics, Firebase Remote Config, AppsFlyer). Đợi Unity compile xong.
4. **Đặt `providerId`** trong `AdsConfiguration`: `none`, `admob` hoặc `max`. Cấu hình cũ giữ nguyên `admob`. Với `max`, điền hai mục Max Test và Max Production. Trang ADS phải báo `Compiled`.
5. **Thêm `Ads.SetPolicyContext`** vào code game như [mục 6](#báo-trạng-thái-game-cho-ads).
6. Nếu game có class tự viết implement `IAdsService`, thêm hai hàm `ShowMrec(MrecPosition)` và `HideMrec()`. Code `switch` trên `AdPlacement` cần xử lý thêm `Pause`, `Replay`, `RewardedFallback`.
7. Giá trị cũ về thời gian chờ interstitial và app open trong `AdsConfiguration` tự chuyển sang luật mới, không mất cấu hình.
8. Build Ads Test rồi chạy [danh sách kiểm tra trên máy](DeviceChecklist-0.4.0.md) trước khi phát hành.

Chi tiết và cách quay lại bản cũ nằm ở [Migration.md](Migration.md).
