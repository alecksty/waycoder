' 收窄：GOTO / 行号标签
DIM n AS INTEGER
n = 1
GOTO skip
n = 99
skip:
PRINT "GOTO="; n
' EXPECT: GOTO=1
