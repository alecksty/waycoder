' skel.bas —— BASIC 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
'
' 取材 Examples/basic/tetris.bas —— 这是唯一需要**显式声明**外部函数的语言：
' 只有 `NATIVE SUB/FUNCTION` + **空体** 才是裸标签（否则会被加上 sub_/func_ 前缀，
' 链接期找不到；见 BasicCompiler/CodeGenerator.Sub.cs:1456-1463、
' CodeGenerator.Expressions.cs:437-443）。前端实现 VMLPrepares/BasicCompiler/。
'
' 刻意写法（每处都是这个前端的要求或绕行）：
'  ① `DIM a(3)` = 4 个元素（下标 0..3）—— BASIC 的 DIM n 是**上界**、下界固定 0，
'     写 a(4) 会得到 5 个（Parser.Statements.cs:352-386）。
'  ② 数组名大小写必须前后一致（arrayVariables 是大小写敏感的 Dictionary）。
'  ③ 颜色用 &HFFFF0000（= -65536）：BASIC 词法器认 &H，不认 0x。
'  ④ `PRINT "…"; s` 分号不加分隔符、末尾**自动补换行** ⇒ 正好一行。
'  ⑤ 循环 `FOR i = 0 TO 3 … NEXT`（含上界）；模块级变量必须在赋值前 DIM。
'  ⑥ BASIC 没有 asm()（已移除），调 C 共享库只有 NATIVE 这一条路。

FUNCTION inc(x AS INTEGER) AS INTEGER
    inc = x + 1
END FUNCTION

NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, c AS INTEGER, f AS INTEGER, lw AS INTEGER, r AS INTEGER)
END SUB
NATIVE SUB ui_present()
END SUB

DIM a(3) AS INTEGER
DIM s AS INTEGER
DIM i AS INTEGER

a(0) = 1
a(1) = 2
a(2) = 3
a(3) = 4
s = 0
FOR i = 0 TO 3
    a(i) = inc(a(i))
    s = s + a(i)
NEXT
PRINT "SKEL-SUM="; s

ui_rect(10, 10, 50, 50, &HFFFF0000, 1, 0, 0)
ui_present()
