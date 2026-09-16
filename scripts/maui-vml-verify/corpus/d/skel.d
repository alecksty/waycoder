// skel.d —— D 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
//
// ⚠ **没有 D 例子可抄**：Examples/ 下没有 d/ 目录，本文件按
//   VMLPrepares/DCompiler/D_LANGUAGE_SPEC.md + DCompiler 源码写，
//   语法可信度**低于其余 10 个骨架** —— 编不过时先怀疑这里，再怀疑产品。
//
// 依据与已知缺陷（跑不过=产品缺陷，不是语料写错）：
//  ① `int inc(int x) { return x + 1; }`，标签 func_inc（CodeGenerator.Statements.cs:68）；
//     顶层语句照常执行（CodeGenerator.cs:55-77），main 不是必须的。
//  ② 数组三处全坏，这里按标准 D 写出来就是要看它怎么坏：
//     · `[1,2,3,4]` 字面量在 Expressions.cs:45 落进 EmitLoadConstant →
//       Convert.ToInt32(List) 抛 InvalidCastException（最可能先炸的就是这里）；
//     · `int[4] a;` 的维度在 Parser.cs:51-57 就被丢掉，GenerateVarDecl 完全不分配；
//     · `a[i] = v` 被 Parser.cs:570-574 压成 `a = v`（下标整个丢弃、静默）。
//     ⇒ D 目前没有可用的数组语法。若只想探 UI 绑定，可把数组那四行删掉再跑。
//  ③ 打印只有 writeln / print / write（按名字内联，Expressions.cs:136-151）：
//     实参之间不加分隔符，writeln 末尾补换行 ⇒ 正好一行。
//  ④ 颜色写 -65536：`0xFFFF0000` 在 Parser.cs:814-818 会 OverflowException
//     （长度 10 不满足 `> 10` 的判定，直接 Convert.ToInt32 溢出）。
//  ⑤ D 没有 extern/asm，ui_rect 会被编成 `CALL func_ui_rect`，靠链接器剥 `func_` 前缀
//     解析到 lib_vmlui_ui_rect（VMLAssembler/LibraryLinker.cs:162-182）。

int inc(int x) { return x + 1; }

void main() {
    int[4] a = [1, 2, 3, 4];
    int s = 0;
    for (int i = 0; i <= 3; i = i + 1) {
        a[i] = inc(a[i]);
        s = s + a[i];
    }
    writeln("SKEL-SUM=", s);
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
    ui_present();
}
