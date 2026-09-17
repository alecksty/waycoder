/* p5_array_after_lib —— 库调用之后**数组下标仍然算得对**。
 *
 * 为什么单列一条：数组下标的中间量（基址、下标、步长）在 C 前端的产物里是
 * 用 `push` / `pop` 暂存的（见任意一条 `a[i]` 的汇编：`push R0`（基址）
 * … `pop R9` … `add R0 R9`）。SP 一旦漂了，**这些 pop 取回来的就不再是自己压进去的值**，
 * 于是「下标表达式本身没错，读到的却是别的内存」。
 *
 * 这正是 CLAUDE.md 记的那句：漂了之后凡是「用 `pop` 取临时值」的地方都读错 ——
 * 实参槽与**数组下标的中间量**都在此列。
 *
 * 判据：下标访问与直接访问必须给出同样的值。
 */
int main(void) {
    int a[4];
    int i;
    int r;

    r = 0;
    a[0] = 10; a[1] = 20; a[2] = 30; a[3] = 40;

    /* ⚠ 调用次数要够多才踩得到（漂移 +4/次，p3 的头注释记了这条教训）。
       4 次 = 16 字节，够穿过这个只有 4 个元素的帧。 */
    println_int(0); println_int(0); println_int(0); println_int(0);

    i = 2;
    if (a[i] != 30) r = r + 1;      /* 下标访问（中间量走 push/pop） */
    if (a[0] != 10) r = r + 2;      /* 常量下标 */
    if (a[3] != 40) r = r + 4;

    println_int(0); println_int(0); println_int(0); println_int(0);

    if (a[i] != 30) r = r + 8;
    if (a[0] != 10) r = r + 16;

    if (r != 0) {
        print_str("ABI-FAIL 数组读写被踩，掩码 ");
        println_int(r);
        print_str("\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
