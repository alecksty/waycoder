' 缺陷 ②(b)：带括号的复合子表达式 —— 顶层 vs SUB 内
PRINT "T-P1="; 1 + (2 * 3)
PRINT "T-P2="; 10 * (5 - 1)
PRINT "T-P3="; (2 + 3) * 4
SUB inSub()
    PRINT "S-P1="; 1 + (2 * 3)
    PRINT "S-P2="; 10 * (5 - 1)
    PRINT "S-P3="; (2 + 3) * 4
    PRINT "S-P4="; 7 * (8 + 2)
END SUB
inSub()
' EXPECT: T-P1=7|T-P2=40|T-P3=20|S-P1=7|S-P2=40|S-P3=20|S-P4=70
