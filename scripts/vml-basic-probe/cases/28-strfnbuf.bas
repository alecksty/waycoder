' 字符串函数的返回值缓冲：同一个表达式里两次调用会互相覆盖
' （`STR$` / `LEFT$` / `MID$` … 都返回 _buf1，谁后写谁赢）
DIM a AS INTEGER
DIM b AS INTEGER
a = 1
b = 2
PRINT "P="; STR$(a) + "/" + STR$(b)
PRINT "Q="; STR$(a) + STR$(b)
' KNOWN-RED: 字符串函数返回值共用 _buf1，见 FRONTEND_DEFECTS.md
' EXPECT: P=1/2|Q=12
