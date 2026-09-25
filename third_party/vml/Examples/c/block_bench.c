/* block_bench.c —— 矢量图块（ui_create_block / ui_end_block / ui_draw_block）的压力测试。
 *
 * ## 它测什么
 *
 * 用 10 种**样式各异的飞机图块**在屏幕上飞（各朝各的方向、速度不同），
 * 数量从 1 架一路涨到 100 架，右上角实时显示：
 *
 *     帧率 / 飞机数 / 虚拟机内存 / 栈大小 / 堆基址 / 场景图元数
 *
 * 用来回答一个问题：**贴 100 个带旋转的矢量图块，帧率还剩多少。**
 *
 * ## 为什么这是图块的好用例
 *
 * 每架飞机每帧都要"转 + 移"，而矢量图块贴一次的代价是
 * `push + translate + rotate + scale + 块内行 + pop` —— 旋转是**变换矩阵**，
 * 不是重采样；放大也不糊（它的上限是图元预算，不是像素）。
 *
 * ## 三条要知道的
 *
 * 1. **颜色写完整 ARGB**（0xAARRGGBB）—— 写 `0xFF0000` 会被当成 alpha=0 的全透明色。
 * 2. 图块**造一次、一直贴**；块表**不随 ui_clear 清**（每帧 create 一遍会在 128 帧后拿不到句柄）。
 * 3. 结尾**不能调 ui_win_close()** —— 那会把场景置空，而桌面 `--frame` 是跑完之后才取场景的。
 */

#include <waycoder_ui.h>

/* `Lib/shared/src/sysinfo.c` 的包装（第一个参数按调用约定放 R0，所以 asm 里不用占位符）。 */
int getconfig(int type);
int ui_tick(void);

#define MAX_PLANES 1000
#define KINDS 10

/* 飞机状态（文件级全局变量是好的，见 shot.c 的说明） */
int planeX[MAX_PLANES];
int planeY[MAX_PLANES];
int planeKind[MAX_PLANES];
int planeSpd[MAX_PLANES];
int planeDir[MAX_PLANES];    /* 朝向角度（度） */
int planeAlive[MAX_PLANES];

int blockOf[KINDS];          /* 10 种样式的图块句柄 */
int sw;
int sh;
int planeCount;

/* ── 手写整数转字符串（不去碰 printf 那套，避免格式化的坑）── */
char* fmtInt(int v, char* buf) {
    int n;
    int i;
    int neg;
    n = 0;
    neg = 0;
    if (v < 0) { neg = 1; v = -v; }
    if (v == 0) { buf[0] = '0'; buf[1] = 0; return buf; }
    while (v > 0) {
        buf[n] = '0' + (v - (v / 10) * 10);
        n = n + 1;
        v = v / 10;
    }
    if (neg) { buf[n] = '-'; n = n + 1; }
    /* 反转 */
    for (i = 0; i < n / 2; i++) {
        char t = buf[i];
        buf[i] = buf[n - 1 - i];
        buf[n - 1 - i] = t;
    }
    buf[n] = 0;
    return buf;
}

/* ── 10 种飞机样式：每种一组图元拼出来，录成一个图块 ── */
void buildKind(int k) {
    int c;
    int body;
    /* 每种一个主色（完整 ARGB） */
    if (k == 0) { c = 0xFFFF5555; }
    else if (k == 1) { c = 0xFFFFAA33; }
    else if (k == 2) { c = 0xFFFFEE55; }
    else if (k == 3) { c = 0xFF66DD66; }
    else if (k == 4) { c = 0xFF44DDBB; }
    else if (k == 5) { c = 0xFF55BBFF; }
    else if (k == 6) { c = 0xFF6688FF; }
    else if (k == 7) { c = 0xFFAA77FF; }
    else if (k == 8) { c = 0xFFEE77DD; }
    else { c = 0xFFFFFFFF; }

    blockOf[k] = ui_create_block(28, 28, 0);

    /* 机身：一个竖条（机头朝上，方便按角度旋转） */
    ui_rect(12, 2, 4, 22, c, 1, 0, 2);
    /* 机翼：横向渐短的条 */
    ui_rect(2, 12, 24, 4, c, 1, 0, 1);
    /* 尾翼 */
    ui_rect(9, 20, 10, 3, c, 1, 0, 1);
    /* 驾驶舱（白点） */
    ui_circle(14, 9, 2, 0xFFFFFFFF, 1, 0);

    /* 样式区分：每种加一个不同的配饰（k 决定位置/形状） */
    body = 3 + (k - (k / 4) * 4) * 3;
    if (k < 5) {
        ui_circle(body, 6, 2, 0xFFFFFFFF, 1, 0);
    } else {
        ui_rect(body, 17, 3, 3, 0xFFFFFFFF, 1, 0, 0);
    }

    ui_end_block();
}

void spawnPlane(int i) {
    planeX[i] = 20 + (i * 37 - (i * 37 / (sw - 60)) * (sw - 60));
    planeY[i] = 40 + (i * 53 - (i * 53 / (sh - 120)) * (sh - 120));
    planeKind[i] = i - (i / KINDS) * KINDS;
    planeSpd[i] = 2 + (i - (i / 5) * 5);          /* 2..6 */
    planeDir[i] = (i * 37) - ((i * 37) / 360) * 360;
    planeAlive[i] = 1;
}

void drawPlane(int i) {
    int k;
    k = blockOf[planeKind[i]];
    ui_draw_block(k, planeX[i], planeY[i], 1000, 1000, planeDir[i]);
}

/* 一步：按朝向飞，出界就绕回对面 */
void stepPlane(int i) {
    int d;
    d = planeDir[i];
    /* 用查表避免 sin/cos 的坑（本仓 BASIC 前端那套 sin/cos 有问题；C 这边也统一走整数推进） */
    if (d < 45) { planeX[i] = planeX[i] + planeSpd[i]; }
    else if (d < 90) { planeX[i] = planeX[i] + planeSpd[i]; planeY[i] = planeY[i] - planeSpd[i] / 2; }
    else if (d < 135) { planeY[i] = planeY[i] - planeSpd[i]; }
    else if (d < 180) { planeX[i] = planeX[i] - planeSpd[i] / 2; planeY[i] = planeY[i] - planeSpd[i]; }
    else if (d < 225) { planeX[i] = planeX[i] - planeSpd[i]; }
    else if (d < 270) { planeX[i] = planeX[i] - planeSpd[i]; planeY[i] = planeY[i] + planeSpd[i] / 2; }
    else if (d < 315) { planeY[i] = planeY[i] + planeSpd[i]; }
    else { planeX[i] = planeX[i] + planeSpd[i] / 2; planeY[i] = planeY[i] + planeSpd[i]; }

    if (planeX[i] < -20) { planeX[i] = sw + 10; }
    if (planeX[i] > sw + 20) { planeX[i] = -10; }
    if (planeY[i] < 20) { planeY[i] = sh - 60; }
    if (planeY[i] > sh - 40) { planeY[i] = 30; }
}

/* ── 主循环 ── */
int main() {
    int i;
    int k;
    int m;
    int t0;
    int t1;
    int frames;
    int fps;
    int grow;
    char buf[24];
    char line[128];

    ui_win_open("图块压力测试", 0, 0);

    sw = ui_scr_w();
    sh = ui_scr_h();

    for (k = 0; k < KINDS; k++) {
        buildKind(k);
    }

    planeCount = 1;
    for (i = 0; i < MAX_PLANES; i++) { planeAlive[i] = 0; }
    spawnPlane(0);

    frames = 0;
    grow = 0;
    t0 = ui_tick();

    for (;;) {
        if (ui_win_closed() != 0) { break; }

        ui_clear(0xFF0A0A14);

        /* 画所有飞机（每架一次 block 贴图，带旋转） */
        for (i = 0; i < planeCount; i++) {
            if (planeAlive[i] != 0) {
                stepPlane(i);
                drawPlane(i);
            }
        }

        /* ── 信息面板 ── */
        /* 底衬：飞机上千之后，没底衬的文字根本读不清（半透明黑，压在文字下面） */
        ui_rect(0, 0, 168, 84, 0xCC000000, 1, 0, 0);

        frames = frames + 1;
        grow = grow + 1;
        t1 = ui_tick();
        if (t1 - t0 >= 500) {
            fps = (frames * 1000) / (t1 - t0);
            frames = 0;
            t0 = t1;
        }

        {
            char b1[24]; char b2[24]; char b3[24]; char b4[24]; char b5[24];
            int p; int q;
            p = 0;
            q = 0;
            /* 手工拼一行信息（避开 sprintf） */
            line[q] = 'F'; q = q + 1;
            line[q] = 'P'; q = q + 1;
            line[q] = 'S'; q = q + 1;
            line[q] = ' '; q = q + 1;
            fmtInt(fps, b1);
            p = 0; while (b1[p] != 0) { line[q] = b1[p]; q = q + 1; p = p + 1; }
            line[q] = ' '; q = q + 1;
            line[q] = '|'; q = q + 1;
            line[q] = ' '; q = q + 1;
            fmtInt(planeCount, b1);
            p = 0; while (b1[p] != 0) { line[q] = b1[p]; q = q + 1; p = p + 1; }
            line[q] = ' '; q = q + 1;
            line[q] = 'j'; q = q + 1;
            line[q] = 'i'; q = q + 1;
            line[q] = 0;
            ui_text(8, 20, line, 0xFFCCDDFF, 14, 0);
        }

        {
            /* 资源读数：内存总量 / 栈大小 / 堆基址（都来自 GetConfig） */
            int memKB; int stkKB; int heapBase; int p; int q;
            char b[24];
            memKB = getconfig(7) / 1024;
            stkKB = getconfig(12) / 1024;
            heapBase = getconfig(13);

            q = 0;
            line[q] = 'M'; q = q + 1; line[q] = 'E'; q = q + 1;
            line[q] = 'M'; q = q + 1; line[q] = ' '; q = q + 1;
            fmtInt(memKB, b);
            p = 0; while (b[p] != 0) { line[q] = b[p]; q = q + 1; p = p + 1; }
            line[q] = 'K'; q = q + 1; line[q] = 0;
            ui_text(8, 40, line, 0xFF88AACC, 13, 0);

            q = 0;
            line[q] = 'S'; q = q + 1; line[q] = 'T'; q = q + 1;
            line[q] = 'K'; q = q + 1; line[q] = ' '; q = q + 1;
            fmtInt(stkKB, b);
            p = 0; while (b[p] != 0) { line[q] = b[p]; q = q + 1; p = p + 1; }
            line[q] = 'K'; q = q + 1; line[q] = 0;
            ui_text(8, 60, line, 0xFF88AACC, 13, 0);

            q = 0;
            line[q] = 'H'; q = q + 1; line[q] = 'P'; q = q + 1;
            line[q] = ' '; q = q + 1;
            fmtInt(heapBase, b);
            p = 0; while (b[p] != 0) { line[q] = b[p]; q = q + 1; p = p + 1; }
            line[q] = 0;
            ui_text(8, sh - 80, line, 0xFF88AACC, 13, 0);
        }

        ui_present();

        /* 每 3 帧加一架，加到 MAX_PLANES 架为止
           （1000 架按每 12 帧加要跑一万多帧，手机上太慢；3 帧一架约 20~30 秒跑满） */
        if (grow >= 3) {
            grow = 0;
            if (planeCount < MAX_PLANES) {
                spawnPlane(planeCount);
                planeCount = planeCount + 1;
            }
        }

        ui_wait(m, 8);
    }
    return 0;
}
