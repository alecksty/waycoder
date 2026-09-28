// demo_tty.d —— **彩色控制台**示范（D）
// demo_tty.d —— **color console** demo (D)
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
// The second of the four demo layers: clear the screen + set foreground/background colors + position the cursor + print colored text, finishing normally.
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/d/conio.vml`
// ◆ Why it emits ANSI directly instead of using `Lib/d/conio.vml`
//
// `Lib/d/conio.vml` 确实在 `Lib/d/` 下（GenLib 生成的包装器），但**它在这门前端上
// `Lib/d/conio.vml` really does sit under `Lib/d/` (a GenLib-generated wrapper), but **it
// 用不了**，三条路都实测过：
// cannot be used** on this frontend; all three routes were measured:
//   · 不写 `#param lib("conio")`：`clrscr` 这类名字不在 `SharedPrefixMap` 里 ⇒
//   · Without `#param lib("conio")`: names like `clrscr` are not in `SharedPrefixMap` ⇒
//     报「未定义的函数 'func_clrscr'」；
//     it reports "undefined function 'func_clrscr'";
//   · 写 `#param lib("conio")`：包**确实**被链了进来（链接日志有
//   · With `#param lib("conio")`: the package **is** linked in (the link log has
//     `成功链接: conio.vml`），但符号对不上 —— 包装器标签是 `d_clrscr`，
//     `successfully linked: conio.vml`), but the symbols do not match —— the wrapper label is `d_clrscr`,
//     而 `clrscr()` 编成 `func_clrscr`、`d_clrscr()` 编成 `func_d_clrscr`，两个都不解析；
//     while `clrscr()` compiles into `func_clrscr` and `d_clrscr()` into `func_d_clrscr`; neither resolves;
//   · `--lib <Lib/d/conio.vml>`：**完全没效果** —— `DCompiler.CompileFileWithIncludes`
//   · `--lib <Lib/d/conio.vml>`: **no effect at all** —— `DCompiler.CompileFileWithIncludes`
//     调 `CompileFile(filePath, includePaths, null, false)`，把 `libraryPaths` 写成了
//     calls `CompileFile(filePath, includePaths, null, false)`, passing `libraryPaths` as
//     `null`（见交付报告）。
//     `null` (see the delivery report).
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走**同一条
// ⇒ emit exactly the bytes a real terminal would emit, walking **the same
//    渲染链**（`AnsiMarkup`）。
//    rendering chain** (`AnsiMarkup`) as `Examples/c/conio_screen.c` on the command-line page.
//
// ◆ 写法（D 前端的优势）
// ◆ How to write it (the D frontend's advantage)
//
//   · **`\x1b` 转义是解析的**（与 Rust/Go 相反）⇒ 整条转义序列可以直接写进字符串，
//   · **`\x1b` escapes are parsed** (unlike Rust/Go) ⇒ a whole escape sequence can go straight into a string,
//     一行一条 CSI，不必像 Rust/Go 那样拆成两条语句。
//     one CSI per line, with no need to split it into two statements as Rust/Go do.
//
// 跑法：命令行页输入  vml run examples/d/demo_tty.d
// How to run: type this on the command-line page  vml run examples/d/demo_tty.d

void main() {
    int lang = ui_get_language();
    writeln("\x1b[2J");
    writeln("\x1b[H");
    writeln("\x1b[0;1;44;97m");
    if (lang == 0) { writeln("\x1b[3;1H  === WayCoder 彩色控制台演示 (D)  ==="); } else { writeln("\x1b[3;1H  === WayCoder color console demo (D)  ==="); }
    writeln("\x1b[0;37m");
    if (lang == 0) { writeln("\x1b[5;1H标准 8 色前景（30-37）："); } else { writeln("\x1b[5;1HStandard 8 foreground colors (30-37):"); }
    writeln("\x1b[0;30m");
    if (lang == 0) { writeln("\x1b[6;1H  30 黑"); } else { writeln("\x1b[6;1H  30 black"); }
    writeln("\x1b[0;31m");
    if (lang == 0) { writeln("\x1b[7;1H  31 红"); } else { writeln("\x1b[7;1H  31 red"); }
    writeln("\x1b[0;32m");
    if (lang == 0) { writeln("\x1b[8;1H  32 绿"); } else { writeln("\x1b[8;1H  32 green"); }
    writeln("\x1b[0;33m");
    if (lang == 0) { writeln("\x1b[9;1H  33 黄"); } else { writeln("\x1b[9;1H  33 yellow"); }
    writeln("\x1b[0;34m");
    if (lang == 0) { writeln("\x1b[10;1H  34 蓝"); } else { writeln("\x1b[10;1H  34 blue"); }
    writeln("\x1b[0;35m");
    if (lang == 0) { writeln("\x1b[11;1H  35 品红"); } else { writeln("\x1b[11;1H  35 magenta"); }
    writeln("\x1b[0;36m");
    if (lang == 0) { writeln("\x1b[12;1H  36 青"); } else { writeln("\x1b[12;1H  36 cyan"); }
    writeln("\x1b[0;37m");
    if (lang == 0) { writeln("\x1b[13;1H  37 白"); } else { writeln("\x1b[13;1H  37 white"); }
    writeln("\x1b[0;37m");
    if (lang == 0) { writeln("\x1b[15;1H亮色前景（90-97）与背景色："); } else { writeln("\x1b[15;1HBright foreground (90-97) and background colors:"); }
    writeln("\x1b[0;91m");
    if (lang == 0) { writeln("\x1b[16;1H  91 亮红"); } else { writeln("\x1b[16;1H  91 bright red"); }
    writeln("\x1b[0;92m");
    if (lang == 0) { writeln("\x1b[17;1H  92 亮绿"); } else { writeln("\x1b[17;1H  92 bright green"); }
    writeln("\x1b[0;41m");
    if (lang == 0) { writeln("\x1b[18;1H  41 红底"); } else { writeln("\x1b[18;1H  41 red bg"); }
    writeln("\x1b[0;104m");
    if (lang == 0) { writeln("\x1b[19;1H 104 亮蓝底"); } else { writeln("\x1b[19;1H 104 bright blue bg"); }
    writeln("\x1b[0;103m");
    if (lang == 0) { writeln("\x1b[20;1H 103 亮黄底"); } else { writeln("\x1b[20;1H 103 bright yellow bg"); }
    writeln("\x1b[0;37m");
    if (lang == 0) { writeln("\x1b[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）："); } else { writeln("\x1b[22;1H256-color (38;5;N) and truecolor (38;2;r;g;b):"); }
    writeln("\x1b[0;38;5;208m");
    if (lang == 0) { writeln("\x1b[23;1H  256-208 橙"); } else { writeln("\x1b[23;1H  256-208 orange"); }
    writeln("\x1b[0;38;5;46m");
    if (lang == 0) { writeln("\x1b[24;1H  256-46 亮绿"); } else { writeln("\x1b[24;1H  256-46 bright green"); }
    writeln("\x1b[0;38;2;255;128;0m");
    if (lang == 0) { writeln("\x1b[25;1H  真彩 橙"); } else { writeln("\x1b[25;1H  truecolor orange"); }
    writeln("\x1b[0;38;2;0;200;255m");
    if (lang == 0) { writeln("\x1b[26;1H  真彩 青"); } else { writeln("\x1b[26;1H  truecolor cyan"); }
    writeln("\x1b[0;36m");
    if (lang == 0) { writeln("\x1b[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框："); } else { writeln("\x1b[28;1HCursor positioning (move the cursor to row 30, column 5) + draw a box:"); }
    writeln("\x1b[30;5H+----------------+");
    if (lang == 0) { writeln("\x1b[31;5H|  定位画框      |"); } else { writeln("\x1b[31;5H|  located box   |"); }
    writeln("\x1b[32;5H+----------------+");
    writeln("\x1b[0;2m");
    if (lang == 0) { writeln("\x1b[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）"); } else { writeln("\x1b[34;1H(all of the above are ANSI SGR sequences, rendered by the terminal / AnsiMarkup on the command-line page)"); }
    writeln("\x1b[0;32m");
    if (lang == 0) { writeln("\x1b[36;1H结束 —— 正常退出"); } else { writeln("\x1b[36;1Hdone -- exiting normally"); }
}
