#include "stdarg.h"

#param lib("math")

// VML Shared Utility Library

__stdcall void delay(int ms) {
    asm("SYSCALL #52");
}

// ⚠ 这里原本还有一份 `sscanf` —— **已删**。它是个**返回 0 的桩**
//   （`{ return 0; }`，签名还是定参的 `(const char*, const char*, void*)`），
//   而 `scanf.c` 里有一份**真的**（变参 → `vsscanf`）。
//   两份同时被链接时，重名标签**谁赢取决于链接顺序** ⇒ 实测生效的是**这个桩**，
//   于是 `sscanf("12 hello", "%d %s", &v, w)` **一个变量都不写回**、返回 0 ——
//   判据 `scripts/vml-c-probe/cases/12-sscanf.c` 压的就是这个（`F=0||n=0`）。
//
//   这与本文件下面那条 `atoi`（曾有三份）是**同一个毛病**：**静默不写**比报错难查得多，
//   调用方只看到"变量还是老值"。处置也一样 —— **相同函数只留一份**，
//   唯一的实现留在 `scanf.c`（它的绑定本来就是从那儿导出的：
//   `Lib/c/shared_bindings.h` 里 `int sscanf(const char *str, const char *format, ...)`，
//   证明 GenLib 导得出变参函数，不需要这个定参桩来"顶绑定"）。

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

/* ── vfprintf：**参数已经装好**的那一版 ──
   老程序把它包在自己的日志函数里，这是标准写法：
       void warn(const char* fmt, ...) { va_list ap; va_start(ap, fmt);
                                          vfprintf(stderr, fmt, ap); va_end(ap); }
   与 `fprintf` 的唯一差别是**参数从哪来**（那边自己 `va_start`，这边直接用
   传进来的 `va_list`）—— 正文逐字相同，**别把 `va_start` 抄过来**（对一个
   已经启动的 `va_list` 再启动一次是未定义行为）。
   `stdio.h` 早就声明了它（第 63 行），只是一直没有实现。 */
__stdcall int vfprintf(int stream, const char* fmt, va_list ap) {
    char buf[512];
    int vals[16];
    int i;
    int nargs;
    int n;

    (void)stream;
    nargs = format_arg_count(fmt);
    if (nargs > 16) nargs = 16;
    for (i = 0; i < nargs; i++) vals[i] = va_arg(ap, int);
    n = vsnprintf(buf, fmt, vals, nargs);
    buf[n] = 0;
    print_str(buf);
    return n;
}

/* ── ttyname / isatty：老程序拿它们判断"我是不是在终端上跑" ──
   典型写法是 `if (!isatty(1)) { 关掉颜色 }` 或 `ttyname(0) == NULL` ⇒ 走降级分支。
   本平台**恒为终端**（输出就是发到命令行页那个字符网格上的）⇒ 如实作答：
   `isatty` 返回 1、`ttyname` 给一个标准设备名。
   ⚠ **不能返回 NULL** —— 那会让程序把彩色界面关掉，而它明明是能彩色的。 */
__stdcall int isatty(int fd) {
    (void)fd;
    return 1;
}

__stdcall char* ttyname(int fd) {
    (void)fd;
    return "/dev/tty";
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

/* ── signal：**老程序最常用的那个**信号接口（cmatrix 第 473-476 行就是它）──
   与 sigaction 同一处置：收下、返回成功、处理函数永不触发。

   ⚠ 第二个参数声明成 `void*`、返回值也是 `int`，**不是 C 标准的
   `void (*signal(int, void (*)(int)))(int)`** —— 那个"返回函数指针"的
   嵌套声明本 C 前端支持得不好，而老程序**从不使用 signal() 的返回值**
   （清一色 `signal(SIGX, handler);` 当语句用）。两边声明要对齐：
   `Lib/c/signal.h` 里写的是同一种形状。 */
__stdcall int signal(int sig, void* handler) {
    (void)sig; (void)handler;
    return 0;
}

/* `raise(sig)`：自己给自己发信号。本平台没有信号，收下返回 0。
   注意**不能**改成"直接调 handler" —— 那会让 `raise(SIGSEGV)` 之类的
   恢复逻辑走进一个本不存在的路径。 */
__stdcall int raise(int sig) {
    (void)sig;
    return 0;
}

/* ── localtime / gmtime ──
   ⚠ 字段顺序必须与 `Lib/c/time.h` 的 `struct tm` **逐字对齐**（实现文件不 include 头，
   靠约定 —— 与 curses 那条同源的风险）。

   ⚠ 这两者此前**结果相同**（原文就写着"本平台没有时区/夏令时"），而 `localtime` 因此
   名不符实：`tty-clock` 的钟面小时差 8 小时（分/秒/日期全对，因为 UTC+8 是整小时）。
   现在 `_ts_to_tm` 仍是**纯 UTC** 换算，本地化只发生在 `localtime` 那一层（加 `#61`）。 */
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

/* `localtime` = **本地**时间：`_ts_to_tm` 本身是纯 UTC 换算，这里加上本地时区偏移（`#61`）。
   ⚠ 偏移那一段**刻意内联**、不抽成 `_vml_utc_offset()` 助手 —— `GenLib -A` 会把本文件里的
   函数（连 `static` 的也算）导出成 22 份 `<语言>/shared.*` 绑定，内部细节不该进跨语言契约
   （curses.c 那条踩过：生成出 `nt sc_write_attr(WINDOW *w);`，返回类型还被截断成 `nt`）。 */
__stdcall struct vml_tm* localtime(int* t) {
    int ts;
    int off;
    if (t) ts = *t;
    else ts = (int)asm("SYSCALL 54");           /* 不给就用当前时间（#54 GetDateTime） */
    off = (int)asm("SYSCALL 61");               /* #61 GetUtcOffset（秒，东为正） */
    return _ts_to_tm(ts + off);
}

/* `gmtime` = **纯 UTC**：同一个时间戳，**不加**偏移。
   ⚠ 此前它直接转调 `localtime`（注释写着"本平台无时区，两者相同"）—— 有了 `#61` 之后
   那句话就不成立了，两个函数必须分开，否则"用哪个都一样"会把这个号的意义抹掉。 */
__stdcall struct vml_tm* gmtime(int* t) {
    if (t) return _ts_to_tm(*t);
    return _ts_to_tm((int)asm("SYSCALL 54"));
}

/* `mktime` = **结构体 → epoch 秒**，也就是 `localtime` 的**逆** ——
   所以这里要**减去** `#61` 的偏移（`localtime` 是加、`gmtime` 是不加）。

   C 标准还要求它把 `*tp` 的字段**归一化后写回**，老程序正是靠这一条做日期加减：

       t.tm_mday += 1;  mktime(&t);      ← 明天（跨月、跨年都对）

   ⚠ 这里**不能**再写一对 C 注释符号 —— 块注释**不嵌套**，里面那对会把本段提前关掉，
   后面的正文就漏进词法器了（实测形态：`未知字符：⚠`，报在注释中间那一行，看着莫名其妙）。

   ⚠ 归一化**不逐字段判边界**，而是把整件事化成一次"自 1970-01-01 起的秒数"运算：
   先折月/年，再把 `(tm_mday - 1)` 天与 时/分/秒 一律按秒相加 ⇒ `tm_mday=32`、
   `tm_mon=13`、`tm_sec=3600` 这些写法**自动**滚到正确位置，一个边界 `if` 都不用写。

   ⚠ `_days_from_civil`（civil_from_days 的逆）**刻意内联**、不抽成助手 —— 与 `localtime`
   里内联偏移同一个理由：`GenLib -A` 会把本文件的函数（**连 `static` 的也算**）导出成
   22 份 `<语言>/shared.*` 绑定，内部细节不该进跨语言契约。

   ⚠ 回填走 `_ts_to_tm`，而它写的是 `_tm_buf`（`localtime`/`gmtime` 共用的那个缓冲）——
   **先把调用方的字段读进局部量再转调**，否则 `mktime(localtime(&t))` 会一边读一边写
   同一个缓冲。 */
__stdcall int mktime(struct vml_tm* tp) {
    int y;
    int m;
    int days;
    int ts;
    int off;
    int era;
    int yoe;
    int doy;
    int doe;
    struct vml_tm* n;

    if (!tp) return -1;

    y = tp->tm_year + 1900;
    m = tp->tm_mon + 1;                  /* 折成 1 起 */
    if (m > 12 || m < 1) {
        y = y + (m - 1) / 12;            /* C 的除法向零取整：负数月份这里对不上，下面补 */
        m = (m - 1) % 12 + 1;
        if (m < 1) { m = m + 12; y = y - 1; }
    }

    /* 该月 1 号是第几天（Howard Hinnant 的 days_from_civil，整数、无循环） */
    y = y - (m <= 2 ? 1 : 0);
    era = y / 400;
    if (y % 400 < 0) era = era - 1;
    yoe = y - era * 400;
    doy = (153 * (m + (m > 2 ? -3 : 9)) + 2) / 5 + tp->tm_mday - 1;
    doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
    days = era * 146097 + doe - 719468;

    off = (int)asm("SYSCALL 61");        /* #61 GetUtcOffset（秒，东为正） */
    ts = days * 86400 + tp->tm_hour * 3600 + tp->tm_min * 60 + tp->tm_sec - off;

    /* 归一化后的字段写回（C 标准：连 `tm_wday`/`tm_yday` 一并由 mktime 设置） */
    n = _ts_to_tm(ts + off);
    tp->tm_sec   = n->tm_sec;
    tp->tm_min   = n->tm_min;
    tp->tm_hour  = n->tm_hour;
    tp->tm_mday  = n->tm_mday;
    tp->tm_mon   = n->tm_mon;
    tp->tm_year  = n->tm_year;
    tp->tm_wday  = n->tm_wday;
    tp->tm_yday  = n->tm_yday;
    tp->tm_isdst = 0;                    /* 本平台没有夏令时（与 `_ts_to_tm` 同口径） */
    return ts;
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
        /* 复合转换（`%F` = `%Y-%m-%d`、`%T` = `%H:%M:%S`、`%R` = `%H:%M`）。
           ⚠ **少了它不报错、只是把 `%F` 原样打出来** —— `tty-clock` 的默认日期格式
           正是 `"%F"`（`strncpy(ttyclock.option.format, "%F", …)`），症状是钟面下面
           那一行显示成字面的 `%F`，看起来像"程序没跑对"，其实是这里没认。 */
        if (fmt[i] == 'F') {
            v = tm->tm_year + 1900;
            if (n + 10 < max) {
                s[n] = (char)('0' + (v / 1000) % 10); n++;
                s[n] = (char)('0' + (v / 100) % 10);  n++;
                s[n] = (char)('0' + (v / 10) % 10);   n++;
                s[n] = (char)('0' + v % 10);          n++;
                s[n] = '-'; n++;
                s[n] = (char)('0' + ((tm->tm_mon + 1) / 10) % 10); n++;
                s[n] = (char)('0' + (tm->tm_mon + 1) % 10);        n++;
                s[n] = '-'; n++;
                s[n] = (char)('0' + (tm->tm_mday / 10) % 10); n++;
                s[n] = (char)('0' + tm->tm_mday % 10);        n++;
            }
            continue;
        }
        if (fmt[i] == 'T') { _sf_put2(s, &n, max - 1, tm->tm_hour); if (n + 1 < max) { s[n] = ':'; n++; }
                             _sf_put2(s, &n, max - 1, tm->tm_min);  if (n + 1 < max) { s[n] = ':'; n++; }
                             _sf_put2(s, &n, max - 1, tm->tm_sec);  continue; }
        if (fmt[i] == 'R') { _sf_put2(s, &n, max - 1, tm->tm_hour); if (n + 1 < max) { s[n] = ':'; n++; }
                             _sf_put2(s, &n, max - 1, tm->tm_min);  continue; }
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

/* ── nanosleep / usleep / atexit ── */
__stdcall int nanosleep(void* req, void* rem) {
    int sec;
    int nsec;
    (void)rem;
    sec = 0; nsec = 0;
    if (req) { sec = ((int*)req)[0]; nsec = ((int*)req)[1]; }
    delay(sec * 1000 + nsec / 1000000);
    return 0;
}

/* POSIX `usleep`（微秒级睡眠）—— **动画类老程序控帧率就靠它**
   （`sl` 的每一帧、`tty-clock` 的每秒重绘都用它；`sl` 缺了它整条编译过不去）。
 *
 * ⚠ 两条不能省：
 *   ① 内核单位是**毫秒**（`SYSCALL 52` = Sleep），所以要 `/1000`；
 *   ② **不足 1ms 的请求至少睡 1ms** —— 否则 `usleep(500)` 会退化成忙等，
 *      在手机上就是白烧电与发热（而旧程序里 `usleep(500)` 这种写法到处都是）。
 */
__stdcall int usleep(unsigned int usec) {
    int ms;
    ms = (int)(usec / 1000);
    if (usec > 0 && ms == 0) ms = 1;
    if (ms > 0) {
        asm("MOVE R0 ms");      /* 首参本来就在 R0，这里显式写一遍更稳 */
        asm("SYSCALL 52");
    }
    return 0;
}

__stdcall int atexit(void (*fn)(void)) { (void)fn; return 0; }  /* 本平台不跑退出钩子 */

/* ── getopt / strdup（老程序到处在用） ──

   ⚠ 这三个全局**必须在这里定义**，不能只写 `extern`。

   原先写的是三行 `extern` —— 在「`extern` 声明不占数据段槽位」修好之后，
   它们就**没有任何地方定义了**：`getopt` 内部读写的是裸名 `optind`，
   链接期解析不到本模块的槽位 ⇒ 使用者写进去的 `optind` 与 `getopt` 读到的
   根本不是同一块内存（实测 `optind = 1` 之后第一次 `getopt` 直接返回 `'b'`、
   `optind` 读出来是 98）。判据：`scripts/vml-c-probe/cases/22-getopt.c`。

   POSIX 规定 `optind` 初值 **1**（不是 0）—— 顺手按规矩给上。

   头文件那侧（`Lib/c/getopt.h`）写的是 `extern`，与这里配对；
   ⚠ 但 `Lib/c/unistd.h:9` 写的是 `int optind;`（**定义**，不是 extern）——
   那个是头文件里的重复定义，见到就要改成 `extern`。 */
char* optarg;
int   optind = 1;
int   optopt;

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
/* ⚠ `request` 的类型**必须与 `Lib/c/sys/ioctl.h` 的声明逐字一致**（两边都是 `int`）。
   头里原写的是 POSIX 的 `unsigned long`，而本平台 `long` 占**两个参数槽**（8 字节），
   实现是 `int` ⇒ 调用方压进去的 `arg` 与实现读到的**错开一个槽**，
   于是 `arg` 读成 0、整个 `TIOCGWINSZ` 分支被跳过。
   症状极具误导性：**返回 0**（成功），但 `ws` 一个字节没写 ——
   程序拿到的尺寸恒为 `0×0`，而它**没有任何办法察觉**。
   （试过反过来"把实现改成 `unsigned long` 去对齐 POSIX 头"—— **没用**，
     两侧的槽位偏移都是对的，卡在更深的 64 位参数传递上；见 `cases/24` 的注释。）
   判据：`scripts/vml-c-probe/cases/24-termios-ioctl.c`。 */
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
