// 贪吃蛇 —— 用 **C++** 写的手机游戏
//
// ◆ 手机那套 UI 怎么来的
//
// 开窗 / 绘图 / 输入 / 定时器的实现是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="cpp" Libs="vmlui.vml">` 挂给每个前端。
// 数值的唯一真源是 `Lib/c/waycoder_ui.h`。
//
// ◆ 一条**已失效**的旧约束（2026-09-17 实测更正）
//
// `corpus/cpp/skel.cpp` 的文件头写着「实参从左到右压栈 ⇒ 多参调用参数整体反序，
// `ipow(2,3)` 得 9，所以库调用只能用单参数」——**这条现在不成立了**：
// 调用约定统一之后实测 `drift.cpp`（循环里调 `ipow(2, i)`）得 **126**，正确。
// 本文件按自然写法写 8 参的 `ui_rect`，不为那条旧规避而扭曲。
//
// ◆ 写法说明
//
//   · 数组**没有花括号初始化**（`InitializerListExpr` 只编第一个元素、其余静默丢）
//     ⇒ 逐元素赋值。
//   · 循环更新写 `i = i + 1`。
//   · 颜色写负数十进制（`0xAARRGGBB` 的十进制负数形式）。
//   · 状态放**一个**文件级数组、辅助函数一律无参。
//
// ◆ 操作
//
// 方向键转向；SELECT 暂停；回车重开；ESC 或返回箭头退出。
// 吃到食物 +10 分并加速；撞墙或撞到自己结束（弹对话框问要不要再来一局）。

// 状态表（一个数组放全部，避免多个全局数组互相踩）
//   0=head 1=len 2=dx 3=dy 4=fx 5=fy 6=score 7=best
//   8=alive 9=paused 10=stepMs 11=cell 12=ox 13=oy 14=sw 15=sh
//   16+2i=第 i 节 x，17+2i=第 i 节 y（i=0 是头；40 节上限 ⇒ 16..95）
//   100=候选x 101=候选y 105/106=新方向 107=命中
int A[112];

void occupied() {
    int i;
    A[107] = 0;
    i = 0;
    while (i < A[1]) {
        if (A[16+i*2] == A[100] && A[17+i*2] == A[101]) {
            A[107] = 1;
        }
        i = i + 1;
    }
}

void placeFood() {
    int tries;
    A[100] = ui_rand(20);
    A[101] = ui_rand(18);
    occupied();
    tries = 0;
    while (tries < 300) {
        if (A[107] != 0) {
            A[100] = ui_rand(20);
            A[101] = ui_rand(18);
            occupied();
        }
        if (A[107] == 0) {
            A[4] = A[100];
            A[5] = A[101];
            tries = 300;
        }
        tries = tries + 1;
    }
}

void draw() {
    int cellN;
    int i;
    ui_clear(-15724520);

    // 棋盘格：扁平序号 0..359；每格按 (r+c) 的奇偶决定铺不铺
    cellN = 0;
    while (cellN < 360) {
        if ((cellN+cellN/20)-((cellN+cellN/20)/2)*2 == 0) {
            ui_rect(A[12]+(cellN-(cellN/20)*20)*A[11], A[13]+(cellN/20)*A[11],
                    A[11]-1, A[11]-1, -15263713, 1, 0, 2);
        }
        cellN = cellN + 1;
    }

    ui_rect(A[12]+A[4]*A[11], A[13]+A[5]*A[11], A[11]-1, A[11]-1, -131246, 1, 0, 2);

    i = 0;
    while (i < A[1]) {
        if (i == 0) {
            ui_rect(A[12]+A[16+i*2]*A[11], A[13]+A[17+i*2]*A[11],
                    A[11]-1, A[11]-1, -63488, 1, 0, 2);
        }
        if (i != 0) {
            ui_rect(A[12]+A[16+i*2]*A[11], A[13]+A[17+i*2]*A[11],
                    A[11]-1, A[11]-1, -11409298, 1, 0, 2);
        }
        i = i + 1;
    }

    ui_text(8, 8, "得分", -6643536, 13, 0);
    ui_rect(58, 11, A[6], 10, -11409298, 1, 0, 0);
    ui_text(A[14]/2, 8, "最高", -6643536, 13, 1);
    ui_rect(A[14]/2+46, 11, A[7], 10, -63488, 1, 0, 0);
    if (A[9] != 0) {
        ui_text(A[14]/2, A[15]/2, "暂停（SELECT 继续）", -131246, 16, 1);
    }
    ui_present();
}

void resetGame() {
    A[1] = 3;
    A[0] = 2;
    A[16] = 8;
    A[17] = 8;
    A[18] = 7;
    A[19] = 8;
    A[20] = 6;
    A[21] = 8;
    A[2] = 1;
    A[3] = 0;
    A[6] = 0;
    A[8] = 1;
    A[9] = 0;
    A[10] = 170;
    placeFood();
}

void gameOver() {
    A[8] = 0;
    ui_beep(220, 260);
    draw();
    ui_dlg_msg("贪吃蛇", "撞到了，这一局结束。\n再来一局？（选「否」退出）", 0);
    resetGame();
}

void step() {
    int nc;
    int nr;
    int i;
    if (A[8] == 0) { return; }
    if (A[9] != 0) { return; }
    nc = A[16] + A[2];
    nr = A[17] + A[3];
    if (nc < 0) { gameOver(); return; }
    if (nc >= 20) { gameOver(); return; }
    if (nr < 0) { gameOver(); return; }
    if (nr >= 18) { gameOver(); return; }

    // 自撞：只看前 len-1 节（尾巴这一步会挪走）
    A[100] = nc;
    A[101] = nr;
    A[107] = 0;
    i = 0;
    while (i < A[1]-1) {
        if (A[16+i*2] == nc && A[17+i*2] == nr) {
            A[107] = 1;
        }
        i = i + 1;
    }
    if (A[107] != 0) { gameOver(); return; }

    // 整体后移一位（不做环形回绕）
    i = A[1];
    while (i > 0) {
        A[16+i*2] = A[16+(i-1)*2];
        A[17+i*2] = A[17+(i-1)*2];
        i = i - 1;
    }
    A[16] = nc;
    A[17] = nr;

    if (nc == A[4] && nr == A[5]) {
        if (A[1] < 40) {
            A[1] = A[1] + 1;
        }
        A[6] = A[6] + 10;
        if (A[6] > A[7]) {
            A[7] = A[6];
        }
        if (A[10] > 70) {
            A[10] = A[10] - 6;
        }
        ui_beep(880, 40);
        ui_vibrate(30, 0);
        placeFood();
    }
}

void turn() {
    if (A[105]+A[2] == 0 && A[106]+A[3] == 0) {
        return;
    }
    A[2] = A[105];
    A[3] = A[106];
}

int main() {
    int w;
    int h;
    int byw;
    int byh;
    int tid;
    int curMs;
    int t;
    int k;

    w = ui_scr_w();
    h = ui_scr_h();
    if (w <= 0) { w = 360; }
    if (h <= 0) { h = 620; }
    A[14] = w;
    A[15] = h;
    ui_win_open("贪吃蛇", w, h);

    // 版面算一次，画与判定共用
    byw = w - 8;
    byh = h - 46;
    A[11] = byw / 20;
    if (byh/18 < A[11]) { A[11] = byh / 18; }
    if (A[11] < 4) { A[11] = 4; }
    A[12] = (w - A[11]*20) / 2;
    A[13] = 40;

    ui_keep_on(1);
    resetGame();

    tid = ui_timer_set(A[10], 0);
    curMs = A[10];

    while (ui_win_closed() == 0) {
        draw();
        t = ui_wait_msg(0);
        if (t == 10) { break; }
        if (t == 9) {
            step();
            if (A[10] != curMs) {
                ui_timer_kill(tid);
                curMs = A[10];
                tid = ui_timer_set(curMs, 0);
            }
        }
        if (t == 1) {
            k = ui_msg_a();
            if (k == 27) { break; }
            if (k == 37) { A[105] = -1; A[106] = 0;  turn(); }
            if (k == 39) { A[105] = 1;  A[106] = 0;  turn(); }
            if (k == 38) { A[105] = 0;  A[106] = -1; turn(); }
            if (k == 40) { A[105] = 0;  A[106] = 1;  turn(); }
            if (k == 13) { resetGame(); }
            if (k == 16) { A[9] = 1 - A[9]; }
        }
    }

    ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
    return 0;
}
