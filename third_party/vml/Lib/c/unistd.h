/* VML unistd.h stub */
/* 标准流的 fd 号（POSIX 固定值）*/
#define STDIN_FILENO  0
#define STDOUT_FILENO 1
#define STDERR_FILENO 2

/* getopt globals —— **只声明，不定义**。
   ⚠ 定义在 `Lib/shared/src/util.c`（`getopt` 的实现所在模块）。
   写成定义会让**每个 include 了本头的使用者**都生成一份槽位，
   把库里那份遮住 —— 那正是 `stdscr` 当初的形态（见 `extern` 那一版）。 */
extern char* optarg;
extern int   optind;
extern int   opterr;
extern int   optopt;
