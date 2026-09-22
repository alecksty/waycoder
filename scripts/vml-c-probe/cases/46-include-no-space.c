// **`#include<x.h>` 写成没有空格 —— 整行被静默丢掉**（2026-09-22 修）。
//
// ## 症状极具误导性：报错指向**用宏的那一行**，而问题在 `#include`
//
//     #include<x.h>          ← 这一行被整个丢掉（不报错、不警告）
//     int main(){ int gd = DETECT; }   ← 于是 DETECT 未声明
//
// 报的是「未声明的变量 'DETECT'」，位置指着 `main` 里那一行。
// **没有任何线索指向 include** —— 会去查 DETECT 怎么没定义，而不是回头怀疑那个 `#include`。
//
// ## 根因：指令名不是按"标识符"扫的，是按**空白**切的
//
// `Preprocessor.ProcessDirective` 把 `#` 后面按空格/tab 切成两段：
//
//     "#include<x.h>".Substring(1).Trim().Split(new[]{' ','\t'}, 2)
//       ⇒ ["include<x.h>"]        ← 整行一段！
//     dir = "include<x.h>"        ⇒ 匹配不上任何 case ⇒ 整行丢掉
//
// ⚠ 同一族的 `#if(x)`、`#define(x,y)` 一起中招。
//
// ## 为什么现在才暴露
//
// 实测：**6 个经典 BGI 程序全都写的是 `#include<graphics.h>`**（老代码里很常见）。
// 而 `#include<stdio.h>` 无空格看不出问题 —— stdio 本来就被自动提供，
// 丢一行 include 也照样能编，**把 bug 盖住了**。这一条判据因此不能拿 stdio 测。
//
// ## 两处实现都要修
//
// 这 22 门前端里有**两条**预处理器实现（`CompilerBase/Preprocessor.cs` 与
// `CCompiler/Preprocessor.cs`），判据都是同一句。本用例压的是 C 这一条；
// 另一条由别的语言覆盖（改一处就要改另一处 —— 本仓头号坑）。
#include<graphics.h>

int main()
{
    // 走到这里就说明 graphics.h 真的进来了（DETECT 是它定义的）
    int gd = DETECT;
    int gm = VGA;

    print_str("\nGD=");
    print_int(gd);
    print_str("|GM=");
    print_int(gm);
    print_str("|CIRCLE=3|LINE=2");
    print_str("\n");
    return 0;
}
// EXPECT: GD=0|GM=9|CIRCLE=3|LINE=2
