;; 接方块 —— 用 **Scheme** 写的手机游戏
;; Catch -- a mobile game written in **Scheme**
;;
;; 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
;; Gameplay: the left/right arrow keys move the paddle at the bottom and bounce the falling ball back; missing it ends the round. Each catch = +10 points.
;;
;; ◆ 手机那套 UI
;; ◆ The mobile UI set
;;
;; 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
;; Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
;; 由 `vmltool.config.xml` 的 `<Language Name="scheme" Libs="vmlui.vml">` 挂上来。
;; pulled in by `<Language Name="scheme" Libs="vmlui.vml">` in `vmltool.config.xml`.
;;
;; ◆ 写法要求
;; ◆ Style requirements
;;
;;   · **全局状态一律留在顶层**：本前端的用户函数**看不见顶层变量** —— 二者都按
;;   · **Global state always stays at the top level**: this frontend's user functions **cannot see top-level variables** -- both are addressed by
;;     `R12 + (12 - off)` 寻址，而函数里的 `R12` 是它自己的帧指针。实测
;;     `R12 + (12 - off)`, and inside a function `R12` is its own frame pointer. Measured:
;;     `(define g 0) (define (w1) (set! g 5))` 调 `(w1)` 直接踩坏保存的 R12
;;     `(define g 0) (define (w1) (set! g 5))` called as `(w1)` directly clobbers the saved R12
;;     （`R12(BP)` 变成 8，紧接着 `[R12-48]` 越界）；函数里**读**顶层变量同样读错。
;;     (`R12(BP)` becomes 8, immediately followed by an out-of-range `[R12-48]`); **reading** a top-level variable inside a function is just as wrong.
;;     ⇒ 游戏是一个**扁平顶层程序**：循环、状态、判定全在顶层，
;;     => The game is a **flat top-level program**: loops, state and decisions all at the top level,
;;       只有「参数全传进来、不碰全局」的纯函数才允许抽出去（下面的 `clamp`）。
;;        and only "all parameters passed in, no globals touched" pure functions may be extracted (the `clamp` below).
;;   · 颜色写**负数十进制**（词法器只认十进制，`0x…` 会被切成 0 + 符号）。
;;   · Colors are written as **negative decimals** (the lexer is decimal-only; `0x...` is split into 0 + a sign).
;;   · `display`/`print` 只取**第一个**实参、且**不补换行** ⇒ 本份不用控制台。
;;   · `display`/`print` take only the **first** argument and **add no newline** => this file does not use the console.
;;   · `(- 0 x)` 写负号：`-` 只在**两个实参**时才编成减法指令。
;;   · Write negation as `(- 0 x)`: `-` compiles to a subtraction instruction only with **two arguments**.
;;   · `remainder` / `quotient` 在库里没有对应标签（链接期报「未找到标签」）⇒ 取整用 `/`。
;;   · `remainder` / `quotient` have no matching labels in the library (link time reports "label not found") => use `/` for integer division.
;;   · `(let …)` / 命名 `let` **只在语句位受理**（放表达式位不生成任何代码）⇒ 循环用 `do`。
;;   · `(let ...)` / named `let` are **accepted only in statement position** (in expression position they generate no code) => use `do` for loops.
;;
;; ◆ 本份踩过、已修的六个前端缺陷（2026-09-17）
;; ◆ Six frontend defects this file hit, all already fixed (2026-09-17)
;;
;;   ① **顶层变量只能放 3 个** —— `main` 不 `sub R13`，而顶层绑定走 `R12+(12-off)`
;;   ① **Only 3 top-level variables could be held** -- `main` did not `sub R13`, while top-level bindings go through `R12+(12-off)`
;;      （`R12+8 / +4 / +0` 之后继续往下到 `R12-4 …`），正好是**压栈区**。
;;      (after `R12+8 / +4 / +0` it continues down to `R12-4 ...`), which is exactly the **push area**.
;;      实测 12 个顶层变量求和得 24（应 78）、5 个变量夹几次调用后得 65536。
;;      Measured: 12 top-level variables summed to 24 (should be 78), and 5 variables around a few calls gave 65536.
;;      现在 `main` 也占帧 —— 本份用了 14 个顶层变量。
;;      Now `main` occupies a frame too -- this file uses 14 top-level variables.
;;   ② **≥2 个形参的函数读错实参** —— 形参槽原先按 `--varOff*4` 递减分配，
;;   ② **A function with 2 or more formal parameters read the wrong arguments** -- the parameter slots used to be allocated downward by `--varOff*4`,
;;      只在单参数时恰好对；`(define (pick a b) b)` 调 `(pick 11 22)` 得 11。
;;      which happened to be right only for a single parameter; `(define (pick a b) b)` called as `(pick 11 22)` returned 11.
;;   ③ **从用户函数里调库函数必崩** —— 尾位置一律编成 `jmp <名>_body`
;;   ③ **Calling a library function from a user function always crashed** -- a tail position was always compiled to `jmp <name>_body`
;;      （跳进被调者的函数体、跳过它的序言），对自递归是对的、对库函数是错的。
;;      (jumping into the callee's body and skipping its prologue), which is right for self-recursion and wrong for library functions.
;;   ④ **函数体 / `let` body 只编译第一个形式**，第二个起被静默丢掉。
;;   ④ **A function body / `let` body compiled only its first form**; the second onward was silently dropped.
;;   ⑤ **命名 `let` 完全不占帧**，体内局部量直接写进压栈区。
;;   ⑤ **A named `let` occupied no frame at all**; its local variables were written straight into the push area.
;;   ⑥ **帧大小按生成结束时的 varOff 算**，而 `let`/`do` 收尾会把 varOff 还原 ⇒ 低估。
;;   ⑥ **The frame size was computed from varOff at the end of generation**, while the `let`/`do` epilogue restored varOff => underestimated.
;;
;;   骨架 `skel.scm` 只有单参数函数、且循环全在顶层，上面六条一条都照不出来。
;;   The `skel.scm` skeleton has only single-parameter functions and all its loops at the top level, so not one of the six above shows up there.

(define w 360)
(define h 620)
(define bx 0)
(define px 0)
(define py 70)
(define dx 3)
(define dy 5)
(define sc 0)
(define hi 0)
(define alive 1)
(define tid 0)
(define t 0)
(define k 0)
(define quit 0)

;; 唯一允许抽出去的形态：**参数全传、不读全局**的纯函数。
;; The only shape allowed to be extracted: a pure function with **all parameters passed in and no globals read**.
;; 三个形参 —— 正好吃到 ② 那条修复（修前 p0 会读到返回地址槽）。
;; Three formal parameters -- exactly exercising fix ② (before it, p0 read the return-address slot).
(define (clamp v lo cap)
  (if (< v lo)
      lo
      (if (> v cap) cap v)))

(set! w (ui_scr_w))
(set! h (ui_scr_h))
(if (< w 1) (set! w 360) 0)
(if (< h 1) (set! h 620) 0)

(ui_win_open "接方块" w h)
(ui_keep_on 1)
(set! tid (ui_timer_set 40 0))

;; 开局状态（`reset` 那几行的内联版；函数版本看不见顶层变量，见文件头）
;; Start state (the inline version of the `reset` lines; a function version cannot see top-level variables, see the file header)
(set! bx (- (/ w 2) 40))
(set! px (/ w 2))
(set! py 70)
(set! dx 3)
(set! dy 5)
(set! sc 0)
(set! alive 1)

(do ()
    ((= quit 1))

  (if (= (ui_win_closed) 1) (set! quit 1) 0)

  ;; ── draw ──
  (ui_clear -15724520)
  (ui_text 8 8 "得分" -6643536 13 0)
  (ui_rect 58 11 sc 10 -11409298 1 0 0)
  (ui_text (/ w 2) 8 "最高" -6643536 13 1)
  (ui_rect (+ (/ w 2) 46) 11 hi 10 -63488 1 0 0)
  (ui_rect bx (- h 40) 80 12 -63488 1 0 6)
  (ui_circle px py 9 -131246 1 0)
  (if (= alive 0)
      (ui_text (/ w 2) (/ h 2) "按回车重开" -131246 16 1)
      0)
  (ui_present)

  (set! t (ui_wait_msg 0))
  (if (= t 10) (set! quit 1) 0)

  ;; ── 定时器一拍（t=9）：球走一步 ──
  ;; -- One timer tick (t=9): the ball moves one step --
  (if (= t 9)
      (begin
        (if (= alive 1)
            (begin
              (set! px (+ px dx))
              (set! py (+ py dy))
              (if (< px 10) (begin (set! px 10) (set! dx (- 0 dx))) 0)
              (if (> px (- w 10)) (begin (set! px (- w 10)) (set! dx (- 0 dx))) 0)
              (if (< py 30) (begin (set! py 30) (set! dy (- 0 dy))) 0)

              ;; 接住：球落到挡板带上、横向也在挡板范围内。
              ;; Catch: the ball lands on the paddle band and is horizontally within the paddle.
              ;; 多条件用**嵌套 if** —— `and` 本前端有分支但没验过。
              ;; Multiple conditions use **nested if** -- this frontend has a branch for `and` but it was never verified.
              (if (> py (- h 52))
                  (if (< py (- h 30))
                      (if (> px (- bx 9))
                          (if (< px (+ bx 89))
                              (begin
                                (set! dy (- 0 dy))
                                (set! py (- h 52))
                                (set! sc (+ sc 10))
                                (if (> sc hi) (set! hi sc) 0)
                                (ui_beep 880 30))
                              0)
                          0)
                      0)
                  0)

              ;; 没接住：本局结束
              ;; Missed: the round ends
              ;; 选「否/拒绝」→ quit 置 1，主循环下一拍退出（ui_dlg_msg 返回 0=是 / 1=否）
              ;; Choosing "No / reject" -> quit is set to 1 and the main loop exits on the next tick (ui_dlg_msg returns 0=yes / 1=no)
              (if (> py h)
                  (begin
                    (set! alive 0)
                    (ui_beep 220 260)
                    (if (= (ui_dlg_msg "接方块" "没接住，这一局结束。\n再来一局？（选「否」退出）" 0) 0)
                        (begin
                          (set! bx (- (/ w 2) 40))
                          (set! px (/ w 2))
                          (set! py 70)
                          (set! dx 3)
                          (set! dy 5)
                          (set! sc 0)
                          (set! alive 1))
                        (set! quit 1)))
                  0))
            0)
        )
      0)

  ;; ── 按键（t=1）──
  ;; -- Key press (t=1) --
  (if (= t 1)
      (begin
        (set! k (ui_msg_a))
        (if (= k 27) (set! quit 1) 0)
        (if (= k 37) (set! bx (clamp (- bx 20) 4 (- w 84))) 0)
        (if (= k 39) (set! bx (clamp (+ bx 20) 4 (- w 84))) 0)
        (if (= k 13)
            (begin
              (set! bx (- (/ w 2) 40))
              (set! px (/ w 2))
              (set! py 70)
              (set! dx 3)
              (set! dy 5)
              (set! sc 0)
              (set! alive 1))
            0))
      0))

(ui_timer_kill tid)
(ui_keep_on 0)
(ui_win_close)
