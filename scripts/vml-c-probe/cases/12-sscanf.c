// `sscanf` 写不回结果 —— **本次新发现**（此前全仓对 `sscanf` **零覆盖**）
//
// 发现经过：给「变参改走 `va_list`」写判据时顺手加了一条 `sscanf("12 hello", "%d %s", &v, w)`，
// 结果是 `F=0|` —— **两个变量一个都没被写**（`v` 还是初值 0、`w` 是空串）。
//
// 与本次改动无关的判据：同一份文件里 `printf`/`sprintf`/`snprintf` 五条全对，
// 而它们与 `sscanf` **共用同一条"数转换符"的判据**（`format_arg_count`）——
// 数错的话前者也会跟着错。所以问题在 `vsscanf` 的**写回**那一段，不在变参表。
//
// 对老程序的影响面：`sscanf` 是"解析一行配置/一行输入"的标准写法，用得极广
// （读 INI、拆 `key=value`、解析成绩单/存档）。**静默不写**比报错更难查 ——
// 调用方只会看到"变量还是老值"。
#include <stdio.h>

int main()
{
    int v;
    char w[16];
    int n;

    v = 0;
    w[0] = 0;
    n = sscanf("12 hello", "%d %s", &v, w);
    printf("F=%d|%s|n=%d\n", v, w, n);
    return 0;
}
// EXPECT: F=12|hello|n=2
//
// ## 修了什么（v0.96.385）
//
// 根因是**同一函数两份实现**，而且赢的那份是**桩**：
//
//     Lib/shared/src/util.c  : int sscanf(s, fmt, ptr) { return 0; }   ← 定参桩
//     Lib/shared/src/scanf.c : int sscanf(str, format, ...) { … vsscanf … }  ← 真实现
//
// 两份都被链接时**重名标签谁赢取决于链接顺序**（汇编器对重名静默容忍），
// 实测生效的是那个桩 ⇒ 一个变量都不写回、返回 0。
// 处置与本文件下面 `util.c` 里的 `atoi`（曾有三份）一样：**相同函数只留一份**，
// 删掉桩、唯一实现留在 `scanf.c`。
//
// ⚠ 那个桩未必是"手滑加的" —— 它长着一副"给变参函数顶一个可导出的定参签名"的样子。
//   但**绑定本来就从 `scanf.c` 导出**（`Lib/c/shared_bindings.h` 写的就是
//   `int sscanf(const char *str, const char *format, ...)`），所以它从一开始就是多余的。
