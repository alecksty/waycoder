# demo_tty.r —— **第 2 层：彩色控制台**
# demo_tty.r -- **layer 2: color console**
#
# 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
# The problem this layer solves is "**laying out a character display**": clear the screen, move the cursor to a given row and column,
# 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
# set foreground/background colors. Old DOS programs (menus, tables, status bars) were built entirely from these primitives.
#
# R 这一层**没有 conio/crt 绑定**（`Lib/r/console.r` 那几个 `console_*` 是文档性存根），
# R has **no conio/crt binding** at this layer (the `console_*` entries in `Lib/r/console.r` are documentary stubs),
# 所以这里**直接输出 ANSI 转义序列** —— 那正是"支持彩色的控制台程序"：
# so this file **emits ANSI escape sequences directly** -- which is exactly a "console program with color support":
#
#     清屏        ESC [ 2 J          ESC [ H
#     clear screen        ESC [ 2 J          ESC [ H
#     定位光标    ESC [ <行> ; <列> H
#     position cursor     ESC [ <row> ; <col> H
#     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
#     foreground/background  ESC [ 3x m / 4x m / 9x m / 10x m, 256 color 38;5;N, true color 38;2;r;g;b
#     复位        ESC [ 0 m
#     reset        ESC [ 0 m
#
# ## ⚠ 本前端的两条限制（写 demo 时避开）
# ## ⚠ Two limitations of this frontend (avoid them when writing a demo)
#
#   · **`cat` 只打印第一个实参** —— `cat("\x1b[31m", " 红 ")` 只会打出转义。
#   · **`cat` prints only the first argument** -- `cat("\x1b[31m", " red ")` prints only the escape.
#     （`Examples/r/catch.r` 的文件头记着同一条。）所以**一个 `cat` 恰好一个实参**。
#     (The header of `Examples/r/catch.r` notes the same thing.) So **one `cat` has exactly one argument**.
#   · **没有 `paste`**（`未定义的函数 'func_paste'`）⇒ 数字没法拼进字符串里，
#   · **There is no `paste`** ("undefined function 'func_paste'") => numbers cannot be joined into a string,
#     坐标只能写成字面量。
#     so coordinates can only be written as literals.
#   · `\x1b` 在**字符串字面量里是支持的**，所以可以内联 ESC（本前端不认 `0x` 开头的
#   · `\x1b` **is supported inside string literals**, so ESC can be inlined (this frontend does not accept integers
#     整数，颜色要写负数十进制 —— 那是绘图那一层的规矩，这里用不上）。
#     starting with `0x`; colors must be negative decimals -- that is the drawing layer's rule and is not needed here).
#
# ## 在哪儿能看到什么
# ## What you can see where
#
# · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
# · **A real terminal** (`vmlcli ... | cat -v`): the whole escape set takes effect.
# · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
# · **The phone's "command line" page**: that layer turns ANSI on stdout into the repo's unified `«»` intermediate format
#   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
#   (`UI/Shared/AnsiMarkup.cs`) and **understands only SGR (color/style)**; cursor positioning (`ESC[...H`)
#   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
#   and clearing the screen (`ESC[2J`) are **eaten** -- a deliberately limited subset (see
#   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
#   the notes in `Examples/c/ansi_colors.c`). So every line ends with a newline,
#   定位被吃掉时输出仍然一行一句、读得通。
#   and when positioning is eaten the output is still one sentence per line and readable.
#
# 跑法：命令行页输入  vml run examples/r/demo_tty.r
# How to run: type this into the command-line page:  vml run examples/r/demo_tty.r

# ── 开场：清屏 + 回左上角 ───────────────────────────────────────
# -- Opening: clear the screen + go back to the top left --
lang <- ui_get_language()

cat("\x1b[2J")
cat("\x1b[H")

# ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
# -- Title bar: white on blue (ESC[44;97m) --
cat("\x1b[1;1H")
cat("\x1b[44;97m")
if (lang == 0) cat("  demo_tty (R) —— 彩色控制台 / ANSI 转义序列               ") else cat("  demo_tty (R) -- color console / ANSI escapes               ")
cat("\x1b[0m")
cat("\n")

cat("\x1b[2;1H")
cat("\x1b[90m")
if (lang == 0) cat("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m") else cat("clear ESC[2J   cursor ESC[r;cH   color ESC[3xm / ESC[4xm   reset ESC[0m")
cat("\x1b[0m")
cat("\n")

# ── 标准 8 色前景（30–37）──────────────────────────────────────
# -- The standard 8 foreground colors (30-37) --
cat("\x1b[4;1H")
cat("\x1b[1;37m")
if (lang == 0) cat("标准 8 色前景：") else cat("Standard 8 foreground colors:")
cat("\x1b[0m")
cat("\n")

cat("\x1b[5;1H")
cat("\x1b[30m")
if (lang == 0) cat(" 30 黑 ") else cat(" 30 black ")
cat("\x1b[0m")
cat("\x1b[31m")
if (lang == 0) cat(" 31 红 ") else cat(" 31 red ")
cat("\x1b[0m")
cat("\x1b[32m")
if (lang == 0) cat(" 32 绿 ") else cat(" 32 green ")
cat("\x1b[0m")
cat("\x1b[33m")
if (lang == 0) cat(" 33 黄 ") else cat(" 33 yellow ")
cat("\x1b[0m")
cat("\x1b[34m")
if (lang == 0) cat(" 34 蓝 ") else cat(" 34 blue ")
cat("\x1b[0m")
cat("\x1b[35m")
if (lang == 0) cat(" 35 品红 ") else cat(" 35 magenta ")
cat("\x1b[0m")
cat("\x1b[36m")
if (lang == 0) cat(" 36 青 ") else cat(" 36 cyan ")
cat("\x1b[0m")
cat("\x1b[37m")
if (lang == 0) cat(" 37 白 ") else cat(" 37 white ")
cat("\x1b[0m")
cat("\n")

# ── 亮色前景（90–97）────────────────────────────────────────────
# -- Bright foreground colors (90-97) --
cat("\x1b[6;1H")
cat("\x1b[90m")
if (lang == 0) cat(" 90 亮黑(灰) ") else cat(" 90 gray ")
cat("\x1b[0m")
cat("\x1b[91m")
if (lang == 0) cat(" 91 亮红 ") else cat(" 91 bright red ")
cat("\x1b[0m")
cat("\x1b[92m")
if (lang == 0) cat(" 92 亮绿 ") else cat(" 92 bright green ")
cat("\x1b[0m")
cat("\x1b[93m")
if (lang == 0) cat(" 93 亮黄 ") else cat(" 93 bright yellow ")
cat("\x1b[0m")
cat("\x1b[94m")
if (lang == 0) cat(" 94 亮蓝 ") else cat(" 94 bright blue ")
cat("\x1b[0m")
cat("\x1b[95m")
if (lang == 0) cat(" 95 亮品红 ") else cat(" 95 bright magenta ")
cat("\x1b[0m")
cat("\x1b[96m")
if (lang == 0) cat(" 96 亮青 ") else cat(" 96 bright cyan ")
cat("\x1b[0m")
cat("\x1b[97m")
if (lang == 0) cat(" 97 亮白 ") else cat(" 97 bright white ")
cat("\x1b[0m")
cat("\n")

# ── 背景色（40–47 / 100–107）───────────────────────────────────
# -- Background colors (40-47 / 100-107) --
cat("\x1b[8;1H")
cat("\x1b[1;37m")
if (lang == 0) cat("背景色：") else cat("Background colors:")
cat("\x1b[0m")
cat("\n")

cat("\x1b[9;1H")
cat("\x1b[41m")
if (lang == 0) cat(" 红底 ") else cat(" red bg ")
cat("\x1b[0m")
cat("\x1b[42m")
if (lang == 0) cat(" 绿底 ") else cat(" green bg ")
cat("\x1b[0m")
cat("\x1b[44m")
if (lang == 0) cat(" 蓝底 ") else cat(" blue bg ")
cat("\x1b[0m")
cat("\x1b[46m")
if (lang == 0) cat(" 青底 ") else cat(" cyan bg ")
cat("\x1b[0m")
cat("\x1b[103m")
if (lang == 0) cat(" 亮黄底 ") else cat(" bright yellow bg ")
cat("\x1b[0m")
cat("\x1b[105m")
if (lang == 0) cat(" 亮品红底 ") else cat(" bright magenta bg ")
cat("\x1b[0m")
cat("\n")

# ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
# -- Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) --
cat("\x1b[11;1H")
cat("\x1b[1;37m")
if (lang == 0) cat("样式：") else cat("Styles:")
cat("\x1b[0m")
cat("\x1b[1m")
if (lang == 0) cat(" 粗体 ") else cat(" bold ")
cat("\x1b[0m")
cat("\x1b[2m")
if (lang == 0) cat(" 暗淡 ") else cat(" dim ")
cat("\x1b[0m")
cat("\x1b[3m")
if (lang == 0) cat(" 斜体 ") else cat(" italic ")
cat("\x1b[0m")
cat("\x1b[4m")
if (lang == 0) cat(" 下划线 ") else cat(" underline ")
cat("\x1b[0m")
cat("\x1b[9m")
if (lang == 0) cat(" 删除线 ") else cat(" strike ")
cat("\x1b[0m")
cat("\n")

# ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
# -- 256 colors (38;5;N) and true color (38;2;r;g;b) --
cat("\x1b[13;1H")
cat("\x1b[1;37m")
if (lang == 0) cat("256 色 / 真彩：") else cat("256 colors / truecolor:")
cat("\x1b[0m")
cat("\x1b[38;5;208m")
if (lang == 0) cat(" 256-208 橙 ") else cat(" 256-208 orange ")
cat("\x1b[0m")
cat("\x1b[38;5;46m")
if (lang == 0) cat(" 256-46 亮绿 ") else cat(" 256-46 bright green ")
cat("\x1b[0m")
cat("\x1b[38;2;255;128;0m")
if (lang == 0) cat(" 真彩橙 ") else cat(" truecolor orange ")
cat("\x1b[0m")
cat("\x1b[48;2;60;0;90m")
if (lang == 0) cat(" 真彩深紫底 ") else cat(" truecolor deep purple bg ")
cat("\x1b[0m")
cat("\n")

# ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
# -- Draw a color band in a loop (12 cells, colors cycling through 8 steps) --
# 「算出来的」那一格：取模 + 分支挑字面量色码（数字转不进字符串，见文件头）。
# The "computed" cell: modulo plus a branch picking a literal color code (digits cannot be joined into a string, see the file header).
cat("\x1b[16;1H")
cat("\x1b[1;37m")
if (lang == 0) cat("循环画 12 格色带：") else cat("Draw a 12-cell color band in a loop:")
cat("\x1b[0m")
cat("\n")

cat("\x1b[17;1H")
i <- 0
while (i < 12) {
  k <- i %% 8
  if (k == 0) {
    cat("\x1b[41m")
  }
  if (k == 1) {
    cat("\x1b[42m")
  }
  if (k == 2) {
    cat("\x1b[43m")
  }
  if (k == 3) {
    cat("\x1b[44m")
  }
  if (k == 4) {
    cat("\x1b[45m")
  }
  if (k == 5) {
    cat("\x1b[46m")
  }
  if (k == 6) {
    cat("\x1b[47m")
  }
  if (k == 7) {
    cat("\x1b[100m")
  }
  cat("   ")
  cat("\x1b[0m")
  i <- i + 1
}
cat("\n")

# ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
# -- Wrap-up: reset the colors + position to row 20 for the closing line --
cat("\x1b[0m")
cat("\x1b[20;1H")
cat("\x1b[1;32m")
if (lang == 0) cat("demo_tty 结束 —— 没有按键等待，画完即退出。") else cat("demo_tty done -- no key wait, draws and exits.")
cat("\x1b[0m")
cat("\n")
