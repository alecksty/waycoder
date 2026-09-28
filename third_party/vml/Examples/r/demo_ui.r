# demo_ui.r —— **第 4 层：最新 UI 接口**（`ui_*`）
# demo_ui.r -- **layer 4: the newest UI interface** (`ui_*`)
#
# 这一层和第 3 层（BGI）正好相反：
# This layer is the exact opposite of layer 3 (BGI):
#
#   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
#   · **There is no fixed resolution**. The canvas size is given by the host (`ui_scr_w()` / `ui_scr_h()`),
#     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
#     and the program lays itself out for the size it gets -- on a phone the collapsible gamepad area and screen rotation change it.
#   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。
#   · **Colors are true color** (`0xAARRGGBB`), not 16 indexed colors.
#   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait_msg` 出来，
#   · **There is a message loop**. Touch, keys, timers, the window being closed or rotated all come out of `ui_wait_msg`,
#     程序按返回值分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
#     and the program dispatches on the return value -- the watershed between "an interactive program" and "draw then die".
#
# ## 这个 demo 演示什么
# ## What this demo shows
#
#   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定排版。
#   ① **Ask the screen orientation before opening the window** (`ui_orientation()`) -- the program lays out accordingly.
#      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
#      Orientation is a property of the **device**; do not infer it from `ui_scr_w() > ui_scr_h()` (that is the usable drawing area,
#      手柄收起/展开会变）。
#      which changes when the gamepad is collapsed or expanded).
#   ② 开窗 → 拿画布尺寸 → 按尺寸现排（**没有写死的坐标**）。
#   ② Open the window -> get the canvas size -> lay out for that size (**no hard-coded coordinates**).
#   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，位置全部由 w/h 算出来。
#   ③ Draw primitives: rectangle / rounded rectangle / circle / ellipse / line / text, all positioned from w/h.
#   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号。
#   ④ `ui_present()` hands over the frame -- it is the only signal that says "this frame is done".
#   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
#   ⑤ **A message loop**: the timer drives redraws, keys / touches respond, and the exit is **bounded**
#      （收够 N 帧 或 按任意键 / 点任意处）。
#      (after N frames, or on any key / tap).
#
# ## ⚠ 本前端的两条限制（决定了几处写法）
# ## ⚠ Two limitations of this frontend (they dictate a few of the choices here)
#
#   · **`cat` 只打印第一个实参** + **没有 `paste`** ⇒ 数字没法拼进字符串。
#   · **`cat` prints only the first argument** + **there is no `paste`** => numbers cannot be joined into a string.
#     所以计数器**不画数字**，画**长度随计数增长的条**；收尾的统计行
#     So the counters **do not draw digits** but a **bar whose length grows with the count**; the closing statistics line
#     用「一次一个实参」的 `cat` 逐步打（`cat("x=")` 然后 `cat(x)`）。
#     is printed step by step with a "one argument per call" `cat` (`cat("x=")` then `cat(x)`).
#   · 颜色写**负数十进制**（`catch.r` 的规矩，与 `0xAARRGGBB` 同值）：
#   · Colors are written as **negative decimals** (the rule from `catch.r`, equal to `0xAARRGGBB`):
#     `0xFF101018` = `-15724520`。
#   · R 的向量是 **1 基**的，循环也别写 `for (i in 1:4)`（`:` 会被解析成 `CALL func_:`
#   · R vectors are **1-based**, and loops must not use `for (i in 1:4)` either (`:` is parsed into `CALL func_:`
#     而链接期报未定义）—— 本份一律用 `while`。
#     and link time reports it as undefined) -- this file always uses `while`.
#   · 好消息：`break` 在本前端**是好的**（实测 `while` 里嵌 `if` 再 `break` 正常退出）。
#   · Good news: `break` **works** on this frontend (measured: an `if` with a `break` inside a `while` exits properly).
#
# 跑法：
# How to run:
#   手机    vml run examples/r/demo_ui.r
#   phone    vml run examples/r/demo_ui.r
#   桌面    vmlcli Examples/r/demo_ui.r --screen 640x480 --frames /tmp/fr
#   desktop  vmlcli Examples/r/demo_ui.r --screen 640x480 --frames /tmp/fr
#   喂输入  … --input 脚本（一行一条，如 `200 touchdown 300 200` / `150 keydown 13`）
#   input    ... --input <script> (one per line, e.g. `200 touchdown 300 200` / `150 keydown 13`)

c_bg <- -15724520        # 0xFF101018
c_panel <- -15066588     # 0xFF1A1A24
c_title <- -1513232      # 0xFFE8E8F0
c_ok <- -11409298        # 0xFF51E86E
c_dim <- -6643536        # 0xFF9AA0B0
c_blue <- -11890471      # 0xFF4A90D9
c_orange <- -2069424     # 0xFFE06C50
c_gold <- -2509750       # 0xFFD9B44A
c_green <- -11483016     # 0xFF50C878
c_mark <- -8096          # 0xFFFFE060

# ── ① 开窗**之前**就问方向：程序据此决定排版 ──
# -- ① Ask the orientation **before** opening the window: the program lays out accordingly --
orient <- ui_orientation()

# 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
# Open the window: take the size the host gives, falling back to the portrait default if unavailable
w <- ui_scr_w()
h <- ui_scr_h()
if (w <= 0) {
  w <- 360
}
if (h <= 0) {
  h <- 620
}
ui_win_open("demo_ui (R)", w, h)
ui_keep_on(1)

# 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
# Timer: one tick every 60ms, driving redraws (also the tick source that lets it stop on its own with no input)
tid <- ui_timer_set(60, 1)

frames <- 0
keys <- 0
touches <- 0
tx <- 0 - 1          # 最近一次触摸/点击的位置（没点过就是 -1）
# Most recent touch/click position (-1 if never touched)
ty <- 0
done <- 0

# ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
# -- ⑤ Message loop: the timer drives redraws, the exit is bounded --
while (ui_win_closed() == 0) {
  if (done != 0) {
    break
  }

  t <- ui_wait_msg(0)      # 阻塞等一条（有定时器在 ⇒ 一定会返回）
  # Block for one message (a timer is running => it always returns)

  if (t == 9) {
    frames <- frames + 1
  }
  if (t == 1) {
    keys <- keys + 1
    done <- 1              # ← 有界：收到键就退
    # <- Bounded: exit on a key press
  }
  if (t == 6) {
    touches <- touches + 1
    tx <- ui_msg_a()       # A = x
    ty <- ui_msg_b()       # B = y
    done <- 1              # ← 有界：点到就退
    # <- Bounded: exit on a tap
  }
  if (t == 4) {
    touches <- touches + 1
    tx <- ui_msg_a()
    ty <- ui_msg_b()
    done <- 1
  }
  if (t == 10) {
    break                  # 用户把窗口关了
    # The user closed the window
  }

  # ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
  # -- Draw this frame (every position is computed from w / h; no hard-coded coordinates) --
  pad <- w / 16
  cx <- w / 2

  ui_clear(c_bg)

  ui_text(cx, 12, "UI 接口 / demo_ui (R)", c_title, 16, 1)
  if (orient == 1) {
    ui_text(cx, 36, "屏幕方向 = 横屏 (LANDSCAPE)", c_ok, 13, 1)
  }
  if (orient != 1) {
    ui_text(cx, 36, "屏幕方向 = 竖屏 (PORTRAIT)", c_ok, 13, 1)
  }
  ui_text(cx, 56, "画布按宿主给的尺寸现排", c_dim, 12, 1)

  # 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
  # A box that follows the size (after rotating it becomes wider and shorter -- proof of "no hard-coded coordinates")
  ui_rect(pad, 76, w - pad * 2, 84, c_panel, 1, 0, 10)
  ui_rect(pad + 6, 82, w - pad * 2 - 12, 28, c_blue, 1, 0, 6)
  ui_text(pad + 16, 88, "rect / 圆角矩形（随屏宽伸缩）", c_bg, 12, 0)

  # 圆 / 椭圆 / 直线：三个基本形
  # Circle / ellipse / line: the three basic shapes
  ui_circle(cx - w / 6, 200, w / 12, c_orange, 1, 0)
  ui_ellipse(cx + w / 6, 200, w / 9, w / 18, c_gold, 1, 0)
  ui_line(pad, 250, w - pad, 250, c_green, 3)

  # 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
  # An 8-cell true-color band (these are RGB, not palette indexes)
  bw <- (w - pad * 2) / 8
  i <- 0
  while (i < 8) {
    c <- c_blue
    if (i %% 2 == 1) {
      c <- c_orange
    }
    ui_rect(pad + i * bw, 266, bw - 2, 18, c, 1, 0, 2)
    i <- i + 1
  }
  ui_text(cx, 296, "真彩 0xAARRGGBB（不是索引色）", c_dim, 12, 1)

  # 事件计数：不画数字，画**长度随计数增长的条**（数字转不进字符串，见文件头）
  # Event counts: no digits are drawn, but a **bar whose length grows with the count** (digits cannot be joined into a string, see the file header)
  ui_text(pad, 336, "帧", c_dim, 12, 0)
  bw2 <- frames * 6
  if (bw2 > w - pad * 2 - 40) {
    bw2 <- w - pad * 2 - 40
  }
  ui_rect(pad + 40, 324, bw2, 14, c_blue, 1, 0, 3)

  ui_text(pad, 366, "按键", c_dim, 12, 0)
  bw3 <- keys * 30
  if (bw3 > w - pad * 2 - 60) {
    bw3 <- w - pad * 2 - 60
  }
  ui_rect(pad + 60, 354, bw3, 14, c_orange, 1, 0, 3)

  ui_text(pad, 396, "触摸", c_dim, 12, 0)
  bw4 <- touches * 30
  if (bw4 > w - pad * 2 - 60) {
    bw4 <- w - pad * 2 - 60
  }
  ui_rect(pad + 60, 384, bw4, 14, c_gold, 1, 0, 3)

  # 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
  # Touch marker: a circle is left wherever you tap (proving the coordinates really work)
  if (tx >= 0) {
    ui_circle(tx, ty, 18, c_mark, 0, 2)
    ui_circle(tx, ty, 4, c_mark, 1, 0)
    ui_text(cx, h - 60, "触摸坐标已经用上了", c_mark, 12, 1)
  }
  if (tx < 0) {
    ui_text(cx, h - 60, "点一下屏幕 / 按任意键退出", c_dim, 12, 1)
  }

  ui_text(cx, h - 36, "退出：按任意键或点任意处（或等 N 帧到点）", c_dim, 12, 1)

  ui_present()

  if (frames >= 30) {
    done <- 1              # ← 有界：到点自己停
    # <- Bounded: it stops by itself once the frame count is reached
  }
}

# ── 收尾：定时器要杀，屏幕常亮要还回去 ──
# -- Wrap-up: kill the timer and give back keep-screen-on --
ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()

# 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
# A few **deterministic** checks are left for headless verification (the graphics can only be eyeballed; these numbers can be compared automatically)
cat("demo_ui: orient=")
cat(orient)
cat("\n")
cat("demo_ui: canvas=")
cat(w)
cat(" x ")
cat(h)
cat("\n")
cat("demo_ui: frames=")
cat(frames)
cat(" keys=")
cat(keys)
cat(" touches=")
cat(touches)
cat("\n")
cat("demo_ui: done\n")
