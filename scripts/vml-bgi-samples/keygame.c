/* keygame.c —— 用**纯 BGI 接口**写的小游戏，专门验"老程序在手机上能不能玩"。
 *
 * 为什么要有它：.scratch/bgi/real 那批（pie/Hut/barChart/Concentric）只验了**画面**，
 * 它们调 `getch()` 只是为了"画完停住"，拿到什么值根本不管。
 * **输入这条链一直没验过** —— 而它恰好有硬伤（见 `graphics.h` 的 `getch` 注释：
 * 缓冲区给少了踩栈 + 返回的是消息类型而不是按键）。
 *
 * 写法刻意照 **Turbo C 老程序的习惯**：
 *   · 只用 `graphics.h` / `conio.h` 的接口，不碰 `ui_*`（那些是本平台的扩展）；
 *   · 画完就 `getch()`，靠垫层在 `getch()` 里自动 `present`（BGI 就是这样"立即出图"）；
 *   · **方向键按 DOS 那套两段式处理**：先收到 0，再收到扫描码（0x48/0x50/0x4B/0x4D）。
 *     这一条是重点 —— 平台发的是 Win32 虚拟键，翻译在垫层里做。
 *
 * ⚠ 刻意**不用 `sprintf`**：老库里 `%` 转换那条路有历史问题，别把"输入坏了"和
 *   "格式化坏了"混在一起测。分数用一根**变长的蓝条**表示，一个字节的格式化都不用。
 */
#include <graphics.h>
#include <conio.h>

int main(void)
{
    int gd = DETECT, gm;
    int x = 320, y = 240;
    int score = 0;
    int ch;
    int i;

    initgraph(&gd, &gm, NULL);

    for (;;)
    {
        cleardevice();

        /* 边框 */
        setcolor(WHITE);
        rectangle(2, 2, 637, 477);

        /* 分数：一根随按键变长的蓝条（避开 sprintf，见文件头） */
        setfillstyle(SOLID_FILL, BLUE);
        bar(10, 10, 10 + score * 10, 26);
        setcolor(WHITE);
        rectangle(10, 10, 210, 26);

        /* 红方块：方向键移动它 */
        setfillstyle(SOLID_FILL, RED);
        bar(x - 20, y - 20, x + 20, y + 20);

        /* 静态说明用 outtextxy（不格式化，安全） */
        setcolor(YELLOW);
        outtextxy(12, 40, "arrow keys move the red block, q quits");

        /* 画完等一个键 —— 垫层在 getch() 里自动 present */
        ch = getch();

        if (ch == 'q' || ch == 'Q') break;

        if (ch == 0)
        {
            /* 扩展键：**第二次** getch 才是扫描码（DOS 约定） */
            ch = getch();
            if (ch == 0x4B) { x = x - 40; score = score + 1; }   /* ← */
            else if (ch == 0x4D) { x = x + 40; score = score + 1; }  /* → */
            else if (ch == 0x48) { y = y - 40; score = score + 1; }  /* ↑ */
            else if (ch == 0x50) { y = y + 40; score = score + 1; }  /* ↓ */
        }
        else
        {
            /* 普通键（A–Z/0–9 的虚拟键码就是 ASCII）：按一下也算一次 */
            score = score + 1;
        }

        /* 别让方块跑出边框 —— 顺便证明"程序真的读到了方向" */
        if (x < 40) x = 40;
        if (x > 600) x = 600;
        if (y < 60) y = 60;
        if (y > 440) y = 440;
    }

    closegraph();

    /* 收尾把分数打到 stdout：命令行页上能直接看到"玩了几步" */
    print_str("moves=");
    print_int(score);
    print_str("\n");
    return 0;
}
