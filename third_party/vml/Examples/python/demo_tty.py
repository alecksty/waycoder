# demo_tty.py —— **第 2 层：彩色控制台**
# demo_tty.py — **Layer 2: color console**
#
# 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
# The problem this layer solves is "**doing layout on a character screen**": clearing the screen, moving the cursor to row/column N,
# 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
# setting foreground/background colors. Old DOS programs (menus, tables, status bars) were all built from these few primitives.
#
# Python 这一层**没有 conio/crt 绑定**（`Lib/python/gfx.py` 里那些 `*_print` 是文档性
# Python has **no conio/crt bindings** at this layer (the `*_print` entries in `Lib/python/gfx.py` are documentary
# 存根，`conio.vml` 的绑定也不是给 Python 调的），所以这里**直接输出 ANSI 转义序列**
# stubs, and the `conio.vml` bindings are not meant to be called from Python), so here we **emit ANSI escape sequences directly**
# —— 那正是"支持彩色的控制台程序"：
# — which is exactly what a "color-capable console program" is:
#
#     清屏        ESC [ 2 J          ESC [ H
#     clear screen: ESC [ 2 J, ESC [ H
#     定位光标    ESC [ <行> ; <列> H
#     position the cursor: ESC [ <row> ; <col> H
#     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
#     foreground/background: ESC [ 3x m / 4x m / 9x m / 10x m, 256-color 38;5;N, true color 38;2;r;g;b
#     复位        ESC [ 0 m
#     reset: ESC [ 0 m
#
# ## ⚠ 本前端的坑：字符串字面量里**没有 `\xNN` 转义**
# ## ⚠ Pitfall of this frontend: **no `\xNN` escape** inside string literals
#
# `PythonCompiler/Lexer.cs` 的转义表只有 `\n \t \r \\ \' \"`，其余是
# the escape table in `PythonCompiler/Lexer.cs` only has `\n \t \r \\ \' \"`, everything else is
# 「丢掉反斜杠、留原字符」⇒ `"\x1b[31m"` 打出的是字面量 `x1b[31m`。
# "drop the backslash, keep the character" ⇒ `"\x1b[31m"` prints the literal `x1b[31m`.
# 所以 `ansi()` 用 `putchar(27)` **直接写 ESC 那个字节**，再把后半截当普通字符串打。
# So `ansi()` uses `putchar(27)` to **write the ESC byte directly**, then prints the rest as an ordinary string.
# `print_str`/`putchar` 都**不补换行**，换行要自己 `print_str("\n")`。
# Neither `print_str` nor `putchar` **appends a newline**; you have to emit it yourself with `print_str("\n")`.
#
# ## ⚠ 另一条踩过的坑：别在用户函数里连着调两次 `int_to_str`
# ## ⚠ Another pitfall already hit: do not call `int_to_str` twice in a row inside a user function
#
# 想「算出坐标再拼进转义串」会踩到 `Lib/` 那两套栈清理约定（见 CLAUDE.md ⑨）：
# Wanting to "compute a coordinate and splice it into an escape string" runs into the two stack-cleanup conventions in `Lib/` (see CLAUDE.md ⑨):
# 实测
# Measured:
#
#     def at(row, col):
#         print_str(int_to_str(row)); print_str(";"); print_str(int_to_str(col))
#     at(1, 1)   # 期望 1;1H，实得 1;22818H（第二个 int_to_str 读到的实参漂了）
#     at(1, 1)   # expected 1;1H, actually got 1;22818H (the second int_to_str read a drifted argument)
#
# 所以本 demo 的坐标**写成字面量**（`ansi("5;1H")`），一个 `int_to_str` 都不用。
# So the coordinates in this demo are **written as literals** (`ansi("5;1H")`), without using `int_to_str` at all.
#
# ## 在哪儿能看到什么
# ## What you can see where
#
# · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
# · **A real terminal** (`vmlcli … | cat -v`): the whole escape set works.
# · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
# · **The phone's "command line" page**: that layer converts the ANSI in stdout into the repo's unified `«»` intermediate format
#   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
#   (`UI/Shared/AnsiMarkup.cs`), and it **only understands SGR (color/style)**; cursor positioning (`ESC[…H`)
#   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
#   and screen clearing (`ESC[2J`) get **eaten** — this is a deliberately limited subset (see
#   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
#   the notes in `Examples/c/ansi_colors.c`). That is why every line ends with a newline:
#   定位被吃掉时输出仍然一行一句、读得通。
#   even when positioning is eaten, the output still reads one sentence per line.
#
# 跑法：命令行页输入  vml run examples/python/demo_tty.py
# How to run: type this on the command line page  vml run examples/python/demo_tty.py

# ── ANSI 小工具 ────────────────────────────────────────────────
# ── ANSI helpers ────────────────────────────────────────────────
# `ansi("31m")` = ESC[31m；定位也走同一个口子（`ansi("5;1H")` = ESC[5;1H）。
# `ansi("31m")` = ESC[31m; positioning goes through the same entry point (`ansi("5;1H")` = ESC[5;1H).
def ansi(code):
    putchar(27)
    print_str("[")
    print_str(code)

def text(s):
    print_str(s)

lang = ui_get_language()

# ── 开场：清屏 + 回左上角 ───────────────────────────────────────
# ── opening: clear screen + back to the top-left ─────────────────────────
ansi("2J")
ansi("H")

# ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
# ── title bar: white on blue (ESC[44;97m) ────────────────────────────
ansi("1;1H")
ansi("44;97m")
text("  demo_tty (Python) —— 彩色控制台 / ANSI 转义序列          " if lang == 0 else "  demo_tty (Python) -- color console / ANSI escapes          ")
ansi("0m")
text("\n")

ansi("2;1H")
ansi("90m")
text("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m" if lang == 0 else "clear ESC[2J   cursor ESC[r;cH   color ESC[3xm / ESC[4xm   reset ESC[0m")
ansi("0m")
text("\n")

# ── 标准 8 色前景（30–37）──────────────────────────────────────
# ── standard 8 foreground colors (30–37) ────────────────────────────────
ansi("4;1H")
ansi("1;37m")
text("标准 8 色前景：" if lang == 0 else "Standard 8 foreground colors:")
ansi("0m")
text("\n")

ansi("5;1H")
ansi("30m")
text(" 30 黑 " if lang == 0 else " 30 black ")
ansi("0m")
ansi("31m")
text(" 31 红 " if lang == 0 else " 31 red ")
ansi("0m")
ansi("32m")
text(" 32 绿 " if lang == 0 else " 32 green ")
ansi("0m")
ansi("33m")
text(" 33 黄 " if lang == 0 else " 33 yellow ")
ansi("0m")
ansi("34m")
text(" 34 蓝 " if lang == 0 else " 34 blue ")
ansi("0m")
ansi("35m")
text(" 35 品红 " if lang == 0 else " 35 magenta ")
ansi("0m")
ansi("36m")
text(" 36 青 " if lang == 0 else " 36 cyan ")
ansi("0m")
ansi("37m")
text(" 37 白 " if lang == 0 else " 37 white ")
ansi("0m")
text("\n")

# ── 亮色前景（90–97）────────────────────────────────────────────
# ── bright foreground colors (90–97) ────────────────────────────────────
ansi("6;1H")
ansi("90m")
text(" 90 亮黑(灰) " if lang == 0 else " 90 gray ")
ansi("0m")
ansi("91m")
text(" 91 亮红 " if lang == 0 else " 91 bright red ")
ansi("0m")
ansi("92m")
text(" 92 亮绿 " if lang == 0 else " 92 bright green ")
ansi("0m")
ansi("93m")
text(" 93 亮黄 " if lang == 0 else " 93 bright yellow ")
ansi("0m")
ansi("94m")
text(" 94 亮蓝 " if lang == 0 else " 94 bright blue ")
ansi("0m")
ansi("95m")
text(" 95 亮品红 " if lang == 0 else " 95 bright magenta ")
ansi("0m")
ansi("96m")
text(" 96 亮青 " if lang == 0 else " 96 bright cyan ")
ansi("0m")
ansi("97m")
text(" 97 亮白 " if lang == 0 else " 97 bright white ")
ansi("0m")
text("\n")

# ── 背景色（40–47 / 100–107）───────────────────────────────────
# ── background colors (40–47 / 100–107) ─────────────────────────────────
ansi("8;1H")
ansi("1;37m")
text("背景色：" if lang == 0 else "Background colors:")
ansi("0m")
text("\n")

ansi("9;1H")
ansi("41m")
text(" 红底 " if lang == 0 else " red bg ")
ansi("0m")
ansi("42m")
text(" 绿底 " if lang == 0 else " green bg ")
ansi("0m")
ansi("44m")
text(" 蓝底 " if lang == 0 else " blue bg ")
ansi("0m")
ansi("46m")
text(" 青底 " if lang == 0 else " cyan bg ")
ansi("0m")
ansi("103m")
text(" 亮黄底 " if lang == 0 else " bright yellow bg ")
ansi("0m")
ansi("105m")
text(" 亮品红底 " if lang == 0 else " bright magenta bg ")
ansi("0m")
text("\n")

# ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
# ── styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) ──────────
ansi("11;1H")
ansi("1;37m")
text("样式：" if lang == 0 else "Styles:")
ansi("0m")
ansi("1m")
text(" 粗体 " if lang == 0 else " bold ")
ansi("0m")
ansi("2m")
text(" 暗淡 " if lang == 0 else " dim ")
ansi("0m")
ansi("3m")
text(" 斜体 " if lang == 0 else " italic ")
ansi("0m")
ansi("4m")
text(" 下划线 " if lang == 0 else " underline ")
ansi("0m")
ansi("9m")
text(" 删除线 " if lang == 0 else " strike ")
ansi("0m")
text("\n")

# ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
# ── 256 colors (38;5;N) and true color (38;2;r;g;b) ─────────────────────
ansi("13;1H")
ansi("1;37m")
text("256 色 / 真彩：" if lang == 0 else "256 colors / truecolor:")
ansi("0m")
ansi("38;5;208m")
text(" 256-208 橙 " if lang == 0 else " 256-208 orange ")
ansi("0m")
ansi("38;5;46m")
text(" 256-46 亮绿 " if lang == 0 else " 256-46 bright green ")
ansi("0m")
ansi("38;2;255;128;0m")
text(" 真彩橙 " if lang == 0 else " truecolor orange ")
ansi("0m")
ansi("48;2;60;0;90m")
text(" 真彩深紫底 " if lang == 0 else " truecolor deep purple bg ")
ansi("0m")
text("\n")

# ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
# ── draw a color band in a loop (12 cells, colors cycling through 8 steps) ───────────────
# 「算出来的」那一格：用取模 + 分支挑出字面量色码。这不是绕远 ——
# The "computed" cell: pick a literal color code with modulo + branches. This is not a detour —
# 数字转字符串这条路在多数前端上要么缺、要么像上面那样会漂，
# the number-to-string path is either missing on most frontends or drifts as shown above,
# 所以「色码写字面量、循环只算用哪一个」是所有语言都能照做的写法。
# so "write the color codes as literals, let the loop only compute which one to use" is a pattern every language can follow.
ansi("16;1H")
ansi("1;37m")
text("循环画 12 格色带：" if lang == 0 else "Draw a 12-cell color band in a loop:")
ansi("0m")
text("\n")

ansi("17;1H")
i = 0
while i < 12:
    k = i % 8
    if k == 0:
        ansi("41m")
    if k == 1:
        ansi("42m")
    if k == 2:
        ansi("43m")
    if k == 3:
        ansi("44m")
    if k == 4:
        ansi("45m")
    if k == 5:
        ansi("46m")
    if k == 6:
        ansi("47m")
    if k == 7:
        ansi("100m")
    text("   ")
    ansi("0m")
    i = i + 1
text("\n")

# ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
# ── wrap-up: reset the colors + position to row 20 and write the closing line ────────────
ansi("0m")
ansi("20;1H")
ansi("1;32m")
text("demo_tty 结束 —— 没有按键等待，画完即退出。" if lang == 0 else "demo_tty done -- no key wait, draws and exits.")
ansi("0m")
text("\n")
