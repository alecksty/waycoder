// **`double` 数组同样是零表**（与 `cases/41` 的 `float` 同族，但**修法不同**）
//
// `float` 那条能只改一处就修好（4 字节槽装位模式），因为数据段的元素槽就是 4 字节。
// `double` 是 **8 字节**，而数据段的 `object[]` 通道**按 4 字节/元素写死**
// （`VmRuntime.LoadProgram` → `AllocateMemory(len*4)` + `ResolveDataElement` 只出 int）
// ⇒ 要修得单独给它一条 8 字节路径：CLR `double[]` + 文本 `.dword` + 装载侧 8 字节铺字节
// + VMB 的 `0x10` 元素 tag 在数组里也要按 8 字节走。那是**另一轮**的事。
//
// ## 为什么留成 KNOWN-RED 而不是当场做
//
// ① 全语料**零处**用到 `double` 数组（`Lib/shared/src/*.c` 与 `Examples/*/*.c` 各扫一遍，
//    只有 `math.c` 一处 `float` 表）；② 它牵动"数据段数组的元素宽度"这条**整体规则**，
//    改完要重跑全部 22 门语言的判据。留判据钉住症状：修好那天它会自己变绿。
//
// KNOWN-RED
// STDIN:
// EXPECT: D0=25|D1=125|D2=225|SZ=24
#include <stdio.h>

static const double dt[3] = {0.25, 1.25, 2.25};

int main()
{
    printf("D0=%d\n", (int)(dt[0] * 100));
    printf("D1=%d\n", (int)(dt[1] * 100));
    printf("D2=%d\n", (int)(dt[2] * 100));
    printf("SZ=%d\n", (int)sizeof(dt));
    return 0;
}
