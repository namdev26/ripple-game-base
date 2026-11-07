# Hướng dẫn dùng Save

Module `RippleGame.Save` lưu dữ liệu game vào một file mã hóa. Package không áp đặt cấu trúc dữ liệu nào, nên dùng được cho mọi game.

## Bắt đầu trong 3 bước

**1. Tạo key.** Mở **RIPPLE Game > Open Hub > Save**, chọn thư mục trong `Assets`, bấm **Create GameSaveKey.cs**. Commit file này cùng game.

**2. Khai báo dữ liệu.** Một class `[Serializable]` có constructor mặc định. Giá trị khởi tạo chính là giá trị mặc định khi chưa có save.

```csharp
[Serializable]
public sealed class PlayerData
{
    public int bestScore;
    public bool soundOn = true;
    public List<string> ownedSkins = new List<string>();
}
```

**3. Mở file một lần và dùng chung cả game.**

```csharp
using RippleGame.Save;

public static class GameData
{
    public static readonly SaveFile Save = Open();
    public static readonly SaveSection<PlayerData> Player = Save.Section<PlayerData>("player");

    private static SaveFile Open()
    {
        var save = new SaveFile("main", GameSaveKey.Value);
        save.ProblemRaised += problem => { /* Báo lên hệ thống log của game */ };
        return save;
    }
}
```

Đọc và ghi:

```csharp
var best = GameData.Player.Value.bestScore;

GameData.Player.Update(data => data.bestScore = 120);
```

Chỉ vậy là đủ. Dữ liệu được tự ghi xuống file, bạn không cần tự gọi lệnh lưu.

## Khi nào dữ liệu được ghi

| Thời điểm | Ghi chú |
|---|---|
| Tối đa 5 giây sau thay đổi | Đổi bằng `new SaveOptions { AutoFlushSeconds = 10 }`, đặt `0` để tắt |
| App xuống nền, mất focus, thoát | Tự động |
| Khi gọi `Save.Flush()` | Dùng sau việc quan trọng như mua hàng hoặc thắng level |

Nhiều lần `Update` liên tiếp chỉ gây một lần ghi. Nếu nội dung không đổi thì không ghi.

## Ví tiền và tiến trình level

Hai loại này cắm thẳng vào cùng một file, nên coin và level luôn được ghi cùng lúc:

```csharp
using RippleGame.Economy;
using RippleGame.Persistence;
using RippleGame.Progression;

var wallet   = new CurrencyWallet(GameData.Save.BalanceStore("coins"));
var progress = new LevelProgress(GameData.Save.LevelStore("levels"));

wallet.TryAdd(50);
if (wallet.TrySpend(10)) UnlockItem();

progress.MarkCompleted(3);
var canPlay = progress.IsUnlocked(4);
```

`LevelFlow` tự cộng thưởng, lưu tiến trình và gọi `Flush` khi thắng. Bạn chỉ cần truyền `Commit = () => GameData.Save.Flush()`.

## Đổi cấu trúc dữ liệu giữa các bản game

Thêm field mới thì không cần làm gì: save cũ chưa có field đó sẽ nhận giá trị khởi tạo.

Khi đổi tên, đổi kiểu hoặc đổi ý nghĩa một field, hãy tăng version và viết hàm chuyển đổi:

```csharp
var player = save.Section<PlayerData>("player", 2, (oldVersion, oldJson) =>
{
    if (oldVersion == 1) return oldJson.Replace("\"score\"", "\"bestScore\"");
    return oldJson;
});
```

Thiếu hàm chuyển đổi khi save cũ có version thấp hơn sẽ báo lỗi ngay lúc gọi `Section`, để bạn phát hiện khi phát triển.

## Lỗi xảy ra thì sao

Mọi lỗi đều được ghi log cảnh báo sẵn. Nếu muốn tự xử lý, hãy đăng ký `ProblemRaised` **trước** lần gọi `Section` đầu tiên, vì file được đọc vào lúc đó, như trong `GameData.Open()` ở trên.

| `SaveProblem` | Chuyện gì đã xảy ra | Game nên làm gì |
|---|---|---|
| `RecoveredFromBackup` | File chính hỏng, đã đọc từ bản dự phòng | Thường không cần làm gì. Có thể mất vài thay đổi gần nhất |
| `DataLost` | Không còn bản nào đọc được. Dữ liệu về mặc định, mọi file lỗi được giữ lại ở `.corrupt`, `.corrupt.1`... | Điều tra ngay. Hay gặp nhất là do đổi key: trả lại key đúng thì save tự khôi phục từ các bản giữ lại |
| `WriteFailed` | Không ghi được, thường do đầy bộ nhớ. Sẽ tự thử lại | Có thể báo người chơi |
| `NewerVersion` | Save được tạo bởi bản game mới hơn. Phiên này không ghi để bảo vệ dữ liệu | Nhắc người chơi cập nhật game |
| `ReadFailed` | Có file nhưng tạm thời không đọc được. File giữ nguyên, game chạy với dữ liệu mặc định, phiên này không ghi | Nhắc người chơi mở lại game |

## Xem nội dung save khi debug

Mở **RIPPLE Game > Open Hub > Save**, chọn file, dán key rồi bấm **Decrypt**. File nằm trong `Application.persistentDataPath`, tên `main.sav` nếu bạn đặt tên `main`.

Trong khi chơi trên Editor, muốn xóa save để chơi lại từ đầu thì xóa các file `main.*` trong thư mục đó.

## Chuyển dữ liệu cũ từ PlayerPrefs

Chép một lần, dùng một section làm dấu đã chép:

```csharp
[Serializable] public sealed class LegacyImport { public bool done; }

var import = save.Section<LegacyImport>("legacy_import");
if (!import.Value.done)
{
    wallet.TryAdd(PlayerPrefs.GetInt("old.coin.key", 0));
    import.Update(data => data.done = true);
    save.Flush();
}
```

Giữ lại key PlayerPrefs cũ để có thể quay về bản trước nếu cần.

## Giới hạn cần biết

- **Đổi hoặc mất key thì save cũ không đọc được.** Sau khi phát hành, key phải giữ cố định và lưu trong repo của game. Lỡ phát hành nhầm key thì trả lại key cũ sẽ khôi phục được từ các bản `.corrupt`, nhưng mất key là mất hẳn.
- **Mã hóa chỉ chặn việc sửa file thông thường.** Người có kinh nghiệm vẫn có thể lấy key từ bản build, sửa bộ nhớ khi chạy hoặc chép đè save cũ. IAP và bảng xếp hạng thật vẫn cần server xác thực.
- **`JsonUtility` không lưu trực tiếp `Dictionary`.** Dùng `List` của một class `[Serializable]` thay thế. Class dữ liệu không được là `MonoBehaviour` hay `ScriptableObject`.
- **Mở trùng tên báo lỗi.** Mỗi tên file chỉ có một `SaveFile` trong một phiên chạy, nên hãy giữ nó ở một nơi dùng chung cho cả game. Nếu tạo trong một scene thì phải `Dispose` khi scene đó bị hủy.
- **Nhắm tới Android và iOS.** WebGL cần xử lý ghi file riêng, hiện chưa làm.
- **Chưa có cloud save.** Có thể tự thêm bằng cách implement `ISaveStorage` và truyền qua `SaveOptions.Storage`.
