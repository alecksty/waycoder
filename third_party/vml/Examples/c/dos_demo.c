/* dos_demo.c —— `dos.h` 的验收程序（Turbo C / MS-DOS 那套）
 *
 * 跑法：命令行页输入  vml run examples/c/dos_demo.c
 *
 * ## 它验什么
 *
 * `dos.h` 里的函数**几乎全是 `void`** —— 没有返回值可判，只能看**副作用**：
 *   · `delay` / `sleep` 的副作用是"时间真的过去了"
 *   · `getdate` / `gettime` 是"把真实时钟写进结构体"
 *   · `sound` / `nosound` 是"**喇叭真的响**"
 *
 * 前两条在桌面探针里已经判死了（`scripts/vml-c-probe/cases/18-dos.c`）——
 * 这里是把它们**画出来**，并且**补上桌面验不了的那条：音效**。
 * 桌面宿主只记日志、不出声（`CliVmlHost.Tone`），所以音效**只能在手机/真机上听**。
 *
 * ## ⚠ 关于 `sound()` 的时长（与 DOS 的差别，听的时候心里有数）
 *
 * DOS 的 `sound(f)` 是「一直响到 `nosound()`」；本平台是「响固定时长，上限 5 秒」。
 * 下面每一段都在 5 秒内 `nosound()`，所以听感与 DOS **一致**；
 * 只有"`sound()` 之后不关"的写法才会在 5 秒后自己停。
 */
#include <conio.h>
#include <dos.h>

/* 画一条进度条 —— 用 conio 画，顺便把两个头文件**一起用**（DOS 程序的常态） */
static void bar(int y, int percent)
{
    int i;
    gotoxy(3, y);
    cprintf("[");
    for (i = 0; i < 40; i++) {
        if (i * 100 / 40 < percent) cprintf("#");
        else                        cprintf(".");
    }
    cprintf("] %d%%   ", percent);
}

int main()
{
    struct date d;
    struct time t;
    int i;

    textbackground(BLUE);
    textcolor(WHITE);
    clrscr();

    /* ── 标题 ── */
    textbackground(CYAN);
    textcolor(BLACK);
    gotoxy(1, 1);
    for (i = 0; i < 80; i++) putch(' ');
    gotoxy(3, 1);
    cputs("  dos.h  demo   --   Turbo C / MS-DOS compatibility  ");

    /* ── 真实时钟 ── */
    textbackground(BLUE);
    textcolor(YELLOW);
    getdate(&d);
    gettime(&t);
    gotoxy(3, 3);
    cprintf("date: %d-%d-%d    time: %d:%d:%d",
            d.da_year, d.da_mon, d.da_day,
            t.ti_hour, t.ti_min, t.ti_sec);

    textcolor(LIGHTGRAY);
    gotoxy(3, 4);
    cprintf("DosVersion: %d (want 1792 = 0x700)", DosVersion());

    /* ── 延时 + 进度条（`delay` 的副作用看得见）── */
    textcolor(WHITE);
    gotoxy(3, 6);
    cputs("delay() progress:");
    for (i = 0; i <= 100; i += 10) {
        bar(7, i);
        delay(80);              /* 走完全程约 0.8 秒 */
    }

    /* ── 音阶：`sound` / `nosound` 的副作用听得见 ──
       升调 C-D-E-G-A，每段 150ms —— 手机喇叭上是"哔 哔 哔 哔 哔"。 */
    textcolor(LIGHTGREEN);
    gotoxy(3, 9);
    cputs("sound() scale (listen!):");
    {
        int freqs[5];
        freqs[0] = 262;  freqs[1] = 294;  freqs[2] = 330;
        freqs[3] = 392;  freqs[4] = 440;
        gotoxy(3, 10);
        for (i = 0; i < 5; i++) {
            cprintf("%d Hz  ", freqs[i]);
            sound(freqs[i]);    /* 开始响 */
            delay(150);
            nosound();          /* 掐掉 */
            delay(40);          /* 留个缝，听得清是"哔哔哔"而不是一声长音 */
        }
    }

    /* ── 收尾 ── */
    textcolor(YELLOW);
    gotoxy(3, 12);
    cprintf("done. sleep(1) ...");
    sleep(1);                   /* 秒级延时（`delay` 的包装） */

    gotoxy(3, 13);
    textcolor(LIGHTGRAY);
    cputs("(desktop build is silent -- run this on the phone to hear it)");

    gotoxy(1, 25);
    textbackground(BLACK);
    cputs("done.");

    return 0;
}
