/* signal.h —— 信号常量与安装入口
 *
 * ## 本平台的语义：**照单收下，永不触发**
 *
 * 手机上没有 UNIX 那套信号（没有进程组、没有异步中断、没有控制终端），
 * 但老程序几乎都拿它做两件事：
 *
 *   ① `signal(SIGINT, handler)` —— "用户按了 Ctrl+C 就收尾退出"
 *   ② `SIGWINCH`（窗口尺寸变了）/ `SIGTSTP`（挂起）/ `SIGCONT`（恢复）
 *
 * 所以这里的做法是：**常量给全**（缺一个就编译不过），
 * **装处理函数一律返回成功**（返回 `SIG_ERR` 会让不少程序当成致命错误
 * 直接退出，那比"收不到信号"糟得多 —— 见 docs/老程序兼容性.md 第四节）。
 * 处理函数**永远不会被调用**，这在手机上是对的：窗口尺寸变化走的是
 * MAUI 的 `OnSizeAllocated`，不经过信号。
 *
 * ⚠ **补头的判据是「系统头的完整清单」，不是「某个程序用到什么」** ——
 * 这里踩过一次：最初只补了 tty-clock 用到的那几个（SIGSEGV/SIGPIPE/
 * SIGINT/SIGALRM…），轮到 cmatrix 一上来就报
 * `未声明的变量 'SIGWINCH'` —— 而它明明 include 了 <signal.h>。
 * **同一个头被两个程序用，缺的常量不会重合。**
 */
#ifndef _SIGNAL_H
#define _SIGNAL_H

#param lib("util")

/* ── 信号编号：与 Linux/x86 逐字对齐 ──
   对齐是有意的：老程序里偶有 `if (signo == 20)` 这种硬编码写法，
   换个编号体系会让它静默判错。 */
#define SIGHUP       1
#define SIGINT       2
#define SIGQUIT      3
#define SIGILL       4
#define SIGTRAP      5
#define SIGABRT      6
#define SIGIOT       6
#define SIGBUS       7
#define SIGFPE       8
#define SIGKILL      9
#define SIGUSR1     10
#define SIGSEGV     11
#define SIGUSR2     12
#define SIGPIPE     13
#define SIGALRM     14
#define SIGTERM     15
#define SIGSTKFLT   16
#define SIGCHLD     17
#define SIGCLD      17
#define SIGCONT     18
#define SIGSTOP     19
#define SIGTSTP     20
#define SIGTTIN     21
#define SIGTTOU     22
#define SIGURG      23
#define SIGXCPU     24
#define SIGXFSZ     25
#define SIGVTALRM   26
#define SIGPROF     27
#define SIGWINCH    28
#define SIGIO       29
#define SIGPOLL     29
#define SIGPWR      30
#define SIGSYS      31

/* ── 特殊处理函数值（老程序拿它们比对/传参） ── */
#define SIG_ERR  ((void (*)(int))-1)
#define SIG_DFL  ((void (*)(int))0)
#define SIG_IGN  ((void (*)(int))1)
#define SIG_HOLD ((void (*)(int))2)

/* ── 安装入口 ──
   ⚠ 声明成 `void*` 而不是 C 标准的函数指针返回类型：本 C 前端对
   "返回函数指针的函数"那种嵌套声明支持有限，而老程序**从不使用**
   `signal()` 的返回值（都是 `signal(SIGX, h);` 当语句用）。
   传进来的 handler 也是直接写函数名 —— 见 `util.c` 的实现说明。 */
int signal(int sig, void *handler);
int raise(int sig);

#endif /* _SIGNAL_H */
