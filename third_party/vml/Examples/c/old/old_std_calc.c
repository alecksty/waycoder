/* old_std_calc.c —— 老式四则计算器（命令行的，纯 stdio）
 * old_std_calc.c -- an old-style four-function calculator (command line, plain stdio)
 *
 * 类别：std（命令行输入输出）
 * Category: std (command-line input and output)
 * 兼容面：`scanf` 反复读、`while` + `switch` 分派、`double` 运算、
 * Compatibility: repeated `scanf` reads, `while` + `switch` dispatch, `double` arithmetic,
 *         用 `scanf` 返回值判 EOF（老程序的经典写法，**不**看 feof）
 *         and using the `scanf` return value to detect EOF (the classic old-program idiom, **not** looking at feof)
 * 出处：自写，仿 80/90 年代随书附赠的「计算器练习」风格。
 * Origin: self-written, in the style of the "calculator exercise" shipped with books in the 1980s and 1990s.
 */
#include <stdio.h>
#include <waycoder_ui.h>

int main(void)
{
    double a, b;
    char   op;
    int    lang = ui_get_language();   /* 开局查一次，之后按它分支 */
                                       /* Query it once at startup, then branch on it */

    if (lang == 0) printf("简单计算器（老式）\n");
    else           printf("Simple Calculator (classic)\n");
    if (lang == 0) printf("输入形如  3 + 4 ，每行一个算式；Ctrl+D / Ctrl+Z 结束\n");
    else           printf("Type like  3 + 4 , one per line; Ctrl+D / Ctrl+Z to quit\n");

    /* ⚠ 判据是 `scanf` 的**返回值**：老程序都这么写，而不用 feof()。
     * ⚠ The criterion is the **return value** of `scanf`: old programs all wrote it this way rather than using feof().
     *   这条在 VML 上是真兼容面 —— feof() 至今没有实现。
     *   This is a genuine compatibility surface on VML -- feof() still has no implementation.
     */
    while (scanf("%lf %c %lf", &a, &op, &b) == 3) {
        double r = 0;
        int    ok = 1;

        switch (op) {
        case '+': r = a + b; break;
        case '-': r = a - b; break;
        case '*': r = a * b; break;
        case '/':
            if (b == 0) { ok = 0; }
            else        { r = a / b; }
            break;
        default: ok = 0; break;
        }

        if (ok) printf("= %.6g\n", r);
        else if (lang == 0) printf("算不了：除数为 0 或运算符不认识（%c）\n", op);
        else                printf("Cannot compute: divide by 0 or bad operator (%c)\n", op);
    }

    if (lang == 0) printf("再见。\n");
    else           printf("Bye.\n");
    return 0;
}
