/* piano.c —— **钢琴**：黑白键 + 多点触控按和弦 + 示范曲 + 录音回放。
 *
 * ## 为什么它必须**轮询** `ui_touch` 而不是收事件
 *
 * 事件式的触摸消息（`VML_MSG_TOUCHDOWN` 等）**只有槽位 0 会投**
 * （`VmlHostRuntime.PostTouch`：所有槽位都更新查询状态，但只在 `slot == 0` 时投消息）。
 * 所以光靠事件最多只能做**单指** —— 按不出和弦。
 * 要十指同时按，就得每拍把 `ui_touch(slot)` 的 0–9 号槽位都查一遍。
 *
 * ⚠ 因此本程序**不用 `ui_timer_set`**：定时器消息会和触摸消息争同一个队列，
 *   而 `ui_wait(m, 16)` 的超时天然就是"至少每 16ms 醒一次"，一拍两用。
 *
 * ## 键盘布局：**自适应列数 + 两行**
 *
 * 竖屏一块屏只放一个八度的话，每个键宽 ~51dp 而高 ~540dp —— 成了 1:10 的**竖长条**，
 * 一点都不像琴键（用户原话："画面是长条"）。所以：
 *
 *   · **每行几个键按屏宽定**（每个键至少约 44dp，手指才按得准），且取 **7 的整数倍** ——
 *     保证一行正好是若干个**完整八度**（落在 B 上收尾，不会切在 mi/si 中间而缺黑键）。
 *     竖屏 360dp ⇒ 7 个（一个八度）；平板 800dp ⇒ 14 个（两个八度）。
 *   · **两行** —— 上行比下行**高一个"每行八度数"**，音域直接翻倍，
 *     而且每行的高度只剩一半 ⇒ 键的宽高比回到像琴键的样子。
 *   · 每行高度再按"不超过宽度的 6 倍"收一道 —— 屏幕很高时别又把键拉成长条。
 *
 * ## 引用计数：为什么不能"抬起就关音"
 *
 * 两根手指先后按同一个键、其中一根先抬起来时，**那个键不该停**
 * （另一根还按着）。所以每个音记一个引用计数，**归零才真的关**。
 * 表按**半音偏移**建（0..25，覆盖两行），一张就够 ——
 * 分白键/黑键两张是"怎么画"的事，与"谁按着"无关。
 *
 * ## ⚠ C 前端的四条硬约束
 *
 * 1. **局部数组必须单独一行声明**（`calc.c:414-422` 踩过：标量与数组同声明会让数组
 *    分不到槽位，连读取指令都不生成 ⇒ 变量恒为 0，很难往语法上想）。
 * 2. **一行一个变量**，别写 `int x = m[1], y = m[2];`。
 * 3. **全局数组的元素要先落局部变量再比较**（`calc.c:99-102` 记过）。
 * 4. `#define` **不支持反斜杠续行** ⇒ 大表只能写成一行；本程序因此用
 *    **记谱字符串**（`"Cq Cq Gq"`）而不是 int 数组来表达曲子。
 */
#include <waycoder_ui.h>

/* ── 尺寸 ── */
static int SW;
static int SH;
static int BARH;      /* 顶部状态栏高 */
static int KEYTOP;    /* 键盘上沿（第一行的顶） */
static int RowKeyH;   /* **每一行**白键的高（不再是整块屏幕的高度） */
static int RowGap;    /* 两行之间的缝 —— 手指按错行时能看出来 */

/* ── 键盘布局 ── */
static int PerRow;    /* 每行几个白键（7 的整数倍：7 或 14） */
static int RowCount;  /* 行数（2） */
static int RowStep;   /* 上下两行差几个半音（= 12 × 每行的八度数） */

/* ── 音域 ──
 * `BaseNote` 是**第一行**的 do；第二行是 `BaseNote + RowStep`。
 * 状态栏那两个「−/+」整体移调（两行一起走），比把键盘挤成一条窄键好得多。 */
static int BaseNote;

/* 白键半音偏移：do re mi fa sol la si */
static int WHITE_SEMI[7];
/* 第 i 个白键右上方有没有黑键（-1 = 没有：mi 和 si 右上方没有） */
static int BLACK_AT[7];
/* 那个黑键的半音偏移 */
static int BLACK_SEMI[7];

/* ── 手指状态 ── */
static int KeyDown[10];    /* 这个槽位按着哪个**音符号**；-1 = 没按 */
static int PrevDown[10];   /* 上一拍这个槽位按着没有 —— 用来取"按下沿"（见按钮那段） */
static int Ref[26];        /* 每个半音偏移（相对 BaseNote）被几根手指按着 */

/* ── 示范曲 ── */
static char* SONG;
static int SongPos;        /* 解析到第几个字符 */
static int SongOn;
static int SongTick;       /* 这一首已经走了几拍 */
static int SongNote;       /* 正在响的示范音（音符号）；-1 = 没响 */
static int SongOffAt;      /* 到这一拍就该关掉 */
static int SongNextAt;     /* 到这一拍取下一个音 */

/* ── 录音 ── */
static int RecOn;
static int RecN;
static int RecTick[384];   /* 事件时刻（拍） */
static int RecNote[384];   /* 音符号；-1 表示"关音" */

/* ── 状态栏按钮（布局时算好，**画与命中同源**）── */
/* 0=移调- 1=移调+ 2=示范曲 3=录音 4=**强制清除** */
static int BtnX[5];
static int BtnY;
static int BtnW;
static int BtnH;

/* ══════════════════════════════════════════════════════════════════════
 * 发声：手弹与示范曲**共用**这两个出口
 * ══════════════════════════════════════════════════════════════════════ */

static void p_on(int ch, int note, int vel)
{
    ui_tone_on(ch, note, vel);
    if (RecOn && RecN < 383) {
        RecTick[RecN] = ui_tick();
        RecNote[RecN] = note;
        RecN = RecN + 1;
    }
}

static void p_off(int ch, int note)
{
    ui_tone_off(ch, note);
    if (RecOn && RecN < 383) {
        RecTick[RecN] = ui_tick();
        RecNote[RecN] = -1;
        RecN = RecN + 1;
    }
}

/* ══════════════════════════════════════════════════════════════════════
 * 键位几何 —— **画与命中同源**（同一个函数算出来的矩形）
 * ══════════════════════════════════════════════════════════════════════ */

static int white_w(void)
{
    return SW / PerRow;
}

/* 第 `row` 行第 `i` 个白键的**上沿** y。 */
static int row_top(int row)
{
    return KEYTOP + row * (RowKeyH + RowGap);
}

/* 第 `row` 行第 `i` 个白键右上方黑键的矩形（out[0..3] = x y w h）。 */
static void black_rect(int row, int i, int* out)
{
    int w;
    int bw;
    w = SW / PerRow;
    bw = w * 62 / 100;
    out[0] = (i + 1) * w - bw / 2;
    out[1] = row_top(row);
    out[2] = bw;
    out[3] = RowKeyH * 62 / 100;
}

/* ══════════════════════════════════════════════════════════════════════
 * 绘制
 * ══════════════════════════════════════════════════════════════════════ */

static void draw(void)
{
    int i;
    int row;
    int r[4];
    int x;
    int w;
    int semi;
    int oct;
    int y0;
    char buf[8];

    ui_clear(0xFF101820);

    /* ── 状态栏 ── */
    ui_set_font(18, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
    ui_set_valign(VML_VANCHOR_MIDDLE);
    ui_text_cur(10, BtnY + BtnH / 2, "钢琴");

    /* 当前音域（下行那排的 do，标成 C2..C7 这样比写数字直观） */
    oct = (BaseNote - 60) / 12 + 4;
    buf[0] = 'C';
    buf[1] = '0' + oct;
    buf[2] = '-';
    buf[3] = '0' + oct + (RowStep / 12);
    buf[4] = 0;
    ui_set_font(19, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
    ui_text_cur(SW / 2 + 26, BtnY + BtnH / 2, buf);

    for (i = 0; i < 5; i++) {
        /* 第 5 个（清除）用暖红底 —— 它是"急停"，混在其余按钮里不好找 */
        ui_rect(BtnX[i], BtnY, BtnW, BtnH, i == 4 ? 0xFF6A2A2A : 0xFF2A3A4A, 1, 0, 8);
        ui_set_font(15, VML_FONT_BOLD, 0xFFDDEEFF, VML_ANCHOR_CENTER);
        ui_set_valign(VML_VANCHOR_MIDDLE);
        if (i == 0) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "-");
        if (i == 1) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "+");
        if (i == 2) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, SongOn ? "停" : "示范");
        if (i == 3) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, RecOn ? "停录" : "录音");
        if (i == 4) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "清音");
    }

    /* ── 两行白键 ── */
    w = white_w();
    for (row = 0; row < RowCount; row++) {
        y0 = row_top(row);
        for (i = 0; i < PerRow; i++) {
            /* ⚠ 全局数组元素先落局部变量（坑 3）——`WHITE_SEMI[i % 7]` 直接
             *   拿去比较/运算都可能读不出正确值。 */
            semi = WHITE_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
            x = i * w;
            ui_rect(x + 1, y0 + 3, w - 2, RowKeyH, 0xFF000000, 1, 0, 6);   /* 底影 */
            if (Ref[semi] > 0) {
                ui_rect(x + 1, y0 + 2, w - 2, RowKeyH - 2, 0xFFFFD98A, 1, 0, 6);  /* 按下：暖色 */
            } else {
                ui_rect(x + 1, y0, w - 2, RowKeyH, 0xFFF2F2F4, 1, 0, 6);
            }
            ui_rect(x + 1, y0, w - 2, RowKeyH, 0xFF8A8A92, 0, 2, 6);        /* 描边 */
        }
    }

    /* ── 黑键（画在白键之上；每行各自铺一遍）── */
    for (row = 0; row < RowCount; row++) {
        for (i = 0; i < PerRow; i++) {
            if (BLACK_AT[i - (i / 7) * 7] < 0) continue;
            semi = BLACK_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
            black_rect(row, i, r);
            ui_rect(r[0], r[1] + 2, r[2], r[3], 0xFF000000, 1, 0, 5);
            if (Ref[semi] > 0) {
                ui_rect(r[0], r[1], r[2], r[3] - 2, 0xFF6A5A3A, 1, 0, 5);
            } else {
                ui_rect(r[0], r[1], r[2], r[3], 0xFF1A1A20, 1, 0, 5);
            }
        }
    }

    ui_present();
}

/* ══════════════════════════════════════════════════════════════════════
 * 命中
 * ══════════════════════════════════════════════════════════════════════ */

/* 打中哪个键？返回**半音偏移**（相对 BaseNote，0..RowStep+12），-1 = 没打中。
 * ⚠ **黑键优先** —— 它压在白键上方，先判白键会让黑键永远按不到。 */
static int hit_key(int x, int y)
{
    int row;
    int i;
    int r[4];
    int w;
    int rel;

    if (y < KEYTOP) return -1;

    /* 落在哪一行？—— 按"行 + 缝"的周期算，再夹到合法行 */
    row = (y - KEYTOP) / (RowKeyH + RowGap);
    if (row < 0) row = 0;
    if (row >= RowCount) row = RowCount - 1;
    if (y >= row_top(row) + RowKeyH) return -1;   /* 正好落在两行之间那道缝里 */

    /* 黑键优先 */
    for (i = 0; i < PerRow; i++) {
        if (BLACK_AT[i - (i / 7) * 7] < 0) continue;
        black_rect(row, i, r);
        if (x >= r[0] && x < r[0] + r[2] && y < r[1] + r[3]) {
            rel = BLACK_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
            return row * RowStep + rel;
        }
    }

    w = SW / PerRow;
    for (i = 0; i < PerRow; i++) {
        if (x >= i * w && x < (i + 1) * w) {
            rel = WHITE_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
            return row * RowStep + rel;
        }
    }
    return -1;
}

static int hit_btn(int x, int y)
{
    int i;
    if (y < BtnY || y >= BtnY + BtnH) return -1;
    for (i = 0; i < 5; i++) {
        if (x >= BtnX[i] && x < BtnX[i] + BtnW) return i;
    }
    return -1;
}

/* ══════════════════════════════════════════════════════════════════════
 * 示范曲：**记谱字符串**（音名 + 时值字母，`.` = 休止）
 *
 * 用字符串而不是 int 表，是因为 `#define` 不支持续行 ⇒ 一张几百项的 int 表
 * 只能塞在一行里，写起来和改起来都是灾难；字符串天然可以按乐句分行。
 * ══════════════════════════════════════════════════════════════════════ */

static int name_to_semi(char c)
{
    if (c == 'C') return 0;
    if (c == 'D') return 2;
    if (c == 'E') return 4;
    if (c == 'F') return 5;
    if (c == 'G') return 7;
    if (c == 'A') return 9;
    if (c == 'B') return 11;
    return -1;
}

/* 取下一个音。返回 0 = 曲子完了。`note` 填 -1 表示休止。 */
static int song_next(int* note, int* hold)
{
    char c;
    int semi;

    while (SONG[SongPos] != 0) {
        c = SONG[SongPos];
        SongPos = SongPos + 1;
        if (c == ' ') continue;
        if (c == '.') { *note = -1; *hold = 300; return 1; }
        semi = name_to_semi(c);
        if (semi < 0) continue;
        *note = 60 + semi;          /* 示范曲固定在中央八度，与当前移调无关 */
        *hold = 380;
        if (SONG[SongPos] == 'h') { *hold = 760; SongPos = SongPos + 1; }
        else if (SONG[SongPos] == 'q') { SongPos = SongPos + 1; }
        return 1;
    }
    return 0;
}

/* ══════════════════════════════════════════════════════════════════════
 * 主循环
 * ══════════════════════════════════════════════════════════════════════ */

/* 按钮的动作。
 * ⚠ **判定**（在轮询里取"按下沿"）与**动作**分开：判定只能有一处，动作也只写一遍。 */
static void apply_btn(int btn)
{
    int i;

    if (btn == 0 || btn == 1) {
        /* 移调前先把响着的都关掉 —— 否则那些音的"相对偏移"会跟着基准一起漂，
         * 引用计数就成了错的（关音时会去减另一个键的账）。 */
        for (i = 0; i < 10; i++) {
            if (KeyDown[i] >= 0) { p_off(i, KeyDown[i]); KeyDown[i] = -1; }
        }
        for (i = 0; i < 26; i++) Ref[i] = 0;
        BaseNote = BaseNote + (btn == 0 ? -RowStep : RowStep);
        if (BaseNote < 36) BaseNote = 36;
        if (BaseNote > 84 - RowStep) BaseNote = 84 - RowStep;
    } else if (btn == 2) {
        if (SongOn) {
            SongOn = 0;
            if (SongNote >= 0) { p_off(0, SongNote); SongNote = -1; }
        } else {
            SongOn = 1; SongPos = 0; SongTick = 0;
            SongNote = -1; SongOffAt = 0; SongNextAt = 0;
        }
    } else if (btn == 3) {
        RecOn = 1 - RecOn;
        if (RecOn) RecN = 0;
    } else if (btn == 4) {
        /* **强制清除**（用户要的"急停"）：把所有正在响的音立刻掐掉。
         *
         * ⚠ 三件事必须**一起**做，少一件这个键就是假的：
         *   ① `ui_tone_panic()` —— 立刻全停（**不是** `ui_tone_all_off()` 的淡出：
         *      这个键的语义就是"马上安静"，卡住的音再拖 90ms 没有意义）；
         *   ② 清**引用计数** —— 不清的话，抬起手指时会去减一个早就清空的账，
         *      越减越负，之后那个键就再也起不来音了；
         *   ③ 清槽位的记账 —— 让"这一拍手指还按着"被当成**新按下**（重新起音）。
         *      于是：还按着的手指会继续弹（那是它本来就该做的），
         *      而**已经卡住/松开了却没收到 KeyUp 的槽位**从此不再发声。
         */
        for (i = 0; i < 26; i++) Ref[i] = 0;
        for (i = 0; i < 10; i++) KeyDown[i] = -1;
        ui_tone_panic();
    }
}

int main(void)
{
    int m[4];
    int t[3];
    int i;
    int slot;
    int semi;
    int note;
    int msgType;
    int n;
    int hold;
    int dirty;
    int hit;
    int btn;
    int perRowOct;

    WHITE_SEMI[0] = 0; WHITE_SEMI[1] = 2; WHITE_SEMI[2] = 4; WHITE_SEMI[3] = 5;
    WHITE_SEMI[4] = 7; WHITE_SEMI[5] = 9; WHITE_SEMI[6] = 11;
    BLACK_AT[0] = 0; BLACK_AT[1] = 1; BLACK_AT[2] = -1; BLACK_AT[3] = 3;
    BLACK_AT[4] = 4; BLACK_AT[5] = 5; BLACK_AT[6] = -1;
    BLACK_SEMI[0] = 1; BLACK_SEMI[1] = 3; BLACK_SEMI[2] = -1; BLACK_SEMI[3] = 6;
    BLACK_SEMI[4] = 8; BLACK_SEMI[5] = 10; BLACK_SEMI[6] = -1;

    for (i = 0; i < 10; i++) KeyDown[i] = -1;
    for (i = 0; i < 26; i++) Ref[i] = 0;

    BaseNote = 60;
    RecOn = 0; RecN = 0;
    SongOn = 0; SongPos = 0; SongTick = 0; SongNote = -1; SongOffAt = 0; SongNextAt = 0;
    SONG = "Cq Cq Gq Gq Aq Aq Gh Fq Fq Eq Eq Dq Dq Ch Gq Gq Fq Fq Eq Eq Dh Gq Gq Fq Fq Eq Eq Dh Cq Cq Gq Gq Aq Aq Gh Fq Fq Eq Eq Dq Dq Ch";

    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;

    /* ── 每行放几个白键：按屏宽自适应 ──
     * 每键至少约 44dp 手指才按得准；取 **7 的整数倍** 保证一行是完整八度。 */
    PerRow = SW / 44;
    PerRow = (PerRow / 7) * 7;
    if (PerRow < 7) PerRow = 7;
    if (PerRow > 14) PerRow = 14;

    /* ── 两行：音域翻倍，且每行只剩一半高 ⇒ 键不再是个竖长条 ── */
    RowCount = 2;
    perRowOct = PerRow / 7;             /* 每行有几个八度 */
    RowStep = 12 * perRowOct;           /* 上行比下行高这么多半音 */

    BARH = 54;
    KEYTOP = BARH + 10;
    RowGap = 6;
    RowKeyH = (SH - KEYTOP - 20 - RowGap * (RowCount - 1)) / RowCount;

    /* ⚠ 再按宽高比收一道：屏幕很高时（平板竖放）别又把键拉成长条。
     *   真实琴键的宽高比大约 1:5~1:6，这里取 6 作上限。 */
    if (RowKeyH > (SW / PerRow) * 6) RowKeyH = (SW / PerRow) * 6;
    if (RowKeyH < 60) RowKeyH = 60;     /* 太矮就按不准了 */

    /* 5 个按钮：`- + 示范 录音 清音`。窄屏上按 4 个的宽度放不下，所以键宽按屏宽算。 */
    BtnH = 32;
    BtnY = (BARH - BtnH) / 2;
    BtnW = (SW - 60) / 5;               /* 左边留出放「钢琴 + 音域」的地方 */
    if (BtnW > 52) BtnW = 52;           /* 宽屏上别拉太宽 */
    if (BtnW < 40) BtnW = 40;           /* 太窄字就挤没了 */
    BtnX[0] = SW - 5 * BtnW - 20;
    BtnX[1] = BtnX[0] + BtnW + 4;
    BtnX[2] = BtnX[1] + BtnW + 6;
    BtnX[3] = BtnX[2] + BtnW + 4;
    BtnX[4] = BtnX[3] + BtnW + 6;

    ui_win_open_ex("钢琴", SW, SH, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    draw();

    while (ui_win_closed() == 0) {
        msgType = ui_wait(m, 16);
        if (msgType == VML_MSG_WINDOWCLOSE) break;

        dirty = 0;

        /* ── 琴键与按钮：**都在轮询里判** ────────────────────────────── */
        for (slot = 0; slot < 10; slot++) {
            if (ui_touch(slot, t) == 0) continue;

            /* 按钮：取**按下沿**（这一拍按下、上一拍没按）。
             *
             * ⚠ 先前只在 slot 0 的**事件消息**里判按钮，于是
             *   "一根手指按着琴键、另一根手指去点清音"——**最需要急停的那个场合**——
             *   反而点不到：事件消息只有 slot 0 会投，而 slot 0 正按着琴键。
             *   统一到轮询之后就都点得到了（16ms 一拍，人手点击至少 50ms，不会漏）。 */
            if (t[2] != 0 && PrevDown[slot] == 0) {
                btn = hit_btn(t[0], t[1]);
                if (btn >= 0) {
                    apply_btn(btn);
                    dirty = 1;
                    PrevDown[slot] = 1;
                    continue;               /* 这一拍就不当琴键处理了 */
                }
            }
            PrevDown[slot] = t[2] != 0;

            if (t[2] != 0) {
                semi = hit_key(t[0], t[1]);
            } else {
                semi = -1;                                      /* 抬起了 */
            }

            note = semi < 0 ? -1 : BaseNote + semi;
            if (note != KeyDown[slot]) {
                /* 这个槽位换了音（含抬起）：先把上一个音还回去 */
                if (KeyDown[slot] >= 0) {
                    hit = KeyDown[slot] - BaseNote;
                    if (hit >= 0 && hit < 26) {
                        if (Ref[hit] > 0) Ref[hit] = Ref[hit] - 1;
                        /* ⚠ **归零才真的关** —— 两根手指按同一个键时，
                         *   先抬起的那根不该把还按着的那根的音关掉 */
                        if (Ref[hit] == 0) p_off(slot, KeyDown[slot]);
                    }
                }
                KeyDown[slot] = note;
                if (note >= 0 && semi >= 0 && semi < 26) {
                    if (Ref[semi] == 0) p_on(slot, note, 100);
                    Ref[semi] = Ref[semi] + 1;
                }
                dirty = 1;
            }
        }

        /* ── 示范曲推进（单声部，起音与关音各按自己的拍子）──────────── */
        if (SongOn) {
            SongTick = SongTick + 16;
            if (SongNote >= 0 && SongTick >= SongOffAt) {
                p_off(0, SongNote);
                SongNote = -1;
            }
            if (SongTick >= SongNextAt) {
                if (song_next(&n, &hold)) {
                    if (n >= 0) {
                        p_on(0, n, 96);
                        SongNote = n;
                        SongOffAt = SongTick + hold * 3 / 4;   /* 留 1/4 空隙，音才分得开 */
                    }
                    SongNextAt = SongTick + hold;
                } else {
                    SongOn = 0;
                    if (SongNote >= 0) { p_off(0, SongNote); SongNote = -1; }
                    dirty = 1;
                }
            }
        }

        if (dirty) draw();
    }

    for (i = 0; i < 10; i++) {
        if (KeyDown[i] >= 0) p_off(i, KeyDown[i]);
    }
    ui_tone_all_off();
    ui_keep_on(0);
    ui_win_close();
    return 0;
}