' gfx_demo.bas —— QBasic 图形语句的 UI 后端演示（VML 的 BASIC 前端）
' gfx_demo.bas -- a demo of the UI backend for QBasic graphics statements (the VML BASIC frontend)
'
' 这个程序**不写任何内存**、也不碰 DOS 显存：SCREEN / CLS / COLOR / PSET / LINE /
' This program **writes to no memory at all** and never touches DOS video memory: SCREEN / CLS / COLOR / PSET / LINE /
' CIRCLE / PAINT 全部由前端翻译成宿主图元 ui_*（那扇绘图窗口）。
' CIRCLE / PAINT are all translated by the frontend into host primitives ui_* (that drawing window).
' 于是同一份源码在桌面 vmlcli 与手机 MAUI 上是**同一套画面**。
' So the same source gives **the very same picture** on desktop vmlcli and on phone MAUI.
'
' 跑法（桌面）：
' How to run (desktop):
'   dotnet run --project scripts/vmlcli -- Examples/basic/gfx_demo.bas --screen 640x480 --frame out.png
'
' 画面（从上到下，能一眼看出对错）：
' The picture (top to bottom, so right or wrong is obvious at a glance):
'   ① 背景是深蓝（COLOR ,1 + CLS 的效果 —— CLS 用的是**背景色索引**翻译出来的真彩）
'   ① The background is dark blue (the effect of COLOR ,1 + CLS -- CLS uses the true colour translated from the **background colour index**)
'   ② 一行 16 个色块：0-15 号调色板索引各一条竖线（LINE）
'   ② One row of 16 colour blocks: palette indices 0-15, each one a vertical line (LINE)
'   ③ 左边一个绿色空心矩形（LINE ... , B）、右边一个实心品红矩形（LINE ... , BF）
'   ③ A green hollow rectangle on the left (LINE ... , B) and a filled magenta rectangle on the right (LINE ... , BF)
'   ④ 中间一个黄色圆（CIRCLE），圆里用 PAINT 灌成青色
'   ④ A yellow circle in the middle (CIRCLE), with its interior flooded cyan by PAINT
'   ⑤ 右下角一条对角线（PSET 逐点画的，看得到虚线感）
'   ⑤ A diagonal line at the bottom right (drawn point by point with PSET, which shows a dotted feel)
'
' ⚠ 已知差异：CIRCLE 带起始/结束角（画弧）**没做** —— 本程序只画整圆，不涉及。
' ⚠ Known difference: CIRCLE with a start/end angle (drawing an arc) is **not implemented** -- this program draws whole circles only, so it does not come into play.

' ── 外部库声明（NATIVE = 裸标签，别加 sub_/func_ 前缀）──────────────
' ── External library declarations (NATIVE = a bare label, do not add a sub_/func_ prefix) ──────────────
NATIVE SUB ui_present ()
END SUB

' SCREEN 12 = 640x480 / 16 色 ⇒ 开一扇 640x480 的绘图窗口
' SCREEN 12 = 640x480 / 16 colours => opens a 640x480 drawing window
SCREEN 12

' COLOR fg, bg：前景 15（亮白）、背景 1（蓝）
' COLOR fg, bg: foreground 15 (bright white), background 1 (blue)
COLOR 15, 1
CLS

' ── ① 16 个调色板色块（LINE 竖条）────────────────────────────
' ── ① 16 palette colour blocks (LINE vertical bars) ────────────────────
DIM c
FOR c = 0 TO 15
  LINE (20 + c * 38, 20)-(20 + c * 38 + 24, 60), c, BF
NEXT c

' ── ② 空心矩形（B）与实心矩形（BF）──────────────────────────
' ── ② A hollow rectangle (B) and a filled rectangle (BF) ──────────────
COLOR 10, 1
LINE (40, 100)-(220, 220), 10, B

COLOR 13, 1
LINE (260, 100)-(440, 220), 13, BF

' ── ③ 圆 + 灌色（CIRCLE + PAINT）─────────────────────────────
' ── ③ Circle + flood fill (CIRCLE + PAINT) ─────────────────────────
COLOR 14, 1
CIRCLE (540, 160), 70, 14

COLOR 11, 1
PAINT (540, 160), 11, 14

' ── ④ PSET 逐点画一条对角线 ──────────────────────────────────
' ── ④ Drawing a diagonal point by point with PSET ─────────────────────
COLOR 15, 1
DIM i
FOR i = 0 TO 200
  PSET (60 + i, 260 + i), 15
NEXT i

' ── ⑤ 再来一个圆，验证 PAINT 的 border 判据 ───────────────────
' ── ⑤ One more circle, to exercise PAINT's border probe ────────────────
COLOR 12, 1
CIRCLE (200, 380), 60, 12
PAINT (200, 380), 12, 12

' 老 BASIC 用它当"帧边界"：告诉宿主"这一帧画完了，可以显示了"
' Old BASIC uses it as a "frame boundary": it tells the host "this frame is done, you may display it"
ui_present

' 用一行文本收尾（PRINT 仍走 CRT/命令行通道，不进绘图窗口 —— 见报告）
' Wraps up with one line of text (PRINT still goes through the CRT / command-line channel, not into the drawing window -- see the report)
PRINT "gfx_demo done"
