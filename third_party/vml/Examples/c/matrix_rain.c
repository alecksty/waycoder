/* matrix_rain.c —— 字符矩阵雨
 *
 * ## 这是什么、为什么自己写
 *
 * 名字最像的那个程序（`cmatrix`）是 **GPL-3.0**（Chris Allegretta 等），
 * 而 `Examples/` 会随 APK 分发 ⇒ **不能直接收进来**。按本仓既定的那条路走
 * （同 `basic/gorilla.bas`）：**只参考玩法规则，代码自己写** ——
 * 结构、变量名、注释、函数划分都不抄。
 *
 * "玩法规则"就三条，够描述清楚了：
 *   ① 屏幕按列划分，每列有一串往下落的字符；
 *   ② 串的头部亮、越往后越暗（本平台 conio 只有 16 色，所以用"亮绿 / 暗绿"两档）；
 *   ③ 掉出屏幕底部后从顶部重新开始，`kbhit()` 一有按键就结束。
 *
 * ## 用哪一层画
 *
 * 走 `conio.h`（DOS 那套：**坐标 1 起、写完立即上屏**）——
 * 它不需要 `refresh()`，代码最短，也正好压一压"老程序兼容"这条线的另一组接口。
 *
 * ⚠ 坐标必须自己夹在 1..COLS / 1..ROWS 之内：本平台**不做**边界裁剪，
 *   越界写会画到别的行上去（不报错，只是画面花）。
 */

#include <conio.h>

#define COLS 80
#define ROWS 25
#define DROPS 40          /* 同时下落的串数 */
#define GLYPHS 40         /* 字符集大小：'!' 起往后取 40 个 */

/* 每条串的状态 */
int drop_col[DROPS];
int drop_head[DROPS];    /* 头部所在行（0 = 还没进屏幕） */
int drop_tail[DROPS];    /* 尾巴长度 */
int drop_gap[DROPS];     /* 连下几拍（让串有快有慢） */

/* 自带的伪随机：**不用库里的 `rand`** —— 一来本平台 `rand` 曾长期只有声明没有实现，
   二来自己带一个 LCG 才能保证"同一台机器两次跑起来的序列可复现"（排查画面问题时省事）。 */
unsigned int rng_state = 20260923;
static int rnd(int n)
{
    rng_state = rng_state * 1103515245 + 12345;
    return (int)((rng_state >> 16) % (unsigned int)n);
}

static void drop_reset(int i)
{
    drop_col[i]  = 1 + rnd(COLS);
    drop_head[i] = 0 - rnd(ROWS);      /* 负数 = 还在屏幕上方等着 */
    drop_tail[i] = 4 + rnd(10);
    drop_gap[i]  = rnd(3);
}

int main(void)
{
    int i;
    int frame;

    clrscr();
    for (i = 0; i < DROPS; i = i + 1)
        drop_reset(i);

    /* 跑够一段时间就自己收尾（手机上没键盘时不会永远占着） */
    for (frame = 0; frame < 600; frame = frame + 1) {
        if (kbhit())
            break;

        for (i = 0; i < DROPS; i = i + 1) {
            int y;

            if (drop_gap[i] > 0) {          /* 这一拍不落 */
                drop_gap[i] = drop_gap[i] - 1;
                continue;
            }
            drop_gap[i] = rnd(3);

            /* 把尾巴最上面那一格擦掉 */
            y = drop_head[i] - drop_tail[i];
            if (y >= 1 && y <= ROWS) {
                gotoxy(drop_col[i], y);
                textcolor(2);               /* 暗绿 */
                putch(' ');
            }

            /* 头部先降一格再画：留下来的是"尾"，正在画的是"头" */
            drop_head[i] = drop_head[i] + 1;

            /* 上一格由"头"变成"尾"：重画成暗绿 */
            y = drop_head[i] - 1;
            if (y >= 1 && y <= ROWS) {
                gotoxy(drop_col[i], y);
                textcolor(2);
                putch(33 + rnd(GLYPHS));
            }

            /* 新的头：亮绿 */
            y = drop_head[i];
            if (y >= 1 && y <= ROWS) {
                gotoxy(drop_col[i], y);
                textcolor(10);              /* 亮绿 */
                putch(33 + rnd(GLYPHS));
            }

            if (drop_head[i] - drop_tail[i] > ROWS)
                drop_reset(i);
        }
    }

    /* 收尾：恢复默认配色并把光标放到最后一行的行首，别把终端留在半截状态 */
    gotoxy(1, ROWS);
    textcolor(7);
    return 0;
}
