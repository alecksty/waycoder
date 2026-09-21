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
// ## 已修（v0.96.346 续）—— 整条链断在**两处**，都不在 `getopt` 本身
//
// ① `char **` 的双下标 `argv[i][j]` 取 4 字节（`"-a\0\0"` 整个字永远不等于 `'-'`）
//    ⇒ 第一句判断恒真、立刻 `return -1`。见 `23-charpp-subscript.c`。
// ② 修了 ① 之后仍然不对 —— `optarg`/`optind`/`optopt` **没有任何地方定义**：
//    `util.c` 原先只写三行 `extern`，而"`extern` 声明不占数据段槽位"修好之后
//    它们就悬空了 ⇒ 使用者写进去的 `optind` 与 `getopt` 读到的**不是同一块内存**
//    （实测 `optind = 1` 之后第一次 `getopt` 直接返回 `'b'`、`optind` 读出来是 **98**）。
//    已在 `util.c` 里**真正定义**（`optind` 按 POSIX 初值给 **1**），
//    并把 `Lib/c/unistd.h` 里那几行**定义**（`int optind;`）改成 `extern` ——
//    否则每个 include 了它的使用者都会生成一份槽位、把库里那份遮住，
//    那正是 `stdscr` 当初的形态。
//
// **本条修不修取决于 23**：23 绿了本条才轮到 ② 显形 —— 两处断点叠在同一条链上，
// 所以"改一处没变好"当时并**不能**说明那一处没修对。
//
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
// EXPECT: A=a,2|B=b,VAL,4|C=-1,4|D=-1,4|E=?,2|F=?,3|G=-1,1
