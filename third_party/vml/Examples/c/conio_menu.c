/* conio_menu.c —— 一个**DOS / Turbo C 风格**的文本菜单（`conio.h` 的验收程序）
 *
 * 为什么拿它当验收：`conio` 是老 DOS 程序里出现最多的那一套（GitHub 上含
 * `#include <conio.h>` 的 C 文件 11 万个，比 curses 两个头加起来还多）。
 * 它要证明的不是"某几个函数能调通"，而是**一整套坐标系与按键约定能撑起一个真界面**：
 *
 *   · 坐标 1 起、`gotoxy(1,1)` 是左上角 —— 差一格，整个边框就错位（而且不报错）
 *   · 底色 + 前景 + 亮色的组合（DOS 那 16 色）
 *   · `getch()` 的**扩展键**约定：方向键先返回 0、下一次才是扫描码（72 上 / 80 下）
 *   · 局部重画：只改菜单那几行，标题栏与边框不动（老机器的做法，也是本实现的做法）
 *
 * 跑法：命令行页输入  vml run examples/c/conio_menu.c
 * 操作：屏幕手柄的方向键上下选，SELECT / 回车确认。
 *
 * ⚠ **只用 ASCII**：本平台的 `conio` 目前按**单字节**一格处理，中文/框线字符
 *   （UTF-8 多字节）会被拆成好几格。这是已知限制，写在 `Lib/c/conio.h` 的说明里。
 */
#include <conio.h>

#define MENU_N 4

char *g_items[MENU_N];

static void draw_frame(void)
{
    int i;

    /* 标题栏（青底黑字，铺满第一行） */
    textbackground(CYAN);
    textcolor(BLACK);
    gotoxy(1, 1);
    for (i = 0; i < 80; i++) putch(' ');
    gotoxy(3, 1);
    cputs("WayCoder conio.h demo  --  DOS text console");

    /* 外框（ASCII 的 +-|，见文件头那条说明） */
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

    /* 底部提示行 */
    gotoxy(20, 14);
    textcolor(YELLOW);
    cputs("Up/Down to move, Enter to pick");
}

static void draw_items(int sel)
{
    int i;

    for (i = 0; i < MENU_N; i++) {
        gotoxy(21, 6 + i);
        if (i == sel) {
            textbackground(LIGHTGRAY);
            textcolor(BLACK);
        } else {
            textbackground(BLUE);
            textcolor(WHITE);
        }
        cputs(g_items[i]);
    }
    textbackground(BLUE);
}

static void show_pick(int sel)
{
    gotoxy(20, 16);
    textbackground(BLACK);
    textcolor(LIGHTGREEN);
    cprintf("picked: %s   ", g_items[sel]);
    textbackground(BLUE);
    textcolor(WHITE);
}

int main()
{
    int sel;
    int key;

    g_items[0] = "New             ";
    g_items[1] = "Open            ";
    g_items[2] = "Save            ";
    g_items[3] = "Quit            ";

    textbackground(BLUE);
    textcolor(WHITE);
    clrscr();

    draw_frame();
    sel = 0;
    draw_items(sel);

    while (1) {
        key = getch();
        if (key == 0) key = getch();          /* 扩展键：取扫描码 */

        if (key == 72) {                      /* 上 */
            if (sel > 0) sel = sel - 1;
        } else if (key == 80) {               /* 下 */
            if (sel < MENU_N - 1) sel = sel + 1;
        } else if (key == 13 || key == 28) {  /* 回车 / 手柄 SELECT */
            show_pick(sel);
            if (sel == MENU_N - 1) break;     /* 选到 Quit 就退出 */
            continue;
        } else if (key == 27) {               /* Esc */
            break;
        }

        draw_items(sel);
    }

    textbackground(BLACK);
    textcolor(LIGHTGRAY);
    clrscr();
    gotoxy(1, 1);
    cputs("bye.");
    return 0;
}
