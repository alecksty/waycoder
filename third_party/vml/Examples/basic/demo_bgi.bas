' demo_bgi.bas —— BASIC 第三层：**传统图形接口**（BGI 那一类）
' demo_bgi.bas -- BASIC layer 3: the **classic graphics API** (the BGI family)
'
' 这一层用的是 QBasic 原生那套「固定分辨率 + 索引色」的图形语句：
' This layer uses QBasic's native "fixed resolution + indexed color" graphics statements:
' `SCREEN` / `COLOR` / `CLS` / `PSET` / `LINE` / `CIRCLE` / `PAINT`。
'
' ◆ 这层为什么还能在手机上跑
' ◆ Why this layer still runs on a phone
'
' QBasic 原生的 `SCREEN`/`LINE` 那套在老实现里是**往 DOS 内存地址写**的
' In older implementations QBasic's native `SCREEN`/`LINE` family **wrote to DOS memory addresses**
' （`0xA0000` 显存、`0x6FF0` 模式字节），本平台没有那块内存的语义 —— 手机上跑不了。
' (`0xA0000` video RAM, `0x6FF0` mode byte); this platform has no semantics for that memory -- it cannot run on a phone.
' 现在这条路已经改成由 BASIC 前端把它们**逐条翻译成宿主图元 `ui_*`**
' Now this path has been changed so that the BASIC frontend **translates them one by one into host primitives `ui_*`**
' （见 `CodeGenerator.Qbasic.Graphics.*`），于是同一份源码在桌面 vmlcli 与手机上
' (see `CodeGenerator.Qbasic.Graphics.*`), so the same source draws
' 是**同一套画面**。判据就是同目录的 `gfx_demo.bas` 能跑。
' **the same picture** on desktop vmlcli and on a phone. The probe is that `gfx_demo.bas` in this directory runs.
'
' ⚠ 正因为翻译成了 `ui_*`，`SCREEN n` 的 n **只用来决定开多大的窗口**，
' ⚠ Precisely because it is translated into `ui_*`, the n in `SCREEN n` **only decides how large a window to open**,
'   不再是"切换显存模式"；`COLOR fg, bg` 的两色也**不是** RGB，而是
'   it no longer "switches the video mode"; the two colors of `COLOR fg, bg` are **not** RGB either, but
'   与 C 侧 BGI 垫层同一张 **16 色调色板索引**（`setcolor(4)` 是"红"而不是 0x000004）。
'   the same **16-color palette index** as the C-side BGI shim (`setcolor(4)` is "red", not 0x000004).
'
' 跑法（桌面 vmlcli，默认 `--basicgfx ui`）：
' How to run (desktop vmlcli, `--basicgfx ui` by default):
'   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/basic/demo_bgi.bas \
'       --screen 640x480 --frame demo_bgi.png
'
' ⚠ 看出图请用 `--frame 单个文件`（**已经验过**：PNG 里 31 种颜色，蓝底 + 16 色块 +
' ⚠ To inspect the rendered image use `--frame` with a single file (**already measured**: 31 colors in
'   空心/实心矩形 + 灌色圆 + 白色对角线，逐项对得上）。
'   the PNG -- blue background + 16 color blocks + hollow/solid rectangles + a filled circle + a white diagonal,
'   **别用 `--frames 目录`** —— 本前端给每一条图形语句都**隐式补了一次 `ui_present`**
'   every item matching). **Do not use `--frames` with a directory** -- this frontend **implicitly adds one `ui_present`** to every
'   （实测这个程序一共 present 了 **226** 次），于是目录里前 100 多帧全是画到一半的中间态，
'   graphics statement (measured: this program presents **226** times), so the first 100-odd frames in the
'   `frame_0000.png` 甚至是一张纯黑空图（那时还没画任何东西），很容易误判成"程序没画"。
'   directory are all half-drawn intermediate states; `frame_0000.png` is even a pure black empty image (nothing drawn yet), easily mistaken for "the program drew nothing".
'
' 画面（从上到下，能一眼看出对错）：
' The picture (top to bottom, correctness is visible at a glance):
'   ① 深蓝背景（`COLOR ,1` + `CLS`）
'   ① Dark blue background (`COLOR ,1` + `CLS`)
'   ② 一行 16 个色块 —— 0-15 号调色板索引各一条实心竖条（`LINE … , BF`）
'   ② One row of 16 color blocks -- a solid vertical bar for each palette index 0-15 (`LINE ... , BF`)
'   ③ 左边绿色空心矩形（`LINE … , B`）、右边品红实心矩形（`LINE … , BF`）
'   ③ A green hollow rectangle on the left (`LINE ... , B`), a magenta solid rectangle on the right (`LINE ... , BF`)
'   ④ 中间一个黄色圆（`CIRCLE`）+ `PAINT` 灌成青色
'   ④ A yellow circle in the middle (`CIRCLE`) + `PAINT` filling it cyan
'   ⑤ 左下角一条 `PSET` 逐点画的对角线（看得到"点"的颗粒感）
'   ⑤ A diagonal in the lower left drawn point by point with `PSET` (you can see the grainy dots)
'   ⑥ 右下角一个红圆，用 `PAINT` 灌色时把**边界色**也传进去（`PAINT (x,y), 填充色, 边界色`）
'   ⑥ A red circle in the lower right, where `PAINT` is also given the **border color** (`PAINT (x,y), fill, border`)
'
' ◆ 两条本前端的写法要求
' ◆ Two syntax requirements of this frontend
'   · `LINE (x1,y1)-(x2,y2), 色, 模式` 的坐标是**成对括号**，不是四个逗号参数（QBasic 语法）。
'   · `LINE (x1,y1)-(x2,y2), color, mode` takes its coordinates as **paired parentheses**, not four comma arguments (QBasic syntax).
'     模式：`B` = 空心框、`BF` = 实心框、省略 = 单条线。
'     Mode: `B` = hollow box, `BF` = solid box, omitted = a single line.
'   · `CIRCLE (x,y), 半径, 色`；`PAINT (x,y), 填充色, 边界色`。
'   · `CIRCLE (x,y), radius, color`; `PAINT (x,y), fill, border`.
'     ⚠ `CIRCLE` 带起止角的**画弧**在本后端没做（只画整圆），本程序不涉及。
'     ⚠ Drawing an **arc** with start/end angles is not implemented in this backend (only full circles), and this program does not use it.

' ── 外部库声明（NATIVE = 裸标签，别加 sub_/func_ 前缀；空体是必须的）──────
' ── External library declaration (NATIVE = the bare label, no sub_/func_ prefix; the empty body is required) ──────
' 用它当"帧边界"：告诉宿主"这一帧画完了，可以显示了"
' Used as a "frame boundary": tells the host "this frame is finished, you may display it"
NATIVE SUB ui_present ()
END SUB

' SCREEN 12 = 640x480 / 16 色 ⇒ 开一扇 640x480 的绘图窗口
' SCREEN 12 = 640x480 / 16 colors => opens a 640x480 drawing window
SCREEN 12

' COLOR fg, bg：前景 15（亮白）、背景 1（蓝），然后清屏铺底色
' COLOR fg, bg: foreground 15 (bright white), background 1 (blue), then clear the screen to lay the background down
COLOR 15, 1
CLS

' ── 变量声明（必须在赋值之前）────────────────────────────────
' ── Variable declarations (must come before any assignment) ────────────────────────────────
DIM c AS INTEGER
DIM i AS INTEGER

' ── ① 16 个调色板色块 ───────────────────────────────────────
' ── ① 16 palette color blocks ───────────────────────────────────────
FOR c = 0 TO 15
  LINE (20 + c * 38, 20)-(20 + c * 38 + 24, 60), c, BF
NEXT c

' ── ② 空心矩形（B）与实心矩形（BF）──────────────────────────
' ── ② Hollow rectangle (B) and solid rectangle (BF) ──────────────────────────
LINE (40, 100)-(220, 220), 10, B
LINE (260, 100)-(440, 220), 13, BF

' ── ③ 圆 + 灌色（CIRCLE + PAINT）────────────────────────────
' ── ③ Circle + fill (CIRCLE + PAINT) ────────────────────────────
CIRCLE (540, 160), 70, 14
PAINT (540, 160), 11, 14

' ── ④ PSET 逐点画一条对角线 ─────────────────────────────────
' ── ④ PSET draws a diagonal point by point ─────────────────────────────────
FOR i = 0 TO 200
  PSET (60 + i, 260 + i), 15
NEXT i

' ── ⑤ 再来一个圆：PAINT 的边界色与填充色相同 ─────────────────
' ── ⑤ One more circle: the border color and the fill color given to PAINT are the same ─────────────────
CIRCLE (200, 380), 60, 12
PAINT (200, 380), 12, 12

' 帧边界标记（老 BASIC 用它告诉宿主"这一帧画完了"）
' Frame boundary marker (old BASIC used this to tell the host "this frame is finished")
ui_present

' 收尾文字走控制台（PRINT 不进绘图窗口 —— 那是另一条通道）
' The closing text goes to the console (PRINT does not enter the drawing window -- that is a separate channel)
PRINT "demo_bgi done (SCREEN 12 = 640x480, 16 palette colors)"
