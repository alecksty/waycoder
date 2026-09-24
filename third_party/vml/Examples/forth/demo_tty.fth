\ demo_tty.fth —— Forth 第二层：**彩色控制台**（tty）
\
\ Forth 这边**没有 conio / crt / ANSI 辅助词**（`Lib/forth/gfx.fth` 与
\ `Lib/forth/sys.fth` 里那些词体是 `asm("SYSCALL 80")` 一类写法，而**本前端不支持
\ `asm()`** —— 用它会报「未定义的函数 'word_asm'」，所以那两个文件在这个前端下编不过、
\ 也不是给它们用的）。所以这一层**直接发 ANSI 转义序列**，一个字节一个字节地用
\ `EMIT` 送出去 —— 与 `Examples/c/ansi_colors.c` 同一个做法、同一套效果。
\
\ 跑法（桌面 vmlcli）：
\   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/forth/demo_tty.fth
\
\ 画面（从上到下）：
\   ① 清屏（ESC[2J + ESC[H）+ 复位属性
\   ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰
\   ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
\   ④ 光标定位：同一行先写右半段、再回到左端写左半段 —— 证明是**定位**不是顺序输出
\   ⑤ 复位成白字黑底，正常结束
\
\ ═══════════════════════════════════════════════════════════════════════
\  ⚠ 这份文件为什么写得这么"素"（本前端的四条硬限制，全实测过）
\
\  ① **只有零参 / 一参的词才可靠**。
\       `: ADD2 ( a b -- c ) + ;  5 7 ADD2 .`   打出 **7**（应 12）
\       ⇒ **两个以上参数会串位**。所以每个辅助词都是零参的，颜色、行列全部
\       **以字面量形式**写在调用点。
\
\  ② **词体里禁止"带参数 + 再调别的词"**。实测最小复现：
\       : TEN ( n -- t ) 10 / ;
\       : SGR ( n -- ) CSI DUP TEN ... 109 EMIT ;   33 SGR ." B" CR
\     结果只发出 `ESC[33m`，**其后整个顶层程序被静默丢弃**（`B` 与 `CR` 都不见了）。
\      同一份程序把 `TEN` 去掉、改成词体内联算术，`B`/`CR` 虽然回来了，
\      但数字算成了乱码。⇒ 词体里只留"EMIT 若干字面量"这一种形态。
\
\  ③ **取模 `MOD` 是坏的：它编成了加法**。
\       `10 3 MOD` ⇒ 13、`100 7 MOD` ⇒ 107（生成器把 `TokenType.MOD` 映射成字符串
\       "MOD"，而基类那张表只认 "%" ⇒ 落到 `_ => OpCode.ADD`，汇编里就是 `add`）。
\      ⇒ 本文件的"两位数字"是靠**字面量拼**出来的，不做任何取模。
\
\  ④ 词名避开已有库符号（`H` / `M` / `CSI` 这些都先查过 `Lib/forth/*.vml` 没有重名）。
\
\  另外：`." …"` 里的中文没问题（走 stdout 那条会解码 UTF-8 的路），可以放心用。
\ ═══════════════════════════════════════════════════════════════════════

\ ── ANSI 骨架辅助词（全部零参，词体里只有字面量 + EMIT）────────────
: ESC   ( -- ) 27 EMIT ;
: CSI   ( -- ) ESC 91 EMIT ;
: SEMI  ( -- ) 59 EMIT ;          \ ';'
: H     ( -- ) 72 EMIT ;          \ 'H'  光标定位结尾
: SGRM  ( -- ) 109 EMIT ;         \ 'm'  颜色序列结尾
: RESET ( -- ) CSI 48 EMIT SGRM ; \ ESC[0m
: CLS   ( -- ) CSI 50 EMIT 74 EMIT CSI H ;   \ ESC[2J ESC[H

\ ── ① 清屏 ──────────────────────────────────────────────────────
RESET CLS

\ ── 标题：亮白字(97) + 蓝底(44)：ESC[97;44m ─────────────────────
CSI 57 EMIT 55 EMIT SEMI 52 EMIT 52 EMIT SGRM
." === Forth color console demo ===  (ANSI 97;44 = bright white on blue)" CR

\ ── ② 暗色 0-7（行 10-17，第 3 列；色 30-37，背景 47）────────────
\   行号 = 1<0+I>（I 走 0..7）、色号 = 3<0+I>
8 0 DO
  CSI 49 EMIT 48 I + EMIT SEMI 51 EMIT H          \ ESC[1<0+I>;3H
  CSI 51 EMIT 48 I + EMIT SEMI 52 EMIT 55 EMIT SGRM  \ ESC[3<0+I>;47m
  ." 前景色 暗色（0-7），背景 = 7 浅灰" CR
LOOP

\ ── ③ 亮色 8-15（行 20-27，第 3 列；色 90-97，背景 40）──────────
\   行号 = 2<0+I>（避开上面 10-17 那几行）、色号 = 9<0+I>
8 0 DO
  CSI 50 EMIT 48 I + EMIT SEMI 51 EMIT H          \ ESC[2<0+I>;3H
  CSI 57 EMIT 48 I + EMIT SEMI 52 EMIT 48 EMIT SGRM  \ ESC[9<0+I>;40m
  ." 前景色 亮色（8-15），背景 = 0 黑" CR
LOOP

\ ── ④ 光标定位：第 29 行第 34 列先写，再回第 29 行第 1 列写 ──────
CSI 50 EMIT 57 EMIT SEMI 51 EMIT 52 EMIT H        \ ESC[29;34H
CSI 57 EMIT 51 EMIT SEMI 52 EMIT 49 EMIT SGRM     \ ESC[93;41m 亮黄 / 红底
." <- 先写的（第 34 列）" CR

CSI 50 EMIT 57 EMIT SEMI 49 EMIT H                \ ESC[29;1H
CSI 57 EMIT 54 EMIT SEMI 52 EMIT 48 EMIT SGRM     \ ESC[96;40m 亮青 / 黑底
." 后写的（第 1 列）-> " CR

\ ── ⑤ 收尾：第 31 行第 1 列，复位成白字黑底（37 / 40）──────────
CSI 51 EMIT 49 EMIT SEMI 49 EMIT H                \ ESC[31;1H
CSI 51 EMIT 55 EMIT SEMI 52 EMIT 48 EMIT SGRM     \ ESC[37;40m
." === done（已复位为白字黑底）===" CR
