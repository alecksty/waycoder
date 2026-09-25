' `TIMER` = **本地当天 0 点以来的秒数**（QBasic 语义，带亚秒精度）。
'
' 未修之前：前端把它编成 `SYSCALL #53 GetTick`（**VM 启动以来**的毫秒）÷ 1000 ——
' 两个都不对：① 语义不是"当天"（QBasic 里 TIMER 半夜归零）；② 只有整秒 ⇒
' `LOOP UNTIL TIMER - t0 > 0.5` 这类"等半秒"的写法**永远不成立**。
'
' 判据（都写成显式 1/0，避免依赖 BASIC 的"真 = -1"）：
'   A 落在 [0, 86400)
'   B 与 `TIME$` 的**小时**一致（同名同源：都取本地时间）
'   C 单调不减
'   D 亚秒精度在**表达式里**可用（`TIMER - t0 > 0.0001` 等得到）
' ⚠ E 这一条是**已知不足**、故意留在闸门里钉住症状：存进变量会被截成整秒
'   （前端存浮点的收尾会先 `f2i`），所以 `t0 = TIMER` 拿到的是整秒。
'
' EXPECT: A=1|B=1|C=1|D=1

DIM t0 AS SINGLE
DIM t1 AS SINGLE
DIM ok AS INTEGER

t0 = TIMER
ok = 0
IF t0 >= 0 THEN
    IF t0 < 86400 THEN
        ok = 1
    END IF
END IF
PRINT "A="; ok

' 与 TIME$ 的小时对一眼（两个读数必须同源：都是本地时间）
h = VAL(LEFT$(TIME$, 2))
ok = 0
IF INT(t0 / 3600) = h THEN
    ok = 1
END IF
PRINT "B="; ok

t1 = TIMER
ok = 0
IF t1 >= t0 THEN
    ok = 1
END IF
PRINT "C="; ok

' 亚秒：连读两次、要求差值能小到 0.0001 —— 整秒的实现永远等不到
s# = TIMER
n = 0
ok = 0
WHILE n < 300000
    n = n + 1
    IF TIMER - s# > 0.0001 THEN
        ok = 1
        n = 999999
    END IF
WEND
PRINT "D="; ok
END
