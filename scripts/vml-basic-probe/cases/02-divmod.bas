' 缺陷 ②(a)：`\` 与 MOD —— 顶层 vs SUB 内
PRINT "T-DIV="; 100 \ 2
PRINT "T-MOD="; 100 MOD 7
SUB inSub()
    PRINT "S-DIV="; 100 \ 2
    PRINT "S-MOD="; 100 MOD 7
    PRINT "S-INTDIV="; INT(100 / 2)
END SUB
inSub()
' EXPECT: T-DIV=50|T-MOD=2|S-DIV=50|S-MOD=2|S-INTDIV=50
