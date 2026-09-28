/* audio_test.c —— **音频 + 多点触控测试**：音阶 / 和弦 / 旋律 / 共存 / 交互。
 * audio_test.c -- audio + multi-touch test: scale / chord / melody / coexistence / touch.
 *
 * ## 它验的是**五件事**
 * ## It checks **five things**
 *
 *   ① **音准**：C 大调上下行。听得出"哆来咪"就说明音符号→频率的换算是对的。
 *   1. **Pitch accuracy**: a C major scale up and down. If you can hear do-re-mi, then the
 * note number -> frequency conversion is right.
 *   ② **复音**：C 大三和弦（do-mi-sol）**同时**按下。这是核心 ——
 *   2. **Polyphony**: a C major chord (do-mi-sol) pressed **at the same time**.
 *      老的单通道 `ui_beep` 在这里只会听见最后一个音。
 *   This is the core -- the old single channel `ui_beep` would only let you hear the last note.
 *   ③ **旋律**：小星星。音符之间要**接得上**（不能每个音都被掐掉尾巴）。
 *   3. **Melody**: Twinkle Twinkle. The notes must **join up** (no note has its tail cut off).
 *   ④ **共存**：和弦按着不放的同时连打 `ui_beep` —— 和弦**不该被打断**
 *   4. **Coexistence**: hold a chord down and fire `ui_beep` at the same time -- the chord
 *      （这是 v0.96.485 改造的目的：蜂鸣走自己的声道，不吃掉别的通道）。
 *      **must not be interrupted** (that is what the v0.96.485 rework was for: the beep
 * runs on its own voice and does not eat the other channels).
 *   ⑤ **多点触控**（交互，10 秒）：按住七根色条发声，**多指同按就是和弦** ——
 *   5. **Multi-touch** (interactive, 10 seconds): hold the seven colour bars to make
 *      这一段同时验「音频接口」与「多点触控」两件事：
 *      sound; **several fingers at once make a chord** -- this block checks both the audio
 *      interface and multi-touch at the same time:
 *      音高对不对是耳朵听，多指能不能同时响看 `voices`。
 *      whether the pitches are right is for your ears; whether several fingers sound
 *      together is read off `voices`.
 *
 * ## 桌面怎么验收（不用耳朵）
 * ## How to verify on the desktop (no ears needed)
 *
 * `scripts/vmlcli` 不发声，但把那行 `[vml-audio] note-on ch=… note=… voices=N [音符…]`
 * `scripts/vmlcli` makes no sound, but it writes the line
 * 打进了 stderr。判据是**快照里同时出现三个音**：
 * `[vml-audio] note-on ch=… note=… voices=N [notes…]` to stderr. The criterion is that
 * **three notes appear together in the snapshot**:
 *
 *     note-on ch=0 note=60 … voices=1 [60]
 *     note-on ch=1 note=64 … voices=2 [60,64]
 *     note-on ch=2 note=67 … voices=3 [60,64,67]      ← 复音成立
 *     note-on ch=2 note=67 … voices=3 [60,64,67]      <- polyphony holds
 *
 * 第 ⑤ 段靠 `--input` 脚本投 `touchn_down 0/1/2 <x> <y>` 到不同色条上，
 * Block 5 drives `--input` scripts that post `touchn_down 0/1/2 <x> <y>` onto different
 * 期望看到 `voices=3`。
 * colour bars, and expects to see `voices=3`.
 *
 * ## ⚠ C 前端的两个坑（`calc.c` 踩过，这里同样致命）
 * ## ⚠ Two C front end pitfalls (`calc.c` hit them, and they are just as fatal here)
 *
 * 1. **局部数组必须单独一行声明** —— 标量与数组写在同一声明里会让数组分不到槽位，
 * 1. **A local array must be declared on its own line** -- putting a scalar and an array
 *    连读取指令都不生成（症状是"变量恒为 0"，很难往语法上想）。
 *    in one declaration leaves the array without a slot; not even the read instruction
 *    is generated (the symptom is "the variable is always 0", hard to trace back to syntax).
 * 2. **一行一个变量**，别写 `int x = m[1], y = m[2];`。
 * 2. **One variable per line** -- do not write `int x = m[1], y = m[2];`.
 */
#include <waycoder_ui.h>

/* 屏幕宽高（先问再开窗，见 waycoder_ui.h 的说明）。 */
/* Screen width and height (ask first, then open the window -- see waycoder_ui.h). */
static int SW;
static int SH;

/* 画当前在验什么。
 * Draw what is currently being checked.
 * ⚠ 每帧都要 `ui_clear` —— 图元是**追加**的，不清会越画越糊。
 * ⚠ `ui_clear` is needed every frame -- primitives are **appended**, so without a
 * clear the picture gets smeared. */
static void show(const char* line, int lang)
{
    ui_clear(0xFF101820);
    ui_set_font(18, 0, 0xFFFFFFFF, VML_ANCHOR_LEFT);
    ui_text_cur(16, 40, lang == 0 ? "音频测试" : "Audio test");
    ui_set_font(22, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
    ui_text_cur(16, 84, (char*)line);
    ui_present();
}

/* 弹一个音：起音 → 等 → 关音 → 喘口气。
 * Play one note: note on -> wait -> note off -> a short breath.
 * `ms` 是"按住"的时长。
 * `ms` is how long the key is held down. */
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
    int lang;

    lang = ui_get_language();      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
                                   /* UI language: queried once at start (do not call it every frame) */
    SW = ui_scr_w();
    SH = ui_scr_h();
    if (SW <= 0) SW = 360;
    if (SH <= 0) SH = 620;
    ui_win_open_ex(lang == 0 ? "音频测试" : "Audio test", SW, SH, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
    ui_keep_on(1);

    /* ── ① 音阶：C 大调上下行 ─────────────────────────────────────────── */
    /* -- 1. Scale: a C major scale up and down --------------------------------------- */
    show(lang == 0 ? "(1/5) 音阶 —— 应听到 哆来咪发唆拉西哆，再下行"
                   : "(1/5) Scale -- you should hear do-re-mi-fa-sol-la-ti-do, then back down", lang);
    for (i = 0; i < 8; i++) scale[i] = 60 + i;
    for (i = 0; i < 8; i++) play(0, scale[i], 240);
    for (i = 7; i >= 0; i--) play(0, scale[i], 240);
    ui_wait(m, 300);

    /* ── ② 复音：C 大三和弦**同时**响 ─────────────────────────────────── */
    /* -- 2. Polyphony: a C major chord **sounding at once** --------------------------- */
    show(lang == 0 ? "(2/5) 和弦 —— do mi sol 三个音【同时】响（老接口做不到这个）"
                   : "(2/5) Chord -- do mi sol sound [at the same time] (the old API cannot do this)", lang);
    ui_tone_on(0, 60, 100);       /* do  */
    ui_wait(m, 120);
    ui_tone_on(1, 64, 100);       /* mi  —— 此时 do 仍在响 */
    /* mi -- do is still sounding at this point */
    ui_wait(m, 120);
    ui_tone_on(2, 67, 100);       /* sol —— 三个音同时在响 */
    /* sol -- all three notes are sounding at once now */
    ui_wait(m, 1800);
    /* 依次收掉，能听出"三层"一层层少下去 */
    /* Release them one by one, so you can hear the three layers thin out */
    ui_tone_off(2, 67);
    ui_wait(m, 300);
    ui_tone_off(1, 64);
    ui_wait(m, 300);
    ui_tone_off(0, 60);
    ui_wait(m, 300);

    /* ── ③ 旋律：小星星（音符要接得上，不能互相掐）─────────────────────── */
    /* -- 3. Melody: Twinkle Twinkle (notes must join up, not cut each other off) ------ */
    show(lang == 0 ? "(3/5) 旋律 —— 小星星" : "(3/5) Melody -- Twinkle Twinkle", lang);
    {
        /* ⚠ 局部数组单独一行（见文件头的坑 1）。 */
        /* ⚠ local array on its own line (see pitfall 1 at the top of the file). */
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
    /* -- 4. Coexistence: hold the chord, fire the old style beep at the same time ----- */
    show(lang == 0 ? "(4/5) 共存 —— 和弦持续响，同时连打旧式音效（和弦不该断）"
                   : "(4/5) Coexistence -- the chord keeps sounding while old-style beeps fire", lang);
    ui_tone_on(0, 60, 100);
    ui_tone_on(1, 64, 100);
    ui_tone_on(2, 67, 100);
    for (i = 0; i < 6; i++) {
        ui_beep(1200, 60);        /* 老接口：连发只听见最后一个，但**不掐和弦** */
        /* old interface: back to back beeps only let the last one be heard, but the chord
         * is **not cut off** */
        ui_wait(m, 260);
    }
    ui_tone_off(0, 60);
    ui_tone_off(1, 64);
    ui_tone_off(2, 67);

    /* ── ⑤ 多点触控：**按住就响，多指就是和弦** ──────────────────────────
     * -- 5. Multi-touch: **press and it sounds, several fingers make a chord** ---------
     *
     * 这一段**同时验两件事**：
     * This block checks **two things at once**:
     *   · **音频接口** —— 手指按到哪一根色条就发哪个音（听得出音高对不对）
     *   · **the audio interface** -- whichever colour bar a finger lands on, that note
     *   · **多点触控** —— 几根手指同时按 ⇒ 几个音**同时**响（和弦）
     *   · **multi-touch** -- several fingers down at once means several notes sounding
     *
     * ⚠ 必须**轮询** `ui_touch(slot)`：事件式的消息只有槽位 0 会投，
     * ⚠ You must **poll** `ui_touch(slot)`: the event style message is only delivered to
     *   光靠事件最多只能做单指 —— 那样就测不出复音了。
     * slot 0, so events alone can only ever give you one finger -- that would never test
     * polyphony.
     * ⚠ 限时 10 秒：它是自测程序，不能永远停在这里等手指。
     * ⚠ It is limited to 10 seconds: this is a self test program and cannot sit here
     * waiting for fingers forever. */
    {
        int keyNote[10];
        int prev[10];
        int slot;
        int x;
        int col;
        int w;
        int tick;
        int down;
        int cur;
        int semi[7];
        int t2[3];

        semi[0] = 0; semi[1] = 2; semi[2] = 4; semi[3] = 5;
        semi[4] = 7; semi[5] = 9; semi[6] = 11;
        for (slot = 0; slot < 10; slot++) { keyNote[slot] = -1; prev[slot] = 0; }

        w = SW / 7;
        tick = 0;
        while (tick < 10000 && ui_win_closed() == 0) {
            /* 七根色条：按着的变亮，一眼看出"哪个槽位在读" */
            /* Seven colour bars: the held one lights up, so you can see which slot is reading */
            ui_clear(0xFF101820);
            ui_set_font(19, VML_FONT_BOLD, 0xFF7FD4FF, VML_ANCHOR_LEFT);
            ui_set_valign(VML_VANCHOR_MIDDLE);
            ui_text_cur(14, 36, lang == 0 ? "(5/5) 按住色条发声 · 多指同按 = 和弦"
                                         : "(5/5) Hold a bar to sound a note · several fingers = a chord");
            for (i = 0; i < 7; i++) {
                x = i * w;
                cur = 60 + semi[i];
                down = 0;
                for (slot = 0; slot < 10; slot++) {
                    if (keyNote[slot] == cur) down = 1;
                }
                if (down) ui_rect(x + 2, 70, w - 4, SH - 100, 0xFFFFD98A, 1, 0, 8);
                else ui_rect(x + 2, 70, w - 4, SH - 100, 0xFF2A3A4A, 1, 0, 8);
                ui_rect(x + 2, 70, w - 4, SH - 100, 0xFF8A8A92, 0, 2, 8);
            }
            ui_present();

            for (slot = 0; slot < 10; slot++) {
                if (ui_touch(slot, t2) == 0) continue;
                down = t2[2];
                col = -1;
                if (down) {
                    col = t2[0] / w;
                    if (col < 0) col = 0;
                    if (col > 6) col = 6;
                }
                if (down && prev[slot] == 0) {
                    /* 这一根手指刚按下：在**它自己的通道**上起音
                     * This finger has just gone down: start its note on **its own channel**
                     * （通道不同才不会互相掐掉 —— 那正是复音要的）
                     * (different channels are what stops them cutting each other off --
                     * that is exactly what polyphony needs) */
                    keyNote[slot] = 60 + semi[col];
                    ui_tone_on(slot, keyNote[slot], 110);
                } else if (!down && prev[slot] != 0) {
                    if (keyNote[slot] >= 0) ui_tone_off(slot, keyNote[slot]);
                    keyNote[slot] = -1;
                }
                prev[slot] = down;
            }

            ui_wait(m, 40);
            tick = tick + 40;
        }

        /* 收尾：这一段结束时可能还有手指按着 */
        /* Wrap up: fingers may still be down when this block ends */
        for (slot = 0; slot < 10; slot++) {
            if (keyNote[slot] >= 0) ui_tone_off(slot, keyNote[slot]);
        }
    }

    show(lang == 0 ? "完成 —— 五段都过了" : "Done -- all five blocks passed", lang);
    ui_wait(m, 900);

    ui_keep_on(0);
    ui_win_close();
    return 0;
}
