/* config.h —— 本平台的「能力描述头」（autoconf 生成物的替代品）
 *
 * ## 为什么需要它
 *
 * 90 年代之后的 C 程序几乎都带一个 autoconf 生成的 `config.h`，内容是
 * 一堆 `HAVE_XXX_H` / `HAVE_XXX` —— **这台机器有什么**。程序正文靠它开
 * 关 #include（这是当年为了跨 Solaris/HPUX/Linux 通吃而立的规矩）：
 *
 *     #ifdef HAVE_TERMIOS_H
 *     #include <termios.h>
 *     #endif
 *
 * 我们是把源文件直接丢给编译器、**没有 configure 那一步** ⇒ `config.h`
 * 不存在 ⇒ 所有 `HAVE_*` 都是未定义 ⇒ **条件里的头一个都进不来**。
 * 症状是一堆看起来像"缺头"、其实是"缺配置"的报错：
 *
 *     cmatrix.c:282:24: error: 未声明的变量 'TIOCGWINSZ'
 *     cmatrix.c:341:14: error: 未声明的变量 'opterr'
 *     cmatrix.c:359:29: error: 未声明的变量 'optarg'
 *
 * ⚠ **实测踩过**：这六个头（getopt/fcntl/termios/termio/sys-ioctl/ncurses）
 * 当时**全都已经躺在 `Lib/c/` 下**，一个都没被 include —— 因为卡在
 * `#include "config.h"` 那一句上（**引号形式**，做缺口扫描时按尖括号
 * 扫会整个漏掉）。看着像"库没补"，其实是"配置没给"。
 *
 * ## 为什么放在这里、而不是喂 -D
 *
 * `config.h` 的内容是**目标平台的能力**，与具体程序无关 —— 这与交叉编译
 * 时提供一份 sysroot 描述是同一件事。放一份在这里，**所有老程序共享**。
 * （程序**自己**目录下的 `config.h` 仍然优先：`ResolveIncludePath` 先查
 * 源文件所在目录 —— 那是程序自己的配置，比我们的通用描述更准。）
 *
 * ## 判据：**只写我们真有的**
 *
 * 定义错一个 `HAVE_XXX` 的后果**不是编译报错、而是静默走错分支** ——
 * 程序以为有 `resizeterm` 就去调它，一路编到链接期才缺符号，或者更糟：
 * 走了一条本平台根本没实现的代码路径、跑起来才出错（本仓历史上最难查
 * 的一类）。所以下面每一条都要能在本仓库里**指出实现出处**。
 *
 * ## 这里**不放**什么
 *
 * 程序**自己**的版本号/包名（`VERSION`/`PACKAGE`/`PACKAGE_VERSION`）——
 * 那是每个程序各不相同的，属于 `-D` 的事，不是平台能力。
 * （实测 `cmatrix.c:175` 就是 `printf(VERSION, __TIME__, __DATE__)`。）
 */
#ifndef _VML_CONFIG_H
#define _VML_CONFIG_H

/* ══ 标准 C 头（对应 Lib/c/ 下同名的那些） ══ */
#define HAVE_ASSERT_H   1
#define HAVE_CTYPE_H    1
#define HAVE_ERRNO_H    1
#define HAVE_FLOAT_H    1
#define HAVE_LIMITS_H   1
#define HAVE_LOCALE_H   1
#define HAVE_MATH_H     1
#define HAVE_SIGNAL_H   1
#define HAVE_STDARG_H   1
#define HAVE_STDBOOL_H  1
#define HAVE_STDDEF_H   1
#define HAVE_STDINT_H   1
#define HAVE_STDIO_H    1
#define HAVE_STDLIB_H   1
#define HAVE_STRING_H   1
#define HAVE_TIME_H     1

/* 老程序用它判"有没有标准 C 头"，进了这条分支才有 string.h/stdlib.h。
   （autoconf 的 AC_HEADER_STDC，1990s 程序遍地都是。） */
#define STDC_HEADERS    1

/* ⚠ 刻意**不**定义 `HAVE_SETJMP_H` —— `Lib/c/` 下没有 `setjmp.h`，
   定义了就等于骗程序去 `#include` 一个不存在的头。真遇到要 setjmp 的
   程序再补实现，**别在这里造假**。 */

/* ══ POSIX 头 ══ */
#define HAVE_FCNTL_H        1
#define HAVE_GETOPT_H       1
#define HAVE_UNISTD_H       1
#define HAVE_TERMIOS_H      1   /* 优先于 termio.h（程序里是 #elif 关系） */
#define HAVE_TERMIO_H       1
#define HAVE_SYS_IOCTL_H    1
#define HAVE_SYS_STAT_H     1
#define HAVE_SYS_TYPES_H    1
#define HAVE_SYS_SELECT_H   1

/* ══ 终端 / 界面头 ══ */
#define HAVE_CURSES_H       1
#define HAVE_NCURSES_H      1   /* 转发 curses.h，见该文件头部 */
#define HAVE_CONIO_H        1

/* ══ 平台提供的能力（**函数级**，不是头级） ══
 *
 * 这一组对应的是"库函数在不在"，autoconf 用 `AC_CHECK_LIB` 探测、
 * 我们按 `Lib/shared/src/curses.c` 里的**实际实现**填。
 * ⚠ 同样只写真有的：写错就是让程序去调一个不存在的函数。 */
#define HAVE_USE_DEFAULT_COLORS 1   /* curses.c：use_default_colors() */
#define HAVE_WRESIZE            1   /* curses.c：wresize() */

/* ⚠ 刻意**不**定义 `HAVE_RESIZETERM` —— `curses.c` 里还没有 `resizeterm()`。
   cmatrix 那边是 `#ifdef HAVE_RESIZETERM … #else #ifdef HAVE_WRESIZE …`，
   不定义它会自动退到 `wresize`，正是我们要的那条路。 */

/* ⚠ 刻意**不**定义 `HAVE_SETFONT` / `HAVE_CONSOLECHARS` ——
   那是 **Linux 虚拟控制台**专有的（往 /dev/tty 发 KDFONTOP/PIO_FONT 换字体），
   手机上既没有那个设备、也没有那个概念。cmatrix 用它们给方块换"墙/空格"
   字符，不定义就会走普通 `addch` 那条路，是对的。 */

#endif /* _VML_CONFIG_H */
