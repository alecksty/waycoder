/* skel.cpp —— VML 骨架程序（C++）。
 *
 * ⚠ Examples/cpp/ 里**没有任何能跑的例子**（只有两个 stm32 驱动片段），CppCompiler
 *   在自己仓库里也是 0 个测试文件，所以下面每条写法都只由前端源码背书，没有现成示例背书：
 *   · 花括号初始化不能用来填数组：InitializerListExpr 只编第一个元素（其余静默丢），
 *     所以四个元素逐个赋值；
 *   · 未定义的函数名发**裸标签** CALL（CodeGenerator.Expressions.cs:1285），但实参是
 *     **从左到右**压栈（同文件 :1177），与库包装读 [R12+12] 的约定相反 ⇒ 多参调用参数整体反序。
 *     所以这里的库调用只用**单参数**的 print_str / println_int（它们从 R0 取值，
 *     单参数不受压栈顺序影响）。8 参的 ui_rect 照自然写法写 —— 参数会不会反序正是要测的。
 *
 * ⚠ 2026-09-17 调用约定统一之后**这条缺陷仍在**（CppCompiler 有自己的调用生成，
 *   与 CCompiler 那份是两套代码）：实测 `ipow(2,3)` 在本前端得 **9**（实参反序），
 *   同一份程序在 C 前端得 **8**。所以上面那条「只用单参数」的规避现在还是必需的。
 *   前端侧的统一见 docs/VML调用约定统一.md「其余前端」一节。
 */
int inc(int x) { return x + 1; }

void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
void ui_present(void);
void print_str(char* s);
void println_int(int v);

int main() {
    int a[4];
    a[0] = 1;
    a[1] = 2;
    a[2] = 3;
    a[3] = 4;
    int s = 0;
    for (int i = 0; i < 4; i++) {
        a[i] = inc(a[i]);
        s = s + a[i];
    }
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);   /* -65536 = 0xFFFF0000 */
    ui_present();
    print_str("SKEL-SUM=");
    println_int(s);
    return 0;
}
