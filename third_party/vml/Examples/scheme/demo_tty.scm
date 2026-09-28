; demo_tty.scm —— **第 2 层：彩色控制台**
; demo_tty.scm -- **layer 2: color console**
;
; 这一层解决的问题是「**在一个字符界面上做版面**」：清屏、把光标挪到第几行第几列、
; The problem this layer solves is "**laying out a character display**": clear the screen, move the cursor to a given row and column,
; 设前景/背景色。老 DOS 程序（菜单、表格、状态栏）全靠这几个原语拼出来。
; set foreground/background colors. Old DOS programs (menus, tables, status bars) were built entirely from these primitives.
;
; Scheme 这一层**没有 conio/crt 绑定**，所以这里**直接输出 ANSI 转义序列**
; Scheme has **no conio/crt binding** at this layer, so this file **emits ANSI escape sequences directly**
; —— 那正是"支持彩色的控制台程序"：
; -- which is exactly a "console program with color support":
;
;     清屏        ESC [ 2 J          ESC [ H
;     clear screen        ESC [ 2 J          ESC [ H
;     定位光标    ESC [ <行> ; <列> H
;     position cursor     ESC [ <row> ; <col> H
;     前景/背景   ESC [ 3x m / 4x m / 9x m / 10x m，256 色 38;5;N，真彩 38;2;r;g;b
;     foreground/background  ESC [ 3x m / 4x m / 9x m / 10x m, 256 color 38;5;N, true color 38;2;r;g;b
;     复位        ESC [ 0 m
;     reset        ESC [ 0 m
;
; ## ⚠ 本前端的三条限制（写 demo 时避开）
; ## ⚠ Three limitations of this frontend (avoid them when writing a demo)
;
;   · **字符串字面量里没有任何转义** —— `SchemeCompiler/Lexer.cs` 的 `ReadString`
;   · **String literals contain no escapes at all** -- `ReadString` in `SchemeCompiler/Lexer.cs`
;     是「见到 `\` 就把反斜杠跳过、把下一个字符原样收下」⇒ `"\x1b[31m"` 得到的是
;     is "on seeing a `\`, skip the backslash and take the next character verbatim" => `"\x1b[31m"` yields
;     字面量 `x1b[31m`。所以 ESC 那个字节只能**用 `(putchar 27)` 直接写**，
;     the literal `x1b[31m`. So the ESC byte can only be **written directly with `(putchar 27)`**,
;     换行用 `(newline)`。
;     and a line break uses `(newline)`.
;   · **用户函数里不能调库函数**（尾调用优化会跳过被调者的序言）⇒ `putchar` /
;   · **A user function cannot call a library function** (tail-call optimization skips the callee's prologue) => the calls to `putchar` /
;     `print_str` 这些调用**一律写在顶层**，本份一个 `define` 函数都没有。
;     `print_str` and the like are **all written at the top level**; this file has not a single `define` function.
;   · `display` / `print` **只取第一个实参、且不补换行** ⇒ 文本用 `print_str` 打，
;   · `display` / `print` **take only the first argument and add no newline** => text is printed with `print_str`,
;     换行单独 `(newline)`。
;     and the line break is a separate `(newline)`.
;
; ## 在哪儿能看到什么
; ## What you can see where
;
; · **真终端**（`vmlcli … | cat -v`）：整套转义都生效。
; · **A real terminal** (`vmlcli ... | cat -v`): the whole escape set takes effect.
; · **手机"命令行"页**：那一层把 stdout 里的 ANSI 转成仓库统一的 `«»` 中间格式
; · **The phone's "command line" page**: that layer turns ANSI on stdout into the repo's unified `«»` intermediate format
;   （`UI/Shared/AnsiMarkup.cs`），**只认 SGR（颜色/样式）**；光标定位（`ESC[…H`）
;   (`UI/Shared/AnsiMarkup.cs`) and **understands only SGR (color/style)**; cursor positioning (`ESC[...H`)
;   与清屏（`ESC[2J`）会被**吃掉** —— 这是刻意的有限子集（见
;   and clearing the screen (`ESC[2J`) are **eaten** -- a deliberately limited subset (see
;   `Examples/c/ansi_colors.c` 的说明）。所以每行末尾都补了换行，
;   the notes in `Examples/c/ansi_colors.c`). So every line ends with a newline,
;   定位被吃掉时输出仍然一行一句、读得通。
;   and when positioning is eaten the output is still one sentence per line and readable.
;
; 跑法：命令行页输入  vml run examples/scheme/demo_tty.scm
; How to run: type this into the command-line page:  vml run examples/scheme/demo_tty.scm

; ── 开场：清屏 + 回左上角 ───────────────────────────────────────
; -- Opening: clear the screen + go back to the top left --
(define lang (ui_get_language))

(putchar 27)
(print_str "[2J")
(putchar 27)
(print_str "[H")

; ── 标题条：蓝底白字（ESC[44;97m）──────────────────────────────
; -- Title bar: white on blue (ESC[44;97m) --
(putchar 27)
(print_str "[1;1H")
(putchar 27)
(print_str "[44;97m")
(if lang (print_str "  demo_tty (Scheme) -- color console / ANSI escapes         ") (print_str "  demo_tty (Scheme) —— 彩色控制台 / ANSI 转义序列         "))
(putchar 27)
(print_str "[0m")
(newline)

(putchar 27)
(print_str "[2;1H")
(putchar 27)
(print_str "[90m")
(if lang (print_str "clear ESC[2J   cursor ESC[r;cH   color ESC[3xm / ESC[4xm   reset ESC[0m") (print_str "清屏 ESC[2J   定位 ESC[r;cH   颜色 ESC[3xm / ESC[4xm   复位 ESC[0m"))
; (the string above is printed as-is: the ";" inside ESC[r;cH belongs to the text, not to a comment)
(putchar 27)
(print_str "[0m")
(newline)

; ── 标准 8 色前景（30–37）──────────────────────────────────────
; -- The standard 8 foreground colors (30-37) --
(putchar 27)
(print_str "[4;1H")
(putchar 27)
(print_str "[1;37m")
(if lang (print_str "Standard 8 foreground colors:") (print_str "标准 8 色前景："))
(putchar 27)
(print_str "[0m")
(newline)

(putchar 27)
(print_str "[5;1H")
(putchar 27)
(print_str "[30m")
(if lang (print_str " 30 black ") (print_str " 30 黑 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[31m")
(if lang (print_str " 31 red ") (print_str " 31 红 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[32m")
(if lang (print_str " 32 green ") (print_str " 32 绿 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[33m")
(if lang (print_str " 33 yellow ") (print_str " 33 黄 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[34m")
(if lang (print_str " 34 blue ") (print_str " 34 蓝 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[35m")
(if lang (print_str " 35 magenta ") (print_str " 35 品红 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[36m")
(if lang (print_str " 36 cyan ") (print_str " 36 青 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[37m")
(if lang (print_str " 37 white ") (print_str " 37 白 "))
(putchar 27)
(print_str "[0m")
(newline)

; ── 亮色前景（90–97）────────────────────────────────────────────
; -- Bright foreground colors (90-97) --
(putchar 27)
(print_str "[6;1H")
(putchar 27)
(print_str "[90m")
(if lang (print_str " 90 gray ") (print_str " 90 亮黑(灰) "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[91m")
(if lang (print_str " 91 bright red ") (print_str " 91 亮红 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[92m")
(if lang (print_str " 92 bright green ") (print_str " 92 亮绿 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[93m")
(if lang (print_str " 93 bright yellow ") (print_str " 93 亮黄 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[94m")
(if lang (print_str " 94 bright blue ") (print_str " 94 亮蓝 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[95m")
(if lang (print_str " 95 bright magenta ") (print_str " 95 亮品红 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[96m")
(if lang (print_str " 96 bright cyan ") (print_str " 96 亮青 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[97m")
(if lang (print_str " 97 bright white ") (print_str " 97 亮白 "))
(putchar 27)
(print_str "[0m")
(newline)

; ── 背景色（40–47 / 100–107）───────────────────────────────────
; -- Background colors (40-47 / 100-107) --
(putchar 27)
(print_str "[8;1H")
(putchar 27)
(print_str "[1;37m")
(if lang (print_str "Background colors:") (print_str "背景色："))
(putchar 27)
(print_str "[0m")
(newline)

(putchar 27)
(print_str "[9;1H")
(putchar 27)
(print_str "[41m")
(if lang (print_str " red bg ") (print_str " 红底 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[42m")
(if lang (print_str " green bg ") (print_str " 绿底 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[44m")
(if lang (print_str " blue bg ") (print_str " 蓝底 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[46m")
(if lang (print_str " cyan bg ") (print_str " 青底 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[103m")
(if lang (print_str " bright yellow bg ") (print_str " 亮黄底 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[105m")
(if lang (print_str " bright magenta bg ") (print_str " 亮品红底 "))
(putchar 27)
(print_str "[0m")
(newline)

; ── 样式（1 粗 / 2 暗 / 3 斜 / 4 下划线 / 9 删除线）─────────────
; -- Styles (1 bold / 2 dim / 3 italic / 4 underline / 9 strikethrough) --
(putchar 27)
(print_str "[11;1H")
(putchar 27)
(print_str "[1;37m")
(if lang (print_str "Styles:") (print_str "样式："))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[1m")
(if lang (print_str " bold ") (print_str " 粗体 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[2m")
(if lang (print_str " dim ") (print_str " 暗淡 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[3m")
(if lang (print_str " italic ") (print_str " 斜体 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[4m")
(if lang (print_str " underline ") (print_str " 下划线 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[9m")
(if lang (print_str " strike ") (print_str " 删除线 "))
(putchar 27)
(print_str "[0m")
(newline)

; ── 256 色（38;5;N）与真彩（38;2;r;g;b）─────────────────────────
; -- 256 colors (38;5;N) and true color (38;2;r;g;b) --
(putchar 27)
(print_str "[13;1H")
(putchar 27)
(print_str "[1;37m")
(if lang (print_str "256 colors / truecolor:") (print_str "256 色 / 真彩："))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[38;5;208m")
(if lang (print_str " 256-208 orange ") (print_str " 256-208 橙 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[38;5;46m")
(if lang (print_str " 256-46 bright green ") (print_str " 256-46 亮绿 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[38;2;255;128;0m")
(if lang (print_str " truecolor orange ") (print_str " 真彩橙 "))
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[48;2;60;0;90m")
(if lang (print_str " truecolor deep purple bg ") (print_str " 真彩深紫底 "))
(putchar 27)
(print_str "[0m")
(newline)

; ── 循环画一条色带（12 格，颜色在 8 档里循环）──────────────────
; -- Draw a color band in a loop (12 cells, colors cycling through 8 steps) --
; 「算出来的」那一格：取模 + 分支挑字面量色码。
; The "computed" cell: modulo plus a branch picking a literal color code.
; `do` 的循环体在**顶层**执行 ⇒ 里面可以直接调库函数（用户函数里则不行）。
; A `do` loop body runs at the **top level** => it can call library functions directly (a user function cannot).
(putchar 27)
(print_str "[16;1H")
(putchar 27)
(print_str "[1;37m")
(if lang (print_str "Draw a 12-cell color band in a loop:") (print_str "循环画 12 格色带："))
(putchar 27)
(print_str "[0m")
(newline)

(putchar 27)
(print_str "[17;1H")
(do ((i 0 (+ i 1))) ((>= i 12) 0)
    (if (= (- i (* (/ i 8) 8)) 0) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 0) (print_str "[41m") 0)
    (if (= (- i (* (/ i 8) 8)) 1) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 1) (print_str "[42m") 0)
    (if (= (- i (* (/ i 8) 8)) 2) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 2) (print_str "[43m") 0)
    (if (= (- i (* (/ i 8) 8)) 3) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 3) (print_str "[44m") 0)
    (if (= (- i (* (/ i 8) 8)) 4) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 4) (print_str "[45m") 0)
    (if (= (- i (* (/ i 8) 8)) 5) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 5) (print_str "[46m") 0)
    (if (= (- i (* (/ i 8) 8)) 6) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 6) (print_str "[47m") 0)
    (if (= (- i (* (/ i 8) 8)) 7) (putchar 27) 0)
    (if (= (- i (* (/ i 8) 8)) 7) (print_str "[100m") 0)
    (print_str "   ")
    (putchar 27)
    (print_str "[0m"))
(newline)

; ── 收尾：复位颜色 + 定位到第 20 行写结束语 ─────────────────────
; -- Wrap-up: reset the colors + position to row 20 for the closing line --
(putchar 27)
(print_str "[0m")
(putchar 27)
(print_str "[20;1H")
(putchar 27)
(print_str "[1;32m")
(if lang (print_str "demo_tty done -- no key wait, draws and exits.") (print_str "demo_tty 结束 —— 没有按键等待，画完即退出。"))
(putchar 27)
(print_str "[0m")
(newline)
