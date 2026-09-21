// `console.h`（termios 简化版）+ `sys/ioctl.h` —— **"我是不是在终端上跑"那一套**。
//
// ## 为什么这一套对老程序是硬需求
//
// 老程序开头的标准套路是：
//
//     if (!isatty(1)) { color = 0; }             /* 重定向到文件就关掉颜色 */
//     ioctl(0, TIOCGWINSZ, &ws);                 /* 拿屏宽排版，拿不到就按 80 猜 */
//     tcgetattr(0, &saved); cfmakeraw(&raw); tcsetattr(0, TCSANOW, &raw);
//     …跑界面…
//     tcsetattr(0, TCSANOW, &saved);             /* 退出前还原 */
//
// 这四句**任何一句**返回了非 0 / 拿不到尺寸，程序就会走降级分支或者画到屏幕外。
// 而它们全是 `void`/返回码型的接口 —— **返回值对了不等于语义对了**，所以判据要压：
//
//   · 返回码（`isatty` 是不是 1、termios 三个是不是 0）
//   · `ioctl(TIOCGWINSZ)` **真的填了尺寸** —— 老程序拿它排版，答不出来就是画到屏外
//   · `ttyname` 给的是不是个像样的设备名（老程序会 `strstr(ttyname(0), "tty")`）
//
// ## 期望值怎么独立推导
//
// 读 `Lib/shared/src/util.c`：
//   · `isatty(fd)`      ⇒ **无条件 `return 1`**（本平台"永远在终端上跑"）
//   · `ttyname(fd)`     ⇒ 字面量 `"/dev/tty"`
//   · `tcgetattr`/`tcsetattr`/`tcflush` ⇒ 恒 `return 0`（收下不做事，但**不报错**）
//   · `cfmakeraw`       ⇒ 空实现（`void`）
//   · `ioctl(fd, TIOCGWINSZ, arg)` ⇒ 把 `arg` 当 `unsigned short*` 填
//     `ws[0]=25 / ws[1]=80 / ws[2]=0 / ws[3]=0` —— 与 `conio.h` 的 `CON_ROWS/COLS`
//     是同一个 25×80（**这两处是两份独立的常量**，写歪一处就会"文字界面按 25 行排、
//     尺寸查询答另一个数"）
//
// ## KNOWN-RED：④ 那三条压的是一个**新发现的 ABI 不匹配**
//
// 实测 `ioctl(0, TIOCGWINSZ, ws)` **返回 0 但一个字节都没写**（`ws` 保持原值）。
// 根因是**头文件与实现的形参宽度对不上**：
//
//     Lib/c/sys/ioctl.h:23        int ioctl(int fd, **unsigned long** request, void *arg);
//     Lib/shared/src/util.c:444   __stdcall int ioctl(int fd, **int** request, void* arg)
//
// 而本平台的调用约定是「4 字节对齐，**只有 64 位参数占 2 个位置**」
// （见 `docs/` 的 ABI 基准与 `scripts/vml-abi-probe`）。于是：
//
//   调用方（按 `unsigned long` 编）压 **2 个槽**：`[0x0000][0x5413]`
//   被调方（按 `int` 编）读 **1 个槽**：`request = 0x5413` ✓，`arg = 高位槽 = 0` ✗
//   ⇒ `if (request == TIOCGWINSZ && arg)` 的**后半个条件为假** ⇒ 整个分支跳过
//
// 验证方式（两种签名各编一次，其余逐字相同）：
//
//     按头文件（unsigned long）  ⇒ A=0, B=0,0      ← 缓冲区没被碰
//     按实现  （int）           ⇒ A=0, B=25,80    ← 正确
//
// **这一条的杀伤面**：`ioctl(0, TIOCGWINSZ, …)` 正是老程序（含 ncurses 自己）
// 拿屏宽排版的**唯一**入口，答不出来就按 80×25 猜或者画到屏外。而它**返回 0**
// —— 程序读返回码判不出任何异常。
//
// 修法在头文件侧（把 `unsigned long` 改成 `int`，与实现对齐），或用 `#param`
// 让两边同源。**本用例不改**（改了就没判据了），期望值写的是**正确语义**。
//
// ⚠ `Lib/c/console.h` 里还有两个**声明了但链不上**的：`tcdrain` 和
//   `ioctl_console`（实测编译期即报"未定义的函数"）。老程序用 `tcdrain` 做
//   "等输出发完再收尾"，用不到 `ioctl_console`（那是本平台自己的封装）。
//   本用例**不调用它们** —— 调了就整条编不过。
//   `ioctl(0, …)` 与 `ioctl_console` **不是一回事**：前者在 `sys/ioctl.h`。
#include <stdio.h>
#include <string.h>
#include <console.h>
#include <sys/ioctl.h>

int main()
{
    termios_t t;
    unsigned short ws[4];

    /* ① `isatty` —— 老程序拿它决定"要不要上色" */
    printf("\nA=%d", isatty(0));
    printf("\nB=%d", isatty(1));

    /* ② `ttyname` 是个像样的设备名（判据是**包含 tty**，不是逐字比对 ——
          换个实现只要给的是个 tty 名字就该绿） */
    printf("\nC=%d", strstr(ttyname(0), "tty") != 0);

    /* ③ termios 三件套：**返回 0 而不是 -1** —— 返回 -1 会让老程序走
          "没有终端"的降级分支（而那在手机上是错的：我们有终端） */
    printf("\nD=%d", tcgetattr(0, &t));
    printf("\nE=%d", tcsetattr(0, TCSANOW, &t));
    printf("\nF=%d", tcflush(0, TCIOFLUSH));
    cfmakeraw(&t);                       /* void：只验"调了不崩" */
    printf("\nG=1");

    /* ④ `ioctl(TIOCGWINSZ)` 必须**真的填数** —— 这是本用例最硬的一条：
          `30-conio` 系列画界面的行数是 25，这里答的必须也是 25，
          否则"按尺寸排版"的程序会把内容画到屏幕外。
          判据用 `ws[0]*ws[1]` 这个组合值，字段串位也逃不掉。 */
    ws[0] = 0; ws[1] = 0; ws[2] = 0xFFFF; ws[3] = 0xFFFF;
    printf("\nH=%d", ioctl(0, TIOCGWINSZ, ws));
    printf("\nI=%d,%d", ws[0], ws[1]);
    printf("\nJ=%d", ws[0] * ws[1]);

    /* ⑤ 不认识的 request 也要**返回成功且不写坏缓冲区**
          （老程序会试一串 request，拿不到就当不支持） */
    ws[0] = 7;
    printf("\nK=%d", ioctl(0, 0x1234, ws));
    printf("\nL=%d", ws[0]);

    return 0;
}
// KNOWN-RED
// EXPECT: A=1|B=1|C=1|D=0|E=0|F=0|G=1|H=0|I=25,80|J=2000|K=0|L=7
