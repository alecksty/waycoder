// demo_tty.go —— **彩色控制台**示范（Go）
// demo_tty.go -- the **color console** demo (Go)
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
// The second of the four demo layers: clear the screen + set foreground/background colors + position the cursor + print colored text, then exit normally.
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/go/conio.vml`
// ◆ Why emit ANSI directly instead of using `Lib/go/conio.vml`
//
// 与 Rust 那份同因（三条路都实测过，详见 `Examples/rust/demo_tty.rs` 的长注释）：
// The same reason as the Rust one (all three routes were measured; see the long comments in `Examples/rust/demo_tty.rs`):
// `Lib/go/conio.vml` 不在 `SharedPrefixMap` 里 ⇒ 自动链接想不到它；显式
// `Lib/go/conio.vml` is not in `SharedPrefixMap` => auto-linking never considers it; and an explicit
// `#param lib("conio")` 也解不开符号 —— 包装器的标签形如 `go_clrscr`，
// `#param lib("conio")` does not resolve the symbols either -- the wrapper's labels look like `go_clrscr`,
// 与用户侧编出来的 `clrscr` / `go_clrscr` 都对不上。
// which matches neither the `clrscr` nor the `go_clrscr` compiled on the user side.
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走同一条
// => It directly emits the bytes a real terminal would emit, and on the command-line page it goes through the same
//    渲染链（`AnsiMarkup`）。
//    rendering chain (`AnsiMarkup`) as `Examples/c/conio_screen.c`.
//
// ◆ 写法（Go 前端的实测约束）
// ◆ Style (measured constraints of the Go frontend)
//
//   · **`\x1b` 不解析**（原样打出 `x1b`）⇒ ESC 这个字节交给 `putchar(27)` 发；
//   · **`\x1b` is not parsed** (it prints `x1b` verbatim) => the ESC byte is emitted via `putchar(27)`;
//   · **`print` / `println` 都会补换行**（没有"不换行的打印"）⇒ 每行一条 CSI。
//   · **Both `print` and `println` append a newline** (there is no "print without newline") => one CSI per line.
//     （`printx_string` 在 Go 上是可靠的，但这一份用不到它。）
//     (`printx_string` is reliable on Go, but this file has no use for it.)
//
// ◆ 形状说明见 `Examples/rust/demo_tty.rs`：**每行恰好一条 CSI**，多条 SGR 合并进同一条。
// ◆ For why this shape, see `Examples/rust/demo_tty.rs`: **exactly one CSI per line**, with several SGR merged into it.
//
// 跑法：命令行页输入  vml run examples/go/demo_tty.go
// How to run: type this into the command-line page:  vml run examples/go/demo_tty.go

package main

func main() {
	lang := ui_get_language()

	putchar(27)
	println("[2J")
	putchar(27)
	println("[H")
	putchar(27)
	println("[0;1;44;97m")
	putchar(27)
	if lang == 0 { println("[3;1H  === WayCoder 彩色控制台演示 (Go)  ===") } else { println("[3;1H  === WayCoder color console demo (Go)  ===") }
	putchar(27)
	println("[0;37m")
	putchar(27)
	if lang == 0 { println("[5;1H标准 8 色前景（30-37）：") } else { println("[5;1HStandard 8 foreground colors (30-37):") }
	putchar(27)
	println("[0;30m")
	putchar(27)
	if lang == 0 { println("[6;1H  30 黑") } else { println("[6;1H  30 black") }
	putchar(27)
	println("[0;31m")
	putchar(27)
	if lang == 0 { println("[7;1H  31 红") } else { println("[7;1H  31 red") }
	putchar(27)
	println("[0;32m")
	putchar(27)
	if lang == 0 { println("[8;1H  32 绿") } else { println("[8;1H  32 green") }
	putchar(27)
	println("[0;33m")
	putchar(27)
	if lang == 0 { println("[9;1H  33 黄") } else { println("[9;1H  33 yellow") }
	putchar(27)
	println("[0;34m")
	putchar(27)
	if lang == 0 { println("[10;1H  34 蓝") } else { println("[10;1H  34 blue") }
	putchar(27)
	println("[0;35m")
	putchar(27)
	if lang == 0 { println("[11;1H  35 品红") } else { println("[11;1H  35 magenta") }
	putchar(27)
	println("[0;36m")
	putchar(27)
	if lang == 0 { println("[12;1H  36 青") } else { println("[12;1H  36 cyan") }
	putchar(27)
	println("[0;37m")
	putchar(27)
	if lang == 0 { println("[13;1H  37 白") } else { println("[13;1H  37 white") }
	putchar(27)
	println("[0;37m")
	putchar(27)
	if lang == 0 { println("[15;1H亮色前景（90-97）与背景色：") } else { println("[15;1HBright foreground (90-97) and background colors:") }
	putchar(27)
	println("[0;91m")
	putchar(27)
	if lang == 0 { println("[16;1H  91 亮红") } else { println("[16;1H  91 bright red") }
	putchar(27)
	println("[0;92m")
	putchar(27)
	if lang == 0 { println("[17;1H  92 亮绿") } else { println("[17;1H  92 bright green") }
	putchar(27)
	println("[0;41m")
	putchar(27)
	if lang == 0 { println("[18;1H  41 红底") } else { println("[18;1H  41 red bg") }
	putchar(27)
	println("[0;104m")
	putchar(27)
	if lang == 0 { println("[19;1H 104 亮蓝底") } else { println("[19;1H 104 bright blue bg") }
	putchar(27)
	println("[0;103m")
	putchar(27)
	if lang == 0 { println("[20;1H 103 亮黄底") } else { println("[20;1H 103 bright yellow bg") }
	putchar(27)
	println("[0;37m")
	putchar(27)
	if lang == 0 { println("[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：") } else { println("[22;1H256-color (38;5;N) and truecolor (38;2;r;g;b):") }
	putchar(27)
	println("[0;38;5;208m")
	putchar(27)
	if lang == 0 { println("[23;1H  256-208 橙") } else { println("[23;1H  256-208 orange") }
	putchar(27)
	println("[0;38;5;46m")
	putchar(27)
	if lang == 0 { println("[24;1H  256-46 亮绿") } else { println("[24;1H  256-46 bright green") }
	putchar(27)
	println("[0;38;2;255;128;0m")
	putchar(27)
	if lang == 0 { println("[25;1H  真彩 橙") } else { println("[25;1H  truecolor orange") }
	putchar(27)
	println("[0;38;2;0;200;255m")
	putchar(27)
	if lang == 0 { println("[26;1H  真彩 青") } else { println("[26;1H  truecolor cyan") }
	putchar(27)
	println("[0;36m")
	putchar(27)
	if lang == 0 { println("[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：") } else { println("[28;1HCursor positioning (move the cursor to row 30, column 5) + draw a box:") }
	putchar(27)
	println("[30;5H+----------------+")
	putchar(27)
	if lang == 0 { println("[31;5H|  定位画框      |") } else { println("[31;5H|  located box   |") }
	putchar(27)
	println("[32;5H+----------------+")
	putchar(27)
	println("[0;2m")
	putchar(27)
	if lang == 0 { println("[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）") } else { println("[34;1H(all of the above are ANSI SGR sequences, rendered by the terminal / AnsiMarkup on the command-line page)") }
	putchar(27)
	println("[0;32m")
	putchar(27)
	if lang == 0 { println("[36;1H结束 —— 正常退出") } else { println("[36;1Hdone -- exiting normally") }
}
