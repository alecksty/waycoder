' demo_tty.bas —— BASIC 第二层：**彩色控制台**（tty）
'
' 这一层用的是 BASIC 自己的三条文本控制语句 —— `CLS` / `COLOR` / `LOCATE` ——
' 加上 `PRINT`。它们由 BASIC 前端翻译成 **ANSI 转义序列**，在桌面 vmlcli 的终端里
' 能直接看到颜色；同一份源码在手机端「命令行」页也会被 `UI/Shared/AnsiMarkup.cs`
' 翻成彩色富文本。
'
' 跑法（桌面 vmlcli）：
'   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/basic/demo_tty.bas
'
' 画面（从上到下）：
'   ① 清屏
'   ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰，便于看清暗色
'   ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
'   ④ `LOCATE` 定位：同一行先写右半段、再回到左端写左半段 —— 证明是**定位**不是顺序输出
'   ⑤ 复位成白字黑底，正常结束
'
' ⚠ **正文刻意只用 ASCII**（注释才用中文）—— 不是偷懒，是本前端一条实测缺陷：
'   **一旦用了 `COLOR`（前端切进 CrtMode），`PRINT` 就走 `SYSCALL 400 TTY_WriteChar`，
'   而那条路**按字节**送字符（`Console.Write((char)byte)`），不按 UTF-8 解码** ⇒
'   非 ASCII 文本全部碎掉。最小复现（同一份文件只差一行）：
'
'     PRINT "中文ABC"                 ⇒ `\xd6\xd0\xce\xc4ABC`（GBK 的中文，正确）
'     COLOR 7, 0 : PRINT "中文ABC"    ⇒ `??-???ABC`（乱码）
'
'   根因在 VM 运行时的 `#400` 处理器（`VMLRuntime/VMLRuntime.Syscall.cs`）：
'   `Console.Write((char)registers[0])` —— stdout 那条路走的是 `OutputChar()`（会解码 UTF-8），
'   这条不走。**手机端同样**（MAUI 没有重写 #400）。写彩色控制台程序时先按 ASCII 排，
'   等这条修好再上中文。
'
' ◆ 另外两条实测出来的坑（改这份别改回去）
'   ① **`COLOR` 必须写在 `CLS` 之后**。反过来写（`COLOR 14,1` 再 `CLS`）时 `CLS` 会先发
'      `ESC[0m` 复位，把刚设的**背景色冲掉**。实测输出对比：
'        COLOR 14,1 → CLS ⇒ `ESC[1m ESC[33m ESC[0m ESC[2J ESC[H`（**没有** `ESC[44m`）
'        CLS → COLOR 14,1 ⇒ `ESC[2J ESC[H ESC[1m ESC[33m ESC[44m`（背景色在）
'   ② `COLOR fg, bg` 的前景 8-15 会带上 `ESC[1m`（高亮位），0-7 不带 —— 这是 CGA/VGA
'      的 16 色语义，不是 bug。`LOCATE 行, 列` **行列都是 1 基**（DOS 习惯）。

' ── 变量声明（必须在赋值之前）────────────────────────────────
DIM c AS INTEGER
DIM row AS INTEGER

' ① 先清屏 —— 顺序见上面第 ① 条
CLS

' 标题：亮白字 + 蓝底
COLOR 15, 1
LOCATE 1, 1
PRINT "=== BASIC color console demo === (COLOR 15,1 = bright white on blue)"

' ② 暗色 0-7（背景 7 浅灰）
row = 3
FOR c = 0 TO 7
  LOCATE row + c, 3
  COLOR c, 7
  PRINT "fg="; c; "  dark color on light gray background (bg=7)"
NEXT c

' ③ 亮色 8-15（背景 0 黑）
row = 12
FOR c = 8 TO 15
  LOCATE row + (c - 8), 3
  COLOR c, 0
  PRINT "fg="; c; "  bright color on black background (bg=0)"
NEXT c

' ④ LOCATE 定位：先写右半段，再回到左端写左半段
LOCATE 21, 34
COLOR 14, 4
PRINT "<- written FIRST (col 34)"
LOCATE 21, 1
COLOR 11, 0
PRINT "written SECOND (col 1) -> "

' ⑤ 收尾：复位成白字黑底
LOCATE 23, 1
COLOR 7, 0
PRINT "=== done (reset to white on black) ==="
