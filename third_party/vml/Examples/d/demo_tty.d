// demo_tty.d —— **彩色控制台**示范（D）
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/d/conio.vml`
//
// `Lib/d/conio.vml` 确实在 `Lib/d/` 下（GenLib 生成的包装器），但**它在这门前端上
// 用不了**，三条路都实测过：
//   · 不写 `#param lib("conio")`：`clrscr` 这类名字不在 `SharedPrefixMap` 里 ⇒
//     报「未定义的函数 'func_clrscr'」；
//   · 写 `#param lib("conio")`：包**确实**被链了进来（链接日志有
//     `成功链接: conio.vml`），但符号对不上 —— 包装器标签是 `d_clrscr`，
//     而 `clrscr()` 编成 `func_clrscr`、`d_clrscr()` 编成 `func_d_clrscr`，两个都不解析；
//   · `--lib <Lib/d/conio.vml>`：**完全没效果** —— `DCompiler.CompileFileWithIncludes`
//     调 `CompileFile(filePath, includePaths, null, false)`，把 `libraryPaths` 写成了
//     `null`（见交付报告）。
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走**同一条
//    渲染链**（`AnsiMarkup`）。
//
// ◆ 写法（D 前端的优势）
//
//   · **`\x1b` 转义是解析的**（与 Rust/Go 相反）⇒ 整条转义序列可以直接写进字符串，
//     一行一条 CSI，不必像 Rust/Go 那样拆成两条语句。
//
// 跑法：命令行页输入  vml run examples/d/demo_tty.d

void main() {
    writeln("\x1b[2J");
    writeln("\x1b[H");
    writeln("\x1b[0;1;44;97m");
    writeln("\x1b[3;1H  === WayCoder 彩色控制台演示 (D)  ===");
    writeln("\x1b[0;37m");
    writeln("\x1b[5;1H标准 8 色前景（30-37）：");
    writeln("\x1b[0;30m");
    writeln("\x1b[6;1H  30 黑");
    writeln("\x1b[0;31m");
    writeln("\x1b[7;1H  31 红");
    writeln("\x1b[0;32m");
    writeln("\x1b[8;1H  32 绿");
    writeln("\x1b[0;33m");
    writeln("\x1b[9;1H  33 黄");
    writeln("\x1b[0;34m");
    writeln("\x1b[10;1H  34 蓝");
    writeln("\x1b[0;35m");
    writeln("\x1b[11;1H  35 品红");
    writeln("\x1b[0;36m");
    writeln("\x1b[12;1H  36 青");
    writeln("\x1b[0;37m");
    writeln("\x1b[13;1H  37 白");
    writeln("\x1b[0;37m");
    writeln("\x1b[15;1H亮色前景（90-97）与背景色：");
    writeln("\x1b[0;91m");
    writeln("\x1b[16;1H  91 亮红");
    writeln("\x1b[0;92m");
    writeln("\x1b[17;1H  92 亮绿");
    writeln("\x1b[0;41m");
    writeln("\x1b[18;1H  41 红底");
    writeln("\x1b[0;104m");
    writeln("\x1b[19;1H 104 亮蓝底");
    writeln("\x1b[0;103m");
    writeln("\x1b[20;1H 103 亮黄底");
    writeln("\x1b[0;37m");
    writeln("\x1b[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：");
    writeln("\x1b[0;38;5;208m");
    writeln("\x1b[23;1H  256-208 橙");
    writeln("\x1b[0;38;5;46m");
    writeln("\x1b[24;1H  256-46 亮绿");
    writeln("\x1b[0;38;2;255;128;0m");
    writeln("\x1b[25;1H  真彩 橙");
    writeln("\x1b[0;38;2;0;200;255m");
    writeln("\x1b[26;1H  真彩 青");
    writeln("\x1b[0;36m");
    writeln("\x1b[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：");
    writeln("\x1b[30;5H+----------------+");
    writeln("\x1b[31;5H|  定位画框      |");
    writeln("\x1b[32;5H+----------------+");
    writeln("\x1b[0;2m");
    writeln("\x1b[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）");
    writeln("\x1b[0;32m");
    writeln("\x1b[36;1H结束 —— 正常退出");
}
