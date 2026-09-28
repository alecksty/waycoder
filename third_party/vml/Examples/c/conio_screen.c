/* conio_screen.c —— 一屏 **DOS / Turbo C 风格**的文本界面（`conio.h` 的验收程序）
 * conio_screen.c — one screen of a **DOS / Turbo C style** text UI (the acceptance program for `conio.h`)
 *
 * 为什么拿它当验收：`conio` 是老 DOS 程序里出现最多的那一套（GitHub 上含
 * Why this one is the acceptance test: `conio` is the set that appears most in old DOS programs (on GitHub
 * `#include <conio.h>` 的 C 文件 11 万个，比 curses 两个头加起来还多）。
 * there are 110k C files containing `#include <conio.h>`, more than both curses headers combined).
 * 它要证明的不是"某几个函数能调通"，而是**一整套坐标系能撑起一个真界面**：
 * What it has to prove is not that a few functions can be called, but that **a whole coordinate system can hold up a real UI**:
 * 坐标 1 起、底色/前景/亮色的组合、边框对齐、局部重画。
 * coordinates starting at 1, combinations of background/foreground/bright, box alignment, partial redraw.
 *
 * ⚠ **它画进命令行页的字符网格**（就是 `nyancat` 跑的那个界面），**不弹窗**：
 * ⚠ **It draws into the command-line page's character grid** (the very UI `nyancat` runs in) and **opens no window**:
 *   `conio` 只发 ANSI（光标定位 + SGR + 字符），渲染交给命令行页既有的那条链。
 *   `conio` only emits ANSI (cursor positioning + SGR + characters); rendering is left to the command-line page's existing chain.
 *
 * ⚠ **它不等按键**（画完就退出）。原因说清楚：命令行页目前是"程序跑完才把输出
 * ⚠ **It does not wait for a key press** (it exits as soon as it has drawn). The reason, stated plainly: the command-line page
 * currently only takes the output up for rendering after the program has finished, so
 *   交上来渲染"，所以"画一屏 → 等按键 → 重画"这种交互现在还看不到中间态
 *   an interaction like "draw a screen -> wait for a key -> redraw" still shows no intermediate state today
 *   （要做得让命令行页**边跑边渲染**，见 conio.c 末尾的待办）。
 *   (that would require the command-line page to **render while the program runs**; see the TODO at the end of conio.c).
 *
 * 跑法：命令行页输入  vml run examples/c/conio_screen.c
 * How to run: type  vml run examples/c/conio_screen.c  on the command-line page
 */
#include <conio.h>

#define MENU_N 4

char *g_items[MENU_N];

static void draw_frame(void)
{
    int i;

    /* 标题栏：青底黑字，铺满第 1 行 */
    /* title bar: black text on cyan, filling row 1 */
    textbackground(CYAN);
    textcolor(BLACK);
    gotoxy(1, 1);
    for (i = 0; i < 80; i++) putch(' ');
    gotoxy(3, 1);
    cputs("WayCoder  conio.h  demo   --   DOS text console");

    /* 外框（ASCII 的 +-|；见下面那条"只用 ASCII"的说明） */
    /* outer box (ASCII +-|; see the "ASCII only" note below) */
    textbackground(BLUE);
    textcolor(LIGHTGRAY);
    gotoxy(20, 5);
    cputs("+----------------+");
    for (i = 0; i < 6; i++) {
        gotoxy(20, 6 + i);
        cputs("|                |");
    }
    gotoxy(20, 12);
    cputs("+----------------+");

    /* 菜单项：选中项反白（浅灰底黑字）—— DOS 菜单的标准长相 */
    /* menu items: the selected one is reversed (black on light gray) — the standard look of a DOS menu */
    for (i = 0; i < MENU_N; i++) {
        gotoxy(21, 6 + i);
        if (i == 1) {
            textbackground(LIGHTGRAY);
            textcolor(BLACK);
        } else {
            textbackground(BLUE);
            textcolor(WHITE);
        }
        cputs(g_items[i]);
    }

    /* 底部状态行：黄字（亮色档） */
    /* bottom status line: yellow text (the bright tier of colours) */
    textbackground(BLUE);
    textcolor(YELLOW);
    gotoxy(20, 14);
    cputs("Up/Down to move, Enter to pick");

    /* 右下角：16 色色块一条 —— 一条就能看出"亮色档"有没有对上 */
    /* bottom right: a strip of 16 colour patches — one strip shows whether the bright tier lines up */
    textcolor(BLACK);
    for (i = 0; i < 16; i++) {
        textbackground(i);
        gotoxy(21 + i * 2, 16);
        cputs("  ");
    }

    textbackground(BLUE);
    textcolor(WHITE);
    gotoxy(20, 18);
    cprintf("screen: %dx%d   cursor: %d,%d", 80, 25, wherex(), wherey());

    gotoxy(1, 25);
    textbackground(BLACK);
    textcolor(LIGHTGRAY);
    cputs("done.");
}

int main()
{
    g_items[0] = "New             ";
    g_items[1] = "Open            ";
    g_items[2] = "Save            ";
    g_items[3] = "Quit            ";

    textbackground(BLUE);
    textcolor(WHITE);
    clrscr();

    draw_frame();
    return 0;
}
