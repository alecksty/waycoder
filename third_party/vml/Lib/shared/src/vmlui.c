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

/* 开窗（带声明）。rotatable 三选一：VML_WIN_PORTRAIT 只竖屏 / VML_WIN_ROTATABLE 支持旋转 /
 * VML_WIN_LANDSCAPE 只横屏；gamepad=0 ⇒ 不显示屏幕手柄区，画布吃满整屏。
 * 老程序照旧用 ui_win_open()（走 #520）—— **两个号各调各的，默认值只有宿主一处**：
 * 老号那边宿主直接给"可旋转=1、要手柄=1"，在这里再写一遍默认值就是第二张平行表。 */
int ui_win_open_ex(char* title, int w, int h, int rotatable, int gamepad) {
    return asm("SYSCALL #570, ${title}, ${w}, ${h}, ${rotatable}, ${gamepad}");
}

/* 开**电脑屏窗口**（第三种窗口，给老程序用）。
 *
 * 与 ui_win_open_ex 的差别是**三件事**，都在宿主那侧表达：
 *   ① 坐标系**固定**为 (w,h)，**永不**跟随旋转重排 —— 老程序按固定分辨率排的版，
 *      换空间就会画到框外。所以这里的 rotatable **没有"支持旋转"那一档**，
 *      它只决定**锁不锁方向**：PORTRAIT 锁竖 / LANDSCAPE 锁横 / 其它 = 不锁。
 *   ② 触摸**只发鼠标消息**（不发触摸消息）—— 老程序处理的是鼠标。
 *   ③ 带**屏幕键盘**（PC 布局）而不是手柄区；keyboard=0 可以关掉它吃满整屏。
 *
 * 画图照旧走 ui_*（`ui_clear`/`ui_rect`/`ui_line`/`ui_text`…）——
 * **不碰显存**，那是另一套已经不做的东西。
 *
 * ⚠ 默认分辨率 640×480（PC 上最眼熟那一档）由**宿主**兜底，这里不写第二遍。 */
int ui_win_open_pc(char* title, int w, int h, int rotatable, int keyboard) {
    return asm("SYSCALL #582, ${title}, ${w}, ${h}, ${rotatable}, ${keyboard}");
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

/* 屏幕方向：0 = 竖屏，1 = 横屏。开窗之前就能问。
   别拿 ui_scr_w() > ui_scr_h() 去推：那是**绘图区**的形状，会随宿主排版变
   （手柄收起/展开就变），而方向是设备本身的属性。 */
int ui_orientation(void) {
    return asm("SYSCALL #569");
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

/* ── 像素读回（583–585）────────────────────────────────────────────────
 *
 * 老 graphics.h 程序做**填充**与**精灵**绕不开读像素，而场景是保留模式的
 * ⇒ 宿主侧要先光栅化一次。三个号的分工见 `waycoder_ui.h` 的同名声明。
 *
 * ⚠ **多参数必须写在同一个 `asm()` 里**：每个 `${}` 展开都会先载入 R0，
 *   拆成多个 `asm()` 会互相覆盖寄存器（本仓记过这个坑）。
 */

/* 从 (x,y) 灌色，碰到 border 色停。返回落笔的矩形条数（0 = 没填）。 */
int ui_flood_fill(int x, int y, int color, int border) {
    return asm("SYSCALL #583, ${x}, ${y}, ${color}, ${border}");
}

/* 存一块画面 → 句柄（≥1），失败 0。 */
int ui_get_image(int x, int y, int w, int h) {
    return asm("SYSCALL #584, ${x}, ${y}, ${w}, ${h}");
}

/* 把句柄那块贴到 (x,y)。mode：0=COPY 直接贴 / 1=XOR 异或。返回 1 成功。 */
int ui_put_image(int x, int y, int handle, int mode) {
    return asm("SYSCALL #585, ${x}, ${y}, ${handle}, ${mode}");
}

/* ── 老 BASIC 的精灵位图（`DATA` 手打包 + `PUT`）────────────────── */

/* 读一个像素的颜色 → **0xRRGGBB**（越界返回 -1）。异或擦除要用。
 * ⚠ 每次调用宿主都要**光栅化一次**（场景是保留模式的），别放进密集大循环。 */
int ui_get_pixel(int x, int y) {
    return asm("SYSCALL #587, ${x}, ${y}");
}

/* 把一块**手打包在 `DATA` 里的 QBasic 位图**贴到 (x,y)。
 *
 * ## 为什么要它
 *
 * 老 BASIC 游戏的精灵**不是 GET 抓下来的**，而是把位图按 QBasic 的 GET/PUT
 * 数组格式**手打进 `DATA`**、`READ` 进一个长整型数组，再 `PUT (x,y), 数组, PSET/XOR`。
 * （GORILLA.BAS 的 `LBan&` 就是 —— `LBan&(0) = 458758`。）
 * 宿主那套 `ui_put_image` 只认 `ui_get_image` 给的**句柄**，所以这类精灵
 * **一个都画不出来**，而且程序侧一个错都不报。
 *
 * ## 块格式（**实测定的，不是猜的**）
 *
 *   arr[0]（4 字节头）：**低字 = 宽、高字 = 高**（就是宽高本身，**不是减一**）
 *   arr[1] 起          ：像素，**逐行**；每行里**按平面依次**放
 *                        `ceil(宽/8)` 个字节；字节的**最高位是最左像素**
 *   颜色               ：`color = p0 | p1<<1 | p2<<2 | p3<<3`（EGA 模式 9 四个平面）
 *
 * 判据：把 GORILLA.BAS 那四个香蕉解出来渲染成点阵 —— 左/右互为**水平镜像**、
 * 上/下互为**垂直镜像**，而且四条都能看出香蕉的弯月形状。四张图都对上了才算数。
 *
 * ## 两个刻意的取舍（都说清楚，免得下次有人以为漏了）
 *
 * ① **颜色 0 的像素跳过**（不画）。QBasic 的 XOR 本来就是这个语义（0 异或 d = d）；
 *    PSET 严格说会把 0 写上去（= 黑），但那样精灵罩住的背景会被一起涂黑 ——
 *    精灵贴在天际线/太阳上时就露馅。跳过后"画一次再擦一次"仍精确还原。
 * ② **XOR 按调色板索引做**（不是按 RGB）。宿主只回 0xRRGGBB，所以这里拿它到
 *    `pal` 里反查索引（16 项）。这一步非做不可 —— 异或的是**索引**正是
 *    "PSET 画、XOR 擦能还原"的全部依据：精灵用索引 14，擦时 14^14 = 0 = 背景色；
 *    按 RGB 异或会得到 #FFFFAA 这种新颜色，画面上留下一串擦不掉的痕迹。
 *    反查不到（宿主自己画的、不在调色板里的颜色）就退回 RGB 异或，不假装。
 */
void ui_put_qb_bitmap(int x, int y, int* arr, int* pal, int planes, int action) {
    int hdr, w, h, bprow, row, col, p, color, src, dst, off, b;
    char* data;

    hdr = arr[0];
    w = hdr & 65535;
    h = (hdr >> 16) & 65535;
    if (w <= 0 || h <= 0) return;
    if (planes <= 0) return;

    data = (char*)arr + 4;          /* 跳过那 4 字节头 */
    bprow = (w + 7) / 8;

    for (row = 0; row < h; row++) {
        for (col = 0; col < w; col++) {
            color = 0;
            for (p = 0; p < planes; p++) {
                off = (row * planes + p) * bprow + (col >> 3);
                b = data[off] & 255;
                if (b & (128 >> (col & 7))) color = color | (1 << p);
            }
            if (color != 0) {
                src = pal[color & 15];
                if (action == 1) {
                    dst = ui_get_pixel(x + col, y + row);
                    if (dst >= 0) {
                        /* 按**调色板索引**异或，不是按 RGB —— 这是 QBasic 的语义，
                         * 也是"PSET 画、XOR 擦能还原"的全部依据：
                         * 精灵用的是索引 14，擦的时候 14^14 = 0 = 背景色 ⇒ 精确还原。
                         * RGB 异或做不到这件事（#FFFF00 ^ #0000AA = #FFFFAA，是个新颜色）。
                         * 目的色反查索引：拿宿主回的 RGB 在调色板里找（16 项，够快）。
                         * 找不到（宿主自己画的颜色，不在调色板里）就退回 RGB 异或 —— 不假装。 */
                        int idx = -1, k;
                        for (k = 0; k < 16; k++)
                            if ((pal[k] & 0xFFFFFF) == (dst & 0xFFFFFF)) { idx = k; break; }
                        if (idx >= 0) src = pal[(idx ^ color) & 15];
                        else src = 0xFF000000 | ((src ^ dst) & 0xFFFFFF);
                    }
                }
                ui_pixel(x + col, y + row, src);
            }
        }
    }
}

/* ── 文字 ───────────────────────────────────────────────── */

/* 一次性画一行字。anchor: 0=左 1=中 2=右；style: 1=粗 2=斜。 */
void ui_text(int x, int y, char* s, int color, int size, int anchor) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}");
}

void ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}, ${style}");
}

/* 带**竖对齐**的文字。valign: 0=顶（= 老行为）1=中 2=底；style: 1=粗 2=斜。
 * 新号 #581 —— 给老号 #528 加参数会让老程序读到自己上一句留下的垃圾值。 */
void ui_text_v(int x, int y, char* s, int color, int size, int anchor, int valign, int style) {
    asm("SYSCALL #581, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}, ${valign}, ${style}");
}

/* 设置当前文字属性：字号 / 样式位 (1=粗 2=斜) / 颜色 / 锚点。
 * ⚠ 竖对齐**不在这里**（见下面的 ui_set_valign）—— 给这个号加第 5 个参数会让只传四个的
 *   老程序读到自己上一句留下的垃圾值。 */
void ui_set_font(int size, int style, int color, int anchor) {
    asm("SYSCALL #532, ${size}, ${style}, ${color}, ${anchor}");
}

/* 设"当前文字"的竖对齐（状态式，配 ui_text_cur 用）：
 * 0=顶（默认，盒顶落在 y）/ 1=中 / 2=底 / 3=**基线**（y 就是基线）。
 * 新号 #586 —— 理由同 ui_set_font 上面那条。 */
void ui_set_valign(int valign) {
    asm("SYSCALL #586, ${valign}");
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

/* 读一条，带"读完之后留不留"（VML_MSG_KEEP / VML_MSG_CONSUME）。
 * 保留模式只**看**队头那一条，队列里一个都不少 —— 下一次读到的还是它，
 * 直到程序明确地消费掉。想"先看一眼再决定谁来处理"时用；
 * ⚠ **别拿它当循环条件**：保留模式下永远返回同一条 = 死循环。 */
int ui_poll_ex(int* msg, int keep) {
    return asm("SYSCALL #571, ${msg}, ${keep}");
}

int ui_wait_ex(int* msg, int timeout_ms, int keep) {
    return asm("SYSCALL #572, ${msg}, ${timeout_ms}, ${keep}");
}

/* ── 全能接口：两个字符串进、一个 JSON 字符串出 ────────────────────────
 *
 * 给**不要求性能**的可扩展功能用：加一个能力 = 宿主侧注册一个函数，
 * 不占 syscall 号、不用改这里、也不用重生成 22 种语言的绑定。
 * 绘图/输入这类每帧都发生的东西**别走这里**（一次调用要过两趟 JSON + 一次内存拷贝）。
 *
 * 返回值：写进 out_buf 的字节数（不含结尾 NUL）；-1 = 失败（函数不认识 / 参数非法 /
 * 缓冲区放不下 —— 后面这种情形 out_buf 里会是一段说明原因的信封）。
 * 结果永远是对象：成功 {"ok":true,"result":…}，失败 {"ok":false,"error":"…"}。 */
int ui_call_json(char* fn, char* args_json, char* out_buf, int cap) {
    return asm("SYSCALL #573, ${fn}, ${args_json}, ${out_buf}, ${cap}");
}

/* 给**拿不到缓冲区指针**的前端（Python / BASIC / Lua …）的版本：
 * 结果放进静态缓冲，用下面两个函数按字节读 —— 连 char* 都能不解引用。
 * 容量 4096 够放一般的结果；不够时结果里会是"结果太长"的信封（一眼看得出）。 */
#define UI_JSON_BUF_N 4096
static char _ui_json_buf[UI_JSON_BUF_N];
static int _ui_json_len;

int ui_call_json_s(char* fn, char* args_json) {
    _ui_json_len = ui_call_json(fn, args_json, _ui_json_buf, UI_JSON_BUF_N);
    if (_ui_json_len < 0) {
        _ui_json_len = 0;
    }
    return _ui_json_len;
}

/* 上一次 ui_call_json_s 的结果长度（字节，不含结尾 NUL；失败 0）。 */
int ui_call_json_len(void) {
    return _ui_json_len;
}

/* 把上一次 ui_call_json_s 的结果**整份打到 stdout**（末尾补一个换行）。
 *
 * 存在的理由：跨语言自测要"把结果原样打出来给人看/给脚本比"，而让 22 种语言各自
 * 写一遍"逐字节取 + 单个字符输出"的循环，就是 22 份同一段代码（本仓库头号坑）。
 * 放在这里之后，各语言的自测都只剩两行：`ui_call_json_s(...)` + `ui_call_json_print()`。 */
void ui_call_json_print(void) {
    int i;
    for (i = 0; i < _ui_json_len; i = i + 1) {
        putchar(_ui_json_buf[i]);
    }
    putchar(10);   /* '\n' */
}

/* 结果里第 i 个字节（0..len-1）。越界返回 0 —— 与 ui_gget 一样安全失败。 */
int ui_call_json_at(int i) {
    if (i < 0 || i >= _ui_json_len) {
        return 0;
    }
    return _ui_json_buf[i];
}

int ui_msg_count(void) {
    return asm("SYSCALL #562");
}

/* 丢掉队列里**所有待处理消息** → 丢弃条数。
 *
 * 一次点击往往产生**多条**消息（按下/抬起/移动各一条），而游戏主循环通常只读它要的那条，
 * 剩下的就留在队列里 —— 于是"重开一局"时 ui_poll 又把**上一局的残留**读出来，
 * 症状是「重新开始后，黑子立刻又落到上次最后点的位置」。
 * 在**重新开始 / 切关 / 暂停恢复**这类状态断点上调它，把历史输入清干净。 */
int ui_msg_clear(void) {
    return asm("SYSCALL #568");
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

/* 棋盘占 0..199（`BOARD_AT`），7 种方块的掩码紧接着放 200..206 —— 这段布局是**跨语言约定**：
 * `Examples/python/tetris.py` 里写死了 `BOARD_AT = 0` / `MASK_AT = 200`，两边必须对得上。
 *
 * ⚠ **这个宏曾经不存在**：`ui_piece_init` 用了 `MASK_AT` 却没有任何地方定义它，
 *   而当时的前端对未声明标识符是**静默按 0 算**的 ⇒ 掩码实际落在 0..6，
 *   **正好压在前 7 个棋盘格上**（棋盘写 0..6 就把掩码冲了，`ui_piece_cell` 从此返回垃圾）。
 *   一直没暴露，是因为 `GenLib -b` 是**增量**的（`.vml` 比 `.c` 新就跳过）——
 *   直到给本文件加新接口时改了它的 mtime，重编译才把这条打出来：
 *   `error: 未声明的变量 'MASK_AT'`（前端的未定义变量检查是后来加的，
 *   这类"靠 0 蒙对"的代码再也过不去了）。 */
#define MASK_AT 200

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

/* ── 绘图增强（534–539）：渐变刷子 / 路径与曲线 / 多边形 ──
 *
 * ⚠ 这一组与下面「手感」那 5 个，此前**只在 `Lib/c/waycoder_ui.h` 里声明过、
 *   本文件里没有实现**（2026-09-17 实测）。后果是双重的：
 *     · 桌面 vmlcli 里 `CALL ui_beep` **链不上** ⇒ `KeyNotFoundException` 直接崩
 *       （`Examples/go/snake.go` 实测如此）⇒ 所有游戏**只能在设备上验**；
 *     · 设备那边对未解析调用是容忍的 ⇒ 游戏跑得动，但这些调用**全是空转**
 *       （用户早就报过「#57 调了没反应」）。
 *   宿主侧的 syscall 一直是好的（`VmlUiCalls` 的 534–539、`VmSpeakerBeep`、
 *   `DeviceDisplay.KeepScreenOn`、`Preferences`），缺的只是这一层
 *   「把 `ui_*` 标签接到号段」的包装。
 *
 * 几何是**归一化 0..1000 的整数**（千分之一），约定见 `Lib/c/waycoder_ui.h`。 */

void ui_gradient(char* id, int radial, int color_a, int color_b,
                 int a1, int a2, int a3, int a4) {
    asm("SYSCALL #534, ${id}, ${radial}, ${color_a}, ${color_b}, ${a1}, ${a2}, ${a3}, ${a4}");
}

void ui_path(char* d, int stroke, int width, int fill, char* grad, int cap, int dash) {
    asm("SYSCALL #535, ${d}, ${stroke}, ${width}, ${fill}, ${grad}, ${cap}, ${dash}");
}

/* pts 是 int 数组、每两个 int 一个点（x,y）；count 是**点数**不是数组长度。 */
void ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad) {
    asm("SYSCALL #536, ${pts}, ${count}, ${fill}, ${stroke}, ${width}, ${grad}");
}

void ui_polyline(int* pts, int count, int stroke, int width, char* grad) {
    asm("SYSCALL #537, ${pts}, ${count}, ${stroke}, ${width}, ${grad}");
}

void ui_rect_grad(int x, int y, int w, int h, char* grad, int radius) {
    asm("SYSCALL #538, ${x}, ${y}, ${w}, ${h}, ${grad}, ${radius}");
}

void ui_circle_grad(int cx, int cy, int r, char* grad) {
    asm("SYSCALL #539, ${cx}, ${cy}, ${r}, ${grad}");
}

/* ── 刷子 / 样式 / 一个号画所有形状（574–576）────────────────────────
 *
 * ## 为什么只有三个号
 *
 * VML 程序**不直接调 syscall** —— 它调这里的包装函数（22 门语言各有 GenLib 生成的绑定）。
 * **号是给库用的，不是给程序作者用的**，所以一个号可以靠"操作码"承担很多版本：
 * 加一个形状 = 加一个形状码 + 一个包装函数，**不用再占号**。
 *
 * ⚠ `${}` 占位符**只认变量名**（宏名、字面量都不认），所以操作码要先落到一个局部变量。
 *   这是实测校准过的写法（见 Lib/README-ui.md），别图省事写成 ${VML_SHAPE_STAR}。
 */

/* 操作码 —— 与宿主 `VmlShape` / `VmlBrushKind` / `VmlStyleSlot` 一一对应，
 * 也与 `Lib/c/waycoder_ui.h` 里给用户看的 `VML_CAP_*` / `VML_ARROW_*` 同族。
 * ⚠ **跨语言契约，只能末尾追加**。
 *
 * ⚠ 定义在**本文件**而不是头文件里：本文件（共享库的实现）并不 include 头文件 ——
 *   头文件是给**用户的 C 程序**用的（只有声明与常量）。两边是同一个契约，
 *   改一边必须改另一边（`scripts/check-vml-patches.sh` 的判据②a 会盯着生成物）。 */
#define VML_BRUSH_SOLID    0
#define VML_BRUSH_LINEAR   1
#define VML_BRUSH_RADIAL   2
#define VML_BRUSH_BY_NAME  3

#define VML_STYLE_FILL     0
#define VML_STYLE_PEN      1
#define VML_STYLE_TEXT     2

#define VML_SHAPE_RECT         0
#define VML_SHAPE_CIRCLE       1
#define VML_SHAPE_ELLIPSE      2
#define VML_SHAPE_LINE         3
#define VML_SHAPE_POLYGON      4
#define VML_SHAPE_POLYLINE     5
#define VML_SHAPE_PATH         6
#define VML_SHAPE_TEXT         7
#define VML_SHAPE_STAR         8
#define VML_SHAPE_REGULAR      9
#define VML_SHAPE_RING        10
#define VML_SHAPE_PIE         11
#define VML_SHAPE_HEART       12
#define VML_SHAPE_ELLIPSE_GRAD 13

/* 造刷子 → 句柄（>=1；0 = 失败）。颜色 = 0xAARRGGBB。 */
int ui_brush_solid(int color) {
    int op;
    op = VML_BRUSH_SOLID;
    return asm("SYSCALL #575, ${op}, ${color}");
}

/* 线性渐变刷子。几何是**千分之一**（0..1000）：x1 y1 x2 y2 相对形状自己的包围盒。 */
int ui_brush_linear(int color_a, int color_b, int x1, int y1, int x2, int y2) {
    int op;
    op = VML_BRUSH_LINEAR;
    return asm("SYSCALL #575, ${op}, ${color_a}, ${color_b}, ${x1}, ${y1}, ${x2}, ${y2}");
}

/* 径向渐变刷子：cx cy r 也是千分之一。 */
int ui_brush_radial(int color_a, int color_b, int cx, int cy, int r) {
    int op;
    op = VML_BRUSH_RADIAL;
    return asm("SYSCALL #575, ${op}, ${color_a}, ${color_b}, ${cx}, ${cy}, ${r}");
}

/* 按名字引用一个已经 ui_gradient 定义过的渐变 → 句柄（0 = 没有这个名字）。 */
int ui_brush_named(char* gradId) {
    int op;
    op = VML_BRUSH_BY_NAME;
    return asm("SYSCALL #575, ${op}, ${gradId}");
}

/* 设置填充刷子。**直接传颜色也合法**（颜色 = 只有一个色标的刷子）：ui_set_fill(0xFF2A3346)。 */
int ui_set_fill(int brush) {
    int slot;
    slot = VML_STYLE_FILL;
    return asm("SYSCALL #576, ${slot}, ${brush}, 0, 0, 0, 0");
}

/* 设置画笔（描边）：刷子 + 线宽 + 线帽 + 虚线 + 箭头。brush 传 0 = 不描边。 */
int ui_set_pen(int brush, int width, int cap, int dash, int arrow) {
    int slot;
    slot = VML_STYLE_PEN;
    return asm("SYSCALL #576, ${slot}, ${brush}, ${width}, ${cap}, ${dash}, ${arrow}");
}

/* 设置文字刷子（0 = 回到 ui_set_font 给的颜色）。 */
int ui_set_text_brush(int brush) {
    int slot;
    slot = VML_STYLE_TEXT;
    return asm("SYSCALL #576, ${slot}, ${brush}, 0, 0, 0, 0");
}

/* ── 形状（都走 #574，样式取自上面设的刷子）── */

void ui_draw_rect(int x, int y, int w, int h, int radius) {
    int op;
    op = VML_SHAPE_RECT;
    asm("SYSCALL #574, ${op}, ${x}, ${y}, ${w}, ${h}, ${radius}");
}

void ui_draw_circle(int cx, int cy, int r) {
    int op;
    op = VML_SHAPE_CIRCLE;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${r}");
}

void ui_draw_ellipse(int cx, int cy, int rx, int ry) {
    int op;
    op = VML_SHAPE_ELLIPSE;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${rx}, ${ry}");
}

void ui_draw_line(int x1, int y1, int x2, int y2) {
    int op;
    op = VML_SHAPE_LINE;
    asm("SYSCALL #574, ${op}, ${x1}, ${y1}, ${x2}, ${y2}");
}

/* 多边形（close=1 自动闭合）/ 折线（close=0）。pts 每两个 int 一个点，count 是**点数**。 */
void ui_draw_poly(int* pts, int count, int close) {
    int op;
    if (close != 0) { op = VML_SHAPE_POLYGON; } else { op = VML_SHAPE_POLYLINE; }
    asm("SYSCALL #574, ${op}, ${pts}, ${count}");
}

void ui_draw_path(char* d) {
    int op;
    op = VML_SHAPE_PATH;
    asm("SYSCALL #574, ${op}, ${d}");
}

void ui_draw_text(int x, int y, char* s) {
    int op;
    op = VML_SHAPE_TEXT;
    asm("SYSCALL #574, ${op}, ${x}, ${y}, ${s}");
}

/* 渐变填充的椭圆（自带渐变名，与 ui_rect_grad / ui_circle_grad 同形）。 */
void ui_ellipse_grad(int cx, int cy, int rx, int ry, char* grad) {
    int op;
    op = VML_SHAPE_ELLIPSE_GRAD;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${rx}, ${ry}, ${grad}");
}

void ui_draw_star(int cx, int cy, int r_out, int r_in, int points, int rot) {
    int op;
    op = VML_SHAPE_STAR;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${r_out}, ${r_in}, ${points}, ${rot}");
}

void ui_draw_regular(int cx, int cy, int r, int n, int rot) {
    int op;
    op = VML_SHAPE_REGULAR;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${r}, ${n}, ${rot}");
}

void ui_draw_ring(int cx, int cy, int r_out, int r_in) {
    int op;
    op = VML_SHAPE_RING;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${r_out}, ${r_in}");
}

void ui_draw_pie(int cx, int cy, int r, int a0, int a1) {
    int op;
    op = VML_SHAPE_PIE;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${r}, ${a0}, ${a1}");
}

void ui_draw_heart(int cx, int cy, int size) {
    int op;
    op = VML_SHAPE_HEART;
    asm("SYSCALL #574, ${op}, ${cx}, ${cy}, ${size}");
}

/* ── 手感：音效 / 震动 / 持久化 / 常亮 ──
 *
 * 音效走 **VM 内置的 #57 蜂鸣** —— 宿主把它截住接到真实音频（方波 + 3ms 包络），
 * 零素材、不必打包音频文件。 */

void ui_beep(int freq, int ms) {
    asm("SYSCALL #57, ${freq}, ${ms}");
}

/* 震动：时长 ms + 强度（0-255，0 = 默认）。
 * ⚠ 强度位**必须**是形参 —— `${形参}` 是按出现顺序装进 R0、R1…，
 *   在 asm 文本里塞字面量 `0` 不会落到 R1（实测被丢掉），宿主读到的就是垃圾值。 */
void ui_vibrate(int ms, int strength) {
    asm("SYSCALL #545, ${ms}, ${strength}");
}

void ui_keep_on(int on) {
    asm("SYSCALL #553, ${on}");
}

/* 持久化。键由宿主统一加 `vml.` 前缀，不会和 App 自己的设置打架。 */
void ui_store_set(char* key, char* value) {
    asm("SYSCALL #550, ${key}, ${value}");
}

int ui_store_get(char* key, char* buf, int cap) {
    return asm("SYSCALL #551, ${key}, ${buf}, ${cap}");
}

/* ── 命令行参数（#62/#63，v0.96.371）────────────────────────────
 *
 * 参数由**宿主**喂：桌面上是 `vmlcli prog.c --arg -l --arg foo`，
 * 手机上是 `vml run examples/c/tetris.c -l`（多出来的 token 全部当参数）。
 * 编译产物里**不含**参数 ⇒ 同一个 `.vml` 可以带不同参数跑。
 *
 * `argv[0]` 是**程序名**（宿主决定；宿主一个参数都不给时它也一定在 ——
 * C 保证 `argc >= 1`，而老程序常拿 `argv[0]` 打用法）。所以用户给的第一个参数是 `ui_arg(1,…)`。
 *
 * ⚠ `int main(int argc, char **argv)` 的程序**不需要**这两个函数（入口帧直接给），
 *   它们是给 `int main(void)` 的程序、以及非 C 语言的绑定用的。
 *
 * 判据：`scripts/vml-c-probe/cases/45-argv-pass.c`（**两条路取到同一份**）。 */
int ui_argc(void) {
    return asm("SYSCALL #62");
}

/* 把第 i 个参数拷进调用方缓冲区（与 `ui_store_get` 同一套：宿主没有能"交还"的堆），
   返回**写入的字节数**（不含结尾 NUL）；i 越界 / 缓冲区无效返回 -1。
   容量不够就**截断**（照 C 的 `strncpy` 语义，一定补 NUL）。 */
int ui_arg(int i, char* buf, int cap) {
    return asm("SYSCALL #63, ${i}, ${buf}, ${cap}");
}

/* ── 通用宿主调用口（577–580，v0.96.326）──
 *
 * 四个函数的结构完全一样：**把数组的元素装进寄存器 + 发一条 syscall**。
 * 第 0 个元素是**调用号**（id），返回值写回第 0 号寄存器 —— 于是调用方
 * 拿不到原来传进去的 v[0]（这是 ABI 的一部分，见头文件那段）。
 * 宿主按 id 查注册表（两端共用 WayCoder/UI/Shared/VmlCallRegistry.cs）。
 *
 * ## 为什么 int8 直接排 8 个 `${}`，另外三个要绕一道指针
 *
 * `${局部量}` 展开出来的是**一条 32 位 `MOVE` 装载**（见本文件开头那段：
 * 形参/局部量按出现顺序落 R0、R1…）。int8 要的正好就是"32 位值进 R0–R7"，一拍即合。
 *
 * 但 float/long/double **不行**：`MOVE` 只搬 32 位，而且它**不碰浮点/长整数寄存器组** ——
 * 值进了通用寄存器，`F1`/`L1`/`D1` 里还是旧的。这三个组只能由
 * `MOVEF`/`MOVEL`/`MOVED` 写（运行时按操作数编码落到 `floatRegisters`/`longRegisters`/
 * `doubleRegisters`）。所以先把地址算进一个指针局部量，再用
 * `MOVEF F1, [R0]` 把**该类型的值**从内存直接读进对应的寄存器组。
 * 顺带一个好处：值全程没有被拆成低/高两半过（64 位的 long/double 用 `MOVE` 只能拿到低 32 位）。
 *
 * ## 调用号的整数视图
 *
 * 四个口的宿主都从 `R0` 读 id（"怎么读 id"在宿主侧只有一种写法）。int8 本来就是 R0；
 * 另外三个在装完类型寄存器之后再补一句 `id = (int)v[0]`，由前端生成一条
 * `F2I`/`L2I`/`D2I` 把整数视图放进 R0 —— 那一步**不会**碰 F0/L0/D0 里已经装好的值。
 * ⚠ 顺序不能反：先装类型寄存器、后写 R0（反过来 R0 会被 `MOVEF F0` 的低位镜像冲掉）。
 */

/* 8 个 int：v[0]=调用号 R0，v[1..7] → R1..R7；返回值覆盖 R0。 */
int callwithint8(int* v) {
    int a0 = v[0]; int a1 = v[1]; int a2 = v[2]; int a3 = v[3];
    int a4 = v[4]; int a5 = v[5]; int a6 = v[6]; int a7 = v[7];
    return asm("SYSCALL #577, ${a0}, ${a1}, ${a2}, ${a3}, ${a4}, ${a5}, ${a6}, ${a7}");
}

/* 8 个 float：v[0]=调用号 F0（同时 R0 放它的整数视图），v[1..7] → F1..F7；返回值覆盖 F0。 */
float callwithfloat8(float* v) {
    float* p;
    int id;
    p = v;     asm("MOVEF F0, [${p}]");
    p = v + 1; asm("MOVEF F1, [${p}]");
    p = v + 2; asm("MOVEF F2, [${p}]");
    p = v + 3; asm("MOVEF F3, [${p}]");
    p = v + 4; asm("MOVEF F4, [${p}]");
    p = v + 5; asm("MOVEF F5, [${p}]");
    p = v + 6; asm("MOVEF F6, [${p}]");
    p = v + 7; asm("MOVEF F7, [${p}]");
    id = (int)v[0];
    return asm("SYSCALL #578, ${id}");
}

/* 4 个 long：v[0]=调用号 L0（同时 R0 放它的整数视图），v[1..3] → L1..L3；返回值覆盖 L0。 */
long callwithlong4(long* v) {
    long* p;
    int id;
    p = v;     asm("MOVEL L0, [${p}]");
    p = v + 1; asm("MOVEL L1, [${p}]");
    p = v + 2; asm("MOVEL L2, [${p}]");
    p = v + 3; asm("MOVEL L3, [${p}]");
    id = (int)v[0];
    return asm("SYSCALL #579, ${id}");
}

/* 4 个 double：v[0]=调用号 D0（同时 R0 放它的整数视图），v[1..3] → D1..D3；返回值覆盖 D0。 */
double callwithdouble4(double* v) {
    double* p;
    int id;
    p = v;     asm("MOVED D0, [${p}]");
    p = v + 1; asm("MOVED D1, [${p}]");
    p = v + 2; asm("MOVED D2, [${p}]");
    p = v + 3; asm("MOVED D3, [${p}]");
    id = (int)v[0];
    return asm("SYSCALL #580, ${id}");
}
