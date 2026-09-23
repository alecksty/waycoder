' SUB / FUNCTION 体内的**浮点算术**（此前这条路只有整数一条）。
'
' 三处缺失叠在一起，全都不报错：
'   ① 二元表达式不分浮点（`0.5 * 1000` 按整数乘，浮点位型当整数算）；
'   ② 带小数点的字面量按 `(int)` 截断（`0.5` 变成 0）；
'   ③ 浮点算术必须是**三操作数**形式 `FOP dst,a,b` —— 运行时的 FADD/FSUB/FMUL
'      开头就是 `if (operands.Count < 3) return;`，两操作数形式是**静默空操作**。
' 组合起来症状是 `INT(0.5 * 1000)` 在 SUB 里恒为 0（顶层同一条是 500）。
FUNCTION Half (x)
  Half = INT(x * 0.5)
END FUNCTION

FUNCTION Quad (x)
  Quad = INT(x / 4)
END FUNCTION

SUB Show ()
  DIM q AS INTEGER
  q = INT(0.5 * 1000)
  PRINT "Q="; q
  q = INT(1000 * 0.5)
  PRINT "R="; q
  q = INT(2.5 + 0.5)
  PRINT "S="; q
  q = INT(10.5 - 0.25)
  PRINT "T="; q
END SUB
CALL Show

PRINT "H="; Half(1001)
PRINT "U="; Quad(1002)
' EXPECT: Q=500|R=500|S=3|T=10|H=500|U=250
