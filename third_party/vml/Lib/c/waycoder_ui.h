/* waycoder_ui.h —— 手机端 UI syscall 的 C 语言**声明**（对话框 / 窗体 / 绘图 / 输入）
 *
 * ## 实现在哪
 *
 * 函数体在 **`Lib/shared/src/vmlui.c`**，编成 `Lib/shared/vmlui.vml` 后由
 * `vmltool.config.xml` 挂到各语言的 `Libs` 上 —— **C 与 Python 等前端共用同一份实现**。
 * 本头文件只提供声明与常量，`#include` 它不会让程序变大。
 *
 * （早先这层是写在头文件里自带实现的。挪出去的原因：Python 等前端**不支持内联 asm**，
 *   只能按标签调 C 函数 —— 实现留在 `.h` 里，那些语言就一个都调不到，
 *   于是同一套接口会被迫各写一遍。那是本仓库排第一的坑。）
 *
 * ## 号段
 * 500–599 是**宿主 UI 保留段**，只有手机 App（WayCoder）里能用；桌面 vmltool 不认识
 * 这些号。号段表见 `Lib/README-ui.md`。
 *
 * ## 用法（C）
 *
 *     #include <waycoder_ui.h>
 *
 *     int main(void) {
 *         int msg[4];
 *         ui_win_open("演示", 360, 620);
 *         ui_clear(0xFF101020);
 *         ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
 *         ui_text_cur(180, 40, "按方向键退出");
 *         ui_present();
 *         while (ui_win_closed() == 0) {
 *             if (ui_wait(msg, 0) != VML_MSG_TOUCHDOWN) continue;   // msg[1]=x msg[2]=y
 *         }
 *         return 0;
 *     }
 *
 * 文本按 **UTF-8**（NUL 结尾）传给宿主，源文件存成 UTF-8 即可直接写中文。
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

/* ── 键码（Win32 虚拟键值；手柄那一排见 VmlKeys）── */
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

/* ── 文字样式位 / 锚点 ── */
#define VML_FONT_BOLD    1
#define VML_FONT_ITALIC  2
#define VML_ANCHOR_LEFT   0
#define VML_ANCHOR_CENTER 1
#define VML_ANCHOR_RIGHT  2

/* ── 对话框 ── */
int  ui_dlg_msg(char* title, char* body, int style);
int  ui_dlg_select(char* title, char* body, char* opts, int n, int def);
int  ui_dlg_multi(char* title, char* body, char* opts, int n);
int  ui_dlg_input(char* title, char* prompt, char* buf, int cap);

/* ── 窗体 ── */
int  ui_win_open(char* title, int w, int h);
int  ui_win_close(void);
int  ui_win_closed(void);
int  ui_scr_w(void);
int  ui_scr_h(void);

/* ── 绘图 ── */
void ui_clear(int color);
void ui_pixel(int x, int y, int color);
void ui_line(int x1, int y1, int x2, int y2, int color, int lw);
void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
void ui_circle(int cx, int cy, int r, int color, int fill, int lw);
void ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw);
void ui_icon(int x, int y, char* name, int size, int color);
void ui_image(int x, int y, char* path, int w, int h);
void ui_present(void);

/* ── 文字 ── */
void ui_text(int x, int y, char* s, int color, int size, int anchor);
void ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style);
void ui_set_font(int size, int style, int color, int anchor);
void ui_text_cur(int x, int y, char* s);

/* ── 不碰指针的消息读取 + 通用整数网格（非 C 语言用，C 也能用）── */
int  ui_wait_msg(int timeout_ms);
int  ui_poll_msg(void);
int  ui_msg_type(void);
int  ui_msg_a(void);
int  ui_msg_b(void);
void ui_gclear(void);
void ui_piece_init(void);
int  ui_piece_cell(int pid, int rot, int which);
void ui_gset(int idx, int val);
int  ui_gget(int idx);

/* ── 输入 ── */
int  ui_poll(int* msg);
int  ui_wait(int* msg, int timeout_ms);
int  ui_msg_count(void);
int  ui_timer_set(int interval_ms, int tag);
int  ui_timer_kill(int id);

/* ── 随机数 / 计时（游戏用；实现走 VM 的 #50/#53，各语言共用一份）── */
int  ui_rand(int n);      /* 0..n-1 */
int  ui_tick(void);       /* VM 启动至今毫秒 */

#endif /* WAYCODER_UI_H */
