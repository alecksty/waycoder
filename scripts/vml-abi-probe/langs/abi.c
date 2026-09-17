/* abi.c —— 多参库调用的**实参顺序**探针（C，对照组：应为 8）。
 *
 * 为什么需要它：22 语言骨架全绿**证明不了**实参顺序对 —— 骨架里的库调用都是单参
 * （或参数不参与判据），而「实参反序」只在**两个以上实参**时才露馅。
 *
 * 判据：输出 `ABI=8`。取 `ipow`（Lib/shared/src/math.c，base^exp）是因为它**非交换**：
 * `ipow(2,3)=8`，实参反序则得 `ipow(3,2)=9`。
 */
__stdcall int ipow(int base, int exp);
__stdcall void print_str(char* s);
__stdcall void print_int(int v);
__stdcall void newline(void);

int main(void) {
    int r = ipow(2, 3);
    print_str("ABI=");
    print_int(r);
    newline();
    return 0;
}
