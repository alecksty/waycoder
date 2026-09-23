' **SUB 里的循环计数器是该 SUB 自己的**（除非 `DIM SHARED`）。
'
' 真因：模块体里只要出现过同名变量（GORILLA.BAS 的模块体里有 `FOR i = 0 TO 8`），
' 那个名字就成了"模块级变量"，而 SUB 里的 `FOR i` 原先也认它 ⇒ **几个互不相干的 SUB
' 共用一个计数器**：内层循环跑完把外层的计数冲成内层的收尾值，外层循环当场提前退出。
' 实测 GORILLA.BAS：第一局命中之后 `PlayGame` 的 `FOR i = 1 TO 3` 里的 `i` 已经是 3
' ⇒ 只打一局就进 GAME OVER（比分来不及画）。
'
' 最小复现就是本节 `A` 的那三行：修前只打 `outer1` 就 `A-done`，修后 3 轮各带 2 次内层。
' `B` 是对照组：显式 `DIM SHARED` 的计数器**照旧共享**（这是有意保留的能力）。
' EXPECT: O1=1|O2=2|O3=3|IN=4|S1=1|S2=2|S3=3|SH=4
CALL A

DIM SHARED sh
CALL B
PRINT "SH="; sh

END

SUB A
  FOR i = 1 TO 3
    PRINT "O"; i; "="; i
    CALL S
  NEXT
  PRINT "IN="; i
END SUB

SUB S
  FOR i = 1 TO 2
  NEXT
END SUB

SUB B
  FOR sh = 1 TO 3
    PRINT "S"; sh; "="; sh
  NEXT
END SUB
