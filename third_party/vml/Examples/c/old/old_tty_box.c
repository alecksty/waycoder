/* old_tty_box.c —— 用框线字符拼界面（彩色的 tty）
 * old_tty_box.c -- building an interface out of box-drawing characters (color tty)
 *
 * 类别：tty
 * Category: tty
 * 兼容面：**扩展 ASCII / CP437 框线字符**（`┌─┐│└┘` 这类，老 DOS 界面的骨架）、
 * Compatibility: **extended ASCII / CP437 box-drawing characters** (things like `┌─┐│└┘`, the skeleton of old DOS interfaces),
 *         ANSI 前景/背景色、光标定位
 *         ANSI foreground/background colors, and cursor positioning
 * 出处：自写，仿 Turbo C / QBasic 时代那种单线框菜单。
 * Origin: self-written, imitating the single-line box menus of the Turbo C / QBasic era.
 */
#include <stdio.h>
#include <waycoder_ui.h>     /* 只为 ui_get_language() —— 老程序不该猜系统语言 */
                            /* only for ui_get_language() -- an old program must not guess the system language */

#define ESC "\033"

static void at(int y, int x) { printf(ESC "[%d;%dH", y, x); }

/* 画一个单线框（宽 w、高 h，左上角在 y,x）—— 老程序里这是最常手写的一段 */
/* Draw a single-line box (width w, height h, top-left at y,x) -- the piece old programs hand-wrote most often */
static void box(int y, int x, int w, int h, int color)
{
    int i;

    printf(ESC "[%dm", color);
    at(y, x);         printf("┌");
    at(y, x + w - 1); printf("┐");
    at(y + h - 1, x); printf("└");
    at(y + h - 1, x + w - 1); printf("┘");

    for (i = 1; i < w - 1; i++) {
        at(y, x + i);         printf("─");
        at(y + h - 1, x + i); printf("─");
    }
    for (i = 1; i < h - 1; i++) {
        at(y + i, x);         printf("│");
        at(y + i, x + w - 1); printf("│");
    }
    printf(ESC "[0m");
}

int main(void)
{
    int lang;   /* 界面语言：开局查一次 */
                /* UI language: queried once at start */

    lang = ui_get_language();

    printf(ESC "[2J");

    box(2, 4, 34, 9, 36);          /* 青边框 */
                                   /* Cyan outer frame */
    box(3, 6, 30, 7, 33);          /* 黄内框 */
                                   /* Yellow inner frame */

    printf(ESC "[1;33m");
    at(4, 12); printf(lang == 0 ? "主 菜 单" : "MAIN MENU");
    printf(ESC "[0m");

    printf(ESC "[37m");
    at(6, 9);  printf(lang == 0 ? "1. 新建文件" : "1. New file");
    at(7, 9);  printf(lang == 0 ? "2. 打开文件" : "2. Open file");
    printf(ESC "[7m");             /* 反白 = 当前选中项 */
                                   /* Reverse video = the currently selected item */
    at(8, 9);  printf(lang == 0 ? "3. 退出      " : "3. Quit      ");
    printf(ESC "[0m");

    printf(ESC "[36m");
    at(13, 4); printf(lang == 0 ? "F1=帮助  ESC=返回  ↑↓=选择" : "F1=Help  ESC=Back  ↑↓=Select");
    printf(ESC "[0m");

    at(15, 1);
    return 0;
}
