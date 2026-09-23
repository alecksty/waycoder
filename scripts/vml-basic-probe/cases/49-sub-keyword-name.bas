' SUB 的名字**恰好奇是 QBasic 关键字**时，整个 SUB 体必须照常执行。
'
' 未修之前（v0.96.403）：`ParseSubDeclaration` 与 `ParseCallStatement` 都要求
' 名字必须是 IDENTIFIER，而 `Draw`/`Line`/`Circle`… 这些词被词法器认成**关键字 token**
' ⇒ 两边各返回一次 null ⇒ **声明与调用被静默丢掉**，SUB 体一个字都不跑、**一个错都不报**。
' 真 QBasic 里这么写本来就该报语法错，但"静默接受 + 静默不执行"是本仓最忌讳的那种错法。
'
' 判据：11 个 SUB 体**都**要真的执行（每个给 N 加 1）。
'
' EXPECT: RAN=11
DEFINT A-Z
N = 0
CALL Draw
CALL Line
CALL Circle
CALL Paint
CALL Play
CALL Screen
CALL Color
CALL Data
CALL Read
CALL Input
CALL Print
PRINT "RAN="; N
END

SUB Draw
  N = N + 1
END SUB
SUB Line
  N = N + 1
END SUB
SUB Circle
  N = N + 1
END SUB
SUB Paint
  N = N + 1
END SUB
SUB Play
  N = N + 1
END SUB
SUB Screen
  N = N + 1
END SUB
SUB Color
  N = N + 1
END SUB
SUB Data
  N = N + 1
END SUB
SUB Read
  N = N + 1
END SUB
SUB Input
  N = N + 1
END SUB
SUB Print
  N = N + 1
END SUB
