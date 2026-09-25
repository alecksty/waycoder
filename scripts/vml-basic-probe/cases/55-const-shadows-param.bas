' **形参 / 局部量必须遮蔽同名 CONST** —— BASIC 的名字大小写不敏感，
' 所以 `CONST NB = 6` 与 `SUB t(nb AS INTEGER)` 里的 `nb` 是同一个名字。
'
' 未修之前（v0.96.439 发现并修）：求值那条路**先查常量表**，
' 于是子过程体里的 `nb` 被整体折成 6 —— 实参传进来直接丢掉，**一个错都不报**。
' 真实撞上它的是 `Examples/basic/gorilla_pro.bas` 的 `mix2(nr, ng, nb, …)`：
' 「天空 → 云」混色的**蓝色分量恒等于 6**，画出来的云是橄榄绿的，
' 而编译日志、警告、运行时报错里**一个字都没有** —— 只能靠把中间量 PRINT 出来才看得见。
'
' 判据三档：形参遮蔽 / 局部量遮蔽 / **子过程外照旧是常量**（这条才是"没改坏"的证据）。
'
' EXPECT: A=129|B=7|C=6
CONST NB = 6
DIM out AS INTEGER

SUB t(nb AS INTEGER)
    out = nb
END SUB

SUB u()
    DIM nb AS INTEGER
    nb = 7
    out = nb
END SUB

t(129)
PRINT "A="; out
u()
PRINT "B="; out
' 子过程**外**没有同名局部 ⇒ 这里必须仍旧是常量 6
PRINT "C="; NB
END
