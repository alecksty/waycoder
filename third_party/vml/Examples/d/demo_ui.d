// demo_ui.d —— **最新 UI 接口**示范（D）
// demo_ui.d —— **latest UI interface** demo (D)
//
// 四层示范的第四层：开 `ui_*` 绘图窗口 + **查屏幕方向** + 画图元 + `ui_present` +
// The fourth of the four demo layers: open a `ui_*` drawing window + **query the screen orientation** + draw primitives + `ui_present` +
// 处理触摸/按键，**有界**（收到任意键就退出，否则画满 60 帧自动退出），正常结束。
// handle touch/keys, **bounded** (any key exits, otherwise it exits automatically after 60 frames), finishing normally.
//
// 四条关键差别（与第三层 BGI 相比）：① 开窗显式（宽高来自 `ui_scr_w/h`，不写死分辨率）；
// Four key differences (compared with the third layer, BGI): ① window opening is explicit (width/height come from `ui_scr_w/h`, no hard-coded resolution);
// ② 方向要问 `ui_orientation()`（0=竖屏 / 1=横屏，**开窗前就能问**）；
// ② the orientation is asked with `ui_orientation()` (0=portrait / 1=landscape, **askable before the window opens**);
// ③ `ui_present()` 才是"这一帧画完了"的帧边界；④ 输入是**统一消息队列**
// ③ `ui_present()` is what marks the frame boundary of "this frame is drawn"; ④ input is a **unified message queue**
// （键盘/触摸/定时器/窗口事件同一个队列，`ui_wait_msg` / `ui_msg_a` / `ui_win_closed`）。
// (keyboard / touch / timer / window events all share one queue: `ui_wait_msg` / `ui_msg_a` / `ui_win_closed`).
//
// ◆ 跑法与"画面怎么看"（桌面 vmlcli）
// ◆ How to run it and how to look at the picture (desktop vmlcli)
//
//   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll \
//       third_party/vml/Examples/d/demo_ui.d --screen 480x640 --frames /tmp/out_frames/
//
// ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame 单个文件`**：`ui_win_close()`（#521）
// ⚠ **To see the picture use `--frames <dir>`, not `--frame <single file>`**: `ui_win_close()` (#521)
//   在宿主侧把**整个场景置空**，程序跑完之后 `--frame` 那一刀已经无场景可取，只会打一行
//   empties **the whole scene** on the host side, so after the program finishes the `--frame` snapshot has no scene left to take and only prints
//   「没有可导出的帧（场景是空的）」。`--frames` 是每帧 present 当场拍快照，照常出图。
//   "there is no frame to export (the scene is empty)". `--frames` snapshots each frame on the spot at present time, so images come out as usual.
//   这是桌面脚手架的行为，不是本程序写错了。
//   This is the behavior of the desktop scaffold, not a mistake in this program.
//
// 跑法：命令行页输入  vml run examples/d/demo_ui.d
// How to run: type this on the command-line page  vml run examples/d/demo_ui.d
//
// 颜色：0xAARRGGBB 写**负数十进制**（D 词法器十进制专用，不认 `0x`）。
// Colors: 0xAARRGGBB is written as a **negative decimal** (the D lexer is decimal-only and does not recognize `0x`).

void main() {
    // ── 开窗之前就问方向（这一层与 BGI 最"新"的一条）────────────
    // ── Ask the orientation before opening the window (the "newest" point of this layer compared with BGI) ────────────
    int ori = ui_orientation();
    int lang = ui_get_language();

    int w = ui_scr_w();
    int h = ui_scr_h();
    if (w <= 0) { w = 360; }
    if (h <= 0) { h = 620; }

    ui_win_open(lang == 0 ? "UI 接口演示" : "UI API demo", w, h);
    ui_keep_on(1);
    int tid = ui_timer_set(30, 1);

    int frames = 0;
    int keys = 0;
    int tx = 0;
    int ty = 0;
    int touched = 0;
    int running = 1;

    while (running != 0) {
        if (ui_win_closed() != 0) { running = 0; }
        if (frames >= 60) { running = 0; }

        if (running != 0) {
            // ── ① 底色 + 标题（居中锚点 = 1）────────────────────
            // ── ① background color + title (centering anchor = 1) ────────────────────
            ui_clear(-15724520);
            ui_text(w / 2, 26, lang == 0 ? "WayCoder  ui_*  接口演示 (D)" : "WayCoder  ui_*  API demo (D)", -6643536, 16, 1);
            ui_text(w / 2, 50, lang == 0 ? "开窗 / 方向 / 图元 / present / 消息队列" : "window / orientation / primitives / present / message queue", -6643536, 12, 1);

            // ── ② 屏幕方向（0=竖屏 1=横屏）──────────────────────
            // ── ② screen orientation (0=portrait 1=landscape) ──────────────────────
            ui_text(14, 80, lang == 0 ? "屏幕方向 =" : "Orientation =", -11409298, 13, 0);
            ui_text(96, 80, int_to_str(ori), -11409298, 13, 0);
            if (ori == 1) { ui_text(126, 80, lang == 0 ? "(横屏)" : "(LANDSCAPE)", -11409298, 13, 0); }
            else { ui_text(126, 80, lang == 0 ? "(竖屏)" : "(PORTRAIT)", -11409298, 13, 0); }

            // ── ③ 图元：矩形（实心 / 空心 / 圆角）──────────────
            // ── ③ primitives: rectangles (filled / hollow / rounded corners) ──────────────
            ui_text(14, 108, lang == 0 ? "图元：矩形" : "Primitives: rect", -6643536, 13, 0);
            ui_rect(14, 126, 84, 46, -11409298, 1, 0, 0);
            ui_rect(108, 126, 84, 46, -131246, 0, 2, 0);
            ui_rect(202, 126, 84, 46, -63488, 1, 0, 12);

            // ── ④ 图元：圆 / 椭圆 / 线 ──────────────────────────
            // ── ④ primitives: circle / ellipse / line ─────────────────────────
            ui_text(14, 196, lang == 0 ? "图元：圆 / 椭圆 / 线" : "Primitives: circle / ellipse / line", -6643536, 13, 0);
            ui_circle(46, 250, 30, -131246, 1, 0);
            ui_circle(120, 250, 30, -11409298, 0, 3);
            ui_ellipse(210, 250, 44, 26, -63488, 1, 0);
            int i = 0;
            while (i < 8) {
                ui_line(14, 300 + i * 6, w - 14, 300 + i * 6, -15263713, 2);
                i = i + 1;
            }

            // ── ⑤ 8 级灰度色带（0xAARRGGBB 真彩）───────────────
            // ── ⑤ 8-step grayscale band (0xAARRGGBB true color) ───────────────
            ui_text(14, 358, lang == 0 ? "真彩（0xAARRGGBB）：" : "Truecolor (0xAARRGGBB):", -6643536, 13, 0);
            int s = 0;
            while (s < 8) {
                if (s == 0) { ui_rect(14 + s * 40, 376, 36, 26, -16777216, 1, 0, 0); }
                if (s == 1) { ui_rect(14 + s * 40, 376, 36, 26, -14671840, 1, 0, 0); }
                if (s == 2) { ui_rect(14 + s * 40, 376, 36, 26, -12566464, 1, 0, 0); }
                if (s == 3) { ui_rect(14 + s * 40, 376, 36, 26, -10066330, 1, 0, 0); }
                if (s == 4) { ui_rect(14 + s * 40, 376, 36, 26, -8355712, 1, 0, 0); }
                if (s == 5) { ui_rect(14 + s * 40, 376, 36, 26, -6250336, 1, 0, 0); }
                if (s == 6) { ui_rect(14 + s * 40, 376, 36, 26, -4210753, 1, 0, 0); }
                if (s == 7) { ui_rect(14 + s * 40, 376, 36, 26, -1, 1, 0, 0); }
                s = s + 1;
            }

            // ── ⑥ 动画 + 帧进度条（证明每帧确实在重画）─────────
            // ── ⑥ animation + frame progress bar (proof that every frame really redraws) ─────────
            int col = 14 + frames - (frames / 20) * 20;
            col = 14 + col * 10;
            if (col > w - 46) { col = 14; }
            ui_circle(col, 448, 18, -63488, 1, 0);
            ui_rect(14, 480, (w - 28) * frames / 60, 10, -11409298, 1, 0, 4);

            // ── ⑦ 消息队列：帧数 / 最后一次按键 / 最后一次触摸 ──
            // ── ⑦ message queue: frame count / last key / last touch ──
            ui_text(14, 506, lang == 0 ? "已画帧数 =" : "Frames drawn =", -6643536, 13, 0);
            ui_text(112, 506, int_to_str(frames), -6643536, 13, 0);
            ui_text(14, 528, lang == 0 ? "最后一次按键 =" : "Last key =", -6643536, 13, 0);
            ui_text(140, 528, int_to_str(keys), -6643536, 13, 0);
            if (touched == 1) {
                ui_text(14, 550, lang == 0 ? "最后一次触摸 =" : "Last touch =", -6643536, 13, 0);
                ui_text(140, 550, int_to_str(tx), -6643536, 13, 0);
                ui_text(190, 550, int_to_str(ty), -6643536, 13, 0);
                ui_circle(tx, ty, 26, -131246, 0, 3);
            } else {
                ui_text(14, 550, lang == 0 ? "最后一次触摸 = 无（点一下试试）" : "Last touch = none (try tapping)", -6643536, 13, 0);
            }

            ui_text(w / 2, h - 24, lang == 0 ? "按任意键退出，或画满 60 帧自动退出" : "Press any key to exit, or it exits after 60 frames", -6643536, 12, 1);

            // ── ⑧ 帧边界 ────────────────────────────────────────
            // ── ⑧ frame boundary ────────────────────────────────────────
            ui_present();
        }

        // ── ⑨ 收一条消息（最多等 30ms，保证循环有界）────────────
        // ── ⑨ take one message (waiting at most 30ms, keeping the loop bounded) ────────────
        int t = ui_wait_msg(30);
        if (t == 10) { running = 0; }
        else if (t == 1) { keys = ui_msg_a(); running = 0; }
        else if (t == 6) { tx = ui_msg_a(); ty = ui_msg_b(); touched = 1; }
        else if (t == 4) { tx = ui_msg_a(); ty = ui_msg_b(); touched = 1; }

        frames = frames + 1;
    }

    // ── 收尾：关定时器、放开屏幕常亮、关窗 ──────────────────────
    // ── Wrap-up: kill the timer, release keep-screen-on, close the window ──────────────────────
    ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
}
