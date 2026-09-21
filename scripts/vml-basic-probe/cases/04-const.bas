' 缺陷 ②(c)：CONST 参与算术 —— 顶层 vs SUB 内
CONST NB = 6
PRINT "T-C1="; NB - 1
PRINT "T-C2="; 100 + NB
PRINT "T-C3="; NB * 10
SUB inSub()
    PRINT "S-C1="; NB - 1
    PRINT "S-C2="; 100 + NB
    PRINT "S-C3="; NB * 10
END SUB
inSub()
' EXPECT: T-C1=5|T-C2=106|T-C3=60|S-C1=5|S-C2=106|S-C3=60
