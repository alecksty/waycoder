// 飞机空战 —— 用 **Swift** 写的手机游戏
//
// ✅ 能玩。判据是**逐像素量**出来的：本机（青色）随方向键左右移动（左右各 -16px、上下各 -10px）、
//    子弹（黄色）匀速上行、敌机（红色）下行、打中后敌机消失且分数条变长、
//    **漏掉 3 架就结束**（右上角三架小飞机逐架灭掉）并弹对话框重开。
//
// ◆ 玩法
//
// 星空滚动，本机在下方，**自动开火**（手机上按住方向键就够忙的，不必再按射击）。
// 方向键左右移动（上下也能动，只是范围小）。SELECT 暂停，回车重开，ESC 或返回箭头退出。
// 打下一架 +10 分并略微提速；**放跑一架扣一条命**（共 3 条），用完就结束。
//
// ⚠ 为什么是"扣命"而不是"撞机才死"：第一版写的是撞机才结束，实测 **500 拍一次都没输过** ——
//    子弹是沿本机那一列往上的，本机不动就是一个"子弹墙"，那一列上的敌机在顶端就被打掉了，
//    永远走不到本机跟前。于是游戏既输不了、也很难得分。改成"放跑就扣命"之后，
//    玩家必须**横向跑位去拦**每一架，才成为一个真正的游戏（实测 500 拍内 3 局、最高 30 分）。
//
// ◆ 踩过的两个坑（都记在文件头，免得下次再踩）
//
// ① **只能有一个全局数组**（`docs/前端游戏能力评估.md` §4.5）。实测同一程序里开
//    `var A / var B / var C` 三个全局数组会**互相踩内存** —— 蛇那次的表现是"只画出一节、
//    HUD 文字错位"。所以这里所有状态都塞进一个 `A`，靠下标分区（见下面那张表）。
// ② **带括号的表达式曾经恒等于 0**（`(k) * 2` 编成 `move R0 #0`，patch 0011）——
//    症状是"算式下标不能复用"。已修，但写的时候仍然只用**最朴素的算式下标**。
//
// ◆ 为什么是 Swift
//
// 22 个前端逐个过筛（编译出图 / 循环 / 数组 / 函数 / 参数传递）之后，Swift 是干净通过的那一门：
// 直接 `call` 到包装标签、实参逆序压栈、循环分支数组读写带参函数返回值、定时器心跳，
// 而且**字符串拼接可用**（`"a" + "b"`、`numToStr`），所以这里的分数能直接写出数字。
//
// 数值的唯一真源仍是 `Lib/c/waycoder_ui.h`。

// 状态表（只用一个数组，见文件头坑①）
//   0=px 1=py 2=score 3=best 4=alive 5=paused 6=speed 7=tickMs
//   8=starOff 9=sw 10=sh 11=fireCd 12=hits 13=wave
//   20..23=子弹 x，24..27=子弹 y，28..31=子弹在飞
//   40..43=敌机 x，44..47=敌机 y，48..51=敌机在飞
//   60..67=星星 x，68..75=星星 y
var A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

func colorSky() -> Int { return -15986144 }      // 0xFF0C1220
func colorStar() -> Int { return -14011056 }     // 0xFF2A3550
func colorShip() -> Int { return -12922369 }     // 0xFF3AD1FF
func colorBullet() -> Int { return -8090 }       // 0xFFFFE066
func colorEnemy() -> Int { return -50384 }       // 0xFFFF3B30
func colorHud() -> Int { return -6643536 }       // 0xFF9AA0B0
func colorBar() -> Int { return -11409298 }      // 0xFF51E86E
func colorBar2() -> Int { return -63488 }        // 0xFFFF0800
func colorWhite() -> Int { return -1 }

func msgKeydown() -> Int { return 1 }
func msgTimer() -> Int { return 9 }
func msgClose() -> Int { return 10 }

func keyEnter() -> Int { return 13 }
func keySelect() -> Int { return 16 }
func keyEsc() -> Int { return 27 }
func keyLeft() -> Int { return 37 }
func keyUp() -> Int { return 38 }
func keyRight() -> Int { return 39 }
func keyDown() -> Int { return 40 }

func anchorLeft() -> Int { return 0 }
func anchorCenter() -> Int { return 1 }
func anchorRight() -> Int { return 2 }

// 版面常量（都是算出来的，不在多处各写一遍）
func shipW() -> Int { return 34 }
func shipH() -> Int { return 26 }
func bulW() -> Int { return 3 }
func bulH() -> Int { return 10 }
func foeSlots() -> Int { return 6 }     // 敌机槽位：必须 > 一次下落期间的刷新次数，否则会刷满后停摆
func foeW() -> Int { return 28 }
func foeH() -> Int { return 22 }
func topBar() -> Int { return 46 }        // HUD 之下才是活动区

func shipY() -> Int { return A[10] - 56 }

// 位图数字（Swift 的字符串拼接可用，但这里只拼数字，标点保持列距一致）
func numToStr(v: Int) -> String {
    if v == 0 { return "0" }
    var s = ""
    var n = v
    if n < 0 { n = 0 - n }
    while n > 0 {
        var d = n % 10
        if d == 0 { s = "0" + s }
        else if d == 1 { s = "1" + s }
        else if d == 2 { s = "2" + s }
        else if d == 3 { s = "3" + s }
        else if d == 4 { s = "4" + s }
        else if d == 5 { s = "5" + s }
        else if d == 6 { s = "6" + s }
        else if d == 7 { s = "7" + s }
        else if d == 8 { s = "8" + s }
        else { s = "9" + s }
        n = n / 10
    }
    if v < 0 { s = "-" + s }
    return s
}

// 打中一架：分数 +10，每 50 分提一档速度
func addScore() {
    A[2] = A[2] + 10
    if A[2] > A[3] { A[3] = A[2] }
    A[12] = A[12] + 1
    if A[12] % 5 == 0 {
        if A[6] < 14 { A[6] = A[6] + 1 }
        if A[7] > 34 { A[7] = A[7] - 4 }
    }
}

func resetStars() {
    var i = 0
    while i < 8 {
        A[60 + i] = ui_rand(A[9])
        A[68 + i] = ui_rand(A[10] - topBar()) + topBar()
        i = i + 1
    }
}

func reset() {
    A[0] = A[9] / 2
    A[1] = shipY()
    A[2] = 0
    A[4] = 1
    A[5] = 0
    A[6] = 8
    A[7] = 46
    A[8] = 0
    A[11] = 0
    A[12] = 0
    A[13] = 0
    A[14] = 3
    var i = 0
    while i < 4 {
        A[28 + i] = 0
        i = i + 1
    }
    i = 0
    while i < foeSlots() {
        A[48 + i] = 0
        i = i + 1
    }
    resetStars()
}

// 找空槽发射一颗子弹（自动开火，手机上不必再按射击）
func fire() {
    var i = 0
    while i < 4 {
        if A[28 + i] == 0 {
            A[20 + i] = A[0]
            A[24 + i] = A[1] - 6
            A[28 + i] = 1
            ui_beep(1500, 12)
            return
        }
        i = i + 1
    }
}

func spawnFoe() {
    var i = 0
    while i < foeSlots() {
        if A[48 + i] == 0 {
            A[40 + i] = ui_rand(A[9] - 60) + 30
            A[44 + i] = topBar() - foeH()
            A[48 + i] = 1
            return
        }
        i = i + 1
    }
}

func hitFoe(slot: Int) {
    A[48 + slot] = 0
    addScore()
    ui_beep(1900, 18)
}

func drawShip() {
    var x = A[0]
    var y = A[1]
    // 机身
    ui_rect(x - 5, y - shipH() / 2, 10, shipH(), colorShip(), 1, 0, 3)
    // 机翼
    ui_rect(x - shipW() / 2, y, shipW(), 7, colorShip(), 1, 0, 2)
    // 尾焰（每拍换一点，看起来在喷）
    var f = A[8] % 2
    if f == 0 { ui_rect(x - 2, y + shipH() / 2, 4, 6, colorBullet(), 1, 0, 0) }
    else { ui_rect(x - 2, y + shipH() / 2, 4, 9, colorBullet(), 1, 0, 0) }
}

func drawFoe(slot: Int) {
    var x = A[40 + slot]
    var y = A[44 + slot]
    ui_rect(x + 6, y, foeW() - 12, foeH(), colorEnemy(), 1, 0, 3)
    ui_rect(x, y + 6, foeW(), 7, colorEnemy(), 1, 0, 2)
    ui_rect(x + 10, y + 5, 8, 5, colorSky(), 1, 0, 1)
}

func draw() {
    ui_clear(colorSky())

    // 星空：8 颗，每拍按自己的速度下移，出底就回到顶
    var i = 0
    while i < 8 {
        var sy = A[68 + i] + 2 + i % 3
        if sy > A[10] - 4 { sy = topBar() }
        A[68 + i] = sy
        ui_rect(A[60 + i], sy, 3, 3, colorStar(), 1, 0, 0)
        i = i + 1
    }

    // 子弹
    i = 0
    while i < 4 {
        if A[28 + i] != 0 { ui_rect(A[20 + i] - 1, A[24 + i], bulW(), bulH(), colorBullet(), 1, 0, 1) }
        i = i + 1
    }

    // 敌机
    i = 0
    while i < foeSlots() {
        if A[48 + i] != 0 { drawFoe(i) }
        i = i + 1
    }

    drawShip()

    // HUD：分数条 / 最高分条 + 右上角写数字（Swift 拼字符串是好的）
    ui_text(8, 8, "得分", colorHud(), 13, anchorLeft())
    var bar = A[2]
    if bar > 160 { bar = 160 }
    ui_rect(58, 11, bar, 10, colorBar(), 1, 0, 0)
    ui_text(A[9] / 2, 8, "最高", colorHud(), 13, anchorCenter())
    var bar2 = A[3]
    if bar2 > 160 { bar2 = 160 }
    ui_rect(A[9] / 2 + 46, 11, bar2, 10, colorBar2(), 1, 0, 0)
    ui_text(A[9] - 8, 8, numToStr(A[2]), colorWhite(), 14, anchorRight())
    // 剩余命数：右上角画几架小飞机（不是数字 —— 一眼看得出还剩几条）
    var lf = 0
    while lf < A[14] {
        ui_rect(A[9] - 24 - lf * 16, 26, 10, 8, colorShip(), 1, 0, 2)
        lf = lf + 1
    }

    if A[5] != 0 { ui_text(A[9] / 2, A[10] / 2, "暂停（SELECT 继续）", colorEnemy(), 16, anchorCenter()) }
    ui_present()
}

func overlap(ax: Int, ay: Int, aw: Int, ah: Int, bx: Int, by: Int, bw: Int, bh: Int) -> Int {
    if ax + aw < bx { return 0 }
    if bx + bw < ax { return 0 }
    if ay + ah < by { return 0 }
    if by + bh < ay { return 0 }
    return 1
}

func gameOver() {
    A[4] = 0
    ui_beep(200, 320)
    draw()
    ui_dlg_msg("飞机空战", "被撞到了，这一局结束。\n再来一局？（选「否」退出）", 0)
    reset()
}

// 返回 0 表示本拍无变化、不必重画
func step() -> Int {
    if A[4] == 0 { return 0 }
    if A[5] != 0 { return 0 }

    A[8] = A[8] + 1

    // 自动开火
    A[11] = A[11] - 1
    if A[11] <= 0 {
        fire()
        A[11] = 5 - A[6] / 3
        if A[11] < 2 { A[11] = 2 }
    }

    // 子弹上行
    var i = 0
    while i < 4 {
        if A[28 + i] != 0 {
            A[24 + i] = A[24 + i] - 14
            if A[24 + i] < topBar() - 10 { A[28 + i] = 0 }
        }
        i = i + 1
    }

    // 敌机下行
    i = 0
    while i < foeSlots() {
        if A[48 + i] != 0 { A[44 + i] = A[44 + i] + A[6] }
        i = i + 1
    }

    // 刷敌机
    A[13] = A[13] + 1
    if A[13] >= 22 - A[6] {
        A[13] = 0
        spawnFoe()
    }

    // 子弹 × 敌机
    var b = 0
    while b < 4 {
        if A[28 + b] != 0 {
            var e = 0
            while e < foeSlots() {
                if A[48 + e] != 0 {
                    if overlap(A[20 + b] - 1, A[24 + b], bulW(), bulH(), A[40 + e], A[44 + e], foeW(), foeH()) != 0 {
                        A[28 + b] = 0
                        hitFoe(e)
                        e = foeSlots()
                    }
                }
                e = e + 1
            }
        }
        b = b + 1
    }

    // 敌机 × 本机 / 敌机出底
    // ⚠ 这一圈必须走 foeSlots()：写成 4 的话，第 5、6 号槽的敌机**永远不被清、也永远不扣命**
    //   —— 它们掉出屏幕后槽位一直占着，刷到第 3 架就再也没有敌机了（静默地"游戏自己停了"）。
    i = 0
    while i < foeSlots() {
        if A[48 + i] != 0 {
            if overlap(A[0] - shipW() / 2, A[1] - shipH() / 2, shipW(), shipH(), A[40 + i], A[44 + i], foeW(), foeH()) != 0 {
                gameOver()
                return 1
            }
            if A[44 + i] > A[10] - 20 {
                A[48 + i] = 0
                A[14] = A[14] - 1
                ui_beep(320, 60)
                if A[14] <= 0 {
                    gameOver()
                    return 1
                }
            }
        }
        i = i + 1
    }

    return 1
}

func main() {
    A[9] = ui_scr_w()
    A[10] = ui_scr_h()
    if A[9] <= 0 { A[9] = 360 }
    if A[10] <= 0 { A[10] = 620 }
    ui_win_open("飞机空战", A[9], A[10])
    ui_keep_on(1)
    reset()
    draw()

    var tid = ui_timer_set(A[7], 0)
    var curMs = A[7]

    while ui_win_closed() == 0 {
        var t = ui_wait_msg(0)
        if t == msgClose() { break }
        if t == msgTimer() {
            if step() != 0 { draw() }
            // 提速后要把定时器换掉（重复定时器的间隔是建立时定死的）
            if A[7] != curMs {
                ui_timer_kill(tid)
                curMs = A[7]
                tid = ui_timer_set(curMs, 0)
            }
        }
        else if t == msgKeydown() {
            var k = ui_msg_a()
            if k == keyEsc() { break }
            else if k == keyLeft() { if A[0] > 24 { A[0] = A[0] - 16 } }
            else if k == keyRight() { if A[0] < A[9] - 24 { A[0] = A[0] + 16 } }
            else if k == keyUp() { if A[1] > A[10] - 150 { A[1] = A[1] - 10 } }
            else if k == keyDown() { if A[1] < shipY() + 40 { A[1] = A[1] + 10 } }
            else if k == keyEnter() { reset() }
            else if k == keySelect() { if A[5] != 0 { A[5] = 0 } else { A[5] = 1 } }
        }
        // 每次收到按键就先画一帧（手机上按方向键要立刻看到飞机动）
        if t == msgKeydown() { draw() }
    }

    ui_timer_kill(tid)
    ui_keep_on(0)
    ui_win_close()
}
