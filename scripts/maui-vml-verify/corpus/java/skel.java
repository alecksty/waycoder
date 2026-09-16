// skel.java —— VML 骨架程序（Java）。
//
// 骨架形状抄自 Examples/java/file_io.java（class + public static void main(String[] a)），
// 但那个示例里的 asm(...) 现在**已经不成立**：JavaCompiler/CodeGenerator.Expressions.cs:447
// 注明「asm() 已移除 — 仅限 C/ObjC/C++」，照抄会编成 CALL method_asm。
// 外部库函数只有一条路能用**裸标签** CALL：把它声明成 native（:509-513 走 nativeMethods），
// 否则发的是 CALL method_ui_rect，链接期找不到标签。
//
// ⚠ 已知缺陷：Java 调用方把第 1 个实参放 R0、其余从右到左压栈（:449-459），
//   而库包装是从 [R12+12] 开始一溜读栈的 C 约定 ⇒ 8 参的 ui_rect 参数会整体错位一格。
//   这里照自然写法写（不做「补一个占位参数」之类的规避），参数错位就是要测的东西。
// 打印：System.out.print → print_str（无换行）；System.out.println(整数) → println_int（含换行），
// 两者都按静态类型选标签（:487-499）。
// 颜色写负数十进制（-65536 = 0xFFFF0000），避开十六进制字面量在词法层的解读差异。
class Skel {

    static native void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);

    static native void ui_present();

    static int plus1(int x) {
        return x + 1;
    }

    public static void main(String[] args) {
        int[] a = {1, 2, 3, 4};
        int s = 0;
        for (int i = 0; i < 4; i++) {
            a[i] = plus1(a[i]);
            s = s + a[i];
        }
        ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
        ui_present();
        System.out.print("SKEL-SUM=");
        System.out.println(s);
    }
}
