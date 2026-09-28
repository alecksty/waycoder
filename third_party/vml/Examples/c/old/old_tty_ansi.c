/* old_tty_ansi.c —— ANSI 转义彩色界面（彩色的 tty）
 * old_tty_ansi.c -- a colored interface built from ANSI escapes (color tty)
 *
 * 类别：tty（彩色终端）
 * Category: tty (color terminal)
 * 兼容面：**八进制转义 `\033`**（老程序的头号写法，曾被解析成 NUL 的缺陷就是它）、
 * Compatibility: the **octal escape `\033`** (old programs' number one spelling, and the defect that used to parse it as NUL was exactly this one),
 *         SGR 前景/背景色、清屏、光标定位（`ESC[y;xH`）、粗体/反白
 *         SGR foreground/background colors, clearing the screen, cursor positioning (`ESC[y;xH`), bold and reverse video
 * 出处：自写，仿 90 年代 BBS / DOS 下 ANSI.SYS 那种全屏彩色菜单。
 * Origin: self-written, imitating the full-screen color menus of 1990s BBS systems and DOS ANSI.SYS.
 */
#include <stdio.h>

#define ESC "\033"          /* ⚠ 八进制转义 —— 这条本身就是兼容面 */
                            /* ⚠ Octal escape -- this line is itself a compatibility surface */

static void gotoxy(int y, int x) { printf(ESC "[%d;%dH", y, x); }

int main(void)
{
    int i;

    printf(ESC "[2J");                 /* 清屏 */
                                       /* Clear the screen */
    printf(ESC "[?25l");              /* 藏光标 */
                                      /* Hide the cursor */

    /* 标题：粗体 + 黄底蓝字 */
    /* Title: bold + blue text on a yellow background */
    gotoxy(2, 6);
    printf(ESC "[1;33;44m  WayCoder 老程序演示 · ANSI 彩色  " ESC "[0m");

    /* 调色板：16 色前景 */
    /* Palette: the 16 foreground colors */
    for (i = 0; i < 16; i++) {
        gotoxy(5 + (i / 8), 6 + (i % 8) * 5);
        if (i < 8) printf(ESC "[3%dm  %2d  " ESC "[0m", i, i);
        else       printf(ESC "[9%dm  %2d  " ESC "[0m", i - 8, i);
    }

    /* 反白条 + 下划线 + 闪烁（老终端那套属性） */
    /* A reverse-video bar + underline + blink (the old terminal attribute set) */
    gotoxy(9, 6);
    printf(ESC "[7m  反白 (reverse)  " ESC "[0m");
    gotoxy(10, 6);
    printf(ESC "[4m  下划线 (underline)  " ESC "[0m");
    gotoxy(11, 6);
    printf(ESC "[5m  闪烁 (blink)  " ESC "[0m");

    gotoxy(14, 6);
    printf("按任意键继续……（本示例不读键，直接结束）");
    gotoxy(16, 1);
    printf(ESC "[?25h");              /* 还光标 */
                                      /* Restore the cursor */
    return 0;
}
