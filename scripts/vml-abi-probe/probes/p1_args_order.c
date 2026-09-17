/* p1_args_order —— 参数**位置**判据：6 个互不相同的实参，被调方逐个原样报回。
 *
 * 为什么需要它：VML 里「前 4 参走 R0-R3」的快速路径与「全部压栈」两条路并存，
 * 且调用点还有过「边求值边装寄存器」被后续实参求值冲掉的旧伤（见 C 前端
 * `CodeGenerator.Expressions.Calls.cs` 里那段长注释）。参数个数一旦超过 4，
 * 或者压栈方向不统一，形参就会**错位**——而错位不会报错，只会算出错的数。
 *
 * 判据：6 个形参全对 → 打印 ABI-OK 正常退出；否则打印 ABI-FAIL 并挂死
 *      （挂死会被 CLI 的 --timeout 截成 `VM execution cancelled`，见 run.sh）。
 */
int echo6(int a, int b, int c, int d, int e, int f) {
    if (a != 11) return 1;
    if (b != 22) return 2;
    if (c != 33) return 3;
    if (d != 44) return 4;
    if (e != 55) return 5;
    if (f != 66) return 6;
    return 0;
}

int main(void) {
    int r;
    r = echo6(11, 22, 33, 44, 55, 66);
    if (r != 0) {
        print_str("ABI-FAIL 参数错位，第 ");
        println_int(r);
        print_str(" 个形参不对\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
