// **函数内 `static` 数组：初始化器与元素读取都坏着**（KNOWN-RED，两处独立缺陷）
//
// 由来：修好二维指针数组（`cases/33`）之后，`sl` 仍然画不出火车。
// 插桩读数把范围收到**函数内 static 数组**这一条上 —— 而老程序里
// "函数内静态查表"（整车图形、字模、菜单、CRC 表）几乎都是这个形状。
//
// ## 缺陷 A：**元素读取丢掉了下标计算**
//
// `static char *stat[2] = {"aa","bb"};` 读 `stat[0]` 得到 **0**（`%s` 打出 `(null)`），
// 而非 static 的 `char *plain[2]` 是好的。数据段**是对的**
// （产物里 `f__stat: .word L_…` 两条），所以毛病在**读**这条路。
// 生成的汇编（`--vml` 可看）：
//
//      move @R0 f__stat         ← 基址取对了（标签）
//      push @R0                 ← 基址压栈
//      …取下标的 6 条…           ← 之后**没有** mul/pop基址/add/取值那一段
//      push @R0                 ← 直接把下标当值推出去了
//
// 对照（非 static，正常）：`mul @R0 #4 / pop @R10 / add @R0 @R10 / move @R0 @0`。
//
// ## 缺陷 B：**维度写成表达式/宏时，初始化器落地时只剩一个 0**
//
//     static char *macro[D51HEIGHT]     = {"aa","bb"};   /* 裸宏：产物 `.word 0` */
//     static char *expr [D51HEIGHT + 1] = {"aa","bb"};   /* 宏+1：产物 `.word 0` */
//     static char *lit  [2]             = {"aa","bb"};   /* 字面量：正常 */
//
// 怀疑是维度折不成常量时声明被登记成 **VLA**，而 VLA 那条路在代码生成里
// 第一句就 `return`（把初始化器整个跳过）—— 待查实。
// `sl` 的 `d51[D51PATTERNS][D51HEIGHT + 1]` 正是这一档。
//
// ⚠ 断言只看**分类值**（1 = 指针像个有效地址），不打 `%s` ——
//   `%s` 撞上 0 指针会把整个用例打崩、输出一片空白，
//   那时"已知红"与"程序挂了"就分不出来了。
// ⚠ 保留这条用例的价值是**钉住症状**：修好那天它会自己变绿。
// STDIN:
// EXPECT: PLAIN=[aa]|STATIC=1|MACRO=1|EXPR=1
// KNOWN-RED
#include <stdio.h>
#include "sl.h"

static void probe(void)
{
    char *plain[2] = {"aa", "bb"};                      /* 非 static（对照，正常） */
    static char *stat[2] = {"aa", "bb"};                /* 缺陷 A */
    static char *macro[D51HEIGHT] = {"aa", "bb"};       /* 缺陷 B（裸宏维度） */
    static char *expr[D51HEIGHT + 1] = {"aa", "bb"};    /* 缺陷 B（宏+1 表达式） */

    printf("PLAIN=[%s]\n", plain[0]);
    printf("STATIC=%d\n", (int)stat[0] > 100 ? 1 : 0);
    printf("MACRO=%d\n", (int)macro[0] > 100 ? 1 : 0);
    printf("EXPR=%d\n", (int)expr[0] > 100 ? 1 : 0);
}

int main()
{
    probe();
    return 0;
}
