' DIM SHARED 能不能跨 SUB
DIM SHARED g AS INTEGER
g = 77
SUB show()
    PRINT "shared="
    PRINT g
    PRINT "\n"
END SUB
show()
PRINT "top="
PRINT g
PRINT "\n"
