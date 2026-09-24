-- demo_ui.lua —— **第 4 层：最新 UI 接口**（`ui_*`）
--
-- 这一层和第 3 层（BGI）正好相反：
--
--   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w()` / `ui_scr_h()`），
--     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
--   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。
--   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait_msg` 出来，
--     程序按返回值分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
--
-- ## 这个 demo 演示什么
--
--   ① **开窗前先问屏幕方向**（`ui_orientation()`）—— 程序据此决定排版。
--      方向是**设备**的属性，别拿 `ui_scr_w() > ui_scr_h()` 去推（那是可用绘图区，
--      手柄收起/展开会变）。
--   ② 开窗 → 拿画布尺寸 → 按尺寸现排（**没有写死的坐标**）。
--   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，位置全部由 w/h 算出来。
--   ④ `ui_present()` 交帧 —— 它是"这一帧画完了"的唯一信号。
--   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
--      （收够 N 帧 或 按任意键 / 点任意处）。
--
-- ## ⚠ 本前端的两条限制（决定了几处写法）
--
--   · **`..` 字符串拼接编不过**（链到库里没有的 `lua_concat`）⇒ 颜色与坐标一律写字面量。
--   · 颜色写**负数十进制**（`life.lua` 的规矩，与 `0xAARRGGBB` 同值）：
--     `0xFF101018` = `-15724520`。
--   · 循环变量**先 `local` 声明**再进循环（v0.96.202 修的就是这条）。
--   · 好消息：`break` 在本前端**是好的**（实测 `while` 里嵌 `if` 再 `break` 正常退出）。
--
-- 跑法：
--   手机    vml run examples/lua/demo_ui.lua
--   桌面    vmlcli Examples/lua/demo_ui.lua --screen 640x480 --frames /tmp/fr
--   喂输入  … --input 脚本（一行一条，如 `200 touchdown 300 200` / `150 keydown 13`）

local c_bg = -15724520        -- 0xFF101018
local c_panel = -15066588     -- 0xFF1A1A24
local c_title = -1513232      -- 0xFFE8E8F0
local c_ok = -11409298        -- 0xFF51E86E
local c_dim = -6643536        -- 0xFF9AA0B0
local c_blue = -11890471      -- 0xFF4A90D9
local c_orange = -2069424     -- 0xFFE06C50
local c_gold = -2509750       -- 0xFFD9B44A
local c_green = -11483016     -- 0xFF50C878
local c_mark = -8096          -- 0xFFFFE060

-- ── ① 开窗**之前**就问方向：程序据此决定排版 ──
local orient = ui_orientation()

-- 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
local w = ui_scr_w()
local h = ui_scr_h()
if w <= 0 then
  w = 360
end
if h <= 0 then
  h = 620
end
ui_win_open("demo_ui (Lua)", w, h)
ui_keep_on(1)

-- 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
local tid = ui_timer_set(60, 1)

local frames = 0
local keys = 0
local touches = 0
local tx = 0 - 1          -- 最近一次触摸/点击的位置（没点过就是 -1）
local ty = 0
local done = 0

-- ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
while ui_win_closed() == 0 do
  if done ~= 0 then
    break
  end

  local t = ui_wait_msg(0)      -- 阻塞等一条（有定时器在 ⇒ 一定会返回）

  if t == 9 then
    frames = frames + 1
  end
  if t == 1 then
    keys = keys + 1
    done = 1                    -- ← 有界：收到键就退
  end
  if t == 6 then
    touches = touches + 1
    tx = ui_msg_a()             -- A = x
    ty = ui_msg_b()             -- B = y
    done = 1                    -- ← 有界：点到就退
  end
  if t == 4 then
    touches = touches + 1
    tx = ui_msg_a()
    ty = ui_msg_b()
    done = 1
  end
  if t == 10 then
    break                       -- 用户把窗口关了
  end

  -- ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
  local pad = w / 16
  local cx = w / 2

  ui_clear(c_bg)

  ui_text(cx, 12, "UI 接口 / demo_ui (Lua)", c_title, 16, 1)
  if orient == 1 then
    ui_text(cx, 36, "屏幕方向 = 横屏 (LANDSCAPE)", c_ok, 13, 1)
  end
  if orient ~= 1 then
    ui_text(cx, 36, "屏幕方向 = 竖屏 (PORTRAIT)", c_ok, 13, 1)
  end
  ui_text(cx, 56, "画布按宿主给的尺寸现排", c_dim, 12, 1)

  -- 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
  ui_rect(pad, 76, w - pad * 2, 84, c_panel, 1, 0, 10)
  ui_rect(pad + 6, 82, w - pad * 2 - 12, 28, c_blue, 1, 0, 6)
  ui_text(pad + 16, 88, "rect / 圆角矩形（随屏宽伸缩）", c_bg, 12, 0)

  -- 圆 / 椭圆 / 直线：三个基本形
  ui_circle(cx - w / 6, 200, w / 12, c_orange, 1, 0)
  ui_ellipse(cx + w / 6, 200, w / 9, w / 18, c_gold, 1, 0)
  ui_line(pad, 250, w - pad, 250, c_green, 3)

  -- 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
  local bw = (w - pad * 2) / 8
  local i = 0
  while i < 8 do
    local c = c_blue
    if i % 2 == 1 then
      c = c_orange
    end
    ui_rect(pad + i * bw, 266, bw - 2, 18, c, 1, 0, 2)
    i = i + 1
  end
  ui_text(cx, 296, "真彩 0xAARRGGBB（不是索引色）", c_dim, 12, 1)

  -- 事件计数：不画数字，画**长度随计数增长的条**
  -- （`..` 用不了 ⇒ 数字没法拼进文本；画成条反而一眼看出"事件到了没有"）
  ui_text(pad, 336, "帧", c_dim, 12, 0)
  local bw2 = frames * 6
  if bw2 > w - pad * 2 - 40 then
    bw2 = w - pad * 2 - 40
  end
  ui_rect(pad + 40, 324, bw2, 14, c_blue, 1, 0, 3)

  ui_text(pad, 366, "按键", c_dim, 12, 0)
  local bw3 = keys * 30
  if bw3 > w - pad * 2 - 60 then
    bw3 = w - pad * 2 - 60
  end
  ui_rect(pad + 60, 354, bw3, 14, c_orange, 1, 0, 3)

  ui_text(pad, 396, "触摸", c_dim, 12, 0)
  local bw4 = touches * 30
  if bw4 > w - pad * 2 - 60 then
    bw4 = w - pad * 2 - 60
  end
  ui_rect(pad + 60, 384, bw4, 14, c_gold, 1, 0, 3)

  -- 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
  if tx >= 0 then
    ui_circle(tx, ty, 18, c_mark, 0, 2)
    ui_circle(tx, ty, 4, c_mark, 1, 0)
    ui_text(cx, h - 60, "触摸坐标已经用上了", c_mark, 12, 1)
  end
  if tx < 0 then
    ui_text(cx, h - 60, "点一下屏幕 / 按任意键退出", c_dim, 12, 1)
  end

  ui_text(cx, h - 36, "退出：按任意键或点任意处（或等 N 帧到点）", c_dim, 12, 1)

  ui_present()

  if frames >= 30 then
    done = 1                    -- ← 有界：到点自己停
  end
end

-- ── 收尾：定时器要杀，屏幕常亮要还回去 ──
ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()

-- 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
print("demo_ui: orient=", orient)
print("demo_ui: canvas=", w, " x ", h)
print("demo_ui: frames=", frames, " keys=", keys, " touches=", touches)
print("demo_ui: done")
