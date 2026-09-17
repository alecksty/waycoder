// 打砖块 —— 用 **Rust** 写的手机游戏
//
// 玩法：左右方向键移动底部挡板，把球弹进上方 6×4 的砖块阵；砖块清空即过关，
// 球落底即结束。每打掉一块 +10 分。比「接方块」多一层**数组遍历 + 矩形碰撞**，
// 刻意换玩法是为了让这份例程照到别的前端路径（下标写入、二维布局换算）。
//
// ◆ 手机那套 UI
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="rust" Libs="vmlui.vml">` 挂上来。
//
// ◆ 为什么全部逻辑都在 `main` 里
//
// Rust 这份没有用文件级数组：前端对模块级可变状态的支持没有把握，而骨架
// （`corpus/rust/skel.rs`）也只用了 `main` 内的局部数组。状态留在 `main` 内、
// 逻辑内联是绕开不确定性，不是风格选择。
//
// 已实测可用（2026-09-17）：`let mut a = [0,0,0,0]; a[i] = v;` 与读回都对
// （最小的 `a[0]=7; a[3]=5;` 求和得 12 ✓）—— `corpus/rust/skel.rs` 文件头记的
// 「下标左值会落回表达式语句」那条**现在不成立了**。
//
// ◆ 写法：颜色写负数十进制；循环更新写 `i = i + 1`。

fn main() {
    // 24 块砖（6 列 × 4 行），1 = 还在
    // ⚠ 数组字面量**必须写在一行** —— 跨行的 `];` 报「非法 token: SEMICOLON」（实测）
    let mut bricks = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
    let mut pad = 0;
    let mut bx = 0;
    let mut by = 0;
    let mut bdx = 0;
    let mut bdy = 0;
    let mut score = 0;
    let mut left = 24;
    let mut alive = 0;

    let mut w = ui_scr_w();
    let mut h = ui_scr_h();
    if w <= 0 { w = 360; }
    if h <= 0 { h = 620; }
    ui_win_open("打砖块", w, h);
    ui_keep_on(1);

    let bw = (w - 16) / 6;
    let bh = 20;
    let top = 60;

    pad = w / 2 - 45;
    bx = w / 2;
    by = h - 120;
    bdx = 4;
    bdy = 0 - 5;
    alive = 1;

    let mut tid = ui_timer_set(34, 0);

    while ui_win_closed() == 0 {
        // ── draw ──
        ui_clear(-15724520);
        ui_text(8, 8, "得分", -6643536, 13, 0);
        ui_rect(58, 11, score, 10, -11409298, 1, 0, 0);
        ui_text(w / 2, 8, "余砖", -6643536, 13, 1);
        ui_rect(w / 2 + 46, 11, left * 4, 10, -63488, 1, 0, 0);

        let mut i = 0;
        while i < 24 {
            if bricks[i] != 0 {
                let col = i - (i / 6) * 6;
                let row = i / 6;
                // 每行一色（越上越贵气一点）
                if row == 0 { ui_rect(8 + col * bw, top, bw - 3, bh - 3, -63488, 1, 0, 3); }
                if row == 1 { ui_rect(8 + col * bw, top + 24, bw - 3, bh - 3, -131246, 1, 0, 3); }
                if row == 2 { ui_rect(8 + col * bw, top + 48, bw - 3, bh - 3, -11409298, 1, 0, 3); }
                if row == 3 { ui_rect(8 + col * bw, top + 72, bw - 3, bh - 3, -15263713, 1, 0, 3); }
            }
            i = i + 1;
        }

        ui_rect(pad, h - 40, 90, 12, -63488, 1, 0, 6);
        ui_circle(bx, by, 8, -131246, 1, 0);
        if alive == 0 {
            ui_text(w / 2, h / 2, "按回车重开", -131246, 16, 1);
        }
        ui_present();

        let t = ui_wait_msg(0);
        if t == 10 { break; }

        if t == 9 {
            if alive != 0 {
                bx = bx + bdx;
                by = by + bdy;
                if bx < 10 { bx = 10; bdx = 0 - bdx; }
                if bx > w - 10 { bx = w - 10; bdx = 0 - bdx; }
                if by < 30 { by = 30; bdy = 0 - bdy; }

                // 挡板：球落到挡板带上、横向在挡板范围内 → 弹回
                if by > h - 52 {
                    if by < h - 28 {
                        if bx > pad - 8 {
                            if bx < pad + 98 {
                                bdy = 0 - bdy;
                                by = h - 52;
                            }
                        }
                    }
                }

                // 砖块碰撞：命中就消一块、弹回、加分
                i = 0;
                while i < 24 {
                    if bricks[i] != 0 {
                        let col = i - (i / 6) * 6;
                        let row = i / 6;
                        let rx = 8 + col * bw;
                        let ry = top + row * 24;
                        if bx > rx - 8 {
                            if bx < rx + bw + 8 {
                                if by > ry - 8 {
                                    if by < ry + bh + 8 {
                                        bricks[i] = 0;
                                        left = left - 1;
                                        score = score + 10;
                                        bdy = 0 - bdy;
                                        ui_beep(1046, 25);
                                    }
                                }
                            }
                        }
                    }
                    i = i + 1;
                }

                if left == 0 {
                    alive = 0;
                    ui_beep(1568, 200);
                    ui_present();
                    if (ui_dlg_msg("打砖块", "全清了！这一局结束。\n再来一局？（选「否」退出）", 0)) != 0 { ui_win_close(); break; }
                    i = 0;
                    while i < 24 { bricks[i] = 1; i = i + 1; }
                    left = 24;
                    score = 0;
                    pad = w / 2 - 45;
                    bx = w / 2;
                    by = h - 120;
                    bdx = 4;
                    bdy = 0 - 5;
                    alive = 1;
                }

                if by > h {
                    alive = 0;
                    ui_beep(220, 260);
                    ui_present();
                    if (ui_dlg_msg("打砖块", "球落底了，这一局结束。\n再来一局？（选「否」退出）", 0)) != 0 { ui_win_close(); break; }
                    i = 0;
                    while i < 24 { bricks[i] = 1; i = i + 1; }
                    left = 24;
                    score = 0;
                    pad = w / 2 - 45;
                    bx = w / 2;
                    by = h - 120;
                    bdx = 4;
                    bdy = 0 - 5;
                    alive = 1;
                }
            }
        }

        if t == 1 {
            let k = ui_msg_a();
            if k == 27 { break; }
            if k == 37 {
                pad = pad - 24;
                if pad < 4 { pad = 4; }
            }
            if k == 39 {
                pad = pad + 24;
                if pad > w - 94 { pad = w - 94; }
            }
            if k == 13 {
                i = 0;
                while i < 24 { bricks[i] = 1; i = i + 1; }
                left = 24;
                score = 0;
                pad = w / 2 - 45;
                bx = w / 2;
                by = h - 120;
                bdx = 4;
                bdy = 0 - 5;
                alive = 1;
            }
        }
    }

    ui_timer_kill(tid);
    ui_keep_on(0);
    ui_win_close();
}
