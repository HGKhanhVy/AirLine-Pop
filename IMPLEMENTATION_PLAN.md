# Kế hoạch hoàn thiện Core Gameplay và APK

> Deadline: 12/09/2026  
> Nhánh thực hiện: `Vee`  
> Phạm vi chính: Core Gameplay và lớp tích hợp gameplay với `_SDK`  
> Nguồn chuẩn: GDD, `Asset_Resources`, scene/template `_SDK`, `AGENTS.md`

## Trạng thái đầu vào

- Core path đã có kéo liên tục, backtrack về ô trước, undo, restart, stuck, hint và phản hồi rung cơ bản.
- Dữ liệu hiện có đủ 300 level: 10 chapter, mỗi chapter 30 level.
- Build Settings hiện vẫn là `Splash -> Home -> SingleLine`.
- `LoadingController` của `_SDK` chuyển tới scene tên `Gameplay`.
- Gameplay hiện tự chuyển level sau khi thắng nhưng chưa đọc/lưu level bằng `UserProfileController`.
- Chưa có ScriptableObject Event Channel làm hợp đồng ổn định cho UI.
- Scene `SingleLine` và nhiều script/asset đang có thay đổi chưa commit; branch `Vee` đang đi trước `origin/Vee` 2 commit.
- Android vẫn còn thông tin mặc định `UnityTemplateGame`, version `0.0.2` và chưa có application identifier riêng.
- `ParticleImage` đã được import; package phù hợp với UI/Canvas, không thay thế particle world-space của block.

## Danh sách task

| ID | Tên nhiệm vụ | Trạng thái | Người phụ trách | Hạn chót | Ưu tiên | Kết quả bắt buộc |
|---|---|---|---|---|---|---|
| T00 | Chốt baseline và tách thay đổi hiện tại | Hoàn thành | Core Gameplay (Vee) | 11/09 | Cao | Xác định file thuộc từng task, compile sạch trước khi chuyển scene, không ghi đè thay đổi của team |
| T01 | Chuẩn hóa và kiểm định 300 level | Hoàn thành | Core Gameplay (Vee) | 11/09 | Cao | Level 1–300 theo đúng thứ tự Resources; toàn bộ ID, connectivity, start/end và solution hợp lệ |
| T02 | Đóng gói gameplay thành prefab composition | Hoàn thành | Core Gameplay (Vee) | 11/09 | Cao | Board, input, camera framing, feedback và bootstrap được gom thành prefab có dependency Inspector rõ ràng |
| T03 | Tạo Event Channel cho giao tiếp UI | Hoàn thành | Core Gameplay (Vee) | 11/09 | Cao | UI có thể đăng ký level loaded, progress changed, stuck, hint, win và gameplay state mà không tham chiếu controller cụ thể |
| T04 | Chuyển gameplay vào scene template `Gameplay` | Hoàn thành | Core Gameplay (Vee) | 11/09 | Cao | Scene template giữ Managers/services; gameplay prefab hoạt động trong scene; không còn phụ thuộc scene `SingleLine` trong build flow |
| T05 | Tích hợp tiến trình với `UserProfileController` | Hoàn thành | Core Gameplay (Vee) | 12/09 | Cao | Mở game đọc level đã lưu; thắng cập nhật level tiếp theo và gọi save; level 300 xử lý an toàn; không lưu trạng thái board |
| T06 | Nối state gameplay với service win/lose của `_SDK` | Hoàn thành | Core Gameplay (Vee) | 12/09 | Cao | Win báo `GameStateService`; Stuck không tự thua theo GDD; pause/resume chặn input đúng; UI popup nhận state qua SDK/event |
| T07 | Hoàn thiện prefab block và phản hồi theo GDD | Hoàn thành Core | Core Gameplay (Vee) | 12/09 | Cao | Dùng `OneLineBlock`/asset có sẵn; line khác màu block; nối ô có nhấn–nảy; ô đầu có chấm trắng và pulse; idle 3 giây rung nhắc |
| T08 | Nối audio, haptic và particle presentation | Hoàn thành Core | Core Gameplay (Vee) | 12/09 | Trung bình | Audio gọi qua `AudioController`/config; haptic đúng sự kiện; particle đúng prefab/đối tượng và được pool; UI particle để UI teammate sở hữu |
| T09 | Kiểm tra responsive và Safe Area | Core xong; chờ UI | Core Gameplay + UI teammate | 12/09 | Cao | Board không che HUD/control ở 720×1280, 1080×1920, 1080×2400 và 4:3; safe area vẫn đúng sau chuyển scene |
| T10 | Kiểm thử gameplay và tích hợp scene | Hoàn thành | Core Gameplay (Vee) | 12/09 | Cao | EditMode tests pass; smoke test PlayMode; vuốt nhanh không bỏ ô; undo/restart deterministic; chuyển liên tiếp nhiều level không reload scene |
| T11 | Chuẩn hóa Android và tạo APK kiểm thử | APK xong; chờ thiết bị/app ID | Core Gameplay (Vee) | 12/09 | Cao | Build Settings đúng flow; product/package/version được cấu hình; APK cài và mở được; Loading → Gameplay → win → level kế tiếp hoạt động |
| T12 | Bàn giao hợp đồng event cho UI | Hoàn thành | Core Gameplay + UI teammate | 12/09 | Cao | Có tài liệu event/payload, prefab/asset cần kéo Inspector và checklist scene để UI tích hợp không sửa Core |

## Thứ tự triển khai

### Mốc 1 — Baseline chơi được và dữ liệu tin cậy

Thực hiện `T00 -> T01`.

1. Kiểm tra compile và Console trước khi sửa.
2. Chạy toàn bộ test Core hiện có.
3. Chạy batch validator/solver trên đúng 300 level.
4. Sửa thứ tự level theo Resources và tạo ánh xạ ổn định `1..300 <-> chXX_YYY`.
5. Tách commit theo logic, không gom package, level, scene và gameplay vào một commit.

Tiêu chí thoát mốc: mọi level tải được theo thứ tự, có solution hợp lệ và gameplay hiện tại vẫn chạy.

### Mốc 2 — Hợp đồng Core và cấu trúc scene

Thực hiện `T02 -> T03 -> T04`.

1. Biến hierarchy gameplay hiện tại thành prefab composition thuộc `_Game`.
2. Đưa reference vào Inspector; không thêm runtime lookup.
3. Tạo các ScriptableObject Event Channel nhỏ theo payload, không tạo một event bus tổng.
4. Giữ gameplay logic thuần C# phát state; lớp Unity adapter raise event channel.
5. Mở scene template `Gameplay`, giữ nguyên service/manager có sẵn và gắn gameplay prefab vào composition root.
6. Chuyển Build Settings sang flow template và loại `SingleLine` khỏi danh sách scene được build sau khi xác nhận scene mới tương đương.

Tiêu chí thoát mốc: `Loading` mở đúng `Gameplay`, board xuất hiện, input hoạt động và scene không có missing reference/script.

### Mốc 3 — SDK, save và game flow

Thực hiện `T05 -> T06`.

1. Tạo adapter đọc `UserProfileController.Instance.LEVEL` hoặc API chính thức tương ứng.
2. Clamp dữ liệu cũ/hỏng về miền `1..300`.
3. Load level đã lưu khi gameplay bắt đầu.
4. Khi thắng, raise event, báo `GameStateService.Win()`, cập nhật tiến trình một lần và gọi luồng save của SDK.
5. Khi UI yêu cầu level tiếp theo, reset state và tái sử dụng board/pool; không reload scene.
6. `Stuck` chỉ phát cảnh báo/hướng dẫn, vẫn cho backtrack, undo, restart và hint. GDD hiện không có Game Over tự động.
7. Pause/resume từ popup phải vô hiệu hóa input mà không mất path.

Tiêu chí thoát mốc: tắt/mở app quay lại đúng level; thắng không cộng level hai lần; level cuối không truy cập ngoài dữ liệu.

### Mốc 4 — Feedback theo GDD và Resources

Thực hiện `T07 -> T08 -> T09`.

1. Đối chiếu `OneLineBlock` và các prefab trong `Asset_Resources` trước khi tạo asset mới.
2. Dùng animation clip/controller có sẵn cho pulse, idle reminder và nhấn–nảy.
3. Dùng line có màu tương phản với block theo Theme config.
4. Audio được phát từ subscriber/presentation adapter bằng enum `SoundName` và `SoundAssetConfigs`.
5. Particle gameplay world-space dùng prefab/ParticleSystem phù hợp và pool; `ParticleImage` chỉ dùng cho Canvas/UI.
6. Kiểm tra board fit trong safe content area ở bốn tỉ lệ màn hình bắt buộc.

Tiêu chí thoát mốc: hành vi và cảm giác chính khớp GDD, không tạo/destroy object liên tục và gameplay steady-state không phát sinh GC đáng kể.

### Mốc 5 — Regression và APK

Thực hiện `T10 -> T11 -> T12`.

1. Chạy EditMode tests và sửa mọi regression.
2. Smoke test ít nhất level 1, 2, 30, 31, 299 và 300.
3. Test app restart, pause/resume, mất focus, vuốt nhanh, backtrack, undo, restart, hint, stuck và win.
4. Kiểm tra Console không có error; cảnh báo vendor được ghi nhận riêng.
5. Cấu hình Android package name, product name, orientation portrait, version và kiến trúc build.
6. Build APK Development, cài lên thiết bị/emulator, chạy lại flow bắt buộc.
7. Ghi tài liệu event contract và các reference UI cần gắn.

Tiêu chí thoát mốc: APK mở từ flow đầu, chơi và lưu tiến trình được, không có lỗi chặn release.

## Event contract tối thiểu cho UI

| Event | Payload đề xuất | Khi phát |
|---|---|---|
| GameplayReady | Level number, level ID | Board tải xong và nhận input được |
| LevelProgressChanged | Visited, total | Sau move/backtrack/undo/restart hợp lệ |
| GameplayStateChanged | Previous, current | Khi Ready/Drawing/Stuck/Won/Pause thay đổi |
| InvalidMove | Cell/index hoặc loại lỗi tối thiểu | Khi move bị từ chối để presentation cảnh báo |
| HintStarted | Không payload hoặc request ID | Khi bắt đầu tìm hint |
| HintResolved | Hint result | Khi hint có kết quả hoặc yêu cầu restart |
| LevelWon | Level number, level ID | Chính xác một lần khi level hoàn tất |

Event Channel chỉ truyền dữ liệu gameplay. Popup, label, particle UI và animation UI do subscriber của UI quyết định.

## Kết quả thực hiện 12/09/2026

- Đã đóng gói gameplay thành `SingleLineGameplay.prefab` và đặt trong scene template `Gameplay`; Build Settings chỉ còn `Loading -> Gameplay`.
- Đã tạo `GameplayEventChannelSO` và tài liệu payload để UI đăng ký mà không phụ thuộc trực tiếp vào controller.
- Đã đọc/lưu level qua adapter `UserProfileController`, clamp trong miền 1–300, tự chuyển level bằng pool mà không reload scene.
- Đã hoàn thiện kéo liên tục, backtrack, cảnh báo bước sai, stuck có thể phục hồi, hint, win, chấm trắng/pulse ở ô đầu và rung nhắc sau 3 giây.
- Audio đi qua `AudioController`; haptic đi qua SDK; particle gameplay hiện là object sống cùng prefab nên không phát sinh `Instantiate/Destroy` trong steady state. `ParticleImage` được giữ cho UI teammate.
- `Crystal.SafeArea` vẫn được gắn trên `Canvas - 0/safe`; board dùng camera framer theo bounds. Phần duyệt hình ảnh ở bốn tỉ lệ màn hình còn chờ UI teammate ký duyệt.
- Unity EditMode: 198/198 test pass. Smoke đã xác nhận `Loading -> Gameplay`, board nhận input và solver hoàn thành level với state `Won`.
- Corpus nguồn: 1.059 file, 834 board duy nhất, 831 board dùng được; campaign xuất 10 chapter/300 level và replay 300/300. Ba board nguồn không giải được bị loại trước khi assemble campaign.
- Android APK build thành công tại `Builds/SingleLine-development.apk` (393,51 MB), SHA-256 `1E10201830069049E25F24E7C81584A97D2AFBEBB43D5C1F7AC463BE1AA05226`.
- Chưa thể cài APK vì `adb devices` không có thiết bị/emulator. Product name, application identifier và version vẫn dùng giá trị template vì team chưa cung cấp giá trị phát hành.
- Prefab `Cell` chuyên dụng được dùng cho board để giữ cấu trúc pool và dependency Inspector gọn; hành vi/visual cue được đối chiếu từ `OneLineBlock`, nhưng không nhúng trực tiếp prefab resource lớn đó.
## Kế hoạch commit

Các commit dự kiến, có thể tách nhỏ hơn nếu diff lớn:

```text
chore(deps): import ParticleImage package
fix(level): reorder first 300 levels from resource data
test(level): validate shipped 300 level campaign
refactor(gameplay): package board composition into prefab
feat(events): add gameplay event channels for UI integration
refactor(scene): integrate gameplay into template scene
feat(progress): persist current level through user profile
feat(flow): bridge gameplay state to SDK services
feat(feedback): match block guidance and connection feedback to GDD
feat(audio): route gameplay cues through audio config
fix(layout): preserve board bounds inside safe area
chore(android): configure development APK build
docs(gameplay): document UI event contract and scene setup
```

Hai commit đang có trên local dùng format cũ sẽ không bị rewrite trong phạm vi này nếu team không yêu cầu sửa lịch sử Git.

## Đường găng và giới hạn phạm vi

Đường găng để có APK là:

`300 level hợp lệ -> gameplay prefab -> scene Gameplay -> save/progress -> SDK win flow -> regression -> Android build`

Các hạng mục không được phép chặn APK Core:

- Sửa warning obsolete bên trong source của `ParticleImage`.
- Sửa demo `Scroll.prefab` bị thiếu nested prefab của package.
- Tự thiết kế popup/HUD/Settings thay phần của UI teammate.
- Thêm meta game, booster hoặc economy ngoài yêu cầu GDD.
- Refactor `_SDK` khi có thể giải quyết bằng adapter trong `_Game`.

## Definition of Done

- Flow build dùng scene template `Gameplay` và không còn lỗi gọi sai tên scene.
- 300/300 level qua validator và solver.
- Kéo nhanh, backtrack, undo, restart, hint, stuck và win đúng GDD.
- Tiến trình level được đọc/lưu qua `UserProfileController`.
- Core không thao tác trực tiếp popup/HUD; UI nhận dữ liệu qua Event Channel.
- Gameplay object/effect thường xuyên được pool; không runtime lookup trong hot path.
- Không có compile error, missing script/reference hoặc Console error thuộc code game.
- APK Android cài đặt, mở và hoàn thành smoke test trên thiết bị/emulator.

## Kết quả thực hiện 13/09/2026 (đợt feedback họp)

### Đã xong trong repo

- **Thứ tự level theo resources.** Thêm `SourceOrderAssembler` + `AllBoardsSelector`; campaign giữ nguyên thứ tự pack nguồn thay vì rải lại theo đường cong độ khó. Thứ tự pack mặc định trong `LevelPackOrder.Default`: `beginner → medium → hard → mediumold → expertold → masterold → beginner 1 → beginner222`, nên 300 level đang phát hành là `beginner/Level_1..100` rồi `medium/Level_1..200`. Đã xuất lại `ch01..ch10`, replay 300/300, level đầu đúng là `beginner/Level_1` (board 3x1).
- **LevelConfig tổng.** `Assets/_Game/Config/SingleLineLevelConfig.asset` liệt kê đủ 300 level, mỗi level là một `SingleLineLevelSO` trong `Assets/_Game/Config/Levels/`. Menu `Tools/Single Line/Rebuild Level Config` dựng lại từ chapter json. `LevelBootstrap` đọc thứ tự qua `ILevelCatalog` và giao quyền sở hữu số level cho `LevelService` của template.
- **Chấm trắng + vòng xám.** Màu chấm và màu vòng lấy từ `ThemeSO` (`startDot`, `startHalo`) thay vì hard-code trắng. Rung nhắc sau 3 giây vẫn chạy clip `StartIdle` gốc.
- **Palette theo game mẫu.** Nền đen, khối `#4A5462`, ô bắt đầu đỏ `#DE3B3B`, đường kẻ cam sáng `#FF8A5C` tách hẳn khỏi màu khối. Nhãn level đổi sang dạng `LEVEL {số}`.
- **Cảm giác nối khối.** `CellView.PlayConnect()` nhấn xuống rồi nảy lên (lấy hình dạng từ `TutorialFillSquareSelection`, rút ngắn còn 140 ms cho kịp tốc độ vuốt). Thứ tự vẽ của đường kẻ được nâng lên trên toàn bộ art trong prefab ô.
- **Particle theo đối tượng.** `PooledEffectPlayer` + `EffectPool`, pool sẵn, không `Instantiate`/`Destroy` khi đang chơi. Bốn nhóm: nối ô, bước sai, thắng màn, cue ô bắt đầu. Thắng màn đã gán `ConfettiBlastRed`.
- **Event channel.** Thêm `OnHintStarted` và ba lệnh `RequestUndo/RequestRestart/RequestHint`, nên UI không cần tham chiếu nào vào gameplay.

### Đã kiểm trong Unity Editor

- Chạy `Tools/Single Line/Extract Block Effects`: tách 4 prefab `FX_*` từ `OneLineBlock` và gán vào `PooledEffectPlayer`.
- Chạy `Tools/Single Line/Rebuild Level Config`: 300 level. Asset sinh tay trước đó khớp đúng với bản Unity ghi ra, `git diff` rỗng.
- EditMode tests: 208/208 pass.
- Play thử `Loading -> Gameplay` ở 1080x1920: chấm trắng, vòng xám tỏa (đo được scale 2.68, alpha 0.18), clip `StartIdle` chạy đúng nhịp 3 giây rồi lặp 4 giây, đường kẻ cam tách hẳn màu khối, thắng màn chuyển vàng, tự sang màn kế. Pool particle không rò: sau khi hiệu ứng chạy xong `playing=0 particles=0`.

Hai thứ chỉ lộ ra khi chạy thật, đã sửa:

1. `ConfettiBlastRed` là hiệu ứng có hướng, lúc thắng phun một vệt sang phải. Đổi sang `effect_jump_success` của chính block mẫu.
2. `effect_jump` thực ra là vụ nổ 17 hệ hạt (`ExplosionRoundFire`, `FireBall`, `Ring`, `Glow`), bắn mỗi ~100 ms khi vuốt thì thành nhấp nháy vòng trắng to bằng ô. Đổi sang `effect_collect_star` (7 hạt, size 0.5). Thêm knob `scale` cho từng binding vì art gốc nằm trong block có scale riêng.

### Còn phải làm

1. Chuyển `Cell.prefab` thành Prefab Variant của `OneLineBlock.prefab` — **đề nghị không làm**, xem mục dưới.
2. Nhóm hạt của `effect_collect_star` bay chếch lên phải và nhạt dần về xám; nếu muốn bám màu ô thì phải sửa Color over Lifetime trong prefab `FX_effect_collect_star`.

### Vì sao không nên biến Cell thành Prefab Variant

| | GameObject | ParticleSystem | dòng YAML |
|---|---:|---:|---:|
| `OneLineBlock.prefab` | 66 | 40 | 95.805 |
| `Cell.prefab` | 18 | 0 | 1.634 |

Pool 100 ô sẽ thành 6.600 GameObject và 4.000 ParticleSystem sống cùng lúc, kèm Canvas, Text Legacy, BoxCollider2D nhân lên 100 lần — trái mục 1 và 2 của `CLAUDE.md`.

Lợi ích thật của variant là art sửa bản gốc thì lan sang. Đã kiểm: cả 6 sprite trong `Cell.prefab` đều trỏ đúng asset mà `OneLineBlock` dùng, animator cũng dùng chung `TutorialFillSquareAnimations.controller`, nên sửa file art vẫn lan sang bình thường. Chỉ đổi cấu trúc hierarchy mới không lan.

### Bẫy môi trường

`EditorApplication.update` + `Step()` là cách duy nhất chạy được play mode khi Editor mất focus, nhưng nếu callback ném exception thì Unity **không tự gỡ đăng ký**: nó ném lại mỗi tick và nghẽn luôn plugin MCP, phải recompile mới thoát. Luôn resolve object trong `Main()`, bọc thân callback trong try/catch và gỡ callback ở nhánh lỗi.

Cũng đừng đo `ParticleSystem.particleCount` từ editor tick — đọc ra 0 kể cả với hiệu ứng đang chạy thật. Muốn biết hiệu ứng có bắn hay không thì nhìn ảnh Game View.

### Giả định cần Producer xác nhận

Câu 5 và 6 trong `PROJECT_CONTEXT.md` vẫn chưa có câu trả lời. Thứ tự pack ở trên là lựa chọn mặc định, đổi bằng một dòng trong `LevelPackOrder.Default` rồi chạy lại importer. Ba file nguồn không giải được đã bị loại và ghi trong báo cáo import: `beginner/Level_38`, `masterold/Level_22`, `medium/Level_191`.
