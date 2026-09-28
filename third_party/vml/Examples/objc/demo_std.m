// demo_std.m —— **标准输出**示范（Objective-C）
// demo_std.m — a **standard output** demo (Objective-C)
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// Layer 1 of the four-layer demo: use only this language's own standard output, **no input reading, no drawing, no dialogs**,
// 输出**逐字节确定**，能正常结束。
// with **byte-for-byte deterministic** output, terminating normally.
//
// ◆ 写法（ObjC 前端的实测约束）
// ◆ Style (measured constraints of the ObjC frontend)
//
//   · ObjC 前端是**兼容 C** 的那一套 ⇒ `printf` / `puts` 都能用。
//   · The ObjC frontend is the **C-compatible** one ⇒ `printf` / `puts` both work.
//   · **词法器十进制专用，不认 `0x`**（`0xAA0000` 会被拆成 `0` + 标识符 `xAA0000`，
//   · **The lexer is decimal-only and does not recognize `0x`** (`0xAA0000` is split into `0` + the identifier `xAA0000`,
//     报「未声明的变量 'xAA0000'」）⇒ 颜色只能写负数十进制，见 `demo_tty.m`。
//     reporting "undeclared variable 'xAA0000'") ⇒ colors can only be written as negative decimals, see `demo_tty.m`.
//   · `\x1b` 转义**本前端是解析的**（与 Rust/Go 相反），所以 `demo_tty.m` 直接
//   · The `\x1b` escape **is parsed by this frontend** (unlike Rust/Go), so `demo_tty.m` writes
//     把转义写进字符串即可。
//     the escape straight into the string.
//   · **不 `#include <waycoder_ui.h>`** —— ObjC 前端解析不了那个头文件
//   · **No `#include <waycoder_ui.h>`** — the ObjC frontend cannot parse that header
//     （`expected ) (got IdType 'id'`，报在头文件里）；`ui_*` 直接裸调、
//     (`expected ) (got IdType 'id'`, reported inside the header); `ui_*` is called bare,
//     由链接器解析到 `lib_vmlui_ui_*`（同 `snake.m` / `sysinfo.m`）。
//     and the linker resolves it to `lib_vmlui_ui_*` (same as `snake.m` / `sysinfo.m`).
//
// 跑法：命令行页输入  vml run examples/objc/demo_std.m
// How to run: on the command-line page type  vml run examples/objc/demo_std.m

int main() {
    int lang;
    lang = ui_get_language();

    printf("=== WayCoder demo_std (Objective-C) ===\n");

    // ① 字符串
    // ① Strings
    if (lang == 0) puts("[字符串] hello, world");
    else puts("[string] hello, world");

    // ② 整数
    // ② Integers
    int n = 42;
    if (lang == 0) printf("[整数] n = %d\n", n);
    else printf("[int] n = %d\n", n);

    // ③ 计算结果
    // ③ Computed results
    if (lang == 0) printf("[计算] 6 * 7 = %d\n", 6 * 7);
    else printf("[calc] 6 * 7 = %d\n", 6 * 7);
    int a = 7;
    int b = 5;
    if (lang == 0) printf("[计算] a + b = %d\n", a + b);
    else printf("[calc] a + b = %d\n", a + b);
    if (lang == 0) printf("[计算] a * b - 3 = %d\n", a * b - 3);
    else printf("[calc] a * b - 3 = %d\n", a * b - 3);

    // ④ 循环里算斐波那契前 10 项
    // ④ Compute the first 10 Fibonacci numbers in a loop
    if (lang == 0) puts("[循环] 斐波那契前 10 项：");
    else puts("[loop] first 10 Fibonacci numbers:");
    int x = 0;
    int y = 1;
    int i = 0;
    while (i < 10) {
        int z = x + y;
        x = y;
        y = z;
        printf("  fib = %d\n", x);
        i = i + 1;
    }

    // ⑤ 累加 1+2+…+100
    // ⑤ Sum 1+2+…+100
    int sum = 0;
    int k = 1;
    while (k <= 100) {
        sum = sum + k;
        k = k + 1;
    }
    if (lang == 0) printf("[累加] 1+2+...+100 = %d\n", sum);
    else printf("[sum] 1+2+...+100 = %d\n", sum);

    // ⑥ 阶乘 5!
    // ⑥ Factorial 5!
    int fact = 1;
    int m = 1;
    while (m <= 5) {
        fact = fact * m;
        m = m + 1;
    }
    if (lang == 0) printf("[阶乘] 5! = %d\n", fact);
    else printf("[factorial] 5! = %d\n", fact);

    if (lang == 0) puts("=== 结束 ===");
    else puts("=== done ===");
    return 0;
}
