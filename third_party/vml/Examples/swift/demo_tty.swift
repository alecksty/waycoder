// demo_tty.swift —— **彩色控制台**示范（Swift）
// demo_tty.swift — a **colored console** demo (Swift)
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
// Layer 2 of the four-layer demo: clear screen + set fg/bg colors + position the cursor + print colored text, terminating normally.
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/swift/conio.vml`
// ◆ Why it emits ANSI directly instead of using `Lib/swift/conio.vml`
//
// 与 Rust / Go 那份同因（详见 `Examples/rust/demo_tty.rs` 的长注释）：
// Same cause as the Rust / Go copies (see the long comment in `Examples/rust/demo_tty.rs`):
// `Lib/swift/conio.vml` 是 GenLib 生成的包装器，`clrscr` 这类名字不在
// `Lib/swift/conio.vml` is a GenLib-generated wrapper, and names like `clrscr` are not in
// `SharedPrefixMap` 里 ⇒ 自动链接想不到 conio；显式 `#param lib("conio")` 也解不开 ——
// `SharedPrefixMap` ⇒ automatic linking never thinks of conio; an explicit `#param lib("conio")` cannot resolve it either —
// 包装器标签形如 `swift_clrscr`，与用户侧编出来的 `clrscr` / `swift_clrscr`
// the wrapper labels look like `swift_clrscr`, which match neither the user-side `clrscr` nor
// 都对不上（实测三条路全报「未定义的函数」）。
// `swift_clrscr` (measured: all three routes report "undefined function").
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走同一条
// ⇒ It emits the same bytes a real terminal would, going through the same
//    渲染链（`AnsiMarkup`）。
//    render chain as `Examples/c/conio_screen.c` on the command-line page (`AnsiMarkup`).
//
// ◆ 写法（Swift 前端的实测约束）
// ◆ Style (measured constraints of the Swift frontend)
//
//   · **`\x1b` 不解析**、`\u{1B}` 也不认 ⇒ ESC 这个字节交给 `putchar(27)`；
//   · **`\x1b` is not parsed** and `\u{1B}` is not recognized either ⇒ the ESC byte goes through `putchar(27)`;
//   · `print` 会补换行、而且**多个实参之间不加分隔符**（`print("a", b)` → `ab`）；
//   · `print` adds a newline and **no separator** between multiple arguments (`print("a", b)` → `ab`);
//     要"不换行的打印"用 `print_str_no_nl`（实测可靠，与 Rust 那边的同名函数不同）。
//     for "print without a newline" use `print_str_no_nl` (measured reliable, unlike the same-named function on the Rust side).
//
// ◆ 形状说明见 `Examples/rust/demo_tty.rs`：**每行恰好一条 CSI**。
// ◆ For the shape see `Examples/rust/demo_tty.rs`: **exactly one CSI per line**.
//
// 跑法：命令行页输入  vml run examples/swift/demo_tty.swift
// How to run: on the command-line page type  vml run examples/swift/demo_tty.swift

// 界面语言：开局查一次（`ui_get_language` 是 syscall，别每处都调）
// UI language: queried once at start (`ui_get_language` is a syscall, do not call it everywhere)
var lang = ui_get_language()

// ⚠ 分支文字用**单行 `if/else`** 而不是三元 —— 本前端 `print(非字面量)` 打的是**指针**（实测），
// ⚠ Branched text uses a **one-line `if/else`** instead of a ternary -- this frontend's `print(non-literal)` prints a **pointer** (measured),
//   只有**字面量**实参才真的打字符串；`print` 自带的那一个换行正好当每行的收尾。
//   and only a **literal** argument really prints the string; the newline `print` adds is exactly the line ending wanted.

putchar(27)
print("[2J")
putchar(27)
print("[H")
putchar(27)
print("[0;1;44;97m")
putchar(27)
if lang == 0 { print("[3;1H  === WayCoder 彩色控制台演示 (Swift)  ===") } else { print("[3;1H  === WayCoder color console demo (Swift)  ===") }
putchar(27)
print("[0;37m")
putchar(27)
if lang == 0 { print("[5;1H标准 8 色前景（30-37）：") } else { print("[5;1Hstandard 8 foreground colors (30-37):") }
putchar(27)
print("[0;30m")
putchar(27)
if lang == 0 { print("[6;1H  30 黑") } else { print("[6;1H  30 black") }
putchar(27)
print("[0;31m")
putchar(27)
if lang == 0 { print("[7;1H  31 红") } else { print("[7;1H  31 red") }
putchar(27)
print("[0;32m")
putchar(27)
if lang == 0 { print("[8;1H  32 绿") } else { print("[8;1H  32 green") }
putchar(27)
print("[0;33m")
putchar(27)
if lang == 0 { print("[9;1H  33 黄") } else { print("[9;1H  33 yellow") }
putchar(27)
print("[0;34m")
putchar(27)
if lang == 0 { print("[10;1H  34 蓝") } else { print("[10;1H  34 blue") }
putchar(27)
print("[0;35m")
putchar(27)
if lang == 0 { print("[11;1H  35 品红") } else { print("[11;1H  35 magenta") }
putchar(27)
print("[0;36m")
putchar(27)
if lang == 0 { print("[12;1H  36 青") } else { print("[12;1H  36 cyan") }
putchar(27)
print("[0;37m")
putchar(27)
if lang == 0 { print("[13;1H  37 白") } else { print("[13;1H  37 white") }
putchar(27)
print("[0;37m")
putchar(27)
if lang == 0 { print("[15;1H亮色前景（90-97）与背景色：") } else { print("[15;1Hbright foreground (90-97) and backgrounds:") }
putchar(27)
print("[0;91m")
putchar(27)
if lang == 0 { print("[16;1H  91 亮红") } else { print("[16;1H  91 bright red") }
putchar(27)
print("[0;92m")
putchar(27)
if lang == 0 { print("[17;1H  92 亮绿") } else { print("[17;1H  92 bright green") }
putchar(27)
print("[0;41m")
putchar(27)
if lang == 0 { print("[18;1H  41 红底") } else { print("[18;1H  41 red bg") }
putchar(27)
print("[0;104m")
putchar(27)
if lang == 0 { print("[19;1H 104 亮蓝底") } else { print("[19;1H 104 bright blue bg") }
putchar(27)
print("[0;103m")
putchar(27)
if lang == 0 { print("[20;1H 103 亮黄底") } else { print("[20;1H 103 bright yellow bg") }
putchar(27)
print("[0;37m")
putchar(27)
if lang == 0 { print("[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：") } else { print("[22;1H256 colors (38;5;N) and true color (38;2;r;g;b):") }
putchar(27)
print("[0;38;5;208m")
putchar(27)
if lang == 0 { print("[23;1H  256-208 橙") } else { print("[23;1H  256-208 orange") }
putchar(27)
print("[0;38;5;46m")
putchar(27)
if lang == 0 { print("[24;1H  256-46 亮绿") } else { print("[24;1H  256-46 bright green") }
putchar(27)
print("[0;38;2;255;128;0m")
putchar(27)
if lang == 0 { print("[25;1H  真彩 橙") } else { print("[25;1H  true color orange") }
putchar(27)
print("[0;38;2;0;200;255m")
putchar(27)
if lang == 0 { print("[26;1H  真彩 青") } else { print("[26;1H  true color cyan") }
putchar(27)
print("[0;36m")
putchar(27)
if lang == 0 { print("[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：") } else { print("[28;1Hcursor positioning (move to row 30, col 5) + a box:") }
putchar(27)
print("[30;5H+----------------+")
putchar(27)
if lang == 0 { print("[31;5H|  定位画框      |") } else { print("[31;5H|  position box  |") }
putchar(27)
print("[32;5H+----------------+")
putchar(27)
print("[0;2m")
putchar(27)
if lang == 0 { print("[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）") } else { print("[34;1H(all of the above are ANSI SGR sequences, rendered by the terminal / the command-line page AnsiMarkup)") }
putchar(27)
print("[0;32m")
putchar(27)
if lang == 0 { print("[36;1H结束 —— 正常退出") } else { print("[36;1Hdone -- clean exit") }
