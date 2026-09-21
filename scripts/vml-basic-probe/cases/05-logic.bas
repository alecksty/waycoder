' 缺陷 ②(e)：条件里的 AND / OR —— 顶层 vs SUB 内
DIM a AS INTEGER
DIM b AS INTEGER
a = 5
b = 60
IF a > 0 AND b > 50 THEN
    PRINT "T-AND=1"
ELSE
    PRINT "T-AND=0"
END IF
IF a > 100 OR b > 50 THEN
    PRINT "T-OR=1"
ELSE
    PRINT "T-OR=0"
END IF
SUB inSub()
    IF a > 0 AND b > 50 THEN
        PRINT "S-AND=1"
    ELSE
        PRINT "S-AND=0"
    END IF
    IF a > 100 OR b > 50 THEN
        PRINT "S-OR=1"
    ELSE
        PRINT "S-OR=0"
    END IF
END SUB
inSub()
' EXPECT: T-AND=1|T-OR=1|S-AND=1|S-OR=1
