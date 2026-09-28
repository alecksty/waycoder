// 打砖块 —— 用 **Rust** 写的手机游戏
// Breakout -- a mobile game written in **Rust**
//
// 玩法：左右方向键移动底部挡板，把球弹进上方 6×4 的砖块阵；砖块清空即过关，
// Gameplay: the left/right arrow keys move the paddle at the bottom, bouncing the ball up into the 6x4 brick grid;
// 球落底即结束。每打掉一块 +10 分。比「接方块」多一层**数组遍历 + 矩形碰撞**，
// the round ends when the ball hits the bottom. Each brick destroyed = +10 points. One layer beyond "catch the blocks": **array traversal + rectangle collision**;
// 刻意换玩法是为了让这份例程照到别的前端路径（下标写入、二维布局换算）。
// the deliberately different gameplay is so this example also exercises other frontend paths (subscript writes, 2-D layout arithmetic).
//
// ◆ 手机那套 UI
// ◆ The mobile UI set
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// Opening the window / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
// 由 `vmltool.config.xml` 的 `<Language Name="rust" Libs="vmlui.vml">` 挂上来。
// pulled in by `<Language Name="rust" Libs="vmlui.vml">` in `vmltool.config.xml`.
//
// ◆ 为什么全部逻辑都在 `main` 里
// ◆ Why all the logic lives in `main`
//
// Rust 这份没有用文件级数组：前端对模块级可变状态的支持没有把握，而骨架
// This Rust version uses no file-level arrays: the frontend's support for module-level mutable state was uncertain, and the skeleton
// （`corpus/rust/skel.rs`）也只用了 `main` 内的局部数组。状态留在 `main` 内、
// (`corpus/rust/skel.rs`) also only used local arrays inside `main`. Keeping the state inside `main` and
// 逻辑内联是绕开不确定性，不是风格选择。
// inlining the logic avoids that uncertainty; it is not a style choice.
//
// 已实测可用（2026-09-17）：`let mut a = [0,0,0,0]; a[i] = v;` 与读回都对
// Measured working (2026-09-17): `let mut a = [0,0,0,0]; a[i] = v;` and reading it back are both correct
// （最小的 `a[0]=7; a[3]=5;` 求和得 12 ✓）—— `corpus/rust/skel.rs` 文件头记的
// (the minimal `a[0]=7; a[3]=5;` sums to 12 OK) -- the note in the `corpus/rust/skel.rs` file header
// 「下标左值会落回表达式语句」那条**现在不成立了**。
// that "a subscript lvalue falls back to an expression statement" **no longer holds**.
//
// ◆ 写法：颜色写负数十进制；循环更新写 `i = i + 1`。
// ◆ Style: write colors as negative decimals; write the loop update as `i = i + 1`.

fn main() {
    // 24 块砖（6 列 × 4 行），1 = 还在
    // 24 bricks (6 columns x 4 rows), 1 = still there
    // ⚠ 数组字面量**必须写在一行** —— 跨行的 `];` 报「非法 token: SEMICOLON」（实测）
    // ⚠ The array literal **must be written on one line** -- a line-broken `];` reports "invalid token: SEMICOLON" (measured)
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

    // 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），之后整局按它分支。
    // UI language: ask the host once at the start whether Chinese or English (0=Chinese 1=English), then branch on it for the whole round.
    // ⚠ 别在每帧里调 —— 那是一次 syscall。`if` 表达式（本前端支持，见语言规范 §条件语句）
    // ⚠ Do not call it in every frame -- that is a syscall. The `if` expression (supported by this frontend, see the language spec section on conditionals)
    //   在这里比 `let mut` + `if` 语句短，且分支文本与条件写在同一行。
    //   is shorter here than `let mut` + an `if` statement, and keeps the branch text on the same line as the condition.
    let lang = ui_get_language();
    let t_title = if lang == 0 { "打砖块" } else { "Breakout" };
    let t_score = if lang == 0 { "得分" } else { "Score" };
    let t_left = if lang == 0 { "余砖" } else { "Bricks left" };
    let t_restart = if lang == 0 { "按回车重开" } else { "Press Enter to restart" };
    let t_cleared = if lang == 0 { "全清了！这一局结束。\n再来一局？（选「否」退出）" } else { "Cleared! Round over.\nPlay again? (choose \"No\" to quit)" };
    let t_fell = if lang == 0 { "球落底了，这一局结束。\n再来一局？（选「否」退出）" } else { "Ball fell. Round over.\nPlay again? (choose \"No\" to quit)" };

    ui_win_open(t_title, w, h);
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
        ui_text(8, 8, t_score, -6643536, 13, 0);
        ui_rect(58, 11, score, 10, -11409298, 1, 0, 0);
        ui_text(w / 2, 8, t_left, -6643536, 13, 1);
        ui_rect(w / 2 + 46, 11, left * 4, 10, -63488, 1, 0, 0);

        let mut i = 0;
        while i < 24 {
            if bricks[i] != 0 {
                let col = i - (i / 6) * 6;
                let row = i / 6;
                // 每行一色（越上越贵气一点）
                // One color per row (the higher up, the fancier)
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
            ui_text(w / 2, h / 2, t_restart, -131246, 16, 1);
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
                // Paddle: the ball lands on the paddle band and is horizontally within the paddle -> bounce back
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
                // Brick collision: on a hit, remove one brick, bounce back, add points
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
                                        // 打砖得分：一声高而短的「叮」
                                        // Brick hit score: one high, short "ding"
                                        //   （单音 ui_beep —— v0.96.509 从音序器换回来，
                                        //   (single-tone ui_beep -- switched back from the sequencer in v0.96.509,
                                        //    那一版多声部叠加 / 长音在真机上破音）
                                        //    that version's multi-voice layering / long notes crackled on the real device)
                                        ui_beep(1047, 66);
                                    }
                                }
                            }
                        }
                    }
                    i = i + 1;
                }

                if left == 0 {
                    alive = 0;
                    // 过关（赢方）：**最高音**、最长
                    // Round cleared (winner): **highest pitch**, longest
                    ui_beep(1047, 320);
                    ui_present();
                    if (ui_dlg_msg(t_title, t_cleared, 0)) != 0 { ui_win_close(); break; }
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
                    // 死（输方）：「最低音」、最长 —— 与过关那条 1047 正好是两端
                    // Death (loser): "lowest pitch", longest -- exactly the opposite end from the 1047 of the round-cleared one
                    ui_beep(131, 320);
                    ui_present();
                    if (ui_dlg_msg(t_title, t_fell, 0)) != 0 { ui_win_close(); break; }
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
