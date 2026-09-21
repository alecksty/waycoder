' SUB / FUNCTION 里的 SELECT CASE（分派里曾整支漏掉 ⇒ 一条代码都不生成、恒返回 0）
FUNCTION f(n AS INTEGER) AS INTEGER
    SELECT CASE n
        CASE 0
            f = 10
        CASE 1
            f = 20
        CASE ELSE
            f = 30
    END SELECT
END FUNCTION
SUB s(n AS INTEGER)
    SELECT CASE n
        CASE 0
            PRINT "S=zero"
        CASE 1
            PRINT "S=one"
        CASE ELSE
            PRINT "S=other"
    END SELECT
END SUB
PRINT "F0="; f(0)
PRINT "F1="; f(1)
PRINT "F2="; f(2)
s(0)
s(1)
s(2)
' EXPECT: F0=10|F1=20|F2=30|S=zero|S=one|S=other
