# demo_ui.rb —— **第 4 层：最新 UI 接口**（`ui_*`）
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
# ## ⚠ 本前端的两条限制（决定了这份为什么完全平铺）
#
#   · **不能定义函数** —— 最朴素的 `def f … end` 会报
#     `Unexpected token: End(end)`（`Examples/ruby/catch.rb` 的文件头记着同一条）。
#     所以状态放顶层局部变量、主循环内联，一个 `def` 都没有。
#   · **词法器不认 `&&`**（`Unexpected char: &`）⇒ 只有嵌套 `if`，没有 `and`/`&&`。
#   · 颜色写**负数十进制**（`catch.rb` 的规矩，与 `0xAARRGGBB` 同值）：
#     `0xFF101018` = `-15724520`。`#{...}` 字符串插值也不生效，所以数字用 `print` 分次打。
#
# ## ⚠⚠ 第三条，这条是**新发现的前端缺陷**：`break` 在本前端**根本不生效**
#
# 最小复现（实测 2026-09-24）：
#
#     n = 0
#     while n < 5
#       break          # ← 期望立刻退出；实测一直转到 VM 超时被取消
#     end
#
# 连**裸 `break`**（不套 `if`）都不退，所以这不是"`break` 只在某层生效"。
# 后果：`Examples/ruby/catch.rb` 里那两处 `break`（`t == 10` 与对话框选"否"）
# **都不会退出**——它平时看起来没事，是因为窗口关掉后 `ui_win_closed()` 变成 1、
# 循环靠**条件**退出，把 `break` 那条路盖住了。
# （对照：Lua / JavaScript / R 的同形写法都对，实测 `n=3`。）
#
# ⇒ 本份**一个 `break` 都不用**：退出走一个 `alive` 标志，
#   由 `while alive == 1` 这个**循环条件**结束。
#
# 跑法：
#   手机    vml run examples/ruby/demo_ui.rb
#   桌面    vmlcli Examples/ruby/demo_ui.rb --screen 640x480 --frames /tmp/fr
#   喂输入  … --input 脚本（一行一条，如 `200 touchdown 300 200` / `150 keydown 13`）

c_bg = -15724520        # 0xFF101018
c_panel = -15066588     # 0xFF1A1A24
c_title = -1513232      # 0xFFE8E8F0
c_ok = -11409298        # 0xFF51E86E
c_dim = -6643536        # 0xFF9AA0B0
c_blue = -11890471      # 0xFF4A90D9
c_orange = -2069424     # 0xFFE06C50
c_gold = -2509750       # 0xFFD9B44A
c_green = -11483016     # 0xFF50C878
c_mark = -8096          # 0xFFFFE060

# ── ① 开窗**之前**就问方向：程序据此决定排版 ──
orient = ui_orientation()

# 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
w = ui_scr_w()
h = ui_scr_h()
if w <= 0
  w = 360
end
if h <= 0
  h = 620
end
ui_win_open("demo_ui (Ruby)", w, h)
ui_keep_on(1)

# 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
tid = ui_timer_set(60, 1)

frames = 0
keys = 0
touches = 0
tx = 0 - 1          # 最近一次触摸/点击的位置（没点过就是 -1）
ty = 0
alive = 1

# ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
# ⚠ 退出**只靠这个循环条件**（`break` 在本前端不生效，见文件头第三条）。
while alive == 1
  t = ui_wait_msg(0)      # 阻塞等一条（有定时器在 ⇒ 一定会返回）

  if t == 9
    frames = frames + 1
    if frames >= 30
      alive = 0           # ← 有界：到点自己停
    end
  end
  if t == 1
    keys = keys + 1
    alive = 0             # ← 有界：收到键就退
  end
  if t == 6
    touches = touches + 1
    tx = ui_msg_a()       # A = x
    ty = ui_msg_b()       # B = y
    alive = 0             # ← 有界：点到就退
  end
  if t == 4
    touches = touches + 1
    tx = ui_msg_a()
    ty = ui_msg_b()
    alive = 0
  end
  if t == 10
    alive = 0             # 用户把窗口关了
  end
  if ui_win_closed() != 0
    alive = 0
  end

  # ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
  pad = w / 16
  cx = w / 2

  ui_clear(c_bg)

  ui_text(cx, 12, "UI 接口 / demo_ui (Ruby)", c_title, 16, 1)
  if orient == 1
    ui_text(cx, 36, "屏幕方向 = 横屏 (LANDSCAPE)", c_ok, 13, 1)
  end
  if orient != 1
    ui_text(cx, 36, "屏幕方向 = 竖屏 (PORTRAIT)", c_ok, 13, 1)
  end
  ui_text(cx, 56, "画布按宿主给的尺寸现排", c_dim, 12, 1)

  # 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
  ui_rect(pad, 76, w - pad * 2, 84, c_panel, 1, 0, 10)
  ui_rect(pad + 6, 82, w - pad * 2 - 12, 28, c_blue, 1, 0, 6)
  ui_text(pad + 16, 88, "rect / 圆角矩形（随屏宽伸缩）", c_bg, 12, 0)

  # 圆 / 椭圆 / 直线：三个基本形
  ui_circle(cx - w / 6, 200, w / 12, c_orange, 1, 0)
  ui_ellipse(cx + w / 6, 200, w / 9, w / 18, c_gold, 1, 0)
  ui_line(pad, 250, w - pad, 250, c_green, 3)

  # 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
  bw = (w - pad * 2) / 8
  i = 0
  while i < 8
    c = c_blue
    if i % 2 == 1
      c = c_orange
    end
    ui_rect(pad + i * bw, 266, bw - 2, 18, c, 1, 0, 2)
    i = i + 1
  end
  ui_text(cx, 296, "真彩 0xAARRGGBB（不是索引色）", c_dim, 12, 1)

  # 事件计数：不画数字，画**长度随计数增长的条**（数字转字符串这条路是断的）
  ui_text(pad, 336, "帧", c_dim, 12, 0)
  bw2 = frames * 6
  if bw2 > w - pad * 2 - 40
    bw2 = w - pad * 2 - 40
  end
  ui_rect(pad + 40, 324, bw2, 14, c_blue, 1, 0, 3)

  ui_text(pad, 366, "按键", c_dim, 12, 0)
  bw3 = keys * 30
  if bw3 > w - pad * 2 - 60
    bw3 = w - pad * 2 - 60
  end
  ui_rect(pad + 60, 354, bw3, 14, c_orange, 1, 0, 3)

  ui_text(pad, 396, "触摸", c_dim, 12, 0)
  bw4 = touches * 30
  if bw4 > w - pad * 2 - 60
    bw4 = w - pad * 2 - 60
  end
  ui_rect(pad + 60, 384, bw4, 14, c_gold, 1, 0, 3)

  # 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
  if tx >= 0
    ui_circle(tx, ty, 18, c_mark, 0, 2)
    ui_circle(tx, ty, 4, c_mark, 1, 0)
    ui_text(cx, h - 60, "触摸坐标已经用上了", c_mark, 12, 1)
  end
  if tx < 0
    ui_text(cx, h - 60, "点一下屏幕 / 按任意键退出", c_dim, 12, 1)
  end

  ui_text(cx, h - 36, "退出：按任意键或点任意处（或等 N 帧到点）", c_dim, 12, 1)

  ui_present()
end

# ── 收尾：定时器要杀，屏幕常亮要还回去 ──
ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()

# 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
print("demo_ui: orient=")
print(orient)
print("\n")
print("demo_ui: canvas=")
print(w)
print(" x ")
print(h)
print("\n")
print("demo_ui: frames=")
print(frames)
print(" keys=")
print(keys)
print(" touches=")
print(touches)
print("\n")
print("demo_ui: done\n")
