! demo_ui.f90 —— Fortran 第四层：**最新 UI 接口**（宿主 `ui_*` 图元 + 绘图窗口）
! demo_ui.f90 -- Fortran layer four: **the newest UI interface** (host `ui_*` primitives + a drawing window)
!
! 这一层和 BGI 那一路刻意不同。BGI 是"固定分辨率 + 索引色"的老世界，
! This layer is deliberately different from the BGI path. BGI is the old world of "fixed resolution + indexed colors",
! 这一层是**宿主窗口 + 0xAARRGGBB 真彩 + 统一消息队列**。四条关键差别：
! while this layer is **a host window + 0xAARRGGBB true color + a unified message queue**. Four key differences:
!
!   ① **开窗显式**：`ui_win_open(标题, 宽, 高)`，宽高来自 `ui_scr_w()/ui_scr_h()`
!   ① **Opening the window is explicit**: `ui_win_open(title, width, height)`, where width/height come from `ui_scr_w()/ui_scr_h()`
!      （可用绘图区），不是写死的 640x480。
!      (the usable drawing area), not a hard-coded 640x480.
!   ② **屏幕方向要问**：`ui_orientation()` 返回 0=竖屏 / 1=横屏 —— 程序据此决定
!   ② **The screen orientation must be asked**: `ui_orientation()` returns 0=portrait / 1=landscape -- the program uses it to decide
!      "面板怎么摆"。这是**开窗前就能问**的（详见 `docs/VML宿主接口.md`）。
!      "how to lay the panel out". This is **askable before the window opens** (see the VML host interface doc under `docs/`).
!   ③ **显式呈现**：图元只是追加进场景，`ui_present()` 才是"这一帧画完了"的帧边界。
!   ③ **Presentation is explicit**: primitives are only appended to the scene; `ui_present()` is the frame boundary that says "this frame is done".
!   ④ **输入是消息队列**：`ui_wait_msg(超时) / ui_msg_a() / ui_win_closed()` 收
!   ④ **Input is a message queue**: `ui_wait_msg(timeout) / ui_msg_a() / ui_win_closed()` receive
!      键盘、触摸、定时器、窗口事件 —— 全部走同一个队列。
!      keyboard, touch, timer and window events -- all through one and the same queue.
!      消息类型：1=KeyDown 2=KeyUp 3..5=鼠标 6..8=触摸 9=定时器 10=窗口关闭。
!      Message types: 1=KeyDown 2=KeyUp 3..5=mouse 6..8=touch 9=timer 10=window closed.
!
! 跑法（桌面 vmlcli）：
! How to run (desktop vmlcli):
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_ui.f90 \
!       --screen 480x640 --frames out_frames/
!
! ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame` 单个文件**：`ui_win_close()`（#521）
! ⚠ **To see the picture use `--frames <dir>`, not `--frame` for a single file**: `ui_win_close()` (#521)
!   在宿主侧把整个场景置空（`VmlHostRuntime.WinClose` 的 `_scene = null`），
!   nulls out the whole scene on the host side (`_scene = null` in `VmlHostRuntime.WinClose`),
!   跑完之后 `--frame` 已经没场景可取；而 `--frames` 是每帧 present **当场拍快照**。
!   so after it finishes there is no scene for `--frame` to take; whereas `--frames` **snapshots right at each present**.
!
! ◆ **有界**（任务要求：按 N 帧或收到键就退出）—— 三条出口都写了：
! ◆ **Bounded** (a task requirement: exit after N frames or on a key press) -- all three exits are implemented:
!   · 收到**任意按键**（消息类型 1 = KeyDown）立刻退出
!   · exits immediately on **any key press** (message type 1 = KeyDown)
!   · 或画满 150 帧自动退出（每帧 `ui_wait_msg(30)` 最多等 30ms ⇒ 约 4.5 秒）
!   · or exits automatically after 150 drawn frames (each `ui_wait_msg(30)` waits at most 30ms => about 4.5 seconds)
!   · 或用户点了标题栏的返回箭头（`ui_win_closed() /= 0`）
!   · or the user taps the back arrow in the title bar (`ui_win_closed() /= 0`)
!   退出前 `ui_win_close()`，程序正常结束、退出码 0。
!   `ui_win_close()` runs before exiting; the program ends normally with exit code 0.
!
! ═══════════════════════════════════════════════════════════════════════
!  ⚠ 三条本前端的写法要求（每条都来自实测，不是风格偏好）
!  ⚠ Three style requirements of this frontend (each from measurement, not style preference)
!
!  ① **调库函数一律写成函数调用表达式 `k = ui_rect(...)`**。
!  ① **Always write a library call as the function-call expression `k = ui_rect(...)`**.
!     写成 `call ui_rect(...)` 会被编成 `CALL sub_ui_rect`，而**链接器只剥
!     Written as `call ui_rect(...)` it compiles to `CALL sub_ui_rect`, and **the linker strips only
!     `func_`/`word_`/`method_`/`var_` 四种前缀、不含 `sub_`** ⇒ 碰不到 UI 库。
!     the four prefixes `func_`/`word_`/`method_`/`var_`, not `sub_`** => it never reaches the UI library.
!     （对照组：`call putchar(27)` 恰好能编成裸名 `CALL putchar`，所以那一句能跑
!     (Control case: `call putchar(27)` happens to compile to the bare name `CALL putchar`, so that one line works
!      —— 见 `demo_tty.f90`。**别把某个 `call` 能跑推广到所有 `call`**。）
!      -- see `demo_tty.f90`. **Do not generalize "one `call` works" to all `call`s**.)
!
!  ② **一个子程序都不用**。`contains` 里的内部子程序**传不进实参**、
!  ② **No subroutines at all**. A contained subroutine **cannot receive arguments**
!     也**看不见宿主程序的变量**（实测 `call show(97)` 里 `v` 读到 0）；
!     and **cannot see the host program's variables** (measured: inside `call show(97)` `v` reads 0);
!     外部子程序则直接「未定义的函数」。所以这份 demo 全部内联。
!     an external subroutine just reports "undefined function". So this demo is entirely inline.
!
!  ③ **正文里不出现数字**。本前端没有整数转串（`Str()` 没有、`//` 拼串没有、
!  ③ **No digits appear in the display text**. This frontend has no integer-to-string conversion (no `Str()`, no `//` concatenation,
!     `character` 变量存不住字符串），而 `print` 浮点又会打出整数的位模式
!     and a `character` variable cannot hold a string), while `print` on a float prints the integer bit pattern
!     （见 `demo_std.f90`）⇒ 状态一律用**形状与固定标签**表达：进度用 `ui_rect`
!     (see `demo_std.f90`) => state is expressed purely with **shapes and fixed labels**: progress through the width of
!     的宽度、方向用两个不同的词。这与同目录 `sokoban.f90` 的取舍一致。
!     a `ui_rect`, orientation through two different words. This matches the trade-off in `sokoban.f90` in this directory.
!
!  附：颜色用的是**十进制负数**（`0xAARRGGBB` 作为 32 位有符号整数会溢出）。
!  Note: colors are written as **negative decimals** (`0xAARRGGBB` overflows as a 32-bit signed integer).
!      每一条后面都标了它对应的十六进制值，改的时候照着改、别手算 ——
!      Every one carries the corresponding hex value; change it by copying, never by hand --
!      手算差了 1 个最低位，画出来"只差一点点"，肉眼根本看不出来。
!      being off by one least-significant bit draws "just a tiny bit different", which the eye simply cannot see.
! ═══════════════════════════════════════════════════════════════════════

program demo_ui
  implicit none
  integer :: w, h
  integer :: ori
  integer :: frame, maxf
  integer :: tid, msg, kc
  integer :: running
  integer :: k
  integer :: bx, by, bw, bh

  ! ① 问可用绘图区；拿不到就给个兜底尺寸（桌面脚手架 / 某些后端会给 0）
  ! ① Ask for the usable drawing area; if unavailable fall back to a default size (the desktop scaffolding / some backends return 0)
  w = ui_scr_w()
  h = ui_scr_h()
  if (w <= 0) then
    w = 360
  end if
  if (h <= 0) then
    h = 620
  end if

  ! ② 开窗 —— 宽高直接取"可用绘图区"，于是手机上就是整页
  ! ② Open the window -- width/height come straight from the "usable drawing area", so on a phone it fills the page
  k = ui_win_open('Fortran UI demo', w, h)

  ! ③ 问屏幕方向：0 = 竖屏，1 = 横屏
  ! ③ Ask the screen orientation: 0 = portrait, 1 = landscape
  !    ⚠ 这里本来想先把文字存进 `character(len=40) :: label1` 再画 —— **做不到**：
  !    ⚠ The original plan was to store the text in `character(len=40) :: label1` and then draw it -- **impossible**:
  !    本前端的 `character(len=…)` 声明直接编译错（「期望变量名（得到 LParen）」），
  !    this frontend's `character(len=...)` declaration is a compile error ("expected variable name (got LParen)"),
  !    而不带长度的 `character :: s` 虽然编得过、却**存不住字符串**（`s = 'AB'` 读回来是 0）。
  !    and a length-less `character :: s` compiles but **cannot hold a string** (`s = 'AB'` reads back as 0).
  !    ⇒ 文字只能以**字面量**形式直接写在 `ui_text(...)` 那一句里，所以下面的
  !    => the text can only be written as a **literal** directly in the `ui_text(...)` call, so the
  !    if/else 是两份重复的绘制调用（而不是"选一句话再画一次"）。
  !    if/else below is two duplicated drawing calls (instead of "pick a sentence and draw once").
  ori = ui_orientation()

  ! ④ 方向决定面板怎么摆：横屏把方块放右上，竖屏放右下
  ! ④ The orientation decides the panel layout: in landscape the block goes top right, in portrait bottom right
  bx = 24
  by = 24
  bw = 140
  bh = 100
  if (ori == 1) then
    bx = w - bw - 24
    by = 120
  else
    by = h - bh - 110
  end if

  maxf = 150
  frame = 0
  running = 1

  do while (running == 1)
    ! ── 每帧重画（保留模式：场景每帧从头追加）────────────────────
    ! -- Redraw every frame (retained mode: the scene is appended from scratch each frame) --
    k = ui_clear(-15724520)                                   ! 0xFF101018 深底
    ! 0xFF101018 dark background

    ! 标题（方向那行只能按 if/else 各写一份字面量 —— 理由见上面第 ③ 条）
    ! Title (the orientation line can only be written as one literal per if/else branch -- see item ③ above)
    k = ui_text(16, 16, 'Fortran UI demo', -6629121, 20, 0)   ! 0xFF9AD8FF 浅蓝
    ! 0xFF9AD8FF light blue
    if (ori == 1) then
      k = ui_text(16, 44, 'orientation = LANDSCAPE (1)', -11446, 15, 0)   ! 0xFFFFD34A
    else
      k = ui_text(16, 44, 'orientation = PORTRAIT (0)', -11446, 15, 0)    ! 0xFFFFD34A
    end if
    k = ui_text(16, 66, 'ui_win_open / ui_rect / ui_circle / ui_line / ui_text / ui_present', -7693656, 11, 0)  ! 0xFF8A9AA8

    ! 图元三件套：线 / 矩形 / 圆
    ! The primitive trio: line / rectangle / circle
    k = ui_line(16, 92, w - 16, 92, -13414294, 1)             ! 0xFF33506A 分隔线
    ! 0xFF33506A separator line
    k = ui_rect(bx, by, bw, bh, -13730510, 1, 0, 8)           ! 0xFF2E7D32 深绿实心圆角框
    ! 0xFF2E7D32 dark green filled rounded box
    k = ui_rect(bx + 12, by + 12, bw - 24, bh - 24, -10044566, 0, 2, 0)  ! 0xFF66BB6A 浅绿空心
    ! 0xFF66BB6A light green hollow
    k = ui_circle(w / 2, h / 2, 44, -1618884, 1, 0)           ! 0xFFE74C3C 红实心圆
    ! 0xFFE74C3C red filled circle
    k = ui_circle(w / 2, h / 2, 20, -932849, 1, 0)            ! 0xFFF1C40F 黄内圆
    ! 0xFFF1C40F yellow inner circle

    ! 帧计数进度条（不写字：这里没有整数转串，见文件头第 ③ 条）
    ! Frame-count progress bar (no text: there is no integer-to-string here, see item ③ in the file header)
    k = ui_rect(16, h - 62, w - 32, 14, -14536644, 1, 0, 3)   ! 0xFF22303C 进度条底
    ! 0xFF22303C progress bar background
    k = ui_rect(16, h - 62, (w - 32) * frame / maxf, 14, -16727384, 1, 0, 3)  ! 0xFF00C2A8
    k = ui_text(16, h - 42, 'press any key to exit', -7693656, 13, 0)
    k = ui_text(w - 16, h - 42, 'ui_present every frame', -7693656, 13, 2)

    ! ── 帧边界 ────────────────────────────────────────────────────
    ! -- Frame boundary --
    k = ui_present()
    frame = frame + 1

    ! 出口 A：画满 maxf 帧
    ! Exit A: after maxf frames are drawn
    if (frame >= maxf) then
      running = 0
    end if

    ! 出口 B：收到任意按键（消息类型 1 = KeyDown）
    ! Exit B: any key press received (message type 1 = KeyDown)
    if (running == 1) then
      msg = ui_wait_msg(30)
      if (msg == 1) then
        kc = ui_msg_a()
        running = 0
      end if
    end if

    ! 出口 C：用户点了标题栏的返回箭头
    ! Exit C: the user tapped the back arrow in the title bar
    if (ui_win_closed() /= 0) then
      running = 0
    end if
  end do

  ! 正常收尾：关窗（定时器没用到就不必 kill）
  ! Normal wrap-up: close the window (no timer was used, so nothing needs killing)
  k = ui_win_close()
end program demo_ui
