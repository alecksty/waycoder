#include "stdarg.h"

#param lib("math")

// VML Shared Utility Library

__stdcall void delay(int ms) {
    asm("SYSCALL #52");
}

__stdcall int sscanf(const char* s, const char* fmt, void* ptr) {
    return 0;
}

// ⚠ 这里原本还有一份 `atoi` —— **已删**。全库曾有**三份** `atoi`：
//   `convert.c`（跳空白 + 认 +/-）、`util.c`（**不跳空白、不认 +、只认 -**）、
//   `builtins.c`（同 convert）。模块映射把 `atoi` 指向 `convert`，但 `util` 也被链进来，
//   而重名标签**按链接顺序后者覆盖**⇒ 实测生效的是 **util 那份最弱的**：
//       atoi("42")=42 ✓   atoi("+7")=0 ✗   atoi("   42")=0 ✗
//   ⇒ 按「相同函数只留一份」删掉本份与 builtins 那份，唯一实现留在 `convert.c`。

// Integer power: base^exp (exp >= 0)
__stdcall int int_pow(int base, int exp) {
    int result = 1;
    while (exp > 0) {
        if (exp & 1) result *= base;
        base *= base;
        exp >>= 1;
    }
    return result;
}

// Integer square root (floor)
__stdcall int int_sqrt(int n) {
    if (n <= 0) return 0;
    int x = n;
    int y = (x + 1) / 2;
    while (y < x) {
        x = y;
        y = (x + n / x) / 2;
    }
    return x;
}

// Sum of integer array
__stdcall int sum(int* arr, int n) {
    int total = 0;
    for (int i = 0; i < n; i++)
        total += arr[i];
    return total;
}

/* ── locale（见 Lib/c/locale.h：本平台只有 C/POSIX 一种 locale） ──
   `setlocale` 恒返回 "C"：老程序拿它判"设置成没成功"，给真字符串比给 NULL 安全
   （NULL 会让不少程序走"locale 不可用"的降级分支）。 */
__stdcall char* setlocale(int category, const char* locale) {
    (void)category; (void)locale;
    return "C";
}

static struct { char* dp; char* ts; char* gr; } _lconv = { ".", "", "" };

__stdcall void* localeconv(void) {
    return &_lconv;
}

/* ═══════════════════════════════════════════════════════════
 * 老程序要的那些"标准但本平台没有"的函数（补环境，不是补库）
 * 动因：拿真实老程序（tty-clock）压编译链时，卡在这里的全是这一族。
 * ═══════════════════════════════════════════════════════════ */

/* ── fprintf：老程序拿它往 stderr 打错误信息 ──
   ⚠ VML 的 `FILE` 就是个 fd（见 stdio.h），这里不去区分流 —— 全走标准输出。
   真"分流出错信息"要等命令行页把两个流分开（那边本来就把 stderr 单独收着）。 */
extern int format_arg_count(const char* format);
extern int vsnprintf(char* buf, const char* fmt, const int* args, int nargs);
extern int print_str(const char* s);

__stdcall int fprintf(int stream, const char* fmt, ...) {
    char buf[512];
    int vals[16];
    int i;
    int nargs;
    int n;
    va_list ap;

    (void)stream;
    nargs = format_arg_count(fmt);
    if (nargs > 16) nargs = 16;
    va_start(ap, fmt);
    for (i = 0; i < nargs; i++) vals[i] = va_arg(ap, int);
    va_end(ap);
    n = vsnprintf(buf, fmt, vals, nargs);
    buf[n] = 0;
    print_str(buf);
    return n;
}

/* ── strerror：返回一句人话 ──
   老程序把它拼进错误信息里。本平台没有 errno 表，给个固定的描述即可。 */
__stdcall char* strerror(int errnum) {
    if (errnum == 0) return "no error";
    return "error";
}

/* ── sigaction：老程序拿它装 SIGWINCH/SIGINT 的处理函数 ──
   本平台没有信号这一套（手机上没有"终端窗口改变"这回事），
   装了就装不上、也不该报错 ⇒ **返回 0（成功）**，让程序继续走。
   真返回 -1 的话，不少程序会当成"致命错误"直接退出。 */
struct vml_sigaction { void* handler; int mask; int flags; };

__stdcall int sigaction(int sig, void* act, void* old) {
    (void)sig; (void)act; (void)old;
    return 0;
}

/* ── localtime / gmtime ──
   ⚠ 字段顺序必须与 `Lib/c/time.h` 的 `struct tm` **逐字对齐**（实现文件不 include 头，
   靠约定 —— 与 curses 那条同源的风险）。本平台没有时区/夏令时，两者结果相同。 */
struct vml_tm {
    int tm_sec;
    int tm_min;
    int tm_hour;
    int tm_mday;
    int tm_mon;
    int tm_year;
    int tm_wday;
    int tm_yday;
    int tm_isdst;
};

static struct vml_tm _tm_buf;

/* 天数 → 年月日（Howard Hinnant 的 civil_from_days，整数运算、无循环） */
static struct vml_tm* _ts_to_tm(int ts)
{
    int z;
    int era;
    int doe;
    int yoe;
    int y;
    int doy;
    int mp;
    int d;
    int m;
    int secs;

    secs = ts;
    z = secs / 86400;
    if (secs % 86400 < 0) z = z - 1;          /* 负数时间戳：向零取整的修正 */

    _tm_buf.tm_hour = (secs / 3600) % 24;
    _tm_buf.tm_min  = (secs / 60) % 60;
    _tm_buf.tm_sec  = secs % 60;
    if (_tm_buf.tm_hour < 0) _tm_buf.tm_hour = _tm_buf.tm_hour + 24;
    if (_tm_buf.tm_min  < 0) _tm_buf.tm_min  = _tm_buf.tm_min  + 60;
    if (_tm_buf.tm_sec  < 0) _tm_buf.tm_sec  = _tm_buf.tm_sec  + 60;

    _tm_buf.tm_wday = (z + 4) % 7;            /* 1970-01-01 是星期四 */
    if (_tm_buf.tm_wday < 0) _tm_buf.tm_wday = _tm_buf.tm_wday + 7;

    z = z + 719468;                           /* 移到 0000-03-01 纪元 */
    era = z / 146097;
    if (z % 146097 < 0) era = era - 1;
    doe = z - era * 146097;
    yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
    y = yoe + era * 400;
    doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
    mp = (5 * doy + 2) / 153;
    d = doy - (153 * mp + 2) / 5 + 1;
    m = mp + 3;
    if (mp >= 10) m = mp - 9;
    if (m <= 2) y = y + 1;

    _tm_buf.tm_mday = d;
    _tm_buf.tm_mon  = m - 1;                  /* tm_mon 是 0 起 */
    _tm_buf.tm_year = y - 1900;               /* tm_year 是"1900 起" */
    _tm_buf.tm_yday = doy;                    /* 近似：3 月起的年内天序，够用 */
    _tm_buf.tm_isdst = 0;
    return &_tm_buf;
}

__stdcall struct vml_tm* localtime(int* t) {
    if (t) return _ts_to_tm(*t);
    return _ts_to_tm((int)asm("SYSCALL 54"));   /* 不给就用当前时间（#54 GetDateTime） */
}

__stdcall struct vml_tm* gmtime(int* t) {
    return localtime(t);                        /* 本平台无时区，两者相同 */
}

/* ── strftime：时间程序都靠它把 struct tm 拼成字符串 ──
   只做老程序真正常用的那些转换；不认识的 `%x` 原样保留（比输出空白好查）。 */
static void _sf_put2(char* s, int* n, int max, int v) {
    if (*n + 2 > max) return;
    s[*n] = (char)('0' + (v / 10) % 10); (*n)++;
    s[*n] = (char)('0' + v % 10);        (*n)++;
}

static void _sf_puts(char* s, int* n, int max, const char* t) {
    int i;
    for (i = 0; t[i] != 0; i++) { if (*n + 1 > max) return; s[*n] = t[i]; (*n)++; }
}

static const char* _sf_mon[] = { "Jan","Feb","Mar","Apr","May","Jun",
                                 "Jul","Aug","Sep","Oct","Nov","Dec" };
static const char* _sf_day[] = { "Sun","Mon","Tue","Wed","Thu","Fri","Sat" };

__stdcall int strftime(char* s, int max, const char* fmt, struct vml_tm* tm) {
    int n;
    int i;
    int v;

    if (!s || !fmt || !tm) return 0;
    n = 0;
    for (i = 0; fmt[i] != 0; i++) {
        if (fmt[i] != '%') {
            if (n + 1 < max) { s[n] = fmt[i]; n++; }
            continue;
        }
        i++;
        if (fmt[i] == 0) break;
        if (fmt[i] == 'H') { _sf_put2(s, &n, max - 1, tm->tm_hour); continue; }
        if (fmt[i] == 'M') { _sf_put2(s, &n, max - 1, tm->tm_min);  continue; }
        if (fmt[i] == 'S') { _sf_put2(s, &n, max - 1, tm->tm_sec);  continue; }
        if (fmt[i] == 'm') { _sf_put2(s, &n, max - 1, tm->tm_mon + 1); continue; }
        if (fmt[i] == 'd') { _sf_put2(s, &n, max - 1, tm->tm_mday); continue; }
        if (fmt[i] == 'Y') {
            v = tm->tm_year + 1900;
            if (n + 4 < max) {
                s[n] = (char)('0' + (v / 1000) % 10); n++;
                s[n] = (char)('0' + (v / 100) % 10);  n++;
                s[n] = (char)('0' + (v / 10) % 10);   n++;
                s[n] = (char)('0' + v % 10);          n++;
            }
            continue;
        }
        if (fmt[i] == 'y') { _sf_put2(s, &n, max - 1, (tm->tm_year + 1900) % 100); continue; }
        if (fmt[i] == 'a') { _sf_puts(s, &n, max - 1, _sf_day[tm->tm_wday % 7]); continue; }
        if (fmt[i] == 'b' || fmt[i] == 'h') { _sf_puts(s, &n, max - 1, _sf_mon[tm->tm_mon % 12]); continue; }
        if (fmt[i] == 'p') { _sf_puts(s, &n, max - 1, tm->tm_hour < 12 ? "AM" : "PM"); continue; }
        if (fmt[i] == 'I') { v = tm->tm_hour % 12; if (v == 0) v = 12; _sf_put2(s, &n, max - 1, v); continue; }
        if (fmt[i] == 'j') { v = tm->tm_yday + 1; if (n + 3 < max) {
                s[n] = (char)('0' + (v / 100) % 10); n++;
                s[n] = (char)('0' + (v / 10) % 10);  n++;
                s[n] = (char)('0' + v % 10);         n++; } continue; }
        if (fmt[i] == '%') { if (n + 1 < max) { s[n] = '%'; n++; } continue; }
        /* 不认识：原样留 `%X`，免得"悄悄少一段" */
        if (n + 2 < max) { s[n] = '%'; n++; s[n] = fmt[i]; n++; }
    }
    if (n < max) s[n] = 0;
    return n;
}

/* ── time：当前 Unix 秒（`#54 GetDateTime`） ── */
__stdcall int time(int* t) {
    int now;
    now = (int)asm("SYSCALL 54");
    if (t) *t = now;
    return now;
}

/* ── sys/select.h 的实现（见 Lib/c/sys/select.h 的说明） ── */
__stdcall void _vml_fd_zero(void* p) {
    int* w = (int*)p;
    int i;
    for (i = 0; i < 3; i++) w[i] = 0;
}

__stdcall void _vml_fd_set(int n, void* p) {
    int* w = (int*)p;
    if (n < 0 || n >= 64) return;
    w[n / 32] = w[n / 32] | (1 << (n % 32));
}

__stdcall void _vml_fd_clr(int n, void* p) {
    int* w = (int*)p;
    if (n < 0 || n >= 64) return;
    w[n / 32] = w[n / 32] & ~(1 << (n % 32));
}

__stdcall int _vml_fd_isset(int n, void* p) {
    int* w = (int*)p;
    if (n < 0 || n >= 64) return 0;
    return (w[n / 32] >> (n % 32)) & 1;
}

/* `select`：本平台没有 fd 多路复用，**一律按"超时"返回**（0 = 没有 fd 就绪）。
   老程序拿到 0 就会走"这一轮没输入"的分支，正是我们要的节奏。
   超时时间照等 —— 否则 tty-clock 那类"每秒刷一次"的循环会变成忙等烧 CPU。 */
__stdcall int select(int nfds, void* r, void* w, void* e, void* timeout) {
    int sec;
    int usec;
    int ms;

    (void)nfds; (void)r; (void)w; (void)e;
    sec = 0;
    usec = 0;
    if (timeout) {
        sec  = ((int*)timeout)[0];
        usec = ((int*)timeout)[1];
    }
    ms = sec * 1000 + usec / 1000;
    if (ms > 0 && ms < 60000) delay(ms);
    return 0;
}

__stdcall int pselect(int nfds, void* r, void* w, void* e, void* timeout, void* mask) {
    (void)mask;
    return select(nfds, r, w, e, timeout);
}

/* ── nanosleep / atexit ── */
__stdcall int nanosleep(void* req, void* rem) {
    int sec;
    int nsec;
    (void)rem;
    sec = 0; nsec = 0;
    if (req) { sec = ((int*)req)[0]; nsec = ((int*)req)[1]; }
    delay(sec * 1000 + nsec / 1000000);
    return 0;
}

__stdcall int atexit(void (*fn)(void)) { (void)fn; return 0; }  /* 本平台不跑退出钩子 */

/* ── getopt / strdup（老程序到处在用） ──
   `getopt` 的全局变量（optarg/optind/opterr/optopt）在 `Lib/c/unistd.h` 里已声明，
   这里只补**函数本体**。 */
extern char* optarg;
extern int optind;
extern int optopt;

__stdcall int getopt(int argc, char** argv, const char* optstring) {
    int i;
    int j;
    int needs_arg;

    if (optind >= argc) return -1;
    if (argv[optind][0] != '-') return -1;
    if (argv[optind][1] == 0) return -1;              /* 单个 "-"：不是选项 */

    optopt = argv[optind][1];

    /* 在 optstring 里找这个字母；后面跟 ':' 表示它要带一个参数 */
    needs_arg = 0;
    for (j = 0; optstring[j] != 0; j++) {
        if (optstring[j] == optopt) {
            if (optstring[j + 1] == ':') needs_arg = 1;
            break;
        }
    }
    if (optstring[j] == 0) { optind = optind + 1; return '?'; }   /* 不认识的选项 */

    if (needs_arg) {
        optind = optind + 1;
        if (optind >= argc) return '?';
        optarg = argv[optind];
        optind = optind + 1;
    } else {
        optind = optind + 1;
    }
    (void)i;
    return optopt;
}

__stdcall char* strdup(const char* s) {
    char* p;
    int i;
    int n;
    if (!s) return 0;
    n = 0;
    while (s[n] != 0) n++;
    p = (char*)alloc(n + 1);
    if (!p) return 0;
    for (i = 0; i <= n; i++) p[i] = s[i];
    return p;
}

/* ── ioctl / termios（见 Lib/c/sys/ioctl.h、termios.h 的说明） ──
   本平台没有真终端 ⇒ 除 TIOCGWINSZ 外一律"返回成功但不做事"。 */

/* `TIOCGWINSZ` 必须**真答尺寸**：老程序拿它做布局，答不出来就是"画到屏幕外"。 */
__stdcall int ioctl(int fd, int request, void* arg) {
    if (request == 0x5413 && arg) {          /* TIOCGWINSZ */
        unsigned short* ws = (unsigned short*)arg;
        ws[0] = 25;                          /* ws_row */
        ws[1] = 80;                          /* ws_col */
        ws[2] = 0;                           /* ws_xpixel */
        ws[3] = 0;                           /* ws_ypixel */
    }
    (void)fd;
    return 0;
}

__stdcall int tcgetattr(int fd, void* t) {
    (void)fd; (void)t;
    return 0;
}

__stdcall int tcsetattr(int fd, int actions, void* t) {
    (void)fd; (void)actions; (void)t;
    return 0;
}

__stdcall void cfmakeraw(void* t) { (void)t; }
__stdcall int tcflush(int fd, int queue) { (void)fd; (void)queue; return 0; }
__stdcall int cfgetospeed(void* t) { (void)t; return 38400; }
__stdcall int cfsetospeed(void* t, int speed) { (void)t; (void)speed; return 0; }

/* ── fcntl.h 的 open/creat ──
   本平台的文件访问走既有的 file.* 那层（沙箱根 = 工作区）。这里给最小实现：
   开了记个句柄号，真正的读写由 `fread`/`fwrite`（file.c）负责。 */
__stdcall int open(const char* path, int flags, int mode) {
    (void)flags; (void)mode;
    return (int)path;
}

__stdcall int creat(const char* path, int mode) {
    (void)mode;
    return (int)path;
}
