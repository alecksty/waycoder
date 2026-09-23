/* conio.c —— DOS / Turbo C 文本控制台（常用子集）
 *
 * ## 它画到哪儿：**命令行页的字符网格**，不弹窗
 *
 * 这一条是最初设计错、被用户纠正的地方，写在最前面免得后人再走一遍：
 * 命令行程序**本来就跑在一个字符界面里**（命令行页那个 80×25 的网格，`nyancat`
 * 走的就是它）。所以 `conio` 该做的事只有一件 —— **产生一个真终端会产生的那些字节**：
 *   · 光标定位 `ESC[{行};{列}H`   · 颜色/属性 SGR `ESC[{码}m`   · 字符本身
 *   · 清屏 `ESC[2J`、擦到行尾 `ESC[K`
 *
 * **不发任何绘图指令**（不开窗、不 `ui_rect`/`ui_text`）。第一版就是那么写的，
 * 后果是三重的：① 弹一个和命令行页抢屏幕的窗口；② 窗口尺寸由宿主决定（实测请求
 * 200×100 实得 393×225），于是"填满"变成一道无解的算术题，白花了一轮；
 * ③ 与既有的网格渲染链（全屏检测 + FrameBuffer + 标记 + 命令行页）**成了两套实现**
 * —— 正是本仓排第一的坑。
 *
 * ## 三条约束
 *
 * ① **不做任何"把指针当整数算偏移"的读写**（ROADMAP 第零节的硬规矩）。
 *    屏幕影子就是两个普通数组，下标是普通整数运算。
 * ② **不另立渲染链** —— 只发 ANSI，交给命令行页那条既有的链去渲染。
 * ③ **出口只有 `putchar`**（`SYSCALL #4`）：一个字符一个字符发，与真终端一致。
 *
 * ## 影子缓冲为什么必要
 *
 * `delline` / `insline` / `clreol` 要**改动已经画过的内容**，而 ANSI 里能做这事的
 * `ESC[M`/`ESC[L`（DL/IL）**本平台的网格不支持**（`FrameBuffer` 只认 CUP/CUU/CUD/CUF/CUB/
 * CHA/VPA/存光标/EL/ED/SGR）。所以自己留一份影子，改完把受影响的行**重新发一遍**。
 */
#include "stdarg.h"

/* 出口与格式化：都用既有的那一份实现，不另写（`format_arg_count` 的注释见 printf.c） */
extern int putchar(int c);                    /* SYSCALL #4 —— 唯一的输出口 */
extern int getchar(void);
extern int format_arg_count(const char *format);
extern int vsnprintf(char *buf, const char *fmt, const int *args, int nargs);

#define CON_ROWS 25
#define CON_COLS 80

/* 屏幕影子（普通数组，无地址算术） */
static char conCh[CON_ROWS * CON_COLS];
static char conAt[CON_ROWS * CON_COLS];      /* 低 4 位前景 / 高 4 位背景 */
static int  conX;                            /* 光标列 0 起（对外 +1） */
static int  conY;                            /* 光标行 0 起（对外 +1） */
static int  conFg;
static int  conBg;
static int  conReady;

/* ── 出口原语 ── */
static void con_putc(int c)
{
    putchar(c);
}

static void con_puts(const char *s)
{
    int i;
    for (i = 0; s[i] != 0; i++) con_putc(s[i]);
}

/* 十进制输出（只为几个数字，不拖进 printf 家族 —— 那会把 conio 与 printf 绑死） */
static void con_putn(int v)
{
    char t[12];
    int n;
    if (v < 0) { con_putc('-'); v = -v; }
    n = 0;
    if (v == 0) { con_putc('0'); return; }
    while (v > 0 && n < 11) { t[n] = (char)('0' + (v % 10)); v = v / 10; n++; }
    while (n > 0) { n--; con_putc(t[n]); }
}

/* 光标定位（**1 起**，与 DOS 同） */
static void con_cup(int x, int y)
{
    con_putc(27); con_putc('[');
    con_putn(y + 1); con_putc(';'); con_putn(x + 1); con_putc('H');
}

/* ── DOS 16 色 → SGR ──
   0-7 走 30-37 / 40-47；8-15 走**亮色** 90-97 / 100-107（真终端就是这么表达的）。 */
/* ⚠⚠ **DOS 与 ANSI 的调色板顺序不一样，必须映射** —— 直接拿 DOS 号当 ANSI 号是错的：
     DOS: 0黑 1蓝 2绿 3青 4红 5紫 6棕 7浅灰
     ANSI:40黑 41红 42绿 43黄 44蓝 45紫 46青 47白
   也就是 DOS 的"蓝"(1) 在 ANSI 里是 44，而 ANSI 的 41 是**红**。
   实测症状：标题栏写的是 `textbackground(CYAN)`，打出来是 `[43m`（**黄**底）——
   一张 DOS 界面里所有颜色都串了位，而且**不报错**。 */
static int con_dos2ansi(int c)
{
    if (c == 0) return 0;      /* 黑 */
    if (c == 1) return 4;      /* 蓝 → ANSI 蓝 */
    if (c == 2) return 2;      /* 绿 */
    if (c == 3) return 6;      /* 青 → ANSI 青 */
    if (c == 4) return 1;      /* 红 → ANSI 红 */
    if (c == 5) return 5;      /* 紫 */
    if (c == 6) return 3;      /* 棕 → ANSI 黄 */
    if (c == 7) return 7;      /* 浅灰 */
    if (c == 8) return 8;      /* 深灰 → ANSI 亮黑 */
    if (c == 9) return 12;     /* 亮蓝 */
    if (c == 10) return 10;    /* 亮绿 */
    if (c == 11) return 14;    /* 亮青 */
    if (c == 12) return 9;     /* 亮红 */
    if (c == 13) return 13;    /* 亮紫 */
    if (c == 14) return 11;    /* 黄 → ANSI 亮黄 */
    return 15;                 /* 白 */
}

static void con_sgr_fg(int c)
{
    int a;
    int code;
    a = con_dos2ansi(c & 15);
    if (a < 8) code = 30 + a;
    else       code = 90 + (a - 8);
    con_putc(27); con_putc('['); con_putn(code); con_putc('m');
}

static void con_sgr_bg(int c)
{
    int a;
    int code;
    a = con_dos2ansi(c & 15);
    if (a < 8) code = 40 + a;
    else       code = 100 + (a - 8);
    con_putc(27); con_putc('['); con_putn(code); con_putc('m');
}

static void con_sgr(int at)
{
    con_sgr_fg(at & 15);
    con_sgr_bg((at >> 4) & 15);
}

/* 把一行的**某一段**重新发一遍（改过影子之后用它刷新） */
static void con_redraw_range(int y, int x0, int x1)
{
    int x;
    int at;
    int last;

    if (y < 0 || y >= CON_ROWS) return;
    if (x0 < 0) x0 = 0;
    if (x1 > CON_COLS - 1) x1 = CON_COLS - 1;

    last = -1;
    for (x = x0; x <= x1; x++) {
        at = conAt[y * CON_COLS + x];
        if (at != last) { con_cup(x, y); con_sgr(at); last = at; }
        con_putc(conCh[y * CON_COLS + x]);
    }
}

static void con_redraw_all(void)
{
    int y;
    for (y = 0; y < CON_ROWS; y++) con_redraw_range(y, 0, CON_COLS - 1);
}

static void con_fill(int x0, int y0, int x1, int y1, int ch, int at)
{
    int x;
    int y;
    for (y = y0; y <= y1; y++)
        for (x = x0; x <= x1; x++) {
            conCh[y * CON_COLS + x] = (char)ch;
            conAt[y * CON_COLS + x] = (char)at;
        }
}

static void con_init(void)
{
    if (conReady != 0) return;
    conReady = 1;
    conFg = 7;                   /* = conio.h 的 LIGHTGRAY（DOS 定义的默认前景） */
    conBg = 0;                   /* = conio.h 的 BLACK */
    conX = 0;
    conY = 0;
    con_fill(0, 0, CON_COLS - 1, CON_ROWS - 1, ' ', (conBg << 4) | conFg);
}

/* ── 公开接口 ── */

void clrscr(void)
{
    con_init();
    con_putc(27); con_puts("[2J");      /* ED：清整屏 */
    con_sgr((conBg << 4) | conFg);
    con_cup(0, 0);
    con_fill(0, 0, CON_COLS - 1, CON_ROWS - 1, ' ', (conBg << 4) | conFg);
    conX = 0;
    conY = 0;
}

void clreol(void)
{
    con_init();
    con_cup(conX, conY);
    con_putc(27); con_puts("[K");       /* EL(0)：擦到行尾 */
    con_fill(conX, conY, CON_COLS - 1, conY, ' ', (conBg << 4) | conFg);
}

void gotoxy(int x, int y)
{
    con_init();
    conX = x - 1;
    conY = y - 1;
    if (conX < 0) conX = 0;
    if (conX > CON_COLS - 1) conX = CON_COLS - 1;
    if (conY < 0) conY = 0;
    if (conY > CON_ROWS - 1) conY = CON_ROWS - 1;
    con_cup(conX, conY);
}

int wherex(void) { return conX + 1; }
int wherey(void) { return conY + 1; }

void textcolor(int color) { con_init(); conFg = color & 15; }
void textbackground(int color) { con_init(); conBg = color & 15; }

void textattr(int attr)
{
    con_init();
    conFg = attr & 15;
    conBg = (attr >> 4) & 15;
}

void highvideo(void) { con_init(); conFg = conFg | 8; }
void lowvideo(void) { con_init(); conFg = conFg & 7; }
void normvideo(void) { con_init(); conFg = 7; conBg = 0; }   /* 7/0 = LIGHTGRAY / BLACK */

void putch(int c)
{
    int at;
    int idx;
    int y;
    int x;

    con_init();

    if (c == 10 || c == 13) {           /* 换行：DOS 的 \n 自带回车 */
        conX = 0;
        conY = conY + 1;
        if (conY >= CON_ROWS) {
            /* 滚一行：影子整体上移，重发整屏（真终端是硬件滚，这里只能重画） */
            for (y = 0; y < CON_ROWS - 1; y++)
                for (x = 0; x < CON_COLS; x++) {
                    conCh[y * CON_COLS + x] = conCh[(y + 1) * CON_COLS + x];
                    conAt[y * CON_COLS + x] = conAt[(y + 1) * CON_COLS + x];
                }
            con_fill(0, CON_ROWS - 1, CON_COLS - 1, CON_ROWS - 1, ' ', (conBg << 4) | conFg);
            conY = CON_ROWS - 1;
            con_redraw_all();
        }
        con_cup(conX, conY);
        return;
    }

    if (c == 8) {                        /* 退格：DOS 的回退**不擦**，只移光标 */
        if (conX > 0) conX = conX - 1;
        con_cup(conX, conY);
        return;
    }

    at = (conBg << 4) | conFg;
    idx = conY * CON_COLS + conX;
    conCh[idx] = (char)c;
    conAt[idx] = (char)at;
    con_sgr(at);
    con_putc(c);

    conX = conX + 1;
    if (conX >= CON_COLS) {
        conX = 0;
        conY = conY + 1;
        if (conY >= CON_ROWS) conY = CON_ROWS - 1;
        con_cup(conX, conY);
    }
}

void cputs(const char *s)
{
    int i;
    con_init();
    for (i = 0; s[i] != 0; i++) putch(s[i]);
}

void cprintf(const char *fmt, ...)
{
    /* 格式化**复用 printf 家族那份实现**（`format_arg_count` + `vsnprintf`），
       不另写一份 —— "同一规则两处实现"是本仓排第一的坑。
       变参走**标准内建**取，不自己算形参地址（ROADMAP 第零节的硬规矩）。 */
    char buf[512];
    int vals[16];
    int i;
    int nargs;
    int n;
    va_list ap;

    nargs = format_arg_count(fmt);
    if (nargs > 16) nargs = 16;

    va_start(ap, fmt);
    for (i = 0; i < nargs; i++) vals[i] = va_arg(ap, int);
    va_end(ap);

    n = vsnprintf(buf, fmt, vals, nargs);
    buf[n] = 0;
    cputs(buf);
}

void delline(void)
{
    int y;
    int x;
    con_init();
    for (y = conY; y < CON_ROWS - 1; y++)
        for (x = 0; x < CON_COLS; x++) {
            conCh[y * CON_COLS + x] = conCh[(y + 1) * CON_COLS + x];
            conAt[y * CON_COLS + x] = conAt[(y + 1) * CON_COLS + x];
        }
    con_fill(0, CON_ROWS - 1, CON_COLS - 1, CON_ROWS - 1, ' ', (conBg << 4) | conFg);
    for (y = conY; y < CON_ROWS; y++) con_redraw_range(y, 0, CON_COLS - 1);
    con_cup(conX, conY);
}

void insline(void)
{
    int y;
    int x;
    con_init();
    for (y = CON_ROWS - 1; y > conY; y--)
        for (x = 0; x < CON_COLS; x++) {
            conCh[y * CON_COLS + x] = conCh[(y - 1) * CON_COLS + x];
            conAt[y * CON_COLS + x] = conAt[(y - 1) * CON_COLS + x];
        }
    con_fill(0, conY, CON_COLS - 1, conY, ' ', (conBg << 4) | conFg);
    for (y = conY; y < CON_ROWS; y++) con_redraw_range(y, 0, CON_COLS - 1);
    con_cup(conX, conY);
}

/* ── 键盘 ──
 *
 * ⚠ **有限兼容**，写在明处：命令行页的 stdin 是**按行**交给程序的
 *   （用户在输入框敲一行、按「运行」提交），所以拿不到"单键即时"的语义。
 *   `getch()` 退回"读一行、逐字符吐出来" —— 老程序里 `ch = getch();` 那种写法
 *   仍然能跑（第一个字符就是这一次按键），但**方向键**要用户自己敲出来
 *   （菜单那样的程序，这一版只能靠 `w`/`s` 这类字符键）。
 *   要做到真单键 + 方向键，得让命令行页把每一下按键**原样转发**给程序（待办）。
 */
static char conKb[128];
static int  conKbLen;
static int  conKbPos;

/* ── 绘图窗口那条输入路（BGI 老程序靠它活着）──────────────────
 *
 * ## 为什么必须有这一条
 *
 * BGI 老程序**必然同时 include `<graphics.h>` 与 `<conio.h>`**（Turbo C 里 `getch`
 * 本来就归 conio，BGI 只管画），主循环写成 `ch = getch();` 靠**单键**控制游戏。
 * 而上面那条"按行读 stdin"的路在图形窗口下**永远等不到** —— 窗口的按键走的是
 * `ui_wait` 消息队列，stdin 那边一个字节都没有 ⇒ **游戏一按键就卡死**
 * （实测：键盘游戏在窗口里按下方向键，程序一动不动，最后被超时杀掉）。
 *
 * ⚠ 而且这里**必须**是唯一实现：`graphics.h` 里曾另有一份读窗口消息的 `getch`，
 *   两份同名同时存在时**链接器会把 `call getch` 重定向到库里这一份**
 *   （即已知红 `28-name-shadowed-by-lib` 那个"库函数静默劫持"的病）
 *   ⇒ 那份从来没生效过。现在 `graphics.h` 的那份**已删**，只留这里一份。
 *
 * ## 判据：`ui_win_closed()` 的**三值**
 *
 *   0 = 窗口开着（**走消息**）／1 = 被用户关掉／**2 = 从来没开过窗口**（走按行 stdin）。
 *   只有 0/1 两档的话，"从没开过窗口"与"窗口正开着"都报 0、分不出来 ——
 *   控制台程序就会被误判成图形程序，`getch` 于是去等一个永远不来的消息。
 */
static int _con_win_state(void)
{
    return (int)asm("SYSCALL #565");     /* ui_win_closed()：0 开着 / 1 关了 / 2 没开过 */
}

/* DOS 那个"两段式"的后半段（扩展键先返回 0、下一次返回扫描码）。 */
static int _con_pending_scan = 0;

/* Win32 虚拟键 → DOS 扫描码。只列**扩展键**；普通键的虚拟键码就等于 ASCII，
 * 不走这条路。 */
static int _con_scan_code(int vk)
{
    if (vk == 0x25) return 0x4B;              /* ← */
    if (vk == 0x26) return 0x48;              /* ↑ */
    if (vk == 0x27) return 0x4D;              /* → */
    if (vk == 0x28) return 0x50;              /* ↓ */
    if (vk == 0x24) return 0x47;              /* Home */
    if (vk == 0x23) return 0x4F;              /* End  */
    if (vk == 0x21) return 0x49;              /* PgUp */
    if (vk == 0x22) return 0x51;              /* PgDn */
    if (vk == 0x2D) return 0x52;              /* Ins  */
    if (vk == 0x2E) return 0x53;              /* Del  */
    if (vk >= 0x70 && vk <= 0x79) return 0x3B + (vk - 0x70);   /* F1–F10 */
    if (vk == 0x7A) return 0x57;              /* F11 */
    if (vk == 0x7B) return 0x58;              /* F12 */
    return 0;
}

/* 等一个**窗口按键**。返回 0 = 窗口关了（与原来的契约一致）。 */
static int _con_getch_window(void)
{
    int buf[4];
    int* msg = buf;          /* ⚠ 必须用**指针变量**：`asm` 里的 `${msg}` 展开的是
                                "这个标识符的值" —— 数组名给的是元素而不是地址
                                （实测：写成 `int msg[4]` 直接传，`ui_wait` 收到的是垃圾地址，
                                 消息永远等不来 ⇒ getch 卡死）。`vmlui.c` 里那份能跑，
                                正是因为它的形参本来就是 `int*`。 */
    int vk;
    int sc;

    if (_con_pending_scan != 0)
    {
        sc = _con_pending_scan;
        _con_pending_scan = 0;
        return sc;
    }

    for (;;)
    {
        /* #561 = ui_wait(msg, 0)：0 是**无限等**，不是不阻塞。
         *
         * ⚠⚠ **超时必须用一条字面 `MOVE` 显式写进 R1 —— 不能写成 `${0}`。**
         *   asm 模板的 `${...}` 只做**变量替换**（`${msg}` → msg 所在的寄存器），
         *   **字面量根本不认**：`${0}` 在展开时**一行都不生成**（见
         *   `CodeGenerator.Statements.cs` 的 `else searchStart = braceEnd + 1;`），
         *   于是 `syscall #561` 前面只有 `move @R0, [msg]`。
         *   后果不是"用不上默认值"，而是 **R1 保留上一次留下的值**。
         *
         *   实测（真机 + 桌面，同一个根因）：按一下键之后 R1 残留着**上一条消息的类型**
         *   （KeyUp = 2）⇒ 下一次 `ui_wait` 的"无限等"变成 **2 毫秒**、立刻返回 0
         *   ⇒ `getch()` **凭空吐一个 0**。而老程序对 0 的约定是"扩展键前缀"
         *   （`if (ch == 0) ch = getch();`）⇒ 它把**下一个真按键当成扫描码吃掉**。
         *   用户看到的现象就是「按了没反应 / 输入无效」。
         *
         *   设备上更早中招：开窗那一刻宿主先发 `WindowResize`(11) —— 那一条就让 R1 = 11。
         *   桌面看不出来只是因为桌面脚手架不投那条消息（第一拍 R1 恰好还是 0）。
         *
         *   同族错法在 `io.c` 的 `getchar`（那边是 `R0`）已经修过一次，修法一样。 */
        asm("MOVE R1 #0");
        if ((int)asm("SYSCALL #561, ${msg}") <= 0) return 0;
        /* ⚠ 只认按下：KeyUp / 触摸 / 尺寸变化一律跳过 ——
           返回 0 会被老程序当成"窗口没了"，返回别的又会把"按一下"变成"两下"。 */
        if (msg[0] == 1) break;              /* VML_MSG_KEYDOWN */
    }

    vk = msg[1];
    sc = _con_scan_code(vk);
    if (sc != 0)
    {
        _con_pending_scan = sc;              /* 扩展键：先给 0，下次给扫描码 */
        return 0;
    }
    return vk;                               /* 普通键：虚拟键码 == ASCII */
}

int getch(void)
{
    int c;
    if (conKbPos < conKbLen) { c = conKb[conKbPos]; conKbPos++; return c; }

    /* ⚠ 有绘图窗口就走窗口消息（见上面那一段）——
       少了这一句，所有 BGI 游戏的按键都是死的。 */
    if (_con_win_state() == 0) return _con_getch_window();

    conKbLen = 0;
    conKbPos = 0;
    while (conKbLen < 126) {
        c = getchar();
        if (c <= 0 || c == 10 || c == 13) break;
        conKb[conKbLen] = (char)c;
        conKbLen++;
    }
    conKbPos = 0;
    if (conKbLen == 0) return 13;       /* 空行 = 回车（DOS 里最常见的"确认"） */
    c = conKb[conKbPos];
    conKbPos++;
    return c;
}

int getche(void)
{
    int c;
    c = getch();
    if (c != 0) putch(c);
    return c;
}

/* `kbhit()` —— "有没有按键（**不取走**）"。
 *
 * ## 这条路线踩过的坑：症状是"动画程序一帧都不画"
 *
 * 旧实现只查**自己的行缓冲**，空缓冲就返回 0，还留着一句注释说
 * "按行的 stdin 无法预知有没有按键" —— **那句是错的**：宿主侧本来就有
 * `ISystemCallHandler.KeyAvailable()`，而且 `SYSCALL #5` 的 `R0=0` 模式
 * 就是"非阻塞地取一个字符"（没键返回 0，见 `SyscallNumber` 附近的注释）。
 *
 * 后果不是"kbhit 偶尔不准"，而是**一整类动画老程序跑不起来**：
 * `cmatrix` / `tty-clock` 的主循环都写成
 *     timeout(0);   …   if ((ch = wgetch(stdscr)) != ERR) {…}
 * —— 靠"kbhit 说没键"来推进每一帧。旧实现里它恒为 0，于是 `getch`
 * 每次都**阻塞读一整行**，第一帧都画不出来就卡在那儿（实测：跑满 38 秒
 * 被超时杀掉，屏幕输出只有 `initscr` 那一句清屏）。
 *
 * ## 实现：**非阻塞探一下，取到就存进缓冲**
 *
 * 不能直接把 `SYSCALL #5` 的结果返回 —— 那会**把字符取走**，而 kbhit 的
 * 契约是"看一眼、不动它"。所以取到就压进 `conKb`（`getch` 接着从那里拿），
 * 缓冲的语义从"一整行"放宽成"待处理的字符"。
 *
 * ⚠ 与 `getchar()` 的配合：那边**必须显式 `R0=1`**，否则会继承这里的
 * `R0=0` 退化成非阻塞（见 `io.c` 里 getchar 的说明）。两处是一对。 */
int kbhit(void)
{
    int c;
    int buf[4];
    int* msg = buf;          /* 同上：指针变量，不是数组名 */
    if (conKbPos < conKbLen) return 1;

    /* ⚠ 有绘图窗口时看**窗口消息队列**（与 `getch` 同一条路，否则图形程序里
       `getch` 等窗口、`kbhit` 看 stdin，两边永远对不上）。 */
    if (_con_win_state() == 0)
    {
        /* 队头**是**按键 ⇒ "有按键"，而且**不取走**（#571 = ui_poll_ex + KEEP，
           kbhit 的契约就是"只看一眼、不动它"）。
           ⚠ 队头**不是**按键时（KeyUp / 触摸 / 尺寸变化…）必须**吃掉它**再往下看 ——
           只用"看一眼"的话，那条非按键消息会**一直堵在队头**，它后面的按键
           永远轮不到 ⇒ 症状是"按了没反应，按第二下才行"。 */
        for (;;)
        {
            /* R1=1 = `VmlUi.Keep`（只看队头、不取走）。⚠ 同样是**字面 `MOVE`**：
               `${1}` 不生成任何东西 ⇒ R1 残留上一个值，而 R1≠Keep 时宿主会把那条
               消息**取走**（`_queue.Read(timeout, keep: false, …)`）——
               `kbhit` 的契约却是"只看一眼、不动它"，于是它会把定时器/鼠标消息吃掉。 */
            asm("MOVE R1 #1");
            if ((int)asm("SYSCALL #571, ${msg}") <= 0) return 0;   /* 队列空 */
            if (msg[0] == 1) return 1;                                   /* 队头就是按键 */
            asm("SYSCALL #560, ${msg}");                                 /* ui_poll：消费掉 */
        }
    }

    /* R0=0 ⇒ 非阻塞；没键返回 0。⚠ **必须字面 `MOVE`**：写成 `${0}` 是同一个错法
       （那边靠 R0 残留值"碰巧"非阻塞，而不是靠这行 asm）—— `io.c` 的 `getchar` 已修。 */
    asm("MOVE R0 #0");
    c = asm("SYSCALL #5");
    if (c <= 0) return 0;
    conKb[0] = (char)c;
    conKbLen = 1;
    conKbPos = 0;
    return 1;
}
