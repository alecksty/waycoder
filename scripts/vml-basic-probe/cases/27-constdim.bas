' 用 CONST 当数组维度（曾只分 2 格 ⇒ 一写就越界）
CONST N = 5
DIM a(N) AS INTEGER
SUB fill()
    DIM i AS INTEGER
    i = 0
    WHILE i < N
        a(i) = i * 10
        i = i + 1
    WEND
END SUB
fill()
PRINT "A0="; a(0)
PRINT "A4="; a(4)
' EXPECT: A0=0|A4=40
