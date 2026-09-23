' RND(1) 必须是 **[0,1) 的单精度分数**（QBasic 语义），不是原始随机整数。
'
' 库函数 `basic_rnd` 给的是 `SYSCALL #50` 的原始 32 位整数，前端要换算。
' 不换算的话 `INT(RND(1) * x) + 1`（老 BASIC 取 1..x 随机数的**通用写法**，
' GORILLA.BAS 的 `DEF FnRan (x) = INT(RND(1) * x) + 1` 就是它）会按大整数去乘、
' 再过一遍对整数恒等的 `INT` ⇒ 天文数字（实测 1143039323），
' 而那正是楼房宽度/高度的来源。
'
' 取值本身是随机的 ⇒ 判据用**谓词**（范围 / 分布），不打印随机数。
FUNCTION FnRan (x)
  FnRan = INT(RND(1) * x) + 1
END FUNCTION

SUB probe ()
  DIM i AS INTEGER
  DIM v AS INTEGER
  DIM lo AS INTEGER
  DIM hi AS INTEGER
  lo = 9999
  hi = -9999
  FOR i = 1 TO 400
    v = FnRan(100)
    IF v < lo THEN lo = v
    IF v > hi THEN hi = v
  NEXT i
  IF lo >= 1 THEN PRINT "LO1="; 1 ELSE PRINT "LO1="; 0
  IF hi <= 100 THEN PRINT "HI100="; 1 ELSE PRINT "HI100="; 0
  IF hi >= 95 THEN PRINT "HISPREAD="; 1 ELSE PRINT "HISPREAD="; 0
  IF lo <= 5 THEN PRINT "LOSPREAD="; 1 ELSE PRINT "LOSPREAD="; 0
  PRINT "ONE="; INT(RND(1) * 1) + 1
END SUB
CALL probe

' 顶层同一形状（两张内置函数表是两份，必须都换到新实现）
DIM t AS INTEGER
DIM tlo AS INTEGER
DIM thi AS INTEGER
tlo = 9999
thi = -9999
FOR i = 1 TO 400
  t = INT(RND(1) * 50)
  IF t < tlo THEN tlo = t
  IF t > thi THEN thi = t
NEXT i
IF tlo >= 0 THEN PRINT "TLO="; 1 ELSE PRINT "TLO="; 0
IF thi <= 49 THEN PRINT "THI="; 1 ELSE PRINT "THI="; 0
IF thi >= 45 THEN PRINT "THISPREAD="; 1 ELSE PRINT "THISPREAD="; 0
' EXPECT: LO1=1|HI100=1|HISPREAD=1|LOSPREAD=1|ONE=1|TLO=1|THI=1|THISPREAD=1
