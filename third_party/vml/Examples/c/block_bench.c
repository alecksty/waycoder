/* block_bench.c —— 矢量图块（ui_create_block / ui_end_block / ui_draw_block）的压力测试。
 * block_bench.c -- a stress test for vector blocks (ui_create_block / ui_end_block /
 * ui_draw_block).
 *
 * ## 它测什么
 * ## What it measures
 *
 * 用 10 种**样式各异的飞机图块**在屏幕上飞（各朝各的方向、速度不同），
 * Ten **differently styled plane blocks** fly around the screen (each heading its own
 * 数量从 1 架一路涨到 100 架，右上角实时显示：
 * way, at its own speed); the count grows from 1 plane up to 100, and the top right
 * corner shows live:
 *
 *     帧率 / 飞机数 / 虚拟机内存 / 栈大小 / 堆基址 / 场景图元数
 *     fps / plane count / VM memory / stack size / heap base / scene primitive count
 *
 * 用来回答一个问题：**贴 100 个带旋转的矢量图块，帧率还剩多少。**
 * It answers one question: **with 100 rotated vector blocks blitted, how much frame
 * rate is left.**
 *
 * ## 为什么这是图块的好用例
 * ## Why this is a good use case for blocks
 *
 * 每架飞机每帧都要"转 + 移"，而矢量图块贴一次的代价是
 * Every plane must "rotate + move" every frame, and the cost of one block blit is
 * `push + translate + rotate + scale + 块内行 + pop` —— 旋转是**变换矩阵**，
 * `push + translate + rotate + scale + the block's rows + pop` -- the rotation is a
 * **transformation matrix**, not a resample; and scaling up is not blurry either (its
 * 不是重采样；放大也不糊（它的上限是图元预算，不是像素）。
 * limit is the primitive budget, not pixels).
 *
 * ## 三条要知道的
 * ## Three things to know
 *
 * 1. **颜色写完整 ARGB**（0xAARRGGBB）—— 写 `0xFF0000` 会被当成 alpha=0 的全透明色。
 * 1. **Write the full ARGB colour** (0xAARRGGBB) -- writing `0xFF0000` is taken as a
 * fully transparent colour with alpha=0.
 * 2. 图块**造一次、一直贴**；块表**不随 ui_clear 清**（每帧 create 一遍会在 128 帧后拿不到句柄）。
 * 2. A block is **created once and blitted forever**; the block table is **not cleared by
 * ui_clear** (creating one every frame runs out of handles after about 128 frames).
 * 3. 结尾**不能调 ui_win_close()** —— 那会把场景置空，而桌面 `--frame` 是跑完之后才取场景的。
 * 3. Do **not call ui_win_close()** at the end -- it blanks the scene, and the desktop
 * `--frame` only grabs the scene after the program has finished.
 */

#include <waycoder_ui.h>

/* `Lib/shared/src/sysinfo.c` 的包装（第一个参数按调用约定放 R0，所以 asm 里不用占位符）。 */
/* Wrappers for `Lib/shared/src/sysinfo.c` (the first argument goes in R0 by the calling
 * convention, so the asm needs no placeholder). */
int getconfig(int type);
int ui_tick(void);

#define MAX_PLANES 1000
#define KINDS 10

/* 飞机状态（文件级全局变量是好的，见 shot.c 的说明） */
/* Plane state (file level globals are fine here -- see the notes in shot.c) */
int planeX[MAX_PLANES];
int planeY[MAX_PLANES];
int planeKind[MAX_PLANES];
int planeSpd[MAX_PLANES];
int planeDir[MAX_PLANES];    /* 朝向角度（度） */
/* heading angle in degrees */
int planeAlive[MAX_PLANES];

int blockOf[KINDS];          /* 10 种样式的图块句柄 */
/* block handles for the 10 styles */
int sw;
int sh;
int planeCount;

/* ── 手写整数转字符串（不去碰 printf 那套，避免格式化的坑）── */
/* ── Hand rolled int to string (avoid the whole printf path, and its formatting traps) ── */
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
    /* reverse it */
    for (i = 0; i < n / 2; i++) {
        char t = buf[i];
        buf[i] = buf[n - 1 - i];
        buf[n - 1 - i] = t;
    }
    buf[n] = 0;
    return buf;
}

/* ── 10 种飞机样式：每种一组图元拼出来，录成一个图块 ── */
/* ── 10 plane styles: each is drawn from a set of primitives and recorded into a block ── */
void buildKind(int k) {
    int c;
    int body;
    /* 每种一个主色（完整 ARGB） */
    /* one main colour per style (full ARGB) */
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
    /* fuselage: a vertical bar (nose up, so it rotates cleanly by angle) */
    ui_rect(12, 2, 4, 22, c, 1, 0, 2);
    /* 机翼：横向渐短的条 */
    /* wings: horizontal bars that get shorter */
    ui_rect(2, 12, 24, 4, c, 1, 0, 1);
    /* 尾翼 */
    /* tail fin */
    ui_rect(9, 20, 10, 3, c, 1, 0, 1);
    /* 驾驶舱（白点） */
    /* cockpit (a white dot) */
    ui_circle(14, 9, 2, 0xFFFFFFFF, 1, 0);

    /* 样式区分：每种加一个不同的配饰（k 决定位置/形状） */
    /* Style marker: each kind gets a different ornament (k decides position and shape) */
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
/* One step: fly along the heading, and wrap around to the far side when out of bounds */
void stepPlane(int i) {
    int d;
    d = planeDir[i];
    /* 用查表避免 sin/cos 的坑（本仓 BASIC 前端那套 sin/cos 有问题；C 这边也统一走整数推进） */
    /* Use a lookup table to dodge the sin/cos traps (this repo's BASIC front end has
     * problems with sin/cos; on the C side too we advance with integers only) */
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
/* ── Main loop ── */
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
    int lang = ui_get_language();   /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                                    /* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    /* ⚠ 尺寸必须传 `ui_scr_w()/ui_scr_h()`（可用绘图区），**不能传 0** ——
     * ⚠ The size must be `ui_scr_w()/ui_scr_h()` (the usable drawing area), **not 0** --
       传 0 时窗口拿到的尺寸与程序以为的不是一回事，飞机全画到画布外面去了。
     * passing 0 gives the window a size different from what the program assumes, and
     * every plane gets drawn outside the canvas. */
    ui_win_open(lang == 0 ? "图块压力测试" : "Block stress test", ui_scr_w(), ui_scr_h());

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
        /* Draw all the planes (one rotated block blit each) */
        for (i = 0; i < planeCount; i++) {
            if (planeAlive[i] != 0) {
                stepPlane(i);
                drawPlane(i);
            }
        }

        /* ── 信息面板 ── */
        /* ── Info panel ── */
        /* 底衬：飞机上千之后，没底衬的文字根本读不清（半透明黑，压在文字下面） */
        /* Backing plate: with a thousand planes around, text with no backing is unreadable
         * (semi transparent black, drawn under the text) */
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
            /* Build one info line by hand (dodging sprintf) */
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
            /* Resource readings: total memory / stack size / heap base (all from GetConfig) */
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
         * Add one plane every 3 frames, up to MAX_PLANES
           （1000 架按每 12 帧加要跑一万多帧，手机上太慢；3 帧一架约 20~30 秒跑满）
         * (adding one every 12 frames would take over ten thousand frames for 1000
         * planes, far too slow on a phone; one every 3 frames fills up in 20-30 seconds) */
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
