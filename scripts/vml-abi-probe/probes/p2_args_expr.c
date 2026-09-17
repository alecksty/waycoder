/* p2_args_expr —— 实参是**带运算的表达式**时，位置仍然正确。
 *
 * 为什么需要它：这是本仓真实踩过的坑。C 前端曾经「边求值边 `MOVE Ri, R0`」，
 * 而下一个实参的求值会把 R0/R1 当临时寄存器用（表达式生成器用 R0/R1 做
 * push/pop 暂存）⇒ 已经装进 Ri 的参数被冲掉，且丢的是**先求值的那些**
 * （右到左 ⇒ 编号大的那个），表现得像「后面的参数拿到前面参数的值」。
 *
 * 实测基线（历史）：`probe(p + 3*k, q + 3*k, 3*k)` 传出去是
 *   R0=101  R1=**101**（应为 113）  R2=72
 * ——五子棋的星位 `ui_circle(pad + 3*cell, padY + 3*cell, …)` 就是这么画歪的。
 *
 * 判据：三个形参分别等于 101 / 113 / 0。
 */
int echo3(int a, int b, int c) {
    if (a != 101) return 1;
    if (b != 113) return 2;
    if (c != 0)   return 3;
    return 0;
}

int main(void) {
    int p; int q; int k; int r;
    p = 101; q = 113; k = 0;
    r = echo3(p + 3*k, q + 3*k, 3*k);
    if (r != 0) {
        print_str("ABI-FAIL 表达式实参错位，第 ");
        println_int(r);
        print_str(" 个不对\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
