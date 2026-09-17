// 接方块 —— 用 **JavaScript** 写的手机游戏
//
// 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
//
// ◆ 手机那套 UI
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="javascript" Libs="vmlui.vml">` 挂上来。
//
// ◆ 两条写法要求（不是偏好）
//
//   ① 调库函数**必须先 `native function` 声明** —— 本前端对不认识的函数名会先找 `func_<名>`，
//      都没有就**把名字当变量**、编成「MOVE R1, var_<名>；CALL R0」⇒ 运行期跳野地址
//      （`CodeGenerator.Calls.cs:823-855`）。声明之后才发裸标签 CALL。
//      （`asm()` 已从本前端移除，用不了。）
//   ② 颜色写**负数十进制**（`-65536` = `0xFFFF0000`）。
//
// ⚠ `corpus/javascript/skel.js` 的文件头记着「8 参的 ui_rect 参数会整体反序」——
//   本次读生成汇编，`call lib_vmlui_ui_rect` 之前是**从右到左**逐参压栈、与 C 前端同形，
//   **没看到反序**。该注记疑为调用约定统一前的旧观察。
//   真机画面以 `scripts/maui-vml-verify/corpus.tsv` 的 `game-javascript-catch` 为准。

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

// 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高
let A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

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
    ui_clear(-15724520);
    ui_text(8, 8, "得分", -6643536, 13, 0);
    ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
    ui_text(A[8] / 2, 8, "最高", -6643536, 13, 1);
    ui_rect(A[8] / 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
    ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
    ui_circle(A[1], A[2], 9, -131246, 1, 0);
    if (A[7] == 0) {
        ui_text(A[8] / 2, A[9] / 2, "按回车重开", -131246, 16, 1);
    }
    ui_present();
}

function step() {
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
        ui_beep(880, 30);
    }
    if (A[2] > A[9]) {
        A[7] = 0;
        ui_beep(220, 260);
        draw();
        ui_dlg_msg("接方块", "没接住，这一局结束。\n再来一局？（选「否」退出）", 0);
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
    ui_win_open("接方块", w, h);
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
