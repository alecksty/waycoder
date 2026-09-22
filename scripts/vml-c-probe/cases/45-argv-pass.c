// **命令行参数**：宿主喂进来、两种取法取到**同一份**（v0.96.371）
//
// 由来：参数这条路上两头都断着 —— 运行时从来没人给它喂过（`CommandLineArgs` 恒空），
// 而 22 门语言里**只有 C/C++ 前端认识 `argv`**（其余 20 门一个字都没有）。
// 设计见 `docs/命令行参数.md`：宿主喂一份、程序按两种方式取。
//
// ## 两种取法（本用例**两样都测**，并比对它们是否一致）
//
// · **入口帧**：`main(int argc, char **argv)` —— C 的调用约定，运行时把 argv/argc/返回地址
//   三格搭好（`cases/43` 钉的是"搭没搭"）；
// · **syscall**：`#62 ArgCount` / `#63 ArgGet` —— 给 `int main(void)` 的程序和
//   非 C 语言的绑定用（C 侧的包装是 `<waycoder_ui.h>` 的 `ui_argc()` / `ui_arg(i,buf,cap)`）。
//
// ⚠ 两条路**必须取自同一份**（`_argvStrings`）：分成两处各算一份就是
//   "`main` 拿到的和 `ui_arg` 拿到的不是一回事"这种最难查的分叉。
//
// ## 约定
//
// · `argv[0]` = **程序名**（宿主决定：CLI 给源文件名），所以用户给的第一个参数是下标 1；
// · 宿主一个参数都不给时 `argc` 也是 **1**、`argv[0]` 照样可读（C 保证 `argc >= 1`）；
// · 越界/负数序号 → `ui_arg` 返回 **-1**（**不是** 0、更不是随机地址）；
// · 容量不够就截断（一定补 NUL）。
//
// 参数由 runner 的 `// ARGS:` 行给（照 `// STDIN:` 的写法，按空白切分）。
// STDIN:
// ARGS: -l hello
// EXPECT: SAME=1|A0=1|V1=[-l]|V2=[hello]|OOB=-1|TRUNC=1
#include <stdio.h>
#include <string.h>
#include <waycoder_ui.h>

int main(int argc, char *argv[])
{
    int i, same = 1, oob, trunc;
    char buf[64];
    char small[4];

    /* 两条路取到的是不是同一份 */
    if (ui_argc() != argc) same = 0;
    for (i = 0; i < argc; ++i)
    {
        if (ui_arg(i, buf, 64) < 0) same = 0;
        else if (strcmp(buf, argv[i]) != 0) same = 0;
    }

    oob = ui_arg(argc + 5, buf, 64);        /* 越界必须是 -1 */
    /* 容量 2 ⇒ 只能写 1 字节 + NUL。⚠ 期望值要按**实参的长度**算：
       `argv[1]` 是 `-l`（2 字节），cap 给 4 是**截不到**的（那会返回 2、不是 3）——
       第一版就是这里写错，修的是期望、不是代码。 */
    trunc = ui_arg(1, small, 2);

    printf("SAME=%d\n", same);
    printf("A0=%d\n", argc >= 1 && argv[0][0] != 0 ? 1 : 0);
    printf("V1=[%s]\n", argv[1]);
    printf("V2=[%s]\n", argv[2]);
    printf("OOB=%d\n", oob);
    printf("TRUNC=%d\n", trunc == 1 && small[0] == '-' && small[1] == '\0' ? 1 : 0);

    return 0;
}
