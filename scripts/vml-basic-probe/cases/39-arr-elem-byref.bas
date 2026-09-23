' **数组元素当 BYREF 实参**：元素地址本身就是合法左值。
'
' `SUB t(a) / a = 5` + `t arr(1)` 必须把 `arr(1)` 改掉（QBasic 就是这样）。
' 此前非左值判定把数组元素也算进去（造临时量）⇒ 被调方的写不回流。
' 顺带钉住"UDT 数组元素的字段地址"能当实参传（`arr(i).F` 是左值）。
TYPE XYPoint
  XCoor AS INTEGER
  YCoor AS INTEGER
END TYPE

SUB bump (v)
  v = v + 100
END SUB

SUB bumpf (f)
  f = f + 7
END SUB

DIM arr(0 TO 3)
DIM pts(0 TO 2) AS XYPoint

arr(1) = 5
CALL bump(arr(1))
PRINT "E="; arr(1)

pts(2).XCoor = 30
CALL bumpf(pts(2).XCoor)
PRINT "F="; pts(2).XCoor

' 标量也要照旧（对照组）
DIM sv AS INTEGER
sv = 1
CALL bump(sv)
PRINT "G="; sv
' EXPECT: E=105|F=37|G=101
