/* tty.c —— **彩色命令行**的统一接口层（给 22 门语言共用）
 *
 * ## 一句话
 *
 * 在**字符网格**上定位、上色、写字。**两块屏**：
 *   · **主屏**（默认）—— 就是命令行页那个 80×25 网格，只发 ANSI 字节；
 *   · **副屏**（`tty_alt_open`）—— **弹一扇窗口**当终端，自己画字符网格。
 * 两块屏**同一套 API**（`tty_goto`/`tty_puts`/`tty_color`/…），内部按当前屏路由。
 *
 * ## 为什么主屏不自己实现（本仓排第一的坑）
 *
 * 主屏的引擎是 **`conio.c`**，本文件只转发。那边已经解决了这条路上所有难的部分 ——
 * 尤其是**影子缓冲**：`delline`/`insline`/`clreol` 要改动已画过的内容，而 ANSI 的
 * `ESC[M`/`ESC[L`(DL/IL) 在本平台网格上**不支持**（`FrameBuffer` 只认
 * CUP/CUU/CUD/CUF/CUB/CHA/VPA/存光标/EL/ED/SGR）⇒ 那边留了一份屏幕影子、
 * 改完把受影响的行重发一遍。在这里再写一遍 = 两份实现漂移，症状是
 * "`conio` 画的框对、`tty` 画的不对"这种最难查的东西。
 *
 * 那为什么还要这一层：**各语言要一个统一、好记的名字** —— `gotoxy`/`textattr`/`cputs`
 * 是 Turbo C 的历史包袱，而且每门语言都得自己写一份声明才能调。`tty_*` 是这套接口的
 * 正式名字，配套 `Lib/<语言>/tty.vml` 绑定，于是 22 门语言写出来的 `demo_tty` 是
 * **同一串调用**。
 *
 * ## 坐标与颜色（**1 起算**，与全平台一致）
 *
 * · 坐标 **1-based**：`(1,1)` 左上角 —— 与 `GotoXY`(Pascal) / `gotoxy`(Turbo C) /
 *   `LOCATE`(BASIC) 一致。**不是** 0-based，写惯 0-based 的人在这里最容易差一格。
 * · 颜色 **0–15 索引色**（CGA/VGA 标准表，与 `graph.pas`/`crt.pas`/BGI 同表）：
 *   `0 黑 1 蓝 2 绿 3 青 4 红 5 品红 6 棕 7 浅灰 8 深灰 9 亮蓝 10 亮绿
 *    11 亮青 12 亮红 13 亮洋红 14 黄 15 白`
 *
 * ## 物理尺寸
 *
 * 主屏的物理网格在手机上**固定 80×25**（命令行页的字符网格，`conio` 的影子也是这个尺寸）；
 * 副屏则**由 `tty_alt_open` 的 w/h 决定**（真开窗，格子 8×16 px ⇒ 80×25 = 640×400）。
 * `tty_init`/`tty_alt_open` 传 `0,0` 一律取 80×25。
 *
 * ## 出口
 *
 * 主屏一个字符一个字符走 `putch`（`conio` 的规矩，`SYSCALL #4`）——
 * 于是**桌面 vmlcli 的真终端**与**手机命令行页**走同一条渲染链。
 * 副屏走宿主 `ui_*` 图元（`ui_rect` 铺底 + `ui_text_v` 画字），与绘图窗口同一套。
 */

#param lib("conio")
#param lib("vmlui")

/* ── 主屏引擎（`Lib/shared/src/conio.c`）—— 只转发，不重新实现 ──────── */
extern void gotoxy(int x, int y);
extern int  wherex(void);
extern int  wherey(void);
extern void textattr(int attr);
extern void clrscr(void);
extern void clreol(void);
extern void cputs(const char *s);
extern void putch(int c);
extern int  kbhit(void);
extern int  getch(void);

/* ── 副屏引擎（`Lib/shared/src/vmlui.c`）──────────────────────────── */
extern int  ui_win_open(char *title, int w, int h);
extern int  ui_win_close(void);
extern int  ui_win_closed(void);
extern void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
extern void ui_text_v(int x, int y, char *s, int color, int size, int anchor, int valign, int style);
extern void ui_present(void);
extern int  ui_poll(int *msg);
extern int  ui_wait(int *msg, int timeout_ms);
extern int  ui_msg_type(void);

/* 宿主常量（`Lib/c/waycoder_ui.h`，这里只抄需要的几个） */
#define TTY_MSG_KEYDOWN     1
#define TTY_MSG_KEYUP       2
#define TTY_MSG_WINDOWCLOSE 10
#define TTY_ANCHOR_LEFT     0
#define TTY_VANCHOR_TOP     3

/* ── 几何 ─────────────────────────────────────────────────────────
   格子的像素尺寸。**主屏不用它**（那边由命令行页的网格定），副屏用它算窗口大小。 */
#define TTY_CW 8
#define TTY_CH 16
#define TTY_FONT 13
#define TTY_MAXC 80
#define TTY_MAXR 25

/* ── 状态 ───────────────────────────────────────────────────────
   ⚠ 主屏的光标/颜色/影子**全在 `conio` 那边**，这里不再存一份
   （存了就要同步，同步就会漂）—— 下面这几个只在**副屏**下有效。 */
static int  _tty_w = 0;          /* 逻辑列数 */
static int  _tty_h = 0;          /* 逻辑行数 */
static int  _tty_alt = 0;        /* 0 = 主屏，1 = 副屏 */
static int  _tty_cx = 1;         /* 副屏光标（1-based） */
static int  _tty_cy = 1;
static int  _tty_fg = 7;         /* 副屏当前前景/背景（索引 0-15） */
static int  _tty_bg = 0;
static int  _tty_cell[TTY_MAXC * TTY_MAXR];   /* 每格 ch | fg<<8 | bg<<12 */
static char _tty_row[TTY_MAXC + 8];           /* 一行文字的重建缓冲 */
static int  _tty_msg[4];

/* CGA/VGA 16 色 → 平台色（**0xAARRGGBB**，alpha 必须补 0xFF）。
   ⚠ 与 `Lib/c/graphics.h` 的 `_bgi_pal`、`graph.pas`、`crt.pas` 必须是**同一张表**。
      这里是第 N 份副本，只是因为它们是 static（各自编进各自的模块）。
      **改一处就要改全部** —— 本仓记过"同一份清单两处实现"的教训。 */
static int _tty_pal[16] = {
    0xFF000000, 0xFF0000AA, 0xFF00AA00, 0xFF00AAAA,
    0xFFAA0000, 0xFFAA00AA, 0xFFAA5500, 0xFFAAAAAA,
    0xFF555555, 0xFF5555FF, 0xFF55FF55, 0xFF55FFFF,
    0xFFFF5555, 0xFFFF55FF, 0xFFFFFF55, 0xFFFFFFFF
};

/* ── 内部：主屏 ───────────────────────────────────────────────── */

static void _tty_main_attr(void) { textattr(((_tty_bg & 15) << 4) | (_tty_fg & 15)); }

/* ── 内部：自适应尺寸（副屏开窗用）────────────────────────────────
   问宿主"可画区有多大"再除以格子尺寸。拿不到（返回 0/负数，或纯控制台环境）
   就退回 80×25 —— **不要因为宿主不支持就开出一扇 0×0 的窗**。 */
extern int ui_scr_w(void);
extern int ui_scr_h(void);

static int _tty_auto_cols(void) {
    int c = ui_scr_w() / TTY_CW;
    if (c <= 0) c = TTY_MAXC;
    if (c > TTY_MAXC) c = TTY_MAXC;
    return c;
}

static int _tty_auto_rows(void) {
    int r = ui_scr_h() / TTY_CH;
    if (r <= 0) r = TTY_MAXR;
    if (r > TTY_MAXR) r = TTY_MAXR;
    return r;
}

/* ── 内部：副屏 ───────────────────────────────────────────────── */

static int _tty_idx(int x, int y) { return (y - 1) * TTY_MAXC + (x - 1); }

static void _tty_alt_clear(void) {
    int i;
    for (i = 0; i < TTY_MAXC * TTY_MAXR; i++)
        _tty_cell[i] = 32 | (7 << 8) | (0 << 12);   /* 空格 + 浅灰/黑 */
    _tty_cx = 1;
    _tty_cy = 1;
}

static void _tty_alt_putc(int ch) {
    if (ch == 10) {                                  /* '\n' */
        _tty_cx = 1;
        _tty_cy = _tty_cy + 1;
    } else if (ch == 13) {                           /* '\r' */
        _tty_cx = 1;
    } else if (ch == 9) {                            /* '\t' → 下一个 8 的倍数 */
        _tty_cx = ((_tty_cx - 1) / 8 + 1) * 8 + 1;
    } else {
        _tty_cell[_tty_idx(_tty_cx, _tty_cy)] = (ch & 255) | (_tty_fg << 8) | (_tty_bg << 12);
        _tty_cx = _tty_cx + 1;
    }
    if (_tty_cx > _tty_w) { _tty_cx = 1; _tty_cy = _tty_cy + 1; }
    if (_tty_cy > _tty_h) {                          /* 滚屏：整体上移一行 */
        int x, y;
        for (y = 1; y < _tty_h; y++)
            for (x = 1; x <= _tty_w; x++)
                _tty_cell[_tty_idx(x, y)] = _tty_cell[_tty_idx(x, y + 1)];
        for (x = 1; x <= _tty_w; x++)
            _tty_cell[_tty_idx(x, _tty_h)] = 32 | (_tty_fg << 8) | (_tty_bg << 12);
        _tty_cy = _tty_h;
    }
}

/* 把一行的背景铺出来：连续同色段各发一个矩形。 */
static void _tty_alt_bg_row(int y) {
    int x, run, bg;
    x = 1;
    while (x <= _tty_w) {
        bg = (_tty_cell[_tty_idx(x, y)] >> 12) & 15;
        run = 1;
        while (x + run <= _tty_w && (((_tty_cell[_tty_idx(x + run, y)] >> 12) & 15) == bg))
            run = run + 1;
        ui_rect((x - 1) * TTY_CW, (y - 1) * TTY_CH, run * TTY_CW, TTY_CH,
                _tty_pal[bg & 15], 1, 0, 0);
        x = x + run;
    }
}

/* 把一行的文字画出来：连续同前景、且非空白的段拼成一句再发。 */
static void _tty_alt_fg_row(int y) {
    int x, n, base, fg, ch;
    x = 1;
    while (x <= _tty_w) {
        ch = _tty_cell[_tty_idx(x, y)] & 255;
        if (ch == 32) { x = x + 1; continue; }
        fg = (_tty_cell[_tty_idx(x, y)] >> 8) & 15;
        base = x;
        n = 0;
        while (x <= _tty_w && n < TTY_MAXC) {
            ch = _tty_cell[_tty_idx(x, y)] & 255;
            if (ch == 32) break;
            if (((_tty_cell[_tty_idx(x, y)] >> 8) & 15) != fg) break;
            _tty_row[n] = (char)ch;
            n = n + 1;
            x = x + 1;
        }
        _tty_row[n] = 0;
        ui_text_v((base - 1) * TTY_CW, (y - 1) * TTY_CH, _tty_row,
                  _tty_pal[fg & 15], TTY_FONT, TTY_ANCHOR_LEFT, TTY_VANCHOR_TOP, 0);
    }
}

/* 把副屏的网格刷到那扇窗口上（**只画，不阻塞**）。 */
void tty_refresh(void) {
    int y;
    if (_tty_alt == 0) return;
    for (y = 1; y <= _tty_h; y++) _tty_alt_bg_row(y);
    for (y = 1; y <= _tty_h; y++) _tty_alt_fg_row(y);
    ui_present();
}

/* ── 初始化 / 副屏开关 ─────────────────────────────────────────── */

/* 初始化**主屏**（命令行页）。
 *
 * · `tty_init(0, 0, 0)` —— **最常用的那一句**：尺寸**自适应**（不指定大小）、**不清屏**。
 *   "自适应"在这里的意思是**不强行改尺寸**：主屏就用平台的字符网格（手机 80×25），
 *   副屏已经开着的话就沿用那扇窗口的行列数。
 * · 给了正数则按它钳制（上限 80×25）；`clear` 非 0 则清屏。
 *
 * ⚠ 会**先复位颜色为"浅灰/黑"** —— 否则上一位调用者留下的属性会带进来。 */
int tty_init(int width, int height, int clear) {
    if (width > 0 || height > 0) {
        if (width <= 0)  width = _tty_w > 0 ? _tty_w : TTY_MAXC;
        if (height <= 0) height = _tty_h > 0 ? _tty_h : TTY_MAXR;
        if (width > TTY_MAXC)  width = TTY_MAXC;
        if (height > TTY_MAXR) height = TTY_MAXR;
        _tty_w = width;
        _tty_h = height;
    }
    /* `w <= 0 && h <= 0` ⇒ 自适应：**不动** `_tty_w/_tty_h`（主屏初值就是 80×25） */
    if (_tty_w <= 0) _tty_w = TTY_MAXC;
    if (_tty_h <= 0) _tty_h = TTY_MAXR;
    _tty_alt = 0;
    _tty_fg = 7;
    _tty_bg = 0;
    _tty_main_attr();
    if (clear != 0) clrscr();
    return 0;
}

/* 打开**副屏**：弹一扇 (w × h) 个字符格的窗口当终端。
 *
 * · `tty_alt_open(0, 0, "标题", 0)` —— **自适应窗口**：不指定大小，按宿主给的可画区
 *   算能放几行几列（`ui_scr_w/h ÷ 格子`，上限 80×25），于是"手机上开出来就是满屏"。
 * · `title` 是窗口标题（UTF-8）。
 * · 副屏**开出来必然是空白的**（那是一扇新窗口）⇒ `clear` 参数在这里**不起作用**，
 *   留着只是为了与 `tty_init` 的签名对齐。
 *
 * 返回 0 成功、-1 打不开（宿主不支持开窗，例如纯控制台环境）—— **失败就别当副屏用**。 */
int tty_alt_open(int width, int height, char *title, int clear) {
    int rc;
    if (width <= 0)  width = _tty_auto_cols();
    if (height <= 0) height = _tty_auto_rows();
    if (width > TTY_MAXC)  width = TTY_MAXC;
    if (height > TTY_MAXR) height = TTY_MAXR;
    rc = ui_win_open(title, width * TTY_CW, height * TTY_CH);
    if (rc < 0) return -1;
    _tty_w = width;
    _tty_h = height;
    _tty_alt = 1;
    _tty_fg = 7;
    _tty_bg = 0;
    _tty_alt_clear();
    tty_refresh();
    return 0;
}

/* 关副屏、回到主屏。属性复位成"浅灰/黑"。 */
void tty_alt_close(void) {
    if (_tty_alt == 0) return;
    ui_win_close();
    _tty_alt = 0;
    _tty_w = TTY_MAXC;
    _tty_h = TTY_MAXR;
    _tty_fg = 7;
    _tty_bg = 0;
    _tty_main_attr();
}

int tty_alt_is_open(void) { return _tty_alt; }
int tty_width(void)  { return _tty_w > 0 ? _tty_w : TTY_MAXC; }
int tty_height(void) { return _tty_h > 0 ? _tty_h : TTY_MAXR; }

/* ── 清屏 / 擦行 ──────────────────────────────────────────────── */

void tty_cls(void) {
    if (_tty_alt) { _tty_alt_clear(); tty_refresh(); return; }
    clrscr();
}

/* 擦到本行行尾（主屏走 `ESC[K`；副屏把本行余下的格子填空格）。 */
void tty_clreol(void) {
    int x;
    if (_tty_alt == 0) { clreol(); return; }
    for (x = _tty_cx; x <= _tty_w; x++)
        _tty_cell[_tty_idx(x, _tty_cy)] = 32 | (_tty_fg << 8) | (_tty_bg << 12);
    tty_refresh();
}

/* ── 光标 ─────────────────────────────────────────────────────── */

/* 定位到 (x, y)，**1 起算**。越界钳进逻辑尺寸内。 */
void tty_goto(int x, int y) {
    if (x < 1) x = 1;
    if (y < 1) y = 1;
    if (x > tty_width())  x = tty_width();
    if (y > tty_height()) y = tty_height();
    if (_tty_alt) { _tty_cx = x; _tty_cy = y; return; }
    gotoxy(x, y);
}

int tty_wherex(void) { return _tty_alt ? _tty_cx : wherex(); }
int tty_wherey(void) { return _tty_alt ? _tty_cy : wherey(); }

/* ── 颜色 ─────────────────────────────────────────────────────── */

/* 设定后续输出的前景/背景色（各 0–15）。 */
void tty_color(int fg, int bg) {
    _tty_fg = fg & 15;
    _tty_bg = bg & 15;
    if (_tty_alt == 0) _tty_main_attr();
}

/* 直接给 DOS 属性字节（`bg<<4 | fg`）—— 老程序里 `TextAttr` 的写法。 */
void tty_attr(int attr) { tty_color(attr & 15, (attr >> 4) & 15); }

int tty_getfg(void) { return _tty_fg; }
int tty_getbg(void) { return _tty_bg; }

/* ── 输出 ─────────────────────────────────────────────────────── */

void tty_putc(int ch) {
    if (_tty_alt) { _tty_alt_putc(ch & 255); return; }
    putch(ch & 255);
}

void tty_puts(char *s) {
    if (_tty_alt == 0) { cputs(s); return; }
    while (*s != 0) {
        _tty_alt_putc((int)(*s) & 255);
        s = s + 1;
    }
}

/* 按十进制打一个整数（`tty_puts` 只吃字符串，而"数字转字符串"各语言各不相同，
   放在这里一份就够）。负数带 '-'。 */
void tty_put_int(int v) {
    char buf[16];
    int n, neg;
    unsigned u;
    neg = 0;
    if (v < 0) { neg = 1; u = (unsigned)(0 - v); } else { u = (unsigned)v; }
    n = 0;
    if (u == 0) { buf[n] = 48; n = n + 1; }
    while (u > 0) {
        buf[n] = (char)(48 + (int)(u % 10));
        n = n + 1;
        u = u / 10;
    }
    if (neg) { buf[n] = 45; n = n + 1; }
    while (n > 0) { n = n - 1; tty_putc(buf[n] & 255); }
}

/* 在 (x, y) 用指定颜色打一行字，**打完恢复** (7,0)。 */
void tty_print_at(int x, int y, char *s, int fg, int bg) {
    tty_goto(x, y);
    tty_color(fg, bg);
    tty_puts(s);
    tty_color(7, 0);
}

/* ── 便利图元（都是"发字符"，不是绘图）───────────────────────── */

void tty_hline(int x, int y, int len, int ch) {
    int i;
    tty_goto(x, y);
    for (i = 0; i < len; i++) tty_putc(ch);
}

void tty_vline(int x, int y, int len, int ch) {
    int i;
    for (i = 0; i < len; i++) { tty_goto(x, y + i); tty_putc(ch); }
}

/* 边框。style: 0 = ASCII（`+ - |`），1 = 单线框（UTF-8 `┌ ─ │ ┐ └ ┘`）。
   ⚠ UTF-8 那三个字节按**字节**发（本层的出口就是字节），主屏副屏都一样。 */
static void _tty_boxchar(int style, int which) {
    /* which: 0=左上 1=右上 2=左下 3=右下 4=横 5=竖 */
    if (style == 0) {
        if (which == 4) tty_putc(45);                            /* '-' */
        else if (which == 5) tty_putc(124);                      /* '|' */
        else tty_putc(43);                                       /* '+' */
        return;
    }
    tty_putc(226);                                               /* 0xE2 */
    if (which == 0)      { tty_putc(148); tty_putc(140); }       /* ┌ */
    else if (which == 1) { tty_putc(148); tty_putc(144); }       /* ┐ */
    else if (which == 2) { tty_putc(148); tty_putc(148); }       /* └ */
    else if (which == 3) { tty_putc(148); tty_putc(152); }       /* ┘ */
    else if (which == 4) { tty_putc(148); tty_putc(128); }       /* ─ */
    else                 { tty_putc(148); tty_putc(130); }       /* │ */
}

void tty_box(int x1, int y1, int x2, int y2, int style) {
    int x, y, t;
    if (x2 < x1) { t = x1; x1 = x2; x2 = t; }
    if (y2 < y1) { t = y1; y1 = y2; y2 = t; }
    tty_goto(x1, y1); _tty_boxchar(style, 0);
    for (x = x1 + 1; x < x2; x++) _tty_boxchar(style, 4);
    tty_goto(x2, y1); _tty_boxchar(style, 1);
    tty_goto(x1, y2); _tty_boxchar(style, 2);
    for (x = x1 + 1; x < x2; x++) _tty_boxchar(style, 4);
    tty_goto(x2, y2); _tty_boxchar(style, 3);
    for (y = y1 + 1; y < y2; y++) {
        tty_goto(x1, y); _tty_boxchar(style, 5);
        tty_goto(x2, y); _tty_boxchar(style, 5);
    }
}

/* ── 输入 ─────────────────────────────────────────────────────── */

/* 有按键就返回它的编码，没有返回 -1（**不阻塞**）。窗口被关掉时返回 -1。 */
int tty_key(void) {
    int t;
    if (_tty_alt == 0) { if (kbhit()) return getch(); return -1; }
    if (ui_win_closed() != 0) return -1;
    t = ui_poll(_tty_msg);
    if (t == TTY_MSG_KEYDOWN) return _tty_msg[1];
    return -1;
}

/* 等一个按键（**阻塞**）。副屏在等之前会先把网格刷上屏。
   ⚠ 返回 -1 表示"窗口关了"（主屏不会返 -1）。 */
int tty_wait(void) {
    int t;
    if (_tty_alt == 0) return getch();
    tty_refresh();
    for (;;) {
        if (ui_win_closed() != 0) return -1;
        t = ui_wait(_tty_msg, 200);
        if (t == TTY_MSG_KEYDOWN) return _tty_msg[1];
        if (t == TTY_MSG_WINDOWCLOSE) return -1;
    }
}

/* 副屏窗口是不是被用户关掉了（主屏恒 0）。 */
int tty_closed(void) { return _tty_alt ? ui_win_closed() : 0; }
