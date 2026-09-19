/* p7_param_addr —— **取形参地址**判据（`&形参` 读、写两条路都要对）。
 *
 * 为什么需要它：C 前端里「取地址」有**两份实现** —— 一份在 `GenerateUnaryOp` 的
 * `case "&"` 里手写，一份是 `GenerateAddressOf`。手写那份算的是
 * `stackFrameSize - 变量偏移` 再 `SUB`，而 `stackFrameSize` **从来没被赋过非 0 值**：
 *   · 局部变量偏移是负的（`R12-8`）⇒ 减负数得正数 ⇒ SUB 出 `R12-8`，**碰巧对**；
 *   · 形参偏移是正的（`R12+12`）⇒ 减出负数 ⇒ 那个 `if (offset > 0)` 不成立
 *     ⇒ **一句不加，`&形参` 得到裸 `R12`**。
 * 于是「取局部地址」一直是好的、「取形参地址」一直是坏的，而坏的那半边**不报错**：
 * 读回来是个栈上的垃圾值，写进去写到了别处（实测读 65528、写回完全不生效）。
 *
 * 判据：读回形参本值、且写回能被本函数看见 → 打印 ABI-OK 正常退出；
 *      否则打印 ABI-FAIL 并挂死（挂死会被 CLI 的 --timeout 截成 `VM execution cancelled`）。
 */
int g_out;                       /* 顺带确认取全局地址仍不受影响 */

void write_through_param(int v) {
    int* p = &v;                 /* ← 取**形参**地址 */
    if (*p != v) {               /* 读：原先读到的是 R12 处保存的旧寄存器值 */
        print_str("ABI-FAIL 取形参地址读回不对\n");
        for (;;) { }
    }
    *p = 12345;                  /* 写：原先写到了别处，v 一个字都不动 */
    if (v != 12345) {
        print_str("ABI-FAIL 通过形参地址写回不生效\n");
        for (;;) { }
    }
}

void write_through_local(void) {
    int a;
    int* q = &a;                 /* 取局部地址这条路原本就是对的，一起钉住别改坏 */
    a = 7;
    if (*q != 7) {
        print_str("ABI-FAIL 取局部地址读回不对\n");
        for (;;) { }
    }
    *q = 8;
    if (a != 8) {
        print_str("ABI-FAIL 通过局部地址写回不生效\n");
        for (;;) { }
    }
}

int main(void) {
    write_through_param(42);
    write_through_local();
    *(&g_out) = 9;
    if (g_out != 9) {
        print_str("ABI-FAIL 取全局地址写回不生效\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
