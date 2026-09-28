/* demo_std.c —— **第 1 层：标准输入输出**（C 的 `stdio.h`）
 * demo_std.c -- **Layer 1: standard input/output** (C's `stdio.h`)
 *
 * 这一层就是每门语言自己的 `printf`/`puts`：往标准输出写文本。
 * This layer is each language's own `printf`/`puts`: writing text to standard output.
 * 它是最基础的一层，也是**唯一有逐字节确定性判据**的一层 —— 下面每一行输出
 * It is the most basic layer and the **only one with a byte-exact determinism check** -- every output line below
 * 都是可预期的字符串，跑两遍完全一样（不读输入、不用随机数、不看时间）。
 * is a predictable string, identical across runs (no input read, no randomness, no clock).
 *
 * ## 判据
 * ## The check
 *
 *     vmlcli Examples/c/demo_std.c
 *
 * 期望 stdout 逐字节等于本文件末尾「期望输出」注释里那一段。
 * Expect stdout to be byte-for-byte equal to the "expected output" comment at the end of this file.
 *
 * ## ⚠ 本前端实测的两条格式化缺口（写 demo 时避开，另见报告）
 * ## WARNING: two measured formatting gaps in this frontend (avoided while writing the demo; see the report too)
 *
 *   · `%f` / `%.3f` 一律打出 `0.000000` —— printf 的浮点转换这条路是断的
 *   · `%f` / `%.3f` always print `0.000000` -- printf's floating-point conversion path is broken
 *     （整数、字符串、宽度、进制全正常）。所以本 demo **不演示浮点**。
 *     (integers, strings, width and radix are all fine). So this demo **does not demonstrate floats**.
 *   · `%ld` 打出 `0` —— `long` 长度修饰符同样没接上。同理避开。
 *   · `%ld` prints `0` -- the `long` length modifier is not hooked up either. Avoided for the same reason.
 *   两条都能编过、不报错，只是**结果不对**，所以这里按"只用确定能对的那些"来写。
 *   Both compile without errors and only give the **wrong result**, so this demo sticks to "only what is known to be right".
 *
 * ⚠ 桌面脚手架里 `printf` 曾经崩过（`MOVEB @2, R0`，地址是垃圾值）；现已正常，
 * WARNING: `printf` used to crash in the desktop scaffold (`MOVEB @2, R0`, a garbage address); it is fine now,
 *   但 `puts`/`printf` 都用得上时，本 demo 两种都用了，正好把两条路都压住。
 *   but since both `puts`/`printf` are useful, this demo uses both and exercises both paths.
 */

#include <stdio.h>

int main(void)
{
    int a = 17;
    int b = 25;
    int i;
    int sum = 0;

    /* ── 1. 字符串与换行 ──
     * -- 1. Strings and newlines -- */
    puts("=== demo_std (C) ===");
    printf("纯字符串一行\n");
    printf("转义：制表\t反斜杠\\引号\"\n");

    /* ── 2. 整数与算术 ──
     * -- 2. Integers and arithmetic -- */
    printf("a=%d b=%d\n", a, b);
    printf("a+b=%d a-b=%d a*b=%d\n", a + b, a - b, a * b);
    printf("a/b=%d a%%b=%d\n", a / b, a % b);
    printf("负数：%d %d\n", 0 - a, 0 - (a * b));

    /* ── 3. 进制与宽度（老程序靠它排对齐的表格）──
     * -- 3. Radix and width (how old programs align tables) -- */
    printf("十进制=%d 十六进制=%x 八进制=%o\n", 255, 255, 255);
    printf("宽度：[%5d][%-5d][%05d]\n", 42, 42, 42);
    printf("字符=%c 百分号=%%\n", 'A');

    /* ── 4. 循环算一个结果（证明这层和语言本身是通的）──
     * -- 4. Compute a result in a loop (proof that this layer and the language itself are connected) -- */
    for (i = 1; i <= 10; i++) {
        sum = sum + i * i;
    }
    printf("1^2+...+10^2 = %d\n", sum);

    /* ── 5. 九九表的一小段（多行、要缩进对齐）──
     * -- 5. A small slice of the multiplication table (several lines, alignment matters) -- */
    for (i = 1; i <= 5; i++) {
        printf("%d x 7 = %2d\n", i, i * 7);
    }

    puts("=== 完成 ===");
    return 0;
}

/* ── 期望输出（逐字节）────────────────────────────────────────────
 * -- Expected output (byte-for-byte) --
=== demo_std (C) ===
纯字符串一行
A plain string line
转义：制表	反斜杠\引号"
escapes: tab	backslash\quote"
a=17 b=25
a+b=42 a-b=-8 a*b=425
a/b=0 a%b=17
负数：-17 -425
negatives: -17 -425
十进制=255 十六进制=ff 八进制=377
decimal=255 hex=ff octal=377
宽度：[   42][42   ][00042]
width: [   42][42   ][00042]
字符=A 百分号=%
char=A percent=%
1^2+...+10^2 = 385
1 x 7 =  7
2 x 7 = 14
3 x 7 = 21
4 x 7 = 28
5 x 7 = 35
=== 完成 ===
=== done ===
──────────────────────────────────────────────────────────────── */
