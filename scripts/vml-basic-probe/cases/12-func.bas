' 用户 FUNCTION / SELECT CASE / ELSEIF
FUNCTION twice(n AS INTEGER) AS INTEGER
    twice = n * 2
END FUNCTION
PRINT "FN="; twice(21)
SELECT CASE twice(5)
    CASE 10
        PRINT "SC=ten"
    CASE ELSE
        PRINT "SC=other"
END SELECT
DIM v AS INTEGER
v = 3
IF v = 1 THEN
    PRINT "EI=one"
ELSEIF v = 3 THEN
    PRINT "EI=three"
ELSE
    PRINT "EI=other"
END IF
' EXPECT: FN=42|SC=ten|EI=three
