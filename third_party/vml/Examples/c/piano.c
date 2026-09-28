/* piano.c —— **钢琴**：黑白键 + 多点触控按和弦 + 示范曲 + 录音回放。
 * piano.c — **Piano**: black/white keys + multi-touch chords + a demo song + record/playback.
 *
 * ## 为什么它必须**轮询** `ui_touch` 而不是收事件
 * ## Why it must **poll** `ui_touch` instead of receiving events
 *
 * 事件式的触摸消息（`VML_MSG_TOUCHDOWN` 等）**只有槽位 0 会投**
 * Event-style touch messages (`VML_MSG_TOUCHDOWN` etc.) are **delivered to slot 0 only**
 * （`VmlHostRuntime.PostTouch`：所有槽位都更新查询状态，但只在 `slot == 0` 时投消息）。
 * (`VmlHostRuntime.PostTouch`: every slot updates the query state, but a message is posted only when `slot == 0`).
 * 所以光靠事件最多只能做**单指** —— 按不出和弦。
 * So events alone can give you **one finger** at most — you cannot play a chord.
 * 要十指同时按，就得每拍把 `ui_touch(slot)` 的 0–9 号槽位都查一遍。
 * To press ten fingers at once, you have to scan slots 0–9 of `ui_touch(slot)` on every tick.
 *
 * ⚠ 因此本程序**不用 `ui_timer_set`**：定时器消息会和触摸消息争同一个队列，
 * ⚠ That is why this program **does not use `ui_timer_set`**: timer messages would fight the touch messages for the same queue,
 *   而 `ui_wait(m, 16)` 的超时天然就是"至少每 16ms 醒一次"，一拍两用。
 *   whereas the timeout of `ui_wait(m, 16)` is naturally "wake up at least every 16ms" — one tick serving two purposes.
 *
 * ## 键盘布局：**自适应列数 + 两行**
 * ## Keyboard layout: **adaptive column count + two rows**
 *
 * 竖屏一块屏只放一个八度的话，每个键宽 ~51dp 而高 ~540dp —— 成了 1:10 的**竖长条**，
 * If a portrait screen held only one octave, each key would be ~51dp wide and ~540dp tall — a 1:10 **vertical strip**,
 * 一点都不像琴键（用户原话："画面是长条"）。所以：
 * nothing like a piano key at all (the user's own words: "the picture is a long strip"). So:
 *
 *   · **每行几个键按屏宽定**（每个键至少约 44dp，手指才按得准），且取 **7 的整数倍** ——
 *   · **How many keys per row depends on the screen width** (each key at least ~44dp so a finger can hit it), rounded to a **multiple of 7** —
 *     保证一行正好是若干个**完整八度**（落在 B 上收尾，不会切在 mi/si 中间而缺黑键）。
 *     that guarantees a row is exactly a whole number of **complete octaves** (ending on B, never cut between mi/si with a black key missing).
 *     竖屏 360dp ⇒ 7 个（一个八度）；平板 800dp ⇒ 14 个（两个八度）。
 *     Portrait 360dp ⇒ 7 keys (one octave); tablet 800dp ⇒ 14 keys (two octaves).
 *   · **两行** —— 上行比下行**高一个"每行八度数"**，音域直接翻倍，
 *   · **Two rows** — the upper row is **one "octaves per row" higher** than the lower one, which doubles the range directly,
 *     而且每行的高度只剩一半 ⇒ 键的宽高比回到像琴键的样子。
 *     and each row is only half as tall ⇒ the key aspect ratio goes back to looking like a piano key.
 *   · 每行高度再按"不超过宽度的 6 倍"收一道 —— 屏幕很高时别又把键拉成长条。
 *   · Each row's height is then capped at "no more than 6× its width" — on a very tall screen, don't stretch the keys into strips again.
 *
 * ## 引用计数：为什么不能"抬起就关音"
 * ## Reference counting: why you cannot "stop the note as soon as a finger lifts"
 *
 * 两根手指先后按同一个键、其中一根先抬起来时，**那个键不该停**
 * When two fingers press the same key one after the other and one of them lifts first, **that key must not stop**
 * （另一根还按着）。所以每个音记一个引用计数，**归零才真的关**。
 * (the other finger is still holding it). So every note keeps a reference count, and **only a count of zero really stops it**.
 * 表按**半音偏移**建（0..25，覆盖两行），一张就够 ——
 * The table is indexed by **semitone offset** (0..25, covering both rows) — one table is enough:
 * 分白键/黑键两张是"怎么画"的事，与"谁按着"无关。
 * splitting it into two tables for white/black keys is a "how to draw" matter, unrelated to "who is holding it".
 *
 * ## ⚠ C 前端的四条硬约束
 * ## ⚠ Four hard constraints of the C frontend
 *
 * 1. **局部数组必须单独一行声明**（`calc.c:414-422` 踩过：标量与数组同声明会让数组
 * 1. **A local array must be declared on a line of its own** (`calc.c:414-422` hit this: declaring a scalar and an array together leaves the array
 *    分不到槽位，连读取指令都不生成 ⇒ 变量恒为 0，很难往语法上想）。
 *    with no slot and not even a load instruction is generated ⇒ the variable is always 0, which is very hard to trace back to the syntax).
 * 2. **一行一个变量**，别写 `int x = m[1], y = m[2];`。
 * 2. **One variable per line**; don't write `int x = m[1], y = m[2];`.
 * 3. **全局数组的元素要先落局部变量再比较**（`calc.c:99-102` 记过）。
 * 3. **A global array's element must be loaded into a local variable before being compared** (recorded at `calc.c:99-102`).
 * 4. `#define` **不支持反斜杠续行** ⇒ 大表只能写成一行；本程序因此用
 * 4. `#define` **does not support backslash line continuation** ⇒ a big table can only be written on one line; this program therefore uses
 *    **记谱字符串**（`"Cq Cq Gq"`）而不是 int 数组来表达曲子。
 *    a **notation string** (`"Cq Cq Gq"`) rather than an int array to express the tune.
 */
#include <waycoder_ui.h>

/* ── 尺寸 ── */
/* ── Sizes ── */
static int SW;
static int SH;
static int BARH;      /* 顶部状态栏高 */
                      /* height of the top status bar */
static int KEYTOP;    /* 键盘上沿（第一行的顶） */
                      /* keyboard top edge (the top of the first row) */
static int RowKeyH;   /* **每一行**白键的高（不再是整块屏幕的高度） */
                      /* white-key height **per row** (no longer the whole screen's height) */
static int RowGap;    /* 两行之间的缝 —— 手指按错行时能看出来 */
                      /* gap between the two rows — visible when a finger lands on the wrong row */

/* ── 键盘布局 ── */
/* ── Keyboard layout ── */
static int PerRow;    /* 每行几个白键（7 的整数倍：7 或 14） */
                      /* white keys per row (a multiple of 7: 7 or 14) */
static int RowCount;  /* 行数（2） */
                      /* number of rows (2) */
static int RowStep;   /* 上下两行差几个半音（= 12 × 每行的八度数） */
                      /* semitones between the two rows (= 12 × octaves per row) */

/* ── 音域 ──
 * ── Range ──
 * `BaseNote` 是**第一行**的 do；第二行是 `BaseNote + RowStep`。
 * `BaseNote` is the do of the **first row**; the second row is `BaseNote + RowStep`.
 * 状态栏那两个「−/+」整体移调（两行一起走），比把键盘挤成一条窄键好得多。
 * The two "−/+" buttons in the status bar transpose everything (both rows move together), far better than squeezing the keyboard into one narrow row.
 */
static int BaseNote;

/* 音名（标在白键底部用）。
 * Note names (drawn at the bottom of the white keys).
 * ⚠ 用**两个一维 char 数组**而不是 `char* NAMES[12]` 或二维数组 ——
 * ⚠ Use **two one-dimensional char arrays** rather than `char* NAMES[12]` or a 2-D array —
 *   本仓记过"全局**指针**数组在这条前端上会读出垃圾值"，能不碰就不碰。
 *   this repo has recorded that "a global **pointer** array reads garbage on this frontend"; avoid it if you can.
 *   12 个半音里只有 5 个带升号（C# D# F# G# A#），另一个数组存 '#' 或 0。
 *   only 5 of the 12 semitones carry a sharp (C# D# F# G# A#); the other array holds '#' or 0.
 */
static char NAME0[12];
static char NAME1[12];

/* 白键半音偏移：do re mi fa sol la si */
/* white-key semitone offsets: do re mi fa sol la si */
static int WHITE_SEMI[7];
/* 第 i 个白键右上方有没有黑键（-1 = 没有：mi 和 si 右上方没有） */
/* is there a black key above-right of the i-th white key (-1 = no: mi and si have none) */
static int BLACK_AT[7];
/* 那个黑键的半音偏移 */
/* semitone offset of that black key */
static int BLACK_SEMI[7];

/* ── 手指状态 ── */
/* ── Finger state ── */
static int KeyDown[10];    /* 这个槽位按着哪个**音符号**；-1 = 没按 */
                           /* which **note number** this slot is holding; -1 = not pressed */
static int PrevDown[10];   /* 上一拍这个槽位按着没有 —— 用来取"按下沿"（见按钮那段） */
                           /* whether this slot was pressed on the previous tick — used to take the "pressed edge" (see the button section) */
static int Ref[26];        /* 每个半音偏移（相对 BaseNote）被几根手指按着 */
                           /* how many fingers are holding each semitone offset (relative to BaseNote) */

/* ── 示范曲 ── */
/* ── Demo song ── */
static char* SONG;
static int SongPos;        /* 解析到第几个字符 */
                           /* how far the parse has got (character index) */
static int SongOn;
static int SongTick;       /* 这一首已经走了几拍 */
                           /* how many ticks this song has already run */
static int SongNote;       /* 正在响的示范音（音符号）；-1 = 没响 */
                           /* the demo note currently sounding (note number); -1 = silent */
static int SongOffAt;      /* 到这一拍就该关掉 */
                           /* at this tick it should be switched off */
static int SongNextAt;     /* 到这一拍取下一个音 */
                           /* at this tick take the next note */

/* ── 录音 ── */
/* ── Recording ── */
static int RecOn;
static int RecN;
static int RecTick[384];   /* 事件时刻（拍） */
                           /* event time (ticks) */
static int RecNote[384];   /* 音符号；-1 表示"关音" */
                           /* note number; -1 means "note off" */

/* ── 状态栏按钮（布局时算好，**画与命中同源**）── */
/* ── Status-bar buttons (computed at layout time; **drawing and hit-testing share one source**) ── */
/* 0=移调- 1=移调+ 2=示范曲 3=录音 4=**强制清除** */
/* 0=transpose- 1=transpose+ 2=demo song 3=record 4=**force clear** */
static int BtnX[5];
static int Lang;      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                      /* UI language: queried once at startup (ui_get_language is a syscall, don't call it every frame) */
static int BtnY;
static int BtnW;
static int BtnH;

/* ══════════════════════════════════════════════════════════════════════
 * 发声：手弹与示范曲**共用**这两个出口
 * Sound: the hand-played notes and the demo song **share** these two exits
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
 * Key geometry — **drawing and hit-testing share one source** (the rectangle comes out of the same function)
 * ══════════════════════════════════════════════════════════════════════ */

static int white_w(void)
{
    return SW / PerRow;
}

/* 第 `row` 行第 `i` 个白键的**上沿** y。 */
/* the **top edge** y of the i-th white key in row `row`. */
static int row_top(int row)
{
    return KEYTOP + row * (RowKeyH + RowGap);
}

/* ══════════════════════════════════════════════════════════════════════
 * 「第几行第几个键 → 哪个半音偏移」——**画与命中共用的唯一真源**
 * "which row, which key → which semitone offset" — **the single source shared by drawing and hit-testing**
 *
 * ⚠ 这两个函数是踩出来的：最先绘制时只算了**行内**偏移、忘了加 `row * RowStep`，
 * ⚠ These two functions were born from a bug: at first the drawing side computed only the **in-row** offset and forgot to add `row * RowStep`,
 *   而命中那条**算对了**。于是表现成一对其怪的组合：
 *   while the hit-testing side **got it right**. The result was a bizarre pair of symptoms:
 *     · 按**上行** ⇒ 两行查的是同一个 `Ref` 键 ⇒ **下面那行一起亮**；
 *     · Pressing the **upper row** ⇒ both rows looked up the same `Ref` entry ⇒ **the lower row lit up too**;
 *     · 按**下行** ⇒ 查的键根本没人写过 ⇒ **声音是有的、却不高亮**。
 *     · Pressing the **lower row** ⇒ nobody had ever written that entry ⇒ **the sound played, but no highlight**.
 *   两处各算一遍就是两个真源，迟早对不上 —— 收成一个函数之后不可能再错开。
 *   Computing it in two places is two sources of truth, and sooner or later they disagree — once folded into one function they cannot drift apart again.
 * ══════════════════════════════════════════════════════════════════════ */

/* 第 `row` 行**多高**（相对 BaseNote 的半音数）。
 * **How high** row `row` is (semitones relative to BaseNote).
 *
 * ⚠ **下面那行是低音**（`row` 越大越低）—— 音高往上走、屏幕上也往上走，
 * ⚠ **The lower row is the bass** (larger `row` = lower) — pitch goes up and so does the screen position,
 *   与钢琴/电子琴的直觉一致。第一版写反了（下面反而高一个八度），
 *   matching the intuition of a piano or electric keyboard. The first version had it backwards (the lower row was an octave higher),
 *   标上音名才一眼看出来（下面是 C5、上面是 C4）。
 *   and it only became obvious once the note names were drawn on (C5 below, C4 above).
 *   这类"反了也能跑"的错误，靠**把语义画出来**（音名）比靠断言容易发现得多。
 *   For this kind of "backwards but still runs" mistake, **drawing the semantics** (the note names) reveals it far more easily than an assertion. */
static int row_shift(int row)
{
    return (RowCount - 1 - row) * RowStep;
}

/* 第 `row` 行第 `i` 个白键的半音偏移（相对 BaseNote）。 */
/* the semitone offset of the i-th white key in row `row` (relative to BaseNote). */
static int white_semi_at(int row, int i)
{
    return row_shift(row) + WHITE_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
}

/* 第 `row` 行第 `i` 个白键右上方黑键的半音偏移（调用方先判 `BLACK_AT` 不是 -1）。 */
/* the semitone offset of the black key above-right of the i-th white key in row `row` (the caller first checks that `BLACK_AT` is not -1). */
static int black_semi_at(int row, int i)
{
    return row_shift(row) + BLACK_SEMI[i - (i / 7) * 7] + (i / 7) * 12;
}

/* 第 `row` 行第 `i` 个白键右上方黑键的矩形（out[0..3] = x y w h）。 */
/* the rectangle of the black key above-right of the i-th white key in row `row` (out[0..3] = x y w h). */
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
 * Drawing
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
    int rel;
    char nm[4];

    ui_clear(0xFF101820);

    /* ── 状态栏 ── */
    /* ── Status bar ── */
    ui_set_font(18, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
    ui_set_valign(VML_VANCHOR_MIDDLE);
    ui_text_cur(10, BtnY + BtnH / 2, Lang == 0 ? "钢琴" : "Piano");

    /* ⚠ 这里原先还有一行"音域提示"（`C4-5` 之类），居中画的 ——
     * ⚠ There used to be a "range hint" line here (`C4-5` and the like), drawn centered —
     *   而 5 个按钮从 x≈80 起把右边占满了，那行字**大半被盖住、只露出一个 C**，
     *   but the 5 buttons fill the right side from x≈80 on, so that text was **mostly covered, with only one C sticking out**,
     *   看着像凭空多出来的字符（用户报的就是这个）。
     *   looking like a stray character out of nowhere (this is exactly what the user reported).
     *   既然音名已经标在每个白键上，那个提示本来就是重复的 —— 直接去掉。
     *   Since the note names are already drawn on every white key, that hint was redundant anyway — so it was removed.
     */

    for (i = 0; i < 5; i++) {
        /* 第 5 个（清除）用暖红底 —— 它是"急停"，混在其余按钮里不好找 */
        /* the 5th button (clear) gets a warm red background — it is the "emergency stop", and it is hard to find among the other buttons */
        ui_rect(BtnX[i], BtnY, BtnW, BtnH, i == 4 ? 0xFF6A2A2A : 0xFF2A3A4A, 1, 0, 8);
        ui_set_font(15, VML_FONT_BOLD, 0xFFDDEEFF, VML_ANCHOR_CENTER);
        ui_set_valign(VML_VANCHOR_MIDDLE);
        if (i == 0) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "-");
        if (i == 1) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "+");
        /* 英文按"这个按钮此刻的动作"给词（Demo/Stop、Rec/Stop）—— 中文那对 停/停录
           The English wording follows "what this button does right now" (Demo/Stop, Rec/Stop) — the Chinese pair "stop"/"stop recording"
           在窄按钮里放不下（StopRec 会溢出 40~52px 的键宽），而按钮各自的位置已经把
           would not fit in a narrow button (StopRec overflows the 40~52px key width), and each button's position already makes
           "停的是哪一个"说清楚了。
           clear which one is being stopped.
           */
        if (i == 2) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2,
                                SongOn ? (Lang == 0 ? "停" : "Stop") : (Lang == 0 ? "示范" : "Demo"));
        if (i == 3) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2,
                                RecOn ? (Lang == 0 ? "停录" : "Stop") : (Lang == 0 ? "录音" : "Rec"));
        if (i == 4) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, Lang == 0 ? "清音" : "Clear");
    }

    /* ── 两行白键 ── */
    /* ── Two rows of white keys ── */
    w = white_w();
    for (row = 0; row < RowCount; row++) {
        y0 = row_top(row);
        for (i = 0; i < PerRow; i++) {
            /* ⚠ 这里的 `semi` **必须**用 `white_semi_at(row, i)`（含行偏移）——
             * ⚠ The `semi` here **must** come from `white_semi_at(row, i)` (including the row shift) —
             *   先前写成"只有行内偏移"，于是两行共用同一个 `Ref` 键：
             *   it used to carry the in-row offset only, so both rows shared the same `Ref` entry:
             *   按上行两行一起亮、按下行反而不亮（声音却有）。见那个函数的注释。
             *   pressing the upper row lit both rows, pressing the lower row lit nothing (though it did sound). See that function's comment.
             */
            semi = white_semi_at(row, i);
            x = i * w;
            ui_rect(x + 1, y0 + 3, w - 2, RowKeyH, 0xFF000000, 1, 0, 6);   /* 底影 */
                                                                           /* bottom shadow */
            if (Ref[semi] > 0) {
                ui_rect(x + 1, y0 + 2, w - 2, RowKeyH - 2, 0xFFFFD98A, 1, 0, 6);  /* 按下：暖色 */
                                                                                  /* pressed: warm color */
            } else {
                ui_rect(x + 1, y0, w - 2, RowKeyH, 0xFFF2F2F4, 1, 0, 6);
            }
            ui_rect(x + 1, y0, w - 2, RowKeyH, 0xFF8A8A92, 0, 2, 6);        /* 描边 */
                                                                           /* outline */

            /* 音名标在白键**底部**（黑键太窄，标了也看不清）。
             * The note name goes at the **bottom** of the white key (the black keys are too narrow to read a label on).
             * 八度用"相对中央 C 的八度号"—— `BaseNote` 就是第一行的 C。
             * The octave uses "the octave number relative to middle C" — `BaseNote` is the C of the first row.
             */
            /* ⚠ 坑 3（全局数组元素先落局部变量）：`NAME0[...]` 的下标也先算出来，
             * ⚠ Trap 3 (load a global array's element into a local first): the index into `NAME0[...]` is computed up front as well,
             *   别在两次取用里各算一遍。
             *   don't compute it once for each of the two uses.
             */
            rel = semi - semi / 12 * 12;                    /* 这个音在八度内的位置 */
                                                            /* this note's position within the octave */
            oct = (BaseNote + semi - 60) / 12 + 4;          /* C4 = 中央 C */
                                                            /* C4 = middle C */
            if (oct < 0) oct = 0;
            if (oct > 9) oct = 9;                           /* 只画一位，画不下就夹住 */
                                                            /* only one digit is drawn; clamp it if it won't fit */
            if (NAME1[rel] == 0) {
                nm[0] = NAME0[rel];                         /* 白键：C4 / D4 … */
                                                            /* white key: C4 / D4 … */
                nm[1] = '0' + oct;
                nm[2] = 0;
            } else {
                nm[0] = NAME0[rel];                         /* 黑键名用不上，但留着不会错 */
                                                            /* the black-key name is unused here, but keeping it does no harm */
                nm[1] = '#';
                nm[2] = '0' + oct;
                nm[3] = 0;
            }
            ui_set_font(13, 0, 0xFF8A8A92, VML_ANCHOR_CENTER);
            ui_set_valign(VML_VANCHOR_BOTTOM);
            ui_text_cur(x + w / 2, y0 + RowKeyH - 6, nm);
        }
    }

    /* ── 黑键（画在白键之上；每行各自铺一遍）── */
    /* ── Black keys (drawn on top of the white keys; laid down once for each row) ── */
    for (row = 0; row < RowCount; row++) {
        for (i = 0; i < PerRow; i++) {
            if (BLACK_AT[i - (i / 7) * 7] < 0) continue;
            semi = black_semi_at(row, i);
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
 * Hit-testing
 * ══════════════════════════════════════════════════════════════════════ */

/* 打中哪个键？返回**半音偏移**（相对 BaseNote，0..RowStep+12），-1 = 没打中。
 * Which key was hit? Returns the **semitone offset** (relative to BaseNote, 0..RowStep+12); -1 = nothing hit.
 * ⚠ **黑键优先** —— 它压在白键上方，先判白键会让黑键永远按不到。
 * ⚠ **Black keys first** — they sit on top of the white keys, so testing the white keys first would make the black keys unreachable forever.
 */
static int hit_key(int x, int y)
{
    int row;
    int i;
    int r[4];
    int w;

    if (y < KEYTOP) return -1;

    /* 落在哪一行？—— 按"行 + 缝"的周期算，再夹到合法行 */
    /* Which row did it land in? — compute it by the "row + gap" period, then clamp to a legal row */
    row = (y - KEYTOP) / (RowKeyH + RowGap);
    if (row < 0) row = 0;
    if (row >= RowCount) row = RowCount - 1;
    if (y >= row_top(row) + RowKeyH) return -1;   /* 正好落在两行之间那道缝里 */
                                                  /* it landed exactly in the gap between the two rows */

    /* 黑键优先 */
    /* Black keys first */
    for (i = 0; i < PerRow; i++) {
        if (BLACK_AT[i - (i / 7) * 7] < 0) continue;
        black_rect(row, i, r);
        if (x >= r[0] && x < r[0] + r[2] && y < r[1] + r[3]) return black_semi_at(row, i);
    }

    w = SW / PerRow;
    for (i = 0; i < PerRow; i++) {
        if (x >= i * w && x < (i + 1) * w) return white_semi_at(row, i);
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
 * Demo song: a **notation string** (note names + duration letters, `.` = rest)
 *
 * 用字符串而不是 int 表，是因为 `#define` 不支持续行 ⇒ 一张几百项的 int 表
 * A string is used instead of an int table because `#define` does not support line continuation ⇒ a several-hundred-entry int table
 * 只能塞在一行里，写起来和改起来都是灾难；字符串天然可以按乐句分行。
 * could only be squeezed onto a single line, a disaster to write and to edit; a string can naturally be split by phrase.
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
/* Take the next note. Returns 0 = the song is finished. `note` set to -1 means a rest. */
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
                                    /* the demo song stays in the middle octave, independent of the current transposition */
        *hold = 380;
        if (SONG[SongPos] == 'h') { *hold = 760; SongPos = SongPos + 1; }
        else if (SONG[SongPos] == 'q') { SongPos = SongPos + 1; }
        return 1;
    }
    return 0;
}

/* ══════════════════════════════════════════════════════════════════════
 * 主循环
 * Main loop
 * ══════════════════════════════════════════════════════════════════════ */

/* 按钮的动作。
 * The button actions.
 * ⚠ **判定**（在轮询里取"按下沿"）与**动作**分开：判定只能有一处，动作也只写一遍。
 * ⚠ **Detection** (taking the "pressed edge" in the polling loop) is kept apart from **action**: detection has exactly one place, and the action is written only once.
 */
static void apply_btn(int btn)
{
    int i;

    if (btn == 0 || btn == 1) {
        /* 移调前先把响着的都关掉 —— 否则那些音的"相对偏移"会跟着基准一起漂，
         * Shut down everything that is sounding before transposing — otherwise those notes' "relative offsets" drift along with the base,
         * 引用计数就成了错的（关音时会去减另一个键的账）。
         * and the reference counts become wrong (note-off would subtract from another key's account).
         */
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
         * **Force clear** (the "emergency stop" the user asked for): cut off every sounding note immediately.
         *
         * ⚠ 三件事必须**一起**做，少一件这个键就是假的：
         * ⚠ Three things must be done **together**; leave one out and this key is a fake:
         *   ① `ui_tone_panic()` —— 立刻全停（**不是** `ui_tone_all_off()` 的淡出：
         *   ① `ui_tone_panic()` — stop everything at once (**not** the fade-out of `ui_tone_all_off()`:
         *      这个键的语义就是"马上安静"，卡住的音再拖 90ms 没有意义）；
         *      this key's whole point is "silence right now", and dragging a stuck note on for another 90ms means nothing);
         *   ② 清**引用计数** —— 不清的话，抬起手指时会去减一个早就清空的账，
         *   ② Clear the **reference counts** — otherwise, lifting a finger subtracts from an account that was emptied long ago,
         *      越减越负，之后那个键就再也起不来音了；
         *      going further and further negative, and after that the key can never sound again;
         *   ③ 清槽位的记账 —— 让"这一拍手指还按着"被当成**新按下**（重新起音）。
         *   ③ Clear the slot bookkeeping — so that "the finger is still holding on this tick" counts as a **fresh press** (retriggering the note).
         *      于是：还按着的手指会继续弹（那是它本来就该做的），
         *      The result: fingers still held keep playing (which is what they should do anyway),
         *      而**已经卡住/松开了却没收到 KeyUp 的槽位**从此不再发声。
         *      while **slots that are stuck, or were released without a KeyUp** stop sounding from then on.
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

    /* 音名表（标在白键底部）—— 12 个半音，只有 5 个带升号 */
    /* Note-name table (drawn at the bottom of the white keys) — 12 semitones, only 5 of which carry a sharp */
    NAME0[0] = 'C'; NAME0[1] = 'C'; NAME0[2] = 'D'; NAME0[3] = 'D';
    NAME0[4] = 'E'; NAME0[5] = 'F'; NAME0[6] = 'F'; NAME0[7] = 'G';
    NAME0[8] = 'G'; NAME0[9] = 'A'; NAME0[10] = 'A'; NAME0[11] = 'B';
    NAME1[0] = 0; NAME1[1] = '#'; NAME1[2] = 0; NAME1[3] = '#';
    NAME1[4] = 0; NAME1[5] = 0; NAME1[6] = '#'; NAME1[7] = 0;
    NAME1[8] = '#'; NAME1[9] = 0; NAME1[10] = '#'; NAME1[11] = 0;

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
     * ── How many white keys go in a row: adapted to the screen width ──
     * 每键至少约 44dp 手指才按得准；取 **7 的整数倍** 保证一行是完整八度。
     * Each key at least ~44dp so a finger can hit it accurately; round to a **multiple of 7** so a row is complete octaves.
     */
    PerRow = SW / 44;
    PerRow = (PerRow / 7) * 7;
    if (PerRow < 7) PerRow = 7;
    if (PerRow > 14) PerRow = 14;

    /* ── 两行：音域翻倍，且每行只剩一半高 ⇒ 键不再是个竖长条 ── */
    /* ── Two rows: the range doubles, and each row is only half as tall ⇒ the keys are no longer vertical strips ── */
    RowCount = 2;
    perRowOct = PerRow / 7;             /* 每行有几个八度 */
                                        /* how many octaves per row */
    RowStep = 12 * perRowOct;           /* 上行比下行高这么多半音 */
                                        /* the upper row is this many semitones above the lower one */

    BARH = 54;
    KEYTOP = BARH + 10;
    RowGap = 6;
    RowKeyH = (SH - KEYTOP - 20 - RowGap * (RowCount - 1)) / RowCount;

    /* ⚠ 再按宽高比收一道：屏幕很高时（平板竖放）别又把键拉成长条。
     * ⚠ One more cap, on the aspect ratio: on a very tall screen (a tablet held upright) don't stretch the keys into strips again.
     *   真实琴键的宽高比大约 1:5~1:6，这里取 6 作上限。
     *   A real piano key's aspect ratio is about 1:5~1:6; 6 is taken as the cap here.
     */
    if (RowKeyH > (SW / PerRow) * 6) RowKeyH = (SW / PerRow) * 6;
    if (RowKeyH < 60) RowKeyH = 60;     /* 太矮就按不准了 */
                                        /* too short and you cannot hit it accurately */

    /* 5 个按钮：`- + 示范 录音 清音`。窄屏上按 4 个的宽度放不下，所以键宽按屏宽算。 */
    /* 5 buttons: `- + demo record clear`. Four buttons' worth of width does not fit on a narrow screen, so the key width follows the screen width. */
    BtnH = 32;
    BtnY = (BARH - BtnH) / 2;
    BtnW = (SW - 60) / 5;               /* 左边留出放「钢琴 + 音域」的地方 */
                                        /* leave room on the left for the "Piano + range" text */
    if (BtnW > 52) BtnW = 52;           /* 宽屏上别拉太宽 */
                                        /* don't stretch it too wide on a wide screen */
    if (BtnW < 40) BtnW = 40;           /* 太窄字就挤没了 */
                                        /* too narrow and the text gets squeezed away */
    BtnX[0] = SW - 5 * BtnW - 20;
    BtnX[1] = BtnX[0] + BtnW + 4;
    BtnX[2] = BtnX[1] + BtnW + 6;
    BtnX[3] = BtnX[2] + BtnW + 4;
    BtnX[4] = BtnX[3] + BtnW + 6;

    Lang = ui_get_language();
    ui_win_open_ex(Lang == 0 ? "钢琴" : "Piano", SW, SH, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    draw();

    while (ui_win_closed() == 0) {
        msgType = ui_wait(m, 16);
        if (msgType == VML_MSG_WINDOWCLOSE) break;

        dirty = 0;

        /* ── 琴键与按钮：**都在轮询里判** ────────────────────────────── */
        /* ── Keys and buttons: **both are decided in the polling loop** ────────────────────────────── */
        for (slot = 0; slot < 10; slot++) {
            if (ui_touch(slot, t) == 0) continue;

            /* 按钮：取**按下沿**（这一拍按下、上一拍没按）。
             * Buttons: take the **pressed edge** (pressed this tick, not pressed on the previous one).
             *
             * ⚠ 先前只在 slot 0 的**事件消息**里判按钮，于是
             * ⚠ Buttons used to be tested only in slot 0's **event message**, so
             *   "一根手指按着琴键、另一根手指去点清音"——**最需要急停的那个场合**——
             *   "one finger holding a key while another finger taps clear" — **the exact situation that most needs an emergency stop** —
             *   反而点不到：事件消息只有 slot 0 会投，而 slot 0 正按着琴键。
             *   could not be tapped at all: only slot 0 receives event messages, and slot 0 was holding a key.
             *   统一到轮询之后就都点得到了（16ms 一拍，人手点击至少 50ms，不会漏）。
             *   Once unified into the polling loop they are all reachable (16ms per tick, and a human tap lasts at least 50ms, so none are missed).
             */
            if (t[2] != 0 && PrevDown[slot] == 0) {
                btn = hit_btn(t[0], t[1]);
                if (btn >= 0) {
                    apply_btn(btn);
                    dirty = 1;
                    PrevDown[slot] = 1;
                    continue;               /* 这一拍就不当琴键处理了 */
                                            /* this tick is no longer treated as a key press */
                }
            }
            PrevDown[slot] = t[2] != 0;

            if (t[2] != 0) {
                semi = hit_key(t[0], t[1]);
            } else {
                semi = -1;                                      /* 抬起了 */
                                                                /* lifted */
            }

            note = semi < 0 ? -1 : BaseNote + semi;
            if (note != KeyDown[slot]) {
                /* 这个槽位换了音（含抬起）：先把上一个音还回去 */
                /* This slot changed note (including lifting): give the previous note back first */
                if (KeyDown[slot] >= 0) {
                    hit = KeyDown[slot] - BaseNote;
                    if (hit >= 0 && hit < 26) {
                        if (Ref[hit] > 0) Ref[hit] = Ref[hit] - 1;
                        /* ⚠ **归零才真的关** —— 两根手指按同一个键时，
                         * ⚠ **Only reaching zero really stops it** — when two fingers hold the same key,
                         *   先抬起的那根不该把还按着的那根的音关掉
                         *   the finger that lifts first must not stop the note the other one is still holding
                         */
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
        /* ── Demo-song advance (single voice; note-on and note-off each keep their own beat) ──────────── */
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
                                                              /* leave a 1/4 gap so the notes come out separated */
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