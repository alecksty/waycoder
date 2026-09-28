# 接方块 —— 用 **R** 写的手机游戏
# Catch -- a mobile game written in **R**
#
# 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
# Gameplay: the left/right arrow keys move the paddle at the bottom and bounce the falling ball back; missing it ends the round. Each catch = +10 points.
#
# ◆ 手机那套 UI
# ◆ The mobile UI set
#
# 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
# Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
# 由 `vmltool.config.xml` 的 `<Language Name="r" Libs="vmlui.vml">` 挂上来。
# pulled in by `<Language Name="r" Libs="vmlui.vml">` in `vmltool.config.xml`.
#
# ◆ 修掉的那条前端缺陷（2026-09-17）
# ◆ The frontend defect that was fixed (2026-09-17)
#
# `GenerateFuncDef` 此前是**写死的 `EmitPrologueWithFrame(64)`** —— 每个函数只给 64 字节帧，
# `GenerateFuncDef` used to be a **hard-coded `EmitPrologueWithFrame(64)`** -- every function got only a 64-byte frame,
# 而局部量按 `[R12-4]、[R12-8]…` 分配、`R12 == R13`，**超出的部分直接写进调用方的帧**。
# while locals were allocated as `[R12-4]`, `[R12-8]`, ... with `R12 == R13`, so **anything beyond that was written straight into the caller's frame**.
# 有意思的是同一个文件里，**顶层**（`GenerateCode`）早就改成了「序言占位 + 生成完回填」，
# Interestingly, in the same file the **top level** (`GenerateCode`) had already been changed to "reserve a prologue placeholder + backfill when done",
# 只有函数那条路还留着写死的 64 —— 两个口径并存。
# and only the function path still kept the hard-coded 64 -- two conventions coexisting.
# 现已统一成回填式；判据是一个含 20 个局部量（80 字节 > 64）的函数：修前崩、修后得 210 ✓。
# It is now unified to the backfill form; the criterion is a function with 20 locals (80 bytes > 64): it crashed before the fix and returns 210 after it.
# 骨架照不出来，因为骨架的函数都极小。
# The skeleton cannot show this, because all of its functions are tiny.
#
# ◆ 写法要求（沿用 corpus/r/skel.r）
# ◆ Style requirements (following corpus/r/skel.r)
#
#   · 循环**不写** `for (i in 1:4)`：`:` 被解析成 `CallNode(":")`、GenerateCall 无此分支
#   · Do **not** write `for (i in 1:4)`: `:` is parsed into `CallNode(":")` and GenerateCall has no branch for it
#     ⇒ 编出 `CALL func_:`（带冒号的标签，永不解析）。用 `c(1,2,3,4)`，循环变量拿到元素值。
#     => it compiles to `CALL func_:` (a label with a colon, which never resolves). Use `c(1,2,3,4)`; the loop variable takes the element value.
#   · R 向量 **1 基**，下标写 1..n。
#   · R vectors are **1-based**; subscripts are written 1..n.
#   · `cat` / `print` 只打印**第一个实参** ⇒ 分数画成图形，不走控制台。
#   · `cat` / `print` print only the **first argument** => the score is drawn as a graphic instead of going to the console.
#   · 颜色写负数十进制（词法器不认 0x）。
#   · Colors are written as negative decimals (the lexer does not accept 0x).

# 状态：1=挡板x 2=球x 3=球y 4=球dx 5=球dy 6=分数 7=最高 8=存活 9=屏宽 10=屏高
# State: 1=paddle x 2=ball x 3=ball y 4=ball dx 5=ball dy 6=score 7=best 8=alive 9=screen w 10=screen h
A <- c(0, 0, 0, 0, 0, 0, 0, 0, 0, 0)

resetGame <- function() {
  A[1] <<- A[9] / 2 - 40
  A[2] <<- A[9] / 2
  A[3] <<- 70
  A[4] <<- 3
  A[5] <<- 5
  A[6] <<- 0
  A[8] <<- 1
}

draw <- function() {
  ui_clear(-15724520)
  ui_text(8, 8, "得分", -6643536, 13, 0)
  ui_rect(58, 11, A[6], 10, -11409298, 1, 0, 0)
  ui_text(A[9] / 2, 8, "最高", -6643536, 13, 1)
  ui_rect(A[9] / 2 + 46, 11, A[7], 10, -63488, 1, 0, 0)
  ui_rect(A[1], A[10] - 40, 80, 12, -63488, 1, 0, 6)
  ui_circle(A[2], A[3], 9, -131246, 1, 0)
  if (A[8] == 0) {
    ui_text(A[9] / 2, A[10] / 2, "按回车重开", -131246, 16, 1)
  }
  ui_present()
}

tick <- function() {
  if (A[8] == 0) {
    return(0)
  }
  A[2] <<- A[2] + A[4]
  A[3] <<- A[3] + A[5]
  if (A[2] < 10) {
    A[2] <<- 10
    A[4] <<- 0 - A[4]
  }
  if (A[2] > A[9] - 10) {
    A[2] <<- A[9] - 10
    A[4] <<- 0 - A[4]
  }
  if (A[3] < 30) {
    A[3] <<- 30
    A[5] <<- 0 - A[5]
  }
  # 接住：球落到挡板带上、且横向落在挡板范围内（多条件用嵌套 if —— 本前端对 && 的支持未验证）
  # Catch: the ball lands on the paddle band and is horizontally within the paddle (multiple conditions use nested if -- this frontend's support for && is unverified)
  if (A[3] > A[10] - 52) {
    if (A[3] < A[10] - 30) {
      if (A[2] > A[1] - 9) {
        if (A[2] < A[1] + 89) {
          A[5] <<- 0 - A[5]
          A[3] <<- A[10] - 52
          A[6] <<- A[6] + 10
          if (A[6] > A[7]) {
            A[7] <<- A[6]
          }
          # 音效：单音 ui_beep（v0.96.509 从音序器换回来 ——
          # Sound: single-tone ui_beep (switched back from the sequencer in v0.96.509 --
          #   那一版多声部叠加 / 长音拖尾在真机上破音）
          #   that version's multi-voice layering / long-note tails crackled on the real device)
          ui_beep(1047, 165)
        }
      }
    }
  }
  if (A[3] > A[10]) {
    A[8] <<- 0
    # 音效：单音 ui_beep；**结局音取最低音**（接住 1047 / 没接住 131，差得开）
    # Sound: single-tone ui_beep; **the ending tone takes the lowest pitch** (catch 1047 / miss 131, far enough apart)
    ui_beep(131, 320)
    draw()
    if (ui_dlg_msg("接方块", "没接住，这一局结束。\n再来一局？（选「否」退出）", 0) != 0) { ui_win_close(); return(0) }
    resetGame()
  }
  return(0)
}

# ── 主流程 ──
# -- Main flow --
A[9] <- ui_scr_w()
A[10] <- ui_scr_h()
if (A[9] <= 0) {
  A[9] <- 360
}
if (A[10] <= 0) {
  A[10] <- 620
}
ui_win_open("接方块", A[9], A[10])
ui_keep_on(1)
resetGame()
tid <- ui_timer_set(40, 0)

while (ui_win_closed() == 0) {

  draw()
  t <- ui_wait_msg(0)
  if (t == 10) {
    break
  }
  if (t == 9) {
    tick()
  }
  if (t == 1) {
    k <- ui_msg_a()
    if (k == 27) {
      break
    }
    if (k == 37) {
      A[1] <- A[1] - 20
      if (A[1] < 4) {
        A[1] <- 4
      }
    }
    if (k == 39) {
      A[1] <- A[1] + 20
      if (A[1] > A[9] - 84) {
        A[1] <- A[9] - 84
      }
    }
    if (k == 13) {
      resetGame()
    }
  }
}

ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()
