' demo_ui.bas —— BASIC 第四层：**最新 UI 接口**（宿主 `ui_*` 图元 + 绘图窗口）
' demo_ui.bas -- BASIC layer four: **the newest UI interface** (host `ui_*` primitives + a drawing window)
'
' 这一层和第三层（BGI/`SCREEN`）刻意不同：BGI 那层是"固定分辨率 + 索引色"的老世界，
' This layer differs deliberately from layer three (BGI / `SCREEN`): the BGI layer is the old world of "fixed resolution + indexed colour",
' 这一层是**宿主窗口 + 0xAARRGGBB 真彩 + 统一消息队列**。四条关键差别：
' while this layer is **a host window + 0xAARRGGBB true colour + a unified message queue**. Four key differences:
'
'   ① **开窗显式**：`ui_win_open(标题, 宽, 高)`，宽高来自 `ui_scr_w()/ui_scr_h()`
'   ① **Opening the window is explicit**: `ui_win_open(title, width, height)`, where width/height come from `ui_scr_w()/ui_scr_h()`
'      （可用绘图区），不是写死的 640x480。
'      (the usable drawing area), not a hard-coded 640x480.
'   ② **屏幕方向要问**：`ui_orientation()` 返回 0=竖屏 / 1=横屏 —— 程序据此决定
'   ② **The screen orientation must be asked for**: `ui_orientation()` returns 0 = portrait / 1 = landscape -- the program uses it to decide
'      "面板横排还是竖排"。这是**开窗前就能问**的（见 `docs/VML宿主接口.md`）。
'      "whether to lay the panel out horizontally or vertically". This is **askable before the window opens** (see the VML host interface doc under `docs/`).
'   ③ **显式呈现**：图元只是追加进场景，`ui_present()` 才是"这一帧画完了"的帧边界。
'   ③ **Presentation is explicit**: primitives are only appended to the scene; `ui_present()` is the frame boundary that says "this frame is done".
'   ④ **输入是消息队列**：`ui_wait_msg(超时) / ui_msg_a() / ui_win_closed()` 收
'   ④ **Input is a message queue**: `ui_wait_msg(timeout) / ui_msg_a() / ui_win_closed()` receive
'      键盘、触摸、定时器、窗口事件 —— 全部走同一个队列。
'      keyboard, touch, timer and window events -- all through one and the same queue.
'
' 跑法（桌面 vmlcli）：
' How to run (desktop vmlcli):
'   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/basic/demo_ui.bas \
'       --screen 480x640 --frames out_frames/
'
' ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame 单个文件`**。原因不在本程序：
' ⚠ **To see the picture use `--frames <dir>`, not `--frame` for a single file**. The reason is not in this program:
'   `ui_win_close()`（#521）在宿主侧把**整个场景置空**（`VmlHostRuntime.WinClose`
'   `ui_win_close()` (#521) **nulls out the whole scene** on the host side (`VmlHostRuntime.WinClose`
'   的 `_scene = null`），于是程序跑完之后 `--frame` 那一刀**已经没有场景可取**，
'   does `_scene = null`), so after the program finishes the `--frame` snapshot **has no scene left to take**,
'   只会打一行「没有可导出的帧（场景是空的）」。而 `--frames` 是**每帧 present
'   it only prints one line saying there is no frame to export (the scene is empty). `--frames`, by contrast, **snapshots
'   当场拍快照**，所以照常出图（实测本程序 150 帧全部落盘）。
'   right at each present**, so images still come out (measured: all 150 frames of this program landed on disk).
'   这是桌面脚手架的行为，不是本程序写错了 —— 手机上窗口关掉当然也就没有画面了。
'   This is the behaviour of the desktop scaffold, not a mistake in this program -- once the window is closed on a phone there is of course no picture left either.
'
' ◆ **有界**（任务要求：按 N 帧或收到键就退出）—— 两条出口都写了：
' ◆ **Bounded** (a task requirement: exit after N frames or on a key press) -- both exits are implemented:
'   · 收到**任意按键**（消息类型 1 = KeyDown）立刻退出
'   · exits immediately on **any key press** (message type 1 = KeyDown)
'   · 或画满 150 帧自动退出（`ui_wait_msg(30)` 每帧最多等 30ms ⇒ 约 4.5 秒）
'   · or exits automatically after 150 drawn frames (each `ui_wait_msg(30)` waits at most 30ms => about 4.5 seconds)
'   退出前 `ui_win_close()`，程序正常结束、退出码 0。
'   `ui_win_close()` runs before exiting; the program ends normally with exit code 0.
'
' ◆ 写法要求（都是本前端的实测要求，照抄同目录的 `whack.bas` / `tetris.bas`）
' ◆ Style requirements (all of them measured requirements of this frontend, copied from `whack.bas` / `tetris.bas` in this directory)
'   · 外部过程必须 `NATIVE SUB` + **空体**；`NATIVE FUNCTION` 同理。
'   · An external procedure must be `NATIVE SUB` + **an empty body**; the same goes for `NATIVE FUNCTION`.
'   · 形参名不能是 BASIC 关键字（`on` / `and` / `or` …全不能当形参名）。
'   · A parameter name cannot be a BASIC keyword (`on` / `and` / `or` ... none of them can be a parameter name).
'   · 颜色用 `&HFFFF0000` 形式（词法器认 `&H`，**不认** `0x`）。
'   · Colours use the `&HFFFF0000` form (the lexer knows `&H` and **does not know** `0x`).
'   · 多个条件不写 `AND`/`OR`（本前端未验证），用嵌套 IF + 标志位。
'   · Do not write `AND`/`OR` for multiple conditions (unverified in this frontend); use nested IF + a flag variable.
'   · `STR$(n)` 把整数转字符串，`+` 拼串。
'   · `STR$(n)` converts an integer to a string; `+` concatenates strings.

' ══ 外部库声明（NATIVE = 裸标签，别加 sub_/func_ 前缀）════════════════
' ══ External library declarations (NATIVE = a bare label, do not add a sub_/func_ prefix) ════════════════
NATIVE SUB ui_clear(c AS INTEGER)
END SUB
NATIVE SUB ui_line(x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER, c AS INTEGER, lw AS INTEGER)
END SUB
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
END SUB
NATIVE SUB ui_circle(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER)
END SUB
NATIVE SUB ui_text(x AS INTEGER, y AS INTEGER, s AS STRING, c AS INTEGER, size AS INTEGER, anchor AS INTEGER)
END SUB
NATIVE SUB ui_present()
END SUB
NATIVE FUNCTION ui_scr_w() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_scr_h() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_orientation() AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_open(t AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_win_closed() AS INTEGER
END FUNCTION
NATIVE SUB ui_win_close()
END SUB
NATIVE FUNCTION ui_wait_msg(timeout AS INTEGER) AS INTEGER
END FUNCTION
NATIVE FUNCTION ui_msg_a() AS INTEGER
END FUNCTION

' ══ 变量（必须在赋值之前声明）════════════════════════════════════════
' ══ Variables (must be declared before assignment) ════════════════════════════════════
DIM w AS INTEGER
DIM h AS INTEGER
DIM ori AS INTEGER
DIM frame AS INTEGER
DIM maxf AS INTEGER
DIM msg AS INTEGER
DIM kc AS INTEGER
DIM running AS INTEGER
DIM label AS STRING
DIM bx AS INTEGER
DIM by AS INTEGER
DIM bw AS INTEGER
DIM bh AS INTEGER

' ① 问可用绘图区；拿不到就给个兜底尺寸（桌面脚手架 / 某些后端会给 0）
' ① Ask for the usable drawing area; if we cannot get it, fall back to a safe size (the desktop scaffold / some backends return 0)
w = ui_scr_w()
h = ui_scr_h()
IF w <= 0 THEN w = 360
IF h <= 0 THEN h = 620

' ② 开窗 —— 宽高直接取"可用绘图区"，于是手机上就是整页
' ② Open the window -- width and height are taken straight from the usable drawing area, so on a phone it fills the whole page
ui_win_open "BASIC UI demo", w, h

' ③ 问屏幕方向：0 = 竖屏，1 = 横屏
' ③ Ask for the screen orientation: 0 = portrait, 1 = landscape
ori = ui_orientation()
label = "orientation = " + STR$(ori)

' ④ 方向决定面板怎么摆：横屏把方块放右边，竖屏放下面
' ④ The orientation decides where the panel goes: in landscape the square goes on the right, in portrait below
bx = 24
by = 24
bw = 140
bh = 100
IF ori = 1 THEN
  bx = w - bw - 24
END IF
IF ori = 1 THEN
  by = 120
END IF
IF ori = 0 THEN
  by = h - bh - 90
END IF

maxf = 150
frame = 0
running = 1

WHILE running = 1
  ' ── 每帧重画（保留模式：场景每帧从头追加）────────────────────
  ' ── Redraw every frame (retained mode: the scene is appended from scratch each frame) ────────────────────
  ui_clear &HFF0E1418

  ' 标题
  ' Title
  ui_text 16, 16, "BASIC UI demo", &HFF9AD8FF, 20, 0
  ui_text 16, 44, label, &HFFFFD34A, 15, 0
  ui_text 16, 66, "screen " + STR$(w) + " x " + STR$(h), &HFF8A9AA8, 13, 0

  ' 图元三件套：线 / 矩形 / 圆
  ' The primitive trio: line / rectangle / circle
  ui_line 16, 92, w - 16, 92, &HFF33506A, 1
  ui_rect bx, by, bw, bh, &HFF2E7D32, 1, 0, 8
  ui_rect bx + 12, by + 12, bw - 24, bh - 24, &HFF66BB6A, 0, 2, 0
  ui_circle w / 2, h / 2, 44, &HFFE74C3C, 1, 0
  ui_circle w / 2, h / 2, 20, &HFFF1C40F, 1, 0

  ' 帧计数进度条
  ' Frame counter progress bar
  ui_rect 16, h - 62, w - 32, 14, &HFF22303C, 1, 0, 3
  ui_rect 16, h - 62, (w - 32) * frame / maxf, 14, &HFF00C2A8, 1, 0, 3
  ui_text 16, h - 42, "frame " + STR$(frame) + " / " + STR$(maxf), &HFF8A9AA8, 13, 0
  ui_text w - 16, h - 42, "press any key to exit", &HFF8A9AA8, 13, 2

  ' ── 帧边界 ─────────────────────────────────────────────────
  ' ── Frame boundary ──────────────────────────────────────────────
  ui_present
  frame = frame + 1

  ' 出口 A：画满 maxf 帧
  ' Exit A: maxf frames drawn
  IF frame >= maxf THEN
    running = 0
  END IF

  ' 出口 B：收到任意按键（消息类型 1 = KeyDown）
  ' Exit B: any key press received (message type 1 = KeyDown)
  IF running = 1 THEN
    msg = ui_wait_msg(30)
    IF msg = 1 THEN
      kc = ui_msg_a()
      running = 0
    END IF
  END IF

  ' 出口 C：用户点了标题栏的返回箭头
  ' Exit C: the user tapped the back arrow in the title bar
  IF ui_win_closed() <> 0 THEN
    running = 0
  END IF
WEND

' 正常收尾：关窗 + 一行文字说明退出原因
' Normal wrap-up: close the window + one line of text explaining why we exited
ui_win_close()
PRINT "demo_ui done (BASIC) - frames drawn: "; frame
