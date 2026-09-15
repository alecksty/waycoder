' ★ 目标用例：SUB 里读模块级变量
DIM BW AS INTEGER
BW = 10
SUB show()
    PRINT "sub="
    PRINT BW
    PRINT " half="
    PRINT 100 \ BW
    PRINT "\n"
END SUB
show()
PRINT "top="
PRINT BW
PRINT "\n"
