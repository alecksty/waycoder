// **结构体数组**：元素步长与数据段尺寸（两处缺陷，都已修）
//
// 由来：`sl` 的 `add_smoke` 里有
//     static struct smokes { int y, x; int ptrn, kind; } S[1000];
// 两个毛病叠在一起，最后把 `Eraser[]` 那串空格字符串当指针用
//（崩在地址 `0x20202020` —— 四个空格）：
//
// ## 缺陷 A：**函数内定义的结构体类型没登记** ⇒ 元素步长成了 4
//
// 局部声明那条路遇到 `struct 标签 { … }` 时**只按 `depth` 把成员体跳过去**，
// 于是这张类型表里查不到它：`S[i]` 的步长回落到 **4**（应 16）⇒
// `S[0].kind` 压在被当作 `S[1].ptrn` 的那 4 字节上，读出来的字段是**别的元素的值**
//（最小复现：`S[1]` 打出 `400 400 400 400`）。
// 修法：真解析成员表（复用 `ParseAnonStructBody`）→ 登记进 `program.Structs`/`Unions`
//（匿名的合成一个标签，并把变量类型串也改成 `struct <标签>`，与匿名成员那条路同一套）。
//
// ## 缺陷 B：**数组的数据段按 4 字节/元素开**
//
// `dataSection[name] = new int[N]`：对 `int` 恰好对，对结构体数组就**只有 1/4 大**——
// `struct smokes S[8]`（元素 16 字节）只开 32 字节，写 `S[7].y` 就落到**紧邻的数据**上。
// 实测最小复现：`for (i=0;i<3;++i) S[i].y = i*10;` 之后第一个 `printf` 打出**制表符**
//（把后面的格式串踩了）。修法：`AllocArrayData` 按真实元素宽度开
//（`char`/`short` **仍保持 4 字节/元素** —— 那是既有行为，`Examples/` 里一批
// `char buf[N]` 靠它偷余量，缩到真实宽度会把"静默容忍"变成"真越界"）。
//
// ⚠ 下面三组断言**缺一不可**：
//   · `GO`（文件域对照）修前就是对的 —— 它证明"是这条路径的问题、不是整体坏了"；
//   · `SI`（函数内、带标签的内联类型）钉缺陷 A；
//   · `MSG`（结构体数组**后面**的字符串还在不在）钉缺陷 B ——
//     尺寸不足时踩的是**邻居**，只有回头看邻居才知道。
// STDIN:
// EXPECT: GO=100|200|300|400|SI=100|200|300|400|AN=5|6|7|8|SZ=77|MSG=[INTACT]
#include <stdio.h>

struct outer { int y, x, ptrn, kind; };     /* 文件域：对照，修前也正常 */
static struct outer G[4];

/* 缺陷 B 的触发形状：**无初始化器**的结构体数组 + 紧邻其后的字符串。
   元素 16 字节 × 64 = 1024 字节，而按 4 字节/元素只开 256 ⇒ `Big[63].kind`
   写在偏移 1020 上，正好落在 `msg` 与它的字符串上。 */
static struct outer Big[64];
static char *msg = "INTACT";

static void f(void)
{
    static struct inner { int y, x, ptrn, kind; } S[4];   /* 缺陷 A：函数内联类型 */
    static struct { int a; int b; } A[3];                 /* 同一路径的匿名体 */

    S[1].y = 100; S[1].x = 200; S[1].ptrn = 300; S[1].kind = 400;
    S[0].y = 1;   S[0].x = 2;   S[0].ptrn = 3;   S[0].kind = 4;   /* 步长错就会踩到 S[1] */
    printf("SI=");
    printf("%d|", S[1].y);
    printf("%d|", S[1].x);
    printf("%d|", S[1].ptrn);
    printf("%d\n", S[1].kind);

    A[0].a = 5; A[0].b = 6; A[1].a = 7; A[1].b = 8;
    printf("AN=");
    printf("%d|", A[0].a);
    printf("%d|", A[0].b);
    printf("%d|", A[1].a);
    printf("%d\n", A[1].b);
}

int main(void)
{
    G[1].y = 100; G[1].x = 200; G[1].ptrn = 300; G[1].kind = 400;
    G[0].y = 1;   G[0].x = 2;   G[0].ptrn = 3;   G[0].kind = 4;
    printf("GO=");
    printf("%d|", G[1].y);
    printf("%d|", G[1].x);
    printf("%d|", G[1].ptrn);
    printf("%d\n", G[1].kind);

    f();

    Big[63].kind = 77;
    printf("SZ=");
    printf("%d|", Big[63].kind);
    printf("MSG=[%s]\n", msg);

    return 0;
}
