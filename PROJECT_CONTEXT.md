# Tổng hợp yêu cầu dự án Single Line

> Trạng thái: Tài liệu tổng hợp đầu vào trước khi xác nhận phạm vi và lập kế hoạch sửa project.
>
> Nguyên tắc hiện tại: Chưa thay đổi gameplay, scene, prefab hoặc asset cho tới khi các điểm chưa rõ được xác nhận.

## 1. Nguồn yêu cầu

Các nguồn thông tin phải được đọc và áp dụng:

1. Game Design Document: [GDD_Single_Line_Block_Fill_Puzzle](https://docs.google.com/document/d/1KeBMtz0UFEpxM27px9UXXz_fJvcZxiSdkD1QHVdrBho/edit?tab=t.0).
2. Template của team: `U:/AS-Teams/AS-Teams-Template`.
3. Quy tắc AI/Unity trong `AGENTS.md` và `C:/Users/khanh/Downloads/message (1).txt`.
4. Tài liệu training Unity: `C:/Users/khanh/Downloads/Unity.pdf`.
5. Nội dung training của team về AudioController và UserProfileController.
6. Nội dung họp về prefab, animation, particle, level, event channel và lưu tiến trình.
7. Safe Area package: [Safe Area Helper - Asset Store package 130488](https://assetstore.unity.com/packages/package/130488).

Thứ tự áp dụng khi triển khai:

1. GDD quyết định hành vi và phạm vi sản phẩm.
2. Template `_SDK` quyết định flow và cách tích hợp các service có sẵn.
3. `AGENTS.md` quyết định tiêu chuẩn kiến trúc và code.
4. Training của team quyết định cách gọi các API nội bộ như audio và save data.
5. Tài liệu Unity cung cấp pattern tham khảo; code ví dụ nhập môn phải được điều chỉnh nếu xung đột với các quy tắc trên.

## 2. Phạm vi công việc

Phạm vi của người thực hiện là **Core Gameplay**. UI do thành viên khác phụ trách.

Phạm vi Core Gameplay đang được hiểu gồm:

- Board/grid model và trạng thái cell.
- Luật di chuyển và kiểm tra move hợp lệ.
- Path session, backtrack, undo và restart.
- Win, stuck, resolving và state machine gameplay.
- Level data, repository, validator, solver và hint logic.
- Gameplay input abstraction, touch tracking và nội suy khi người chơi vuốt nhanh.
- Pooling cho cell, path segment và gameplay effect thuộc board.
- Gameplay presentation của cell/path/board nếu được xác nhận thuộc phạm vi.
- Event và interface để UI, audio và hệ thống khác đăng ký nhận trạng thái.
- Unit test cho luật chơi, solver, validator và dữ liệu level.

Các phần không tự triển khai hoặc thay đổi thiết kế nếu chưa được giao:

- Home, Level Select, Settings và Shop.
- Gameplay HUD và popup Win/Lose.
- Navigation UI, font, button và icon UI.
- Logic hiển thị coin/hint và các màn hình meta.

Core Gameplay không thao tác trực tiếp UI. UI nhận dữ liệu qua interface hoặc event channel.

## 3. Quy tắc Git và task

### 3.1 Commit

Format:

```text
type(scope): mô tả ngắn gọn
```

Các type được chấp nhận:

- `feat`: tính năng mới.
- `fix`: sửa bug.
- `refactor`: cải thiện code, không thêm feature và không sửa bug.
- `docs`: thêm hoặc sửa documentation.
- `test`: thêm hoặc sửa test.
- `chore`: setup, config hoặc dependency.
- `perf`: cải thiện performance.
- `style`: format code, không đổi logic.

Ví dụ:

```text
feat: add player double jump
feat(combat): implement combo attack system
fix(ui): health bar not updating on damage
refactor: split PlayerController into components
perf: replace Instantiate/Destroy with object pool
docs: add README setup instructions
```

### 3.2 Task planning

Task của team cần có:

- Tên nhiệm vụ.
- Trạng thái.
- Người được giao.
- Hạn chót.
- Mức độ ưu tiên.

Chỉ lập task plan sau khi các yêu cầu chưa rõ được xác nhận.

## 4. Template `_SDK`

Template local:

```text
U:/AS-Teams/AS-Teams-Template
```

Thông tin đã kiểm tra:

- Branch: `main`.
- Commit: `cd0839cfa9c119adad9bada649714dd98190cdd9`.
- Worktree template sạch tại thời điểm kiểm tra.
- Flow mặc định của template: `Loading -> Gameplay`.
- `_SDK/Runtime` chứa phần dùng lại.
- `_SDK/Integrations` chứa SDK bên thứ ba.
- `_SDK/Template` chứa gameplay service, UI, config và scene mẫu.
- `MANAGERS.prefab` là prefab quan trọng của flow hệ thống.
- Template đã cung cấp win/lose, popup, save, level storage, audio và các controller chung.
- Game cụ thể chủ yếu triển khai gameplay và kết nối vào flow có sẵn.

Nguyên tắc tích hợp:

- Tận dụng service có sẵn trước khi viết hệ thống mới.
- Không sửa `_SDK` nếu có thể mở rộng bằng adapter, interface hoặc code trong `_Game`.
- SDK bên thứ ba phải được bọc qua interface khi Core Gameplay cần sử dụng.
- Config mặc định phải nằm trong config/ScriptableObject, không hard-code.

## 5. Trạng thái project đã quan sát

- Branch hiện tại khi kiểm tra: `Vee`.
- Scene gameplay cần làm việc là `SingleLine`, không phải scene `Loading`.
- Build Settings hiện có flow `Splash -> Home -> SingleLine`.
- `LoadingController` của template vẫn gọi scene `Gameplay`, tạo lỗi vì scene này không có trong Build Settings.
- `SingleLine` có 6 root object và 72 object tại thời điểm kiểm tra.
- Không phát hiện missing script hoặc custom reference bị bỏ trống trong lần kiểm tra scene.
- Cell của SingleLine được tạo runtime từ prefab/pool nên không xuất hiện sẵn trong hierarchy edit mode.
- Scene `_SDK/Template/Scenes/Gameplay.unity` là scene template riêng, có 5 root object và hệ thống service của SDK.
- Scene template `Gameplay.unity` được tạo từ commit `a7b2394...` bởi `namdev26`; commit này nằm trong lịch sử chung của các branch đã kiểm tra.
- Project đang có nhiều thay đổi chưa commit của người dùng/team. Không được reset, ghi đè hoặc xóa các thay đổi này.

## 6. Game Design bắt buộc

### 6.1 Sản phẩm

- Casual logic puzzle, single-stroke path.
- Portrait 9:16, chơi một tay.
- Mỗi level mục tiêu kéo dài 30–120 giây.
- Core level phải chơi được offline.
- Không sao chép artwork, UI, âm thanh, level hoặc nhận diện của game tham chiếu.

### 6.2 Core loop

1. Hiển thị hình dạng board.
2. Người chơi chọn ô bắt đầu hợp lệ.
3. Kéo qua các ô liền kề.
4. Mỗi Active cell chỉ được đi qua một lần.
5. Khi mắc kẹt, người chơi có thể backtrack, undo, restart hoặc dùng hint.
6. Phủ kín toàn bộ Active cell để thắng.
7. Cấp reward/progress và chuyển sang level tiếp theo qua flow của template.

### 6.3 Luật di chuyển MVP

- Chỉ đi ngang/dọc, không đi chéo.
- Không đi lại ô đã visited, ngoại trừ backtrack về ô ngay trước.
- Path luôn liên tục từ start tới current cell.
- Nhấc tay không làm mất path.
- Chạm lại ô cuối để tiếp tục path.
- Vuốt qua nhiều ô trong một frame phải được nội suy và xử lý tuần tự.
- Input ở vùng không hợp lệ không reset path.
- Thắng khi `visitedCount == activeCellCount` và thỏa fixed Start/End nếu có.

### 6.4 Backtrack, undo, restart và stuck

- Kéo về previous cell để bỏ bước cuối; có thể backtrack liên tục.
- Undo bỏ một bước và miễn phí trong MVP.
- Restart không có popup xác nhận và không reload scene.
- Mục tiêu phản hồi restart nhỏ hơn 100 ms.
- Không có Game Over tự động.
- Khi hết neighbor hợp lệ nhưng chưa phủ đủ, chuyển sang `Stuck` và vẫn cho phép backtrack/undo/restart/hint.

### 6.5 State machine

- `Loading`
- `Ready`
- `Drawing`
- `Stuck`
- `Resolving`
- `Won`
- `Paused`
- `Error`

### 6.6 Input

- Cell hit radius mặc định: `0.55 * cellSize`.
- Drag threshold mặc định: 8 px tại reference 1080p, scale theo device.
- Chỉ pointer đầu tiên điều khiển board.
- Giữ touch ID trong toàn bộ drag.
- Khi mất focus: hủy pointer capture nhưng giữ path.
- Input phải đi qua abstraction; board logic không đọc input trực tiếp.

### 6.7 Board và responsive

- Board là grid `W x H`, gồm Active và Empty cell.
- MVP ưu tiên Normal cell; subtype khác nằm sau MVP trừ khi được chốt lại.
- Board fit trong safe content area và không che HUD/control.
- Board căn giữa theo bounding box của Active cells.
- Reference Canvas: 1080 x 1920, Match 0.5.
- Cần kiểm tra 720x1280, 1080x2400 và tablet 4:3.

### 6.8 Level data

- Không hard-code level trong scene.
- Authoring bằng ScriptableObject và export JSON/binary cho build.
- Level cần ID bất biến, version, width, height, active cells, difficulty và các field Start/End/solution khi cần.
- Validator phải kiểm tra ID, index, connectivity, Start/End và solution.
- Level không có solution không được release.
- Solver dùng DFS/backtracking với heuristic Warnsdorff và không block main thread quá 16 ms.

### 6.9 Performance

- 60 FPS trên thiết bị mid-range; fallback 30 FPS.
- Main thread gameplay dưới 16,6 ms p95.
- Board load local dưới 300 ms.
- Không để màn hình trống quá 500 ms khi transition.
- Mục tiêu bộ nhớ dưới 250 MB trên Android mid-range.
- Gameplay steady state xấp xỉ 0 B GC/frame.
- Không chạy solver liên tục khi idle.
- Không reload scene giữa các level; tái sử dụng board và visual pool.

### 6.10 Acceptance liên quan Core Gameplay

- 100% level release vượt qua batch validator và có solution.
- Không thể thắng khi chưa phủ đủ hoặc sai fixed end.
- Backtrack, undo và restart deterministic.
- Vuốt nhanh không bỏ ô.
- Restart tức thì.
- Không có GC spike đáng kể khi vẽ path.
- Prototype có tối thiểu 20 level hợp lệ và người mới hoàn thành được 10 level mà không cần giải thích bằng lời.

## 7. Quy tắc code bắt buộc

### 7.1 Performance

- Ưu tiên Inspector reference, prefab reference và `Initialize()`/DI.
- Tránh runtime lookup: `GetComponent`, `TryGetComponent`, `GetComponentInChildren`, `GetComponentInParent`, `Find` và các biến thể.
- Tuyệt đối không lookup trong Update, FixedUpdate, LateUpdate, loop hoặc coroutine lặp.
- Không dùng LINQ hoặc tạo allocation trong hot path.
- Không dùng `Destroy()` cho object sinh thường xuyên; sử dụng Object Pool.

### 7.2 Kiến trúc

- Tuân thủ SOLID, DRY và KISS.
- Tách View, Controller, Model, Service, Config, Factory và Pool theo trách nhiệm.
- Gameplay không thao tác trực tiếp UI.
- UI không chứa gameplay logic.
- Class cấp cao phụ thuộc interface.
- Gameplay logic thuần C# càng nhiều càng tốt để unit test không cần Unity.
- Không tạo God Object hoặc switch/if chain khó mở rộng.

### 7.3 Convention

- Một file một class; tên file trùng tên class.
- Interface, enum dùng chung và ScriptableObject đặt file riêng.
- Không khai báo data class bên trong file ScriptableObject.
- PascalCase cho type/method/property/event.
- camelCase cho private field và parameter.
- Boolean bắt đầu bằng `Is`, `Has`, `Can`, `Should` hoặc `Needs`.
- Method async kết thúc bằng `Async`.
- Comment bằng tiếng Anh và chỉ dùng cho logic khó hoặc quyết định đặc biệt.

## 8. ScriptableObject và Observer Pattern

### 8.1 ScriptableObject

ScriptableObject dùng cho:

- Game config và balance.
- Level data.
- Theme/item/reward config.
- Shared immutable data.
- Event channel khi cần tách publisher và subscriber.

Không dùng ScriptableObject để giữ runtime state của ván chơi hoặc chứa gameplay logic phức tạp.

### 8.2 Observer

- C# event dùng cho giao tiếp thuần code, nhanh và type-safe.
- ScriptableObject Event Channel dùng cho giao tiếp giữa các module, scene hoặc thành viên làm gameplay/UI.
- UnityEvent chỉ dùng khi designer cần cấu hình listener trong Inspector.
- Interface dùng khi consumer bắt buộc phải có một contract cụ thể.
- Subscribe trong `OnEnable` và unsubscribe trong `OnDisable`, hoặc quản lý lifecycle tương đương rõ ràng.
- Không để event tạo coupling ngược từ Core Gameplay sang UI.

Luồng mong muốn:

```text
Core Gameplay thay đổi state
    -> phát event có payload rõ ràng
    -> Gameplay Presentation / UI / Audio đăng ký
    -> từng subscriber tự cập nhật phần của mình
```

## 9. Prefab, animation và particle từ Asset_Resources

Thư mục chính:

```text
Assets/_Game/Asset_Resources
```

### 9.1 Prefab OneLineBlock

Có hai prefab:

- `GameObject/OneLineBlock.prefab`
- `GameObject/OneLineBlock 1.prefab`

`OneLineBlock.prefab` là bản đầy đủ đã quan sát thấy các child/component liên quan:

- `Inner`
- `Wall`
- `Fill`
- `Surface`
- `Shadow`
- `Line`
- `StartBlockDot`
- `EndBlockDot`
- `ObjCircleEnd`
- Các light/glow.
- Nhiều particle được nhúng trong prefab.
- Animator dùng `TutorialFillSquareAnimations.controller`.

`OneLineBlock 1.prefab` là bản nhẹ hơn và có nhiều SpriteRenderer chưa gán sprite; không tự chọn bản này làm prefab chính trước khi xác nhận.

### 9.2 Animation

Đã tìm thấy 48 AnimationClip và 27 Animator/Override Controller. Các asset có khả năng liên quan gameplay gồm:

- `StartIdle.anim`
- `StartScaleAnim.anim`
- `BlockDropingAnimation.anim`
- `BlockUnSelected.anim`
- `GlowWithHand.anim`
- `TutorialFillSquareSelection.anim`
- `TutorialFillSquareComplete.anim`
- `Block.controller`
- `TutorialFillSquareAnimations.controller`
- `BlockSplesh.overrideController`
- `LineSelectionPopUps.overrideController`

### 9.3 Feedback bắt buộc theo nội dung họp

- Start block có chấm tròn trắng ở giữa.
- Vòng tròn xám mờ tỏa liên tục quanh chấm trắng.
- Nếu idle khoảng 3 giây, start block rung nhẹ để nhắc người chơi.
- Khi nối hai block, block có cảm giác bị nhấn xuống rồi nảy lên.
- Line phải có màu khác với block.
- Particle phải chọn đúng loại theo đối tượng và sự kiện.
- Ưu tiên tái sử dụng animation, controller, override controller và particle trong Asset_Resources.
- Gameplay object/particle phát sinh thường xuyên phải dùng pool.

## 10. AudioController

Cách gọi chuẩn của team:

```csharp
AudioController.Instance.PlaySound(SoundName.UI_CollectGem);
```

Thông tin implementation hiện tại:

- Namespace: `ASTeams.Base`.
- Enum: `SoundName`.
- Config: `SoundAssetConfigs` ScriptableObject.
- Mỗi entry gồm `SoundName`, `AudioClip` và volume.
- Controller tạo trước 10 AudioSource để phát SFX.
- Cooldown mặc định theo SoundName là 0,1 giây.
- Config hoặc clip bị thiếu sẽ được bỏ qua.

Khi thêm sound:

1. Thêm enum trong `SoundName`.
2. Thêm entry trong `SoundAssetConfigs`.
3. Gán clip và volume.

Core C# không gọi trực tiếp Singleton. Core phát event như cell entered, backtracked, invalid, stuck hoặc won; Unity audio adapter/presenter nhận event rồi gọi `AudioController` theo API của team.

GDD yêu cầu feedback audio cho:

- Enter cell/step.
- Backtrack.
- Invalid move.
- Stuck/warning.
- Win/level complete.

## 11. Lưu dữ liệu người chơi

Không gọi `PlayerPrefs` trực tiếp từ gameplay. Dùng `UserProfileController` trong `_SDK`.

API chính:

```csharp
UserProfileController.Instance.GetParam<T>(key);
UserProfileController.Instance.SetParam(key, value);
```

Thông tin implementation:

- `UserData` có sẵn `coin`, `level`, win/lose streak, life, booster, reward và journey.
- Dữ liệu mở rộng nằm trong chuỗi JSON `UserData.data`.
- `SetParam` cập nhật JSON, gọi `SaveUser`, ghi `PlayerPrefs` và phát `OnUserChanged`.
- Coin dùng API có sẵn `AddCoin()` và `UseCoin()`.
- Level tổng dùng property `LEVEL`, giới hạn tối thiểu là 1.
- Storage bên dưới vẫn là `PlayerPrefs`, nhưng chỉ `_SDK` được thao tác trực tiếp.

Quy tắc sử dụng trong gameplay:

- Không gọi `GetParam`/`SetParam` theo từng bước kéo.
- Runtime session giữ dữ liệu trong memory.
- Persist tại checkpoint như level complete, reward, app pause hoặc thoát gameplay.
- Key gameplay đặt tập trung bằng constant, không rải magic string.
- Core làm việc qua interface như `IGameplayProgressRepository`; Unity adapter gọi `UserProfileController`.
- Reset data phải đặt level và currency về giá trị mặc định đã được team xác nhận.

## 12. Level source và LevelConfig hiện tại

SDK có asset tổng:

```text
Assets/_SDK/Template/Gameplay/Services/LevelService/SO/LevelConfigSO.asset
```

Tại thời điểm kiểm tra:

- `levels` đang rỗng.
- `LevelCount == 0`.
- `levelStartLoop == 1`.

Resource level nằm tại:

```text
Assets/_Game/Asset_Resources/Level Data/oneline
```

Số file quan sát được:

| Thư mục | Số file level |
|---|---:|
| `beginner` | 200 |
| `beginner 1` | 142 |
| `beginner222` | 100 |
| `medium` | 920 |
| `hard` | 200 |
| `mediumold` | 100 |
| `expertold` | 100 |
| `masterold` | 100 |

`PackInfo.json` mô tả bốn pack Beginner, Medium, Expert và Master, mỗi pack 100 level. Nội dung thư mục thực tế không khớp hoàn toàn với PackInfo, nên cần xác định nguồn level chính thức trước khi import hoặc sửa level hiện tại.

GDD yêu cầu MVP 300 level chia 10 chapter, trong khi resource và PackInfo thể hiện số lượng khác. Không tự chọn hoặc trộn level trước khi Producer xác nhận.

## 13. Safe Area

- Package Safe Area đã có dưới dạng source `Assets/_SDK/Runtime/Utilities/SafeArea.cs` với namespace `Crystal`.
- SingleLine và UI template đã có component `Crystal.SafeArea` ở một số container.
- UI do thành viên khác phụ trách, nhưng board gameplay vẫn phải fit trong usable/safe content area theo GDD.
- Cần phối hợp contract về vùng board khả dụng để tránh gameplay tự phụ thuộc hierarchy UI.

## 14. Các điểm cần xác nhận trước khi lập plan

Các câu hỏi này được giữ lại cho bước làm rõ sau khi toàn bộ thông tin đã được gửi xong:

1. Flow scene chính thức sẽ là `Splash -> Home -> SingleLine`, hay cần đưa về flow `Loading -> Gameplay` của template?
2. `SingleLine` sẽ giữ nguyên tên hay đổi thành `Gameplay`?
3. Phạm vi Core Gameplay có bao gồm presentation của board, animation, particle, audio và haptic không?
4. Prefab nào là nguồn chính: `OneLineBlock.prefab`, `OneLineBlock 1.prefab`, hay cần tạo prefab variant mới?
5. Bộ level nào trong Asset_Resources là canonical?
6. Số level mục tiêu của lần triển khai này là 20 prototype, 100+, 300 theo GDD, hay 400 theo PackInfo?
7. Có giữ pipeline JSON/chapter hiện tại hay chuyển sang `LevelConfigSO` tổng của SDK và export theo GDD?
8. GDD cho phép bắt đầu ở bất kỳ Active cell, còn nội dung họp mô tả start block riêng. Mọi level có fixed start hay chỉ một số level?
9. Khi mở lại game, chỉ khôi phục level hiện tại hay phải khôi phục cả path/board đang chơi dở?
10. Khi reset data, các giá trị mặc định cụ thể của level, coin, hint và tutorial là gì?
11. Danh sách ScriptableObject Event Channel và payload nào được thống nhất với người làm UI?
12. Ai sở hữu và tạo các Event Channel asset: gameplay hay UI?
13. Audio gameplay sẽ thêm nhóm `SoundName` riêng cho Single Line hay tái sử dụng các enum hiện có?
14. Mapping chính xác giữa particle/animation trong Asset_Resources và từng event gameplay là gì?
15. Các subtype Start, End, Locked, Bridge, Teleport và OneWay có thuộc milestone hiện tại không?
16. Những thay đổi Core Gameplay đang có trên branch `Vee` sẽ được giữ lại để refactor hay bỏ để làm lại từ nền template?
17. Task “endless map 9x9, 3x3x3” trong ảnh task cũ còn thuộc scope hay đã được thay bằng GDD level-based?

## 15. Điều kiện trước khi sửa project

Trước khi lập plan và chỉnh code cần hoàn thành:

1. Người phụ trách xác nhận đã gửi xong toàn bộ thông tin đầu vào.
2. Trả lời các câu hỏi còn chưa rõ ở mục 14.
3. Chốt phạm vi Core Gameplay và ranh giới tích hợp với UI.
4. Chốt scene flow, nguồn level, prefab và asset feedback chính thức.
5. Sau đó mới lập task plan theo format của team.

