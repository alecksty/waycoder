# demo_ui.py —— **第 4 层：最新 UI 接口**（`ui_*`）
# demo_ui.py — **Layer 4: the newest UI interface** (`ui_*`)
#
# 这一层和第 3 层（BGI）正好相反：
# This layer is the exact opposite of layer 3 (BGI):
#
#   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
#   · **No fixed resolution.** How big the canvas is comes from the host (`ui_scr_w()` / `ui_scr_h()`), and the
#     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
#     program lays out for whatever size it got — on a phone the gamepad area can collapse and rotating the screen changes it.
#   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。
#   · **Colors are true color** (`0xAARRGGBB`), not 16 indexed colors.
#   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait_msg` 出来，
#   · **There is a message loop.** Touches, key presses, timers, the window being closed / rotated all come out of `ui_wait_msg`, and the
#     程序按返回值分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
#     program dispatches on the return value — this is the divide between "a program you can interact with" and "it dies as soon as it finishes drawing".
#
# ## 这个 demo 演示什么
# ## What this demo demonstrates
#
#   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定排版。
#   ① **Ask the screen orientation before opening the window** (`ui_orientation()`) — the program uses it to decide the layout.
#      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
#      Orientation is a property of the **device**; do not infer it from `ui_scr_w() > ui_scr_h()` (that is the usable drawing area, and it
#      手柄收起/展开会变）。
#      changes as the gamepad is collapsed/expanded).
#   ② 开窗 → 拿画布尺寸 → 按尺寸现排（**没有写死的坐标**）。
#   ② Open the window → get the canvas size → lay out for that size (**no hard-coded coordinates**).
#   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，位置全部由 w/h 算出来。
#   ③ Draw primitives: rectangle / rounded rectangle / circle / ellipse / line / text, with every position computed from w/h.
#   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号。
#   ④ `ui_present()` submits the frame — it is the only signal that says "this frame is done".
#   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
#   ⑤ **Message loop**: a timer drives redraws, keys / touches respond, and it **exits in a bounded way**
#      （收够 N 帧 或 按任意键 / 点任意处）。
#      (after N frames, or on any key / any tap).
#
# ## 退出是有界的，这一点是刻意的
# ## The exit is bounded, and that is deliberate
#
# 没有输入也要能自己停下来：主循环数**定时器拍数**，到 `MAX_FRAMES` 就收尾。
# It must be able to stop on its own even with no input: the main loop counts **timer ticks** and wraps up at `MAX_FRAMES`.
# 手机上按任意键 / 点任意处可以提前退出。
# On a phone, any key / any tap exits early.
#
# ## ⚠ 为什么整份是平铺的（没有 def）
# ## ⚠ Why the whole thing is flat (no def)
#
# 本前端**用户函数里的赋值是局部的**（没有 `global`），而 draw 要读 `frames`/`keys`
# In this frontend, **assignments inside a user function are local** (there is no `global`), while draw needs to read `frames`/`keys`
# 这些跨帧保留的状态 ⇒ 状态要么放共享库的整数网格、要么整份平铺。
# — state that stays alive across frames ⇒ state either goes into the shared library's integer grid, or everything stays flat.
# 本 demo 选平铺（少一层依赖）。另一个理由是实测过的
# This demo chose flat (one less dependency). The other reason is the measured
# 「同一个用户函数里连着调两次 `int_to_str` 会漂」（见 demo_tty.py 的文件头），
# "calling `int_to_str` twice in a row inside one user function drifts" (see the header of demo_tty.py);
# 平铺把这类风险也一并躲开。
# being flat sidesteps that class of risk too.
#
# 又：计数器**不画成数字**，画成**长度随计数增长的条** —— 数字要转字符串，
# Also: the counters are **not drawn as numbers** but as **bars whose length grows with the count** — numbers have to be converted to strings,
# 而本前端的 `chr()` 返回的地址不是字符串、`int_to_str` 在嵌套调用里不稳。
# and in this frontend `chr()` returns an address that is not a string, and `int_to_str` is unstable in nested calls.
#
# 跑法：
# How to run:
#   手机    vml run examples/python/demo_ui.py
#   phone    vml run examples/python/demo_ui.py
#   桌面    vmlcli Examples/python/demo_ui.py --screen 640x480 --frame out.png
#   desktop  vmlcli Examples/python/demo_ui.py --screen 640x480 --frame out.png
#   喂输入  … --input 脚本（一行一条，如 `touchdown 100 200` / `keydown 27`）
#   feed input  … --input script (one entry per line, e.g. `touchdown 100 200` / `keydown 27`)

MAX_FRAMES = 30          # 到点自己停：30 拍 × 60ms ≈ 1.8 秒
# stops by itself when it is reached: 30 ticks × 60ms ≈ 1.8 seconds

# 消息类型（与 `UI/Shared/VmlUiProtocol.cs` 的 VmlMsgType 同源）
# Message types (same source as VmlMsgType in `UI/Shared/VmlUiProtocol.cs`)
MSG_KEYDOWN = 1
MSG_MOUSEDOWN = 4
MSG_TOUCHDOWN = 6
MSG_TIMER = 9
MSG_WINDOWCLOSE = 10

lang = ui_get_language()

# ── ① 开窗**之前**就问方向：程序据此决定排版 ──
# ── ① ask the orientation **before** opening the window: the program decides its layout from it ──
orient = ui_orientation()

# 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
# Open the window: use the size the host gives; fall back to portrait defaults if it is unavailable
w = ui_scr_w()
h = ui_scr_h()
if w <= 0:
    w = 360
if h <= 0:
    h = 620
ui_win_open("demo_ui (Python)", w, h)
ui_keep_on(1)

# 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
# Timer: one tick every 60ms, driving redraws (also the tick source that lets it stop on its own with no input)
tid = ui_timer_set(60, 1)

frames = 0
keys = 0
touches = 0
tx = 0 - 1          # 最近一次触摸/点击的位置（没点过就是 -1）
# most recent touch/click position (-1 if never tapped)
ty = 0
done = 0

# ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
# ── ⑤ message loop: the timer drives redraws, exits in a bounded way ──
while ui_win_closed() == 0:
    if done != 0:
        break

    t = ui_wait_msg(0)      # 阻塞等一条（有定时器在 ⇒ 一定会返回）
    # block until one message arrives (a timer is running ⇒ it always returns)

    if t == MSG_TIMER:
        frames = frames + 1
    if t == MSG_KEYDOWN:
        keys = keys + 1
        done = 1            # ← 有界：收到键就退
        # ← bounded: exits as soon as a key arrives
    if t == MSG_TOUCHDOWN:
        touches = touches + 1
        tx = ui_msg_a()     # A = x
        ty = ui_msg_b()     # B = y
        done = 1            # ← 有界：点到就退
        # ← bounded: exits as soon as a tap arrives
    if t == MSG_MOUSEDOWN:
        touches = touches + 1
        tx = ui_msg_a()
        ty = ui_msg_b()
        done = 1
    if t == MSG_WINDOWCLOSE:
        break               # 用户把窗口关了
        # the user closed the window

    # ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
    # ── draw this frame (every position is computed from w / h, no hard-coded coordinates) ──
    pad = w / 16
    cx = w / 2

    ui_clear(0xFF101018)

    ui_text(cx, 12, "UI 接口 / demo_ui (Python)" if lang == 0 else "UI interface / demo_ui (Python)", 0xFFE8E8F0, 16, 1)
    if orient == 1:
        ui_text(cx, 36, "屏幕方向 = 横屏 (LANDSCAPE)" if lang == 0 else "Orientation = LANDSCAPE", 0xFF51E86E, 13, 1)
    if orient != 1:
        ui_text(cx, 36, "屏幕方向 = 竖屏 (PORTRAIT)" if lang == 0 else "Orientation = PORTRAIT", 0xFF51E86E, 13, 1)
    ui_text(cx, 56, "画布按宿主给的尺寸现排" if lang == 0 else "Canvas laid out from the host size", 0xFF9AA0B0, 12, 1)

    # 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
    # A box that follows the size (after rotation it gets wider and shorter — that is the proof of "no hard-coded coordinates")
    ui_rect(pad, 76, w - pad * 2, 84, 0xFF1A1A24, 1, 0, 10)
    ui_rect(pad + 6, 82, w - pad * 2 - 12, 28, 0xFF4A90D9, 1, 0, 6)
    ui_text(pad + 16, 88, "rect / 圆角矩形（随屏宽伸缩）" if lang == 0 else "rect / rounded rect (follows width)", 0xFF101018, 12, 0)

    # 圆 / 椭圆 / 直线：三个基本形
    # Circle / ellipse / line: three basic shapes
    ui_circle(cx - w / 6, 200, w / 12, 0xFFE06C50, 1, 0)
    ui_ellipse(cx + w / 6, 200, w / 9, w / 18, 0xFFD9B44A, 1, 0)
    ui_line(pad, 250, w - pad, 250, 0xFF50C878, 3)

    # 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
    # A band of 8 true-color cells (these are RGB, not palette indices)
    bw = (w - pad * 2) / 8
    i = 0
    while i < 8:
        c = 0xFF4A90D9
        if i % 2 == 1:
            c = 0xFFE06C50
        ui_rect(pad + i * bw, 266, bw - 2, 18, c, 1, 0, 2)
        i = i + 1
    ui_text(cx, 296, "真彩 0xAARRGGBB（不是索引色）" if lang == 0 else "true color 0xAARRGGBB (not indexed)", 0xFF9AA0B0, 12, 1)

    # 事件计数：不画数字，画**长度随计数增长的条**（数字转字符串这条路是断的）
    # Event counters: not drawn as numbers, but as **bars whose length grows with the count** (the number-to-string path is broken)
    ui_text(pad, 336, "帧" if lang == 0 else "frames", 0xFF9AA0B0, 12, 0)
    bw2 = frames * 6
    if bw2 > w - pad * 2 - 40:
        bw2 = w - pad * 2 - 40
    ui_rect(pad + 40, 324, bw2, 14, 0xFF4A90D9, 1, 0, 3)

    ui_text(pad, 366, "按键" if lang == 0 else "keys", 0xFF9AA0B0, 12, 0)
    bw3 = keys * 30
    if bw3 > w - pad * 2 - 60:
        bw3 = w - pad * 2 - 60
    ui_rect(pad + 60, 354, bw3, 14, 0xFFE06C50, 1, 0, 3)

    ui_text(pad, 396, "触摸" if lang == 0 else "touches", 0xFF9AA0B0, 12, 0)
    bw4 = touches * 30
    if bw4 > w - pad * 2 - 60:
        bw4 = w - pad * 2 - 60
    ui_rect(pad + 60, 384, bw4, 14, 0xFFD9B44A, 1, 0, 3)

    # 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
    # Touch marker: leave a circle wherever the user taps (proving the coordinates really work)
    if tx >= 0:
        ui_circle(tx, ty, 18, 0xFFFFE060, 0, 2)
        ui_circle(tx, ty, 4, 0xFFFFE060, 1, 0)
        ui_text(cx, h - 60, "触摸坐标已经用上了" if lang == 0 else "touch coordinates in use", 0xFFFFE060, 12, 1)
    if tx < 0:
        ui_text(cx, h - 60, "点一下屏幕 / 按任意键退出" if lang == 0 else "Tap anywhere / any key to exit", 0xFF9AA0B0, 12, 1)

    ui_text(cx, h - 36, "退出：按任意键或点任意处（或等 N 帧到点）" if lang == 0 else "Exit: any key or any tap (or wait for N frames)", 0xFF9AA0B0, 12, 1)

    ui_present()

    if frames >= MAX_FRAMES:
        done = 1            # ← 有界：到点自己停
        # ← bounded: stops by itself when the frame budget runs out

# ── 收尾：定时器要杀，屏幕常亮要还回去 ──
# ── wrap-up: kill the timer, give screen-on back ──
ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()

# 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
# Leave a few **deterministic** checks for headless verification (the graphics part can only be eyeballed; these numbers can be compared automatically)
print("demo_ui: orient=", orient)
print("demo_ui: canvas=", w, "x", h)
print("demo_ui: frames=", frames, " keys=", keys, " touches=", touches)
print("demo_ui: done")
