' 字符串函数
s$ = "hello"
PRINT "LEN="; LEN(s$)
PRINT "LEFT="; LEFT$(s$, 3)
PRINT "MID="; MID$(s$, 2, 3)
PRINT "RIGHT="; RIGHT$(s$, 2)
PRINT "UC="; UCASE$("abc")
PRINT "CHR="; CHR$(65)
' EXPECT: LEN=5|LEFT=hel|MID=ell|RIGHT=lo|UC=ABC|CHR=A
