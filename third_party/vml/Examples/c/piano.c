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
 * ## 两套输入的边界
 *
 * - **琴键**：轮询（多指）
 * - **按钮**（八度 −/+、示范曲、录音）：事件式（单指点击）
 *
 * 两者会打架（按琴键也会发 slot 0 的 TOUCHDOWN），所以**先判按钮**：
 * 落在按钮矩形里就当点击、否则才当琴键。
 *
 * ## 引用计数：为什么不能"抬起就关音"
 *
 * 两根手指先后按同一个键、其中一根先抬起来时，**那个键不该停**
 * （另一根还按着）。所以每个音记一个引用计数，**归零才真的关**。
 * 表按**半音偏移**建（0..12，含高八度的 do），一张就够 ——
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
static int KEYTOP;    /* 键盘上沿 */
static int KEYH;      /* 白键高 */

/* ── 音域：一个八度 + 整体移调 ──
 * 竖屏下 7 个白键、每键约 SW/7 —— 手指按得准。
 * 想要别的音域按状态栏那两个「−/+」，比把键盘挤成一条窄键好得多。 */
static int BaseNote;  /* 当前这排的 do 的音符号 */

/* 白键半音偏移：do re mi fa sol la si */
static int WHITE_SEMI[7];
/* 第 i 个白键右上方有没有黑键（-1 = 没有：mi 和 si 右上方没有） */
static int BLACK_AT[7];
/* 那个黑键的半音偏移 */
static int BLACK_SEMI[7];

/* ── 手指状态 ── */
static int KeyDown[10];    /* 这个槽位按着哪个**音符号**；-1 = 没按 */
static int Ref[13];        /* 每个半音偏移被几根手指按着（0..12） */

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
static int BtnX[4];
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
 * 键位几何
 * ══════════════════════════════════════════════════════════════════════ */

static int white_w(void)
{
    return SW / 7;
}

/* 第 i 个白键右上方黑键的矩形（out[0..3] = x y w h）。 */
static void black_rect(int i, int* out)
{
    int w;
    int bw;
    w = SW / 7;
    bw = w * 62 / 100;
    out[0] = (i + 1) * w - bw / 2;
    out[1] = KEYTOP;
    out[2] = bw;
    out[3] = KEYH * 62 / 100;
}

/* ══════════════════════════════════════════════════════════════════════
 * 绘制
 * ══════════════════════════════════════════════════════════════════════ */

static void draw(void)
{
    int i;
    int r[4];
    int x;
    int w;
    int semi;
    int oct;
    char buf[8];

    ui_clear(0xFF101820);

    /* ── 状态栏 ── */
    ui_set_font(18, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
    ui_set_valign(VML_VANCHOR_MIDDLE);
    ui_text_cur(10, BtnY + BtnH / 2, "钢琴");

    /* 当前音域（C2..C7 这样标，比写数字直观） */
    oct = (BaseNote - 60) / 12 + 4;
    buf[0] = 'C';
    buf[1] = '0' + oct;
    buf[2] = 0;
    ui_set_font(20, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
    ui_text_cur(SW / 2 + 30, BtnY + BtnH / 2, buf);

    for (i = 0; i < 4; i++) {
        ui_rect(BtnX[i], BtnY, BtnW, BtnH, 0xFF2A3A4A, 1, 0, 8);
        ui_set_font(15, VML_FONT_BOLD, 0xFFDDEEFF, VML_ANCHOR_CENTER);
        ui_set_valign(VML_VANCHOR_MIDDLE);
        if (i == 0) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "-");
        if (i == 1) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, "+");
        if (i == 2) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, SongOn ? "停" : "示范");
        if (i == 3) ui_text_cur(BtnX[i] + BtnW / 2, BtnY + BtnH / 2, RecOn ? "停录" : "录音");
    }

    /* ── 白键 ── */
    w = white_w();
    for (i = 0; i < 7; i++) {
        semi = WHITE_SEMI[i];
        x = i * w;
        ui_rect(x + 1, KEYTOP + 3, w - 2, KEYH, 0xFF000000, 1, 0, 6);   /* 底影 */
        if (Ref[semi] > 0) {
            ui_rect(x + 1, KEYTOP + 2, w - 2, KEYH - 2, 0xFFFFD98A, 1, 0, 6);   /* 按下的暖色 */
        } else {
            ui_rect(x + 1, KEYTOP, w - 2, KEYH, 0xFFF2F2F4, 1, 0, 6);
        }
        ui_rect(x + 1, KEYTOP, w - 2, KEYH, 0xFF8A8A92, 0, 2, 6);       /* 描边 */
    }

    /* ── 黑键（画在白键之上）── */
    for (i = 0; i < 7; i++) {
        if (BLACK_AT[i] < 0) continue;
        semi = BLACK_SEMI[i];
        black_rect(i, r);
        ui_rect(r[0], r[1] + 2, r[2], r[3], 0xFF000000, 1, 0, 5);
        if (Ref[semi] > 0) {
            ui_rect(r[0], r[1], r[2], r[3] - 2, 0xFF6A5A3A, 1, 0, 5);
        } else {
            ui_rect(r[0], r[1], r[2], r[3], 0xFF1A1A20, 1, 0, 5);
        }
    }

    ui_present();
}

/* ══════════════════════════════════════════════════════════════════════
 * 命中
 * ══════════════════════════════════════════════════════════════════════ */

/* 打中哪个键？返回**半音偏移**（0..12），-1 = 没打中。
 * ⚠ **黑键优先** —— 它压在白键上方，先判白键会让黑键永远按不到。 */
static int hit_key(int x, int y)
{
    int i;
    int r[4];
    int w;

    if (y < KEYTOP) return -1;

    for (i = 0; i < 7; i++) {
        if (BLACK_AT[i] < 0) continue;
        black_rect(i, r);
        if (x >= r[0] && x < r[0] + r[2] && y < r[1] + r[3]) return BLACK_SEMI[i];
    }

    w = SW / 7;
    for (i = 0; i < 7; i++) {
        if (x >= i * w && x < (i + 1) * w) return WHITE_SEMI[i];
    }
    return -1;
}

static int hit_btn(int x, int y)
{
    int i;
    if (y < BtnY || y >= BtnY + BtnH) return -1;
    for (i = 0; i < 4; i++) {
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

    WHITE_SEMI[0] = 0; WHITE_SEMI[1] = 2; WHITE_SEMI[2] = 4; WHITE_SEMI[3] = 5;
    WHITE_SEMI[4] = 7; WHITE_SEMI[5] = 9; WHITE_SEMI[6] = 11;
    BLACK_AT[0] = 0; BLACK_AT[1] = 1; BLACK_AT[2] = -1; BLACK_AT[3] = 3;
    BLACK_AT[4] = 4; BLACK_AT[5] = 5; BLACK_AT[6] = -1;
    BLACK_SEMI[0] = 1; BLACK_SEMI[1] = 3; BLACK_SEMI[2] = -1; BLACK_SEMI[3] = 6;
    BLACK_SEMI[4] = 8; BLACK_SEMI[5] = 10; BLACK_SEMI[6] = -1;

    for (i = 0; i < 10; i++) KeyDown[i] = -1;
    for (i = 0; i < 13; i++) Ref[i] = 0;

    BaseNote = 60;
    RecOn = 0; RecN = 0;
    SongOn = 0; SongPos = 0; SongTick = 0; SongNote = -1; SongOffAt = 0; SongNextAt = 0;
    SONG = "Cq Cq Gq Gq Aq Aq Gh Fq Fq Eq Eq Dq Dq Ch Gq Gq Fq Fq Eq Eq Dh Gq Gq Fq Fq Eq Eq Dh Cq Cq Gq Gq Aq Aq Gh Fq Fq Eq Eq Dq Dq Ch";

    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;

    BARH = 54;
    KEYTOP = BARH + 10;
    KEYH = SH - KEYTOP - 20;

    BtnH = 32;
    BtnY = (BARH - BtnH) / 2;
    BtnW = 56;
    BtnX[0] = SW - 4 * BtnW - 20;
    BtnX[1] = BtnX[0] + BtnW + 4;
    BtnX[2] = BtnX[1] + BtnW + 6;
    BtnX[3] = BtnX[2] + BtnW + 4;

    ui_win_open_ex("钢琴", SW, SH, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);
    draw();

    while (ui_win_closed() == 0) {
        msgType = ui_wait(m, 16);
        if (msgType == VML_MSG_WINDOWCLOSE) break;

        dirty = 0;

        /* ── 按钮：事件式（单指点击）────────────────────────────────── */
        if (msgType == VML_MSG_TOUCHDOWN || msgType == VML_MSG_MOUSEDOWN) {
            btn = hit_btn(m[1], m[2]);
            if (btn == 0) {
                /* 移调前先把响着的都关掉 —— 否则那些音的"音符号"会跟着基准一起漂 */
                for (i = 0; i < 10; i++) {
                    if (KeyDown[i] >= 0) { p_off(i, KeyDown[i]); KeyDown[i] = -1; }
                }
                for (i = 0; i < 13; i++) Ref[i] = 0;
                BaseNote = BaseNote - 12;
                if (BaseNote < 36) BaseNote = 36;
                dirty = 1;
            } else if (btn == 1) {
                for (i = 0; i < 10; i++) {
                    if (KeyDown[i] >= 0) { p_off(i, KeyDown[i]); KeyDown[i] = -1; }
                }
                for (i = 0; i < 13; i++) Ref[i] = 0;
                BaseNote = BaseNote + 12;
                if (BaseNote > 84) BaseNote = 84;
                dirty = 1;
            } else if (btn == 2) {
                if (SongOn) {
                    SongOn = 0;
                    if (SongNote >= 0) { p_off(0, SongNote); SongNote = -1; }
                } else {
                    SongOn = 1; SongPos = 0; SongTick = 0;
                    SongNote = -1; SongOffAt = 0; SongNextAt = 0;
                }
                dirty = 1;
            } else if (btn == 3) {
                RecOn = 1 - RecOn;
                if (RecOn) RecN = 0;
                dirty = 1;
            }
        }

        /* ── 琴键：**轮询**（只有轮询才看得到多指）──────────────────── */
        for (slot = 0; slot < 10; slot++) {
            if (ui_touch(slot, t) == 0) continue;

            if (t[2] != 0) {
                if (hit_btn(t[0], t[1]) >= 0) { semi = -1; }   /* 按钮区：在那儿处理过了 */
                else { semi = hit_key(t[0], t[1]); }
            } else {
                semi = -1;                                      /* 抬起了 */
            }

            note = semi < 0 ? -1 : BaseNote + semi;
            if (note != KeyDown[slot]) {
                /* 这个槽位换了音（含抬起）：先把上一个音还回去 */
                if (KeyDown[slot] >= 0) {
                    hit = KeyDown[slot] - BaseNote;
                    if (hit >= 0 && hit < 13) {
                        if (Ref[hit] > 0) Ref[hit] = Ref[hit] - 1;
                        /* ⚠ **归零才真的关** —— 两根手指按同一个键时，
                         *   先抬起的那根不该把还按着的那根的音关掉 */
                        if (Ref[hit] == 0) p_off(slot, KeyDown[slot]);
                    }
                }
                KeyDown[slot] = note;
                if (note >= 0 && semi >= 0 && semi < 13) {
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
