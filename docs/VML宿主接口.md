# VML 宿主接口（手机端 syscall 500–599）

> 这份文档是**手机端 VML 宿主接口的唯一存档**：号段约定、现有接口全表、设计约束、
> 以及"想做但还没做"的那批（含理由与优先级）。改接口先改这里，再改
> `WayCoder/UI/Shared/VmlUiProtocol.cs`（纯逻辑，四端共享）。
>
> 相关代码：`UI/Shared/VmlUiProtocol.cs`（协议层）、`WayCoder.Maui/Services/VmlUiCalls.cs`
> （宿主实现）、`WayCoder.Maui/Pages/DrawWindowPage.xaml.cs`（渲染与输入）。

---

## 1. 号段约定

| 项 | 值 |
|---|---|
| 保留号段 | **500–599**（运行时现用最大号是 402，见 `VMLRuntime/SyscallNumber.cs`） |
| 运行模式 | `mcu`（privilege > 0）。dispatch 顶部有 `UserAllowedSyscalls.Contains(n)` 这道门 |
| 白名单 | 宿主启动时 `VmlUiCalls.EnsureReservedSyscallsAllowed()` 把**整段**塞进 `SyscallConstants.UserAllowed`——它与运行时内部那份是**同一个对象引用**（`VMLRuntime.cs:179`） |
| 处理器 | `ISystemCallHandler`，**在内置 switch 之前**被调用（`VMLRuntime.Syscall.cs:23`）⇒ 这个号段完全由宿主解释 |

**两条结论**：
1. **加接口不需要改 `third_party/vml` 任何一行**——这是这个号段存在的全部意义。
2. 漏了白名单那一步的现象是「处理器明明注册了却永远不被调用」，而且不报错。

### 1.1 返回值与失败约定

- 寄存器 `R0` 是**返回值**；多数"动作类"接口返回 `0` 表示成功。
- **认不出的号必须返回 `false`（放行）**，否则会吞掉运行时的内置 syscall。
- 宿主内部出错**不能把 VM 打挂**：`VmlUiCalls` 统一 catch，记 `ErrorLog` 并回 `-1`。

---

## 2. 现有接口全表

### 对话框（500–503）

| 号 | 名称 | 入参 | 返回 |
|---|---|---|---|
| 500 | `DLG_MSG` | R0=标题\* R1=正文\* R2=样式(0信息/1警告/2错误/3询问) | 0=确定/是，1=否/取消 |
| 501 | `DLG_SELECT` | R0=标题\* R1=提示\* R2=选项块\* R3=选项数 R4=默认项 | 选中索引，取消 -1 |
| 502 | `DLG_MULTI` | 同单选 | 选中位掩码，取消 -1 |
| 503 | `DLG_INPUT` | R0=标题\* R1=提示\* R2=缓冲\* R3=容量 | 写入缓冲，返回长度，取消 -1 |

\* = VML 内存里的 UTF-8、NUL 结尾字符串地址。

### 窗体与绘图（520–533，保留模式）

| 号 | 名称 | 入参 | 返回 |
|---|---|---|---|
| 520 | `WIN_OPEN` | R0=标题\* R1=宽 R2=高 | 句柄，失败 -1 |
| 521 | `WIN_CLOSE` | R0=句柄 | 0 |
| 522 | `DRAW_CLEAR` | R0=颜色(ARGB) | 0 |
| 523 | `DRAW_PIXEL` | x y 颜色 | 0 |
| 524 | `DRAW_LINE` | x1 y1 x2 y2 颜色 线宽 | 0 |
| 525 | `DRAW_RECT` | x y w h 颜色 填充 线宽 圆角半径 | 0 |
| 526 | `DRAW_CIRCLE` | cx cy r 颜色 填充 线宽 | 0 |
| 527 | `DRAW_ELLIPSE` | cx cy rx ry 颜色 填充 线宽 | 0 |
| 528 | `DRAW_TEXT` | x y 文本\* 颜色 字号 锚点(0左/1中/2右) 样式位 | 0 |
| 529 | `DRAW_ICON` | x y 图标名\* 尺寸 颜色 | 0 |
| 530 | `DRAW_IMAGE` | x y 路径\* w h | 0 |
| 531 | `DRAW_PRESENT` | — | 0（帧边界） |
| 532 | `SET_FONT` | 字号 样式位 颜色 锚点 | 0（设置"当前文字属性"） |
| 533 | `TEXT` | x y 文本\* | 0（用当前文字属性画一行） |

绘图是**保留模式**：程序只管往场景里追加图元，宿主按帧重绘。坐标单位是 **dp**（密度无关像素）。

### 输入与屏幕（560–567）

| 号 | 名称 | 入参 | 返回 |
|---|---|---|---|
| 560 | `MSG_POLL` | R0=消息缓冲地址 | 消息类型，无消息 0 |
| 561 | `MSG_WAIT` | R0=消息缓冲地址 R1=超时毫秒(0=无限) | 消息类型，超时 0 |
| 562 | `MSG_COUNT` | — | 队列里待处理条数 |
| 563 | `TIMER_SET` | R0=间隔毫秒 R1=用户标记 | 定时器 id |
| 564 | `TIMER_KILL` | R0=id | 0 |
| 565 | `WIN_CLOSED` | R0=句柄 | 1/0（用户点返回箭头） |
| 566 | `SCR_W` | — | 可用绘图区宽 |
| 567 | `SCR_H` | — | 可用绘图区高 |

**先问后开**是标准用法：`SCR_W/H` → `WIN_OPEN(title, W, H)`，这样绘图单位与屏幕 1:1，不缩放、不出界。

### 2.1 消息模型（固定 16 字节）

程序按 4 个 int 读：`[0]=类型 [1]=A [2]=B [3]=时间戳毫秒`。
定长布局是为了让**任何语言的 VML 前端**都能直接读（C 用 struct、BASIC 用 PEEK），不依赖宿主序列化。

| 类型 | 值 | A | B |
|---|---|---|---|
| `None` | 0 | — | — |
| `KeyDown` / `KeyUp` | 1 / 2 | 键码（Win32 虚拟键值，见 `VmlKeys`） | 0 |
| `MouseMove` / `MouseDown` / `MouseUp` | 3 / 4 / 5 | 场景 x | 场景 y |
| `TouchDown` / `TouchMove` / `TouchUp` | 6 / 7 / 8 | 场景 x | 场景 y |
| `Timer` | 9 | 定时器 id | 装机时的用户标记 |
| `WindowClose` | 10 | 0 | 0 |
| `WindowResize` | 11 | 新宽 | 新高 |

---

## 3. 规划中的接口（按优先级）

> 状态：`✅ 已实现` / `🚧 在做` / `📋 待做`

### P0 —— 手感（✅ 已实现，v0.96.172）

| 号 | 名称 | 入参 | 返回 | 说明 |
|---|---|---|---|---|
| **`#57`** | `SPEAKER_BEEP` | R0=频率Hz R1=时长ms | 0 | **VM 内置**，宿主截住接到真实音频（方波 + 3ms 包络）。**没有另立 `AUDIO_TONE`** —— 同一件事不做两处实现 |
| 541 | `AUDIO_PLAY` | R0=路径\*(沙箱相对) R1=循环(0/1) | 0 | BGM（wav/ogg/mp3） |
| 542 | `AUDIO_STOP` | — | 0 | 停掉 BGM |
| 543 | `AUDIO_VOLUME` | R0=音量(0-100) | 0 | 设置整体音量 |
| 545 | `VIBRATE` | R0=时长ms R1=强度(0-255，0=默认) | 0 | 单次震动 |
| 546 | `VIBRATE_PATTERN` | R0=模式\*（短整型数组：静/动交替的毫秒数）R1=段数 | 0 | 节奏震动 |
| 550 | `STORE_SET` | R0=键\* R1=值\* | 0 | 持久化（最高分、进度、设置） |
| 551 | `STORE_GET` | R0=键\* R1=缓冲\* R2=容量 | 长度，无此键 -1 | |
| 552 | `STORE_DEL` | R0=键\* | 0 | |
| 553 | `SCREEN_KEEP_ON` | R0=0/1 | 0 | 玩游戏时别熄屏 |
| 555 | `RANDOM` | R0=上限（不含） | 0..上限-1 | 宿主级随机数 |
| — | （不做） | — | — | 取时间用 VM 内置 `#53`/`#54`/`#55`/`#56`；**随机数用 `#50`**。先查有没有现成的 |

**为什么这一组先做**：全部是**普通权限**（`VIBRATE` 是 normal 级、其余不需要权限），
互相独立，加起来就能让现有两个游戏"有感觉"。`AUDIO_TONE` 刻意做成**合成音**而不是播放文件——
游戏音效不需要素材、不占包体、不用版权。

**`VIBRATE_PATTERN` 的数组**：VML 是 32 位小端，模式按 **int 数组**传（每项一个毫秒数），
语义照 Android `Vibrator.vibrate(long[], repeat)`：奇数下标=静、偶数下标=动。`R1` 是段数。

### P1 —— 手机特有的操作方式（📋 待做）

| 号 | 名称 | 说明 |
|---|---|---|
| 556 | `TOUCH_QUERY` | 查某槽位的手指：R0=槽位(0–9) → R0=x R1=y R2=按下。**多点触控**（虚拟摇杆、双指缩放、双人同屏） |
| 557 | `KEY_QUERY` | R0=键码 → 0/1。**查询键是否按住**（"持续按住左移"这类逻辑不必自己维护状态表） |
| 558 | `ORIENTATION_LOCK` | R0=0竖/1横/2自动 |
| 559 | `IMMERSIVE` | R0=0/1，隐藏状态栏与导航栏 |
| 547 | `AUDIO_IS_PLAYING` | → 1/0（BGM 播完没有） |

**`TOUCH_QUERY` 为什么是轮询式而不是新消息类型**：16 字节的消息塞不下"手指 id + x + y"
（`[类型][A][B][时间]` 只有两个自由值）。把它扩成 20 字节会把**所有现有程序**打断，
而轮询是在现有布局外新增，**零破坏**。真要做事件式的多点触控，得先给消息定个版本号。

### P2 —— 平台整合（📋 待做）

| 号 | 名称 | 说明 |
|---|---|---|
| 570 | `SENSOR_READ` | R0=类型(0加速度/1陀螺/2光感/3磁力) → R0=x R1=y R2=z（定点放大 1000 倍）。配 `Sensor` 消息 |
| 571 | `CLIPBOARD_SET` | R0=文本\* |
| 572 | `SHARE_TEXT` | R0=文本\*（调系统分享） |
| 573 | `TOAST` | R0=文本\*（轻提示不打断） |
| 574 | `FRAME_TIME` | → 上一帧耗时（微秒），供程序自适应画质 |

---

## 4. 设计约束（踩过的坑，别重犯）

1. **消息固定 16 字节**，加字段=打断所有现有程序。要加就加**新的轮询接口**。
2. **字符串一律 UTF-8、NUL 结尾、按地址读**。越界/非法读一律返回空串，不抛
   （`VmlUiCalls.Str`）。
3. **宿主侧要钳参数**：字号、音量、时长、频率都要 clamp——传 0 或负数会让下游算出零/负值，
   症状是"画不出来"或"程序卡住"，很难查。
4. **别在宿主里长耗时**：syscall 是在 VM 线程上同步返回的，任何阻塞都会拖住整个程序。
   要等用户的（弹框）例外——那正是"程序在等人"的语义。
5. **音频别用 `ToneGenerator` 做叠加**：它同时只能一个音。要多个音效同时响得用 `AudioTrack`
   自己混（当前 `AUDIO_TONE` 是单通道，够用且简单）。
6. **`WindowResize` 曾经是"协议里有、宿主从不发"的死消息**。转屏、折叠屏、以及绘图页新增的
   **手柄折叠条**都会改视口，程序却一无所知。现在在实测视口变化时补发（见
   `DrawWindowPage.OnSizeAllocated`）——**光有协议不叫有接口，得有人发**。
7. **持久化要带前缀**：`STORE_*` 走宿主 Preferences，键统一加 `vml.` 前缀，免得和 App 自己的
   配置打架。

---

## 5. 加一个新接口的步骤

1. 在 `UI/Shared/VmlUiProtocol.cs` 的 `VmlUi` 里加号 + 文档注释（含入参/返回/钳位规则）；
   纯逻辑的部分（解析、钳位、格式化）也放这里，**这样能被主工程自测覆盖**。
2. 在 `WayCoder.Maui/Services/VmlUiCalls.cs` 的 `HandleSyscall` switch 里加分支，
   实现体写成小方法（弹框/绘图各自成组）。
3. 号段白名单**不用动**（`ReservedRange()` 覆盖整段 500–599）。
4. 需要权限的在 `Platforms/Android/AndroidManifest.xml` 里声明（见 §6）。
5. 想要 VML 侧好用的包装函数，写进 `Lib/shared/vmlui.vml`——**但那个目录是上游仓库同步来的**
   （`sync.sh` 会 rsync 覆盖），本地改会被冲掉 ⇒ 包装函数应该提给上游 VML 仓库。
   C 语言可以直接 `SYSCALL #57` / `#545` 调内置与保留号。
   **v0.96.173 已本地加上、待提给上游的五个**（声明在 `Lib/c/waycoder_ui.h`）：
   `ui_beep`(#57) / `ui_vibrate`(#545) / `ui_keep_on`(#553) / `ui_store_set`(#550) /
   `ui_store_get`(#551)。**提上去之前，`sync.sh` 一跑就没了** —— 那是这条链上唯一的软肋。
6. **真机验证**：新建的接口在桌面上测不出来（没有震动/音频设备），必须装 APK 实跑。
7. **别用 C 内联汇编直连多参数 syscall**（v0.96.173 实测）：
   `asm("MOVE R0 ${freq}"); asm("MOVE R1 ${ms}");` 生成的是
   `move R0 [R12+12]; move R0 R0; move R0 [R12+16]; move R1 R0` —— **`${}` 展开时总是先载入 R0**，
   第二个参数把第一个覆盖掉。多参数一律走 `Lib/` 里的包装函数。
8. **带缓冲区的接口（`STORE_GET` 这类）在 C 里有两条既有前端缺陷**（v0.96.173 实测，见 §8）：
   缓冲区放全局、读回用 `atoi`/`strcmp`。

---

## 8. C 侧使用带缓冲区的接口：一个已修的前端缺陷 + 一条判定纪律（v0.96.173~174）

`ui_store_get(key, buf, cap)` 把字符串写进调用方给的缓冲区。当时的现象是「**长度对、内容不对**」，
一路查下来是一条**既有**的 C 前端缺陷，已在本地修掉并补成第 4 个 patch：

| 现象 | 真因 | 状态 |
|---|---|---|
| **全局 `char` / `short` 数组用下标访问按 32 位读写** | `InferExpressionType(ArrayAccess)` 只查 `variableTypes`（**只装局部变量**），全局数组查不到就退化成 `ExprType.Int` ⇒ 走 `MOVE` 而不是 `MOVEB` | ✅ 已修：`patches/0004-c-global-array-elem-type.patch`（复现 `.scratch/vmlhost/tests/globchar.c`） |

**局部数组一直是对的**（`variableTypes` 里有），所以这个坑只在全局数组上冒头 —— 而这一点很要命：
第一版结论把症状误判成了「**局部数组的地址传给函数是错的**」，于是写下了"缓冲区必须放全局"这条
并不存在的规矩。**教训是判定方法**：那一版用 `puts` 当判据，而本环境的 `puts` 在字符串字面量
较多的程序里输出会**串行/重复**（补丁前后一个样，是另一件事），于是把「stdio 坏了」看成了
「codegen 坏了」。**改判定不经过 stdio**（每条结论用一个 `ui_beep` 频率报出来，宿主原样打印）
之后，六个格子一次就量清了：局部下标读写 ✓✓、全局下标读写 ✗✗。

所以现在写 `ui_store_*` 的用法**不需要任何规避**：缓冲区放哪都行、`buf[0]` 也能直接读。
`Examples/c/tetris.c` 里那份（全局 `hibuf` + `atoi`）照旧能用，只是不必再当成硬约束。

> ⚠ **写桌面验证程序的两条纪律**（都是这次踩出来的）：
> ① **别拿 `puts` / `printf` 的输出当判据** —— 本环境里 `printf` 的格式化路径会崩
>    （`MOVEB @2, R0`，地址是垃圾值），`puts` 在部分程序里输出会串行/重复。
>    正解是**不经过 stdio 的判定**：一条结论一个 `ui_beep` 频率，宿主原样打印；
>    `ui_dlg_msg` 是"宿主从内存里读到的字符串"的现成探针（标题/正文都会被读出来）。
> ② **A==C 只证明"两条执行路径逐字节一致"，不证明逻辑正确** —— 有"两边一样地错"这种情形
>    （实测有：调用方的局部数组会被后续库调用踩坏，两条路径踩得一模一样）。
>    要看逻辑对不对，得靠出帧看画面。
>
> 四条**已复现、未定因**的现象（细节与现场见 CHANGELOG v0.96.174）：调用方局部数组被后续库调用
> 踩坏（顺序相关）、带三元表达式的实参可能传错、`puts` 输出串行、`printf` 崩溃。

---

## 6. Android 权限

| 接口 | 权限 | 级别 |
|---|---|---|
| `VIBRATE` / `VIBRATE_PATTERN` | `android.permission.VIBRATE` | **normal**（装上即生效，无需运行时申请） |
| `AUDIO_*` | 无 | — |
| `SCREEN_KEEP_ON` | 无（`FLAG_KEEP_SCREEN_ON`） | — |
| `ORIENTATION_LOCK` / `IMMERSIVE` | 无 | — |
| `SENSOR_*` | 无（加速度计/陀螺仪非危险权限） | — |

清单里已有：`INTERNET` / `ACCESS_NETWORK_STATE` / `CAMERA` / `RECORD_AUDIO` /
`MANAGE_EXTERNAL_STORAGE` / `READ_EXTERNAL_STORAGE` / `READ_MEDIA_IMAGES`。

---

## 7. 落点对照

| 层 | 文件 | 职责 |
|---|---|---|
| 协议 | `WayCoder/UI/Shared/VmlUiProtocol.cs` | 号、消息模型、纯逻辑（钳位/解析）——**四端共享、可自测** |
| 宿主 | `WayCoder.Maui/Services/VmlUiCalls.cs` | 把寄存器/内存翻成模型，驱动真实 UI |
| 平台 | `WayCoder.Maui/Services/VmlAudio.cs` 等 | MAUI 没提供的（音频合成）放这里，`#if ANDROID` + 其它平台空实现 |
| 渲染/输入 | `WayCoder.Maui/Pages/DrawWindowPage.xaml.cs` | 场景出图、触摸/按键投消息 |
| 示例 | `third_party/vml/Examples/c/*.c` | C 直接 `SYSCALL #nnn`（`Examples/` **不在** sync.sh 的同步范围，本地改动能留住） |
