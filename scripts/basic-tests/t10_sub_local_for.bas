SUB f()
    DIM i AS INTEGER
    DIM n AS INTEGER
    n = 0
    FOR i = 0 TO 3
        n = n + i
    NEXT i
    PRINT "n="
    PRINT n
    PRINT "\n"
END SUB
f()
