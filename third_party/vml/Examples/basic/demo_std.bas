' demo_std.bas —— BASIC 第一层：**标准输入输出**（std）
' demo_std.bas -- BASIC layer one: **standard input/output** (std)
'
' 这一层只用语言自己的 `PRINT`，不碰任何图形 / 控制台 / 宿主 UI 接口 ——
' This layer uses only the language's own `PRINT` and touches no graphics / console / host UI API --
' 所以它在**任何**后端上都是同一份行为，也是四份 demo 里唯一有
' so it behaves identically on **any** backend, and it is the only one of the four demos that has
' "逐字节确定性输出" 判据的一份（另外三份只能验"编得过、跑得完、不挂死"）。
' a "byte-for-byte deterministic output" probe (the other three can only be checked for "it compiles, it finishes, it does not hang").
'
' 跑法（桌面 vmlcli）：
' How to run it (desktop vmlcli):
'   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/basic/demo_std.bas
'
' 期望输出（逐字节，见文件末尾的 `demo_std.expected.txt` 对拍脚本）：
' Expected output (byte for byte; see the `demo_std.expected.txt` diff script at the end of the file):
'   === BASIC 标准输出 demo ===
'   === BASIC standard-output demo ===
'   字符串: 你好，世界
'   string: hello, world  (the label and the text are printed in Chinese)
'   整数: 42
'   integer: 42
'   计算: 7 * 6 = 42
'   arithmetic: 7 * 6 = 42
'   整数除法: 17 \ 5 = 3
'   integer division: 17 \ 5 = 3
'   取余: 17 MOD 5 = 2
'   remainder: 17 MOD 5 = 2
'   浮点(×100): 4.75 * 100 = 475
'   float (x100): 4.75 * 100 = 475
'   循环求和: 1..10 = 55
'   loop sum: 1..10 = 55
'   阶乘: 10! = 3628800
'   factorial: 10! = 3628800
'   === done ===
'
' ◆ 本前端的三条写法要求（都是实测出来的，不是风格偏好）
' ◆ Three syntax requirements of this frontend (all of them measured, not style preferences)
'   ① `PRINT "…"; 值` —— **分号不加分隔符**，末尾自动补换行（见 whack.bas 的说明）。
'   ① `PRINT "..."; value` -- **the semicolon adds no separator**, a newline is appended at the end (see the notes in whack.bas).
'   ② 模块级变量必须在赋值前 `DIM`。
'   ② Module-level variables must be `DIM`ed before they are assigned.
'   ③ 整除用 `\`、取余用 `MOD`（QBasic 方言，`BasicDialect=qbasic`）。
'   ③ Use `\` for integer division and `MOD` for the remainder (QBasic dialect, `BasicDialect=qbasic`).
'
' ◆ 本前端**浮点的三处已知缺陷**（本文件刻意绕开，不是没想到）
' ◆ **Three known float defects** in this frontend (this file deliberately avoids them; it is not that we did not think of them)
'   都在 `Examples/basic/demo_std.bas` 这条链上实测过，最小复现：
'   All of them were measured on the `Examples/basic/demo_std.bas` chain; minimal reproductions:
'
'     DIM x AS SINGLE
'     x = 4.75        : PRINT x * 100     ⇒ 0      （应 475 —— **浮点字面量赋给变量即丢值**）
'     x = 4.75        : PRINT x * 100     => 0      (expected 475 -- **assigning a float literal to a variable loses the value**)
'     PRINT 4.75                          ⇒ 4      （**PRINT 浮点按整数截断**，值本身是对的）
'     PRINT 4.75                          => 4      (**PRINT truncates a float to an integer**; the value itself is correct)
'     PRINT 10 / 4                        ⇒ 2      （QBasic 的 `/` 是**浮点除**，应 2.5；
'     PRINT 10 / 4                        => 2      (QBasic's `/` is **float division**, expected 2.5;
'                                                    只有 `\` 才是整除）
'                                                    only `\` is integer division)
'
'   第一条最要命：**浮点变量读回来恒为 0**（SINGLE / DOUBLE 都一样）。
'   The first one hurts the most: **a float variable always reads back as 0** (SINGLE and DOUBLE alike).
'   所以这里只演示"浮点字面量直接参与运算"这条能走通的路径 —— `4.75 * 100` 算出来
'   So here we only demonstrate the one path that works -- "a float literal used directly in an expression" -- `4.75 * 100` really
'   确实是 475（值正确），只是不能存进变量、也不能直接 PRINT。
'   does evaluate to 475 (the value is correct); it just cannot be stored in a variable, and cannot be PRINTed directly either.

' ── 外部库声明（NATIVE FUNCTION = 裸标签，链接期才找得到；这里只为 ui_get_language）────
' ── External library declaration (NATIVE FUNCTION = the bare label, which is what the linker can find; only ui_get_language here)────
NATIVE FUNCTION ui_get_language() AS INTEGER
END FUNCTION

' ── 变量声明（必须在赋值之前）────────────────────────────────
' ── Variable declarations (must come before any assignment) ────────────────────────────────
DIM i AS INTEGER
DIM sum AS INTEGER
DIM fact AS INTEGER
DIM a AS INTEGER
DIM b AS INTEGER
DIM LANG AS INTEGER

' 界面语言：0 = 中文 / 1 = 英文（ui_get_language 是 syscall，开局查一次）
' UI language: 0 = Chinese / 1 = English (ui_get_language is a syscall, queried once at startup)
LANG = ui_get_language()

IF LANG = 0 THEN PRINT "=== BASIC 标准输出 demo ===" ELSE PRINT "=== BASIC standard-output demo ==="

' ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通）
' ① String literal (contains Chinese -- the source is stored as UTF-8 and the lexer passes it straight through)
IF LANG = 0 THEN PRINT "字符串: 你好，世界" ELSE PRINT "string: hello, world"

' ② 整数
' ② Integer
i = 42
IF LANG = 0 THEN PRINT "整数: "; i ELSE PRINT "integer: "; i

' ③ 整数运算
' ③ Integer arithmetic
a = 7
b = 6
IF LANG = 0 THEN PRINT "计算: 7 * 6 = "; a * b ELSE PRINT "arithmetic: 7 * 6 = "; a * b

' ④ 整除与取余（QBasic 方言：`\` = 整除，`MOD` = 取余）
' ④ Integer division and remainder (QBasic dialect: `\` = integer division, `MOD` = remainder)
IF LANG = 0 THEN PRINT "整数除法: 17 \ 5 = "; 17 \ 5 ELSE PRINT "integer division: 17 \ 5 = "; 17 \ 5
IF LANG = 0 THEN PRINT "取余: 17 MOD 5 = "; 17 MOD 5 ELSE PRINT "remainder: 17 MOD 5 = "; 17 MOD 5

' ⑤ 浮点：只走"字面量直接运算"这条能走通的路径（理由见文件头的三处已知缺陷）
' ⑤ Float: only the "literal used directly in an expression" path that works (for the reason see the three known defects at the top of the file)
IF LANG = 0 THEN PRINT "浮点(×100): 4.75 * 100 = "; 4.75 * 100 ELSE PRINT "float (x100): 4.75 * 100 = "; 4.75 * 100

' ⑥ 循环求和 1..10
' ⑥ Loop sum 1..10
sum = 0
FOR i = 1 TO 10
  sum = sum + i
NEXT i
IF LANG = 0 THEN PRINT "循环求和: 1..10 = "; sum ELSE PRINT "loop sum: 1..10 = "; sum

' ⑦ 循环求阶乘 10!（3628800 —— 用整数足够，不必上浮点）
' ⑦ Loop factorial 10! (3628800 -- integers are enough, no need to go to floats)
fact = 1
FOR i = 1 TO 10
  fact = fact * i
NEXT i
IF LANG = 0 THEN PRINT "阶乘: 10! = "; fact ELSE PRINT "factorial: 10! = "; fact

PRINT "=== done ==="
