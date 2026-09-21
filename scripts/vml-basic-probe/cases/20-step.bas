' 收窄：FOR 的 STEP
DIM i AS INTEGER
DIM n AS INTEGER
n = 0
FOR i = 1 TO 10 STEP 2
    n = n + 1
NEXT i
PRINT "STEP+2="; n
n = 0
FOR i = 10 TO 1 STEP 0 - 3
    n = n + 1
NEXT i
PRINT "STEP-3="; n
n = 0
FOR i = 5 TO 5
    n = n + 1
NEXT i
PRINT "STEP1="; n
n = 0
FOR i = 1 TO 0
    n = n + 1
NEXT i
PRINT "ZERO="; n
' EXPECT: STEP+2=5|STEP-3=4|STEP1=1|ZERO=0
