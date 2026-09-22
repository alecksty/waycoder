/* old_std_calc.c —— 老式四则计算器（命令行的，纯 stdio）
 *
 * 类别：std（命令行输入输出）
 * 兼容面：`scanf` 反复读、`while` + `switch` 分派、`double` 运算、
 *         用 `scanf` 返回值判 EOF（老程序的经典写法，**不**看 feof）
 * 出处：自写，仿 80/90 年代随书附赠的「计算器练习」风格。
 */
#include <stdio.h>

int main(void)
{
    double a, b;
    char   op;

    printf("简单计算器（老式）\n");
    printf("输入形如  3 + 4 ，每行一个算式；Ctrl+D / Ctrl+Z 结束\n");

    /* ⚠ 判据是 `scanf` 的**返回值**：老程序都这么写，而不用 feof()。
     *   这条在 VML 上是真兼容面 —— feof() 至今没有实现。 */
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
        else    printf("算不了：除数为 0 或运算符不认识（%c）\n", op);
    }

    printf("再见。\n");
    return 0;
}
