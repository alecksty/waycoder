// 贪吃蛇 —— 用 **Go** 写的手机游戏
// Snake -- a mobile game written in **Go**
//
// ✅ 能玩。判据是**逐像素量**出来的：蛇头 248 像素（1 格）、蛇身 496（2 格）、
// ✅ Playable. The criterion was measured **pixel by pixel**: snake head 248 pixels (1 cell), body 496 (2 cells),
//    食物 248（1 格），12 拍定时器后蛇头每拍右移一格、吃到食物后变长、撞墙后重开。
//    food 248 (1 cell); with a 12-tick timer the head moves one cell right per tick, grows after eating, and restarts after hitting a wall.
//
// ◆ 手机那套 UI 怎么来的
// ◆ Where the mobile UI set comes from
//
// 开窗 / 绘图 / 输入 / 定时器的实现是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
// Window opening / drawing / input / timers are implemented in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
// 由 `vmltool.config.xml` 的 `<Language Name="go" Libs="vmlui.vml">` 挂给每个前端。
// attached to every frontend by `<Language Name="go" Libs="vmlui.vml">` in `vmltool.config.xml`.
// 数值的唯一真源是 `Lib/c/waycoder_ui.h`。
// The single source of truth for the numbers is `Lib/c/waycoder_ui.h`.
//
// Go 这一路原本**跑不了任何游戏**，四处前端缺陷修完才通（`docs/前端游戏能力评估.md` §4.2）：
// The Go path originally **could not run any game**; it only worked after four frontend defects were fixed (see the frontend game capability assessment in `docs/`, section 4.2):
//   · `L0`–`L7` 是**长整数寄存器**的拼法 ⇒ `jge L0` 变成"跳寄存器"、静默不跳（死循环）
//   · `L0`-`L7` is the spelling of the **long-integer registers** => `jge L0` became "jump to a register", silently not jumping (infinite loop)
//   · 数组三处都缺：声明不分配、`a[i] = v` 没有代码生成、读取是 `MOVE R0, R0` 空操作
//   · Arrays were missing in three places: declarations did not allocate, `a[i] = v` had no code generation, and reads were a `MOVE R0, R0` no-op
//   · 栈帧只按"块里顶层语句"估算 ⇒ `for i := 0; …` 的 `i` 没算进去、帧开小了被 push 冲掉
//   · The stack frame was estimated from "top-level statements in the block" only => the `i` of `for i := 0; ...` was not counted, the frame was too small and got clobbered by pushes
//   · **控制语句头部里的复合字面量**：`for i < A[1] {` 的那个 `{` 被当成 `A[1]` 的复合
//   · **Compound literals in a control-statement header**: the `{` of `for i < A[1] {` was taken as the compound
//     字面量、把整个循环体当元素列表吃掉（报错却是 "Expected RBRACE, but got IF"）
//     literal of `A[1]` and swallowed the whole loop body as an element list (reporting "Expected RBRACE, but got IF")
//   · **形参只搬了第一个**：调用方把实参压栈，而函数序言只从 R0 取了一个，
//   · **Only the first parameter was moved**: the caller pushes the arguments, but the function prologue took only one from R0,
//     且槽位少了 CALL 自己压入的返回地址那 4 字节 ⇒ 除末参外全是垃圾
//     and the slot offsets were short by the 4 bytes of the return address CALL itself pushes => every parameter but the last was garbage
//
// ◆ 还剩两条**真的**限制（写 Go 游戏时绕开）
// ◆ Two **real** limitations remain (work around them when writing a Go game)
//
//   ① **字符串拼接编出来是空串**：`"标" + "题"` 渲染出来一个字都没有
//   ① **String concatenation compiles to an empty string**: `"a" + "b"` renders nothing at all
//      （`GenerateBinaryOp_StringConcat` 手写的那段汇编有问题 ⇒ 分数用**分数条**画，不拼字符串）。
//      (the hand-written assembly in `GenerateBinaryOp_StringConcat` is broken => the score is drawn as a **score bar**, not concatenated text).
//   ② **函数名别和标准库撞**：自定义 `func sum(...)` 会被链接到 `lib_util_sum`，
//   ② **Do not let function names collide with the standard library**: a custom `func sum(...)` gets linked to `lib_util_sum`,
//      **静默调错函数**（编得过、结果莫名其妙）⇒ 起名带上自己的前缀。
//      **silently calling the wrong function** (it compiles, the result is inexplicable) => prefix your own names.
//
// ◆ 写法说明
// ◆ Notes on the style
//
// 状态放模块级数组、辅助函数一律无参 —— 这是修上面那些缺陷时逼出来的形状，
// State lives in module-level arrays and helper functions all take no parameters -- a shape forced by fixing those defects above;
// 缺陷修完之后**参数、返回值、嵌套循环、嵌套 `if` 都已实测可用**（见 `docs/前端游戏能力评估.md` §4.2），
// after the fixes **parameters, return values, nested loops and nested `if` were all measured working** (see the frontend game capability assessment in `docs/`, section 4.2),
// 这里保留原样只是为了少一次全量重测。你要写新游戏可以按自然的 Go 写。
// and this file keeps the old shape only to avoid another full re-test. For a new game you can write natural Go.
//
// 另外一个实现选择：段表**不做环形回绕**，每走一步把各节整体后移一位（O(长度) 的搬移，
// Another implementation choice: the segment table **does not wrap around**; each step shifts every segment back by one (an O(length) move,
// 最长 40 节无所谓），换来的是所有下标都不用回绕。
// and 40 segments at most does not matter), which buys indexes that never need wrapping.
//
// ◆ 操作
// ◆ Controls
//
// 方向键转向；SELECT 暂停；回车重开；ESC 或返回箭头退出。
// Arrow keys turn; SELECT pauses; Enter restarts; ESC or the back arrow exits.
// 吃到食物 +10 分并加速；撞墙或撞到自己结束（弹对话框问要不要再来一局）。
// Eating food is +10 points and speeds up; hitting a wall or yourself ends the round (a dialog asks whether to play again).

package main

// 状态表（一个数组放全部，避免多个全局数组互相踩）
// State table (everything in one array, avoiding several global arrays clobbering each other)
//   0=head 1=len 2=dx 3=dy 4=fx 5=fy 6=score 7=best
//   8=alive 9=paused 10=stepMs 11=cell 12=ox 13=oy 14=sw 15=sh
//   16+2i=第 i 节 x，17+2i=第 i 节 y（i=0 是头；40 节上限 ⇒ 16..95）
//   16+2i = segment i's x, 17+2i = segment i's y (i=0 is the head; 40 segments max => 16..95)
//   100=候选x 101=候选y 102=格x 103=格y 104=格色 105/106=新方向 107=命中 108=临时
//   100=candidate x 101=candidate y 102=cell x 103=cell y 104=cell color 105/106=new direction 107=hit 108=temporary
//   109=退出标志 110=界面语言（0=中文 1=英文）—— 语言码**不能**存包级标量，见 draw
//   109=exit flag 110=UI language (0=Chinese 1=English) -- the language code **cannot** live in a package-level scalar, see draw
var A [112]int

// 扫一遍身上的每一节，看 (A[100],A[101]) 这格有没有被占；结果写 A[107]
// Scan every segment of the body to see whether the cell (A[100],A[101]) is occupied; the result goes into A[107]
func occupied() {
	A[107] = 0
	i := 0
	for i < A[1] {
		if A[16+i*2] == A[100] && A[17+i*2] == A[101] {
			A[107] = 1
		}
		i = i + 1
	}
}

func placeFood() {
	tries := 0
	A[100] = ui_rand(20)
	A[101] = ui_rand(18)
	occupied()
	for tries < 300 {
		if A[107] != 0 {
			A[100] = ui_rand(20)
			A[101] = ui_rand(18)
			occupied()
		}
		if A[107] == 0 {
			A[4] = A[100]
			A[5] = A[101]
			tries = 300
		}
		tries = tries + 1
	}
}

func draw() {
	// 界面语言：**只能从状态表里读**，不能读包级变量。
	// UI language: it **can only be read from the state table**, never from a package-level variable.
	// ⚠ 实测（本前端的缺陷）：`var lang int` 这种**包级标量**在函数里赋值后读回来恒 0
	// ⚠ Measured (a defect of this frontend): a **package-level scalar** like `var lang int` always reads back 0 after being assigned inside a function
	//   （`var A [112]int` 这种数组才是好的）⇒ 语言码统一寄在 A[110]（该槽没人用），
	//   (an array like `var A [112]int` is fine) => the language code is parked in A[110] (nothing else uses that slot),
	//   开局在 main 里问一次宿主，函数里取个局部量再分支。
	//   asking the host once in main at startup, then branching on a local copy inside functions.
	lang := A[110]
	tScore := "Score"
	tBest := "Best"
	tPaused := "Paused (SELECT)"
	if lang == 0 { tScore = "得分" }
	if lang == 0 { tBest = "最高" }
	if lang == 0 { tPaused = "暂停（SELECT 继续）" }

	ui_clear(-15724520)

	// 棋盘格：扁平序号 0..359，行列内联算出来；每格按 (r+c) 的奇偶决定铺不铺
	// Board cells: flat indexes 0..359, with row/column computed inline; whether to fill a cell depends on the parity of (r+c)
	cellN := 0
	for cellN < 360 {
		if (cellN+cellN/20)-((cellN+cellN/20)/2)*2 == 0 {
			ui_rect(A[12]+(cellN-(cellN/20)*20)*A[11], A[13]+(cellN/20)*A[11], A[11]-1, A[11]-1, -15263713, 1, 0, 2)
		}
		cellN = cellN + 1
	}

	// 食物
	// Food
	ui_rect(A[12]+A[4]*A[11], A[13]+A[5]*A[11], A[11]-1, A[11]-1, -131246, 1, 0, 2)

	// 蛇：i=0 是头
	// Snake: i=0 is the head
	i := 0
	for i < A[1] {
		if i == 0 {
			ui_rect(A[12]+A[16+i*2]*A[11], A[13]+A[17+i*2]*A[11], A[11]-1, A[11]-1, -63488, 1, 0, 2)
		}
		if i != 0 {
			ui_rect(A[12]+A[16+i*2]*A[11], A[13]+A[17+i*2]*A[11], A[11]-1, A[11]-1, -11409298, 1, 0, 2)
		}
		i = i + 1
	}

	ui_text(8, 8, tScore, -6643536, 13, 0)
	ui_rect(58, 11, A[6], 10, -11409298, 1, 0, 0)
	ui_text(A[14]/2, 8, tBest, -6643536, 13, 1)
	ui_rect(A[14]/2+46, 11, A[7], 10, -63488, 1, 0, 0)
	if A[9] != 0 {
		ui_text(A[14]/2, A[15]/2, tPaused, -131246, 16, 1)
	}
	ui_present()
}

func reset() {
	A[1] = 3
	A[0] = 2
	A[16] = 8
	A[17] = 8
	A[18] = 7
	A[19] = 8
	A[20] = 6
	A[21] = 8
	A[2] = 1
	A[3] = 0
	A[6] = 0
	A[8] = 1
	A[9] = 0
	A[10] = 170
	placeFood()
}

func gameOver() {
	lang := A[110]
	tTitle := "Snake"
	tOver := "You crashed. Round over.\nPlay again? (choose \"No\" to quit)"
	if lang == 0 { tTitle = "贪吃蛇" }
	if lang == 0 { tOver = "撞到了，这一局结束。\n再来一局？（选「否」退出）" }
	A[8] = 0
	// 音效：单音 ui_beep；**结局音取最低音**（吃到 1047 / 撞到 131，差得开）
	// Sound: single-tone ui_beep; **the ending tone takes the lowest pitch** (eating 1047 / crashing 131, far enough apart)
	//   v0.96.509 从音序器换回来 —— 那一版多声部叠加 / 长音在真机上破音
	//   switched back from the sequencer in v0.96.509 -- that version's multi-voice layering / long notes crackled on the real device
	ui_beep(131, 320)
	draw()
	// 选「否/拒绝」→ 退出游戏（ui_dlg_msg 返回 0=是 / 1=否）。
	// Choosing "No / reject" -> quit the game (ui_dlg_msg returns 0=yes / 1=no).
	// 此前不接返回值 ⇒ 两个按钮一个样、游戏还退不出去（用户实测报的）。
	// The return value used to be ignored => both buttons behaved the same and the game could not be exited (reported by the user on a real device).
	// 用 A[109] 这个没人用的槽当退出标志 —— gameOver 有四五处调用点，逐个改返回值不划算。
	// A[109], an unused slot, serves as the exit flag -- gameOver has four or five call sites and changing each return value was not worth it.
	if ui_dlg_msg(tTitle, tOver, 0) != 0 {
		A[109] = 1
		return
	}
	reset()
}

func step() {
	if A[8] == 0 || A[9] != 0 {
		return
	}
	nc := A[16] + A[2]
	nr := A[17] + A[3]
	if nc < 0 || nc >= 20 || nr < 0 || nr >= 18 {
		gameOver()
		return
	}
	// 自撞：只看前 len-1 节（尾巴这一步会挪走）
	// Self-collision: only check the first len-1 segments (the tail moves away in this step)
	A[100] = nc
	A[101] = nr
	A[107] = 0
	i := 0
	for i < A[1]-1 {
		if A[16+i*2] == nc && A[17+i*2] == nr {
			A[107] = 1
		}
		i = i + 1
	}
	if A[107] != 0 {
		gameOver()
		return
	}

	// 整体后移一位（不做环形回绕，见文件头「写法说明」）
	// Shift everything back by one (no wrap-around; see "Notes on the style" in the file header)
	i = A[1]
	for i > 0 {
		A[16+i*2] = A[16+(i-1)*2]
		A[17+i*2] = A[17+(i-1)*2]
		i = i - 1
	}
	A[16] = nc
	A[17] = nr

	if nc == A[4] && nr == A[5] {
		if A[1] < 40 {
			A[1] = A[1] + 1
		}
		A[6] = A[6] + 10
		if A[6] > A[7] {
			A[7] = A[6]
		}
		if A[10] > 70 {
			A[10] = A[10] - 6
		}
		// 音效：单音 ui_beep（吃到食物：一声高而短的「叮」）
		// Sound: single-tone ui_beep (eating food: one high, short "ding")
		ui_beep(1047, 165)
		placeFood()
	}
}

func turn() {
	if A[105]+A[2] == 0 && A[106]+A[3] == 0 {
		return
	}
	A[2] = A[105]
	A[3] = A[106]
}

func main() {
	w := ui_scr_w()
	h := ui_scr_h()
	if w <= 0 {
		w = 360
	}
	if h <= 0 {
		h = 620
	}
	A[14] = w
	A[15] = h
	// 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），存进 A[110]（见 draw 的说明）。
	// UI language: ask the host once at startup whether Chinese or English (0=Chinese 1=English) and store it in A[110] (see the note in draw).
	// ⚠ 别在每帧里调 —— 那是一次 syscall。
	// ⚠ Do not call it every frame -- that is a syscall.
	A[110] = ui_get_language()
	lang := A[110]
	tTitle := "Snake"
	if lang == 0 { tTitle = "贪吃蛇" }
	ui_win_open(tTitle, w, h)

	// 版面算一次，画与判定共用
	// The layout is computed once and shared by drawing and hit-testing
	byw := w - 8
	byh := h - 46
	A[11] = byw / 20
	if byh/18 < A[11] {
		A[11] = byh / 18
	}
	if A[11] < 4 {
		A[11] = 4
	}
	A[12] = (w - A[11]*20) / 2
	A[13] = 40

	ui_keep_on(1)
	reset()

	tid := ui_timer_set(A[10], 0)
	curMs := A[10]

	for ui_win_closed() == 0 {

		if A[109] != 0 {
			break
		}
		draw()
		t := ui_wait_msg(0)
		if t == 10 {
			break
		}
		if t == 9 {
			step()
			if A[10] != curMs {
				ui_timer_kill(tid)
				curMs = A[10]
				tid = ui_timer_set(curMs, 0)
			}
		}
		if t == 1 {
			k := ui_msg_a()
			if k == 27 {
				break
			}
			if k == 37 {
				A[105] = -1
				A[106] = 0
				turn()
			}
			if k == 39 {
				A[105] = 1
				A[106] = 0
				turn()
			}
			if k == 38 {
				A[105] = 0
				A[106] = -1
				turn()
			}
			if k == 40 {
				A[105] = 0
				A[106] = 1
				turn()
			}
			if k == 13 {
				reset()
			}
			if k == 16 {
				A[9] = 1 - A[9]
			}
		}
	}

	ui_timer_kill(tid)
	ui_keep_on(0)
	ui_win_close()
}
