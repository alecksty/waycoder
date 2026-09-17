// nat.c —— 测**这门语言自己的**标准输出函数（与 out.c 的共享库那条路分开）
//
// ⚠ `OUT-INT` 这一行**刻意不写 `42` 字面量**，而是让 42 由「全局数组里的负数常量」
//    加出来。这是本仓踩过的一个**静默编错**：初始化器里的负数一律被编成 0 ——
//    `-3` 不是 `NumberLiteral` 而是 `UnaryOp("-", NumberLiteral(3))`，而把初始化器
//    摊平成数据段的那两条路只认 `NumberLiteral`，其余掉进兜底 `Add(0)`。
//    症状极隐蔽：**正数全对、只有负数错**，所以只看源码矩阵永远看不出来；
//    真身是 `Examples/c/pacman.c` 的 `int DX[4] = {0,1,0,-1}` 被编成 `{0,1,0,0}`，
//    吃豆人因此**只能往右和往下走**（用户看到的是"按键有反应、人不动"）。
//    现在 45 + (-3) 必须等于 42 —— 负数一丢就变 45，这一行立刻红。
//    （修在 `VMLPrepares/CCompiler/ConstFold.cs`。）
#include <stdio.h>

int BASE[2] = { 45, -3 };

int main() {
    printf("OUT-STR=abc\n");
    printf("OUT-INT=%d\n", BASE[0] + BASE[1]);
    printf("OUT-PUN=hello, world\n");
    return 0;
}
