' **8 字节实参的两种"要地址"情形** —— 判据是值真的到得了被调方。
'
' 这是 v0.96.408 修的另一半（前一半见 50-mixed-param-types.bas）。
' 实参区**一格 4 字节**，8 字节形参（Double/Long）一格装不下 ⇒
' 与 BYREF 同形：**槽里放地址、被调方解引用**。调用方要为此造临时量，而临时量原来
' **一律按 4 字节压** ⇒ 被调方按 `MOVED` 读 8 字节，读到隔壁槽的垃圾。
' 实测修前 `CALL S3(45.5)` 打出 **0**（既不报错也不崩，就是"值没了"）。
'
' 三条判据各压一个点：
'   · T1 —— **非左值 BYREF**（字面量 / 表达式）：QBasic 语义是"造个临时量"，
'           临时量必须是**形参类型**的宽度与位型（double 要 I2D/F2D 之后才存）。
'   · T2 —— **BYVAL 的 8 字节形参**：拷贝一份传过去，被调方写它**不回流**。
'   · T3 —— **转手再 BYREF**（`SUB` 里把形参传给另一个 `SUB`）：
'           `&形参` 在地址格里取的就是**原变量**的地址，不是"我方形参槽"的地址。
'           取错的话被调方读写的是我们的槽（一个装着地址的内存），全程不报错。
'
' ⚠ 另一条同源的坑，别改回去：**8 字节临时量不能用 `DPUSH`/`FPUSH`** ——
'   它们压的是 VM 自己的 `doubleStack`/`floatStack`（`VMLRuntime.Float.cs` 的
'   `ExecuteDpush`），**不是机器栈 R13**，被调方从 `R12+8` 那儿根本看不到。
'   正解是 `SUB R13, #8` + `MOVED [@R13], R0`。
'
'   · T4 —— **`AS DOUBLE` 声明**（没有 `#` 后缀时唯一的类型来源）：
'           形参的类型判据原来只看名字后缀 ⇒ `a AS DOUBLE` 按 Integer 走，
'           读到的其实是 double 的**低半字**（`45.5` → `0x42340000` = 1110835200）。
'           GORILLA 用的是 `Angle#` 后缀写法所以没踩到，老程序里 `AS DOUBLE` 很常见。
'
' EXPECT: T1=45|T1=45|T4=45|T4C=100|T2=7|T2V=7|T3=88|T3W=99|P1=11|P2=22
DEFINT A-Z
CALL T1(45.5)
CALL T1(40.5 + 5#)
CALL T4(45.5)
PRINT "T4C="; T4C(100.25)

' BYVAL：体里把它改成 999，调用方那个变量**必须还是 7**（拷贝不回流）
v# = 7.25
CALL T2(v#)
PRINT "T2V="; v#

' 转手：Outer 把形参 g# 原样交给 T3，T3 要能把值写回调用方的那个变量
g# = 88
CALL Outer(g#)
PRINT "T3W="; g#

' 转手两个形参（一个 8 字节、一个 4 字节），顺序也要对
CALL Pair(11, 22)
END

SUB T1 (a#)
  PRINT "T1="; a#
END SUB

SUB T2 (BYVAL b#)
  PRINT "T2="; b#
  b# = 999
END SUB

SUB T3 (c#)
  PRINT "T3="; c#
  c# = 99
END SUB

SUB Outer (g#)
  CALL T3(g#)
END SUB

SUB Pair (p, q)
  PRINT "P1="; p
  PRINT "P2="; q
END SUB

SUB T4 (a AS DOUBLE)
  PRINT "T4="; a
END SUB

FUNCTION T4C (b AS DOUBLE)
  T4C = b
END FUNCTION
