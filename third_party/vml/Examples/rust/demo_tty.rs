// demo_tty.rs —— **彩色控制台**示范（Rust）
//
// 四层示范的第二层：清屏 + 设前景/背景色 + 定位光标 + 打印彩色文字，正常结束。
//
// ◆ 为什么直接发 ANSI，而不是用 `Lib/rust/conio.vml`
//
// `Lib/rust/` 下确实有一份 `conio.vml`（GenLib 生成的包装器），但**它在这门前端上
// 用不了**，三条路都实测过：
//   · 不写 `#param lib("conio")`：`clrscr` 这类名字不在 `SharedPrefixMap` 里
//     （那张表决定自动链接）⇒ 报「未定义的函数 'func_clrscr'」；
//   · 写 `#param lib("conio")`：包**确实**被链了进来（链接日志里有
//     `成功链接: conio.vml`），但符号对不上 —— 包装器里定义的标签是 `rust_clrscr`
//     这种形状，而用户侧写 `clrscr()` 编成 `func_clrscr`、写 `rust_clrscr()` 编成
//     `func_rust_clrscr`，两个都解析不到；
//   · `--lib <Lib/rust/conio.vml>`：同样无效。
// ⇒ 这一层就直接产出**真终端会产出的那些字节**（清屏 `ESC[2J`、定位 `ESC[{行};{列}H`、
//    颜色 SGR）。那也是一份"支持彩色的控制台程序"，而且在命令行页上与
//    `Examples/c/conio_screen.c` 走**同一条渲染链**（`AnsiMarkup`）。
//
// ◆ Rust 发 ESC 的三条实测约束（这一份的形状全是被它们逼出来的）
//
//   · **`\x1b` 不解析**（原样打出四个字符 `x1b`），`\e` 也不认；
//   · **`\033` 会截断字符串** —— 它被解成 `\0`（NUL）+ 字面量 `33`，字符串在 NUL 处
//     终止。这与 C 前端那条已知缺陷同形，写 ANSI 的程序全都会踩；
//   · **`printx_string` / `print_str_no_nl` 都不可靠** —— 实测 `putchar(27)` +
//     `printx_string("[2J")` 之后**后面所有输出全部丢失**（连着调 `print_str_no_nl`
//     还会多写一个 `?`）；`print!` 更是直接报「未定义的函数 'rust_print'」。
//   ⇒ 唯一可靠的组合是 **`putchar(27)` + `println!`**：ESC 单独发一个字节，转义序列的
//     **其余部分**与正文一起写在 `println!` 的字面量里。
//
// ◆ 这个形状为什么是对的（一条 ANSI 常识，本仓踩过反面）
//
// `println!` 每行会补一个换行，所以**每行只能发一条 CSI**。要在一行上既定位又上色，
// 直觉写法是 `ESC[6;1H` + `ESC[0;30m` 两条 —— 那需要两次 `println!`。这一份把**多条 SGR
// 合并进同一条 CSI**（`ESC[0;1;44;97m` = 复位 + 粗体 + 蓝底 + 亮白前景），于是"上色"与
// "定位"各占一行：上色那行只发 CSI、不带正文（多出来的换行白走一行，无所谓），
// 定位那行把正文写在 `ESC[{行};{列}H` 后面。**每行都绝对定位** ⇒ 中间空走的换行不串位。
// ⚠ **反面写法**（本仓踩过）：`ESC[6;1H[0m[30m` —— 只有第一条 `ESC[6;1H` 是转义序列，
//   后面 `[0m[30m` 会被终端当成**正文**打出来。多段转义必须各带自己的 ESC。
//
// 跑法：命令行页输入  vml run examples/rust/demo_tty.rs

fn main() {
    putchar(27);
    println!("[2J");
    putchar(27);
    println!("[H");
    putchar(27);
    println!("[0;1;44;97m");
    putchar(27);
    println!("[3;1H  === WayCoder 彩色控制台演示 (Rust)  ===");
    putchar(27);
    println!("[0;37m");
    putchar(27);
    println!("[5;1H标准 8 色前景（30-37）：");
    putchar(27);
    println!("[0;30m");
    putchar(27);
    println!("[6;1H  30 黑");
    putchar(27);
    println!("[0;31m");
    putchar(27);
    println!("[7;1H  31 红");
    putchar(27);
    println!("[0;32m");
    putchar(27);
    println!("[8;1H  32 绿");
    putchar(27);
    println!("[0;33m");
    putchar(27);
    println!("[9;1H  33 黄");
    putchar(27);
    println!("[0;34m");
    putchar(27);
    println!("[10;1H  34 蓝");
    putchar(27);
    println!("[0;35m");
    putchar(27);
    println!("[11;1H  35 品红");
    putchar(27);
    println!("[0;36m");
    putchar(27);
    println!("[12;1H  36 青");
    putchar(27);
    println!("[0;37m");
    putchar(27);
    println!("[13;1H  37 白");
    putchar(27);
    println!("[0;37m");
    putchar(27);
    println!("[15;1H亮色前景（90-97）与背景色：");
    putchar(27);
    println!("[0;91m");
    putchar(27);
    println!("[16;1H  91 亮红");
    putchar(27);
    println!("[0;92m");
    putchar(27);
    println!("[17;1H  92 亮绿");
    putchar(27);
    println!("[0;41m");
    putchar(27);
    println!("[18;1H  41 红底");
    putchar(27);
    println!("[0;104m");
    putchar(27);
    println!("[19;1H 104 亮蓝底");
    putchar(27);
    println!("[0;103m");
    putchar(27);
    println!("[20;1H 103 亮黄底");
    putchar(27);
    println!("[0;37m");
    putchar(27);
    println!("[22;1H256 色（38;5;N）与真彩（38;2;r;g;b）：");
    putchar(27);
    println!("[0;38;5;208m");
    putchar(27);
    println!("[23;1H  256-208 橙");
    putchar(27);
    println!("[0;38;5;46m");
    putchar(27);
    println!("[24;1H  256-46 亮绿");
    putchar(27);
    println!("[0;38;2;255;128;0m");
    putchar(27);
    println!("[25;1H  真彩 橙");
    putchar(27);
    println!("[0;38;2;0;200;255m");
    putchar(27);
    println!("[26;1H  真彩 青");
    putchar(27);
    println!("[0;36m");
    putchar(27);
    println!("[28;1H光标定位（把光标移到第 30 行第 5 列）+ 画框：");
    putchar(27);
    println!("[30;5H+----------------+");
    putchar(27);
    println!("[31;5H|  定位画框      |");
    putchar(27);
    println!("[32;5H+----------------+");
    putchar(27);
    println!("[0;2m");
    putchar(27);
    println!("[34;1H（以上全部是 ANSI SGR 序列，由终端 / 命令行页的 AnsiMarkup 渲染）");
    putchar(27);
    println!("[0;32m");
    putchar(27);
    println!("[36;1H结束 —— 正常退出");
}
