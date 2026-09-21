' FOR / NEXT（经典 BASIC 游戏的主力循环）
DIM i AS INTEGER
DIM s AS INTEGER
s = 0
FOR i = 1 TO 5
    s = s + i
NEXT i
PRINT "FOR1="; s
FOR i = 10 TO 1 STEP -3
    s = s + 1
NEXT i
PRINT "FOR2="; s
SUB inSub()
    DIM j AS INTEGER
    DIM t AS INTEGER
    t = 0
    FOR j = 1 TO 4
        t = t + j * j
    NEXT j
    PRINT "S-FOR="; t
END SUB
inSub()
' EXPECT: FOR1=15|FOR2=19|S-FOR=30
