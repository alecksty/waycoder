// `signal.h` —— **同名函数被另一个模块抢走**，返回值与头文件的承诺相反。
//
// ## KNOWN-RED：`signal()` 返回 `-102`，而头文件明写"一律返回成功"
//
// `Lib/c/signal.h` 的承诺是白纸黑字的：
//
// > **装处理函数一律返回成功**（返回 `SIG_ERR` 会让不少程序当成致命错误
// > 直接退出，那比"收不到信号"糟得多）
//
// 而 `util.c` 的实现也是 `return 0`。**但 `signal` 这个名字在本库里有两份实现**：
//
//     Lib/shared/src/util.c :  int signal(int sig, void* handler) { return 0; }        ← 头文件要的是这份
//     Lib/shared/src/os.c   :  int signal(int signum, void* handler)
//                                  { return asm("SYSCALL #350"); }                     ← 实际链上的是这份
//
// C 前端把 `signal` 路由到了 **`os`** 模块（生成代码里写死 `call lib_os_signal`），
// 而 `SYSCALL #350` 在**桌面宿主**里没有实现 ⇒ 运行时返回
// **`-102` = `SYSCALL_PERMISSION_DENIED`**（`VMLRuntime/ErrorCodes.cs:83`）。
//
// ⚠ 这就是 `signal.h` 那句话**预言过的那个故障**：老程序里极常见的写法是
//
//     if (signal(SIGINT, on_int) == SIG_ERR) { perror("signal"); exit(1); }
//
//   ⇒ 拿到 `-102`（≠ `SIG_ERR` = -1，所以**不会**误判成"失败"），看起来没事；
//     但另一批程序写的是 **`if (signal(...) != 0) { 致命错误 }`** ⇒ 直接退出。
//     **返回码既不是 0 也不是 SIG_ERR 的第三种值**是最难缠的形态：
//     一半程序静默通过、一半程序莫名退出，而源码上完全看不出差别。
//
// ## 已经查到哪一步 / 下一步从哪接
//
//   · 已确认**不是** `util.c` 那份实现的错（它就是 `return 0`）
//   · 已确认**不是**头文件声明的错（`Lib/c/signal.h` 与 `util.c` 的签名、
//     语义都一致）
//   · 断点在**前端/链接期的模块路由**：`signal` 这个名字同时存在于 `os` 与 `util`
//     两个模块，`CompilerHelper.cs` 的「函数名→模块」表里没有 `signal` 这条
//     （实测 `grep '"signal"'` 只命中 `moving_avg`/`ema_`/`kalman_` 那一组
//     **信号处理**库的映射），于是走了兜底搜索 —— 而 `os` 排在 `util` 前面。
//   · 下一步，二选一：
//     ① 给 `signal`/`raise` 补一条显式映射（指向 `util`）；或
//     ② 把 `os.c` 里那个 `signal` 改名（它本来就是**另一套东西** ——
//        `os.c` 是"操作系统能力"层，而 `signal.h` 要的是"POSIX 空实现"）
//     两条都要**顺带处理 `raise`**（同一族，同为 `os.c`/`util.c` 各一份）。
//
// ⚠ 顺带记一条**没验**的：手机宿主（`WayCoder.Maui`）是否实现了 `#350` 未查 ——
//   若实现了，这条在手机上是绿的、在桌面是红的，「桌面验过」不能直接推手机。
#include <stdio.h>
#include <signal.h>

int handled;

int on_int(int sig)
{
    handled = 1;
    return 0;
}

int main()
{
    int r1;
    int r2;

    /* ① 装处理函数：头文件承诺"返回成功" ⇒ 判据是 **== 0** */
    r1 = signal(SIGINT, on_int);
    printf("\nA=%d", r1);

    /* ② 换一个信号、换一个 handler：同样必须是 0（不是"第一次特例"） */
    r2 = signal(SIGWINCH, on_int);
    printf("\nB=%d", r2);

    /* ③ `SIG_IGN` / `SIG_DFL` 这两种特殊 handler 也要收（老程序用它屏蔽信号） */
    printf("\nC=%d", signal(SIGPIPE, SIG_IGN));

    /* ④ `raise` 同族：本平台没有信号，收下返回 0（处理函数**不该**被调用） */
    handled = 0;
    printf("\nD=%d", raise(SIGINT));
    printf("\nE=%d", handled);

    /* ⑤ 信号号必须与 Linux/x86 逐字对齐（老程序偶有硬编码 `== 20` 的写法） */
    printf("\nF=%d,%d,%d", SIGINT, SIGWINCH, SIGTSTP);

    return 0;
}
// KNOWN-RED
// EXPECT: A=0|B=0|C=0|D=0|E=0|F=2,28,20
