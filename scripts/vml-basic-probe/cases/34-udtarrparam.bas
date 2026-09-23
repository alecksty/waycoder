' 用户自定义类型数组 —— 整数组形参 + 字段偏移（GORILLA.BAS 的 BCoor 形态）。
'
' 两条独立的缺陷叠在一起，都**不报错**：
'   ① 数组形参没被登记（同上一条）；
'   ② 字段访问拿的是**值**当**基址**（`GenerateExpression(RecordExpression, 2)`），
'      于是 `b(0).XCoor` / `b(0).YCoor` / `b(3).XCoor` / `b(3).YCoor` 四个落进
'      同一对与 b 无关的地址（实测四个读出来全是 1212）。
' 另外钉住 **UDT 元素的步长**：元素必须是整个记录的字节数（2 个字段 = 8 字节），
' 按 4 算的话 `b(1).XCoor` 会落在 `b(0).YCoor` 上。
TYPE XYPoint
  XCoor AS INTEGER
  YCoor AS INTEGER
END TYPE

SUB putp (BYREF b() AS XYPoint, i, x, y)
  b(i).XCoor = x
  b(i).YCoor = y
END SUB

FUNCTION getx (BYREF c() AS XYPoint, i)
  getx = c(i).XCoor
END FUNCTION

SUB showp (BYREF d() AS XYPoint, i)
  PRINT "S="; d(i).XCoor; ","; d(i).YCoor
END SUB

' 模块级 UDT 数组（跨 SUB 传参）
DIM pts(0 TO 3) AS XYPoint
CALL putp(pts(), 0, 11, 22)
CALL putp(pts(), 3, 33, 44)
PRINT "P0="; pts(0).XCoor; ","; pts(0).YCoor
PRINT "P3="; pts(3).XCoor; ","; pts(3).YCoor
PRINT "G="; getx(pts(), 3)
CALL showp(pts(), 3)

' SUB 内 DIM 的 UDT 数组（PlayGame 里 DIM BCoor 的形态）
SUB inSub ()
  DIM bc(0 TO 3) AS XYPoint
  CALL putp(bc(), 1, 77, 88)
  PRINT "N1="; bc(1).XCoor; ","; bc(1).YCoor
END SUB
CALL inSub
' EXPECT: P0=11,22|P3=33,44|G=33|S=33,44|N1=77,88
