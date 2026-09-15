/* waycoder_ui.h —— 手机端 UI syscall 的 C 语言封装（对话框 / 窗体 / 绘图 / 输入）
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  这个头文件**自带实现**（不另配 .c），因为 VML 的 C 前端一次只编一个文件、
 *  没有"多目标文件 + 链接"这一步 —— 拆成 .h/.c 两个文件反而用不起来。
 *  头文件里有 include guard，同一个文件里 include 多次是安全的。
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * ## 号段
 * 500–599 是**宿主 UI 保留段**，只有手机 App（WayCoder）里能用；桌面 vmltool 不认识
 * 这些号，会走内置 switch 报未知 syscall。号段表见 `Lib/README-ui.md`。
 *
 * ## 参数怎么进寄存器（这一段是实测出来的，别照别的语料猜）
 *
 * C 里调 syscall 的**唯一可靠**写法是 `${名}` 占位符：
 *
 *     asm("SYSCALL #500, ${title}, ${body}");     // → R0=title, R1=body，然后 SYSCALL
 *
 * `CodeGenerator.Statements.cs` 的 `GenerateAsmStatement` 会把 `${名}` 按出现顺序
 * 换成 R0、R1、R2…，并在它前面补一条 `MOVE Rn, [变量槽]`。
 *
 * **不可靠的写法**（本仓库的 `Lib/c/stdlib.c`、`Lib/c/time.c` 里就是这么写的）：
 *
 *     asm("MOVE R0, s");        // ✗ 这句文本原样进汇编器，而局部变量在栈上（R12±偏移）、
 *                               //   汇编器没有它的符号表 —— R0 拿到的是垃圾
 *
 * `${名}` 只认**简单变量名**，不认 `p.x`、`arr[i]`、函数名这类表达式 ——
 * 要传就先赋给一个变量。
 *
 * ## 返回值怎么出来
 *
 * **必须把 asm 当表达式用**，写成 `return asm(...)` 或 `x = asm(...)`：
 *
 *     int ui_scr_w(void) { return asm("SYSCALL #566"); }     // ✓ 拿到返回值
 *     int ui_scr_w(void) { asm("SYSCALL #566"); return 0; }  // ✗ 返回值丢了
 *
 * 两条路都靠 `[R12-4]` 这个暂存槽中转（见下），所以下面那条"坑"对它也适用。
 *
 * ## ⚠ 唯一的坑：`R12-4`（**已在编译器侧修掉**）
 *
 * `asm("SYSCALL #…")` 执行完会把 R0 存回 `[R12-4]`，而局部变量分配器原来把
 * **第一个声明的局部变量**也放在 `R12-4`。两者撞车，症状是"第一条 asm 之前的那个变量，
 * 在后面的 asm 里读到的是上一条 syscall 的返回值" —— 表现就是字符串传过去是空的。
 *
 * 已修：C 前端现在先占住 R12-4，用户局部变量从 R12-8 起分配
 * （`patches/0002-c-asm-result-slot.patch`，并已同步进 VML 上游）。
 *
 * 万一你手上的编译器还没有这个修复，两条规避仍然有效：
 *   1. 先声明一个用不上的 `int _pad;` 占住 `R12-4`；
 *   2. **只用函数参数**（参数在 `R12+8` 往上，根本不在这个槽里）—— 本文件的做法。
 */

#ifndef WAYCODER_UI_H
#define WAYCODER_UI_H

/* ── 消息类型（与 UI/Shared/VmlUiProtocol.cs 的 VmlMsgType 一一对应）── */
#define VML_MSG_NONE          0
#define VML_MSG_KEYDOWN       1
#define VML_MSG_KEYUP         2
#define VML_MSG_MOUSEMOVE     3
#define VML_MSG_MOUSEDOWN     4
#define VML_MSG_MOUSEUP       5
#define VML_MSG_TOUCHDOWN     6
#define VML_MSG_TOUCHMOVE     7
#define VML_MSG_TOUCHUP       8
#define VML_MSG_TIMER         9
#define VML_MSG_WINDOWCLOSE  10
#define VML_MSG_WINDOWRESIZE 11

/* ── 键码（Win32 虚拟键值）── */
#define VML_KEY_BACKSPACE  8
#define VML_KEY_ENTER     13
#define VML_KEY_SELECT    16    /* 手柄 SELECT */
#define VML_KEY_PAUSE     19    /* 手柄 PAUSE */
#define VML_KEY_ESCAPE    27
#define VML_KEY_SPACE     32
#define VML_KEY_LEFT      37
#define VML_KEY_UP        38
#define VML_KEY_RIGHT     39
#define VML_KEY_DOWN      40
#define VML_KEY_PAD_A     65    /* 'A' */
#define VML_KEY_PAD_B     66    /* 'B' */
#define VML_KEY_PAD_X     88    /* 'X' */
#define VML_KEY_PAD_Y     89    /* 'Y' */

/* ── 对话框样式 ── */
#define VML_DLG_INFO     0
#define VML_DLG_WARN     1
#define VML_DLG_ERROR    2
#define VML_DLG_QUESTION 3

/* ═══════════════ 对话框 ═══════════════ */

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

/* ═══════════════ 窗体与绘图 ═══════════════
 * 保留模式：程序只管往窗口场景里追加图元，宿主负责按帧渲染。
 * 画完一批记得调一次 ui_present()（或者不调也行 —— 宿主定时器也会刷）。 */

/* 开一个绘图窗口（带标题栏与返回箭头）。返回句柄(1)，失败 -1。 */
int ui_win_open(char* title, int w, int h) {
    return asm("SYSCALL #520, ${title}, ${w}, ${h}");
}

int ui_win_close(void) {
    return asm("SYSCALL #521");
}

/* 可用绘图区（单位与绘图一致，可直接拿来算格子）。开窗后才有意义。 */
int ui_scr_w(void) {
    return asm("SYSCALL #566");
}

int ui_scr_h(void) {
    return asm("SYSCALL #567");
}

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

/* ── 文字 ──
 * 两种写法都提供，按场合挑：
 *   一次性：`ui_text(x, y, s, 颜色, 字号, 锚点)` —— 每行属性都不同时用
 *   状态式：`ui_set_font(字号, 样式, 颜色, 锚点)` + `ui_text_cur(x, y, s)` —— 一次设好连画很多行
 *
 * 文本按 **UTF-8** 编码（NUL 结尾）传给宿主；C 源码里直接写中文即可（源文件存成 UTF-8）。
 * `s` 只能是**变量或字符串字面量**，不能是表达式（`${}` 不认 `p->name`、`arr[i]` 这类）。 */

/* 文字样式位：可用 `VML_FONT_BOLD | VML_FONT_ITALIC` 组合 */
#define VML_FONT_BOLD    1
#define VML_FONT_ITALIC  2

/* 文字锚点 */
#define VML_ANCHOR_LEFT   0
#define VML_ANCHOR_CENTER 1
#define VML_ANCHOR_RIGHT  2

/* 一次性画一行字。anchor: 0=左 1=中 2=右（相对 x）。 */
void ui_text(int x, int y, char* s, int color, int size, int anchor) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}");
}

/* 一次性画一行字，**带粗体/斜体**（style 见上面的 VML_FONT_*）。 */
void ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style) {
    asm("SYSCALL #528, ${x}, ${y}, ${s}, ${color}, ${size}, ${anchor}, ${style}");
}

/* 设置当前文字属性：字号 / 样式位 / 颜色 / 锚点。之后用 ui_text_cur 画。 */
void ui_set_font(int size, int style, int color, int anchor) {
    asm("SYSCALL #532, ${size}, ${style}, ${color}, ${anchor}");
}

/* 用**当前文字属性**画一行字 —— 最常用的一行式写法：先 ui_set_font，之后只管 ui_text_cur(x,y,s)。 */
void ui_text_cur(int x, int y, char* s) {
    asm("SYSCALL #533, ${x}, ${y}, ${s}");
}

void ui_icon(int x, int y, char* name, int size, int color) {
    asm("SYSCALL #529, ${x}, ${y}, ${name}, ${size}, ${color}");
}

void ui_image(int x, int y, char* path, int w, int h) {
    asm("SYSCALL #530, ${x}, ${y}, ${path}, ${w}, ${h}");
}

/* 帧边界标记（本帧画完了）。 */
void ui_present(void) {
    asm("SYSCALL #531");
}

/* ═══════════════ 输入（统一消息队列）═══════════════
 * 键盘、鼠标、触摸、定时器、窗口事件**进同一个队列**，程序统一按
 * 「取消息 → 分派」写循环。
 *
 * 消息缓冲固定 **16 字节 = 4 个 int**（小端）：
 *     msg[0]=类型  msg[1]=A  msg[2]=B  msg[3]=时间戳
 * 触摸类消息：A=x，B=y（单位与绘图一致，可直接拿来算格子）。
 *
 * 用法：
 *     int msg[4];
 *     int t = ui_wait(msg, 0);
 *     if (t == VML_MSG_TOUCHDOWN) { x = msg[1]; y = msg[2]; }
 */

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

/* 定时器：每隔 interval_ms 往队列投一条 VML_MSG_TIMER（A=定时器 id，B=tag）。 */
int ui_timer_set(int interval_ms, int tag) {
    return asm("SYSCALL #563, ${interval_ms}, ${tag}");
}

int ui_timer_kill(int id) {
    return asm("SYSCALL #564, ${id}");
}

/* 用户点了返回箭头 → 1，程序主循环应退出。 */
int ui_win_closed(void) {
    return asm("SYSCALL #565");
}

#endif /* WAYCODER_UI_H */
