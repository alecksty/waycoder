/* audio_test.c —— **复音音频测试**：音阶 / 和弦 / 旋律 / 与老式蜂鸣共存。
 *
 * ## 它验的是四件事
 *
 *   ① **音准**：C 大调上下行。听得出"哆来咪"就说明音符号→频率的换算是对的。
 *   ② **复音**：C 大三和弦（do-mi-sol）**同时**按下。这是核心 ——
 *      老的单通道 `ui_beep` 在这里只会听见最后一个音。
 *   ③ **旋律**：小星星。音符之间要**接得上**（不能每个音都被掐掉尾巴）。
 *   ④ **共存**：和弦按着不放的同时连打 `ui_beep` —— 和弦**不该被打断**
 *      （这是 v0.96.485 改造的目的：蜂鸣走自己的声道，不吃掉别的通道）。
 *
 * ## 桌面怎么验收（不用耳朵）
 *
 * `scripts/vmlcli` 不发声，但把那行 `[vml-audio] note-on ch=… note=… voices=N [音符…]`
 * 打进了 stderr。判据是**快照里同时出现三个音**：
 *
 *     note-on ch=0 note=60 … voices=1 [60]
 *     note-on ch=1 note=64 … voices=2 [60,64]
 *     note-on ch=2 note=67 … voices=3 [60,64,67]      ← 复音成立
 *
 * ## ⚠ C 前端的两个坑（`calc.c` 踩过，这里同样致命）
 *
 * 1. **局部数组必须单独一行声明** —— 标量与数组写在同一声明里会让数组分不到槽位，
 *    连读取指令都不生成（症状是"变量恒为 0"，很难往语法上想）。
 * 2. **一行一个变量**，别写 `int x = m[1], y = m[2];`。
 */
#include <waycoder_ui.h>

/* 屏幕宽高（先问再开窗，见 waycoder_ui.h 的说明）。 */
static int SW;
static int SH;

/* 画当前在验什么。
 * ⚠ 每帧都要 `ui_clear` —— 图元是**追加**的，不清会越画越糊。 */
static void show(const char* line)
{
    ui_clear(0xFF101820);
    ui_set_font(18, 0, 0xFFFFFFFF, VML_ANCHOR_LEFT);
    ui_text_cur(16, 40, "音频测试");
    ui_set_font(22, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
    ui_text_cur(16, 84, (char*)line);
    ui_present();
}

/* 弹一个音：起音 → 等 → 关音 → 喘口气。
 * `ms` 是"按住"的时长。 */
static void play(int ch, int note, int ms)
{
    int m[4];
    ui_tone_on(ch, note, 100);
    ui_wait(m, ms);
    ui_tone_off(ch, note);
    ui_wait(m, 40);
}

int main(void)
{
    int m[4];
    int i;
    int base;
    int scale[8];

    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;
    ui_win_open_ex("音频测试", SW, SH, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* ── ① 音阶：C 大调上下行 ─────────────────────────────────────────── */
    show("(1/4) 音阶 —— 应听到 哆来咪发唆拉西哆，再下行");
    for (i = 0; i < 8; i++) scale[i] = 60 + i;
    for (i = 0; i < 8; i++) play(0, scale[i], 240);
    for (i = 7; i >= 0; i--) play(0, scale[i], 240);
    ui_wait(m, 300);

    /* ── ② 复音：C 大三和弦**同时**响 ─────────────────────────────────── */
    show("(2/4) 和弦 —— do mi sol 三个音【同时】响（老接口做不到这个）");
    ui_tone_on(0, 60, 100);       /* do  */
    ui_wait(m, 120);
    ui_tone_on(1, 64, 100);       /* mi  —— 此时 do 仍在响 */
    ui_wait(m, 120);
    ui_tone_on(2, 67, 100);       /* sol —— 三个音同时在响 */
    ui_wait(m, 1800);
    /* 依次收掉，能听出"三层"一层层少下去 */
    ui_tone_off(2, 67);
    ui_wait(m, 300);
    ui_tone_off(1, 64);
    ui_wait(m, 300);
    ui_tone_off(0, 60);
    ui_wait(m, 300);

    /* ── ③ 旋律：小星星（音符要接得上，不能互相掐）─────────────────────── */
    show("(3/4) 旋律 —— 小星星");
    {
        /* ⚠ 局部数组单独一行（见文件头的坑 1）。 */
        int twinkle[7];
        int j;
        twinkle[0] = 60; twinkle[1] = 60; twinkle[2] = 67; twinkle[3] = 67;
        twinkle[4] = 69; twinkle[5] = 69; twinkle[6] = 67;
        for (j = 0; j < 7; j++) play(0, twinkle[j], 380);
        play(0, 65, 380); play(0, 65, 380);
        play(0, 64, 380); play(0, 64, 380);
        play(0, 62, 380); play(0, 62, 380);
        play(0, 60, 700);
    }
    ui_wait(m, 300);

    /* ── ④ 共存：和弦按着不放，同时连打老式蜂鸣 ───────────────────────── */
    show("(4/4) 共存 —— 和弦持续响，同时连打旧式音效（和弦不该断）");
    ui_tone_on(0, 60, 100);
    ui_tone_on(1, 64, 100);
    ui_tone_on(2, 67, 100);
    for (i = 0; i < 6; i++) {
        ui_beep(1200, 60);        /* 老接口：连发只听见最后一个，但**不掐和弦** */
        ui_wait(m, 260);
    }
    ui_tone_off(0, 60);
    ui_tone_off(1, 64);
    ui_tone_off(2, 67);

    show("完成 —— 四段都过了");
    ui_wait(m, 900);

    ui_keep_on(0);
    ui_win_close();
    return 0;
}
