/* tty.h —— **彩色命令行**库的 C 语言**声明**（给 22 门语言共用的那一套接口）
 *
 * ## 实现在哪
 *
 * 函数体在 **`Lib/shared/src/tty.c`**，编成 `Lib/shared/tty.vml`
 * （它自己再链 `conio.vml` + `vmlui.vml`）。本头文件只提供声明与常量，
 * `#include` 它**不会让程序变大**。
 *
 * ## 它是什么 / 不是什么
 *
 * **是**：字符界面的库 —— 定位、上色、写字、画框。**只有字符，没有任何图形功能。**
 *   要画图请用别的层：传统固定分辨率图形 = BGI（`Lib/c/graphics.h`），
 *   现代图元 = `ui_*`（`Lib/c/waycoder_ui.h`）。
 *
 * **不是**：Turbo C 的 `conio`（那是 `Lib/c/conio.h` 一系）、也不是 Turbo Pascal 的
 *   `Crt`（`Lib/pascal/crt.pas`）。那两套是**兼容层**，保留给老程序；
 *   `tty_*` 是这套能力的**正式名字**，22 门语言写出来的程序用同一串调用。
 *
 * ## 两块屏
 *
 * · **主屏**（默认）—— 命令行页那个 80×25 网格。只发 ANSI 字节
 *   （`ESC[{行};{列}H` / `ESC[{码}m` / `ESC[2J` / `ESC[K`），走既有的渲染链。
 * · **副屏**（`tty_alt_open`）—— **弹一扇窗口**当终端。内容照旧是**字符网格**
 *   （一格 8×16 像素，80×25 ⇒ 640×400），只是换了一块地方显示。
 *
 * ## 坐标与颜色（**1 起算**）
 *
 * · 坐标 **1-based**：`(1,1)` 左上角 —— 与 `GotoXY`(Pascal) / `gotoxy`(Turbo C) /
 *   `LOCATE`(BASIC) 一致。**不是** 0-based。
 * · 颜色 **0–15 索引色**（CGA/VGA 标准表）：
 *   `0 黑 1 蓝 2 绿 3 青 4 红 5 品红 6 棕 7 浅灰 8 深灰 9 亮蓝 10 亮绿
 *    11 亮青 12 亮红 13 亮洋红 14 黄 15 白`
 *
 * ## 最小用法
 *
 *     #include <tty.h>
 *
 *     int main(void) {
 *         tty_init(0, 0, 0);              // 自适应尺寸、不清屏
 *         tty_color(14, 1);               // 黄字 / 蓝底
 *         tty_goto(10, 5);
 *         tty_puts("Hello, TTY!");
 *         tty_box(5, 3, 40, 10, 1);       // 单线框
 *         tty_color(7, 0);
 *         tty_goto(1, 24);
 *         tty_puts("按任意键继续");
 *         tty_wait();
 *         return 0;
 *     }
 */

/* ⚠ 本头文件要能被 **Objective-C** 源文件 `#include` —— 所以形式参数**不能叫 `id`**
 *   （`id` 在 ObjC 里是保留的**类型名**）。原型里的形参名对 C 没有语义，改名零风险。
 *   同样的坑在 `waycoder_ui.h` 上踩过一次。 */

#ifndef TTY_H
#define TTY_H

/* ⚠ **这一行是必须的** —— 它是"用到本头文件的程序要链上 `tty.vml`"的声明。
 *   少了它，`tty_*` 的调用会在**链接期**报「未定义的函数 'tty_init'」，
 *   而那个报错看起来像"函数名拼错了"，很容易往错的方向查
 *   （`Lib/c/conio.h` 里同样有一句 `#param lib("conio")`）。
 *   ⚠ **C++ 前端不处理 C 头文件里的 `#param`**（已知缺陷，见 `Lib/c/graphics.h` 那段），
 *     C++ 程序请在自己源码顶部显式写 `#param lib("tty")`。 */
#param lib("tty")

/* ── 初始化 / 副屏 ─────────────────────────────────────────────── */

/* 初始化**主屏**（命令行页）。
 * `tty_init(0, 0, 0)` —— 最常用的那一句：尺寸**自适应**（不指定大小）、**不清屏**。
 * 给了正数则按它钳制（上限 80×25）；`clear` 非 0 则清屏。返回 0。 */
int  tty_init(int width, int height, int clear);

/* 打开**副屏**：弹一扇 (w × h) 个字符格的窗口当终端。
 * `tty_alt_open(0, 0, "标题", 0)` —— 自适应窗口（按宿主可画区算行列，上限 80×25）。
 * 副屏开出来必然是空白的 ⇒ `clear` 在这里不起作用（留着只为与 `tty_init` 对齐）。
 * 返回 0 成功、**-1 打不开**（宿主不支持开窗）—— 失败就别当副屏用。 */
int  tty_alt_open(int width, int height, char* title, int clear);

/* 关副屏、回到主屏。未开副屏时是空操作。 */
void tty_alt_close(void);

/* 当前是不是副屏（1/0）。 */
int  tty_alt_is_open(void);

/* 逻辑尺寸（列数 / 行数）。 */
int  tty_width(void);
int  tty_height(void);

/* ── 清屏 / 擦行 ──────────────────────────────────────────────── */

void tty_cls(void);       /* 清屏并把光标归到左上角 */
void tty_clreol(void);    /* 擦到本行行尾 */

/* ── 光标（1 起算）────────────────────────────────────────────── */

void tty_goto(int x, int y);
int  tty_wherex(void);
int  tty_wherey(void);

/* ── 颜色（0–15 索引色）───────────────────────────────────────── */

void tty_color(int fg, int bg);
void tty_attr(int attr);       /* DOS 属性字节 `bg<<4 | fg` */
int  tty_getfg(void);
int  tty_getbg(void);

/* ── 输出 ─────────────────────────────────────────────────────── */

void tty_putc(int ch);
void tty_puts(char* s);
void tty_put_int(int v);                                    /* 十进制，负数带 '-' */
void tty_print_at(int x, int y, char* s, int fg, int bg);   /* 打完恢复 (7,0) */

/* ── 便利图元（都是"发字符"，**不是绘图**）────────────────────── */

void tty_hline(int x, int y, int len, int ch);
void tty_vline(int x, int y, int len, int ch);
/* 边框。style: 0 = ASCII(`+ - |`)，1 = 单线框(UTF-8 `┌ ─ │ ┐ └ ┘`)。 */
void tty_box(int x1, int y1, int x2, int y2, int style);

/* ── 刷新与输入 ───────────────────────────────────────────────── */

/* 把副屏的网格刷到窗口上。**主屏不需要调**（它一个字符一个字符直接就发出去了）。
 * 副屏则要在一批输出之后调一次，否则屏幕不会更新。 */
void tty_refresh(void);

/* 有按键返回它的编码、没有返回 -1（**不阻塞**）。副屏窗口被关掉时返回 -1。 */
int  tty_key(void);
/* 等一个按键（**阻塞**）。返回 -1 = 窗口关了（主屏不会返 -1）。 */
int  tty_wait(void);
/* 副屏窗口是不是被用户关掉了（主屏恒 0）。 */
int  tty_closed(void);

#endif /* TTY_H */
