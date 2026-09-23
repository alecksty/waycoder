/* ANSI CRT 库 — TTY 终端彩色输出 (v1.66.48+)
 * 替换 VGA 显存写入方案，使用 ANSI escape codes
 * 所有字符使用十进制 ASCII 码，避免 VML C 编译器字符解析问题
 *
 * ASCII 速查: ESC=27 [=91 ;=59 H=72 J=74 K=75 m=109 0=48 1=49 2=50
 */

/* ── 全局状态 ── */
int _crt_x, _crt_y, _crt_fg, _crt_bg;
int _crt_wx1, _crt_wy1, _crt_wx2, _crt_wy2;
int _crt_tmp1, _crt_tmp2;

/* ANSI 前景色映射: VML→ANSI */
int _ansi_fg[8];
/* ANSI 背景色映射: VML→ANSI */
int _ansi_bg[8];
/* 临时缓冲区 */
char _crt_buf[32];

/* ── 初始化 ── */
void _crt_init_maps() {
    _ansi_fg[0]=30; _ansi_bg[0]=40;  // Black
    _ansi_fg[1]=34; _ansi_bg[1]=44;  // Blue
    _ansi_fg[2]=32; _ansi_bg[2]=42;  // Green
    _ansi_fg[3]=36; _ansi_bg[3]=46;  // Cyan
    _ansi_fg[4]=31; _ansi_bg[4]=41;  // Red
    _ansi_fg[5]=35; _ansi_bg[5]=45;  // Magenta
    _ansi_fg[6]=33; _ansi_bg[6]=43;  // Brown/Yellow
    _ansi_fg[7]=37; _ansi_bg[7]=47;  // LightGray/White
}

/* ══════════════════════════════════════
 * 核心 CRT 函数 — 每个函数独立导出
 * ══════════════════════════════════════ */

void CRT_INIT();
void CRT_INIT() {
    _crt_init_maps();
    _crt_fg = 7; _crt_bg = 0;
    _crt_wx1 = 1; _crt_wy1 = 1;
    _crt_wx2 = 80; _crt_wy2 = 25;
    _crt_x = 1; _crt_y = 1;
    CRT_CLRSCR();
}

void CRT_CLRSCR();
void CRT_CLRSCR() {
    /* 自动初始化 (兼容未调用 CRT_INIT 的情况) */
    if (_crt_wx2 == 0) {
        _crt_init_maps();
        _crt_fg = 7; _crt_bg = 0;
        _crt_wx1 = 1; _crt_wy1 = 1;
        _crt_wx2 = 80; _crt_wy2 = 25;
    }
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #50"); asm("SYSCALL 400");
    asm("MOVE R0, #74"); asm("SYSCALL 400");
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #72"); asm("SYSCALL 400");
    _crt_x = 1; _crt_y = 1;
}

void CRT_CLREOL();
void CRT_CLREOL() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #75"); asm("SYSCALL 400");
}

void CRT_GOTOXY(int x, int y);
void CRT_GOTOXY(int x, int y) {
    /* 关键: 在 SYSCALL 前保存参数到全局变量 */
    asm("MOVE [_crt_tmp1], R0");  /* 保存 x */
    asm("MOVE [_crt_tmp2], R1");  /* 保存 y */

    /* 限制在窗口范围内 */
    if (_crt_tmp1 < _crt_wx1) _crt_tmp1 = _crt_wx1;
    if (_crt_tmp1 > _crt_wx2) _crt_tmp1 = _crt_wx2;
    if (_crt_tmp2 < _crt_wy1) _crt_tmp2 = _crt_wy1;
    if (_crt_tmp2 > _crt_wy2) _crt_tmp2 = _crt_wy2;

    _crt_x = _crt_tmp1; _crt_y = _crt_tmp2;

    /* ANSI 输出 — 使用保存的全局变量值 */
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, [_crt_tmp2]"); asm("SYSCALL 402");  /* y */
    asm("MOVE R0, #59"); asm("SYSCALL 400");
    asm("MOVE R0, [_crt_tmp1]"); asm("SYSCALL 402");  /* x */
    asm("MOVE R0, #72"); asm("SYSCALL 400");
}

int CRT_WHEREX();  int CRT_WHEREX()  { return _crt_x; }
int CRT_WHEREY();  int CRT_WHEREY()  { return _crt_y; }

/* ⚠ 这几个 CRT_* 是**寄存器传参**入口（BASIC 的 EmitCallBuiltin 只把实参放进 R0-R3），
   所以**绝不可给形参赋值**：C 编译器把「形参赋值」编成对 `[R12+12+4i]` 的写，而
   寄存器传参的调用方**没有分配那个实参槽** —— 写下去就落到调用方的栈上。
   实测（GORILLA.BAS）：CRT_TEXTCOLOR 把 `c = 7` 写成调用方保存的 BP，
   于是调用方 `leave` 后 R12=7，下一条 `[R12-12]` 立刻内存错误。
   一律用显式局部量承接。 */
void CRT_TEXTCOLOR(int c);
void CRT_TEXTCOLOR(int c) {
    int v;
    asm("MOVE [_crt_tmp1], R0");
    v = _crt_tmp1 & 15;
    _crt_fg = v & 7;
    if (v > 7) {
        asm("MOVE R0, #27"); asm("SYSCALL 400");
        asm("MOVE R0, #91"); asm("SYSCALL 400");
        asm("MOVE R0, #49"); asm("SYSCALL 400");
        asm("MOVE R0, #109"); asm("SYSCALL 400");
    }
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    /* VML→ANSI 颜色映射: C switch → 全局变量 → asm */
    switch (_crt_fg) {
        case 0: _crt_tmp1=30; break;  // Black
        case 1: _crt_tmp1=34; break;  // Blue
        case 2: _crt_tmp1=32; break;  // Green
        case 3: _crt_tmp1=36; break;  // Cyan
        case 4: _crt_tmp1=31; break;  // Red
        case 5: _crt_tmp1=35; break;  // Magenta
        case 6: _crt_tmp1=33; break;  // Yellow
        default: _crt_tmp1=37; break; // White
    }
    asm("MOVE R0, [_crt_tmp1]"); asm("SYSCALL 402");
    asm("MOVE R0, #109"); asm("SYSCALL 400");
}

void CRT_TEXTBACKGROUND(int c);
void CRT_TEXTBACKGROUND(int c) {
    int v;
    asm("MOVE [_crt_tmp1], R0");
    v = _crt_tmp1 & 7;
    _crt_bg = v;
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R1, _ansi_bg");
    asm("MOVE R2, [_crt_bg]");
    asm("MUL R2, #4");
    asm("ADD R1, R2");
    asm("MOVE R0, [R1]");
    asm("SYSCALL 402");
    asm("MOVE R0, #109"); asm("SYSCALL 400");
}

void CRT_WINDOW(int x1, int y1, int x2, int y2);
void CRT_WINDOW(int x1, int y1, int x2, int y2) {
    /* 同 CRT_TEXTCOLOR：形参一个字都不能写（见上方长注释），一律用局部量 */
    int a1, a2, a3, a4;
    asm("MOVE [_crt_tmp1], R0");
    asm("MOVE [_crt_tmp2], R1");
    a1 = _crt_tmp1; a2 = _crt_tmp2;
    asm("MOVE [_crt_tmp1], R2");
    asm("MOVE [_crt_tmp2], R3");
    a3 = _crt_tmp1; a4 = _crt_tmp2;
    if (a1 < 1) a1 = 1; if (a1 > a3) a1 = 1;
    if (a3 > 80) a3 = 80;
    if (a2 < 1) a2 = 1; if (a2 > a4) a2 = 1;
    if (a4 > 25) a4 = 25;
    _crt_wx1 = a1; _crt_wy1 = a2;
    _crt_wx2 = a3; _crt_wy2 = a4;
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, [_crt_wy1]"); asm("SYSCALL 402");
    asm("MOVE R0, #59"); asm("SYSCALL 400");
    asm("MOVE R0, [_crt_wy2]"); asm("SYSCALL 402");
    asm("MOVE R0, #114"); asm("SYSCALL 400");  /* 'r' = 114 */
    CRT_GOTOXY(a1, a2);
}

int CRT_KEYPRESSED();
int CRT_KEYPRESSED() {
    asm("SYSCALL 5");
    asm("MOVE [_crt_tmp1], R0");
    return _crt_tmp1 != 0;
}

int CRT_READKEY();
int CRT_READKEY() {
    asm("MOVE R0, #1"); asm("SYSCALL 5");
    asm("MOVE [_crt_tmp1], R0");
    return _crt_tmp1;
}

void CRT_DELAY(int ms);  void CRT_DELAY(int ms)  { asm("SYSCALL 52"); }
void CRT_SOUND(int f);   void CRT_SOUND(int f)   { asm("SYSCALL 57"); }
void CRT_NOSOUND();      void CRT_NOSOUND()      { }

void CRT_NORMVIDEO();
void CRT_NORMVIDEO() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #48"); asm("SYSCALL 400");  /* '0' = 48 */
    asm("MOVE R0, #109"); asm("SYSCALL 400");
    _crt_fg = 7; _crt_bg = 0;
}

void CRT_HIGHVIDEO();
void CRT_HIGHVIDEO() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #49"); asm("SYSCALL 400");  /* '1' */
    asm("MOVE R0, #109"); asm("SYSCALL 400");
}

void CRT_LOWVIDEO();
void CRT_LOWVIDEO() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #50"); asm("SYSCALL 400");  /* '2' */
    asm("MOVE R0, #109"); asm("SYSCALL 400");
}

void CRT_INSLINE();
void CRT_INSLINE() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #76"); asm("SYSCALL 400");  /* 'L' = 76 */
}

void CRT_DELLINE();
void CRT_DELLINE() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #77"); asm("SYSCALL 400");  /* 'M' = 77 */
}

void CRT_CURSORON();
void CRT_CURSORON() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #63"); asm("SYSCALL 400");  /* '?' = 63 */
    asm("MOVE R0, #50"); asm("SYSCALL 400");  /* '2' */
    asm("MOVE R0, #53"); asm("SYSCALL 400");  /* '5' */
    asm("MOVE R0, #104"); asm("SYSCALL 400"); /* 'h' = 104 */
}

void CRT_CURSOROFF();
void CRT_CURSOROFF() {
    asm("MOVE R0, #27"); asm("SYSCALL 400");
    asm("MOVE R0, #91"); asm("SYSCALL 400");
    asm("MOVE R0, #63"); asm("SYSCALL 400");
    asm("MOVE R0, #50"); asm("SYSCALL 400");
    asm("MOVE R0, #53"); asm("SYSCALL 400");
    asm("MOVE R0, #108"); asm("SYSCALL 400"); /* 'l' = 108 */
}

void CRT_WRITE_CHAR(int ch);
void CRT_WRITE_CHAR(int ch) {
    /* 同 CRT_TEXTCOLOR：形参不能写 */
    int v;
    asm("MOVE [_crt_tmp1], R0");
    v = _crt_tmp1;
    if (v == 10) {
        _crt_y++; _crt_x = _crt_wx1;
        if (_crt_y > _crt_wy2) _crt_y = _crt_wy2;
    } else if (v == 13) {
        _crt_x = _crt_wx1;
    } else if (v >= 32) {
        asm("MOVE R0, [_crt_tmp1]"); asm("SYSCALL 400");
        _crt_x++;
        if (_crt_x > _crt_wx2) { _crt_x = _crt_wx1; _crt_y++; }
    }
}

void CRT_WRITE_STRING(const char* s);
void CRT_WRITE_STRING(const char* s) {
    asm("SYSCALL 401");
    /* 光标跟踪 (粗略) */
    _crt_y++; _crt_x = _crt_wx1;
}
