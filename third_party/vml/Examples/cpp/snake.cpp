// 贪吃蛇 —— 用 **C++** 写的手机游戏
// Snake — a mobile game written in **C++**
//
// ◆ 手机那套 UI 怎么来的
// ◆ Where the mobile UI comes from
//
// 开窗 / 绘图 / 输入 / 定时器的实现是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// The implementations of window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` → `vmlui.vml`),
// 由 `vmltool.config.xml` 的 `<Language Name="cpp" Libs="vmlui.vml">` 挂给每个前端。
// and are attached to each frontend by `<Language Name="cpp" Libs="vmlui.vml">` in `vmltool.config.xml`.
// 数值的唯一真源是 `Lib/c/waycoder_ui.h`。
// The single source of truth for the numbers is `Lib/c/waycoder_ui.h`.
//
// ◆ 一条**已失效**的旧约束（2026-09-17 实测更正）
// ◆ One **obsolete** old constraint (corrected by measurement on 2026-09-17)
//
// `corpus/cpp/skel.cpp` 的文件头写着「实参从左到右压栈 ⇒ 多参调用参数整体反序，
// The file header of `corpus/cpp/skel.cpp` says "arguments are pushed left to right ⇒ multi-argument calls receive their parameters in reverse order,
// `ipow(2,3)` 得 9，所以库调用只能用单参数」——**这条现在不成立了**：
// `ipow(2,3)` gives 9, so library calls can only take a single argument" — **that no longer holds**:
// 调用约定统一之后实测 `drift.cpp`（循环里调 `ipow(2, i)`）得 **126**，正确。
// after the calling convention was unified, `drift.cpp` (calling `ipow(2, i)` in a loop) measured **126**, which is correct.
// 本文件按自然写法写 8 参的 `ui_rect`，不为那条旧规避而扭曲。
// This file writes the 8-argument `ui_rect` in the natural way, without contorting itself for that old workaround.
//
// ◆ 写法说明
// ◆ Notes on the style
//
//   · 数组**没有花括号初始化**（`InitializerListExpr` 只编第一个元素、其余静默丢）
//   · Arrays **have no brace initialization** (`InitializerListExpr` compiles only the first element and silently drops the rest)
//     ⇒ 逐元素赋值。
//     ⇒ assign element by element.
//   · 循环更新写 `i = i + 1`。
//   · Loop updates are written `i = i + 1`.
//   · 颜色写负数十进制（`0xAARRGGBB` 的十进制负数形式）。
//   · Colors are written as negative decimals (the negative-decimal form of `0xAARRGGBB`).
//   · 状态放**一个**文件级数组、辅助函数一律无参。
//   · State lives in **one** file-level array, and helper functions never take arguments.
//   · 音效用 **`ui_beep` 单音**（v0.96.509 从共享库的音序器换回来 —— 那一版在真机上
//   · Sound effects use the **single-tone `ui_beep`** (switched back from the shared library's sequencer in v0.96.509 — on a real device that version
//     **破音**）。判据：频率取原音型的**首音**、**结局音取最低音**（输要一眼听出来），
//     **broke up**). Criteria: the frequency takes **the first note** of the original pattern, **the ending note takes the lowest pitch** (a loss has to be audible at a glance),
//     时长取整块时长、封顶 320ms，低音不低于 C3(131Hz)（手机外放 200Hz 以下衰减很快）。
//     the duration takes the whole block's duration capped at 320ms, and low notes stay no lower than C3 (131Hz) (phone speakers roll off fast below 200Hz).
//
// ◆ 操作
// ◆ Controls
//
// 方向键转向；SELECT 暂停；回车重开；ESC 或返回箭头退出。
// Arrow keys steer; SELECT pauses; Enter restarts; ESC or the back arrow exits.
// 吃到食物 +10 分并加速；撞墙或撞到自己结束（弹对话框问要不要再来一局）。
// Eating food gives +10 points and speeds things up; hitting a wall or hitting yourself ends it (a dialog asks whether to play another round).

// 状态表（一个数组放全部，避免多个全局数组互相踩）
//   0=head 1=len 2=dx 3=dy 4=fx 5=fy 6=score 7=best
//   8=alive 9=paused 10=stepMs 11=cell 12=ox 13=oy 14=sw 15=sh
//   16+2i=第 i 节 x，17+2i=第 i 节 y（i=0 是头；40 节上限 ⇒ 16..95）
//   16+2i = segment i's x, 17+2i = segment i's y (i=0 is the head; 40-segment cap ⇒ 16..95)
//   100=候选x 101=候选y 105/106=新方向 107=命中
//   100=candidate x 101=candidate y 105/106=new direction 107=hit
int A[112];

// 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调）。
// UI language: queried once at game start (ui_get_language is a syscall, do not call it every frame).
// 状态表 A[] 刻意只放游戏状态，语言是"界面"的事，单独一个文件级量。
// The state table A[] deliberately holds only game state; language is a matter of the "UI", so it is a separate file-level value.
int Lang;

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
    // Board squares: flat index 0..359; each square is filled or not depending on the parity of (r+c)
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

    ui_text(8, 8, Lang == 0 ? "得分" : "Score", -6643536, 13, 0);
    ui_rect(58, 11, A[6], 10, -11409298, 1, 0, 0);
    ui_text(A[14]/2, 8, Lang == 0 ? "最高" : "Best", -6643536, 13, 1);
    ui_rect(A[14]/2+46, 11, A[7], 10, -63488, 1, 0, 0);
    if (A[9] != 0) {
        ui_text(A[14]/2, A[15]/2, Lang == 0 ? "暂停（SELECT 继续）" : "Paused (SELECT)",
                -131246, 16, 1);
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
    // 结束音：**最低最长的**那一档（`ui_beep` 单音 —— 见文件末的音效说明）
    // End sound: the **lowest and longest** setting (single-tone `ui_beep` — see the sound notes at the end of the file)
    ui_beep(131, 320);
    draw();
    // 选「否/拒绝」→ 退出游戏（ui_dlg_msg 返回 0=是 / 1=否）。
    // Choosing "no/decline" → quit the game (ui_dlg_msg returns 0=yes / 1=no).
    // 此前不接返回值 ⇒ 两个按钮一个样、游戏还退不出去（用户实测报的）。
    // Previously the return value was not used ⇒ both buttons looked the same and the game could not be exited (reported by the user on a real device).
    // 用 A[109] 这个没人用的槽当退出标志 —— gameOver 有四五处调用点，逐个改返回值不划算。
    // Use the unused slot A[109] as the quit flag — gameOver has four or five call sites, so changing the return value at each one is not worth it.
    if (ui_dlg_msg(Lang == 0 ? "贪吃蛇" : "Snake",
                   Lang == 0 ? "撞到了，这一局结束。\n再来一局？（选「否」退出）"
                             : "Crashed - game over.\nPlay again? (choose \"No\" to quit)", 0) != 0) {
        A[109] = 1;
        return;
    }
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
    // Self-collision: only look at the first len-1 segments (the tail moves away in this step)
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
    // Shift everything back by one (no ring wraparound)
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
        // 吃到食物：一声高而短的「叮」（原上行两音的首音）
        // Food eaten: one high, short "ding" (the first note of the original two-note rise)
        ui_beep(1047, 165);
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
    Lang = ui_get_language();
    ui_win_open(Lang == 0 ? "贪吃蛇" : "Snake", w, h);

    // 版面算一次，画与判定共用
    // The layout is computed once and shared by the drawing and the collision checks
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

        if (A[109] != 0) { break; }   // 对话框里选了「否」→ 退出
        // "No" was chosen in the dialog → exit
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
