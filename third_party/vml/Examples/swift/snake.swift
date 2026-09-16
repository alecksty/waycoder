// 贪吃蛇 —— 用 **Swift** 写的手机游戏
//
// ⚠ **状态：编译通过、能出图，但还不能玩**（v0.96.185）。
//   窗口尺寸、棋盘网格、HUD 文字都出来了，蛇身与分数画不出来 —— 卡在
//   **"在一个函数里写全局数组、在另一个函数里读"读回来不对**（实测：`A[1] = 3` 之后
//   经一个函数读回来，循环画了 8 个而不是 3；提进局部变量也救不了）。
//   同一份评估里 C# 是"带参方法的参数会串"、Go/Pascal 是"条件跳转跳寄存器"。
//   **全 22 个前端的结论、根因、修法见 `docs/前端游戏能力评估.md`。**
//   修好那一条（一处）之后，这个文件就是可玩的 —— 剩下的全是纯逻辑。
//
// ## 为什么是 Swift
//
// 手机那套 UI（开窗 / 绘图 / 输入 / 定时器）的实现是 C 写的
// （`Lib/shared/src/vmlui.c` → `Lib/shared/vmlui.vml`），由 `vmltool.config.xml` 的
// `<Language ... Libs="vmlui.vml">` 挂给各前端。把 22 个前端逐个过筛（编译出图 / 循环 /
// 数组 / 函数 / 参数传递）之后，**Swift 是干净通过的那一门**：
//
//   ✔ 直接 `call` 到包装标签，且实参**逆序压栈**（包装读 `[R12+12]` = 最后压的那个）
//   ✔ 循环 / 分支 / 数组读写 / 带参函数与返回值
//   ✔ 定时器 + `ui_wait_msg` 收消息（游戏心跳）
//
// 其它语言的结论见 `docs/前端游戏能力评估.md`（Go/Pascal 条件跳转跳寄存器、JavaScript
// 把未知函数当变量 + 正序压栈、C# 参数槽串了…）。**没有一门是"差不多就行"的** ——
// 这类毛病的表现都是"编译全绿、跑起来不对"。
//
// ## 两条写法上的约束（都是实测出来的，不是偏好）
//
// ① **状态一律放数组**：这几门前端的**模块级标量**（`var n = 5`）读出来是垃圾
//    （在 C# 上是把标签**地址**当成了值）。数组是堆上的，读写都可靠。
// ② **常量写成 `main` 里的局部变量**：同理，模块级 `let` 也不可靠。
//
// 数值的唯一真源仍是 `Lib/c/waycoder_ui.h`。

var A = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
// ⚠ **只用一个数组**。实测：同一程序里开多个全局数组（S / SEGX / SEGY）会互相踩内存 ——
// 症状是蛇只画出一节、HUD 文字错位（写蛇身把状态槽冲了）。一个数组不自我别名，绕开它。
//   0=head 1=len 2=dx 3=dy 4=fx 5=fy 6=score 7=best
//   8=alive 9=paused 10=stepMs 11=cell 12=ox 13=oy 14=sw 15=sh
//   16+2i=第 i 节 x，16+2i+1=第 i 节 y
func segx(i: Int) -> Int { return 16 + i * 2 }
func segy(i: Int) -> Int { return 17 + i * 2 }

func cw() -> Int { return 20 }
func ch() -> Int { return 18 }
func maxseg() -> Int { return 40 }

func cellColorBody() -> Int { return -11409298 }   // 0xFF51E86E
func cellColorHead() -> Int { return -63488 }      // 0xFFFF0800
func cellColorFood() -> Int { return -131246 }     // 0xFFFDFF52
func cellColorGrid() -> Int { return -15263713 }   // 0xFF17181F
func colorBg() -> Int { return -15724520 }         // 0xFF101018
func colorText() -> Int { return -6643536 }        // 0xFF9AA0B0

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

func occupied(c: Int, r: Int) -> Int {
    var i = 0
    while i < A[1] {
        var k = A[0] - i
        while k < 0 { k = k + maxseg() }
        if A[segx(k)] == c && A[segy(k)] == r { return 1 }
        i = i + 1
    }
    return 0
}

func placeFood() {
    var tries = 0
    while tries < 500 {
        var c = ui_rand(cw())
        var r = ui_rand(ch())
        if occupied(c, r) == 0 {
            A[4] = c
            A[5] = r
            return
        }
        tries = tries + 1
    }
    A[4] = 0
    A[5] = 0
}

func drawCell(c: Int, r: Int, color: Int) {
    ui_rect(A[12] + c * A[11], A[13] + r * A[11], A[11] - 1, A[11] - 1, color, 1, 0, 2)
}

func draw() {
    ui_clear(colorBg())
    var c = 0
    while c < cw() {
        var r = 0
        while r < ch() {
            if (c + r) % 2 == 0 { drawCell(c, r, cellColorGrid()) }
            r = r + 1
        }
        c = c + 1
    }
    drawCell(A[4], A[5], cellColorFood())
    var i = 0
    while i < A[1] {
        var k = A[0] - i
        while k < 0 { k = k + maxseg() }
        if i == 0 { drawCell(A[segx(k)], A[segy(k)], cellColorHead()) }
        else { drawCell(A[segx(k)], A[segy(k)], cellColorBody()) }
        i = i + 1
    }
    ui_text(8, 8, "得分", colorText(), 13, 0)
    ui_text(56, 8, numToStr(A[6]), -1, 15, 0)
    ui_text(A[14] / 2, 8, "最高", colorText(), 13, 1)
    ui_text(A[14] / 2 + 44, 8, numToStr(A[7]), -1, 15, 0)
    if A[9] != 0 { ui_text(A[14] / 2, A[15] / 2, "暂停（SELECT 继续）", cellColorFood(), 16, 1) }
    ui_present()
}

func reset() {
    A[1] = 3        // 长度
    A[0] = 2        // 头下标
    A[segx(0)] = 8; A[segy(0)] = 8
    A[segx(1)] = 7; A[segy(1)] = 8
    A[segx(2)] = 6; A[segy(2)] = 8
    A[2] = 1; A[3] = 0
    A[6] = 0
    A[8] = 1
    A[9] = 0
    A[10] = 170
    placeFood()
}

func gameOver() {
    A[8] = 0
    ui_beep(220, 260)
    draw()
    ui_dlg_msg("贪吃蛇", "撞到了，这一局结束。\n再来一局？（选「否」退出）", 0)
    reset()
}

func step() -> Int {
    if A[8] == 0 || A[9] != 0 { return 0 }
    var nc = A[segx(A[0])] + A[2]
    var nr = A[segy(A[0])] + A[3]
    if nc < 0 || nc >= cw() || nr < 0 || nr >= ch() {
        gameOver()
        return 1
    }
    var i = 0
    while i < A[1] - 1 {
        var k = A[0] - i
        while k < 0 { k = k + maxseg() }
        if A[segx(k)] == nc && A[segy(k)] == nr {
            gameOver()
            return 1
        }
        i = i + 1
    }
    var eat = 0
    if nc == A[4] && nr == A[5] { eat = 1 }
    A[0] = A[0] + 1
    if A[0] >= maxseg() { A[0] = 0 }
    A[segx(A[0])] = nc
    A[segy(A[0])] = nr
    if eat != 0 {
        if A[1] < maxseg() { A[1] = A[1] + 1 }
        A[6] = A[6] + 10
        if A[6] > A[7] { A[7] = A[6] }
        if A[10] > 70 { A[10] = A[10] - 6 }
        ui_beep(880, 40)
        placeFood()
    }
    return 1
}

func turn(nx: Int, ny: Int) {
    if nx + A[2] == 0 && ny + A[3] == 0 { return }
    A[2] = nx
    A[3] = ny
}

func main() {
    // 常量（源：Lib/c/waycoder_ui.h）—— 只能放局部变量，见文件头 ②
    var KEY_ENTER = 13
    var KEY_SELECT = 16
    var KEY_ESCAPE = 27
    var KEY_LEFT = 37
    var KEY_UP = 38
    var KEY_RIGHT = 39
    var KEY_DOWN = 40
    var MSG_TIMER = 9
    var MSG_KEYDOWN = 1
    var MSG_CLOSE = 10

    // 尺寸先落到局部再存进状态表 —— **实测**：把 `A[14] = ui_scr_w()` 直接喂给
    // `ui_win_open` 会开出一个 60×64 的小窗（编译全绿、只有出图才看得出来）；
    // 经一道局部变量就正常。这类"某个位置读全局就是不行"的坑，各前端都不一样，
    // 唯一的办法是**出图看**，不能靠推断。
    var w = ui_scr_w()
    var h = ui_scr_h()
    if w <= 0 { w = 360 }
    if h <= 0 { h = 620 }
    A[14] = w
    A[15] = h
    ui_win_open("贪吃蛇", w, h)

    // 版面算一次，画与判定共用
    var byw = A[14] - 8
    var byh = A[15] - 46
    A[11] = byw / cw()
    if byh / ch() < A[11] { A[11] = byh / ch() }
    if A[11] < 4 { A[11] = 4 }
    A[12] = (A[14] - A[11] * cw()) / 2
    A[13] = 40

    ui_keep_on(1)
    reset()
    draw()

    var tid = ui_timer_set(A[10], 0)
    var curMs = A[10]

    while ui_win_closed() == 0 {
        var t = ui_wait_msg(0)
        if t == 0 { continue }
        if t == MSG_CLOSE { break }
        if t == MSG_TIMER {
            if step() != 0 { draw() }
            if A[10] != curMs {
                ui_timer_kill(tid)
                curMs = A[10]
                tid = ui_timer_set(curMs, 0)
            }
            continue
        }
        if t == MSG_KEYDOWN {
            var k = ui_msg_a()
            if k == KEY_ESCAPE { break }
            else if k == KEY_LEFT { turn(-1, 0) }
            else if k == KEY_RIGHT { turn(1, 0) }
            else if k == KEY_UP { turn(0, -1) }
            else if k == KEY_DOWN { turn(0, 1) }
            else if k == KEY_ENTER { reset(); draw() }
            else if k == KEY_SELECT {
                if A[9] != 0 { A[9] = 0 } else { A[9] = 1 }
                draw()
            }
        }
    }

    ui_timer_kill(tid)
    ui_keep_on(0)
    ui_win_close()
}
