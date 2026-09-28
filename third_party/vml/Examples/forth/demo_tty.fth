\ demo_tty.fth —— Forth 第二层：**彩色控制台**（tty）
\ demo_tty.fth —— Forth layer two: **color console** （tty）
\
\ Forth 这边**没有 conio / crt / ANSI 辅助词**（`Lib/forth/gfx.fth` 与
\ On the Forth side there are **no conio / crt / ANSI helper words** —— `Lib/forth/gfx.fth` and
\ `Lib/forth/sys.fth` 里那些词体是 `asm("SYSCALL 80")` 一类写法，而**本前端不支持
\ `Lib/forth/sys.fth` have word bodies written as `asm（"SYSCALL 80"）` and the like, while **this frontend does not support
\ `asm()`** —— 用它会报「未定义的函数 'word_asm'」，所以那两个文件在这个前端下编不过、
\ the bare `asm` keyword** —— using it reports "undefined function 'word_asm'", so those two files do not compile on this frontend,
\ 也不是给它们用的）。所以这一层**直接发 ANSI 转义序列**，一个字节一个字节地用
\ these two files are not meant for this frontend anyway. So this layer **emits ANSI escape sequences directly**, byte by byte with
\ `EMIT` 送出去 —— 与 `Examples/c/ansi_colors.c` 同一个做法、同一套效果。
\ `EMIT` —— the same approach and the same effect as `Examples/c/ansi_colors.c`.
\
\ 跑法（桌面 vmlcli）：
\   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/forth/demo_tty.fth
\
\ 画面（从上到下）：
\ The screen （top to bottom）:
\   ① 清屏（ESC[2J + ESC[H）+ 复位属性
\   ① clear the screen （ESC[2J + ESC[H） + reset the attributes
\   ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰
\   ② 8 dark foreground colors （color numbers 0-7）, background = 7 light gray
\   ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
\   ③ 8 bright foreground colors （color numbers 8-15）, background = 0 black
\   ④ 光标定位：同一行先写右半段、再回到左端写左半段 —— 证明是**定位**不是顺序输出
\   ④ cursor positioning: on the same row write the right half first, then go back to the left end and write the left half —— proving it is **positioning**, not sequential output
\   ⑤ 复位成白字黑底，正常结束
\   ⑤ reset to white on black, finishing normally
\
\ ═══════════════════════════════════════════════════════════════════════
\  ⚠ 这份文件为什么写得这么"素"（本前端的四条硬限制，全实测过）
\  ⚠ Why this file is written so "plainly" （four hard limits of this frontend, all measured in practice）
\
\  ① **只有零参 / 一参的词才可靠**。
\  ① **Only words with zero or one parameter are reliable**.
\       `: ADD2 ( a b -- c ) + ;  5 7 ADD2 .`   打出 **7**（应 12）
\       `: ADD2 （ a b -- c ） + ;  5 7 ADD2 .`   prints **7**（should be 12）
\       ⇒ **两个以上参数会串位**。所以每个辅助词都是零参的，颜色、行列全部
\       ⇒ **more than two parameters go out of step**. So every helper word takes zero parameters, and colors, rows and columns are all
\       **以字面量形式**写在调用点。
\       **written as literals** at the call site.
\
\  ② **词体里禁止"带参数 + 再调别的词"**。实测最小复现：
\       : TEN ( n -- t ) 10 / ;
\       : SGR ( n -- ) CSI DUP TEN ... 109 EMIT ;   33 SGR ." B" CR
\     结果只发出 `ESC[33m`，**其后整个顶层程序被静默丢弃**（`B` 与 `CR` 都不见了）。
\     the result emits only `ESC[33m`, and **the whole top-level program after it is silently dropped** （both `B` and `CR` are gone）.
\      同一份程序把 `TEN` 去掉、改成词体内联算术，`B`/`CR` 虽然回来了，
\      In the same program, removing `TEN` and inlining the arithmetic in the word body brings `B`/`CR` back,
\      但数字算成了乱码。⇒ 词体里只留"EMIT 若干字面量"这一种形态。
\      but the numbers come out garbled. ⇒ keep only the "EMIT a few literals" form inside a word body.
\
\  ③ **取模 `MOD` 是坏的：它编成了加法**。
\  ③ **Modulus `MOD` is broken: it compiles into addition**.
\       `10 3 MOD` ⇒ 13、`100 7 MOD` ⇒ 107（生成器把 `TokenType.MOD` 映射成字符串
\       `10 3 MOD` ⇒ 13, `100 7 MOD` ⇒ 107 —— the generator maps `TokenType.MOD` to the string
\       "MOD"，而基类那张表只认 "%" ⇒ 落到 `_ => OpCode.ADD`，汇编里就是 `add`）。
\       "MOD", while the base class's table only recognizes "%" ⇒ it falls through to `_ => OpCode.ADD`, which in the assembly is just `add`.
\      ⇒ 本文件的"两位数字"是靠**字面量拼**出来的，不做任何取模。
\      ⇒ this file's "two-digit numbers" are **assembled from literals**; no modulus is taken at all.
\
\  ④ 词名避开已有库符号（`H` / `M` / `CSI` 这些都先查过 `Lib/forth/*.vml` 没有重名）。
\  ④ Word names avoid existing library symbols （`H` / `M` / `CSI` and the like were all checked against `Lib/forth/*.vml` for duplicates first）.
\
\  另外：`." …"` 里的中文没问题（走 stdout 那条会解码 UTF-8 的路），可以放心用。
\  Also: Chinese inside `." …"` is fine （it takes the stdout path that decodes UTF-8）, so feel free to use it.
\ ═══════════════════════════════════════════════════════════════════════

\ ── ANSI 骨架辅助词（全部零参，词体里只有字面量 + EMIT）────────────
\ ── ANSI skeleton helper words （all zero-parameter; the body holds only literals + EMIT） ────────────
: ESC   ( -- ) 27 EMIT ;
: CSI   ( -- ) ESC 91 EMIT ;
: SEMI  ( -- ) 59 EMIT ;          \ ';'
: H     ( -- ) 72 EMIT ;          \ 'H'  光标定位结尾
\ 'H': the character that ends a cursor-positioning sequence
: SGRM  ( -- ) 109 EMIT ;         \ 'm'  颜色序列结尾
\ 'm': the character that ends a color sequence
: RESET ( -- ) CSI 48 EMIT SGRM ; \ ESC[0m
: CLS   ( -- ) CSI 50 EMIT 74 EMIT CSI H ;   \ ESC[2J ESC[H

\ ── ① 清屏 ──────────────────────────────────────────────────────
\ ── ① clear the screen ──────────────────────────────────────────────────
\ 界面语言：0 = 中文 / 1 = 英文（ui_get_language 是 syscall，开局查一次存进 LANG）
\ UI language: 0 = Chinese / 1 = English (ui_get_language is a syscall: query it once at startup into LANG)
VARIABLE LANG
ui_get_language LANG !
RESET CLS

\ ── 标题：亮白字(97) + 蓝底(44)：ESC[97;44m ─────────────────────
\ ── Title: bright white text （97） + blue background （44）: ESC[97;44m ─────────────────────
CSI 57 EMIT 55 EMIT SEMI 52 EMIT 52 EMIT SGRM
." === Forth color console demo ===  (ANSI 97;44 = bright white on blue)" CR

\ ── ② 暗色 0-7（行 10-17，第 3 列；色 30-37，背景 47）────────────
\ ── ② dark colors 0-7 （rows 10-17, column 3; colors 30-37, background 47） ────────────
\   行号 = 1<0+I>（I 走 0..7）、色号 = 3<0+I>
\   row number = 1<0+I> （I runs 0..7）, color number = 3<0+I>
8 0 DO
  CSI 49 EMIT 48 I + EMIT SEMI 51 EMIT H          \ ESC[1<0+I>;3H
  CSI 51 EMIT 48 I + EMIT SEMI 52 EMIT 55 EMIT SGRM  \ ESC[3<0+I>;47m
  LANG @ 0= IF ." 前景色 暗色（0-7），背景 = 7 浅灰" ELSE ." foreground dark (0-7), background = 7 light gray" THEN CR
LOOP

\ ── ③ 亮色 8-15（行 20-27，第 3 列；色 90-97，背景 40）──────────
\ ── ③ bright colors 8-15 （rows 20-27, column 3; colors 90-97, background 40） ──────────
\   行号 = 2<0+I>（避开上面 10-17 那几行）、色号 = 9<0+I>
\   row number = 2<0+I> （avoiding the rows 10-17 above）, color number = 9<0+I>
8 0 DO
  CSI 50 EMIT 48 I + EMIT SEMI 51 EMIT H          \ ESC[2<0+I>;3H
  CSI 57 EMIT 48 I + EMIT SEMI 52 EMIT 48 EMIT SGRM  \ ESC[9<0+I>;40m
  LANG @ 0= IF ." 前景色 亮色（8-15），背景 = 0 黑" ELSE ." foreground bright (8-15), background = 0 black" THEN CR
LOOP

\ ── ④ 光标定位：第 29 行第 34 列先写，再回第 29 行第 1 列写 ──────
\ ── ④ cursor positioning: write at row 29 column 34 first, then back at row 29 column 1 ──────
CSI 50 EMIT 57 EMIT SEMI 51 EMIT 52 EMIT H        \ ESC[29;34H
CSI 57 EMIT 51 EMIT SEMI 52 EMIT 49 EMIT SGRM     \ ESC[93;41m 亮黄 / 红底
\ ESC[93;41m bright yellow on red background
LANG @ 0= IF ." <- 先写的（第 34 列）" ELSE ." <- written first (column 34)" THEN CR

CSI 50 EMIT 57 EMIT SEMI 49 EMIT H                \ ESC[29;1H
CSI 57 EMIT 54 EMIT SEMI 52 EMIT 48 EMIT SGRM     \ ESC[96;40m 亮青 / 黑底
\ ESC[96;40m bright cyan on black background
LANG @ 0= IF ." 后写的（第 1 列）-> " ELSE ." written second (column 1) -> " THEN CR

\ ── ⑤ 收尾：第 31 行第 1 列，复位成白字黑底（37 / 40）──────────
\ ── ⑤ wrap-up: at row 31 column 1, reset to white on black （37 / 40） ──────────
CSI 51 EMIT 49 EMIT SEMI 49 EMIT H                \ ESC[31;1H
CSI 51 EMIT 55 EMIT SEMI 52 EMIT 48 EMIT SGRM     \ ESC[37;40m
LANG @ 0= IF ." === done（已复位为白字黑底）===" ELSE ." === done (reset to white on black) ===" THEN CR
