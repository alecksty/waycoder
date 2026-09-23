' SELECT CASE 的**字符串**测试值必须比**内容**，不能比指针。
'
' 生成器一直用 `CMP` 比 CASE 条件 —— 而 BASIC 的字符串是「1 字符串指针」，
' 拿指针比大小/相等与内容毫无关系：
'   · `CASE "0" TO "9"` 对 `K$="4"` **恒不匹配**（指针不在两个字面量地址之间）；
'   · `CASE CHR$(13)` 反而**恒匹配** —— `INKEY$` 与 `CHR$()` 都走 `basic_chr`
'     的同一个 1 字符串缓冲区，两个指针**恰好相等**。
' 实测 GORILLA.BAS 的 `GetNum#`（角度/力度输入）正是这个形状 ⇒ 按任何数字都被
' 当成回车，输入永远读不到（`.scratch/gor/mre_scase.bas` 是先做出来的最小复现）。
'
' 判据：数字落 `CASE "0" TO "9"`、回车落 `CASE CHR$(13)`、别的一律 `CASE ELSE`，
' 顶层与 SUB 内各来一遍（本仓的历史经验：SUB 体才是重灾区）。
SUB Try (k$)
  SELECT CASE k$
    CASE "0" TO "9"
      PRINT "D="; k$
    CASE CHR$(13)
      PRINT "R=CR"
    CASE "ab" TO "az"
      PRINT "AB="; k$
    CASE ELSE
      PRINT "EL=1"
  END SELECT
END SUB

SELECT CASE "4"
  CASE "0" TO "9"
    PRINT "M1=D"
  CASE ELSE
    PRINT "M1=ELSE"
END SELECT

SELECT CASE MID$("xyz", 2, 1)
  CASE "0" TO "9"
    PRINT "M2=D"
  CASE "y"
    PRINT "M2=Y"
  CASE ELSE
    PRINT "M2=ELSE"
END SELECT

SELECT CASE CHR$(13)
  CASE "0" TO "9"
    PRINT "M3=D"
  CASE CHR$(13)
    PRINT "M3=CR"
  CASE ELSE
    PRINT "M3=ELSE"
END SELECT

CALL Try("7")
CALL Try(CHR$(13))
CALL Try("q")
CALL Try("am")
' EXPECT: M1=D|M2=Y|M3=CR|D=7|R=CR|EL=1|AB=am
