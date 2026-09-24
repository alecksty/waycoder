// demo_tty.m —— **彩色控制台**示范（Objective-C）
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
//
// ◆ 为什么直接发 ANSI，而不是用 `#include <conio.h>`
//
// `Lib/c/conio.h` 能被 ObjC 前端**解析**（它没有 hex 字面量，不像 `graphics.h`），
// 头文件里的 `#param lib("conio")` 也会被预处理器收下 —— 但**符号解不开**：
//   · `clrscr` 这类名字不在 `SharedPrefixMap` 里 ⇒ `AutoDetectSharedLibs` 想不到 conio；
//   · 显式 `#param lib("conio")` + 直接调 `clrscr()`，实测报「未定义的函数 'clrscr'」，
//     而链接日志里**根本没有 conio.vml**。
// ⇒ 直接产出真终端会产出的字节，与 `Examples/c/conio_screen.c` 在命令行页上走**同一条
//    渲染链**（`AnsiMarkup`）。
//
// ◆ 写法（ObjC 前端的优势）
//
//   · **`\x1b` 转义是解析的**（与 Rust/Go 相反）⇒ 整条转义序列可以直接写进字符串；
//   · `printf` 不补换行（`puts` 补）⇒ 一行一条 CSI，换行自己写在末尾。
//
// 跑法：命令行页输入  vml run examples/objc/demo_tty.m

int main() {
    printf("\x1b[2J\n");
    printf("\x1b[H\n");
    printf("\x1b[0;1;44;97m\n");
    printf("\x1b[3;1H  === WayCoder 彩色控制台演示 (Objective-C)  ===\n");
    printf("\x1b[0;37m\n");
    printf("\x1b[5;1H标准 8 色前景（30-37）：\n");
    printf("\x1b[0;30m\n");
    printf("\x1b[6;1H  30 黑\n");
    printf("\x1b[0;31m\n");
    printf("\x1b[7;1H  31 红\n");
    printf("\x1b[0;32m\n");
    printf("\x1b[8;1H  32 绿\n");
    printf("\x1b[0;33m\n");
    printf("\x1b[9;1H  33 黄\n");
    printf("\x1b[0;34m\n");
    printf("\x1b[10;1H  34 蓝\n");
    printf("\x1b[0;35m\n");
    printf("\x1b[11;1H  35 品红\n");
    printf("\x1b[0;36m\n");
    printf("\x1b[12;1H  36 青\n");
    printf("\x1b[0;37m\n");
    printf("\x1b[13;1H  37 白\n");
    printf("\x1b[0;37m\n");
    printf("\x1b[15;1H亮色前景（90-97）与背景色：\n");
    printf("\x1b[0;91m\n");
    printf("\x1b[16;1H  91 亮红\n");
    printf("\x1b[0;92m\n");
    printf("\x1b[17;1H  92 亮绿\n");
    printf("\x1b[0;41m\n");
    printf("\x1b[18;1H  41 红底\n");
    printf("\x1b[0;104m\n");
    printf("\x1b[19;1H 104 亮蓝底\n");
    printf("\x1b[0;103m\n");
    printf("\x1b[20;1H 103 亮黄底\n");
    printf("\x1b[0;37m\n");
    printf("\x1b[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：\n");
    printf("\x1b[0;38;5;208m\n");
    printf("\x1b[23;1H  256-208 橙\n");
    printf("\x1b[0;38;5;46m\n");
    printf("\x1b[24;1H  256-46 亮绿\n");
    printf("\x1b[0;38;2;255;128;0m\n");
    printf("\x1b[25;1H  真彩 橙\n");
    printf("\x1b[0;38;2;0;200;255m\n");
    printf("\x1b[26;1H  真彩 青\n");
    printf("\x1b[0;36m\n");
    printf("\x1b[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：\n");
    printf("\x1b[30;5H+----------------+\n");
    printf("\x1b[31;5H|  定位画框      |\n");
    printf("\x1b[32;5H+----------------+\n");
    printf("\x1b[0;2m\n");
    printf("\x1b[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）\n");
    printf("\x1b[0;32m\n");
    printf("\x1b[36;1H结束 —— 正常退出\n");
    return 0;
}
