/* pcscreen.c —— **电脑屏窗口**（第三种窗口）的最小体检程序。
 * pcscreen.c — minimal self-check program for the **PC screen window** (the third window type).
 *
 * 用途：真机上验 `ui_win_open_pc`（`#582`）那三件与图形窗口不同的行为。
 * Purpose: verify on a real device the three behaviors of `ui_win_open_pc` (`#582`) that differ from a graphics window.
 * 它刻意**只画固定分辨率的东西** —— 屏幕上看到的应该是一整幅 640×480 的框，
 * It deliberately draws **only fixed-resolution things** — what you see should be a full 640x480 frame,
 * 等比缩放贴进画布、**四边都在**（不是被裁掉一截）。
 * scaled proportionally into the canvas, with **all four edges present** (not cropped).
 *
 * 该看到什么：
 * What you should see:
 *   ① 一圈边框正好贴着窗口**四边**（分辨率没被换过 / 没被裁）；
 *   1) A border exactly flush with the window's **four edges** (resolution not swapped / not cropped);
 *   ② 格线是 40 点一格 —— 数得出 16×12 格（不是自适应后的别的数字）；
 *   2) Grid lines every 40 points — you can count 16x12 cells (not some other adaptive number);
 *   ③ 底部两行：最后按下的键、以及鼠标位置；
 *   3) Two bottom lines: last key pressed, and mouse position;
 *   ④ **触摸**：手指按在哪儿，十字准星就画在哪儿（坐标是 640×480 那套）；
 *   4) **Touch**: the crosshair is drawn wherever the finger lands (coordinates are the 640x480 set);
 *   ⑤ **没有手柄区**（底部那排方向键/A/B 不该出现）；
 *   5) **No gamepad area** (the bottom row of D-pad/A/B buttons should not appear);
 *   ⑥ 按 Esc 或点窗口的返回箭头退出。
 *   6) Press Esc or tap the window's back arrow to exit.
 *
 * ⚠ 它**不是**给用户用的示例，是**体检程序** —— 与 `draw_prims.c` 同一性质：
 * ⚠ It is **not** a user-facing example but a **self-check program** — same nature as `draw_prims.c`:
 *   程序要的就是"固定格距、固定分辨率"，好让截图能逐项核对。
 *   The program wants "fixed grid spacing, fixed resolution" so screenshots can be checked item by item.
 *
 * 坐标说明：窗口宽高就是程序的坐标空间（这里 640×480，取 PC 上最眼熟那一档）。
 * Coordinates: the window width/height is the program's coordinate space (here 640x480, the most familiar PC tier).
 * 宿主默认等比缩放让你看全貌，双指可以放大细看 —— 那只是**显示**，
 * The host scales proportionally by default so you see the whole picture, and two fingers can zoom in — that is only **display**,
 * 程序这边的坐标一个数都不会变。
 * not a single coordinate on the program side changes.
 */
#include <waycoder_ui.h>

#define PC_W 640
#define PC_H 480
#define GRID 40

static int msg[4];

static int lastKey;      /* 最后按下的键码 */
                         /* Last key code pressed */
static int mx;           /* 鼠标（触摸）位置 */
                         /* Mouse (touch) position */
static int my;
static int clicked;      /* 鼠标按下的次数 —— 用来区分"只移动"和"真的点了" */
                         /* Number of mouse presses — distinguishes "just moved" from "actually clicked" */
static int touchSeen;    /* **收到过几次触摸消息** —— 电脑屏窗口里这个数**必须恒为 0** */
                         /* **How many touch messages arrived** — in a PC screen window this must **always be 0** */

/* 一个 32 位色：0xAARRGGBB */
/* A 32-bit color: 0xAARRGGBB */
#define RGB_BG    0xFF101820
#define RGB_LINE  0xFF2A3A4A
#define RGB_EDGE  0xFF40C0FF
#define RGB_TEXT  0xFFE0E0E0
#define RGB_DIM   0xFF8090A0
#define RGB_MARK  0xFFFF4040

static void put_int(char* buf, int v)
{
    /* 极简十进制：避免依赖 printf 的格式化路径（那套在手机上会崩，见宿主接口文档）。
       Minimal decimal conversion: avoids printf's formatting path (it crashes on mobile, see the host interface doc).
       返回写在 buf 里的字符串。
       Returns the string written into buf. */
    char t[12];
    int n = 0, i = 0, neg = 0;
    if (v < 0) { neg = 1; v = -v; }
    if (v == 0) { t[n] = '0'; n = 1; }
    while (v > 0 && n < 11) { t[n] = (char)('0' + v % 10); n = n + 1; v = v / 10; }
    if (neg) { buf[i] = '-'; i = i + 1; }
    while (n > 0) { n = n - 1; buf[i] = t[n]; i = i + 1; }
    buf[i] = 0;
}

static void draw_all(void)
{
    int x, y;
    char buf[64];
    int i;

    ui_clear(RGB_BG);

    /* ① 格线：每 40 点一条 —— 数格子就能看出分辨率有没有被换过 */
    /* 1) Grid lines: one every 40 points — counting cells shows whether the resolution was swapped */
    for (x = GRID; x < PC_W; x = x + GRID)
        ui_line(x, 0, x, PC_H, RGB_LINE, 1);
    for (y = GRID; y < PC_H; y = y + GRID)
        ui_line(0, y, PC_W, y, RGB_LINE, 1);

    /* ② 四边贴边（空心矩形，线宽 3）—— 四边都在才说明没被裁 */
    /* 2) Flush on all four edges (hollow rectangle, line width 3) — all four present means not cropped */
    ui_rect(2, 2, PC_W - 4, PC_H - 4, RGB_EDGE, 0, 3, 0);

    /* ③ 标题 */
    /* 3) Title */
    ui_text(20, 10, "电脑屏窗口 640x480", RGB_EDGE, 24, VML_ANCHOR_LEFT);

    /* ④ 底部两行：最后按键 + 鼠标位置 */
    /* 4) Two bottom lines: last key + mouse position */
    buf[0] = 'K'; buf[1] = 'E'; buf[2] = 'Y'; buf[3] = ' ';
    i = 4;
    {
        char nb[12];
        int k;
        put_int(nb, lastKey);
        for (k = 0; nb[k] != 0; k = k + 1) { buf[i] = nb[k]; i = i + 1; }
    }
    buf[i] = 0;
    /* 这一行的末尾会再补上 TOUCH 计数（见下面那段） */
    /* The TOUCH counter is appended at the end of this line (see the block below) */
    ui_text(20, PC_H - 64, buf, RGB_TEXT, 20, VML_ANCHOR_LEFT);

    buf[0] = 'M'; buf[1] = 'O'; buf[2] = 'U'; buf[3] = 'S'; buf[4] = 'E'; buf[5] = ' ';
    i = 6;
    {
        char nb[12];
        int k;
        put_int(nb, mx);
        for (k = 0; nb[k] != 0; k = k + 1) { buf[i] = nb[k]; i = i + 1; }
        buf[i] = ','; i = i + 1;
        put_int(nb, my);
        for (k = 0; nb[k] != 0; k = k + 1) { buf[i] = nb[k]; i = i + 1; }
        buf[i] = ' '; i = i + 1;
        buf[i] = '#'; i = i + 1;
        put_int(nb, clicked);
        for (k = 0; nb[k] != 0; k = k + 1) { buf[i] = nb[k]; i = i + 1; }
    }
    /* ⚠ 这一行是**"抑制触摸"的判据**：电脑屏窗口只该发鼠标消息，
       ⚠ This line is the **"touch suppression" criterion**: a PC screen window should only send mouse messages,
       触摸消息一条都不该来。这里若数出非 0，说明抑制没生效 ——
       not a single touch message should arrive. A non-zero count here means suppression is not working —
       而画面上看不出任何异常（准星照旧跟着走），只有这一行会变。
       and nothing looks wrong on screen (the crosshair still follows), only this line changes. */
    buf[i] = ' '; i = i + 1;
    buf[i] = 'T'; i = i + 1;
    buf[i] = 'O'; i = i + 1;
    buf[i] = 'U'; i = i + 1;
    buf[i] = 'C'; i = i + 1;
    buf[i] = 'H'; i = i + 1;
    buf[i] = ' '; i = i + 1;
    {
        char nb[12];
        int k;
        put_int(nb, touchSeen);
        for (k = 0; nb[k] != 0; k = k + 1) { buf[i] = nb[k]; i = i + 1; }
    }
    buf[i] = 0;
    ui_text(20, PC_H - 36, buf, RGB_DIM, 20, VML_ANCHOR_LEFT);

    /* ⚠ **报警块**：只在"收到过触摸消息"时亮（右上角一块实心红）。
       ⚠ **Alarm block**: lights up only when "touch messages were received" (a solid red square in the top-right).
       电脑屏窗口里它**永远不该出现**。
       In a PC screen window it **should never appear**.
       为什么用色块而不是文字：画布上的文字**读不出**（不是 UI 节点），
       Why a color block instead of text: canvas text **cannot be read back** (it is not a UI node),
       而验收脚本要能**逐像素**判 —— 色块一行断言就够，不必上 OCR。
       and the acceptance script must judge **pixel by pixel** — a color block needs one assertion line, no OCR. */
    if (touchSeen > 0)
        ui_rect(PC_W - 44, 8, 36, 20, RGB_MARK, 1, 0, 0);

    /* ⑤ 十字准星：触摸落在哪儿就画在哪儿（这就是"触摸当鼠标"的判据） */
    /* 5) Crosshair: drawn wherever the touch lands (this is the "touch acts as mouse" criterion) */
    if (mx > 0 || my > 0)
    {
        ui_line(mx - 16, my, mx + 16, my, RGB_MARK, 2);
        ui_line(mx, my - 16, mx, my + 16, RGB_MARK, 2);
        ui_circle(mx, my, 8, RGB_MARK, 0, 2);
    }

    ui_present();
}

int main()
{
    int h;

    /* 开电脑屏窗口：固定 640×480、不锁方向、要屏幕键盘。
       Open a PC screen window: fixed 640x480, orientation not locked, on-screen keyboard wanted.
       ⚠ 第 5 个参数是**键盘**（VML_WIN_NEED_KEYBOARD），不是手柄 ——
       ⚠ The 5th parameter is the **keyboard** (VML_WIN_NEED_KEYBOARD), not the gamepad —
       位置与 ui_win_open_ex 的第 5 个参数对称，但语义不同。
       it sits in the same position as the 5th parameter of ui_win_open_ex, but means something different. */
    h = ui_win_open_pc("电脑屏体检", PC_W, PC_H,
                       VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);
    if (h < 0) return 1;

    ui_msg_clear();     /* 丢掉开窗之前积压的消息 */
                        /* Drop messages queued up before the window was opened */

    draw_all();

    while (ui_win_closed() == 0)
    {
        int t = ui_wait(msg, 0);        /* timeout 0 = **无限等**（事件驱动，省电） */
                                        /* timeout 0 = **wait forever** (event driven, saves power) */
        if (t == 0) continue;

        if (t == VML_MSG_WINDOWCLOSE) break;

        /* 触摸**在这里应该是鼠标消息**（电脑屏窗口不发触摸消息）——
           Touch **should be a mouse message here** (a PC screen window does not send touch messages) —
           若下面这两支一次都不进，说明"抑制触摸"那条没生效。
           if neither of the two branches below is ever entered, that "touch suppression" is not working. */
        if (t == VML_MSG_MOUSEDOWN) { clicked = clicked + 1; mx = msg[1]; my = msg[2]; draw_all(); }
        else if (t == VML_MSG_MOUSEMOVE) { mx = msg[1]; my = msg[2]; draw_all(); }

        /* 触摸消息：**电脑屏窗口里不该出现**（见 touchSeen 的说明）。
           Touch messages: **should not appear in a PC screen window** (see the note on touchSeen).
           计数而不是忽略，是为了让"抑制失效"这件事**看得见**。
           Counting instead of ignoring makes "suppression failed" **visible**. */
        else if (t == VML_MSG_TOUCHDOWN || t == VML_MSG_TOUCHMOVE || t == VML_MSG_TOUCHUP)
        {
            touchSeen = touchSeen + 1;
            draw_all();
        }

        else if (t == VML_MSG_KEYDOWN)
        {
            lastKey = msg[1];
            if (lastKey == VML_KEY_ESCAPE) break;
            draw_all();
        }
    }

    ui_win_close();
    return 0;
}
