// demo_tty.go —— **彩色控制台**示范（Go）
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/go/conio.vml`
//
// 与 Rust 那份同因（三条路都实测过，详见 `Examples/rust/demo_tty.rs` 的长注释）：
// `Lib/go/conio.vml` 不在 `SharedPrefixMap` 里 ⇒ 自动链接想不到它；显式
// `#param lib("conio")` 也解不开符号 —— 包装器的标签形如 `go_clrscr`，
// 与用户侧编出来的 `clrscr` / `go_clrscr` 都对不上。
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走同一条
//    渲染链（`AnsiMarkup`）。
//
// ◆ 写法（Go 前端的实测约束）
//
//   · **`\x1b` 不解析**（原样打出 `x1b`）⇒ ESC 这个字节交给 `putchar(27)` 发；
//   · **`print` / `println` 都会补换行**（没有"不换行的打印"）⇒ 每行一条 CSI。
//     （`printx_string` 在 Go 上是可靠的，但这一份用不到它。）
//
// ◆ 形状说明见 `Examples/rust/demo_tty.rs`：**每行恰好一条 CSI**，多条 SGR 合并进同一条。
//
// 跑法：命令行页输入  vml run examples/go/demo_tty.go

package main

func main() {
	putchar(27)
	println("[2J")
	putchar(27)
	println("[H")
	putchar(27)
	println("[0;1;44;97m")
	putchar(27)
	println("[3;1H  === WayCoder 彩色控制台演示 (Go)  ===")
	putchar(27)
	println("[0;37m")
	putchar(27)
	println("[5;1H标准 8 色前景（30-37）：")
	putchar(27)
	println("[0;30m")
	putchar(27)
	println("[6;1H  30 黑")
	putchar(27)
	println("[0;31m")
	putchar(27)
	println("[7;1H  31 红")
	putchar(27)
	println("[0;32m")
	putchar(27)
	println("[8;1H  32 绿")
	putchar(27)
	println("[0;33m")
	putchar(27)
	println("[9;1H  33 黄")
	putchar(27)
	println("[0;34m")
	putchar(27)
	println("[10;1H  34 蓝")
	putchar(27)
	println("[0;35m")
	putchar(27)
	println("[11;1H  35 品红")
	putchar(27)
	println("[0;36m")
	putchar(27)
	println("[12;1H  36 青")
	putchar(27)
	println("[0;37m")
	putchar(27)
	println("[13;1H  37 白")
	putchar(27)
	println("[0;37m")
	putchar(27)
	println("[15;1H亮色前景（90-97）与背景色：")
	putchar(27)
	println("[0;91m")
	putchar(27)
	println("[16;1H  91 亮红")
	putchar(27)
	println("[0;92m")
	putchar(27)
	println("[17;1H  92 亮绿")
	putchar(27)
	println("[0;41m")
	putchar(27)
	println("[18;1H  41 红底")
	putchar(27)
	println("[0;104m")
	putchar(27)
	println("[19;1H 104 亮蓝底")
	putchar(27)
	println("[0;103m")
	putchar(27)
	println("[20;1H 103 亮黄底")
	putchar(27)
	println("[0;37m")
	putchar(27)
	println("[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：")
	putchar(27)
	println("[0;38;5;208m")
	putchar(27)
	println("[23;1H  256-208 橙")
	putchar(27)
	println("[0;38;5;46m")
	putchar(27)
	println("[24;1H  256-46 亮绿")
	putchar(27)
	println("[0;38;2;255;128;0m")
	putchar(27)
	println("[25;1H  真彩 橙")
	putchar(27)
	println("[0;38;2;0;200;255m")
	putchar(27)
	println("[26;1H  真彩 青")
	putchar(27)
	println("[0;36m")
	putchar(27)
	println("[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：")
	putchar(27)
	println("[30;5H+----------------+")
	putchar(27)
	println("[31;5H|  定位画框      |")
	putchar(27)
	println("[32;5H+----------------+")
	putchar(27)
	println("[0;2m")
	putchar(27)
	println("[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）")
	putchar(27)
	println("[0;32m")
	putchar(27)
	println("[36;1H结束 —— 正常退出")
}
