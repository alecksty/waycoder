/* vmlui.c —— 手机端 UI syscall 的**共享调用库**（对话框 / 窗体 / 绘图 / 输入）
 *
 * 编译：`Lib/build_libs.sh --shared`（等价于
 *       `vmltool -c vmlui.c -o ../vmlui.vml -I ../shared -I ../c --mode mcu --no-link`）
 * 产物：`Lib/shared/vmlui.vml` —— 由 `vmltool.config.xml` 挂到各语言的 `Libs` 上，
 *       **C 与 Python 等前端共用同一份实现**。
 *
 * ## 为什么要有这个文件（而不是让各语言自己写）
 *
 * 汇编级 syscall 只有 C/ObjC/C++ 能直接调（`asm()`），其余前端（Python/BASIC/…）
 * 只能**按标签调用 C 函数**。所以"让所有语言都能用上 UI 接口"的唯一做法，
 * 就是在这里写一层 C 包装、编成 `.vml` 供大家链接 —— 这正是上游 `Lib/shared/src/vmlsys.c`
 * 的定位（它给非 C 语言提供类型安全的 SYSCALL 接口）。
 *
 * ⚠ **上游那份 `vmlsys.c` 的参数传递是坏的**：里面写成
 * `void vml_print_int(int n) { asm("SYSCALL 6"); }` —— **没有把 n 放进 R0**，
 * 而 `asm()` 的文本是原样进汇编器的，汇编器并不认识 C 的形参名。
 * 本文件用 `${形参}` 占位符（C 前端唯一实现了的替换），形参按出现顺序自动落
 * R0、R1、R2…，所以参数是真的传进去了（详见 `Lib/README-ui.md` 的实测校准）。
 *
 * ## 返回值
 *
 * 形如 `return asm("SYSCALL #566");` —— **必须把 asm 当表达式用**；
 * 写成"先 asm(...) 再 return 0"会把返回值丢掉。
 *
 * ## 线程/生命周期
 *
 * 这些函数只做"把参数量进寄存器 + 发一条 syscall"，状态全在宿主侧
 * （`WayCoder.Maui/Services/VmlUiCalls.cs`）。没有全局变量，可重入。
 */

/* ── 对话框 ─────────────────────────────────────────────── */

/* 消息框。返回 0=确定/是，1=否/取消。 */
int ui_dlg_msg(char* title, char* body, int style) {
    return asm("SYSCALL #500, ${title}, ${body}, ${style}");
}

/* 单选。opts 是 NUL 分隔的选项块，n=选项数，def=默认项。返回选中下标，取消 -1。 */
int ui_dlg_select(char* title, char* body, char* opts, int n, int def) {
    return asm("SYSCALL #501, ${title}, ${body}, ${opts}, ${n}, ${def}");
}

/* 多选。返回位掩码（第 i 项 = 1<<i），取消 -1。 */
int ui_dlg_multi(char* title, char* body, char* opts, int n) {
    return asm("SYSCALL #502, ${title}, ${body}, ${opts}, ${n}");
}

/* 输入框：把用户输入写进 buf（最多 cap-1 字节 + 结尾 NUL）。返回写入长度，取消 -1。 */
int ui_dlg_input(char* title, char* prompt, char* buf, int cap) {
    return asm("SYSCALL #503, ${title}, ${prompt}, ${buf}, ${cap}");
}

/* ── 窗体 ───────────────────────────────────────────────── */

/* 开一个绘图窗口（带标题栏与返回箭头）。返回句柄(1)，失败 -1。 */
int ui_win_open(char* title, int w, int h) {
    return asm("SYSCALL #520, ${title}, ${w}, ${h}");
}

int ui_win_close(void) {
    return asm("SYSCALL #521");
}

/* 用户点了返回箭头 → 1，程序主循环应退出。 */
int ui_win_closed(void) {
    return asm("SYSCALL #565");
}

/* 可用绘图区（单位与绘图一致，可直接拿来算格子）。 */
int ui_scr_w(void) {
    return asm("SYSCALL #566");
}

int ui_scr_h(void) {
    return asm("SYSCALL #567");
}

/* ── 绘图（保留模式：只管追加图元，宿主按帧渲染）───────────── */

void ui_clear(int color) {
    asm("SYSCALL #522, ${color}");
}

void ui_pixel(int x, int y, int color) {
    asm("SYSCALL #523, ${x}, ${y}, ${color}");
}

void ui_line(int x1, int y1, int x2, int y2, int color, int lw) {
    asm("SYSCALL #524, ${x1}, ${y1}, ${x2}, ${y2}, ${color}, ${lw}");
}

/* fill!=0 画实心；radius>0 画圆角。 */
void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius) {
    asm("SYSCALL #525, ${x}, ${y}, ${w}, ${h}, ${color}, ${fill}, ${lw}, ${radius}");
}

void ui_circle(int cx, int cy, int r, int color, int fill, int lw) {
    asm("SYSCALL #526, ${cx}, ${cy}, ${r}, ${color}, ${fill}, ${lw}");
}

void ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw) {
    asm("SYSCALL #527, ${cx}, ${cy}, ${rx}, ${ry}, ${color}, ${fill}, ${lw}");
}

/* 帧边界标记（本帧画完了）。 */
void ui_present(void) {
    asm("SYSCALL #531");
}

/* ── 文字 ───────────────────────────────────────────────── */

/* 一次性画一行字。anchor: 0=左 1=中 2=右；style: 1=粗 2=斜。 */
void ui_text(int x, int y, char* s, int color, int size, int anchor) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}");
}

void ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}, ${style}");
}

/* 设置当前文字属性：字号 / 样式位 (1=粗 2=斜) / 颜色 / 锚点。 */
void ui_set_font(int size, int style, int color, int anchor) {
    asm("SYSCALL #532, ${size}, ${style}, ${color}, ${anchor}");
}

/* 用**当前文字属性**画一行字（属性由 ui_set_font 设定）。 */
void ui_text_cur(int x, int y, char* s) {
    asm("SYSCALL #533, ${x}, ${y}, ${s}");
}

/* ── 输入（统一消息队列）────────────────────────────────────
 *
 * 消息缓冲固定 **16 字节 = 4 个 int**：msg[0]=类型 msg[1]=A msg[2]=B msg[3]=时间戳
 * 触摸类消息：A=x，B=y（单位与绘图一致）。 */

/* 非阻塞取一条。返回消息类型，无消息 0。 */
int ui_poll(int* msg) {
    return asm("SYSCALL #560, ${msg}");
}

/* 阻塞取一条，timeout_ms=0 表示无限等。返回消息类型，超时 0。 */
int ui_wait(int* msg, int timeout_ms) {
    return asm("SYSCALL #561, ${msg}, ${timeout_ms}");
}

int ui_msg_count(void) {
    return asm("SYSCALL #562");
}

/* ── 面向**不支持指针的语言**（Python/BASIC/…）的取消息接口 ──────────
 *
 * C 里可以直接 `int msg[4]; ui_wait(msg, 0)`，但其它前端拿不到"一块内存的地址"，
 * 也没有办法按 int 下标去读它。所以这里把消息**取出来拆开放进静态变量**，
 * 由三个访问器读 —— 调用方只需要：
 *
 *     t = ui_wait_msg(0)
 *     if t == 6:  x = ui_msg_a();  y = ui_msg_b()
 *
 * ⚠ 一次只保留**最近一条**消息（单缓冲）。要"把队列里积压的都处理完"，
 * 用 `while ui_poll_msg() != 0:` 连取即可 —— 每次取都会刷新这三个值。
 */
static int _ui_msg_type;
static int _ui_msg_a;
static int _ui_msg_b;

/* 取一条消息（timeout_ms=0 无限等），返回类型；内容用下面三个读。 */
int ui_wait_msg(int timeout_ms) {
    int* buf;
    int tmp[4];
    buf = tmp;
    _ui_msg_type = asm("SYSCALL #561, ${buf}, ${timeout_ms}");
    _ui_msg_a = tmp[1];
    _ui_msg_b = tmp[2];
    return _ui_msg_type;
}

/* 非阻塞取一条，返回类型（无消息返回 0）。 */
int ui_poll_msg(void) {
    int* buf;
    int tmp[4];
    buf = tmp;
    _ui_msg_type = asm("SYSCALL #560, ${buf}");
    _ui_msg_a = tmp[1];
    _ui_msg_b = tmp[2];
    return _ui_msg_type;
}

/* 最近一条消息的三个字段。 */
int ui_msg_type(void) { return _ui_msg_type; }
int ui_msg_a(void) { return _ui_msg_a; }
int ui_msg_b(void) { return _ui_msg_b; }

/* ── 通用整数网格（给"没有可靠数组"的语言用）────────────────────────
 *
 * C 里棋盘就是 `int board[200]`，但实测 Python 前端的列表**读可以、写不生效**
 * （`b[i] = v` 之后读回来还是 0），嵌套列表也错 —— 于是棋盘只能放在这边，
 * 由调用方用整数下标读写：
 *
 *     ui_gclear()
 *     ui_gset(3, 1)            # 第 3 格
 *     v = ui_gget(3)
 *
 * 256 个 int 够放 10×20 的棋盘（200）+ 7 种方块的 4×4 位掩码（7）。
 * 越界一律**安全失败**（写丢弃、读返回 0），不抛异常 —— 游戏代码里少一层边界判断。
 */
#define UI_GRID_N 256
static int _ui_grid[UI_GRID_N];

void ui_gclear(void) {
    int i;
    for (i = 0; i < UI_GRID_N; i = i + 1) _ui_grid[i] = 0;
}

void ui_gset(int idx, int val) {
    if (idx >= 0 && idx < UI_GRID_N) _ui_grid[idx] = val;
}

int ui_gget(int idx) {
    if (idx >= 0 && idx < UI_GRID_N) return _ui_grid[idx];
    return 0;
}

/* ── 方块几何（给"没有可靠位运算/数组"的语言用）────────────────────
 *
 * 7 种方块各 4 次旋转，每次 4 个格子。**位运算与旋转公式都放这里**：
 * BASIC 只有 AND/OR、没有移位取位，Python 的列表读写也不可靠 ——
 * 各前端只负责"拿坐标 → 画出来 / 判一下"，几何这一层共用一份。
 *
 *   ui_piece_init()                       一次，初始化形状表
 *   ui_piece_cell(pid, rot, which)        第 which 个格子的坐标，打包为 x*16+y；无此格返回 -1
 *
 * `pid` 0..6，`rot` 0..3，`which` 0..3。
 * 坐标以方块**左上角**为原点（4×4 包围盒内），调用方加上落点即可。
 */
static int _ui_piece_rot[7];     /* 当前 pid 的基准掩码在 _ui_grid 的槽位（见 ui_piece_init） */

void ui_piece_init(void) {
    /* 4×4 行优先位掩码，第 r 行第 c 位 = r*4+c */
    ui_gset(MASK_AT + 0, 0x0066);   /* O */
    ui_gset(MASK_AT + 1, 0x0F00);   /* I */
    ui_gset(MASK_AT + 2, 0x006C);   /* S */
    ui_gset(MASK_AT + 3, 0x00C6);   /* Z */
    ui_gset(MASK_AT + 4, 0x00E4);   /* T */
    ui_gset(MASK_AT + 5, 0x0062);   /* L */
    ui_gset(MASK_AT + 6, 0x00E8);   /* J */
    _ui_piece_rot[0] = MASK_AT + 0;
    _ui_piece_rot[1] = MASK_AT + 1;
    _ui_piece_rot[2] = MASK_AT + 2;
    _ui_piece_rot[3] = MASK_AT + 3;
    _ui_piece_rot[4] = MASK_AT + 4;
    _ui_piece_rot[5] = MASK_AT + 5;
    _ui_piece_rot[6] = MASK_AT + 6;
}

/* 4×4 掩码顺时针 90°：位 (r,c) → (c, 3-r) */
static int _ui_rot90(int m) {
    int out;
    int r;
    int c;
    out = 0;
    r = 0;
    while (r < 4) {
        c = 0;
        while (c < 4) {
            if ((m & (1 << (r * 4 + c))) != 0)
                out = out | (1 << (c * 4 + (3 - r)));
            c = c + 1;
        }
        r = r + 1;
    }
    return out;
}

int ui_piece_cell(int pid, int rot, int which) {
    int m;
    int k;
    int r;
    int c;
    int n;

    if (pid < 0 || pid > 6 || which < 0 || which > 3) return -1;

    m = ui_gget(_ui_piece_rot[pid]);
    k = rot % 4;
    while (k > 0) {
        m = _ui_rot90(m);
        k = k - 1;
    }

    /* 数到第 which 个置位 */
    n = 0;
    r = 0;
    while (r < 4) {
        c = 0;
        while (c < 4) {
            if ((m & (1 << (r * 4 + c))) != 0) {
                if (n == which) return c * 16 + r;
                n = n + 1;
            }
            c = c + 1;
        }
        r = r + 1;
    }
    return -1;
}

/* 定时器：每隔 interval_ms 往队列投一条 VML_MSG_TIMER（A=id，B=tag）。 */
int ui_timer_set(int interval_ms, int tag) {
    return asm("SYSCALL #563, ${interval_ms}, ${tag}");
}

int ui_timer_kill(int id) {
    return asm("SYSCALL #564, ${id}");
}

/* ── 随机数 / 计时（游戏要用，而只有 C 能直接调 #50/#53）──────
 *
 * 非 C 前端拿不到 syscall，游戏里的"下一个方块""洗牌"就只能自己搓一个劣质
 * 伪随机（或者干脆固定顺序）。放这里一份，各语言共用同一个源。
 */

/* 0..n-1 的随机数（n<=0 返回 0）。 */
int ui_rand(int n) {
    int v;
    if (n <= 0) return 0;
    v = asm("SYSCALL #50");
    /* #50 返回的是 32 位有符号随机数，负数取模在 C 里是负数 ⇒ 先归一到非负 */
    return (v % n + n) % n;
}

/* 自 VM 启动起的毫秒数（单调递增，可作动画相位/超时基准）。 */
int ui_tick(void) {
    return asm("SYSCALL #53");
}

/* ── 图标 / 图片 ────────────────────────────────────────── */

void ui_icon(int x, int y, char* name, int size, int color) {
    asm("SYSCALL #529, ${x}, ${y}, ${name}, ${size}, ${color}");
}

void ui_image(int x, int y, char* path, int w, int h) {
    asm("SYSCALL #530, ${x}, ${y}, ${path}, ${w}, ${h}");
}
