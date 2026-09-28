# demo_tty.rb —— **第 2 层：彩色控制台**
# demo_tty.rb — **Layer 2: color console**
#
# 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
# This layer solves "**doing layout on a character screen**": clearing the screen, moving the cursor to row/column,
# 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
# setting foreground/background colors. Old DOS programs (menus, tables, status bars) were built entirely from these few primitives.
#
# Ruby 这一层**没有 conio/crt 绑定**（`Lib/ruby/console.rb` 那几个 `console_*` 是
# Ruby has **no conio/crt bindings** for this layer (the `console_*` entries in `Lib/ruby/console.rb` are
# 文档性存根），所以这里**直接输出 ANSI 转义序列** —— 那正是"支持彩色的控制台程序"：
# documentation-only stubs), so this file **emits ANSI escape sequences directly** — which is exactly a "color-capable console program":
#
#     清屏        ESC [ 2 J          ESC [ H
#     Clear screen        ESC [ 2 J          ESC [ H
#     定位光标    ESC [ <行> ; <列> H
#     Move cursor         ESC [ <row> ; <col> H
#     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
#     Foreground/background   ESC [ 3x m / 4x m / 9x m / 10x m, 256-color 38;5;N, true color 38;2;r;g;b
#     复位        ESC [ 0 m
#     Reset               ESC [ 0 m
#
# ## ⚠ 本前端的三条限制（写 demo 时避开）
# ## ⚠ Three limits of this frontend (avoid them when writing demos)
#
#   · **不能定义函数** —— 最朴素的 `def f … end` 会报
#   · **You cannot define functions** — the plainest `def f … end` reports
#     `Unexpected token: End(end)`（`Examples/ruby/catch.rb` 的文件头记着同一条）。
#     `Unexpected token: End(end)` (the header of `Examples/ruby/catch.rb` records the same item).
#     所以本份**完全平铺**：ESC 用 `print("\x1b[…")` 内联，一个 `def` 都没有。
#     So this file is **completely flat**: ESC is inlined with `print("\x1b[…")`, with not a single `def`.
#   · **词法器不认 `&&`**（`Unexpected char: &`）⇒ 多条件是嵌套 `if`，不是 `and`/`&&`。
#   · **The lexer does not accept `&&`** (`Unexpected char: &`) ⇒ multiple conditions are nested `if`, not `and`/`&&`.
#     本份只有一个等值判断，用 `if k == 0` 就够了。
#     This file has only one equality test, so `if k == 0` is enough.
#   · 字符串插值 `#{...}` **不生效**（原样打出 `#{a}`）⇒ 坐标写成字面量，别想拼字符串。
#   · String interpolation `#{...}` **does not work** (it prints `#{a}` verbatim) ⇒ write coordinates as literals, don't try to build strings.
#
# ✅ 好消息：`\x1b` 在**字符串字面量里是支持的**（Ruby 走 `LexerBase.ReadEscape`，
# ✅ Good news: `\x1b` **is supported inside string literals** (Ruby goes through `LexerBase.ReadEscape`,
#    完整支持 `\n \t \r \\ \' \" \xNN \NNN`）⇒ 本份可以直接内联 ESC，
#    with full support for `\n \t \r \\ \' \" \xNN \NNN`) ⇒ this file can inline ESC directly,
#    不像 Python 那样非得 `putchar(27)`。
#    unlike Python, which has to use `putchar(27)`.
#
# ## 在哪儿能看到什么
# ## Where you can see what
#
# · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
# · **A real terminal** (`vmlcli … | cat -v`): the whole escape set takes effect.
# · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
# · **The phone's "command line" page**: that layer converts the ANSI in stdout into the repo-wide `«»` intermediate format
#   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
#   (`UI/Shared/AnsiMarkup.cs`) and **recognizes only SGR (color/style)**; cursor positioning (`ESC[…H`)
#   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
#   and clearing the screen (`ESC[2J`) are **swallowed** — this is a deliberate limited subset (see
#   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
#   the notes in `Examples/c/ansi_colors.c`). So a newline is appended at the end of every line,
#   定位被吃掉时输出仍然一行一句、读得通。
#   and when positioning is swallowed the output still reads one sentence per line.
#
# 跑法：命令行页输入  vml run examples/ruby/demo_tty.rb
# How to run: type this on the command-line page:  vml run examples/ruby/demo_tty.rb

# ── 开场：清屏 + 回左上角 ───────────────────────────────────────
# ── Opening: clear the screen + return to the top-left corner ───────────────────────────────────────
lang = ui_get_language()

print("\x1b[2J")
print("\x1b[H")

# ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
# ── Title bar: white text on a blue background (ESC[44;97m) ──────────────────────────────
print("\x1b[1;1H")
print("\x1b[44;97m")
if lang == 0 then print("  demo_tty (Ruby) —— 彩色控制台 / ANSI 转义序列            ") else print("  demo_tty (Ruby) -- color console / ANSI escapes            ") end
print("\x1b[0m")
print("\n")

print("\x1b[2;1H")
print("\x1b[90m")
if lang == 0 then print("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m") else print("clear ESC[2J   cursor ESC[r;cH   color ESC[3xm / ESC[4xm   reset ESC[0m") end
print("\x1b[0m")
print("\n")

# ── 标准 8 色前景（30–37）──────────────────────────────────────
# ── Standard 8-color foreground (30–37) ──────────────────────────────────────
print("\x1b[4;1H")
print("\x1b[1;37m")
if lang == 0 then print("标准 8 色前景：") else print("Standard 8 foreground colors:") end
print("\x1b[0m")
print("\n")

print("\x1b[5;1H")
print("\x1b[30m")
if lang == 0 then print(" 30 黑 ") else print(" 30 black ") end
print("\x1b[0m")
print("\x1b[31m")
if lang == 0 then print(" 31 红 ") else print(" 31 red ") end
print("\x1b[0m")
print("\x1b[32m")
if lang == 0 then print(" 32 绿 ") else print(" 32 green ") end
print("\x1b[0m")
print("\x1b[33m")
if lang == 0 then print(" 33 黄 ") else print(" 33 yellow ") end
print("\x1b[0m")
print("\x1b[34m")
if lang == 0 then print(" 34 蓝 ") else print(" 34 blue ") end
print("\x1b[0m")
print("\x1b[35m")
if lang == 0 then print(" 35 品红 ") else print(" 35 magenta ") end
print("\x1b[0m")
print("\x1b[36m")
if lang == 0 then print(" 36 青 ") else print(" 36 cyan ") end
print("\x1b[0m")
print("\x1b[37m")
if lang == 0 then print(" 37 白 ") else print(" 37 white ") end
print("\x1b[0m")
print("\n")

# ── 亮色前景（90–97）────────────────────────────────────────────
# ── Bright foreground (90–97) ────────────────────────────────────────────
print("\x1b[6;1H")
print("\x1b[90m")
if lang == 0 then print(" 90 亮黑(灰) ") else print(" 90 gray ") end
print("\x1b[0m")
print("\x1b[91m")
if lang == 0 then print(" 91 亮红 ") else print(" 91 bright red ") end
print("\x1b[0m")
print("\x1b[92m")
if lang == 0 then print(" 92 亮绿 ") else print(" 92 bright green ") end
print("\x1b[0m")
print("\x1b[93m")
if lang == 0 then print(" 93 亮黄 ") else print(" 93 bright yellow ") end
print("\x1b[0m")
print("\x1b[94m")
if lang == 0 then print(" 94 亮蓝 ") else print(" 94 bright blue ") end
print("\x1b[0m")
print("\x1b[95m")
if lang == 0 then print(" 95 亮品红 ") else print(" 95 bright magenta ") end
print("\x1b[0m")
print("\x1b[96m")
if lang == 0 then print(" 96 亮青 ") else print(" 96 bright cyan ") end
print("\x1b[0m")
print("\x1b[97m")
if lang == 0 then print(" 97 亮白 ") else print(" 97 bright white ") end
print("\x1b[0m")
print("\n")

# ── 背景色（40–47 / 100–107）───────────────────────────────────
# ── Background colors (40–47 / 100–107) ───────────────────────────────────
print("\x1b[8;1H")
print("\x1b[1;37m")
if lang == 0 then print("背景色：") else print("Background colors:") end
print("\x1b[0m")
print("\n")

print("\x1b[9;1H")
print("\x1b[41m")
if lang == 0 then print(" 红底 ") else print(" red bg ") end
print("\x1b[0m")
print("\x1b[42m")
if lang == 0 then print(" 绿底 ") else print(" green bg ") end
print("\x1b[0m")
print("\x1b[44m")
if lang == 0 then print(" 蓝底 ") else print(" blue bg ") end
print("\x1b[0m")
print("\x1b[46m")
if lang == 0 then print(" 青底 ") else print(" cyan bg ") end
print("\x1b[0m")
print("\x1b[103m")
if lang == 0 then print(" 亮黄底 ") else print(" bright yellow bg ") end
print("\x1b[0m")
print("\x1b[105m")
if lang == 0 then print(" 亮品红底 ") else print(" bright magenta bg ") end
print("\x1b[0m")
print("\n")

# ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
# ── Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) ─────────────
print("\x1b[11;1H")
print("\x1b[1;37m")
if lang == 0 then print("样式：") else print("Styles:") end
print("\x1b[0m")
print("\x1b[1m")
if lang == 0 then print(" 粗体 ") else print(" bold ") end
print("\x1b[0m")
print("\x1b[2m")
if lang == 0 then print(" 暗淡 ") else print(" dim ") end
print("\x1b[0m")
print("\x1b[3m")
if lang == 0 then print(" 斜体 ") else print(" italic ") end
print("\x1b[0m")
print("\x1b[4m")
if lang == 0 then print(" 下划线 ") else print(" underline ") end
print("\x1b[0m")
print("\x1b[9m")
if lang == 0 then print(" 删除线 ") else print(" strike ") end
print("\x1b[0m")
print("\n")

# ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
# ── 256 colors (38;5;N) and true color (38;2;r;g;b) ─────────────────────────
print("\x1b[13;1H")
print("\x1b[1;37m")
if lang == 0 then print("256 色 / 真彩：") else print("256 colors / truecolor:") end
print("\x1b[0m")
print("\x1b[38;5;208m")
if lang == 0 then print(" 256-208 橙 ") else print(" 256-208 orange ") end
print("\x1b[0m")
print("\x1b[38;5;46m")
if lang == 0 then print(" 256-46 亮绿 ") else print(" 256-46 bright green ") end
print("\x1b[0m")
print("\x1b[38;2;255;128;0m")
if lang == 0 then print(" 真彩橙 ") else print(" truecolor orange ") end
print("\x1b[0m")
print("\x1b[48;2;60;0;90m")
if lang == 0 then print(" 真彩深紫底 ") else print(" truecolor deep purple bg ") end
print("\x1b[0m")
print("\n")

# ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
# ── Loop drawing a color band (12 cells, colors cycling through 8 steps) ──────────────────
# 「算出来的」那一格：取模 + 分支挑字面量色码。
# The "computed" cell: modulo + branches picking a literal color code.
# （数字转字符串在 Ruby 这边本来就没有 —— 没有 `.to_s`。）
# (Number-to-string does not exist on the Ruby side anyway — there is no `.to_s`.)
print("\x1b[16;1H")
print("\x1b[1;37m")
if lang == 0 then print("循环画 12 格色带：") else print("Draw a 12-cell color band in a loop:") end
print("\x1b[0m")
print("\n")

print("\x1b[17;1H")
i = 0
while i < 12
  k = i % 8
  if k == 0
    print("\x1b[41m")
  end
  if k == 1
    print("\x1b[42m")
  end
  if k == 2
    print("\x1b[43m")
  end
  if k == 3
    print("\x1b[44m")
  end
  if k == 4
    print("\x1b[45m")
  end
  if k == 5
    print("\x1b[46m")
  end
  if k == 6
    print("\x1b[47m")
  end
  if k == 7
    print("\x1b[100m")
  end
  print("   ")
  print("\x1b[0m")
  i = i + 1
end
print("\n")

# ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
# ── Wrap-up: reset the colors + move to row 20 to write the closing line ─────────────────────
print("\x1b[0m")
print("\x1b[20;1H")
print("\x1b[1;32m")
if lang == 0 then print("demo_tty 结束 —— 没有按键等待，画完即退出。") else print("demo_tty done -- no key wait, draws and exits.") end
print("\x1b[0m")
print("\n")
