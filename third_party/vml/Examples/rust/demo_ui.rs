// demo_ui.rs —— **最新 UI 接口**示范（Rust）
//
// 四层示范的第四层：开 `ui_*` 绘图窗口 + **查屏幕方向** + 画图元 + `ui_present` +
// 处理触摸/按键，**有界**（收到任意键就退出，否则画满 60 帧自动退出），正常结束。
//
// 与第三层（BGI）刻意不同：BGI 是"固定分辨率 + 索引色"的老世界，这一层是
// **宿主窗口 + 0xAARRGGBB 真彩 + 统一消息队列**。四条关键差别：
//
//   ① **开窗显式**：`ui_win_open(标题, 宽, 高)`，宽高来自 `ui_scr_w()/ui_scr_h()`
//      （可用绘图区），不是写死的 640x480。
//   ② **屏幕方向要问**：`ui_orientation()` 返回 0=竖屏 / 1=横屏 —— 程序据此决定
//      "面板横排还是竖排"。**开窗之前就能问**（见 `Lib/c/waycoder_ui.h`）。
//   ③ **显式呈现**：图元只是追加进场景，`ui_present()` 才是"这一帧画完了"的帧边界。
//   ④ **输入是消息队列**：键盘/触摸/定时器/窗口事件全走同一个队列
//      （`ui_wait_msg` / `ui_msg_a` / `ui_win_closed`）。
//
// ◆ 跑法与"画面怎么看"（桌面 vmlcli）
//
//   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll \
//       third_party/vml/Examples/rust/demo_ui.rs --screen 480x640 --frames /tmp/out_frames/
//
// ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame 单个文件`**。原因不在本程序：
//   `ui_win_close()`（#521）在宿主侧把**整个场景置空**（`VmlHostRuntime.WinClose` 的
//   `_scene = null`），于是程序跑完之后 `--frame` 那一刀**已经没有场景可取**，只会打一行
//   「没有可导出的帧（场景是空的）」。`--frames` 是**每帧 present 当场拍快照**，
//   所以照常出图。这是桌面脚手架的行为，不是本程序写错了 —— 手机上窗口关掉当然也没有画面。
//
// ◆ 有界（任务要求）
//
//   两条出口都写了：收到**任意按键**（消息类型 1 = KeyDown）立刻退出；或画满 60 帧自动退出。
//   退出前 `ui_win_close()`，程序正常结束、退出码 0。
//
// 跑法：命令行页输入  vml run examples/rust/demo_ui.rs
//
// 颜色：0xAARRGGBB 写**负数十进制**（Rust 词法器十进制专用，不认 `0x`）。

fn main() {
    // ── 开窗之前就问方向（这是这一层与 BGI 最"新"的一条）──────────
    let ori = ui_orientation();

    let mut w = ui_scr_w();
    let mut h = ui_scr_h();
    if w <= 0 { w = 360; }
    if h <= 0 { h = 620; }

    ui_win_open("UI 接口演示", w, h);
    ui_keep_on(1);
    let tid = ui_timer_set(30, 1);

    let mut frames = 0;
    let mut keys = 0;
    let mut tx = 0;
    let mut ty = 0;
    let mut touched = 0;
    let mut running = 1;

    while running != 0 {
        if ui_win_closed() != 0 { break; }
        if frames >= 60 { break; }

        // ── ① 底色 + 标题（居中锚点 = 1）────────────────────────
        ui_clear(-15724520);
        ui_text(w / 2, 26, "WayCoder  ui_*  接口演示 (Rust)", -6643536, 16, 1);
        ui_text(w / 2, 50, "开窗 / 方向 / 图元 / present / 消息队列", -6643536, 12, 1);

        // ── ② 屏幕方向（0=竖屏 1=横屏）──────────────────────────
        ui_text(14, 80, "屏幕方向 =", -11409298, 13, 0);
        ui_text(96, 80, int_to_str(ori), -11409298, 13, 0);
        if ori == 1 { ui_text(126, 80, "(横屏)", -11409298, 13, 0); }
        else { ui_text(126, 80, "(竖屏)", -11409298, 13, 0); }

        // ── ③ 图元：矩形（实心 / 空心 / 圆角）──────────────────
        ui_text(14, 108, "图元：矩形", -6643536, 13, 0);
        ui_rect(14, 126, 84, 46, -11409298, 1, 0, 0);
        ui_rect(108, 126, 84, 46, -131246, 0, 2, 0);
        ui_rect(202, 126, 84, 46, -63488, 1, 0, 12);

        // ── ④ 图元：圆 / 椭圆 / 线 ──────────────────────────────
        ui_text(14, 196, "图元：圆 / 椭圆 / 线", -6643536, 13, 0);
        ui_circle(46, 250, 30, -131246, 1, 0);
        ui_circle(120, 250, 30, -11409298, 0, 3);
        ui_ellipse(210, 250, 44, 26, -63488, 1, 0);
        let mut i = 0;
        while i < 8 {
            ui_line(14, 300 + i * 6, w - 14, 300 + i * 6, -15263713, 2);
            i = i + 1;
        }

        // ── ⑤ 16 级灰度色带（0xAARRGGBB 真彩）──────────────────
        ui_text(14, 358, "真彩（0xAARRGGBB）：", -6643536, 13, 0);
        let mut s = 0;
        while s < 8 {
            if s == 0 { ui_rect(14 + s * 40, 376, 36, 26, -16777216, 1, 0, 0); }
            if s == 1 { ui_rect(14 + s * 40, 376, 36, 26, -14671840, 1, 0, 0); }
            if s == 2 { ui_rect(14 + s * 40, 376, 36, 26, -12566464, 1, 0, 0); }
            if s == 3 { ui_rect(14 + s * 40, 376, 36, 26, -10066330, 1, 0, 0); }
            if s == 4 { ui_rect(14 + s * 40, 376, 36, 26, -8355712, 1, 0, 0); }
            if s == 5 { ui_rect(14 + s * 40, 376, 36, 26, -6250336, 1, 0, 0); }
            if s == 6 { ui_rect(14 + s * 40, 376, 36, 26, -4210753, 1, 0, 0); }
            if s == 7 { ui_rect(14 + s * 40, 376, 36, 26, -1, 1, 0, 0); }
            s = s + 1;
        }

        // ── ⑥ 动画 + 帧进度条（证明每帧确实在重画）─────────────
        let mut col = 14 + frames - (frames / 20) * 20;
        col = 14 + col * 10;
        if col > w - 46 { col = 14; }
        ui_circle(col, 448, 18, -63488, 1, 0);
        ui_rect(14, 480, (w - 28) * frames / 60, 10, -11409298, 1, 0, 4);

        // ── ⑦ 消息队列：帧数 / 最后一次按键 / 最后一次触摸 ──────
        ui_text(14, 506, "已画帧数 =", -6643536, 13, 0);
        ui_text(112, 506, int_to_str(frames), -6643536, 13, 0);
        ui_text(14, 528, "最后一次按键 =", -6643536, 13, 0);
        ui_text(140, 528, int_to_str(keys), -6643536, 13, 0);
        if touched == 1 {
            ui_text(14, 550, "最后一次触摸 =", -6643536, 13, 0);
            ui_text(140, 550, int_to_str(tx), -6643536, 13, 0);
            ui_text(190, 550, int_to_str(ty), -6643536, 13, 0);
            ui_circle(tx, ty, 26, -131246, 0, 3);
        } else {
            ui_text(14, 550, "最后一次触摸 = 无（点一下试试）", -6643536, 13, 0);
        }

        ui_text(w / 2, h - 24, "按任意键退出，或画满 60 帧自动退出", -6643536, 12, 1);

        // ── ⑧ 帧边界 ────────────────────────────────────────────
        ui_present();

        // ── ⑨ 收一条消息（最多等 30ms，保证循环有界）────────────
        let t = ui_wait_msg(30);
        if t == 10 { running = 0; }
        else if t == 1 { keys = ui_msg_a(); running = 0; }
        else if t == 6 { tx = ui_msg_a(); ty = ui_msg_b(); touched = 1; }
        else if t == 4 { tx = ui_msg_a(); ty = ui_msg_b(); touched = 1; }

        frames = frames + 1;
    }

    // ── 收尾：关定时器、放开屏幕常亮、关窗 ──────────────────────
    ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
}
