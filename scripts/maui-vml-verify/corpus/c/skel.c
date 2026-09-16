/* skel.c —— VML 骨架程序（C）：函数 / 数组 / 循环 / 一次 ui 调用 / 打印一行。
 *
 * 习惯用法抄自 Examples/c/tetris.c：`#include <waycoder_ui.h>`，直接调 ui_* 包装
 * （它们是 cdecl，与 C 前端「实参从右到左压栈 + 前 4 个装 R0-R3」一致）。
 *
 * 打印**有意不走 stdio**：docs/VML宿主接口.md 记着本环境 printf 的格式化路径会崩、
 * puts 输出会串行/重复，所以用共享库的 print_str / print_int / newline（SYSCALL #1/#6/#4）。
 * 它们声明的 __stdcall 与 Lib/c/shared_bindings.h 里那份一致（被调用方清栈），
 * 声明成默认的 cdecl 会让每条调用多释放 4 字节栈。
 */
#include <waycoder_ui.h>

__stdcall void print_str(char* s);
__stdcall void print_int(int v);
__stdcall void newline(void);

int inc(int x) { return x + 1; }

int main(void) {
    int a[4] = {1, 2, 3, 4};
    int s = 0;
    int i;
    for (i = 0; i < 4; i++) {
        a[i] = inc(a[i]);
        s = s + a[i];
    }
    ui_rect(10, 10, 50, 50, 0xFFFF0000, 1, 0, 0);
    ui_present();
    print_str("SKEL-SUM=");
    print_int(s);
    newline();
    return 0;
}
