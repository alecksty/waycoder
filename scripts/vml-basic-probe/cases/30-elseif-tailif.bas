' `ELSEIF` 分支的**最后一条语句**若是单行 `IF … THEN <语句>` ⇒ 它把紧跟的下一行
' （`END IF` 或下一个 `ELSEIF`）吞进自己的 THEN 分支 ⇒ 这个 IF 链永不闭合
' ⇒ **其后整个文件被静默丢弃**。
'
' 症状极具误导性：不报"这里错了"，只报后面一堆「未定义的函数」
' （行号标签 `line_40`、后面定义的 SUB/FUNCTION 全没了），看起来像链接问题、
' 像 `#include` 的问题，实际根因在这 4 行。
'
' 发现经过：给 1970~80 年代老 BASIC 游戏做文本控制台垫层（`_tty.bas`），
' 一个 350 行的垫层整个"失效"—— 二分到 `ttyAsk`，再二分到它里面的
' `IF ttyM = 1 THEN … ELSEIF ttyK = 39 THEN  ttyVal = …  IF … THEN ttyVal = ttyHi`
' 这一段。**逐个构造单独测都正常**（嵌套 IF、ELSEIF 链、深嵌套、按名返回…），
' 只有"ELSEIF 分支末尾收一个单行 IF"这个**组合**才触发 —— 所以它躲过了此前所有判据。
'
' 判据（`min2`）：单行 IF 后面**还有**语句时不触发 —— 所以"是不是分支末尾"是关键变量。
'
' **绕法**（机械、可加进转换层）：遇到 ELSEIF 分支末尾的单行 IF，展开成
'   `IF c THEN` / `   语句` / `END IF` 三行。老 BASIC 里单行 IF 极常见，
'   所以这条转换规则必须进转换脚本。
DIM a AS INTEGER
DIM q AS INTEGER
a = 1
q = 0
IF a = 9 THEN
    q = 1
ELSEIF a = 1 THEN
    q = 2
    IF a = 1 THEN q = 3
END IF
PRINT "Q="; q
10 PRINT "L"
20 GOTO 40
30 PRINT "N"
40 PRINT "M"
' KNOWN-RED: ELSEIF 分支末尾的单行 IF 吞掉下一行并丢弃其后整个文件
' EXPECT: Q=3|L|M
