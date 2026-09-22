/* VML unistd.h stub */

/* ⚠ `#param lib("util")`：本头声明的东西（`usleep`/`nanosleep` 一族）实现在
   `Lib/shared/src/util.c`。**规矩是「用了这个头文件就连这个库」** ——
   写在头文件里才对（写生成物上会被 GenLib 抹掉、写实现体的 .c 里语义不对，
   两条都实测踩过）。`util` 是本头的唯一下游。 */
#param lib("util")

/* 标准流的 fd 号（POSIX 固定值）*/
#define STDIN_FILENO  0
#define STDOUT_FILENO 1
#define STDERR_FILENO 2

/* ── 睡眠 ──
   ⚠ 这两个**必须在头文件里声明**：前端要看到声明才知道这个函数存在，
   光有实现会报「未定义的函数 'usleep'」（实测 `sl` 就是卡在这一条）。
   `usleep` 的单位是**微秒**；实现里按毫秒睡、且不足 1ms 的请求至少睡 1ms。 */
int usleep(unsigned int usec);
int nanosleep(void* req, void* rem);

/* getopt globals —— **只声明，不定义**。
   ⚠ 定义在 `Lib/shared/src/util.c`（`getopt` 的实现所在模块）。
   写成定义会让**每个 include 了本头的使用者**都生成一份槽位，
   把库里那份遮住 —— 那正是 `stdscr` 当初的形态（见 `extern` 那一版）。 */
extern char* optarg;
extern int   optind;
extern int   opterr;
extern int   optopt;
