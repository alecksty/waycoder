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
// KNOWN-RED: `sscanf` 一个变量都不写回（`v` 仍是初值、`w` 仍是空串）
// EXPECT: F=12|hello|n=2
