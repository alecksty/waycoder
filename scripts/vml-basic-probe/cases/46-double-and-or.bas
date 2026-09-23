' `AND`/`OR` 连接两个浮点比较：**两个比较都要算对，且互不干扰**。
'
' 两个比较各自单独写都是对的（`IF B# >= A#` 与 `IF B# <= A# + 2` 都成立），
' 但用 `AND`/`OR` 连起来之后**整体判成假**。
'
' 真因不在标志位，而在**顶层表达式的浮点寄存器窗口自己踩自己**
' （`CodeGenerator.Expressions.cs` 的 `isFloat` 分支原来只有 `{reg, (reg+4)%8}` 这一对）：
'   ① 节点把左值放进 `reg`、右操作数求值到 `(reg+4)%8`；
'   ② 但**右操作数内部的嵌套节点又会用回 `reg`** —— 它自己的窗口是
'      `{(reg+4)%8, ((reg+4)%8+4)%8}`，而后者正好等于 `reg`（加 4 再模 8 是自逆的）。
'   ⇒ "求右操作数"这一步把刚算好的左值踩掉，于是
'      · 第一个比较的 0/1 被第二个比较的 `A# + 2` 冲成 `A#+2` ⇒ `2 AND 1` = 0（恒假）；
'      · 第二个比较自己的左操作数 `B#` 又被 `A# + 2` 里的那个 `2` 冲成 `2`
'        ⇒ 比较变成 `2 <= A#+2`（换个 A#/B# 就露馅）。
'
' 实测原型：GORILLA.BAS 的 `PlotShot`
'   `IF (x# >= ScrWidth - Scl(10)) OR (x# <= 3) OR (y# >= ScrHeight - 3) THEN OnScreen = FALSE`
' —— 三个浮点比较一 `OR`，香蕉**第一步就判成"出屏"**、整个飞行只剩一帧。
'
' 修法：左值跨"右操作数的求值"压栈保管（与 SUB 体那条路同源）。
' 逻辑/位运算的操作数是**比较产出的 0/1 整数**，所以那三条用整数 PUSH/POP 而不是 DPUSH/DPOP。
' 同一族的浮点算术嵌套见 `47-float-nest.bas`。
'
' 判据：只会打印下面这些行（`A=1|B=1|C=1|F=1|G=1|J=1`）；
' 任何一条 `NEG-…=BAD` 出现都说明对应的否定用例**错判成真**。
' EXPECT: A=1|B=1|C=1|F=1|G=1|J=1
A# = 0
B# = 0
IF B# >= A# THEN PRINT "A=1"
IF B# <= A# + 2 THEN PRINT "B=1"
IF B# >= A# AND B# <= A# + 2 THEN PRINT "C=1"
' 反例：B# 不在区间内时必须为假（否则"恒真"也能骗过上面那条）
IF B# >= A# + 1 AND B# <= A# + 2 THEN PRINT "NEG-D=BAD"
' 三个 OR 串起来（GORILLA 的形状）：x# 在屏内 ⇒ 三条全假、整体为假
X# = 100 : Y# = 100
IF (X# >= 200) OR (X# <= 3) OR (Y# >= 200) THEN PRINT "NEG-E=BAD"
' 出屏那一条要能真的触发
IF (X# >= 200) OR (X# <= 3) OR (Y# >= 50) THEN PRINT "F=1"
' 三个比较的 AND 串联
IF X# >= 50 AND X# <= 200 AND Y# >= 50 THEN PRINT "G=1"
IF X# >= 50 AND X# <= 200 AND Y# >= 500 THEN PRINT "NEG-H=BAD"
' 混着来：一边真一边假
IF X# >= 50 AND Y# >= 500 THEN PRINT "NEG-I=BAD"
IF X# >= 50 OR Y# >= 500 THEN PRINT "J=1"
END
