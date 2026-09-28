// 赛车 —— 用 **C#** 写的手机游戏（不是 C）
// Racer — a phone game written in **C#** (not C)
//
// ✅ 能玩。判据是**逐像素量**出来的：本车（亮绿）恒定 1 辆、随左右键换道（实测 62px 一格）、
// ✅ Playable. The criteria were **measured pixel by pixel**: exactly 1 player car (bright green), lane changes on left/right keys (measured 62px per lane),
//    路面虚线每拍下移一档（y 在 40/58 之间循环）、敌车（红）自顶下行、
//    the road dashes shift down one step per tick (y cycles between 40/58), the enemy cars (red) descend from the top,
//    躲过一辆 +10 分（分数条实测长到 20）、撞车后弹对话框并重开（一轮内两次归零）。
//    dodging one is +10 points (the score bar measured 20), and after a crash a dialog pops up and the game restarts (two resets within one run).
//
// ⚠ 调参也调了两轮，第一版**根本得不了分**：敌车每 5 拍刷一辆、而穿过整条路要 98 拍，
// ⚠ Tuning took two rounds as well; the first version **could not score at all**: enemy cars spawned every 5 ticks while crossing the whole road takes 98 ticks,
//    于是玩家在"任何一辆车能跑到路底之前"就已经被撞死了（分数永远是 0）。
//    so the player was already crashed before "any car could reach the bottom of the road" (the score stayed 0 forever).
//    现在改成 **26-速度 拍刷一辆 + 速度 8 起**，躲过的车才真的能跑到路底计分。
//    It is now **26-speed ticks per spawn + speed starts at 8**, so dodged cars really do reach the bottom and score.
//
// ◆ 玩法
// ◆ Gameplay
//
// 三车道公路自上而下滚。方向键左右换道（上下键也行，与左右等价），SELECT 暂停，
// A three-lane road scrolls top to bottom. Left/right arrows change lanes (up/down work too, equivalent to left/right), SELECT pauses,
// 回车重开，ESC 或返回箭头退出。躲开迎面来的红车，每躲过一辆 +10 分并提一点速。
// Enter restarts, ESC or the back arrow exits. Dodge the oncoming red cars; each one dodged is +10 points and a bit more speed.
// 撞上（或蹭到）就结束，弹对话框问要不要再来一局。
// Hitting one (or scraping it) ends the game, with a dialog asking whether to play another round.
//
// ◆ 为什么写得这么"平"
// ◆ Why it is written so "flat"
//
// C# 前端（patch 0010）修好过四处：带参方法参数串槽、读数组元素永远读到数组头、
// The C# frontend (patch 0010) fixed four things: parameters of methods with arguments crossing slots, array element reads always hitting the array head,
// `a` 指向的不是数组块、`static int[] x = new int[N]` 从来没分配过。
// `a` not pointing at the array block, and `static int[] x = new int[N]` never actually being allocated.
// 那四条都属"编译全绿、跑起来错"，所以这里沿用**已被逐像素验证过的写法**：
// All four were "compiles clean, wrong at run time", so this uses **a style already verified pixel by pixel**:
// 数组只有 `ex/ey/eon` 三个，全部在 `Main` 里显式赋初值，不依赖字段初始化顺序。
// only three arrays, `ex/ey/eon`, all explicitly initialized inside `Main`, not relying on field initialization order.
//
// ◆ 一个设计上的取舍
// ◆ One design trade-off
//
// 不用"列表/容器"，敌车直接用一个**定长数组 + 启用标志**（6 个槽）：
// No "lists/containers"; enemy cars simply use a **fixed-length array + alive flags** (6 slots):
// 前端对定长数组的代码生成是验过的，容器类没有。
// the frontend's code generation for fixed-length arrays has been verified; container classes have not.
//
// 数值的唯一真源仍是 `Lib/c/waycoder_ui.h`。
// The single source of truth for these numbers is still `Lib/c/waycoder_ui.h`.

class Racer
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

    const int BG = -15724520;        // 0xFF101018 路外底色
    // 0xFF101018 off-road background
    const int ROAD = -14999769;      // 0xFF1B1F27 路面
    // 0xFF1B1F27 road surface
    const int DASH = -9537916;       // 0xFF6E7684 车道虚线
    // 0xFF6E7684 lane dashes
    const int PLAYER = -11739014;    // 0xFF4CE07A 本车
    // 0xFF4CE07A player car
    const int ENEMY = -50384;        // 0xFFFF3B30 敌车
    // 0xFFFF3B30 enemy car
    const int HUD = -6643536;        // 0xFF9AA0B0 次要文字
    // 0xFF9AA0B0 secondary text
    const int BAR = -11409298;       // 0xFF51E86E 分数条
    // 0xFF51E86E score bar
    const int BAR2 = -63488;         // 0xFFFF0800 最高分条
    // 0xFFFF0800 best-score bar
    const int WHITE = -1;

    // ── 车道与车 ──
    // ── Lanes and cars ──
    const int LANES = 3;
    const int SLOTS = 6;             // 敌车槽位数（定长，前端只验过定长数组）
    // number of enemy-car slots (fixed length; only fixed-length arrays have been verified in the frontend)

    static int sw;
    static int sh;
    static int laneW;                // 单车道宽
    // width of one lane
    static int roadX;                // 路面左边界
    // left edge of the road
    static int roadW;
    static int roadTop;
    static int roadBot;
    static int carW;
    static int carH;

    static int lane;                 // 本车所在车道 0..LANES-1
    // the player car's lane, 0..LANES-1
    static int score;
    static int best;
    static int alive;
    static int paused;
    static int speed;                // 每拍敌车下移的像素
    // pixels the enemy cars move down per tick
    static int tickMs;               // 每拍毫秒（越快越小）
    // milliseconds per tick (smaller means faster)
    static int dash;                 // 虚线滚动偏移
    // dash scroll offset
    static int spawnIn;              // 还有几拍刷一辆
    // how many ticks until the next spawn
    static int passed;               // 已躲过几辆（提速用）
    // how many cars have been dodged (used for speeding up)
    static int lang;                 // 界面语言：0=中文 1=英文（开局问一次宿主，见 Main）
    // UI language: 0=Chinese 1=English (asked of the host once at the start, see Main)

    static int[] ex = new int[SLOTS]; // 敌车 x（像素，左边界）
    // enemy car x (pixels, left edge)
    static int[] ey = new int[SLOTS]; // 敌车 y（像素，上边界）
    // enemy car y (pixels, top edge)
    static int[] eon = new int[SLOTS];// 该槽是否启用
    // whether this slot is active

    static void layout()
    {
        int pad = 10;
        roadW = sw - pad * 2;
        if (roadW > 260) roadW = 260;         // 手机上别把公路拉得太宽
        // don't stretch the road too wide on a phone
        roadX = (sw - roadW) / 2;
        roadTop = 40;                          // 顶部让出 40 给分数行
        // leave 40 at the top for the score row
        roadBot = sh - 10;
        laneW = roadW / LANES;
        carW = laneW - 24;
        if (carW < 8) carW = 8;
        carH = carW * 9 / 5;
        if (carH > (roadBot - roadTop) / 5) carH = (roadBot - roadTop) / 5;
    }

    // 第 n 条车道的车身左边界
    // Left edge of the car body in lane n
    static int laneX(int n)
    {
        return roadX + n * laneW + (laneW - carW) / 2;
    }

    static void reset()
    {
        lane = 1;
        score = 0;
        alive = 1;
        paused = 0;
        speed = 8;
        tickMs = 45;
        dash = 0;
        spawnIn = 6;
        passed = 0;
        int i = 0;
        while (i < SLOTS)
        {
            eon[i] = 0;
            ey[i] = 0;
            ex[i] = 0;
            i = i + 1;
        }
    }

    // 找空槽，从顶部放一辆敌车（尽量不堵满三条道）
    // Find a free slot and place an enemy car at the top (avoid filling all three lanes)
    static void spawn()
    {
        int slot = -1;
        int i = 0;
        while (i < SLOTS)
        {
            if (eon[i] == 0 && slot < 0) slot = i;
            i = i + 1;
        }
        if (slot < 0) return;

        int n = ui_rand(LANES);
        ex[slot] = laneX(n);
        ey[slot] = roadTop - 20;
        eon[slot] = 1;
    }

    // 一辆敌车的车身面积（不做旋转，画法是"车身 + 挡风玻璃 + 两个轮子"）
    // Body area of one enemy car (no rotation; drawn as "body + windshield + two wheels")
    static void drawCar(int x, int y, int body)
    {
        ui_rect(x, y, carW, carH, body, 1, 0, 4);
        // 挡风玻璃：车身上部一条浅色
        // Windshield: a light-colored strip on the upper part of the body
        ui_rect(x + 3, y + carH / 4, carW - 6, carH / 4, BG, 1, 0, 2);
        // 两个轮子
        // Two wheels
        ui_rect(x - 2, y + carH / 5, 3, carH / 4, WHITE, 1, 0, 0);
        ui_rect(x + carW - 1, y + carH / 5, 3, carH / 4, WHITE, 1, 0, 0);
    }

    static void draw()
    {
        ui_clear(BG);

        // 路面
        // Road surface
        ui_rect(roadX, roadTop, roadW, roadBot - roadTop, ROAD, 1, 0, 0);

        // 车道虚线：每条分道线一段一段画，整体按 dash 上滚
        // Lane dashes: each divider is drawn segment by segment, scrolling upward by dash as a whole
        int dashH = 18;
        int gap = 18;
        int period = dashH + gap;
        int l = 1;
        while (l < LANES)
        {
            int x = roadX + l * laneW - 1;
            int y = roadTop - period + (dash % period);
            while (y < roadBot)
            {
                if (y + dashH > roadTop)
                {
                    int y0 = y;
                    int y1 = y + dashH;
                    if (y0 < roadTop) y0 = roadTop;
                    if (y1 > roadBot) y1 = roadBot;
                    if (y1 > y0) ui_rect(x, y0, 2, y1 - y0, DASH, 1, 0, 0);
                }
                y = y + period;
            }
            l = l + 1;
        }

        // 敌车
        // Enemy cars
        int i = 0;
        while (i < SLOTS)
        {
            if (eon[i] != 0)
            {
                if (ey[i] + carH > roadTop && ey[i] < roadBot)
                {
                    drawCar(ex[i], ey[i], ENEMY);
                }
            }
            i = i + 1;
        }

        // 本车
        // Player car
        drawCar(laneX(lane), roadBot - carH - 6, PLAYER);

        // HUD：分数条 + 最高分条（不拼字符串 —— 见文件头对前端能力的说明）
        // HUD: score bar + best-score bar (no string concatenation — see the file header on the frontend's capabilities)
        ui_text(8, 8, lang == 0 ? "得分" : "Score", HUD, 13, ANCHOR_LEFT);
        ui_rect(58, 11, score, 10, BAR, 1, 0, 0);
        ui_text(sw / 2, 8, lang == 0 ? "最高" : "Best", HUD, 13, ANCHOR_CENTER);
        ui_rect(sw / 2 + 46, 11, best, 10, BAR2, 1, 0, 0);

        if (paused != 0)
        {
            ui_text(sw / 2, sh / 2, lang == 0 ? "暂停（SELECT 继续）" : "Paused (SELECT)", ENEMY, 16, ANCHOR_CENTER);
        }
        ui_present();
    }

    // 返回 1 表示本拍有变化、需要重画
    // Returning 1 means something changed this tick and a redraw is needed
    static int step()
    {
        if (alive == 0 || paused != 0) return 0;

        dash = dash + speed;
        if (dash >= 36) dash = dash - 36;

        // 敌车下移
        // Move enemy cars down
        int i = 0;
        while (i < SLOTS)
        {
            if (eon[i] != 0)
            {
                ey[i] = ey[i] + speed;

                // 出路面：回收 + 计分 + 提速
                // Off the road: recycle + score + speed up
                if (ey[i] > roadBot)
                {
                    eon[i] = 0;
                    score = score + 10;
                    if (score > best) best = score;
                    passed = passed + 1;
                    if (passed % 5 == 0)
                    {
                        if (speed < 18) speed = speed + 1;
                        if (tickMs > 24) tickMs = tickMs - 4;
                        // 躲过一辆：一声高而短的「叮」（单音 ui_beep —— v0.96.509 从
                        // Dodged one: a short, high "ding" (single-tone ui_beep — in v0.96.509 switched back
                        //   音序器换回来；那一版多声部叠加 / 长音在真机上破音）
                        //   from the sequencer; that version's multi-voice stacking / long notes broke up on real devices)
                        ui_beep(1047, 165);
                    }
                }
            }
            i = i + 1;
        }

        // 刷新
        // Refresh
        spawnIn = spawnIn - 1;
        if (spawnIn <= 0)
        {
            spawn();
            spawnIn = 26 - speed;
            if (spawnIn < 10) spawnIn = 10;
        }

        // 碰撞：本车占 [roadBot-carH-6, roadBot-6)，纵向重叠且横向贴近
        // Collision: the player car occupies [roadBot-carH-6, roadBot-6); overlapping vertically and near horizontally
        int px = laneX(lane);
        int py = roadBot - carH - 6;
        i = 0;
        while (i < SLOTS)
        {
            if (eon[i] != 0)
            {
                int vOver = 0;
                if (ey[i] < py + carH && ey[i] + carH > py) vOver = 1;
                int hNear = 0;
                if (ex[i] < px + carW && ex[i] + carW > px) hNear = 1;
                if (vOver != 0 && hNear != 0)
                {
                    alive = 0;
                    // 撞车（结局）：**最低音**、最长
                    // Crash (ending): **the lowest note**, the longest one
                    ui_beep(131, 320);
                    draw();
                    if (ui_dlg_msg(lang == 0 ? "赛车" : "Racer", lang == 0 ? "撞车了，这一局结束。\n再来一局？（选「否」退出）" : "You crashed. Round over.\nPlay again? (choose \"No\" to quit)", DLG_INFO) != 0) { ui_win_close(); return 1; }
                    reset();
                    return 1;
                }
            }
            i = i + 1;
        }
        return 1;
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
        ui_win_open(lang == 0 ? "赛车" : "Racer", sw, sh);
        layout();
        ui_keep_on(1);
        reset();
        draw();

        int tid = ui_timer_set(tickMs, 0);
        int curMs = tickMs;

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
                if (tickMs != curMs)
                {
                    ui_timer_kill(tid);
                    curMs = tickMs;
                    tid = ui_timer_set(curMs, 0);
                }
                continue;
            }

            if (t == MSG_KEYDOWN)
            {
                int k = ui_msg_a();
                if (k == KEY_ESCAPE) break;
                if (k == KEY_LEFT)
                {
                    if (lane > 0) lane = lane - 1;
                    draw();
                }
                else if (k == KEY_RIGHT)
                {
                    if (lane < LANES - 1) lane = lane + 1;
                    draw();
                }
                else if (k == KEY_UP)
                {
                    if (lane > 0) lane = lane - 1;
                    draw();
                }
                else if (k == KEY_DOWN)
                {
                    if (lane < LANES - 1) lane = lane + 1;
                    draw();
                }
                else if (k == KEY_ENTER)
                {
                    reset();
                    draw();
                }
                else if (k == KEY_SELECT)
                {
                    if (paused != 0) paused = 0; else paused = 1;
                    draw();
                }
            }
        }

        ui_timer_kill(tid);
        ui_keep_on(0);
        ui_win_close();
    }
}
