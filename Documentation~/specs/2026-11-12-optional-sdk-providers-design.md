# Thiết kế 0.4.0: SDK tùy chọn, AppLovin MAX, Remote Config, AppsFlyer

Ngày: 2026-11-12. Trạng thái: chờ duyệt.

## 1. Mục tiêu và phạm vi

ripple-game-base là base gốc duy nhất. Các tính năng quảng cáo, Remote Config và attribution đang có ở một base cũ được viết lại theo kiến trúc của ripple. Không giữ API của base cũ, không có lớp tương thích.

Trong bản 0.4.0, một lần phát hành:

- AppLovin MAX là provider quảng cáo tùy chọn, cùng tồn tại với AdMob. Mỗi game chọn `none`, `admob` hoặc `max`. Mặc định không có SDK nào được bật.
- Thêm định dạng MREC, luật hiện quảng cáo dùng chung, và tùy chọn dùng interstitial khi thiếu rewarded.
- Module Remote Config (Firebase) với key đặt tên mới.
- Module Attribution (AppsFlyer): ghi doanh thu quảng cáo và cung cấp trạng thái organic.
- Đồng bộ phiên bản: ripple ghim một bộ phiên bản SDK. TOOL Hub cài đúng bộ đó bằng một nút bấm. Sai bản thì phần code liên quan không compile và build bị chặn.

Không làm trong 0.4.0: IAP, âm thanh, rung, các tiện ích khác, MREC cho AdMob, hỏi quyền ATT trên iOS, ghi trạng thái organic thành user property trên Firebase, code riêng cho Crashlytics và Messaging (chỉ cài đúng bản).

Không đưa vào ripple: Odin Inspector, DOTween Pro (gói trả phí, không được phát lại), UniRx, UniTask.

## 2. Bộ phiên bản SDK và cách cài

### 2.1 Bộ phiên bản đầu tiên

| Gói UPM | Phiên bản |
| --- | --- |
| com.google.external-dependency-manager | 1.2.186 |
| com.google.firebase.app | 13.1.0 |
| com.google.firebase.analytics | 13.1.0 |
| com.google.firebase.remote-config | 13.1.0 |
| com.google.firebase.crashlytics | 13.1.0 |
| com.google.firebase.messaging | 13.1.0 |
| com.applovin.mediation.ads (AppLovin MAX) | 8.5.1 |
| appsflyer-unity-plugin (AppsFlyer) | 6.17.900 |
| com.google.ads.mobile (Google Mobile Ads) | 11.0.0. Lấy từ OpenUPM (`https://package.openupm.com`), là đường cài UPM mà hướng dẫn AdMob Unity của Google ghi. Đây là bản ổn định mới nhất có `package.json` phụ thuộc đúng EDM 1.2.186 và hỗ trợ Unity 2019.4 trở lên. Bản 11.1.0 đến 11.5.0 cần EDM 1.2.187, bản 11.6.0 chuyển sang gói `com.unity.external-dependency-manager` 2.1.0. |

Mọi SDK đều cài dưới dạng gói UPM (.tgz), không import `.unitypackage` vào `Assets`. Chỉ dùng một bản EDM cho tất cả SDK.

### 2.2 Repo `ripple-game-sdks` (public)

- Mỗi bộ phiên bản là một GitHub Release chứa các file .tgz, license gốc của từng SDK và `sdks.json` (tên gói, phiên bản, tên file, SHA-256).
- File lấy từ nguồn chính thức của từng hãng. Đối chiếu SHA-256 với bản studio đang dùng.
- Kiểm tra license của từng SDK trước khi đưa lên. SDK nào không cho phát lại thì không đưa lên repo. Mục của SDK đó trong catalog trỏ thẳng về link chính thức, vẫn ghim phiên bản và SHA-256.

### 2.3 Catalog trong ripple

`Editor/Sdk/SdkCatalog.json` đi kèm từng bản ripple. Mỗi mục gồm: id, tên gói UPM, phiên bản, URL tải, SHA-256, các id phụ thuộc. Ví dụ MAX phụ thuộc EDM, Remote Config phụ thuộc firebase.app và EDM.

### 2.4 Trang SDK trong TOOL Hub

Mỗi SDK có một dòng, hiện một trong các trạng thái sau:

- Chưa cài.
- Đúng bản.
- Sai bản: hiện cả bản đang cài và bản yêu cầu.
- Cài kiểu cũ: có thư mục `Assets/MaxSdk`, `Assets/Firebase`, `Assets/ExternalDependencyManager`, `Assets/AppsFlyer`...

Các nút trên mỗi dòng:

- **Setup**: tải file về `Packages/RippleSdks/`, kiểm tra SHA-256, ghi `"<gói>": "file:RippleSdks/<file>.tgz"` vào `Packages/manifest.json`, rồi tự Setup các gói phụ thuộc còn thiếu. Nếu SHA-256 sai thì xóa file vừa tải và không động vào manifest.
- **Fix**: đưa về đúng bản.
- **Remove**: gỡ mục khỏi manifest và xóa file .tgz. Không gỡ gói mà SDK khác còn phụ thuộc.

Hub không tự xóa bản cài kiểu cũ trong `Assets`. Hub chỉ liệt kê đường dẫn và hướng dẫn xử lý.

### 2.5 Ba lớp chặn lệch phiên bản

1. **Compile.** Mỗi assembly provider dùng `versionDefines` với khoảng đúng một bản, ví dụ `[8.5.1]`, và `defineConstraints` trên symbol đó. Thiếu gói hoặc sai bản thì assembly không được build.
2. **Build.** Validator chạy trước khi build. Nếu `providerId` chọn provider mà assembly của nó không được build, hoặc có SDK được bật mà sai bản, thì build dừng và báo rõ lý do.
3. **Repo.** Test và `Tools~/Check-Package.ps1` kiểm tra mọi `versionDefines` khớp `SdkCatalog.json`. Nhờ vậy khi nâng SDK, không thể sửa chỗ này mà quên chỗ kia.

## 3. Cấu trúc assembly

Lõi không tham chiếu SDK nào và luôn compile:

| Assembly | Vai trò |
| --- | --- |
| RippleGame.Ads.Runtime | API `Ads`, `AdsPolicy`, `NoAdsService`, registry provider |
| RippleGame.Analytics.Runtime | API `Analytics`, Console fallback |
| RippleGame.RemoteConfig (mới) | API `RemoteConfig`, giá trị mặc định |
| RippleGame.Attribution (mới) | API `Attribution`, trạng thái organic |

Provider chỉ build khi có đúng gói SDK. Mỗi provider tự đăng ký vào lõi bằng `RuntimeInitializeOnLoadMethod`:

| Assembly | Gói SDK yêu cầu |
| --- | --- |
| RippleGame.Ads.AdMob (tách từ code hiện tại) | com.google.ads.mobile |
| RippleGame.Ads.Max (mới) | com.applovin.mediation.ads |
| RippleGame.Firebase (mới, khởi tạo Firebase một lần) | com.google.firebase.app |
| RippleGame.Analytics.Firebase (tách từ code hiện tại) | com.google.firebase.analytics |
| RippleGame.RemoteConfig.Firebase (mới) | com.google.firebase.remote-config |
| RippleGame.Attribution.AppsFlyer (mới) | appsflyer-unity-plugin |

Bridge không cần SDK:

| Assembly | Vai trò |
| --- | --- |
| RippleGame.Analytics.AdsBridge (đã có) | Ghi event quảng cáo, nền tảng lấy từ `AdRevenue.Platform` |
| RippleGame.Ads.RemoteConfigBridge (mới) | Áp `ads_enabled`, `ads_policy`, `ads_unit_ids` |
| RippleGame.Ads.AttributionBridge (mới) | Gửi doanh thu sang Attribution, đưa trạng thái organic vào luật |

Phần Editor đang phụ thuộc SDK, gồm `AdMobSettingsSynchronizer` và các chỗ bật define, được tách sang assembly Editor có cùng `versionDefines`.

Bỏ hai define `ADS` và `FIREBASE`. Giữ `ADS_TEST` để chọn profile Test.

## 4. Ads

### 4.1 API thay đổi

- `AdFormat` thêm `Mrec`.
- `AdPlacement` thêm `Pause`, `Replay`, `RewardedFallback`.
- `IAdsService` và `Ads` thêm `ShowMrec(MrecPosition position)` và `HideMrec()`. `MrecPosition` gồm `TopCenter`, `Center`, `BottomCenter`.
- `AdRevenue` thêm `Platform` (`admob` hoặc `applovin_max`).
- Thêm `Ads.SetPolicyContext(AdsPolicyContext context)`. Ngữ cảnh gồm:
  - `CurrentLevel` (int);
  - `Entitlement`: `None`, `RemoveAds` (chặn banner, MREC, interstitial, mở app) hoặc `RemoveAdsAndFreeRewards` (thêm rewarded cho thưởng ngay, không hiện quảng cáo);
  - `HasUserConsent` (bool?, null nghĩa là chưa hỏi).

### 4.2 Luật quảng cáo (`AdsPolicy`)

Các luật đang nằm trong `AdMobAdsService` chuyển vào lõi. `Ads` gọi `AdsPolicy` trước rồi mới gọi provider. Provider chỉ load và show.

Cấu hình luật nằm trong `AdsConfiguration`:

- **Interstitial:** `cooldownSeconds` (45), `maxPerSession` (4, 0 là không giới hạn), `minLevel` (0 là tắt), `startSession` (1), bật/tắt theo từng `AdPlacement`.
- **Quảng cáo mở app:** `enabled`, `cooldownSeconds` (20), `minBackgroundSeconds` (5).
- **Rewarded fallback:** `enabled` (mặc định false).
- **Organic:** cho phép banner, MREC, interstitial, mở app với người dùng organic (mặc định true cả bốn).
- **Banner và MREC:** `refreshSeconds` (0 là để SDK tự refresh).
- **Toàn màn hình:** `pauseTimeAndAudio` (mặc định true). Đặt `Time.timeScale = 0` và `AudioListener.pause = true` khi quảng cáo hiện, trả lại giá trị cũ khi đóng.

Quy tắc áp dụng:

- Số phiên được tăng một lần mỗi lần mở app và lưu trong PlayerPrefs.
- Giá trị luật được chốt một lần mỗi phiên, lúc lần đầu cần dùng. Remote Config về muộn hơn lúc đó thì áp từ phiên sau.
- Nếu game không gọi `SetPolicyContext` thì bỏ qua `minLevel` và log cảnh báo một lần.
- `AdsConfiguration` cũ được tự chuyển sang cấu trúc mới, gồm `interstitialCooldownSeconds`, `interstitialMaxImpressionsPerSession`, `appOpenCooldownSeconds`, `appOpenMinimumBackgroundSeconds`. Các giá trị này được đọc qua trường ẩn đã serialize trong `ISerializationCallbackReceiver` rồi chép sang luật mới, nên game không mất cấu hình.

### 4.3 Rewarded fallback

Khi `rewardedFallback.enabled` bật, rewarded chưa sẵn sàng, interstitial đã sẵn sàng, organic cho phép interstitial và vị trí `RewardedFallback` đang bật thì hiện interstitial.

- Inter hiện kiểu fallback bỏ qua `cooldownSeconds` và `minLevel`.
- Đóng inter bình thường: gọi callback thưởng, trả `AdsResult.Success`, phát event `RewardEarned` có message `fallback_interstitial`, rồi đặt lại cooldown.
- Inter hiện lỗi: không thưởng, trả `AdsResult.Failed`.
- Thưởng không bao giờ được để lại cho lần hiện quảng cáo sau.

### 4.4 Provider MAX

- Cấu hình MAX trong `AdsConfiguration` có profile Test và Production. Mỗi profile gồm SDK key và ID cho banner, interstitial, rewarded, mở app, MREC, riêng cho Android và iOS. Không có ID nào trong code.
- Nếu `HasUserConsent` có giá trị thì truyền vào SDK trước khi khởi tạo.
- Load lại có backoff khi lỗi, dùng các thông số retry đang có. Khi có mạng trở lại thì load lại các định dạng còn thiếu.
- Doanh thu đổi từ USD (double) sang micros (long), `Platform = applovin_max`, `AdSourceName` lấy từ tên mạng quảng cáo.

### 4.5 Chọn provider

`providerId` nhận một trong `none`, `admob`, `max`. Nếu provider đã chọn không có trong registry: Editor log lỗi và dùng `NoAdsService`; build bị dừng như mục 2.5.

## 5. Firebase, Remote Config, Attribution

### 5.1 Khởi tạo Firebase

`RippleGame.Firebase` gọi `FirebaseApp.CheckAndFixDependenciesAsync()` đúng một lần và cung cấp một Task sẵn sàng. Analytics.Firebase và RemoteConfig.Firebase cùng chờ Task này. `AnalyticsBootstrap` không còn `#if FIREBASE`. Analytics vẫn dùng Console trong Editor như hiện tại.

### 5.2 Remote Config

API:

- `RemoteConfig.IsReady`, event `Activated`;
- `Get<T>(key, defaultValue)` cho int, long, float, double, bool, string;
- `GetJson<T>(key, defaultValue)`.

`RemoteConfigSettings` (asset trong project game) gồm `fetchTimeoutSeconds` (5), `minimumFetchIntervalSeconds` (3600; profile Test dùng 0) và danh sách giá trị mặc định dạng key/value.

Hành vi:

- Fetch xong thì kích hoạt và báo sẵn sàng.
- Lỗi hoặc quá `fetchTimeoutSeconds`: dùng giá trị đã kích hoạt lần trước, nếu không có thì dùng mặc định, và vẫn báo sẵn sàng.
- Khi có mạng trở lại mà chưa từng fetch thành công thì fetch lại.
- Không có SDK: chỉ trả giá trị mặc định và báo sẵn sàng ngay.
- Đọc sai kiểu: trả mặc định và log cảnh báo một lần cho mỗi key.
- Ads chờ Remote Config sẵn sàng tối đa `fetchTimeoutSeconds` rồi mới khởi tạo provider.

Đọc JSON bằng Newtonsoft. Thêm dependency `com.unity.nuget.newtonsoft-json` 3.2.2 vào `package.json`.

### 5.3 Key quảng cáo trên Firebase

- `ads_enabled` (bool, mặc định true). Khi false: không hiện quảng cáo nào, còn rewarded gọi callback thưởng ngay và trả `Success`.
- `ads_policy` (JSON). Chỉ trường nào có mặt mới ghi đè cấu hình. Trường lạ được bỏ qua. Ví dụ:

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

  Tên vị trí là tên `AdPlacement` viết dạng snake_case.
- `ads_unit_ids` (JSON). Ghi đè ID theo provider và nền tảng, ví dụ `{"max":{"android":{"interstitial":"…"}},"admob":{"android":{"banner":"…"}}}`. Chuỗi rỗng thì bỏ qua. ID được chốt khi provider khởi tạo.

Key riêng của game do game tự đọc qua `RemoteConfig.Get`.

### 5.4 Attribution và AppsFlyer

Lõi:

- `Attribution.Status`: `Unknown`, `Organic` hoặc `NonOrganic`. Kèm `MediaSource`, `Campaign` và event `StatusChanged`.
- Trạng thái lưu vào PlayerPrefs ngay lần đầu biết, và lần mở sau đọc lại từ đó.

`AttributionSettings` (asset trong project game) gồm dev key, iOS app id, `debugLog` và `logAdRevenue` (mặc định true).

Provider AppsFlyer:

- Init, start, nhận conversion data rồi đặt trạng thái.
- Ghi doanh thu bằng `logAdRevenue`, mạng mediation chọn theo `AdRevenue.Platform`.

`RippleGame.Ads.AttributionBridge`:

- Gửi sự kiện `Paid` của Ads sang Attribution.
- Đưa `Attribution.Status` vào luật organic, trong đó `Unknown` được coi là không organic.

## 6. Kiểm thử

### 6.1 Tự động

- **EditMode, `AdsPolicy`** với provider giả: cooldown, giới hạn mỗi phiên, `minLevel`, `startSession`, theo vị trí, organic, hai loại `Entitlement`, `ads_enabled=false`. Fallback gồm ba trường hợp: thành công có thưởng, inter lỗi không thưởng, bị organic chặn. Thêm trường hợp chốt luật theo phiên.
- **EditMode, `AdsConfiguration`:** chuyển đổi đúng giá trị từ cấu hình cũ.
- **EditMode, Remote Config:** từng kiểu dữ liệu, sai kiểu, JSON chỉ có một phần trường, trường lạ, quá giờ fetch, không có SDK.
- **EditMode, SDK:** `versionDefines` khớp catalog, ghi và gỡ mục manifest, sai SHA-256, phát hiện bản cài kiểu cũ, phụ thuộc giữa các SDK, validator trước khi build.
- **PlayMode:** `Ads` với provider giả qua nhiều scene, dừng và trả lại thời gian và âm thanh, Remote Config quá giờ không chặn khởi tạo ads.
- Toàn bộ test hiện có vẫn đạt. Chỗ nào đổi API thì sửa test tương ứng.
- **`Check-Package.ps1`:** thêm kiểm tra `versionDefines` khớp catalog, và chặn chuỗi có dạng SDK key MAX hoặc dev key AppsFlyer.

### 6.2 Trong Unity

Kết quả ghi vào `Documentation~/Verification.md`:

- Project Unity 6 trống, cài ripple từ thư mục local. Các tổ hợp cần thử:
  - không SDK;
  - chỉ AdMob;
  - chỉ MAX;
  - MAX + AppsFlyer + Remote Config + Firebase Analytics.

  Cả bốn tổ hợp đều không được có lỗi compile.
- Cố tình cài sai bản một SDK: assembly đó tắt, Hub báo lệch, build dừng.
- Android trên thiết bị: người dùng chạy theo checklist, với SDK key và ad unit MAX thật cùng thiết bị test. Bước nào không chạy thì ghi "chưa chạy".

## 7. Phát hành

- `package.json` lên 0.4.0.
- CHANGELOG ghi các thay đổi phá vỡ:
  - bỏ define `ADS` và `FIREBASE`;
  - AdMob và Firebase Analytics cần cài SDK qua Hub;
  - `IAdsService` có thêm MREC;
  - `AdRevenue` có thêm `Platform`;
  - `AdPlacement` có thêm giá trị mới.
- Cập nhật `Migration.md`, `HuongDan.md`, `ads/Setup.md`, `analytics/Setup.md`.
- Tạo repo `ripple-game-sdks`, đẩy release, push nhánh và tạo tag `v0.4.0`. Mỗi bước này đều hỏi người dùng trước khi làm.

## 8. Thư mục mã nguồn cũ

Thư mục mã nguồn cũ đang nằm trong thư mục gốc của package. Khi cài package từ thư mục local, Unity sẽ compile cả các script trong đó và báo lỗi. Thư mục này phải được chuyển ra ngoài repo trước khi bắt đầu viết code, chỉ dùng làm tài liệu tham khảo, và xóa sau khi 0.4.0 đã được kiểm tra. Cả việc chuyển lẫn việc xóa đều cần người dùng đồng ý.
