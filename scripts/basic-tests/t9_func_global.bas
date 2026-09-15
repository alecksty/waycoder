' ★ 模块级 FUNCTION 读写模块级变量 + 被 SUB 调用（游戏的 bget/bset 就是这个形状）
DIM W AS INTEGER
DIM board(12) AS INTEGER
W = 10
FUNCTION bget(idx AS INTEGER) AS INTEGER
    bget = board(idx)
END FUNCTION
SUB fill()
    DIM i AS INTEGER
    FOR i = 0 TO 3
        board(i) = i * 2
    NEXT i
END SUB
SUB dump()
    DIM i AS INTEGER
    DIM v AS INTEGER
    PRINT "vals="
    FOR i = 0 TO 3
        v = bget(i)
        PRINT v
        PRINT " "
    NEXT i
    PRINT "\n"
END SUB
fill()
dump()
PRINT "W="
PRINT W
PRINT "\n"
