// **第一维省略的二维数组（`T a[][N] = {{…},…}`）只写出第一行**
//
// ## 症状
//
// `const bool number[][15] = {{…} × 10};`（`tty-clock` 的数字点阵）编出来的数据段
// **只有第一行**，其余九行读到的全是**隔壁变量** —— 表盘只画得出一个数字。
// 用 `int` 更直观：`int a[][3] = {{1,2,3},{4,5,6},{7,8,9}}` 的 `a[1][0]` 读到
// **下一个数据项**、`a[2][0]` 读到字符串区。
//
// ## 根因：`ArraySize == null` 同时被当成"还没算"和"某一维未知"
//
// `ParseVariableDecl` 在循环里累乘维度（`if (ArraySize == null) ArraySize = dim; else *= dim`），
// 而 `[]` 那一支**只往 `Dimensions` 加 null、不碰 `ArraySize`** ⇒ `T a[][3]` 是
// "先 `[]`（ArraySize 还是 null）再见 `3`（判成'还没算'）" ⇒ **ArraySize = 3**（
// 而不是整块的元素数），生成器于是按 3 个元素截断。
// 显式写全的 `T a[3][3]` 反而一直是对的 —— 这正是"两种写法只坏一种"的形态。
//
// 修法：总元素数用**局部累加**，`[]`/VLA 置 `sizeUnknown`，循环结束才写回
// （未知就给 null，让生成器从初始化器推）。判据下面把两种写法**都**压住。
//
// ⚠ `char`/`short` 的二维数组**不**放进来：它们的元素步长与数据段不一致
//   （数据段按 4 字节存、下标按 1/2 字节走），是**另一个**独立缺陷，
//   混压会让这条判据红在不相干的原因上（见 `cases/39`）。
//
// ⚠ 顺带钉住一条**本编译器的既定模型**：`bool`/`char` 的数组元素按 **4 字节**算
//   （`sizeof(bool[3][3]) == 36`）—— 数据段与下标算术两边同源，所以自洽；
//   与 C 标准不同，但**不是**本次改动引入的，写在这里免得下次误判成回归。
// STDIN:
// EXPECT: I00=1|I01=2|I02=3|I10=4|I11=5|I12=6|I20=7|I21=8|I22=9|SI=36|B00=1|B11=1|B22=1|B01=0|B12=0|SB=36|FX10=4|FX22=9|SFX=36|T00=a1|T10=b1|T12=b2
#include <stdio.h>
#include <stdbool.h>

/* 第一维省略 */
const int  oi[][3] = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
const bool nb[][3] = {{1, 0, 0}, {0, 1, 0}, {0, 0, 1}};
static char *tbl[][2] = {{"a1", "a2"}, {"b1", "b2"}};

/* 对照：维度写全的形态（这条一直是好的，改动不许把它带坏） */
const int fx[3][3] = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};

int main()
{
    printf("I00=%d\n", oi[0][0]);
    printf("I01=%d\n", oi[0][1]);
    printf("I02=%d\n", oi[0][2]);
    printf("I10=%d\n", oi[1][0]);
    printf("I11=%d\n", oi[1][1]);
    printf("I12=%d\n", oi[1][2]);
    printf("I20=%d\n", oi[2][0]);
    printf("I21=%d\n", oi[2][1]);
    printf("I22=%d\n", oi[2][2]);
    printf("SI=%d\n", (int)sizeof(oi));          /* 9 个 int = 36 */

    printf("B00=%d\n", (int)nb[0][0]);
    printf("B11=%d\n", (int)nb[1][1]);
    printf("B22=%d\n", (int)nb[2][2]);
    printf("B01=%d\n", (int)nb[0][1]);
    printf("B12=%d\n", (int)nb[1][2]);
    printf("SB=%d\n", (int)sizeof(nb));          /* 9 个元素 × 4（本编译器 bool=4 字节） */

    printf("FX10=%d\n", fx[1][0]);
    printf("FX22=%d\n", fx[2][2]);
    printf("SFX=%d\n", (int)sizeof(fx));

    printf("T00=%s\n", tbl[0][0]);
    printf("T10=%s\n", tbl[1][0]);
    printf("T12=%s\n", tbl[1][1]);
    return 0;
}
