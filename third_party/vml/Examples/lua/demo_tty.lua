-- demo_tty.lua —— **第 2 层：彩色控制台**
-- demo_tty.lua — **Layer 2: color console**
--
-- 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
-- The problem this layer solves is "**laying out a character-cell screen**": clearing the screen, moving the cursor to a given row and column,
-- 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
-- setting foreground/background colors. Old DOS programs (menus, tables, status bars) were all assembled from these few primitives.
--
-- Lua 这一层**没有 conio/crt 绑定**，所以这里**直接输出 ANSI 转义序列**
-- Lua has **no conio/crt bindings** for this layer, so here we **emit ANSI escape sequences directly**
-- —— 那正是"支持彩色的控制台程序"：
-- — which is exactly what "a color-capable console program" is:
--
--     清屏        ESC [ 2 J          ESC [ H
--     clear screen  ESC [ 2 J          ESC [ H
--     定位光标    ESC [ <行> ; <列> H
--     cursor to     ESC [ <row> ; <col> H
--     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
--     fg/bg         ESC [ 3x m / 4x m / 9x m / 10x m, 256-color 38;5;N, truecolor 38;2;r;g;b
--     复位        ESC [ 0 m
--     reset         ESC [ 0 m
--
-- ## ⚠ 本前端的两条限制（写 demo 时避开）
-- ## ⚠ Two limits of this frontend (avoid them when writing demos)
--
--   · **`..` 字符串拼接编不过** —— 链到 `lua_concat`，库里没有这个标签
--   · **`..` string concatenation does not compile** — it links to `lua_concat`, and the library has no such label
--     （「未定义的函数 'lua_concat'」）。所以一份 `..` 都不用，转义串整条写字面量。
--     ("undefined function 'lua_concat'"). So not a single `..` is used; escape strings are written out as whole literals.
--   · **`putchar(n)` 中间夹了别的库调用就读错实参** —— 单条连着调时是对的
--   · **`putchar(n)` reads the wrong argument when another library call is wedged in between** — a straight run of calls is correct
--     （`putchar(65) putchar(66) putchar(27)` → `A B ESC`），而
--     (`putchar(65) putchar(66) putchar(27)` → `A B ESC`), while
--     `putchar(65)` `print_str("\n")` `putchar(27)` 里的第三个字节实测是 `A7 AD`
--     in `putchar(65)` `print_str("\n")` `putchar(27)` the third byte measures as `A7 AD`
--     而不是 `1B`（与 `Lib/` 那两套栈清理约定同源，见 CLAUDE.md ⑨）。
--     instead of `1B` (same root cause as the two stack-cleanup conventions in `Lib/`, see CLAUDE.md ⑨).
--     好在 Lua 的字符串字面量**支持 `\x1b`**（走 `LexerBase.ReadEscape`），
--     Fortunately Lua string literals **support `\x1b`** (via `LexerBase.ReadEscape`),
--     所以内联 ESC 就好、用不着 putchar —— 本份一个字都没用 putchar。
--     so inlining ESC is enough and putchar is not needed — this file does not use putchar even once.
--   · `print` 会补换行 ⇒ 这里一律用**不补换行**的 `print_str`。
--   · `print` appends a newline ⇒ here we always use `print_str`, which **appends no newline**.
--
-- ## 在哪儿能看到什么
-- ## Where you can see what
--
-- · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
-- · **A real terminal** (`vmlcli … | cat -v`): the whole escape set takes effect.
-- · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
-- · **The phone's "command line" page**: that layer converts ANSI on stdout into this repo's unified `«»` intermediate format
--   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
--   (`UI/Shared/AnsiMarkup.cs`), and **only recognizes SGR (color/style)**; cursor positioning (`ESC[…H`)
--   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
--   and screen clearing (`ESC[2J`) are **swallowed** — this is a deliberately limited subset (see
--   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
--   the notes in `Examples/c/ansi_colors.c`). So a newline is appended at the end of every line, and
--   定位被吃掉时输出仍然一行一句、读得通。
--   when positioning is swallowed the output is still one sentence per line and readable.
--
-- 跑法：命令行页输入  vml run examples/lua/demo_tty.lua
-- How to run: on the command-line page type  vml run examples/lua/demo_tty.lua

-- ── 开场：清屏 + 回左上角 ───────────────────────────────────────
-- ── Opening: clear the screen + go back to the top-left corner ───────────────────────────────────────
print_str("\x1b[2J")
print_str("\x1b[H")

-- ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
-- ── Title bar: white on blue (ESC[44;97m) ──────────────────────────────
print_str("\x1b[1;1H")
print_str("\x1b[44;97m")
print_str("  demo_tty (Lua) —— 彩色控制台 / ANSI 转义序列             ")
print_str("\x1b[0m")
print_str("\n")

print_str("\x1b[2;1H")
print_str("\x1b[90m")
print_str("清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m")
print_str("\x1b[0m")
print_str("\n")

-- ── 标准 8 色前景（30–37）──────────────────────────────────────
-- ── Standard 8 foreground colors (30–37) ──────────────────────────────────────
print_str("\x1b[4;1H")
print_str("\x1b[1;37m")
print_str("标准 8 色前景：")
print_str("\x1b[0m")
print_str("\n")

print_str("\x1b[5;1H")
print_str("\x1b[30m")
print_str(" 30 黑 ")
print_str("\x1b[0m")
print_str("\x1b[31m")
print_str(" 31 红 ")
print_str("\x1b[0m")
print_str("\x1b[32m")
print_str(" 32 绿 ")
print_str("\x1b[0m")
print_str("\x1b[33m")
print_str(" 33 黄 ")
print_str("\x1b[0m")
print_str("\x1b[34m")
print_str(" 34 蓝 ")
print_str("\x1b[0m")
print_str("\x1b[35m")
print_str(" 35 品红 ")
print_str("\x1b[0m")
print_str("\x1b[36m")
print_str(" 36 青 ")
print_str("\x1b[0m")
print_str("\x1b[37m")
print_str(" 37 白 ")
print_str("\x1b[0m")
print_str("\n")

-- ── 亮色前景（90–97）────────────────────────────────────────────
-- ── Bright foreground colors (90–97) ────────────────────────────────────────────
print_str("\x1b[6;1H")
print_str("\x1b[90m")
print_str(" 90 亮黑(灰) ")
print_str("\x1b[0m")
print_str("\x1b[91m")
print_str(" 91 亮红 ")
print_str("\x1b[0m")
print_str("\x1b[92m")
print_str(" 92 亮绿 ")
print_str("\x1b[0m")
print_str("\x1b[93m")
print_str(" 93 亮黄 ")
print_str("\x1b[0m")
print_str("\x1b[94m")
print_str(" 94 亮蓝 ")
print_str("\x1b[0m")
print_str("\x1b[95m")
print_str(" 95 亮品红 ")
print_str("\x1b[0m")
print_str("\x1b[96m")
print_str(" 96 亮青 ")
print_str("\x1b[0m")
print_str("\x1b[97m")
print_str(" 97 亮白 ")
print_str("\x1b[0m")
print_str("\n")

-- ── 背景色（40–47 / 100–107）───────────────────────────────────
-- ── Background colors (40–47 / 100–107) ───────────────────────────────────
print_str("\x1b[8;1H")
print_str("\x1b[1;37m")
print_str("背景色：")
print_str("\x1b[0m")
print_str("\n")

print_str("\x1b[9;1H")
print_str("\x1b[41m")
print_str(" 红底 ")
print_str("\x1b[0m")
print_str("\x1b[42m")
print_str(" 绿底 ")
print_str("\x1b[0m")
print_str("\x1b[44m")
print_str(" 蓝底 ")
print_str("\x1b[0m")
print_str("\x1b[46m")
print_str(" 青底 ")
print_str("\x1b[0m")
print_str("\x1b[103m")
print_str(" 亮黄底 ")
print_str("\x1b[0m")
print_str("\x1b[105m")
print_str(" 亮品红底 ")
print_str("\x1b[0m")
print_str("\n")

-- ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
-- ── Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) ─────────────
print_str("\x1b[11;1H")
print_str("\x1b[1;37m")
print_str("样式：")
print_str("\x1b[0m")
print_str("\x1b[1m")
print_str(" 粗体 ")
print_str("\x1b[0m")
print_str("\x1b[2m")
print_str(" 暗淡 ")
print_str("\x1b[0m")
print_str("\x1b[3m")
print_str(" 斜体 ")
print_str("\x1b[0m")
print_str("\x1b[4m")
print_str(" 下划线 ")
print_str("\x1b[0m")
print_str("\x1b[9m")
print_str(" 删除线 ")
print_str("\x1b[0m")
print_str("\n")

-- ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
-- ── 256 colors (38;5;N) and truecolor (38;2;r;g;b) ─────────────────────────
print_str("\x1b[13;1H")
print_str("\x1b[1;37m")
print_str("256 色 / 真彩：")
print_str("\x1b[0m")
print_str("\x1b[38;5;208m")
print_str(" 256-208 橙 ")
print_str("\x1b[0m")
print_str("\x1b[38;5;46m")
print_str(" 256-46 亮绿 ")
print_str("\x1b[0m")
print_str("\x1b[38;2;255;128;0m")
print_str(" 真彩橙 ")
print_str("\x1b[0m")
print_str("\x1b[48;2;60;0;90m")
print_str(" 真彩深紫底 ")
print_str("\x1b[0m")
print_str("\n")

-- ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
-- ── Draw a color band in a loop (12 cells, colors cycling through 8 steps) ──────────────────
-- 「算出来的」那一格：取模 + 分支挑字面量色码（`..` 不能用，见文件头）。
-- The "computed" cell: modulo + a branch picking a literal color code (`..` cannot be used, see the file header).
print_str("\x1b[16;1H")
print_str("\x1b[1;37m")
print_str("循环画 12 格色带：")
print_str("\x1b[0m")
print_str("\n")

print_str("\x1b[17;1H")
local i = 0
while i < 12 do
  local k = i % 8
  if k == 0 then
    print_str("\x1b[41m")
  end
  if k == 1 then
    print_str("\x1b[42m")
  end
  if k == 2 then
    print_str("\x1b[43m")
  end
  if k == 3 then
    print_str("\x1b[44m")
  end
  if k == 4 then
    print_str("\x1b[45m")
  end
  if k == 5 then
    print_str("\x1b[46m")
  end
  if k == 6 then
    print_str("\x1b[47m")
  end
  if k == 7 then
    print_str("\x1b[100m")
  end
  print_str("   ")
  print_str("\x1b[0m")
  i = i + 1
end
print_str("\n")

-- ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
-- ── Wrap-up: reset the colors + position to row 20 and write the closing line ─────────────────────
print_str("\x1b[0m")
print_str("\x1b[20;1H")
print_str("\x1b[1;32m")
print_str("demo_tty 结束 —— 没有按键等待，画完即退出。")
print_str("\x1b[0m")
print_str("\n")
