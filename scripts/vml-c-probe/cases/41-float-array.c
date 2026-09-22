// **`float` 数组整表读到 0** —— 初始化器的浮点字面量是 `double`，落文本成 `.word 0.0174…`
// （一个十进制），而装载侧 `ResolveDataElement` **不认 `double`** ⇒ 每个元素都 `return 0`。
//
// ## 症状与受害者
//
// `Lib/shared/src/math.c` 的 `static const float sin_table[361]` —— **三角函数表一直是零表**，
// 于是 `sin_deg_d` / `cos_deg_d` 之类全返回 0，而且不报错（"编译链接全绿、算出来是 0"）。
//
// ## 修法：元素存成**位模式的 int**
//
// 4 字节槽正好装一个 `float`，而**读取端本来就用 `MOVEF`** 去解释那 4 字节
// （装载侧 `ResolveDataElement` 对 `float` 也走 `SingleToInt32Bits`）——
// 位模式原样过去就对了，**五层里一层都不用动**（只改 `BuildArrayData` 一处）。
//
// ⚠ 这与 `char`/`short` 那条（`cases/39`）是**两件不同的事**：那条是"元素宽度与下标步长不一致"，
//   这条是"浮点字面量的类型在数据段里丢了"。所以分开压，别混在一个判据里。
// ⚠ `double` 数组**不**在这条里（见 `cases/42`）：元素 8 字节，而数据段的 `object[]` 通道
//   按 4 字节/元素写死，要给 double 单独一条 8 字节路径才谈得上修。
// STDIN:
// EXPECT: F0=50|F1=150|F2=250|SUM=450|T5=625|SZ=12|SZ2=32
#include <stdio.h>

static const float ft[3] = {0.5f, 1.5f, 2.5f};

/* 大一点的表 + 计算出来的下标（照 `sin_table[361]` 的用法：`tab[idx]`，idx 是运行时算的） */
static const float tab[8] = {
    0.0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f
};

int main()
{
    int idx = 4 + 1;                    /* 运行时算出来的下标，别让常量折叠绕过数组访问 */

    printf("F0=%d\n", (int)(ft[0] * 100));
    printf("F1=%d\n", (int)(ft[1] * 100));
    printf("F2=%d\n", (int)(ft[2] * 100));
    printf("SUM=%d\n", (int)((ft[0] + ft[1] + ft[2]) * 100));
    printf("T5=%d\n", (int)(tab[idx] * 1000));
    printf("SZ=%d\n", (int)sizeof(ft));
    printf("SZ2=%d\n", (int)sizeof(tab));
    return 0;
}
