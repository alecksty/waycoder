' gfx_modes.bas —— SCREEN 模式与「文本 / 图形」边界（VML 的 BASIC 前端 / UI 图形后端）
'
' 这份专门盯住用户交代的那条语义：
'   **模式 0 = 文本模式（命令行），绝不弹窗**；**其它模式 = 图形模式 ⇒ 弹窗**。
' 顺带把「运行期模式」这条最难的路走一遍 —— 这里 `MODE` 是**变量**，
' 所以模式是在**运行期**决定的（老 QBasic 游戏几乎都这么写：`SCREEN Mode`）。
'
' 跑法（桌面）：
'   dotnet run --project scripts/vmlcli -- Examples/basic/gfx_modes.bas --screen 640x480 --frames out_dir
'   （每个 ui_present 落一帧 —— 因为这份程序会换好几次模式，一次 --frame 只能看到最后一帧）
'
' 预期：
'   SCREEN 1  → 320×200 的窗口（CGA 4 色调色板），画一个青色的实心方块
'   SCREEN 9  → 640×350 的窗口，画一个白色的圆
'   SCREEN 12 → 640×480 的窗口，画一条品红的对角线
'   SCREEN 13 → 320×200 的窗口，画一个亮红的实心方块
'   SCREEN 0  → **回到文本模式，不弹新窗**（后面那句 PRINT 走命令行）

NATIVE SUB ui_present ()
END SUB

DIM MODE
DIM I

' ── 运行期模式 1（CGA 320×200）──────────────────────────────
MODE = 1
SCREEN MODE
COLOR 15, 0
CLS
LINE (10, 10)-(150, 150), 1, BF
ui_present

' ── 运行期模式 9（EGA 640×350）──────────────────────────────
MODE = 9
SCREEN MODE
COLOR 15, 0
CLS
CIRCLE (320, 175), 120, 15
ui_present

' ── 运行期模式 12（VGA 640×480）─────────────────────────────
MODE = 12
SCREEN MODE
COLOR 15, 0
CLS
FOR I = 0 TO 400
  PSET (100 + I, 80 + I), 13
NEXT I
ui_present

' ── 运行期模式 13（VGA 320×200，256 色档）────────────────────
MODE = 13
SCREEN MODE
COLOR 15, 0
CLS
LINE (20, 20)-(300, 180), 12, BF
ui_present

' ── 回到文本模式：不弹窗，后面的 PRINT 走命令行 ──────────────
SCREEN 0
PRINT "back to text mode"
