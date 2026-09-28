// 贪吃蛇 —— 用 **ObjC** 写的手机游戏
// Snake — a phone game written in **ObjC**
//
// ◆ 手机那套 UI 怎么来的
// ◆ Where the phone UI comes from
//
// 开窗 / 绘图 / 输入 / 定时器的实现是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// Window / drawing / input / timers are implemented in C (`Lib/shared/src/vmlui.c` → `vmlui.vml`),
// 由 `vmltool.config.xml` 的 `<Language Name="objc" Libs="vmlui.vml">` 挂给每个前端。
// hooked to each frontend by `<Language Name="objc" Libs="vmlui.vml">` in `vmltool.config.xml`.
// 数值的唯一真源是 `Lib/c/waycoder_ui.h`。
// The single source of truth for these numbers is `Lib/c/waycoder_ui.h`.
//
// ◆ ObjC 这一路原本跑不了游戏，两条缺陷修完才通（2026-09-17）
// ◆ This ObjC path could not run games at first; it only worked after two defects were fixed (2026-09-17)
//
//   · **帧比局部量偏移少 4 字节**：文件级声明（全局量、函数原型）走基类 `EmitStoreVar`
//   · **The frame was 4 bytes smaller than the locals' offset**: file-level declarations (globals, function prototypes) went through the base class's `EmitStoreVar`
//     的兜底分配 `AllocAndRegisterVar` —— 它抬高 `nextStackOffset` 却**不发 `SUB`**，
//     fallback allocation `AllocAndRegisterVar` — it raised `nextStackOffset` without **emitting `SUB`**,
//     每有一条这样的声明，帧就比偏移少 4 字节。实测多一条函数原型即让局部量落到
//     so each such declaration left the frame 4 bytes short of the offset. Measured: one extra function prototype put a local at
//     `R12-12` 而帧只有 8 ⇒ 循环变量正好落在 push 区、被循环体那三个 push 写花。
//     `R12-12` while the frame was only 8 ⇒ the loop variable landed exactly in the push area and was clobbered by the loop body's three pushes.
//     判据 `drift.m` 由 2059 转 126。修法是 Dart 那套：序言占位 `SUB R13,#0`、
//     The `drift.m` criterion went from 2059 to 126. The fix is Dart's approach: a placeholder `SUB R13,#0` in the prologue,
//     函数体生成完回填 `nextStackOffset + 8`。
//     backfilled with `nextStackOffset + 8` once the function body is generated.
//
// ◆ 写法约束（每条都实测过，不是偏好）
// ◆ Style constraints (each one measured, not a preference)
//
//   ① **循环更新写 `i = i + 1`，不能写 `i++`** —— `++` 只 `ADD R0` 不写回
//   ① **Write the loop update as `i = i + 1`, never `i++`** — `++` only does `ADD R0` without writing back
//      （`CodeGenerator.Expressions.cs:235`）⇒ `for (…; …; i++)` 死循环。
//      (`CodeGenerator.Expressions.cs:235`) ⇒ `for (…; …; i++)` loops forever.
//   ② **数组没有初始化列表**：`int a[4] = {1,2,3,4};` 的初值会被丢掉
//   ② **Arrays have no initializer lists**: the values in `int a[4] = {1,2,3,4};` are dropped
//      ⇒ 只能逐元素赋值。
//      ⇒ they can only be assigned element by element.
//   ③ **颜色写负数十进制**：词法器十进制专用（`Lexer.cs:123`），不认 `0x`。
//   ③ **Write colors as negative decimals**: the lexer is decimal-only (`Lexer.cs:123`) and does not recognize `0x`.
//   ④ 状态放**一个**文件级数组、辅助函数一律无参 —— 与 Go/C# 那些示例同一处置。
//   ④ State goes in **one** file-level array and helper functions take no arguments — the same treatment as the Go/C# examples.
//
// ◆ 操作
// ◆ Controls
//
// 方向键转向；SELECT 暂停；回车重开；ESC 或返回箭头退出。
// Arrow keys steer; SELECT pauses; Enter restarts; ESC or the back arrow exits.
// 吃到食物 +10 分并加速；撞墙或撞到自己结束（弹对话框问要不要再来一局）。
// Eating food is +10 points and a speed-up; hitting a wall or yourself ends the game (a dialog asks whether to play another round).
//
// ⚠ 与 Go/C#/Swift 那几份一样，**「开出绘图窗口」这条判据只能在设备上验**
// ⚠ As with the Go/C#/Swift copies, **the "opens a drawing window" criterion can only be verified on a device**
//   （桌面 vmlcli 的 ui_* 是宿主桩，主循环不会自然退出）。
//   (on the desktop, vmlcli's ui_* are host stubs and the main loop never exits on its own).
//   设备验收条目见 `scripts/maui-vml-verify/corpus.tsv` 的 `game-objc-snake`。
//   For the device acceptance entry see `game-objc-snake` in `scripts/maui-vml-verify/corpus.tsv`.

// 状态表（一个数组放全部，避免多个全局数组互相踩）
//   0=head 1=len 2=dx 3=dy 4=fx 5=fy 6=score 7=best
//   8=alive 9=paused 10=stepMs 11=cell 12=ox 13=oy 14=sw 15=sh
//   16+2i=第 i 节 x，17+2i=第 i 节 y（i=0 是头；40 节上限 ⇒ 16..95）
//   16+2i=x of segment i, 17+2i=y of segment i (i=0 is the head; 40 segments max ⇒ 16..95)
//   100=候选x 101=候选y 105/106=新方向 107=命中 108=临时
//   100=candidate x, 101=candidate y, 105/106=new direction, 107=hit, 108=temp
//   110=界面语言（0=中文 1=英文，开局问一次宿主；见 draw 里那段说明）
//   110=UI language (0=Chinese 1=English, asked of the host once at the start; see the note in draw)
int A[112];

// 扫一遍身上的每一节，看 (A[100],A[101]) 这格有没有被占；结果写 A[107]
// Scan every segment of the body to see whether the cell (A[100],A[101]) is occupied; write the result into A[107]
void occupied() {
    int i;
    A[107] = 0;
    i = 0;
    while (i < A[1]) {
        if (A[16+i*2] == A[100]) {
            if (A[17+i*2] == A[101]) {
                A[107] = 1;
            }
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

    // 棋盘格：扁平序号 0..359，行列内联算出来；每格按 (r+c) 的奇偶决定铺不铺
    // Board cells: flat index 0..359, row/column computed inline; whether each cell is filled depends on the parity of (r+c)
    cellN = 0;
    while (cellN < 360) {
        if ((cellN+cellN/20)-((cellN+cellN/20)/2)*2 == 0) {
            ui_rect(A[12]+(cellN-(cellN/20)*20)*A[11], A[13]+(cellN/20)*A[11],
                    A[11]-1, A[11]-1, -15263713, 1, 0, 2);
        }
        cellN = cellN + 1;
    }

    // 食物
    // Food
    ui_rect(A[12]+A[4]*A[11], A[13]+A[5]*A[11], A[11]-1, A[11]-1, -131246, 1, 0, 2);

    // 蛇：i=0 是头
    // Snake: i=0 is the head
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

    // 界面语言（0=中文 1=英文）：从状态表里取，**每个函数各取一次当局部量** ——
    // UI language (0=Chinese 1=English): take it from the state table, **once per function into a local** —
    // 本前端没有三元运算符（`? :` 只到词法，解析器未实现，见 OBJC_LANGUAGE_SPEC 的缺口表），
    // this frontend has no ternary operator (`? :` stops at the lexer, the parser never implements it — see the gap table in OBJC_LANGUAGE_SPEC),
    // 所以界面文字只能靠 `if/else` 二选一、把调用写两遍。
    // so UI text can only be an `if/else` choice that writes the call twice.
    int lang;
    lang = A[110];
    if (lang == 0) ui_text(8, 8, "得分", -6643536, 13, 0);
    else ui_text(8, 8, "Score", -6643536, 13, 0);
    ui_rect(58, 11, A[6], 10, -11409298, 1, 0, 0);
    if (lang == 0) ui_text(A[14]/2, 8, "最高", -6643536, 13, 1);
    else ui_text(A[14]/2, 8, "Best", -6643536, 13, 1);
    ui_rect(A[14]/2+46, 11, A[7], 10, -63488, 1, 0, 0);
    if (A[9] != 0) {
        if (lang == 0) ui_text(A[14]/2, A[15]/2, "暂停（SELECT 继续）", -131246, 16, 1);
        else ui_text(A[14]/2, A[15]/2, "Paused (SELECT)", -131246, 16, 1);
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
    // 按键音略：ui_beep 在 Lib/ 里是「声明了但未实现」（见文件头）
    // Key sound omitted: ui_beep is "declared but not implemented" in Lib/ (see the file header)
    draw();
    // 选「否/拒绝」→ 退出游戏（ui_dlg_msg 返回 0=是 / 1=否）。
    // Choosing "No / refuse" → exit the game (ui_dlg_msg returns 0=yes / 1=no).
    // 此前不接返回值 ⇒ 两个按钮一个样、游戏还退不出去（用户实测报的）。
    // Previously the return value was ignored ⇒ both buttons behaved the same and the game could not be exited (reported by the user on a real device).
    // 用 A[109] 这个没人用的槽当退出标志 —— gameOver 有四五处调用点，逐个改返回值不划算。
    // A[109] is an unused slot and serves as the exit flag — gameOver has four or five call sites, so changing the return value at each is not worth it.
    int lang;
    int ans;
    lang = A[110];
    if (lang == 0) ans = ui_dlg_msg("贪吃蛇", "撞到了，这一局结束。\n再来一局？（选「否」退出）", 0);
    else ans = ui_dlg_msg("Snake", "You crashed. Round over.\nPlay again? (choose \"No\" to quit)", 0);
    if (ans != 0) {
        A[109] = 1;
        return;
    }
    resetGame();
}

void step() {
    int nc;
    int nr;
    int i;
    if (A[8] == 0) {
        return;
    }
    if (A[9] != 0) {
        return;
    }
    nc = A[16] + A[2];
    nr = A[17] + A[3];
    if (nc < 0) { gameOver(); return; }
    if (nc >= 20) { gameOver(); return; }
    if (nr < 0) { gameOver(); return; }
    if (nr >= 18) { gameOver(); return; }

    // 自撞：只看前 len-1 节（尾巴这一步会挪走）
    // Self-collision: only look at the first len-1 segments (the tail moves away on this step)
    A[100] = nc;
    A[101] = nr;
    A[107] = 0;
    i = 0;
    while (i < A[1]-1) {
        if (A[16+i*2] == nc) {
            if (A[17+i*2] == nr) {
                A[107] = 1;
            }
        }
        i = i + 1;
    }
    if (A[107] != 0) {
        gameOver();
        return;
    }

    // 整体后移一位（不做环形回绕）
    // Shift the whole body back by one (no wrap-around)
    i = A[1];
    while (i > 0) {
        A[16+i*2] = A[16+(i-1)*2];
        A[17+i*2] = A[17+(i-1)*2];
        i = i - 1;
    }
    A[16] = nc;
    A[17] = nr;

    if (nc == A[4]) {
        if (nr == A[5]) {
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
            placeFood();
        }
    }
}

void turn() {
    if (A[105]+A[2] == 0) {
        if (A[106]+A[3] == 0) {
            return;
        }
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
    if (w <= 0) {
        w = 360;
    }
    if (h <= 0) {
        h = 620;
    }
    A[14] = w;
    A[15] = h;
    // 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），存进 A[110]（见 draw 的说明）。
    // UI language: ask the host once at the start whether it wants Chinese or English (0=Chinese 1=English) and store it in A[110] (see the note in draw).
    // ⚠ 别在每帧里调 —— 那是一次 syscall。
    // ⚠ Don't call it in every frame — that's a syscall.
    int lang;
    A[110] = ui_get_language();
    lang = A[110];
    if (lang == 0) ui_win_open("贪吃蛇", w, h);
    else ui_win_open("Snake", w, h);

    // 版面算一次，画与判定共用
    // The layout is computed once and shared by drawing and hit-testing
    byw = w - 8;
    byh = h - 46;
    A[11] = byw / 20;
    if (byh/18 < A[11]) {
        A[11] = byh / 18;
    }
    if (A[11] < 4) {
        A[11] = 4;
    }
    A[12] = (w - A[11]*20) / 2;
    A[13] = 40;

    resetGame();

    tid = ui_timer_set(A[10], 0);
    curMs = A[10];

    while (ui_win_closed() == 0) {
        if (A[109] != 0) { break; }   // 对话框里选了「否」→ 退出
        // "No" was chosen in the dialog → exit
        draw();
        t = ui_wait_msg(0);
        if (t == 10) {
            break;
        }
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
            if (k == 27) {
                break;
            }
            if (k == 37) { A[105] = -1; A[106] = 0; turn(); }
            if (k == 39) { A[105] = 1;  A[106] = 0; turn(); }
            if (k == 38) { A[105] = 0;  A[106] = -1; turn(); }
            if (k == 40) { A[105] = 0;  A[106] = 1; turn(); }
            if (k == 13) { resetGame(); }
            if (k == 16) { A[9] = 1 - A[9]; }
        }
    }

    ui_timer_kill(tid);
    ui_win_close();
    return 0;
}
