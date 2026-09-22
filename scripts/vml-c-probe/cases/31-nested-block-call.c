// **裸复合语句里的函数调用会被可达性分析漏掉** —— 已修（v0.96.356）。
//
// ## 症状极具误导性：报错指向**调用**，而定义在原地好好的
//
//     static void helper(char* b) { b[0] = 'x'; }
//     int main() { { char t[4]; helper(t); } return 0; }
//
//     <input>:1:13: warning: 定义了但从未使用的函数 'helper' [CodeGen_UnusedFunction]
//     <input>:5: error: 未定义的函数 'helper'（引用 1 次）
//
// 两条诊断**自相矛盾**（"从未使用" vs "引用了 1 次"），而源码上看不出任何问题。
//
// ## 根因：遍历器少了一支
//
// `CodeGenerator.Functions.cs` 的 `FindUsedFunctions` 从 `main` 出发做可达性分析，
// 由 `FindUsedFunctionsInStatement` 逐种语句分发 —— 它处理了
// if / while / do / for / switch / return / 表达式语句 / 变量声明 / 标签 / 内联汇编，
// **唯独没有"裸复合语句"（`Block`）那一支**。于是块里的一切都被跳过。
//
// 因果是**级联**的，不是"少收集一个函数"那么简单：
//
//     块里的调用没被看到 ⇒ 那个函数被判「定义了但从未使用」
//       ⇒ **从 AST 里剔除** ⇒ 调用点随后解析不到 ⇒ 报「未定义的函数」
//
// ## 为什么影响面比看起来大
//
// "在块里声明临时变量再算"是最普通的写法（本例就是）。它的触发条件是
// **块里同时有变量声明和调用** —— 缺一个都不触发，所以小样本上很容易碰不到。
//
// ⚠ 本用例刻意**只用一层块**：越多层越容易触发，但一层就够说明问题了，
//   而"刚好一层"才是最小复现（两层都过不了的话，看不出到底修没修对一层）。
#include <stdio.h>

static int helper(int x)
{
    return x + 1;
}

/* 另一条路径：块在 if 里面 —— 这条**本来就好的**（`IfStatement` 那一支会递归），
   放在这里当对照，证明修复没有把原本正常的路弄坏。 */
static int viaIf(int x)
{
    int r = 0;
    if (x > 0)
    {
        int t;
        t = x * 2;
        r = helper(t);
    }
    return r;
}

int main()
{
    int r;

    {
        int t;
        t = 41;
        r = helper(t);          /* ① 裸块里的调用 —— 修复前这一支整个看不到 */
    }
    printf("\nA=%d", r);

    printf("\nB=%d", viaIf(10));   /* ② 对照：if 块里的调用（本来就该对） */

    printf("\n");
    return 0;
}
// EXPECT: A=42|B=21
