// 接方块 —— 用 **JavaScript** 写的手机游戏
// Catch -- a mobile game written in **JavaScript**
//
// 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
// Gameplay: the left/right arrow keys move the paddle at the bottom and bounce the falling ball back; missing it ends the round. Each catch = +10 points.
//
// ◆ 手机那套 UI
// ◆ The mobile UI set
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
// 由 `vmltool.config.xml` 的 `<Language Name="javascript" Libs="vmlui.vml">` 挂上来。
// pulled in by `<Language Name="javascript" Libs="vmlui.vml">` in `vmltool.config.xml`.
//
// ◆ 两条写法要求（不是偏好）
// ◆ Two style requirements (not preferences)
//
//   ① 调库函数**必须先 `native function` 声明** —— 本前端对不认识的函数名会先找 `func_<名>`，
//   ① **Calling a library function requires a `native function` declaration first** -- for an unknown function name this frontend first looks for `func_<name>`,
//      都没有就**把名字当变量**、编成「MOVE R1, var_<名>；CALL R0」⇒ 运行期跳野地址
//      and if that is missing too it **treats the name as a variable** and compiles "MOVE R1, var_<name>; CALL R0" => a wild jump at run time
//      （`CodeGenerator.Calls.cs:823-855`）。声明之后才发裸标签 CALL。
//      (`CodeGenerator.Calls.cs:823-855`). Only after the declaration does it emit a bare-label CALL.
//      （`asm()` 已从本前端移除，用不了。）
//      (The `asm()` form has been removed from this frontend and cannot be used.)
//   ② 颜色写**负数十进制**（`-65536` = `0xFFFF0000`）。
//   ② Colors are written as **negative decimals** (`-65536` = `0xFFFF0000`).
//
// ⚠ `corpus/javascript/skel.js` 的文件头记着「8 参的 ui_rect 参数会整体反序」——
// ⚠ The header of `corpus/javascript/skel.js` notes that "the 8 arguments of ui_rect come out in reverse order" --
//   本次读生成汇编，`call lib_vmlui_ui_rect` 之前是**从右到左**逐参压栈、与 C 前端同形，
//   reading the generated assembly this time, the arguments before `call lib_vmlui_ui_rect` are pushed **right to left**, the same shape as the C frontend,
//   **没看到反序**。该注记疑为调用约定统一前的旧观察。
//   so **no reversal was seen**. That note is probably an observation from before the calling convention was unified.
//   真机画面以 `scripts/maui-vml-verify/corpus.tsv` 的 `game-javascript-catch` 为准。
//   For what it looks like on a real device, trust `game-javascript-catch` in `scripts/maui-vml-verify/corpus.tsv`.

native function ui_scr_w() {}
native function ui_scr_h() {}
native function ui_win_open(t, w, h) {}
native function ui_win_closed() {}
native function ui_win_close() {}
native function ui_clear(c) {}
native function ui_rect(x, y, w, h, c, fill, lw, r) {}
native function ui_circle(cx, cy, r, c, fill, lw) {}
native function ui_text(x, y, s, c, size, anchor) {}
native function ui_present() {}
native function ui_timer_set(ms, tag) {}
native function ui_timer_kill(id) {}
native function ui_wait_msg(timeout) {}
native function ui_msg_a() {}
native function ui_beep(freq, ms) {}
native function ui_keep_on(on) {}
native function ui_dlg_msg(title, body, style) {}
native function ui_get_language() {}

// 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高 10=界面语言
// State: 0=paddle x 1=ball x 2=ball y 3=ball dx 4=ball dy 5=score 6=best 7=alive 8=screen w 9=screen h 10=UI language
let A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

function resetGame() {
    A[0] = A[8] / 2 - 40;
    A[1] = A[8] / 2;
    A[2] = 70;
    A[3] = 3;
    A[4] = 5;
    A[5] = 0;
    A[7] = 1;
}

function draw() {
    let lang = A[10];
    ui_clear(-15724520);
    ui_text(8, 8, lang == 0 ? "得分" : "Score", -6643536, 13, 0);
    ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
    ui_text(A[8] / 2, 8, lang == 0 ? "最高" : "Best", -6643536, 13, 1);
    ui_rect(A[8] / 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
    ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
    ui_circle(A[1], A[2], 9, -131246, 1, 0);
    if (A[7] == 0) {
        ui_text(A[8] / 2, A[9] / 2, lang == 0 ? "按回车重开" : "Press Enter to restart", -131246, 16, 1);
    }
    ui_present();
}

function step() {
    let lang = A[10];
    if (A[7] == 0) {
        return;
    }
    A[1] = A[1] + A[3];
    A[2] = A[2] + A[4];
    if (A[1] < 10) { A[1] = 10; A[3] = 0 - A[3]; }
    if (A[1] > A[8] - 10) { A[1] = A[8] - 10; A[3] = 0 - A[3]; }
    if (A[2] < 30) { A[2] = 30; A[4] = 0 - A[4]; }
    if (A[2] > A[9] - 52 && A[2] < A[9] - 30 && A[1] > A[0] - 9 && A[1] < A[0] + 89) {
        A[4] = 0 - A[4];
        A[2] = A[9] - 52;
        A[5] = A[5] + 10;
        if (A[5] > A[6]) { A[6] = A[5]; }
        // 音效：单音 ui_beep（v0.96.509 从音序器换回来 ——
        // Sound: single-tone ui_beep (switched back from the sequencer in v0.96.509 --
        //   那一版多声部叠加 / 长音拖尾在真机上破音）
        //   that version's multi-voice layering / long-note tails crackled on the real device)
        ui_beep(1047, 165);
    }
    if (A[2] > A[9]) {
        A[7] = 0;
        // 音效：单音 ui_beep；**结局音取最低音**（接住 1047 / 没接住 131，差得开）
        // Sound: single-tone ui_beep; **the ending tone takes the lowest pitch** (catch 1047 / miss 131, far enough apart)
        ui_beep(131, 320);
        draw();
        if (ui_dlg_msg(lang == 0 ? "接方块" : "Catch", lang == 0 ? "没接住，这一局结束。\n再来一局？（选「否」退出）" : "Missed. Round over.\nPlay again? (choose 'No' to quit)", 0) != 0) { ui_win_close(); return; }
        resetGame();
    }
}

function main() {
    let w = ui_scr_w();
    let h = ui_scr_h();
    if (w <= 0) { w = 360; }
    if (h <= 0) { h = 620; }
    A[8] = w;
    A[9] = h;
    A[10] = ui_get_language();
    let lang = A[10];
    ui_win_open(lang == 0 ? "接方块" : "Catch", w, h);
    ui_keep_on(1);
    resetGame();
    let tid = ui_timer_set(40, 0);

    while (ui_win_closed() == 0) {

        draw();
        let t = ui_wait_msg(0);
        if (t == 10) { break; }
        if (t == 9) { step(); }
        if (t == 1) {
            let k = ui_msg_a();
            if (k == 27) { break; }
            if (k == 37) { A[0] = A[0] - 20; if (A[0] < 4) { A[0] = 4; } }
            if (k == 39) { A[0] = A[0] + 20; if (A[0] > w - 84) { A[0] = w - 84; } }
            if (k == 13) { resetGame(); }
        }
    }
    ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
}
