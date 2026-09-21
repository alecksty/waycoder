' SUB 里的数组 + 二维数组 + 动态下标
DIM i AS INTEGER
SUB inSub()
    DIM b(5) AS INTEGER
    DIM k AS INTEGER
    k = 0
    WHILE k < 5
        b(k) = k * 3
        k = k + 1
    WEND
    PRINT "S-ARR2="; b(0); b(2); b(4)
END SUB
inSub()
DIM g(3, 3) AS INTEGER
g(1, 2) = 77
PRINT "T-2D="; g(1, 2)
' EXPECT: S-ARR2=0612|T-2D=77
