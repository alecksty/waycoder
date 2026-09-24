// demo_std.cs —— **第 1 层：标准输入输出**（C# 的 stdout）
//
// 这一层是每门语言自己的"往标准输出写文本"。
//
// ## ⚠⚠ 为什么这份**不用 `Console.Write/WriteLine`**
//
// 实测（2026-09-24，`vmlcli Examples/csharp/demo_std.cs`）：直接用 `Console.WriteLine`
// **链接期就过不去**：
//
//     <input>:6: error: 未定义的函数 'PrintlnStr'（引用 2 次）
//     <input>:8: error: 未定义的函数 'PrintlnInt'（引用 4 次）
//     <input>:9: error: 未定义的函数 'PrintStr'（引用 1 次）
//
// 原因：C# 前端把 `Console.WriteLine` 映射成标签 **`PrintlnStr` / `PrintlnInt`**
//（`CSharpCompiler/CodeGenerator.cs:989` 的 `memberLabels` 表），而这两个标签定义在
// **`Lib/csharp/console.vml`** 里 —— 那个模块**没有被挂进 csharp 的 `Libs`**
//（`vmltool.config.xml`：`<Language Name="csharp" Libs="vmlui.vml" />`，
//  `builtins.vml` 来自 `DefaultLibs`）。加一条 `--lib Lib/csharp/console.vml` 就正常，
// 所以这是**配置缺口**、不是能力缺口。
//
// 本份因此走**同一层的另一条可用路径**：`Lib/shared/io.vml` 里的
// `println_str` / `print_str` / `println_int` —— 那三个经 `builtins.vml`
// （`DefaultLibs`）进**每一门**语言的链接，Kotlin / Dart / Java 的例程用的也是它们。
// 换句话说：**这一层在 C# 里可达，只是入口名不是 `Console`。**
//
// ⚠ C# 前端的另一条已知缺口：`int[]` 传给库的 `int*` 形参时指针落在"长度头"上
//   （`callwithint8` 实测返回 -6，见 `demo_ui.cs` 的文件头）⇒ 本 demo 不碰数组传参。
//
// ## ⚠ 字符串拼接
//
// C# 的 `+` 拼接**不可用**（`"a" + n` 出来的不是那句话）⇒ 标签与值分两次写。
//
// ## 判据
//
//     vmlcli Examples/csharp/demo_std.cs
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。

class DemoStd
{
    // 检查语言本身：函数、递归、数组、循环
    static int square(int v)
    {
        return v * v;
    }

    static int fib(int n)
    {
        if (n < 2)
        {
            return n;
        }
        return fib(n - 1) + fib(n - 2);
    }

    static string digitName(int d)
    {
        if (d == 0) return "zero";
        if (d == 1) return "one";
        if (d == 2) return "two";
        return "many";
    }

    static void Main()
    {
        int a = 17;
        int b = 25;
        int i = 0;
        int sum = 0;

        // ── 1. 字符串 ──
        println_str("=== demo_std (C#) ===");
        println_str("纯字面量一行");

        // ── 2. 标签 + 值 分开写（⚠ 不用 `+`，见文件头）──
        print_str("a=");       println_int(a);
        print_str("b=");       println_int(b);
        print_str("a+b=");     println_int(a + b);
        print_str("a-b=");     println_int(a - b);
        print_str("a*b=");     println_int(a * b);
        print_str("a/b=");     println_int(a / b);
        print_str("a%b=");     println_int(a % b);
        print_str("负数：");    println_int(0 - a);

        // ── 3. 进制 ──
        print_str("十进制=");   println_int(255);
        print_str("十六进制="); println_int(255);

        // ── 4. 函数调用（含递归）──
        print_str("square(9)="); println_int(square(9));
        print_str("fib(10)=");   println_int(fib(10));
        print_str("digitName: ");
        print_str(digitName(0)); print_str(" ");
        print_str(digitName(1)); print_str(" ");
        print_str(digitName(2)); print_str(" ");
        println_str(digitName(9));

        // ── 5. 数组 + 循环（纯语言内部，不跨 FFI）──
        int[] v = new int[6];
        v[0] = 1; v[1] = 4; v[2] = 9; v[3] = 16; v[4] = 25; v[5] = 36;
        while (i < 6)
        {
            sum = sum + v[i];
            i = i + 1;
        }
        print_str("1^2+...+6^2 = "); println_int(sum);

        // ── 6. 九九表的一小段 ──
        i = 1;
        while (i <= 5)
        {
            print_int(i);
            print_str(" x 7 = ");
            println_int(i * 7);
            i = i + 1;
        }

        println_str("=== 完成 ===");
    }
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (C#) ===
// 纯字面量一行
// a=17
// b=25
// a+b=42
// a-b=-8
// a*b=425
// a/b=0
// a%b=17
// 负数：-17
// 十进制=255
// 十六进制=255
// square(9)=81
// fib(10)=55
// digitName: zero one two many
// 1^2+...+6^2 = 91
// 1 x 7 = 7
// 2 x 7 = 14
// 3 x 7 = 21
// 4 x 7 = 28
// 5 x 7 = 35
// === 完成 ===
// ────────────────────────────────────────────────────────────────
