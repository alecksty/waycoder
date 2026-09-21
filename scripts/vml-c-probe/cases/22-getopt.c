// `getopt.h` —— 命令行解析（老程序的**入口**几乎人手一个）。
//
// ## 为什么单列一条
//
// `getopt` 是 POSIX 里**唯一一个"半函数半全局状态"**的接口：返回值之外还靠
// `optind` / `optarg` / `optopt` 三个全局变量传递信息。这正好撞上 VML 的两个软肋：
//
//   · 全局变量在 VML 里是**数据段的一个槽**，而 `optind`/`optarg` **没有任何函数形参
//     能带它们**（`optarg` 是个 `char*`，写进去再读出来要走"指针存进全局槽"这条路）
//   · 实现（`Lib/shared/src/util.c`）**不 include 头文件**，靠约定与 `Lib/c/getopt.h`
//     对齐字段/变量名 —— 写歪一个字母不报错、只是行为变成"永远返回 -1"
//
// 所以判据要**同时压在"返回值"和"下次调用看到的状态"上** —— 只看返回值的话，
// `optind` 不推进的版本能骗过前两条（每次都返回同一个字母）。
//
// ## 期望值怎么独立推导
//
// 逐行读 `Lib/shared/src/util.c:394-425` 的状态机：
//   ① `optind>=argc` / `argv[optind][0]!='-'` / 单独的 `"-"` ⇒ **返回 -1**，不动 `optind`
//   ② `optopt = argv[optind][1]`（**只看第二个字符**，不支持 `-abc` 合并）
//   ③ 在 `optstring` 里找 `optopt`；**后面紧跟 `:`** ⇒ 这个选项要带参数
//   ④ 不认识的字母 ⇒ `optind++`、返回 `'?'`
//   ⑤ 要参数 ⇒ `optind++`；再 `optind>=argc` ⇒ 返回 `'?'`；否则 `optarg=argv[optind]`、`optind++`
//   ⑥ 不要参数 ⇒ `optind++`
//
// 用 `argv = {"prog","-a","-b","VAL","tail"}` + `optstring="ab:c"` 手工推：
//   `getopt` → `'a'`, optind 1→2
//   `getopt` → `'b'`, optarg="VAL", optind 2→3→4
//   `getopt` → `-1`（argv[4]="tail" 不是选项），optind 停在 4
//   `getopt` → `-1`（幂等 —— 这条专抓"optind 被改坏"）
// 再换 `argv[1]="-z"` ⇒ `'?'`（不认识）、`optind` 1→2
// 再把 `argc` 收到 3 让 `-b` 后面没有值 ⇒ `'?'`
//
// ## ⚠ `optind` 要**自己先置 1**
//
// POSIX 规定初值 1，而这个实现把它当普通全局变量（数据段初值 0）。
// 用例**显式赋值**，避免把"初值是不是 1"这件与被测语义无关的事混进来。
//
// ## KNOWN-RED：整条链在**第一行**就断了，根因不在 `getopt`
//
// 实测每一问都返回 `-1`、`optind` 停在原地。断点是实现的第一句判断
// `if (argv[optind][0] != '-') return -1;` —— **`char **` 的双下标 `argv[i][j]`
// 在本 C 前端取到的是 4 字节，不是 1 字节**（`"-a\0\0"` 那一整个字永远不等于 `'-'`
// 这个单字节常量）⇒ 恒真 ⇒ 立刻 `return -1`。
//
// 这与 `"-a"` 里有没有值、`optstring` 写没写对**毫无关系** —— 换成
// `argv[1][0]` 那种"常量下标"照样错。最小复现是 `23-charpp-subscript.c`，
// **本条修不修取决于那一条**：那一条绿了，本条应当自己变绿（若没变，才是 `getopt`
// 自己的问题）。所以这里**不去改 `getopt` 的实现**（改了也测不出来）。
//
// ⚠ 因此本用例的期望值写的是**正确的 POSIX 语义**（不是"实得是什么就写什么"）——
//   它现在红着，正是因为实现没有达到它。
#include <stdio.h>
#include <getopt.h>

int main()
{
    char *argv[8];
    int   argc;
    int   c;

    argv[0] = "prog";
    argv[1] = "-a";
    argv[2] = "-b";
    argv[3] = "VAL";
    argv[4] = "tail";
    argc = 5;

    /* ① 无参数选项：返回字母本身，optind 1→2 */
    optind = 1;
    c = getopt(argc, argv, "ab:c");
    printf("\nA=%c,%d", c, optind);

    /* ② 带参数选项：返回字母，optarg 指向**下一个** argv，optind 2→4（跨两位） */
    c = getopt(argc, argv, "ab:c");
    printf("\nB=%c,%s,%d", c, optarg, optind);

    /* ③ 遇到非选项参数就停：返回 -1，optind **不动** */
    c = getopt(argc, argv, "ab:c");
    printf("\nC=%d,%d", c, optind);

    /* ④ 再问一次仍然是 -1（幂等）—— 这条抓"optind 被改坏" */
    c = getopt(argc, argv, "ab:c");
    printf("\nD=%d,%d", c, optind);

    /* ⑤ 不认识的选项 ⇒ `'?'`，optind 照样推进（否则主循环会死转） */
    optind = 1;
    argv[1] = "-z";
    c = getopt(argc, argv, "ab:c");
    printf("\nE=%c,%d", c, optind);

    /* ⑥ 选项要参数、后面却没有了 ⇒ `'?'`（不是崩溃、也不是返回字母） */
    argv[1] = "-a";
    optind = 2;
    argc = 3;                 /* argv[2]="-b" 是最后一个 */
    c = getopt(argc, argv, "ab:c");
    printf("\nF=%c,%d", c, optind);

    /* ⑦ 单独的 `"-"` 不是选项（POSIX：它表示 stdin） */
    argv[1] = "-";
    optind = 1;
    argc = 5;
    c = getopt(argc, argv, "ab:c");
    printf("\nG=%d,%d", c, optind);

    printf("\n");
    return 0;
}
// KNOWN-RED
// EXPECT: A=a,2|B=b,VAL,4|C=-1,4|D=-1,4|E=?,2|F=?,3|G=-1,1
