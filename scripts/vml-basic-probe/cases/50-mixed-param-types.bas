' **混合类型形参**（8 字节的 DOUBLE 夹在整型中间）—— 每个形参都要读到调用方传的值。
'
' ✅ **v0.96.408 已修**。根因**不在形参、在返回值**：`FUNCTION P3 (a#) / P3 = a#`
'    的赋值走 `isFloat ? MOVEF : MOVE`，而值在 R0 里是**双精度位型** ——
'    `MOVEF` 存进去的是 double 45.0 的**低半字**（= 0），函数于是恒返回 0。
'    同轮修的另一半在调用方：实参槽**一格 4 字节**，8 字节形参（Double/Long）
'    一律「槽里放地址、被调方解引用」（与 BYREF 同形），
'    调用方与被调方从此只有一个口径（见 `ParamSlotHoldsAddress`）。
'
' 未修之前（v0.96.406）：`FUNCTION P (a, b, c#, d, e)` 实测
'   a/b 对 ✓，c# 读到 68920（地址级大数）✗，d 的语句整条消失 ✗，e 错位 ✗。
' 调用方（`EmitCallArguments`）的注释写着"被调方按 [R12+12+4i] 的**固定步长 4** 取形参，
' 每个实参只能占一格"；而被调方按 `GetVarByteOffset` **给 DOUBLE 算 8 字节** ——
' 同一处布局两套算法，一有 8 字节类型就漂（本仓头号坑）。
'
' 为什么值得钉住：GORILLA.BAS 的 `PlotShot (StartX, StartY, Angle#, Velocity, PlayerNum)`
' 正是这个形状，而它收到的坐标是**地址**（实测同一轮里：调用前 x 正常、函数内 StartX 变成地址）。
'
' ## 实测把范围缩到了「FUNCTION 而不是 SUB」
'
' 同一轮里（同一份源码，只差被调方是 FUNCTION 还是 SUB）：
'   `SUB    Show (a, b, c#, d, e)` → **五个形参全对** ✓
'   `FUNCTION P3  (a#)`            → 单独调也读到 **0** ✗
' ⇒ 问题在 **FUNCTION 的参数传递**，而 GORILLA 的 `PlotShot` 正是一个 `FUNCTION`。
'
' 另一条独立证据（图形读数，绕开 PRINT 的解引用）：
' `FUNCTION P (a, b, c#, d, e)` 里用 `LINE` 的宽度当读数 ——
'   a/b 正确、`e`（double 之后那个）的语句整条消失、`c#` 读到 **68920**（地址级大数）✗。
' 而同样签名换成 SUB 就全对。
'
' 判据用 **PRINT**（本套件是 stdout 判据）—— PRINT 可能比图形读数**宽松**，
' 所以这条先钉住 PRINT 都能看见的那部分（`C=0`）。
'
' EXPECT: A=278|B=175|C=45|D=80|E=1|A=278|B=175|C=45|D=80|E=1
DEFINT A-Z
CALL Go
END

SUB Go
  x = 278
  y = 175
  Angle# = 45
  Velocity = 80
  PlayerNum = 1
  PRINT "A="; P1(x)
  PRINT "B="; P2(y)
  PRINT "C="; P3(Angle#)
  PRINT "D="; P4(Velocity)
  PRINT "E="; P5(PlayerNum)
  ' 整组一起（真实形状）
  CALL Show(x, y, Angle#, Velocity, PlayerNum)
END SUB

FUNCTION P1 (a)
  P1 = a
END FUNCTION
FUNCTION P2 (a)
  P2 = a
END FUNCTION
FUNCTION P3 (a#)
  P3 = a#
END FUNCTION
FUNCTION P4 (a)
  P4 = a
END FUNCTION
FUNCTION P5 (a)
  P5 = a
END FUNCTION

SUB Show (StartX, StartY, Angle#, Velocity, PlayerNum)
  PRINT "A="; StartX
  PRINT "B="; StartY
  PRINT "C="; Angle#
  PRINT "D="; Velocity
  PRINT "E="; PlayerNum
END SUB
