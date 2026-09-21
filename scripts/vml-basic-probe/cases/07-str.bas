' 缺陷 ④⑤⑥：字符串拼接 / DIM AS STRING / SUB 的字符串形参
b$ = "x" + "y"
PRINT "CAT="; b$
n$ = STR$(123)
PRINT "STR="; n$
SUB withStr(s AS STRING)
    PRINT "PARAM="; s
END SUB
d$ = "hi"
withStr(d$)
' EXPECT: CAT=xy|STR=123|PARAM=hi
