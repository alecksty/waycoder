// demo_std.m —— **标准输出**示范（Objective-C）
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// 输出**逐字节确定**，能正常结束。
//
// ◆ 写法（ObjC 前端的实测约束）
//
//   · ObjC 前端是**兼容 C** 的那一套 ⇒ `printf` / `puts` 都能用。
//   · **词法器十进制专用，不认 `0x`**（`0xAA0000` 会被拆成 `0` + 标识符 `xAA0000`，
//     报「未声明的变量 'xAA0000'」）⇒ 颜色只能写负数十进制，见 `demo_tty.m`。
//   · `\x1b` 转义**本前端是解析的**（与 Rust/Go 相反），所以 `demo_tty.m` 直接
//     把转义写进字符串即可。
//   · **不 `#include <waycoder_ui.h>`** —— ObjC 前端解析不了那个头文件
//     （`expected ) (got IdType 'id'`，报在头文件里）；`ui_*` 直接裸调、
//     由链接器解析到 `lib_vmlui_ui_*`（同 `snake.m` / `sysinfo.m`）。
//
// 跑法：命令行页输入  vml run examples/objc/demo_std.m

int main() {
    printf("=== WayCoder demo_std (Objective-C) ===\n");

    // ① 字符串
    puts("[字符串] hello, world");

    // ② 整数
    int n = 42;
    printf("[整数] n = %d\n", n);

    // ③ 计算结果
    printf("[计算] 6 * 7 = %d\n", 6 * 7);
    int a = 7;
    int b = 5;
    printf("[计算] a + b = %d\n", a + b);
    printf("[计算] a * b - 3 = %d\n", a * b - 3);

    // ④ 循环里算斐波那契前 10 项
    puts("[循环] 斐波那契前 10 项：");
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
    int sum = 0;
    int k = 1;
    while (k <= 100) {
        sum = sum + k;
        k = k + 1;
    }
    printf("[累加] 1+2+...+100 = %d\n", sum);

    // ⑥ 阶乘 5!
    int fact = 1;
    int m = 1;
    while (m <= 5) {
        fact = fact * m;
        m = m + 1;
    }
    printf("[阶乘] 5! = %d\n", fact);

    puts("=== 结束 ===");
    return 0;
}
