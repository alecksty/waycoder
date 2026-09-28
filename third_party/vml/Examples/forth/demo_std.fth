\ demo_std.fth —— Forth 第一层：**标准输入输出**（std）
\ demo_std.fth —— Forth layer one: **standard input/output** （std）
\
\ 这一层只用 Forth 自己的输出词：`." …"`（打印字面量）、`.`（打印栈顶）、
\ This layer uses only Forth's own output words: `." …"` （print a literal）, `.` （print the top of the stack）,
\ `CR`（换行）、`EMIT`（打印一个字符）。不碰 Crt / 图形 / 宿主 `ui_*` ——
\ `CR` （newline）, `EMIT` （print one character）. It touches neither Crt / graphics / the host `ui_*` ——
\ 所以它在任何后端上都是同一份行为，也是四份 demo 里唯一有"逐字节确定性输出"
\ so its behavior is the same on every backend, and it is the only one of the four demos with a "byte-for-byte deterministic output"
\ 判据的一份。
\ criterion.
\
\ 跑法（桌面 vmlcli）：
\   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/forth/demo_std.fth
\
\ 期望输出（逐字节）：
\ Expected output （byte-for-byte; each line below is printed in Chinese and the line under it is its translation）:
\   === Forth 标准输出 demo ===
\   === Forth std output demo ===
\   字符串: 你好，世界
\   String: hello, world
\   整数: 42
\   Integer: 42
\   计算: 7 * 6 = 42
\   Computed: 7 * 6 = 42
\   整除: 17 5 / = 3
\   Integer division: 17 5 / = 3
\   循环求和: 1..10 = 55
\   Loop sum: 1..10 = 55
\   阶乘: 10! = 3628800
\   Factorial: 10! = 3628800
\   === done ===
\
\ ◆ 三条本前端的写法要求（都是实测出来的）
\ ◆ Three writing requirements of this frontend （all measured in practice）
\
\   ① **栈是唯一的参数通道**：`17 5 /` 把 17、5 先后压栈再除。`.` 会吃掉栈顶，
\   ① **The stack is the only parameter channel**: `17 5 /` pushes 17 and 5 and then divides. `.` eats the top of the stack,
\      所以一行里**只能有一个 `.`** —— 要打两个数就分两行（或 `SWAP`）。
\      so there can be **only one `.` per line** —— to print two numbers, use two lines （or `SWAP`）.
\
\   ② 循环用 `DO … LOOP`，循环变量是 `I`：
\   ② Loops use `DO … LOOP`, and the loop variable is `I`:
\        `0  11 1 DO I + LOOP`   从 1 加到 10（Forth 的 `DO` 上限**不含**）
\        `0  11 1 DO I + LOOP`   sums from 1 to 10 （Forth's `DO` upper bound is **exclusive**）
\      写成词定义更清楚：`: SUM10 0 11 1 DO I + LOOP ;` 然后 `SUM10 .`
\      written as a word definition it is clearer: `: SUM10 0 11 1 DO I + LOOP ;` then `SUM10 .`
\
\   ③ ⚠ **本前端的 `MOD` 是坏的：它编成了加法**（实测）。
\   ③ ⚠ **This frontend's `MOD` is broken: it compiles into addition** （measured）.
\       10 3 MOD   ⇒ 13        （应 1）
\       10 3 MOD   ⇒ 13        （should be 1）
\       100 7 MOD  ⇒ 107       （应 2）
\       100 7 MOD  ⇒ 107       （should be 2）
\      根因在生成器：`CodeGenerator.Operations.cs` 把 `TokenType.MOD` 映射成字符串
\      The root cause is in the generator: `CodeGenerator.Operations.cs` maps `TokenType.MOD` to the string
\      `"MOD"`，而基类 `GetArithmeticInstruction(op, type)` 的那张表认的是 `"%"`,
\      `"MOD"`, while the table in the base class `GetArithmeticInstruction（op, type）` only recognizes `"%"`,
\      没有 `"MOD"` 这一项 ⇒ 落到 `_ => OpCode.ADD`。生成的汇编里那一句就是
\      and has no `"MOD"` entry ⇒ it falls through to `_ => OpCode.ADD`. That line in the generated assembly is
\        `add @R0 @R1`，一个错都不报。
\        `add @R0 @R1`, and not a single error is reported.
\      ⇒ 所以本文件里**一次取模都没有**；要取模眼下只能自己减（或先修前端）。
\      ⇒ so this file takes **not one modulus**; for now the only way to take one is to subtract by hand （or fix the frontend first）.
\
\ ◆ 顺带记一条同样是实测的边界：**多参数词不可靠**。
\ ◆ One more measured boundary, noted in passing: **multi-parameter words are unreliable**.
\   `: ADD2 ( a b -- c ) + ;  5 7 ADD2 .` 打出 **7**（应 12）—— 词调用那套
\   `: ADD2 （ a b -- c ） + ;  5 7 ADD2 .` prints **7** （should be 12） —— the word-call sequence
\   "保存 R15 / 压回结果"的序列在**参数多于一个**时会串位（返回值也不可靠：
\   of "save R15 / push the result back" goes out of step when there is **more than one parameter** —— return values are unreliable too:
\   `100 ui_rand .` 打出的是 100，即"把实参原样吐回来了"）。
\   `100 ui_rand .` prints 100, i.e. "it spits the argument back out unchanged".
\   本文件里所有词都是**零参或一参**，走的是能对的那条路。
\   Every word in this file takes **zero or one parameter**, which is the path that works.

\ 界面语言：0 = 中文 / 1 = 英文（ui_get_language 是 syscall，开局查一次存进 LANG）
\ UI language: 0 = Chinese / 1 = English (ui_get_language is a syscall: query it once at startup into LANG)
VARIABLE LANG
ui_get_language LANG !

LANG @ 0= IF ." === Forth 标准输出 demo ===" ELSE ." === Forth std output demo ===" THEN CR

\ ① 字符串字面量（含中文 —— 源码按 UTF-8 存）
\ ① string literals （including Chinese —— the source is stored as UTF-8）
LANG @ 0= IF ." 字符串: 你好，世界" ELSE ." String: hello, world" THEN CR

\ ② 整数
\ ② integer
LANG @ 0= IF ." 整数: " ELSE ." Integer: " THEN 42 . CR

\ ③ 整数运算
\ ③ integer arithmetic
LANG @ 0= IF ." 计算: 7 * 6 = " ELSE ." Computed: 7 * 6 = " THEN 7 6 * . CR

\ ④ 整除
\ ④ integer division
LANG @ 0= IF ." 整除: 17 5 / = " ELSE ." Integer division: 17 5 / = " THEN 17 5 / . CR

\ ⑤ 循环求和 1..10（词定义里做，返回值落在栈上）
\ ⑤ loop sum 1..10 （done inside a word definition; the return value lands on the stack）
: SUM10 ( -- n )  0 11 1 DO I + LOOP ;
LANG @ 0= IF ." 循环求和: 1..10 = " ELSE ." Loop sum: 1..10 = " THEN SUM10 . CR

\ ⑥ 循环求阶乘 10!
\ ⑥ loop factorial 10!
: FAC10 ( -- n )  1 11 1 DO I * LOOP ;
LANG @ 0= IF ." 阶乘: 10! = " ELSE ." Factorial: 10! = " THEN FAC10 . CR

." === done ===" CR
