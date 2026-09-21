/* conio.h —— DOS / Turbo C 那套**文本控制台**接口（常用子集）
 *
 * ## 为什么先做它
 *
 * 按"老程序里出现得多不多"排的：GitHub 代码搜索里 C 文件含 `#include <conio.h>` 的有
 * **110,336** 个，比 `curses.h`(43,264) + `ncurses.h`(40,448) 加起来还多，是
 * `graphics.h`(8,336) 的 13 倍。用户定的规矩是「哪些多就优先哪些」。
 *
 * ## 语义（照 DOS 那一套，不是照 POSIX）
 *
 *   · 坐标是 **1 起**、左上角是 `gotoxy(1,1)`；`wherex()`/`wherey()` 也返回 1 起。
 *   · `clrscr()` 清成**当前背景色**（DOS 也是这样）。
 *   · 颜色是 **0–15**（`BLACK`…`WHITE`），`textattr()` 一个字节里高 4 位背景、低 4 位前景。
 *   · **没有 `refresh()`** —— DOS conio 是立即输出的，这里也是写完就发字节。
 *
 * ## 与 DOS 的差别（「设备相关：有限兼容」，写在明处）
 *
 *   · **没有光标闪烁的方块**：网格上没有"文本光标"这个概念。`wherex/wherey` 能问位置，
 *     但屏幕上不会出现一个闪烁的光标。
 *   · ⚠ **`getch()` 目前是"按行"的**：命令行页把 stdin **按行**交给程序（用户在输入框敲
 *     一行、按「运行」提交），拿不到"单键即时"的语义。`ch = getch();` 那种写法仍能跑，
 *     但**方向键**要用户自己敲字符键代替。要真单键 + 方向键，得让命令行页把按键
 *     **原样转发**给程序（待办，见 `conio.c` 末尾）。
 *   · ⚠ **只用 ASCII**：本实现按**单字节**一格处理，中文/框线那种 UTF-8 多字节字符
 *     会被拆成好几格（DOS 的 DBCS 是 2 列宽，要做得多一层宽度表）。
 *   · **不做** `_setcursortype` / `window()` / `textmode()` / 直接写显存（`0xB8000`）——
 *     那些是设备相关且本平台没有对应物。
 *
 * ## 底层：**只发 ANSI**，画进命令行页的字符网格（不弹窗）
 *
 * 命令行程序本来就跑在一个**字符界面**里 —— 命令行页那个 80×25 的网格，
 * `nyancat` 走的就是它。所以这里只做一件事：**产生一个真终端会产生的那些字节**
 * （光标定位 `ESC[{行};{列}H`、颜色 SGR、字符、清屏 `ESC[2J`、擦行尾 `ESC[K`），
 * 交给既有的那条链去渲染。
 *
 * ⚠ 第一版**走错过**：用 `ui_win_open`/`ui_rect`/`ui_text` 弹了一个绘图窗口。后果三重 ——
 *   与命令行页抢屏幕、窗口尺寸由宿主决定（"填满"成了无解的算术题）、
 *   而且与既有渲染链成了**两套实现**（本仓排第一的坑）。已整条改掉。
 *
 * 实现在 `Lib/shared/src/conio.c`：屏幕影子放普通数组（`delline`/`insline` 要改已画过的
 * 内容，而 DL/IL 本平台网格不支持），不做任何"把指针当整数算偏移"的读写。
 */
#ifndef _CONIO_H
#define _CONIO_H

#param lib("conio")

/* ── 颜色（低 4 位前景 / 高 4 位背景，与 DOS 同）── */
#define BLACK        0
#define BLUE         1
#define GREEN        2
#define CYAN         3
#define RED          4
#define MAGENTA      5
#define BROWN        6
#define LIGHTGRAY    7
#define DARKGRAY     8
#define LIGHTBLUE    9
#define LIGHTGREEN  10
#define LIGHTCYAN   11
#define LIGHTRED    12
#define LIGHTMAGENTA 13
#define YELLOW      14
#define WHITE       15

/* `textattr()` 字位（DOS 的 BLINK 位在本平台无对应物，保留名字只为源码兼容） */
#define BLINK       128

/* ── 屏幕与光标 ── */
void clrscr(void);                 /* 清屏（清成当前背景色） */
void clreol(void);                 /* 从光标擦到行尾 */
void gotoxy(int x, int y);         /* 定位（**1 起**；越界自动钳制） */
int  wherex(void);                 /* 当前列（1 起） */
int  wherey(void);                 /* 当前行（1 起） */

/* ── 颜色 ── */
void textcolor(int color);
void textbackground(int color);
void textattr(int attr);           /* 一个字节设前景+背景 */
void highvideo(void);              /* 前景加亮（+8） */
void lowvideo(void);               /* 前景去掉加亮 */
void normvideo(void);              /* 回到"黑底浅灰" */

/* ── 输出 ── */
void putch(int c);                 /* 打一个字符（会推进光标） */
void cputs(const char *s);         /* 打一个 NUL 结尾串 */
void cprintf(const char *fmt, ...);/* 格式化输出（走与 printf 同一套格式化） */

/* ── 行操作 ── */
void delline(void);                /* 删掉光标所在行，下面的上移 */
void insline(void);                /* 在光标行插入一空行，下面下移 */

/* ── 键盘 ── */
int  kbhit(void);                  /* 有没有按键（**不取走**） */
int  getch(void);                  /* 等一个按键（方向键：先返回 0，再返回扫描码） */
int  getche(void);                 /* 同上，且回显到屏幕（DOS 语义） */

/* ── 屏幕尺寸（DOS 的 80×25；本平台固定这个尺寸，见下面说明）── */
#define CON_ROWS 25
#define CON_COLS 80

#endif /* _CONIO_H */
