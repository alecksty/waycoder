// 接方块 —— 用 **Kotlin** 写的手机游戏
//
// 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
//
// ◆ 手机那套 UI
//
// 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// 由 `vmltool.config.xml` 的 `<Language Name="kotlin" Libs="vmlui.vml">` 挂上来。
//
// ◆ ⚠ 一条本份例程实测出来的前端缺陷：**文件级 `arrayOf` 读回 0**
//
// 两个最小对照（2026-09-17）：
//     var A = arrayOf(0,1,2,3,4)          // 文件级
//     fun main() { A[0] = 7; print(A[0]+A[3]) }   → 0   ✗
//
//     fun main() { var B = arrayOf(0,1,2,3,4)     // main 内局部
//                  B[0] = 7; print(B[0]+B[3]) }   → 10  ✓
//
// 所以**状态数组必须留在 `main` 内**、逻辑不能拆进别的函数（拆出去就访问不到它）。
// 本份例程因此把 draw / tick 全部内联在 main 的循环里 —— 不是风格选择，是绕这个缺陷。
// （`corpus/kotlin/skel.kt` 照不出来，正因为它的数组恰好是 `main` 内的局部 ——
//   「骨架绿证明不了」这是第五次。）
//
// ◆ 另一条：`step` 是 Kotlin 前端的**保留字**（`Expected function name ... got KEYWORD 'step'`），
//   所以计时器那一拍叫 `tick` 不叫 `step`。
//
// ◆ 其余写法：调库函数不用声明（对不认识的函数名发裸标签 CALL、实参右到左）；
//   颜色写负数十进制；打印用 print_str（不换行）+ println_int（含换行）。

fun main() {
    // 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高
    var A = arrayOf(0, 0, 0, 0, 0, 0, 0, 0, 0, 0)

    var w = ui_scr_w()
    var h = ui_scr_h()
    if (w <= 0) { w = 360 }
    if (h <= 0) { h = 620 }
    A[8] = w
    A[9] = h
    ui_win_open("接方块", w, h)
    ui_keep_on(1)

    A[0] = w / 2 - 40
    A[1] = w / 2
    A[2] = 70
    A[3] = 3
    A[4] = 5
    A[5] = 0
    A[6] = 0
    A[7] = 1

    var tid = ui_timer_set(40, 0)

    while (ui_win_closed() == 0) {
        // ── draw ──
        ui_clear(-15724520)
        ui_text(8, 8, "得分", -6643536, 13, 0)
        ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0)
        ui_text(A[8] / 2, 8, "最高", -6643536, 13, 1)
        ui_rect(A[8] / 2 + 46, 11, A[6], 10, -63488, 1, 0, 0)
        ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6)
        ui_circle(A[1], A[2], 9, -131246, 1, 0)
        if (A[7] == 0) {
            ui_text(A[8] / 2, A[9] / 2, "按回车重开", -131246, 16, 1)
        }
        ui_present()

        var t = ui_wait_msg(0)
        if (t == 10) { break }

        if (t == 9) {
            if (A[7] != 0) {
                A[1] = A[1] + A[3]
                A[2] = A[2] + A[4]
                if (A[1] < 10) { A[1] = 10; A[3] = 0 - A[3] }
                if (A[1] > A[8] - 10) { A[1] = A[8] - 10; A[3] = 0 - A[3] }
                if (A[2] < 30) { A[2] = 30; A[4] = 0 - A[4] }
                if (A[2] > A[9] - 52 && A[2] < A[9] - 30 && A[1] > A[0] - 9 && A[1] < A[0] + 89) {
                    A[4] = 0 - A[4]
                    A[2] = A[9] - 52
                    A[5] = A[5] + 10
                    if (A[5] > A[6]) { A[6] = A[5] }
                    ui_beep(880, 30)
                }
                if (A[2] > A[9]) {
                    A[7] = 0
                    ui_beep(220, 260)
                    if (ui_dlg_msg("接方块", "没接住，这一局结束。\n再来一局？（选「否」退出）", 0) != 0) { ui_win_close(); break }
                    A[0] = A[8] / 2 - 40
                    A[1] = A[8] / 2
                    A[2] = 70
                    A[3] = 3
                    A[4] = 5
                    A[5] = 0
                    A[7] = 1
                }
            }
        }

        if (t == 1) {
            var k = ui_msg_a()
            if (k == 27) { break }
            if (k == 37) { A[0] = A[0] - 20; if (A[0] < 4) { A[0] = 4 } }
            if (k == 39) { A[0] = A[0] + 20; if (A[0] > A[8] - 84) { A[0] = A[8] - 84 } }
            if (k == 13) {
                A[0] = A[8] / 2 - 40
                A[1] = A[8] / 2
                A[2] = 70
                A[3] = 3
                A[4] = 5
                A[5] = 0
                A[7] = 1
            }
        }
    }

    ui_timer_kill(tid)
    ui_keep_on(0)
    ui_win_close()
}
