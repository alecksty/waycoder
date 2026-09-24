# demo_ui.py —— **第 4 层：最新 UI 接口**（`ui_*`）
#
# 这一层和第 3 层（BGI）正好相反：
#
#   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
#     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
#   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。
#   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait_msg` 出来，
#     程序按返回值分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
#
# ## 这个 demo 演示什么
#
#   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定排版。
#      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
#      手柄收起/展开会变）。
#   ② 开窗 → 拿画布尺寸 → 按尺寸现排（**没有写死的坐标**）。
#   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，位置全部由 w/h 算出来。
#   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号。
#   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
#      （收够 N 帧 或 按任意键 / 点任意处）。
#
# ## 退出是有界的，这一点是刻意的
#
# 没有输入也要能自己停下来：主循环数**定时器拍数**，到 `MAX_FRAMES` 就收尾。
# 手机上按任意键 / 点任意处可以提前退出。
#
# ## ⚠ 为什么整份是平铺的（没有 def）
#
# 本前端**用户函数里的赋值是局部的**（没有 `global`），而 draw 要读 `frames`/`keys`
# 这些跨帧保留的状态 ⇒ 状态要么放共享库的整数网格、要么整份平铺。
# 本 demo 选平铺（少一层依赖）。另一个理由是实测过的
# 「同一个用户函数里连着调两次 `int_to_str` 会漂」（见 demo_tty.py 的文件头），
# 平铺把这类风险也一并躲开。
#
# 又：计数器**不画成数字**，画成**长度随计数增长的条** —— 数字要转字符串，
# 而本前端的 `chr()` 返回的地址不是字符串、`int_to_str` 在嵌套调用里不稳。
#
# 跑法：
#   手机    vml run examples/python/demo_ui.py
#   桌面    vmlcli Examples/python/demo_ui.py --screen 640x480 --frame out.png
#   喂输入  … --input 脚本（一行一条，如 `touchdown 100 200` / `keydown 27`）

MAX_FRAMES = 30          # 到点自己停：30 拍 × 60ms ≈ 1.8 秒

# 消息类型（与 `UI/Shared/VmlUiProtocol.cs` 的 VmlMsgType 同源）
MSG_KEYDOWN = 1
MSG_MOUSEDOWN = 4
MSG_TOUCHDOWN = 6
MSG_TIMER = 9
MSG_WINDOWCLOSE = 10

# ── ① 开窗**之前**就问方向：程序据此决定排版 ──
orient = ui_orientation()

# 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
w = ui_scr_w()
h = ui_scr_h()
if w <= 0:
    w = 360
if h <= 0:
    h = 620
ui_win_open("demo_ui (Python)", w, h)
ui_keep_on(1)

# 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
tid = ui_timer_set(60, 1)

frames = 0
keys = 0
touches = 0
tx = 0 - 1          # 最近一次触摸/点击的位置（没点过就是 -1）
ty = 0
done = 0

# ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
while ui_win_closed() == 0:
    if done != 0:
        break

    t = ui_wait_msg(0)      # 阻塞等一条（有定时器在 ⇒ 一定会返回）

    if t == MSG_TIMER:
        frames = frames + 1
    if t == MSG_KEYDOWN:
        keys = keys + 1
        done = 1            # ← 有界：收到键就退
    if t == MSG_TOUCHDOWN:
        touches = touches + 1
        tx = ui_msg_a()     # A = x
        ty = ui_msg_b()     # B = y
        done = 1            # ← 有界：点到就退
    if t == MSG_MOUSEDOWN:
        touches = touches + 1
        tx = ui_msg_a()
        ty = ui_msg_b()
        done = 1
    if t == MSG_WINDOWCLOSE:
        break               # 用户把窗口关了

    # ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
    pad = w / 16
    cx = w / 2

    ui_clear(0xFF101018)

    ui_text(cx, 12, "UI 接口 / demo_ui (Python)", 0xFFE8E8F0, 16, 1)
    if orient == 1:
        ui_text(cx, 36, "屏幕方向 = 横屏 (LANDSCAPE)", 0xFF51E86E, 13, 1)
    if orient != 1:
        ui_text(cx, 36, "屏幕方向 = 竖屏 (PORTRAIT)", 0xFF51E86E, 13, 1)
    ui_text(cx, 56, "画布按宿主给的尺寸现排", 0xFF9AA0B0, 12, 1)

    # 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
    ui_rect(pad, 76, w - pad * 2, 84, 0xFF1A1A24, 1, 0, 10)
    ui_rect(pad + 6, 82, w - pad * 2 - 12, 28, 0xFF4A90D9, 1, 0, 6)
    ui_text(pad + 16, 88, "rect / 圆角矩形（随屏宽伸缩）", 0xFF101018, 12, 0)

    # 圆 / 椭圆 / 直线：三个基本形
    ui_circle(cx - w / 6, 200, w / 12, 0xFFE06C50, 1, 0)
    ui_ellipse(cx + w / 6, 200, w / 9, w / 18, 0xFFD9B44A, 1, 0)
    ui_line(pad, 250, w - pad, 250, 0xFF50C878, 3)

    # 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
    bw = (w - pad * 2) / 8
    i = 0
    while i < 8:
        c = 0xFF4A90D9
        if i % 2 == 1:
            c = 0xFFE06C50
        ui_rect(pad + i * bw, 266, bw - 2, 18, c, 1, 0, 2)
        i = i + 1
    ui_text(cx, 296, "真彩 0xAARRGGBB（不是索引色）", 0xFF9AA0B0, 12, 1)

    # 事件计数：不画数字，画**长度随计数增长的条**（数字转字符串这条路是断的）
    ui_text(pad, 336, "帧", 0xFF9AA0B0, 12, 0)
    bw2 = frames * 6
    if bw2 > w - pad * 2 - 40:
        bw2 = w - pad * 2 - 40
    ui_rect(pad + 40, 324, bw2, 14, 0xFF4A90D9, 1, 0, 3)

    ui_text(pad, 366, "按键", 0xFF9AA0B0, 12, 0)
    bw3 = keys * 30
    if bw3 > w - pad * 2 - 60:
        bw3 = w - pad * 2 - 60
    ui_rect(pad + 60, 354, bw3, 14, 0xFFE06C50, 1, 0, 3)

    ui_text(pad, 396, "触摸", 0xFF9AA0B0, 12, 0)
    bw4 = touches * 30
    if bw4 > w - pad * 2 - 60:
        bw4 = w - pad * 2 - 60
    ui_rect(pad + 60, 384, bw4, 14, 0xFFD9B44A, 1, 0, 3)

    # 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
    if tx >= 0:
        ui_circle(tx, ty, 18, 0xFFFFE060, 0, 2)
        ui_circle(tx, ty, 4, 0xFFFFE060, 1, 0)
        ui_text(cx, h - 60, "触摸坐标已经用上了", 0xFFFFE060, 12, 1)
    if tx < 0:
        ui_text(cx, h - 60, "点一下屏幕 / 按任意键退出", 0xFF9AA0B0, 12, 1)

    ui_text(cx, h - 36, "退出：按任意键或点任意处（或等 N 帧到点）", 0xFF9AA0B0, 12, 1)

    ui_present()

    if frames >= MAX_FRAMES:
        done = 1            # ← 有界：到点自己停

# ── 收尾：定时器要杀，屏幕常亮要还回去 ──
ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()

# 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
print("demo_ui: orient=", orient)
print("demo_ui: canvas=", w, "x", h)
print("demo_ui: frames=", frames, " keys=", keys, " touches=", touches)
print("demo_ui: done")
