' 缺陷 ③：数组
DIM a(10) AS INTEGER
DIM i AS INTEGER
a(3) = 42
a(0) = 7
PRINT "ARR-3="; a(3)
PRINT "ARR-0="; a(0)
i = 4
a(i) = 9
PRINT "ARR-i="; a(i)
PRINT "ARR-SUM="; a(0) + a(3) + a(4)
' EXPECT: ARR-3=42|ARR-0=7|ARR-i=9|ARR-SUM=58
