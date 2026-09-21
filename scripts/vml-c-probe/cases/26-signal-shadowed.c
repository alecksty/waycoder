// `signal.h` —— **同名函数被另一个模块抢走**，返回值与头文件的承诺相反。
//
// ## 已修（v0.96.350）：`signal()` 曾返回 `-102`，而头文件明写"一律返回成功"
//
// `Lib/c/signal.h` 的承诺是白纸黑字的：
//
// > **装处理函数一律返回成功**（返回 `SIG_ERR` 会让不少程序当成致命错误
// > 直接退出，那比"收不到信号"糟得多）
//
// 而 `util.c` 的实现也是 `return 0`。**但 `signal` 这个名字在本库里有两份实现**：
//
//     Lib/shared/src/util.c :  int signal(int sig, void* handler) { return 0; }   ← 头文件要的是这份
//     Lib/shared/src/os.c   :  int signal(int signum, void* handler)
//                                  { return asm("SYSCALL #350"); }                ← 实际链上的是这份
//
// `SYSCALL #350` 对**用户程序恒被拒**（`case 350` 先过 `PrivilegeDenied`）⇒ 返回
// **`-102` = `SYSCALL_PERMISSION_DENIED`**。而 `Lib/c/signal.h:26` 写的是
// **`#param lib("util")`** —— 头文件的本意就是 `util.c` 那份。
//
// ⚠ 老程序里极常见的写法是
//
//     if (signal(SIGINT, on_int) == SIG_ERR) { perror("signal"); exit(1); }
//
//   拿到 `-102`（≠ `SIG_ERR` = -1，所以**不会**误判成"失败"）看起来没事；
//   但另一批程序写的是 **`if (signal(...) != 0) { 致命错误 }`** ⇒ 直接退出。
//   **既不是 0 也不是 SIG_ERR 的第三种返回值**是最难缠的形态：
//   一半程序静默通过、一半程序莫名退出，而源码上完全看不出差别。
//
// ## 修法：删掉多的那一份（不是"把两份改成一样"）
//
// 按「**相同的函数只保留一份**」：`os.c` 那份删掉、并从 `Lib/modules.json` 的
// `os.Functions` 里去掉（那份表驱动 GenLib 的包装/绑定生成）。
// 留下的 `util.c` 那份与头文件、与 POSIX 空实现的语义一致。
//
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
// EXPECT: A=0|B=0|C=0|D=0|E=0|F=2,28,20
