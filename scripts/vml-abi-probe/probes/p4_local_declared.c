/* p4_local_declared —— p3 的**对照版**：同样是「调库之后局部必须完好」，
 * 但把库函数显式声明成 `__stdcall`（= 库里那份 `Lib/c/shared_bindings.h` 的声明）。
 *
 * 它证明两件事：
 *   ① 漂移**不是**「局部变量本身有问题」，而是调用点少加/多加了一次清栈；
 *   ② 现状的可用性完全建立在「每个调用方都记得声明 `__stdcall`」上 ——
 *      而对 22 种语言前端、以及任何没读到那份头的程序来说，这个前提不成立。
 *
 * 改造目标：p3 与 p4 **同样通过**（约定统一之后，声明与否不再影响栈平衡）。
 *
 * 基线（改动前）：p4 通过、p3 失败。改造后两者都必须通过。
 */
__stdcall void println_int(int val);
__stdcall void print_str(const char* s);

int main(void) {
    int r;
    int v;

    r = 0;
    v = 12345;

    println_int(0);
    println_int(0);

    if (r != 0) {
        print_str("ABI-FAIL 局部 r 被踩\n");
        for (;;) { }
    }
    if (v != 12345) {
        print_str("ABI-FAIL 局部 v 被踩\n");
        for (;;) { }
    }
    print_str("ABI-OK\n");
    return 0;
}
