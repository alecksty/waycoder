' 数组形参（标量数组）：基址存在形参槽里，元素地址 = 基址 + 下标×4。
'
' 钉的是"数组形参从来没被登记成数组"这条：`arrayVariables` 只从 `DIM` 收，
' 于是 SUB 体内 `b(i)` 在 GenerateArrayAccess 第一句就 `return`（静默 0 / 不生成代码）
' —— 写进去全丢、读回来全 0。GORILLA.BAS 的 `MakeCityScape (BCoor() AS XYPoint)`
' 整座城市画不出来就是它（一组坐标全写进了地址 0）。
'
' 三样一起钉：整数组实参（`CALL fill(arr())`）、元素写、元素读；
' 外加"形参再当实参往下传"（`b()` 转交）与"用形参当下标"（`c(n)`）。
SUB fill (BYREF b() AS ANY)
  b(0) = 11
  b(2) = 33
END SUB

SUB addone (BYREF c() AS ANY, n)
  c(n) = c(n) + 1
END SUB

' 形参再往下传：基址必须**转发**（不是把形参槽的地址传下去，那会整体错位一格间接）
SUB relay (BYREF d() AS ANY)
  CALL fill(d())
END SUB

DIM arr(0 TO 3)
CALL fill(arr())
PRINT "A0="; arr(0)
PRINT "A2="; arr(2)
CALL addone(arr(), 2)
PRINT "A2B="; arr(2)
CALL addone(arr(), 2)
PRINT "A2C="; arr(2)

DIM arr2(0 TO 3)
CALL relay(arr2())
PRINT "B0="; arr2(0)
PRINT "B2="; arr2(2)

' 局部变量当实参的数组也走同一条（PlayGame 里 DIM BCoor 的形态）
SUB useLocal ()
  DIM loc(0 TO 2)
  CALL fill(loc())
  PRINT "L0="; loc(0)
END SUB
CALL useLocal
' EXPECT: A0=11|A2=33|A2B=34|A2C=35|B0=11|B2=33|L0=11
