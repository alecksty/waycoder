' demo_std.bas —— BASIC 第一层：**标准输入输出**（std）
'
' 这一层只用语言自己的 `PRINT`，不碰任何图形 / 控制台 / 宿主 UI 接口 ——
' 所以它在**任何**后端上都是同一份行为，也是四份 demo 里唯一有
' "逐字节确定性输出" 判据的一份（另外三份只能验"编得过、跑得完、不挂死"）。
'
' 跑法（桌面 vmlcli）：
'   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/basic/demo_std.bas
'
' 期望输出（逐字节，见文件末尾的 `demo_std.expected.txt` 对拍脚本）：
'   === BASIC 标准输出 demo ===
'   字符串: 你好，世界
'   整数: 42
'   计算: 7 * 6 = 42
'   整数除法: 17 \ 5 = 3
'   取余: 17 MOD 5 = 2
'   浮点(×100): 4.75 * 100 = 475
'   循环求和: 1..10 = 55
'   阶乘: 10! = 3628800
'   === done ===
'
' ◆ 本前端的三条写法要求（都是实测出来的，不是风格偏好）
'   ① `PRINT "…"; 值` —— **分号不加分隔符**，末尾自动补换行（见 whack.bas 的说明）。
'   ② 模块级变量必须在赋值前 `DIM`。
'   ③ 整除用 `\`、取余用 `MOD`（QBasic 方言，`BasicDialect=qbasic`）。
'
' ◆ 本前端**浮点的三处已知缺陷**（本文件刻意绕开，不是没想到）
'   都在 `Examples/basic/demo_std.bas` 这条链上实测过，最小复现：
'
'     DIM x AS SINGLE
'     x = 4.75        : PRINT x * 100     ⇒ 0      （应 475 —— **浮点字面量赋给变量即丢值**）
'     PRINT 4.75                          ⇒ 4      （**PRINT 浮点按整数截断**，值本身是对的）
'     PRINT 10 / 4                        ⇒ 2      （QBasic 的 `/` 是**浮点除**，应 2.5；
'                                                    只有 `\` 才是整除）
'
'   第一条最要命：**浮点变量读回来恒为 0**（SINGLE / DOUBLE 都一样）。
'   所以这里只演示"浮点字面量直接参与运算"这条能走通的路径 —— `4.75 * 100` 算出来
'   确实是 475（值正确），只是不能存进变量、也不能直接 PRINT。

' ── 变量声明（必须在赋值之前）────────────────────────────────
DIM i AS INTEGER
DIM sum AS INTEGER
DIM fact AS INTEGER
DIM a AS INTEGER
DIM b AS INTEGER

PRINT "=== BASIC 标准输出 demo ==="

' ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通）
PRINT "字符串: 你好，世界"

' ② 整数
i = 42
PRINT "整数: "; i

' ③ 整数运算
a = 7
b = 6
PRINT "计算: 7 * 6 = "; a * b

' ④ 整除与取余（QBasic 方言：`\` = 整除，`MOD` = 取余）
PRINT "整数除法: 17 \ 5 = "; 17 \ 5
PRINT "取余: 17 MOD 5 = "; 17 MOD 5

' ⑤ 浮点：只走"字面量直接运算"这条能走通的路径（理由见文件头的三处已知缺陷）
PRINT "浮点(×100): 4.75 * 100 = "; 4.75 * 100

' ⑥ 循环求和 1..10
sum = 0
FOR i = 1 TO 10
  sum = sum + i
NEXT i
PRINT "循环求和: 1..10 = "; sum

' ⑦ 循环求阶乘 10!（3628800 —— 用整数足够，不必上浮点）
fact = 1
FOR i = 1 TO 10
  fact = fact * i
NEXT i
PRINT "阶乘: 10! = "; fact

PRINT "=== done ==="
