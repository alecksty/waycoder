// 飞机空战 —— 用 **Swift** 写的手机游戏
// Plane combat — a phone game written in **Swift**
//
// ✅ 能玩。判据是**逐像素量**出来的：本机（青色）随方向键左右移动（左右各 -16px、上下各 -10px）、
// ✅ Playable. The criteria were **measured pixel by pixel**: the player plane (cyan) moves left/right with the arrow keys (16px each way horizontally, 10px vertically),
//    子弹（黄色）匀速上行、敌机（红色）下行、打中后敌机消失且分数条变长、
//    bullets (yellow) travel upward at a constant speed, enemy planes (red) come down, a hit removes the enemy and lengthens the score bar,
//    **漏掉 3 架就结束**（右上角三架小飞机逐架灭掉）并弹对话框重开。
//    and **missing 3 planes ends the game** (the three little planes in the top-right go out one by one) with a dialog to restart.
//
// ◆ 玩法
// ◆ Gameplay
//
// 星空滚动，本机在下方，**自动开火**（手机上按住方向键就够忙的，不必再按射击）。
// A starfield scrolls, the player plane sits at the bottom, and **firing is automatic** (holding the arrow keys on a phone is busy enough without also pressing fire).
// 方向键左右移动（上下也能动，只是范围小）。SELECT 暂停，回车重开，ESC 或返回箭头退出。
// Arrow keys move left/right (up/down work too, just with a smaller range). SELECT pauses, Enter restarts, ESC or the back arrow exits.
// 打下一架 +10 分并略微提速；**放跑一架扣一条命**（共 3 条），用完就结束。
// Shooting one down is +10 points and a slight speed-up; **letting one escape costs a life** (3 in total), and the game ends when they run out.
//
// ⚠ 为什么是"扣命"而不是"撞机才死"：第一版写的是撞机才结束，实测 **500 拍一次都没输过** ——
// ⚠ Why "lose a life" instead of "die on collision": the first version ended only on a crash, and measured over **500 ticks it never lost once** —
//    子弹是沿本机那一列往上的，本机不动就是一个"子弹墙"，那一列上的敌机在顶端就被打掉了，
//    bullets travel up the player's own column, so a stationary player is a "wall of bullets" and enemy planes in that column get shot down at the very top,
//    永远走不到本机跟前。于是游戏既输不了、也很难得分。改成"放跑就扣命"之后，
//    never reaching the player. The game could neither be lost nor scored well. After switching to "escape costs a life",
//    玩家必须**横向跑位去拦**每一架，才成为一个真正的游戏（实测 500 拍内 3 局、最高 40 分）。
//    the player must **move sideways to intercept** every plane, which is what makes it a real game (measured: 3 rounds within 500 ticks, best score 40).
//
// ⚠ 另有一处**只有写游戏才会踩到**的：敌机槽位定成 6 个之后，"敌机 × 本机 / 敌机出底"
// ⚠ There is another one you **only step on when writing a game**: after the enemy slot count was set to 6, the "enemy × player / enemy off the bottom"
//    那一圈忘了跟着从 `4` 改成 `foeSlots()` ⇒ 第 5、6 号槽的敌机**永不回收、也永不扣命**，
//    loop was not changed from `4` to `foeSlots()` too ⇒ the planes in slots 5 and 6 were **never recycled and never cost a life**,
//    槽位占满之后**再也没有敌机出现**（静默地"游戏自己停了"：画面还在动、分数不涨）。
//    and once the slots were full **no more enemy planes appeared** (a silent "the game stopped by itself": the picture still moved but the score stopped rising).
//    **凡是"槽位数"这类常量，定下来之后要 grep 一遍所有遍历它的循环。**
//    **Whenever a constant like "slot count" is fixed, grep for every loop that iterates over it.**
//
// ◆ 踩过的两个坑（都记在文件头，免得下次再踩）
// ◆ The two pitfalls hit (both recorded in the file header, so they are not hit again)
//
// ① **只能有一个全局数组**（`docs/前端游戏能力评估.md` §4.5）。实测同一程序里开
// ① **There can be only one global array** (frontend game-capability assessment doc, §4.5). Measured: opening
//    `var A / var B / var C` 三个全局数组会**互相踩内存** —— 蛇那次的表现是"只画出一节、
//    three global arrays `var A / var B / var C` in one program makes them **clobber each other's memory** — in the Snake case it showed as "only one segment drawn,
//    HUD 文字错位"。所以这里所有状态都塞进一个 `A`，靠下标分区（见下面那张表）。
//    HUD text misaligned". So all state here is packed into a single `A`, partitioned by index (see the table below).
// ② **带括号的表达式曾经恒等于 0**（`(k) * 2` 编成 `move R0 #0`，patch 0011）——
// ② **Parenthesized expressions used to be identically 0** (`(k) * 2` compiled to `move R0 #0`, patch 0011) —
//    症状是"算式下标不能复用"。已修，但写的时候仍然只用**最朴素的算式下标**。
//    the symptom was "computed indices cannot be reused". Fixed, but this still writes only the **plainest computed indices**.
//
// ◆ 为什么是 Swift
// ◆ Why Swift
//
// 22 个前端逐个过筛（编译出图 / 循环 / 数组 / 函数 / 参数传递）之后，Swift 是**最干净**的那一门：
// After screening all 22 frontends one by one (compiles and draws / loops / arrays / functions / argument passing), Swift is the **cleanest** one:
// 直接 `call` 到包装标签、实参逆序压栈、循环分支数组读写带参函数返回值、定时器心跳，
// it `call`s the wrapper labels directly, pushes arguments in reverse order, handles loops, branches, array reads/writes and functions with arguments and return values, and the timer heartbeat,
// 而且**字符串拼接可用**（`"a" + "b"`、`numToStr`），所以这里的分数能直接写出数字。
// and **string concatenation works** (`"a" + "b"`, `numToStr`), so the score here can be written out as digits directly.
//
// ⚠ 它也不是全须全尾：**第 5 个形参起，取到的全是第 1 个形参的值**（v0.96.195 修，patch 0011）——
// ⚠ It is not flawless either: **from the 5th parameter on, every one reads back the value of the 1st** (fixed in v0.96.195, patch 0011) —
//   8 参的 `overlap` 因此恒判"相交"，敌机一冒头就被打掉。写多参函数之前值得知道这一条。
//   so the 8-argument `overlap` always judged "intersecting" and enemy planes were shot down the moment they appeared. Worth knowing before writing multi-argument functions.
//
// 数值的唯一真源仍是 `Lib/c/waycoder_ui.h`。
// The single source of truth for these numbers is still `Lib/c/waycoder_ui.h`.

// 状态表（只用一个数组，见文件头坑①）
//   0=px 1=py 2=score 3=best 4=alive 5=paused 6=speed 7=tickMs
//   8=starOff 9=sw 10=sh 11=fireCd 12=hits 13=wave
//   20..23=子弹 x，24..27=子弹 y，28..31=子弹在飞
//   20..23=bullet x, 24..27=bullet y, 28..31=bullet in flight
//   40..43=敌机 x，44..47=敌机 y，48..51=敌机在飞
//   40..43=enemy x, 44..47=enemy y, 48..51=enemy in flight
//   60..67=星星 x，68..75=星星 y
//   60..67=star x, 68..75=star y
//   110=界面语言（0=中文 1=英文）—— 数组刻意留到 112 个，就是给这个槽用的
//   110=UI language (0=Chinese 1=English) — the array is deliberately sized to 112 just for this slot
//   （按文件头 ① 的实测，语言码**不能**用模块级标量存：读出来是垃圾）
//   (per the measurement in header ①, the language code **cannot** be stored in a module-level scalar: it reads back as garbage)
var A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]

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
// Layout constants (all derived, not written out in several places)
func shipW() -> Int { return 34 }
func shipH() -> Int { return 26 }
func bulW() -> Int { return 3 }
func bulH() -> Int { return 10 }
func foeSlots() -> Int { return 6 }     // 敌机槽位：必须 > 一次下落期间的刷新次数，否则会刷满后停摆
// enemy slots: must exceed the number of spawns during one descent, otherwise the slots fill up and it stalls
func foeW() -> Int { return 28 }
func foeH() -> Int { return 22 }
func topBar() -> Int { return 46 }        // HUD 之下才是活动区
// the play area starts below the HUD

func shipY() -> Int { return A[10] - 56 }

// 位图数字（Swift 的字符串拼接可用，但这里只拼数字，标点保持列距一致）
// Bitmap digits (Swift's string concatenation works, but only digits are concatenated here; punctuation keeps the column spacing consistent)
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
// Shot down one: score +10, and one speed step every 50 points
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
// Find a free slot and fire a bullet (automatic fire, no need to press shoot on a phone)
func fire() {
    var i = 0
    while i < 4 {
        if A[28 + i] == 0 {
            A[20 + i] = A[0]
            A[24 + i] = A[1] - 6
            A[28 + i] = 1
            ui_beep(2093, 33)   // 射击：最轻最短（单音 ui_beep，v0.96.509 从音序器换回来）
            // shoot: the lightest and shortest (single-tone ui_beep, switched back from the sequencer in v0.96.509)
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
    ui_beep(1319, 165)   // 击落：一声高而短的「叮」
    // shot down: a short, high "ding"
}

func drawShip() {
    var x = A[0]
    var y = A[1]
    // 机身
    // Fuselage
    ui_rect(x - 5, y - shipH() / 2, 10, shipH(), colorShip(), 1, 0, 3)
    // 机翼
    // Wings
    ui_rect(x - shipW() / 2, y, shipW(), 7, colorShip(), 1, 0, 2)
    // 尾焰（每拍换一点，看起来在喷）
    // Exhaust flame (changes a little each tick, so it looks like it is burning)
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
    // Starfield: 8 stars, each moving down at its own speed per tick, returning to the top when it leaves the bottom
    var i = 0
    while i < 8 {
        var sy = A[68 + i] + 2 + i % 3
        if sy > A[10] - 4 { sy = topBar() }
        A[68 + i] = sy
        ui_rect(A[60 + i], sy, 3, 3, colorStar(), 1, 0, 0)
        i = i + 1
    }

    // 子弹
    // Bullets
    i = 0
    while i < 4 {
        if A[28 + i] != 0 { ui_rect(A[20 + i] - 1, A[24 + i], bulW(), bulH(), colorBullet(), 1, 0, 1) }
        i = i + 1
    }

    // 敌机
    // Enemy planes
    i = 0
    while i < foeSlots() {
        if A[48 + i] != 0 { drawFoe(i) }
        i = i + 1
    }

    drawShip()

    // HUD：分数条 / 最高分条 + 右上角写数字（Swift 拼字符串是好的）
    // HUD: score bar / best-score bar + digits in the top-right (string concatenation in Swift is fine)
    // 界面语言（0=中文 1=英文）：从状态表里取个局部量 —— 模块级标量读出来是垃圾（见文件头 ①）。
    // UI language (0=Chinese 1=English): pull it from the state table into a local — a module-level scalar reads back as garbage (see header ①).
    var lang = A[110]
    ui_text(8, 8, lang == 0 ? "得分" : "Score", colorHud(), 13, anchorLeft())
    var bar = A[2]
    if bar > 160 { bar = 160 }
    ui_rect(58, 11, bar, 10, colorBar(), 1, 0, 0)
    ui_text(A[9] / 2, 8, lang == 0 ? "最高" : "Best", colorHud(), 13, anchorCenter())
    var bar2 = A[3]
    if bar2 > 160 { bar2 = 160 }
    ui_rect(A[9] / 2 + 46, 11, bar2, 10, colorBar2(), 1, 0, 0)
    ui_text(A[9] - 8, 8, numToStr(A[2]), colorWhite(), 14, anchorRight())
    // 剩余命数：右上角画几架小飞机（不是数字 —— 一眼看得出还剩几条）
    // Lives left: draw a few little planes in the top-right (not a number — you can see at a glance how many are left)
    var lf = 0
    while lf < A[14] {
        ui_rect(A[9] - 24 - lf * 16, 26, 10, 8, colorShip(), 1, 0, 2)
        lf = lf + 1
    }

    if A[5] != 0 { ui_text(A[9] / 2, A[10] / 2, lang == 0 ? "暂停（SELECT 继续）" : "Paused (SELECT)", colorEnemy(), 16, anchorCenter()) }
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
    ui_beep(131, 320)   // 死（结局）：**最低音**、最长
    // dead (ending): **the lowest note**, the longest one
    draw()
    var lang = A[110]
    if ui_dlg_msg(lang == 0 ? "飞机空战" : "Air Combat", lang == 0 ? "被撞到了，这一局结束。\n再来一局？（选「否」退出）" : "You crashed. Round over.\nPlay again? (choose 'No' to quit)", 0) != 0 { ui_win_close(); return }
    reset()
}

// 返回 0 表示本拍无变化、不必重画
// Returning 0 means nothing changed this tick and no redraw is needed
func step() -> Int {
    if A[4] == 0 { return 0 }
    if A[5] != 0 { return 0 }

    A[8] = A[8] + 1

    // 自动开火
    // Automatic fire
    A[11] = A[11] - 1
    if A[11] <= 0 {
        fire()
        A[11] = 5 - A[6] / 3
        if A[11] < 2 { A[11] = 2 }
    }

    // 子弹上行
    // Bullets travel up
    var i = 0
    while i < 4 {
        if A[28 + i] != 0 {
            A[24 + i] = A[24 + i] - 14
            if A[24 + i] < topBar() - 10 { A[28 + i] = 0 }
        }
        i = i + 1
    }

    // 敌机下行
    // Enemy planes travel down
    i = 0
    while i < foeSlots() {
        if A[48 + i] != 0 { A[44 + i] = A[44 + i] + A[6] }
        i = i + 1
    }

    // 刷敌机
    // Spawn enemy planes
    A[13] = A[13] + 1
    if A[13] >= 22 - A[6] {
        A[13] = 0
        spawnFoe()
    }

    // 子弹 × 敌机
    // Bullet × enemy
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
    // Enemy × player / enemy off the bottom
    // ⚠ 这一圈必须走 foeSlots()：写成 4 的话，第 5、6 号槽的敌机**永远不被清、也永远不扣命**
    // ⚠ This loop must go through foeSlots(): written as 4, the planes in slots 5 and 6 are **never cleared and never cost a life**
    //   —— 它们掉出屏幕后槽位一直占着，刷到第 3 架就再也没有敌机了（静默地"游戏自己停了"）。
    //   — after they fall off screen their slots stay occupied, and by the 3rd spawn there are no more enemy planes at all (a silent "the game stopped by itself").
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
                // 被撞：低闷的一声（原来 43 = 98Hz 在 C3 以下 —— 按判据抬到 165，
                // Hit: a low, muffled note (it used to be 43 = 98Hz, below C3 — raised to 165 per the criterion,
                //   与"死"那条 131 也差得开）
                //   still far enough from the 131 used for "dead")
                ui_beep(165, 66)
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
    // 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），存进 A[110]（见 draw）。
    // UI language: ask the host once at the start whether it wants Chinese or English (0=Chinese 1=English) and store it in A[110] (see draw).
    // ⚠ 别在每帧里调 —— 那是一次 syscall。
    // ⚠ Don't call it in every frame — that's a syscall.
    A[110] = ui_get_language()
    var lang = A[110]
    ui_win_open(lang == 0 ? "飞机空战" : "Air Combat", A[9], A[10])
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
            // After speeding up you must replace the timer (a repeating timer's interval is fixed when it is created)
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
        // Draw a frame as soon as a key arrives (on a phone pressing an arrow key must move the plane immediately)
        if t == msgKeydown() { draw() }
    }

    ui_timer_kill(tid)
    ui_keep_on(0)
    ui_win_close()
}
