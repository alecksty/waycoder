// 贪吃蛇 —— 用 **C#** 写的手机游戏（不是 C）
// Snake — a phone game written in **C#** (not C)
//
// ✅ **状态：跑通了**（v0.96.190）。逐像素量：蛇头 316 像素（1 格）、蛇身 632（2 格）、
// ✅ **Status: it runs** (v0.96.190). Measured pixel by pixel: snake head 316 pixels (1 cell), snake body 632 (2 cells),
//   食物 316（1 格）—— 三个数都是精确的整格。
//   food 316 (1 cell) — all three numbers are exactly whole cells.
//
// ## 卡住它的四条（patch 0010，全部在 C# 前端里）
// ## The four things that blocked it (patch 0010, all in the C# frontend)
//
// ① **带参方法"参数串槽"** —— 序言里"把实参读进 R0"那句写成了 `MOVE [R12+12+4i], R0`，
// ① **Parameters of methods with arguments "crossing slots"** — the prologue's "read the argument into R0" was written as `MOVE [R12+12+4i], R0`,
//    而 `MOVE dest, src` 的 dest 在前 ⇒ 那是**把 R0 存进调用方的实参槽**。局部变量于是
//    and in `MOVE dest, src` dest comes first ⇒ that **stores R0 into the caller's argument slot**. The local variable then
//    拿到"调用那一刻 R0 恰好留着的值"（= 调用点最后求值的那个实参）。
//    gets "whatever value R0 happened to hold at the moment of the call" (= the last argument evaluated at the call site).
//    `a1(x)` 侥幸对，`a2(x,y)` 的 y 变成 x（两个矩形完全重叠，看着只有一个），
//    `a1(x)` worked by luck; in `a2(x,y)` the y became x (the two rectangles overlapped exactly, so only one was visible),
//    `a3(x,y,c)` 的 c 变成 x（近黑色，在黑底上看不见）。
//    and in `a3(x,y,c)` the c became x (near-black, invisible on a black background).
// ② **读数组元素永远读到数组头** —— `add R1 R1 R0` 的 2 操作数形式是 `Rd = Rd + Rs`，
// ② **Array element reads always hit the array head** — the 2-operand form of `add R1 R1 R0` is `Rd = Rd + Rs`,
//    而 SHL/ADD 的立即数当 dest 时**结果被丢弃**（VM 只写 `dest.Type == REGISTER`）。
//    and when an immediate is used as dest for SHL/ADD **the result is discarded** (the VM only writes when `dest.Type == REGISTER`).
// ③ **`a` 指向的不是数组块** —— 数组字面量结尾那句 `MOVE R0, LABEL ptrLabel` 取的是
// ③ **`a` does not point at the array block** — the `MOVE R0, LABEL ptrLabel` at the end of an array literal takes
//    **标签地址**（LEA），不是标签里的值；`.Length` 那处也一样。
//    the **label address** (LEA), not the value inside the label; the same goes for `.Length`.
// ④ **`static int[] sx = new int[N]` 从来没分配过** —— 类字段一律 `continue` 掉了，
// ④ **`static int[] sx = new int[N]` was never allocated** — class fields were uniformly `continue`d past,
//    数组块一个字都没留；而 `new int[N]` 的**长度在解析时就被丢掉**，所以连"少分配"
//    leaving not a word of the array block; and the **length of `new int[N]` was dropped during parsing**, so it was not even
//    都谈不上，是"没分配 + 基址是 0"。
//    "under-allocated" — it was "not allocated at all + base address 0".
//
//   ⚠ 四条的**表象各不相同**（参数错位 / 读到数组头 / 读到 0 / 读到低内存垃圾），
//   ⚠ The four **looked completely different** (shifted arguments / reading the array head / reading 0 / reading low-memory garbage),
//   但根子只有两类：**操作数顺序写反**（①②③）与**字段初始化没生成**（④）。
//   but the root causes were only two kinds: **operand order reversed** (①②③) and **field initialization not generated** (④).
//   查它们唯一可靠的办法是**倒出生成的汇编读**（`vmlhost asm`），不是改写法试。
//   The only reliable way to investigate them is to **dump the generated assembly and read it** (`vmlhost asm`), not to try different styles.
//
//   全 22 个前端的结论见 `docs/前端游戏能力评估.md`。
//   For the conclusions across all 22 frontends see the frontend game-capability assessment doc under `docs/`.
//
// ## 为什么能这么写
// ## Why it can be written this way
//
// 手机那套 UI（开窗 / 绘图 / 输入 / 定时器）的实现是 C 写的（`Lib/shared/src/vmlui.c`
// The phone UI (window / drawing / input / timers) is implemented in C (`Lib/shared/src/vmlui.c`
// → `Lib/shared/vmlui.vml`），由 `vmltool.config.xml` 的 `<Language ... Libs="vmlui.vml">`
// → `Lib/shared/vmlui.vml`) and hooked to each frontend by `<Language ... Libs="vmlui.vml">` in `vmltool.config.xml`.
// 挂给每种前端。前端只要能**直接 `call` 那个标签**、且**实参逆序压栈**（包装函数读
// A frontend only needs to **`call` that label directly** with **arguments pushed in reverse order** (the wrapper reads
// `[R12+12]` = 最后压的那个 = C 的第一个参数），就能调 —— 实测 C# 四关全过
// `[R12+12]` = the last one pushed = C's first parameter) to call it — measured: C# passes all four gates
// （编译出图 / 循环 / 数组 / 函数），还过了"定时器 + 收消息 + 重画"的游戏心跳。
// (compiles and draws / loops / arrays / functions), and also the "timer + receive message + redraw" game heartbeat.
//
// ## 常量为什么是数字
// ## Why the constants are numbers
//
// 数值的**唯一真源是 `Lib/c/waycoder_ui.h`**（C 的宏）。C# 前端没有 include C 头文件的机制，
// The **single source of truth for these numbers is `Lib/c/waycoder_ui.h`** (the C macros). The C# frontend has no mechanism to include C headers,
// 所以这里复制了一份。⚠ **改那些宏时要同步这里**。
// so a copy lives here. ⚠ **When you change those macros, sync this copy**.
// 想彻底消掉这份重复，正解是用仓库自带的 `Lib/build_static.sh` 从 C 头生成各语言绑定
// To eliminate this duplication entirely, the right move is to generate per-language bindings from the C headers with the repo's own `Lib/build_static.sh`
// （它会产出 `Lib/<语言>/ext/<名字>.<扩展名>`），那是后面该做的一步。
// (it produces `Lib/<language>/ext/<name>.<ext>`); that is a later step.
//
// ## 操作
// ## Controls
//
// 方向键 / 屏幕手柄方向键转向；START 重开；SELECT 暂停；ESC 或返回箭头退出。
// Arrow keys / the on-screen gamepad's arrows steer; START restarts; SELECT pauses; ESC or the back arrow exits.
// 吃到食物 +10 分并加速；撞墙或撞到自己结束（会弹对话框，别让玩家猜）。
// Eating food is +10 points and a speed-up; hitting a wall or yourself ends the game (a dialog pops up — don't make the player guess).

class Snake
{
    // ── 常量（源：Lib/c/waycoder_ui.h）──
    // ── Constants (source: Lib/c/waycoder_ui.h) ──
    const int MSG_KEYDOWN = 1;
    const int MSG_TIMER = 9;
    const int MSG_WINDOWCLOSE = 10;

    const int KEY_ENTER = 13;
    const int KEY_SELECT = 16;
    const int KEY_ESCAPE = 27;
    const int KEY_LEFT = 37;
    const int KEY_UP = 38;
    const int KEY_RIGHT = 39;
    const int KEY_DOWN = 40;

    const int DLG_INFO = 0;
    const int ANCHOR_LEFT = 0;
    const int ANCHOR_CENTER = 1;

    const int BG = -15724520;        // 0xFF101018 底色
    // 0xFF101018 background
    const int GRID_C = -15263713;    // 0xFF17181F 网格
    // 0xFF17181F grid
    const int SNAKE_C = -11409298;   // 0xFF51E86E 蛇身
    // 0xFF51E86E snake body
    const int HEAD_C = -63488;     // 0xFFFF0800 蛇头
    // 0xFFFF0800 snake head
    const int FOOD_C = -131246;      // 0xFFFDFF52 食物
    // 0xFFFDFF52 food
    const int TEXT_C = -6643536;     // 0xFF9AA0B0 次要文字
    // 0xFF9AA0B0 secondary text
    const int WHITE = -1;            // 0xFFFFFFFF

    // ── 棋盘与蛇 ──
    // ── Board and snake ──
    const int CW = 20;               // 列（格子数）
    // columns (number of cells)
    const int CH = 22;               // 行
    // rows
    const int MAXLEN = 400;          // 20*22 的上限
    // the upper bound of 20*22

    static int[] sx = new int[MAXLEN];
    static int[] sy = new int[MAXLEN];
    static int head;                 // 头在数组里的下标
    // index of the head in the array
    static int len;                  // 长度
    // length
    static int dx;
    static int dy;
    static int fx;
    static int fy;
    static int score;
    static int best;
    static int alive;                // 1=在玩 0=结束
    // 1=playing 0=over
    static int paused;
    static int stepMs;               // 每步毫秒（越吃越快）
    // milliseconds per step (the more you eat, the faster)
    static int lang;                 // 界面语言：0=中文 1=英文（开局问一次宿主，见 Main）
    // UI language: 0=Chinese 1=English (asked of the host once at the start, see Main)

    // ── 版面（开窗时算一次，画与命中判定共用）──
    // ── Layout (computed once when the window opens, shared by drawing and hit-testing) ──
    static int sw;
    static int sh;
    static int cell;                 // 格子边长（dp）
    // cell side length (dp)
    static int ox;                   // 棋盘左上角
    // top-left corner of the board
    static int oy;
    static int hudY;                 // 分数行
    // score row

    static void layout()
    {
        int byw = sw - 8;            // 左右各留 4
        // 4 left on each side
        int byh = sh - 46;           // 顶部让出 40 给分数行
        // leave 40 at the top for the score row
        cell = byw / CW;
        if (byh / CH < cell) cell = byh / CH;
        if (cell < 4) cell = 4;
        ox = (sw - cell * CW) / 2;
        oy = 40;
        hudY = 8;
    }

    static void placeFood()
    {
        int tries = 0;
        while (tries < 500)
        {
            int c = ui_rand(CW);
            int r = ui_rand(CH);
            if (occupied(c, r) == 0) { fx = c; fy = r; return; }
            tries = tries + 1;
        }
        // 兜底：找不到空位就放 (0,0)（棋盘满了本来也就快赢了）
        // Fallback: if no free spot is found, place it at (0,0) (a full board means you were about to win anyway)
        fx = 0; fy = 0;
    }

    static int occupied(int c, int r)
    {
        int i = 0;
        while (i < len)
        {
            int k = head - i;
            while (k < 0) k = k + MAXLEN;
            if (sx[k] == c && sy[k] == r) return 1;
            i = i + 1;
        }
        return 0;
    }

    static void reset()
    {
        len = 3;
        head = 2;
        sx[0] = 8; sy[0] = 10;
        sx[1] = 7; sy[1] = 10;
        sx[2] = 6; sy[2] = 10;
        dx = 1; dy = 0;
        score = 0;
        alive = 1;
        paused = 0;
        stepMs = 170;
        placeFood();
    }

    static void drawCell(int c, int r, int color)
    {
        ui_rect(ox + c * cell, oy + r * cell, cell - 1, cell - 1, color, 1, 0, 2);
    }

    static void draw()
    {
        ui_clear(BG);

        // 棋盘格（淡网格 —— 让玩家看得出格子在哪，不然蛇的移动会显得"跳"）
        // Board cells (a faint grid — so the player can see where the cells are; otherwise the snake's movement looks like it "jumps")
        int c = 0;
        while (c < CW)
        {
            int r = 0;
            while (r < CH)
            {
                if ((c + r) % 2 == 0) drawCell(c, r, GRID_C);
                r = r + 1;
            }
            c = c + 1;
        }

        drawCell(fx, fy, FOOD_C);

        int i = 0;
        while (i < len)
        {
            int k = head - i;
            while (k < 0) k = k + MAXLEN;
            if (i == 0) drawCell(sx[k], sy[k], HEAD_C);
            else drawCell(sx[k], sy[k], SNAKE_C);
            i = i + 1;
        }

        ui_text(8, hudY, lang == 0 ? "得分" : "Score", TEXT_C, 13, ANCHOR_LEFT);
        ui_text(56, hudY, numToStr(score), WHITE, 15, ANCHOR_LEFT);
        ui_text(sw / 2, hudY, lang == 0 ? "最高" : "Best", TEXT_C, 13, ANCHOR_CENTER);
        ui_text(sw / 2 + 44, hudY, numToStr(best), WHITE, 15, ANCHOR_LEFT);
        if (paused != 0) ui_text(sw / 2, sh / 2, lang == 0 ? "暂停（SELECT 继续）" : "Paused (SELECT)", FOOD_C, 16, ANCHOR_CENTER);

        ui_present();
    }

    // 数字 → 字符串。**不能借标准库的字符串转换**：那是 C 的运行库，别的前端不一定有；
    // Number → string. **Cannot borrow the standard library's string conversion**: that is C's runtime library and other frontends don't necessarily have it;
    // 自己按位拆反而哪门语言都能照抄。
    // splitting digits by hand is something any language can copy.
    static string numToStr(int v)
    {
        if (v == 0) return "0";
        string s = "";
        int neg = 0;
        if (v < 0) { neg = 1; v = 0 - v; }
        while (v > 0)
        {
            int d = v % 10;
            s = digit(d) + s;
            v = v / 10;
        }
        if (neg != 0) s = "-" + s;
        return s;
    }

    static string digit(int d)
    {
        if (d == 0) return "0";
        if (d == 1) return "1";
        if (d == 2) return "2";
        if (d == 3) return "3";
        if (d == 4) return "4";
        if (d == 5) return "5";
        if (d == 6) return "6";
        if (d == 7) return "7";
        if (d == 8) return "8";
        return "9";
    }

    // 一步推进。返回 1 表示画面变了（要重画）
    // One step of the game. Returning 1 means the picture changed (a redraw is needed)
    static int step()
    {
        if (alive == 0 || paused != 0) return 0;

        int nc = sx[head] + dx;
        int nr = sy[head] + dy;

        // 撞墙
        // Hit a wall
        if (nc < 0 || nc >= CW || nr < 0 || nr >= CH) { gameOver(); return 1; }
        // 撞自己（尾巴那一格下一步会让开，所以只查到 len-1）
        // Hit itself (the tail cell will move away on the next step, so only scan up to len-1)
        int i = 0;
        while (i < len - 1)
        {
            int k = head - i;
            while (k < 0) k = k + MAXLEN;
            if (sx[k] == nc && sy[k] == nr) { gameOver(); return 1; }
            i = i + 1;
        }

        int eat = 0;
        if (nc == fx && nr == fy) eat = 1;

        // 前进：头指针前移一格；不吃就把尾巴缩掉（靠 len 控制，不用真删数据）
        // Advance: move the head pointer forward one cell; if nothing is eaten shrink the tail (controlled by len, no real data deletion)
        head = head + 1;
        if (head >= MAXLEN) head = 0;
        sx[head] = nc;
        sy[head] = nr;
        if (eat != 0)
        {
            if (len < MAXLEN) len = len + 1;
            score = score + 10;
            if (score > best) best = score;
            if (stepMs > 70) stepMs = stepMs - 6;    // 越吃越快，但留个下限
            // the more you eat the faster, but with a floor
            // 音效：单音 ui_beep（v0.96.509 从音序器换回来 ——
            // Sound: single-tone ui_beep (switched back from the sequencer in v0.96.509 —
            //   那一版多声部叠加 / 长音拖尾在真机上破音）
            //   that version's multi-voice stacking / long-note tails broke up on real devices)
            ui_beep(1047, 165);
            placeFood();
        }
        return 1;
    }

    static void gameOver()
    {
        alive = 0;
        // 音效：单音 ui_beep；**结局音取最低音**（吃到 1047 / 撞到 131，差得开）
        // Sound: single-tone ui_beep; **the ending tone takes the lowest note** (1047 on eating / 131 on a crash — far enough apart)
        ui_beep(131, 320);
        draw();                                    // 先把终局画面画出来
        // draw the final frame first
        if (ui_dlg_msg(lang == 0 ? "贪吃蛇" : "Snake", lang == 0 ? "撞到了，这一局结束。\n再来一局？（选「否」退出）" : "You crashed. Round over.\nPlay again? (choose \"No\" to quit)", DLG_INFO) != 0) { ui_win_close(); return; }
        reset();
    }

    static void turn(int nx, int ny)
    {
        // 不能原地掉头（那会立刻撞自己）
        // Cannot reverse in place (that would immediately hit itself)
        if (nx + dx == 0 && ny + dy == 0) return;
        dx = nx;
        dy = ny;
    }

    static void Main()
    {
        sw = ui_scr_w();
        sh = ui_scr_h();
        if (sw <= 0) sw = 360;
        if (sh <= 0) sh = 620;
        // 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），之后整局按它分支。
        // UI language: ask the host once at the start whether it wants Chinese or English (0=Chinese 1=English), then branch on it for the whole game.
        // ⚠ 别在每帧里调 —— 那是一次 syscall。
        // ⚠ Don't call it in every frame — that's a syscall.
        lang = ui_get_language();
        ui_win_open(lang == 0 ? "贪吃蛇" : "Snake", sw, sh);
        layout();
        ui_keep_on(1);
        reset();
        draw();

        int tid = ui_timer_set(stepMs, 0);
        int curMs = stepMs;

        while (ui_win_closed() == 0)

        {
            int t = ui_wait_msg(0);
            if (t == 0) continue;
            if (t == MSG_WINDOWCLOSE) break;

            if (t == MSG_TIMER)
            {
                if (step() != 0) draw();
                // 提速后要把定时器换掉（重复定时器的间隔是建立时定死的）
                // After speeding up you must replace the timer (a repeating timer's interval is fixed when it is created)
                if (stepMs != curMs)
                {
                    ui_timer_kill(tid);
                    curMs = stepMs;
                    tid = ui_timer_set(curMs, 0);
                }
                continue;
            }

            if (t == MSG_KEYDOWN)
            {
                int k = ui_msg_a();
                if (k == KEY_ESCAPE) break;
                if (k == KEY_LEFT) turn(-1, 0);
                else if (k == KEY_RIGHT) turn(1, 0);
                else if (k == KEY_UP) turn(0, -1);
                else if (k == KEY_DOWN) turn(0, 1);
                else if (k == KEY_ENTER) { reset(); draw(); }
                else if (k == KEY_SELECT) { if (paused != 0) paused = 0; else paused = 1; draw(); }
            }
        }

        ui_timer_kill(tid);
        ui_keep_on(0);
        ui_win_close();
    }
}
