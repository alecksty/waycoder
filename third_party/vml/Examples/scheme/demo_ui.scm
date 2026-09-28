; demo_ui.scm —— **第 4 层：最新 UI 接口**（`ui_*`）
; demo_ui.scm -- **layer 4: the newest UI interface** (`ui_*`)
;
; 这一层和第 3 层（BGI）正好相反：
; This layer is the exact opposite of layer 3 (BGI):
;
;   · **没有固定分辨率**。画布多大由宿主给（`ui_scr_w` / `ui_scr_h`），
;   · **There is no fixed resolution**. The canvas size is given by the host (`ui_scr_w` / `ui_scr_h`),
;     程序按拿到的尺寸现排版 —— 手机上还有手柄区收放、横竖屏切换会让它变。
;     and the program lays itself out for the size it gets -- on a phone the collapsible gamepad area and screen rotation change it.
;   · **颜色是真彩**（`0xAARRGGBB`），不是 16 个索引色。
;   · **Colors are true color** (`0xAARRGGBB`), not 16 indexed colors.
;   · **有消息循环**。触摸、按键、定时器、窗口被关 / 被转屏都从 `ui_wait_msg` 出来，
;   · **There is a message loop**. Touch, keys, timers, the window being closed or rotated all come out of `ui_wait_msg`,
;     程序按返回值分派 —— 这是"能交互的程序"和"画完就死"的分水岭。
;     and the program dispatches on the return value -- the watershed between "an interactive program" and "draw then die".
;
; ## 这个 demo 演示什么
; ## What this demo shows
;
;   ① **开窗前先问屏幕方向**（`ui_orientation`）—— 程序据此决定排版。
;   ① **Ask the screen orientation before opening the window** (`ui_orientation`) -- the program lays out accordingly.
;      方向是**设备**的属性，别拿 `ui_scr_w > ui_scr_h` 去推（那是可用绘图区，
;      Orientation is a property of the **device**; do not infer it from `ui_scr_w > ui_scr_h` (that is the usable drawing area,
;      手柄收起/展开会变）。
;      which changes when the gamepad is collapsed or expanded).
;   ② 开窗 → 拿画布尺寸 → 按尺寸现排（**没有写死的坐标**）。
;   ② Open the window -> get the canvas size -> lay out for that size (**no hard-coded coordinates**).
;   ③ 画图元：矩形 / 圆角矩形 / 圆 / 椭圆 / 直线 / 文字，位置全部由 w/h 算出来。
;   ③ Draw primitives: rectangle / rounded rectangle / circle / ellipse / line / text, all positioned from w/h.
;   ④ `ui_present` 交帧 —— 它是"这一帧画完了"的唯一信号。
;   ④ `ui_present` hands over the frame -- it is the only signal that says "this frame is done".
;   ⑤ **消息循环**：定时器驱动重画、按键 / 触摸有响应、**有界退出**
;   ⑤ **A message loop**: the timer drives redraws, keys / touches respond, and the exit is **bounded**
;      （收够 N 帧 或 按任意键 / 点任意处）。
;      (after N frames, or on any key / tap).
;
; ## ⚠ 本前端的四条限制（决定了这份为什么完全平铺）
; ## ⚠ Four limitations of this frontend (they explain why this file is completely flat)
;
;   · **用户函数里不能调库函数**（尾调用优化会跳过被调者的序言）⇒ `ui_*` 这些调用
;   · **A user function cannot call a library function** (tail-call optimization skips the callee's prologue) => the `ui_*` calls
;     **一律写在顶层**，本份一个 `define (f …)` 都没有；主循环用**顶层的 `do`**
;     are **all written at the top level**; this file has not a single `define (f ...)`, and the main loop is a **top-level `do`**
;     （`let` / 命名 `let` 只在语句位受理，放表达式位不生成代码）。
;     (`let` / named `let` are accepted only in statement position; in expression position they generate no code).
;   · **没有 `break`** ⇒ 退出走一个 `quit` 标志，由 `do` 的**终止条件**结束。
;   · **There is no `break`** => exiting goes through a `quit` flag, ended by the `do`'s **termination condition**.
;   · **`remainder` / `quotient` 在库里没有对应标签**、`%` 也没有 ⇒
;   · **`remainder` / `quotient` have no matching labels in the library**, and there is no `%` either =>
;     取模手算 `(- i (* (/ i n) n))`（本前端 `/` 就是整数除）。
;     compute modulo by hand as `(- i (* (/ i n) n))` (in this frontend `/` is integer division).
;   · **字符串字面量里没有任何转义**、`int->string` 也链不到 ⇒ 数字没法拼进字符串。
;   · **String literals contain no escapes at all**, and `int->string` cannot be linked either => numbers cannot be joined into a string.
;     所以计数器**不画数字**，画**长度随计数增长的条**；收尾的统计行用
;     So the counters **do not draw digits** but a **bar whose length grows with the count**; the closing statistics line uses
;     `(display "x=")` + `(display x)` 分次打。
;     `(display "x=")` + `(display x)` printed separately.
;   · 颜色写**负数十进制**（词法器只认十进制，`0x…` 会被切成 `0` + 符号）：
;   · Colors are written as **negative decimals** (the lexer is decimal-only; `0x...` is split into `0` + a sign):
;     `0xFF101018` = `-15724520`。
;
; 跑法：
; How to run:
;   手机    vml run examples/scheme/demo_ui.scm
;   phone    vml run examples/scheme/demo_ui.scm
;   桌面    vmlcli Examples/scheme/demo_ui.scm --screen 640x480 --frames /tmp/fr
;   desktop  vmlcli Examples/scheme/demo_ui.scm --screen 640x480 --frames /tmp/fr
;   喂输入  … --input 脚本（一行一条，如 `200 touchdown 300 200` / `150 keydown 13`）
;   input    ... --input <script> (one per line, e.g. `200 touchdown 300 200` / `150 keydown 13`)

(define c_bg -15724520)        ; 0xFF101018
(define c_panel -15066588)     ; 0xFF1A1A24
(define c_title -1513232)      ; 0xFFE8E8F0
(define c_ok -11409298)        ; 0xFF51E86E
(define c_dim -6643536)        ; 0xFF9AA0B0
(define c_blue -11890471)      ; 0xFF4A90D9
(define c_orange -2069424)     ; 0xFFE06C50
(define c_gold -2509750)       ; 0xFFD9B44A
(define c_green -11483016)     ; 0xFF50C878
(define c_mark -8096)          ; 0xFFFFE060

(define orient 0)
(define w 360)
(define h 620)
(define tid 0)
(define fr 0)
(define keys 0)
(define touches 0)
(define tx -1)
(define ty 0)
(define quit 0)
(define t 0)
(define pad 0)
(define cx 0)
(define bw 0)
(define i 0)
(define cc 0)

; ── ① 开窗**之前**就问方向：程序据此决定排版 ──
; -- ① Ask the orientation **before** opening the window: the program lays out accordingly --
(set! orient (ui_orientation))

; 开窗：尺寸照宿主给的来，拿不到就退到竖屏默认值
; Open the window: take the size the host gives, falling back to the portrait default if unavailable
(set! w (ui_scr_w))
(set! h (ui_scr_h))
(if (< w 1) (set! w 360) 0)
(if (< h 1) (set! h 620) 0)
(ui_win_open "demo_ui (Scheme)" w h)
(ui_keep_on 1)

; 定时器：每 60ms 一拍，驱动重画（也是"没输入也能自己停"的那个节拍源）
; Timer: one tick every 60ms, driving redraws (also the tick source that lets it stop on its own with no input)
(set! tid (ui_timer_set 60 1))

; ── ⑤ 消息循环：定时器驱动重画，有界退出 ──
; -- ⑤ Message loop: the timer drives redraws, the exit is bounded --
; 退出**只靠 `do` 的终止条件** `(= quit 1)`（本前端没有 `break`）。
; Exiting relies **only on the `do`'s termination condition** `(= quit 1)` (this frontend has no `break`).
(do ()
    ((= quit 1))

  (set! t (ui_wait_msg 0))     ; 阻塞等一条（有定时器在 ⇒ 一定会返回）
  ; Block for one message (a timer is running => it always returns)

  (if (= t 9)
      (begin
        (set! fr (+ fr 1))
        (if (> fr 29) (set! quit 1) 0))     ; ← 有界：到点自己停
        ; <- Bounded: it stops by itself once the frame count is reached
      0)
  (if (= t 1)
      (begin
        (set! keys (+ keys 1))
        (set! quit 1))                      ; ← 有界：收到键就退
        ; <- Bounded: exit on a key press
      0)
  (if (= t 6)
      (begin
        (set! touches (+ touches 1))
        (set! tx (ui_msg_a))                ; A = x
        (set! ty (ui_msg_b))                ; B = y
        (set! quit 1))                      ; ← 有界：点到就退
        ; <- Bounded: exit on a tap
      0)
  (if (= t 4)
      (begin
        (set! touches (+ touches 1))
        (set! tx (ui_msg_a))
        (set! ty (ui_msg_b))
        (set! quit 1))
      0)
  (if (= t 10) (set! quit 1) 0)             ; 用户把窗口关了
  ; The user closed the window
  (if (= (ui_win_closed) 1) (set! quit 1) 0)

  ; ── 画这一帧（每个位置都由 w / h 现算，没有写死的坐标）──
  ; -- Draw this frame (every position is computed from w / h; no hard-coded coordinates) --
  (set! pad (/ w 16))
  (set! cx (/ w 2))

  (ui_clear c_bg)

  (ui_text cx 12 "UI 接口 / demo_ui (Scheme)" c_title 16 1)
  (if (= orient 1)
      (ui_text cx 36 "屏幕方向 = 横屏 (LANDSCAPE)" c_ok 13 1)
      (ui_text cx 36 "屏幕方向 = 竖屏 (PORTRAIT)" c_ok 13 1))
  (ui_text cx 56 "画布按宿主给的尺寸现排" c_dim 12 1)

  ; 一个跟随尺寸的方框（转屏后它会跟着变宽变矮 —— 这就是"不写死坐标"的证明）
  ; A box that follows the size (after rotating it becomes wider and shorter -- proof of "no hard-coded coordinates")
  (ui_rect pad 76 (- w (* pad 2)) 84 c_panel 1 0 10)
  (ui_rect (+ pad 6) 82 (- (- w (* pad 2)) 12) 28 c_blue 1 0 6)
  (ui_text (+ pad 16) 88 "rect / 圆角矩形（随屏宽伸缩）" c_bg 12 0)

  ; 圆 / 椭圆 / 直线：三个基本形
  ; Circle / ellipse / line: the three basic shapes
  (ui_circle (- cx (/ w 6)) 200 (/ w 12) c_orange 1 0)
  (ui_ellipse (+ cx (/ w 6)) 200 (/ w 9) (/ w 18) c_gold 1 0)
  (ui_line pad 250 (- w pad) 250 c_green 3)

  ; 一条 8 格真彩色带（这些是 RGB，不是调色板索引）
  ; An 8-cell true-color band (these are RGB, not palette indexes)
  ; `i % 2` 手算成 `(- i (* (/ i 2) 2))`（没有 remainder/%）
  ; `i % 2` is computed by hand as `(- i (* (/ i 2) 2))` (there is no remainder/%)
  (set! bw (/ (- w (* pad 2)) 8))
  (set! i 0)
  (do ((k 0 (+ k 1)))
      ((> k 7))
    (set! i k)
    (set! cc c_blue)
    (if (= (- i (* (/ i 2) 2)) 1) (set! cc c_orange) 0)
    (ui_rect (+ pad (* i bw)) 266 (- bw 2) 18 cc 1 0 2))
  (ui_text cx 296 "真彩 0xAARRGGBB（不是索引色）" c_dim 12 1)

  ; 事件计数：不画数字，画**长度随计数增长的条**（数字拼不进字符串，见文件头）
  ; Event counts: no digits are drawn, but a **bar whose length grows with the count** (digits cannot be joined into a string, see the file header)
  (ui_text pad 336 "帧" c_dim 12 0)
  (set! bw (* fr 6))
  (if (> bw (- (- w (* pad 2)) 40)) (set! bw (- (- w (* pad 2)) 40)) 0)
  (ui_rect (+ pad 40) 324 bw 14 c_blue 1 0 3)

  (ui_text pad 366 "按键" c_dim 12 0)
  (set! bw (* keys 30))
  (if (> bw (- (- w (* pad 2)) 60)) (set! bw (- (- w (* pad 2)) 60)) 0)
  (ui_rect (+ pad 60) 354 bw 14 c_orange 1 0 3)

  (ui_text pad 396 "触摸" c_dim 12 0)
  (set! bw (* touches 30))
  (if (> bw (- (- w (* pad 2)) 60)) (set! bw (- (- w (* pad 2)) 60)) 0)
  (ui_rect (+ pad 60) 384 bw 14 c_gold 1 0 3)

  ; 触摸标记：点哪儿就在哪儿留一个圈（证明坐标真的能用）
  ; Touch marker: a circle is left wherever you tap (proving the coordinates really work)
  (if (> tx 0)
      (begin
        (ui_circle tx ty 18 c_mark 0 2)
        (ui_circle tx ty 4 c_mark 1 0)
        (ui_text cx (- h 60) "触摸坐标已经用上了" c_mark 12 1))
      (ui_text cx (- h 60) "点一下屏幕 / 按任意键退出" c_dim 12 1))

  (ui_text cx (- h 36) "退出：按任意键或点任意处（或等 N 帧到点）" c_dim 12 1)

  (ui_present))

; ── 收尾：定时器要杀，屏幕常亮要还回去 ──
; -- Wrap-up: kill the timer and give back keep-screen-on --
(ui_timer_kill tid)
(ui_keep_on 0)
(ui_win_close)

; 给无头验证留几行**确定性**的判据（图形部分只能肉眼看，这几个数能自动比）
; A few **deterministic** checks are left for headless verification (the graphics can only be eyeballed; these numbers can be compared automatically)
(display "demo_ui: orient=")
(display orient)
(newline)
(display "demo_ui: canvas=")
(display w)
(display " x ")
(display h)
(newline)
(display "demo_ui: frames=")
(display fr)
(display " keys=")
(display keys)
(display " touches=")
(display touches)
(newline)
(display "demo_ui: done")
(newline)
