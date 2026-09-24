// demo_std.java —— **第 1 层：标准输入输出**（Java 的 `System.out`）
//
// 这一层是每门语言自己的"往标准输出写文本"。
//
// ## ⚠ 写之前先量过：Java 前端的 `+` **字符串拼接不可用**
//
// 本文件是先跑探针再写的。实测（2026-09-24，`vmlcli Examples/java/demo_std.java`）：
//
//     System.out.println("num=" + n + " str=" + s);   // n=7, s="hello"
//     实际输出：1063
//               str=hello
//
// 拼出来的东西不是那句话（`"num=" + n` 那一段落成了一个数）。而**单实参**的调用
// 全部正常：
//
//     System.out.println("纯字面量");      ✔
//     System.out.println(42);              ✔
//     System.out.println(someStringVar);   ✔
//
// 所以本 demo 一律 **`print(标签)` + `println(值)` 两段写**，不用 `+` 拼。
// 这不是风格选择，是绕开上面那条。
//
// ## ⚠ 字符串字面量里的 `\u001b` 也不认
//
// `"\u001b[31m"` 会**原样打出** `\u001b[31m`（六个字符），不是 ESC。
// 要发 ESC 只能 `putchar(27)`（见 `demo_tty.java`）。
//
// ## 判据
//
//     vmlcli Examples/java/demo_std.java
//
// 期望 stdout 逐字节等于文件末尾那段「期望输出」。

public class DemoStd {

    // ⚠ Java 调库函数**必须声明成 `static native`** —— `asm()` 已从 Java 前端移除
    //   （`CodeGenerator.Expressions.cs` 注明「仅限 C/ObjC/C++」）。不声明就发
    //   `CALL method_xxx`，链接期找不到标签。
    //
    //   这三个标签由 `Lib/shared/io.vml` 提供，而 `io.vml` 经 `builtins.vml`
    //   （`vmltool.config.xml` 的 `DefaultLibs`）进每一门语言的链接。
    static native void println_str(String s);
    static native void print_str(String s);
    static native void println_int(int n);

    // 自由函数：证明语言本身是通的（算术、控制流、数组、递归）
    static int square(int v) {
        return v * v;
    }

    static int fib(int n) {
        if (n < 2) {
            return n;
        }
        return fib(n - 1) + fib(n - 2);
    }

    static String digitName(int d) {
        if (d == 0) { return "zero"; }
        if (d == 1) { return "one"; }
        if (d == 2) { return "two"; }
        return "many";
    }

    public static void main(String[] args) {
        int a = 17;
        int b = 25;
        int i;
        int sum = 0;

        // ── 1. 字符串 ──
        System.out.println("=== demo_std (Java) ===");
        System.out.println("纯字面量一行");
        // ⚠ 这一行**刻意不写制表 / 反斜杠 / 引号转义**：Java 前端的转义序列里只有换行
        //   （反斜杠 + n）是好的；制表（反斜杠 + t）会打出「一个反斜杠 + 一个制表符」、
        //   两个反斜杠会原样打出两个、引号转义会原样打出反斜杠加引号。
        //   三条都是探针实测出来的，不是推测。
        System.out.println("转义：只有换行符是好的（见本行上面的注释）");

        // ── 2. 标签 + 值 分开写（⚠ 不用 `+`，见文件头）──
        System.out.print("a=");      System.out.println(a);
        System.out.print("b=");      System.out.println(b);
        System.out.print("a+b=");    System.out.println(a + b);
        System.out.print("a-b=");    System.out.println(a - b);
        System.out.print("a*b=");    System.out.println(a * b);
        System.out.print("a/b=");    System.out.println(a / b);
        System.out.print("a%b=");    System.out.println(a % b);
        System.out.print("负数：");  System.out.println(0 - a);

        // ── 3. 进制与宽度 ──
        System.out.print("十进制=");  System.out.println(255);
        System.out.print("十六进制="); System.out.println(255);

        // ── 4. 函数调用（含递归）──
        System.out.print("square(9)=");  System.out.println(square(9));
        System.out.print("fib(10)=");    System.out.println(fib(10));
        System.out.print("digitName: ");
        System.out.print(digitName(0)); System.out.print(" ");
        System.out.print(digitName(1)); System.out.print(" ");
        System.out.print(digitName(2)); System.out.print(" ");
        System.out.println(digitName(9));

        // ── 5. 数组 + 循环 ──
        int[] v = new int[6];
        v[0] = 1; v[1] = 4; v[2] = 9; v[3] = 16; v[4] = 25; v[5] = 36;
        for (i = 0; i < 6; i = i + 1) {
            sum = sum + v[i];
        }
        System.out.print("1^2+...+6^2 = "); System.out.println(sum);

        // ── 6. 九九表的一小段 ──
        for (i = 1; i <= 5; i = i + 1) {
            System.out.print(i);
            System.out.print(" x 7 = ");
            System.out.println(i * 7);
        }

        System.out.println("=== 完成 ===");
    }
}

// ── 期望输出（逐字节）────────────────────────────────────────────
// === demo_std (Java) ===
// 纯字面量一行
// 转义：只有换行符是好的（见本行上面的注释）
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
