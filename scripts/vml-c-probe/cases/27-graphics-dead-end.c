// `graph` / `graphics` 模块 —— **三层全断**，BGI 图形在老程序这条路上一格都走不通。
//
// ## KNOWN-RED：调用任何 `graph` 模块的函数都在**进入函数的第一条指令**上崩
//
// 实测（见下面 main，编译与链接都通过）：
//
//     A=before            ← 说明 main 跑起来了
//     内存错误(PC=00001262): PUSH @R15 —— 尝试访问非法地址=FFFFFFFC, 大小=4
//     R12(BP)=00000004 R13(SP)=FFFFFFFC
//
// 故障点是**被调函数序言的第一条 `push @R15`**，而此刻 `SP = 0xFFFFFFFC`
// （= 只从 0 减了一次 4）—— 也就是说**被调函数进来时栈指针是坏的**。
//
// ## 已经查到哪一步 / 下一步从哪接
//
// 已排除的：
//   · **不是原型写错**。第一版我把 `gfx_cls(void)` 写成了 `gfx_cls(int)`，
//     确实会崩（参数个数与包装函数对不上，`add @R13 #N` 失衡）—— 改成与
//     `Lib/shared/src/graph.c` 里的声明逐一对应之后，**崩的位置一模一样**。
//   · **不是 `#param` 的位置**。`#param lib("graph")` 放在 `#include` 前/后
//     都一样；不调用任何 `gfx_*` 时光是链接它**不崩**（那一步能打印）。
//   · **不是入口符号冲突**。链接产物里 `main:` 只有一处，模块里没有
//     第二个 `main`/`_start`/init。
//   · **不是栈大小**。有/无 graph 的产物都是 `.stack 0`（自动）。
//
// 下一步（按性价比排）：
//   ① 把生成的 `.vml` 里 `main` 的序言与 `c_gfx_width` 的入口各取 5 条指令，
//      对着 `VMLRuntime` 的单步走一遍，看 `R13` 是在**哪一条**上从"正常"跳成
//      `0xFFFFFFFC` 的（本用例的 `--vml out.vml` 已经把现场固定住了）
//   ② 顺手看 `Lib/c/graph.vml` 的 GenLib 包装函数有没有 `add @R13 #N` 与
//      声明不符的地方（第一版那个坑就是这一类，只是换了个方向）
//
// ## 顺带：**另外两层**也是断的（同一个模块，三个独立的缺口）
//
//   ① **头文件缺失**：`Lib/c/graphics.h` 和 `Lib/c/gfx.h` 在本仓**不存在** ——
//      它们在 `3fb839f9`（手机端 Lib 物理剪枝）里被删掉、并登记进了
//      `third_party/vml/PRUNED.txt`。而同一次剪枝里删掉的 `Lib/c/conio.h`、
//      `Lib/c/curses.h` **后来被补回来了**（现在都在），图形这两个没有。
//      ⇒ 任何 `#include <graphics.h>` 的老程序连编译都过不去（只是 warning），
//        紧接着每一个 BGI 调用都报 `未定义的函数 'InitGraph'`。
//   ② **前端没有映射**：`InitGraph`/`PutPixel`/`GetMaxX` 这些 BGI 名字
//      **不在** `CompilerBase/CompilerHelper.cs` 的「函数名→模块」表里
//      （实测 `grep '"InitGraph"'` 零命中），所以即使手工声明原型也照样
//      "未定义的函数" —— 与头文件在不在**无关**。
//   ③ **实现本身调不动**：`Lib/c/graph.vml` 里那 100+ 个 `c_InitGraph`/`c_Line`/
//      `c_Bar` 标签**确实在**（GenLib 产的全套 BGI 封装），但唯一的入口
//       `gfx_*` 一调就崩（本条压的就是这个）。
//
// ⚠ 本用例**只压第 ③ 层**（唯一能跑到运行期的那条路）—— ①② 层是"编译期就
//   报错"，压不到运行期，用 `// EXPECT` 记账没有意义。
//
// ⚠ 跑起来后 `run.sh` 报的"实得"里会混进**寄存器 dump 行**（`R0=…` 碰巧符合
//   判据行的 `^KEY=` 形状）—— 那不是判据，就是崩溃现场本身。看到它说明
//   **程序崩在 VM 里**，而不是"没输出"。
#include <stdio.h>

#param lib("graph")

/* 原型照 `Lib/shared/src/graph.c` 里那份声明逐一对应（**不是**照 `graphics.h` ——
   那种头在本仓不存在。见文件头 ① 层）。 */
void gfx_cls(void);
void gfx_pset(int x, int y, int color);
int  gfx_point(int x, int y);
int  gfx_width(void);
int  gfx_height(void);

int main()
{
    int w;

    /* 这一行**会**打印出来 —— 它是"崩在模块里、不是崩在 main 里"的判据 */
    printf("\nZ=before");

    gfx_cls();
    gfx_pset(3, 4, 7);
    printf("\nA=%d", gfx_point(3, 4));

    w = gfx_width();
    printf("\nB=%d", w > 0);

    return 0;
}
// KNOWN-RED
// EXPECT: Z=before|A=7|B=1
