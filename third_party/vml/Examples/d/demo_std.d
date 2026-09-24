// demo_std.d —— **标准输出**示范（D）
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// 输出**逐字节确定**，能正常结束。
//
// ◆ 写法（D 前端的实测约束）
//
//   · `writeln` 的实参之间**不加分隔符**（`writeln("n = ", 42)` → `n = 42`），
//     所以间距要自己写进字符串里。
//   · D **没有 extern / asm** ⇒ 裸调 `ui_rect(...)` 会被编成 `CALL func_ui_rect`，
//     靠链接器剥 `func_` 前缀解析到 `lib_vmlui_ui_rect`，不需要声明（见 `catch.d`）。
//   · 颜色写负数十进制 —— 词法器十进制专用，不认 `0x`（见 `catch.d` 文件头）。
//   · `\x1b` 转义**本前端是解析的**（与 Rust/Go 相反），见 `demo_tty.d`。
//
// 跑法：命令行页输入  vml run examples/d/demo_std.d

void main() {
    writeln("=== WayCoder demo_std (D) ===");

    // ① 字符串
    writeln("[字符串] hello, world");

    // ② 整数
    int n = 42;
    writeln("[整数] n = ", n);

    // ③ 计算结果
    writeln("[计算] 6 * 7 = ", 6 * 7);
    int a = 7;
    int b = 5;
    writeln("[计算] a + b = ", a + b);
    writeln("[计算] a * b - 3 = ", a * b - 3);

    // ④ 循环里算斐波那契前 10 项
    writeln("[循环] 斐波那契前 10 项：");
    int x = 0;
    int y = 1;
    int i = 0;
    while (i < 10) {
        int z = x + y;
        x = y;
        y = z;
        writeln("  fib = ", x);
        i = i + 1;
    }

    // ⑤ 累加 1+2+…+100
    int sum = 0;
    int k = 1;
    while (k <= 100) {
        sum = sum + k;
        k = k + 1;
    }
    writeln("[累加] 1+2+...+100 = ", sum);

    // ⑥ 阶乘 5!
    int fact = 1;
    int m = 1;
    while (m <= 5) {
        fact = fact * m;
        m = m + 1;
    }
    writeln("[阶乘] 5! = ", fact);

    writeln("=== 结束 ===");
}
