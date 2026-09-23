' gfx_demo.bas —— QBasic 图形语句的 UI 后端演示（VML 的 BASIC 前端）
'
' 这个程序**不写任何内存**、也不碰 DOS 显存：SCREEN / CLS / COLOR / PSET / LINE /
' CIRCLE / PAINT 全部由前端翻译成宿主图元 ui_*（那扇绘图窗口）。
' 于是同一份源码在桌面 vmlcli 与手机 MAUI 上是**同一套画面**。
'
' 跑法（桌面）：
'   dotnet run --project scripts/vmlcli -- Examples/basic/gfx_demo.bas --screen 640x480 --frame out.png
'
' 画面（从上到下，能一眼看出对错）：
'   ① 背景是深蓝（COLOR ,1 + CLS 的效果 —— CLS 用的是**背景色索引**翻译出来的真彩）
'   ② 一行 16 个色块：0-15 号调色板索引各一条竖线（LINE）
'   ③ 左边一个绿色空心矩形（LINE ... , B）、右边一个实心品红矩形（LINE ... , BF）
'   ④ 中间一个黄色圆（CIRCLE），圆里用 PAINT 灌成青色
'   ⑤ 右下角一条对角线（PSET 逐点画的，看得到虚线感）
'
' ⚠ 已知差异：CIRCLE 带起始/结束角（画弧）**没做** —— 本程序只画整圆，不涉及。

' ── 外部库声明（NATIVE = 裸标签，别加 sub_/func_ 前缀）──────────────
NATIVE SUB ui_present ()
END SUB

' SCREEN 12 = 640x480 / 16 色 ⇒ 开一扇 640x480 的绘图窗口
SCREEN 12

' COLOR fg, bg：前景 15（亮白）、背景 1（蓝）
COLOR 15, 1
CLS

' ── ① 16 个调色板色块（LINE 竖条）────────────────────────────
DIM c
FOR c = 0 TO 15
  LINE (20 + c * 38, 20)-(20 + c * 38 + 24, 60), c, BF
NEXT c

' ── ② 空心矩形（B）与实心矩形（BF）──────────────────────────
COLOR 10, 1
LINE (40, 100)-(220, 220), 10, B

COLOR 13, 1
LINE (260, 100)-(440, 220), 13, BF

' ── ③ 圆 + 灌色（CIRCLE + PAINT）─────────────────────────────
COLOR 14, 1
CIRCLE (540, 160), 70, 14

COLOR 11, 1
PAINT (540, 160), 11, 14

' ── ④ PSET 逐点画一条对角线 ──────────────────────────────────
COLOR 15, 1
DIM i
FOR i = 0 TO 200
  PSET (60 + i, 260 + i), 15
NEXT i

' ── ⑤ 再来一个圆，验证 PAINT 的 border 判据 ───────────────────
COLOR 12, 1
CIRCLE (200, 380), 60, 12
PAINT (200, 380), 12, 12

' 老 BASIC 用它当"帧边界"：告诉宿主"这一帧画完了，可以显示了"
ui_present

' 用一行文本收尾（PRINT 仍走 CRT/命令行通道，不进绘图窗口 —— 见报告）
PRINT "gfx_demo done"
