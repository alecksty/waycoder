' EXIT WHILE / EXIT FOR 在 SUB / FUNCTION 里（曾整支失效 ⇒ 循环退不出来）
FUNCTION f(n AS INTEGER) AS INTEGER
    DIM i AS INTEGER
    DIM k AS INTEGER
    i = 0
    k = 0
    WHILE i < 100
        k = k + 1
        IF i >= n THEN
            EXIT WHILE
        END IF
        i = i + 1
    WEND
    f = k
END FUNCTION
SUB s()
    DIM i AS INTEGER
    DIM k AS INTEGER
    k = 0
    FOR i = 0 TO 100
        k = k + 1
        IF i >= 3 THEN
            EXIT FOR
        END IF
    NEXT i
    PRINT "SF="; k
END SUB
PRINT "F4="; f(4)
PRINT "F0="; f(0)
s()
' EXPECT: F4=5|F0=1|SF=4
