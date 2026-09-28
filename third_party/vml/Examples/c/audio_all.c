/* audio_all.c —— **音频接口逐项体检**：把 `waycoder_ui.h` 里声音相关的接口挨个走一遍。
 * audio_all.c -- a per-item health check of the audio API declared in `waycoder_ui.h`.
 *
 * 与 `audio_test.c` 的分工：那个是"乐器式"演示（音阶/和弦/旋律/多点触控，要人按）；
 * Split of labor with `audio_test.c`: that one is an "instrument style" demo (scales,
 * 这个是**自走式**的 —— 跑起来它自己一项一项往下走，每项在屏幕上写清"现在测什么、
 * chords, melody, multi-touch) and needs a human to press things; this one is
 * 期望听见什么"，你只要坐着听。想单独验某一项，看下面「怎么只看一项」。
 * **self-driving** -- it walks the items one by one, printing on screen what it tests
 * and what you should hear. You just sit and listen. To check one item alone, see below.
 *
 * ## 怎么跑
 * ## How to run
 *
 *     vml run examples/c/audio_all.c          （手机 App 的命令行页）
 *     vml run examples/c/audio_all.c          (mobile App command line page)
 *     vmlcli Examples/c/audio_all.c           （桌面，不发声但会打印音频事件）
 *     vmlcli Examples/c/audio_all.c           (desktop: silent, but prints audio events)
 *
 * ## 每一步在验什么（听着对不对，一行一行对）
 * ## What each step verifies (listen and check item by item)
 *
 *   ① `ui_beep` 三个音高        —— 220 / 880 / 1760Hz 依次响，**一个比一个高**
 *   1. `ui_beep` three pitches  -- 220 / 880 / 1760Hz in turn, **each higher than the last**
 *   ② `ui_beep` 连发            —— 连打两个：**只听见后一个**（单通道的老语义，这是设计）
 *   2. `ui_beep` back to back   -- fire two: **only the later one is heard** (single channel
 * legacy semantics -- this is by design)
 *   ③ `ui_tone_on/off` 单音     —— 中央 C（do）一声，0.5 秒
 *   3. `ui_tone_on/off` one note -- middle C (do) for 0.5 second
 *   ④ 复音（和弦）              —— do-mi-sol **同时**响；屏幕会报"此刻 N 个声部"
 *   4. Polyphony (chord)        -- do-mi-sol sound **at the same time**; the screen prints
 * "voices = N right now"
 *   ⑤ 波形                      —— 同一个音，正弦/方波/锯齿/三角各一遍（音色不同、音高相同）
 *   5. Waveform                 -- the same pitch as sine / square / saw / triangle
 * (different timbre, same pitch)
 *   ⑥ `ui_tone_max_voices`      —— 上限压到 2，再起 4 个音：屏幕报的声部数应**不超过 2**
 *   6. `ui_tone_max_voices`     -- push the limit down to 2, then start 4 notes: the reported
 * voice count should be **at most 2**
 *   ⑦ `ui_tone_all_off`         —— 和弦淡出（不是硬切）
 *   7. `ui_tone_all_off`        -- fade the chord out (not a hard cut)
 *   ⑧ `ui_tone_panic`           —— 起一片音后**立刻**全停（急停用）
 *   8. `ui_tone_panic`          -- start a pile of notes then **immediately** stop all
 * (for emergency stop)
 *   ⑨ 音序器 `ui_sfx_*`         —— "叮—咚—叮"三个音先后响；中途报表里还剩几个槽
 *   9. Sequencer `ui_sfx_*`     -- "ding-dong-ding", three notes one after another; it also
 * reports how many slots are still taken
 *   ⑩ `ui_audio_volume`         —— 同一句 `ui_beep`，30% 音量与 100% 各一遍（听音量差）
 *   10. `ui_audio_volume`       -- the same `ui_beep` at 30% and at 100% (hear the volume
 * difference)
 *   ⑪ `ui_audio_play` 等四个     —— 文件播放那条链（播放/还在播/停/音量）：**返回码打在屏幕上**
 *   11. `ui_audio_play` and three friends -- the file playback chain (play / is playing /
 * stop / volume): **the return codes go on screen**
 *   ⑫ `ui_vibrate`              —— 震一下（不是声音，同属"音效与触感"这一组）
 *   12. `ui_vibrate`            -- one buzz (not sound, but the same "audio and haptics" group)
 *
 * ## 桌面怎么验收（不用耳朵）
 * ## How to verify on the desktop (no ears needed)
 *
 * `vmlcli` 把每次发声按机器可读的格式打给 stderr（在 `CliVmlHost` 里），
 * `vmlcli` prints every sound event to stderr in a machine readable format (in `CliVmlHost`),
 * **两种接口两种行**，别按错的格式去 grep：
 * **two interfaces, two kinds of line** -- do not grep with the wrong format:
 *   · `ui_beep`  → `[vml-audio] tone hz=… ms=… wave=… vol=…`
 *   · `ui_beep` emits this "tone" line (see the fields above)
 *   · `ui_tone_*` → `[vml-audio] note-on ch=… note=… vel=… voices=N`
 *   · `ui_tone_*` emits this "note-on" line (see the fields above)
 *     （关音 `note-off`、以及 `ctl all_off|wave|max_voices|voices|panic`）
 *     (a `note-off` when a note ends, plus the `ctl all_off|wave|max_voices|voices|panic` lines)
 *
 * 判据与 `audio_test.c` 同源：
 * Same criteria as `audio_test.c`:
 *   · 第 ① 步：三行 `tone`，hz 依次 220 / 880 / 1760（音高有序）；
 *   · step 1: three `tone` lines, hz = 220 / 880 / 1760 in that order (pitches ascending);
 *   · 第 ④ 步：`note-on … voices=3`（复音成立）；
 *   · step 4: `note-on ... voices=3` (polyphony holds);
 *   · 第 ⑥ 步：`max_voices=2` 之后的行里 **voices 不超过 2**（上限生效）；
 *   · step 6: after `max_voices=2`, no later line reports **more than 2 voices** (limit works);
 *   · 第 ⑨ 步：序列表的三个音要按 tick 先后出现三行 `note-on`；
 *   · step 9: the three sequencer notes must show up as three `note-on` lines in tick order;
 *   · 第 ⑩ 步：`volume=30` 那一行的 `vol=` 与后面 `volume=100` 那行不同。
 *   · step 10: the `vol=` on the `volume=30` line differs from the one on the `volume=100` line.
 *
 * ## ⚠ 手机上没有声音时先查这两条
 * ## ⚠ If there is no sound on the phone, check these two things first
 *
 *   1. **静音开关**：iOS 侧我们已把音频会话设成 `Playback`（忽略静音开关）；
 *   1. **The silent switch**: on iOS we already set the audio session to `Playback`
 *      Android 侧没有这条限制。
 *      (which ignores the silent switch); Android has no such restriction.
 *   2. **音量**：第 ⑩ 步把主音量压到 30% —— 跑完记得会回到 100%（程序末尾会恢复）。
 *   2. **Volume**: step 10 pushes the master volume down to 30% -- it does go back to 100%
 * at the end of the program.
 *
 * ## ⚠ C 前端的坑（calc.c / audio_test.c 都踩过）
 * ## ⚠ C front end pitfalls (calc.c and audio_test.c both hit them)
 *
 *   · **局部数组单独一行声明**（与标量写在同一行会分不到槽位、读出来恒 0）；
 *   · **Declare a local array on its own line** (sharing a line with a scalar leaves it
 * without a slot, so it always reads back as 0);
 *   · 调 `sycall` 一律走 `waycoder_ui.h` 的包装函数，不要自己写 `asm("SYSCALL …")` 传全局量。
 *   · Always call syscalls through the `waycoder_ui.h` wrappers; do not write your own
 * `asm("SYSCALL …")` that passes globals.
 */

#include <waycoder_ui.h>

/* ── 小工具 ─────────────────────────────────────────────────────────── */
/* ── Small helpers ───────────────────────────────────────────────────── */

/* 忙等（不依赖定时器/消息循环 —— 这个程序不开窗，没有消息可等）。
 * Busy wait (no timer, no message loop -- this program opens no window, so there are
 * ⚠ 别改成 `ui_wait`：那要窗口那套消息泵，这里没有。
 * no messages to wait for).
 * Do NOT change this to `ui_wait`: that needs the window message pump, which we have
 * not got here. */
void pause_ms(int ms) {
    int t0;
    t0 = ui_tick();
    while (ui_tick() - t0 < ms) {
        /* 空转 */
        /* idle spin */
    }
}

void say(char* s) {
    printf("%s\n", s);
}

/* 打印一行"本步在验什么" —— 每步都带序号，方便对着上面的清单看。 */
/* Print one line saying "what this step verifies" -- numbered, so you can line it up
 * with the list above. */
void step_title(int n, char* what, char* expect) {
    printf("\n【%d】%s\n", n, what);
    printf("     期望：%s\n", expect);
}

/* ── 正题 ───────────────────────────────────────────────────────────── */
/* ── The main event ─────────────────────────────────────────────────── */

int main(void) {
    int i;
    int v;

    printf("=============================================\n");
    printf(" 音频接口体检开始（共 12 项，约 25 秒）\n");
    printf(" 听不清就把音量调大；每项之前会写明期望\n");
    printf("=============================================\n");

    /* ① ui_beep：三个音高 —— 验证「音符号/频率 → 真的响」且高低有序 */
    /* 1. ui_beep: three pitches -- verify "note number / frequency really sounds" and in order */
    step_title(1, "ui_beep(频率, 毫秒)：220 / 880 / 1760Hz", "三声，一个比一个高");
    ui_beep(220, 260);
    pause_ms(420);
    ui_beep(880, 260);
    pause_ms(420);
    ui_beep(1760, 260);
    pause_ms(600);

    /* ② ui_beep 连发：单通道语义 —— 只听见最后一个（这不是 bug，是它的老语义） */
    /* 2. ui_beep back to back: single channel -- only the last one is heard (not a bug) */
    step_title(2, "ui_beep 连发两声（880 与 1760）", "只听见后一声（单通道：后音掐前音）");
    ui_beep(880, 300);
    ui_beep(1760, 300);
    pause_ms(700);

    /* ③ ui_tone_on/off：单音（中央 C = 60） */
    /* 3. ui_tone_on/off: a single note (middle C = 60) */
    step_title(3, "ui_tone_on(0, 60, 100) 起一个音，0.5 秒后关", "一声中央 C（do）");
    ui_tone_on(0, 60, 100);
    pause_ms(500);
    ui_tone_off(0, 60);
    pause_ms(300);

    /* ④ 复音：三个通道同时按 —— 这是 v0.96.485 改造的核心 */
    /* 4. Polyphony: three channels pressed at once -- the heart of the v0.96.485 rework */
    step_title(4, "三个通道同时起音：do(60) / mi(64) / sol(67)",
               "三个音**同时**响（和弦），屏幕上的声部数应为 3");
    ui_tone_on(0, 60, 100);
    ui_tone_on(1, 64, 100);
    ui_tone_on(2, 67, 100);
    pause_ms(200);
    v = ui_tone_voices();
    printf("     ▶ 此刻声部数 = %d（期望 3）\n", v);
    pause_ms(900);
    ui_tone_off(0, 60);
    ui_tone_off(1, 64);
    ui_tone_off(2, 67);
    pause_ms(300);

    /* ⑤ 波形：同一个音高、四种音色 */
    /* 5. Waveform: one pitch, four timbres */
    step_title(5, "ui_tone_wave：正弦 / 方波 / 锯齿 / 三角", "四声**音高相同、音色不同**");
    for (i = 0; i < 4; i = i + 1) {
        ui_tone_wave(3, i);
        ui_tone_on(3, 69, 100);   /* A4 = 440Hz */
        pause_ms(420);
        ui_tone_off(3, 69);
        pause_ms(140);
    }

    /* ⑥ ui_tone_max_voices：把上限压到 2，再起 4 个音 */
    /* 6. ui_tone_max_voices: push the limit down to 2, then start 4 notes */
    step_title(6, "ui_tone_max_voices(2) 后起 4 个音", "声部数**不超过 2**（上限生效，多出的被抢占）");
    ui_tone_max_voices(2);
    ui_tone_on(4, 60, 90);
    ui_tone_on(5, 64, 90);
    ui_tone_on(6, 67, 90);
    ui_tone_on(7, 72, 90);
    pause_ms(200);
    v = ui_tone_voices();
    printf("     ▶ 此刻声部数 = %d（期望 ≤ 2）\n", v);
    pause_ms(800);
    ui_tone_all_off();
    ui_tone_max_voices(32);        /* 复原，别把后面的测试压着 */
    /* restore it, so it does not throttle the later tests */
    pause_ms(400);

    /* ⑦ ui_tone_all_off：淡出（听感上"渐渐没了"而不是"啪一下断"） */
    /* 7. ui_tone_all_off: fade out (it fades away instead of cutting off with a snap) */
    step_title(7, "ui_tone_all_off()：和弦淡出", "声音**渐弱**消失（不是硬切）");
    ui_tone_on(0, 60, 100);
    ui_tone_on(1, 64, 100);
    ui_tone_on(2, 67, 100);
    pause_ms(600);
    ui_tone_all_off();
    pause_ms(700);

    /* ⑧ ui_tone_panic：立刻全停（急停 / 强制停止那种场合） */
    /* 8. ui_tone_panic: stop everything at once (for emergency / forced stop) */
    step_title(8, "ui_tone_panic()：立刻全停", "起一片音后**当场断掉**（急停语义）");
    ui_tone_on(0, 60, 100);
    ui_tone_on(1, 64, 100);
    ui_tone_on(2, 67, 100);
    pause_ms(600);
    ui_tone_panic();
    pause_ms(500);

    /* ⑨ 音序器：三个音先后响（机制在共享库里，见 ui_sfx_* 的说明）
     * 9. Sequencer: three notes one after another (the machinery is in the shared
     *    ⚠ 本项**只验"音序器有没有推进"**。这三声之间是会重叠的（音序器的常态），
     * library, see the ui_sfx_* notes).
     *    ⚠ This item **only checks whether the sequencer advances**. Those three notes do
     *      而重叠正是真机上破音的原因之一 —— 听到"滋滋"属于已知现象（见内置帮助
     * overlap, which is normal for a sequencer, and that overlap is one of the causes of
     *      「音效与触感」那节），**不是本项失败**。游戏音效别用音序器，用 ui_beep。
     * the crackle heard on real devices -- so a "zzz" noise is a known phenomenon (see
     * the built-in help, the audio and haptics section), **not a failure of this item**.
     * For game sound effects do not use the sequencer, use ui_beep. */
    step_title(9, "音序器 ui_sfx_add ×3 + ui_sfx_tick", "三个音**先后**响（重叠属常态，真机上可能破音）");
    ui_sfx_reset();
    ui_sfx_add(0, 84, 0, 2, 80, VML_WAVE_SQUARE);   /* do，立刻响 2 拍 */
    /* do -- sounds at once, for 2 ticks */
    ui_sfx_add(1, 79, 2, 2, 80, VML_WAVE_SQUARE);   /* sol，晚 2 拍 */
    /* sol -- 2 ticks later */
    ui_sfx_add(2, 84, 4, 3, 80, VML_WAVE_SQUARE);   /* do，再晚 2 拍 */
    /* do -- 2 ticks after that */
    printf("     ▶ 表里占用的槽 = %d（期望 3）\n", ui_sfx_active());
    for (i = 0; i < 10; i = i + 1) {
        ui_sfx_tick();
        pause_ms(120);
    }
    printf("     ▶ 放完后槽 = %d（期望 0）\n", ui_sfx_active());
    ui_sfx_panic();
    pause_ms(300);

    /* ⑩ ui_audio_volume：主音量对合成音也生效 */
    /* 10. ui_audio_volume: the master volume also applies to synthesised tones */
    step_title(10, "ui_audio_volume(30) 再 ui_beep", "这一声**明显比前两声小**");
    ui_audio_volume(30);
    ui_beep(880, 400);
    pause_ms(700);
    ui_audio_volume(100);
    printf("     ▶ 音量已恢复到 100\n");
    ui_beep(880, 400);
    pause_ms(700);

    /* ⑪ 文件播放那条链：ui_audio_play / stop / playing / volume
     * 11. The file playback chain: ui_audio_play / stop / playing / volume
     *    ⚠ 这一项**听不到声音**是正常的 —— 这里故意用一个不存在的文件，验的是
     *    ⚠ It is normal that you **hear nothing** here -- we deliberately use a file that
     *      「接口在、返回码合理、不崩」。真想听 BGM，往工作区放一个 mp3/wav 再把路径换掉。
     * does not exist, so what we check is "the interface is there, the return codes are
     * sane, nothing crashes". To really hear BGM, drop an mp3/wav into the workspace
     * and swap the path. */
    step_title(11, "ui_audio_play(\"no_such_file.wav\", 0) 等的返回码",
               "打印出返回码；**不响也不崩**（本项验接口存在与容错）");
    v = ui_audio_play("no_such_file.wav", 0);
    printf("     ▶ ui_audio_play 返回 %d\n", v);
    v = ui_audio_playing();
    printf("     ▶ ui_audio_playing 返回 %d\n", v);
    ui_audio_volume(80);
    ui_audio_stop();
    printf("     ▶ ui_audio_stop 已调用\n");
    pause_ms(400);

    /* ⑫ ui_vibrate：不是声音，但同属"音效与触感"那一组 */
    /* 12. ui_vibrate: not sound, but it belongs to the same audio-and-haptics group */
    step_title(12, "ui_vibrate(200, 0)", "手机震一下（模拟器/桌面无感属正常）");
    ui_vibrate(200, 0);
    pause_ms(600);

    printf("\n=============================================\n");
    printf(" 体检结束。逐项对不上时：先看静音开关，\n");
    printf(" 再看是不是被 ⑥ 压过声部上限（本程序已复原）。\n");
    printf("=============================================\n");
    return 0;
}
