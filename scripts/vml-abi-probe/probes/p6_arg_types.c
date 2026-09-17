/* p6_arg_types —— **类型化实参**的传参判据。
 *
 * 为什么单列一条：参数不是一律 4 字节。前端压栈时会按类型选指令
 * （`EmitPushArg`：`char` → `PUSHB` 1 字节、`short` → `PUSHH` 2 字节、
 * `double`/`long` → 8 字节且**占两个槽**），而被调方是按
 * `[R12+12+偏移]` 读的，它的偏移由**另一端**自己算出来。
 *
 * 「哪些参数占几个槽」这件事只要两端算法不一致，后面所有参数**整体错位**
 * ——而且不会报错，只会静默读到隔壁的值。这正是「同一段代码有时对有时错」的温床。
 *
 * 判据：混合类型逐个报文正确。
 */
int mix(char c, short s, int i, double d, int tail) {
    if (c != 65)  return 1;     /* 'A' */
    if (s != 1000) return 2;
    if (i != 70000) return 3;
    if (d != 2.5) return 4;
    if (tail != 99) return 5;
    return 0;
}

int main(void) {
    int r;
    r = mix(65, 1000, 70000, 2.5, 99);
    if (r != 0) {
        print_str("ABI-FAIL 类型化实参错位，第 ");
        println_int(r);
        print_str(" 个不对\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
