// skel.m —— ObjC 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
//
// 取材 Examples/objc/file_io.m（`int main()` + asm）与 Examples/objc/parserexpf_demo.m；
// 前端实现 VMLPrepares/ObjCCompiler/。
//
// 相对标准 ObjC 的三处**有意偏离**（每处都在绕一个已证实的后端缺陷）：
//  ① 数组不用 `int a[4] = {1,2,3,4};` —— 该初始化会被丢掉（CodeGenerator.Statements.cs:109-114
//     走 AllocateVmlArray，只清零；Parser.cs:1160 的 `{…}` 字面量只取第一个元素）
//     ⇒ 改成逐元素赋值，否则期望值根本到不了 14。
//  ② 循环更新写 `i = i + 1` 而不是 `i++` —— `++` 只 ADD R0 不写回
//     （CodeGenerator.Expressions.cs:235）⇒ `for (…; …; i++)` 死循环。
//  ③ 颜色写 -65536（= 0xFFFF0000）：词法器十进制专用（Lexer.cs:123），不认 0x。
// 未声明的 ui_rect/ui_present 直接调用会编成裸 `CALL ui_rect`
// （CodeGenerator.Expressions.cs:305-350），能解析到 lib_vmlui_ui_rect。
// ObjC 也支持 asm("CALL …")，但 8 个实参用 asm 逐句手写不现实，故不用。

int inc(int x) { return x + 1; }

int main() {
    int a[4];
    int s;
    int i;
    a[0] = 1; a[1] = 2; a[2] = 3; a[3] = 4;
    s = 0;
    for (i = 0; i < 4; i = i + 1) {
        a[i] = inc(a[i]);
        s = s + a[i];
    }
    printf("SKEL-SUM=%d\n", s);
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
    ui_present();
    return 0;
}
