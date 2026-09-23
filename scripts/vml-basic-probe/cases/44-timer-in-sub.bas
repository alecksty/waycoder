' `TIMER` 在 SUB/FUNCTION 里要真的取到 tick（不是上一条语句留在 R0 里的残值）。
'
' `GenerateSubExpression` 原先**没有** Timer/Date/Time 那三档（只有顶层
' `GenerateExpression` 有），落到方法末尾那个没有 else 的兜底 = 什么都不生成 ⇒
' `reg` 留着残值、**不报错**。GORILLA.BAS 的 `SUB Rest (t#)`：
' `s# = TIMER` 编成 `R1 = R0`（R0 是刚清零的局部量）⇒ `TIMER - s#` 恒为常数、
' `LOOP UNTIL` 永不成立 ⇒ 转屏/等待全变成死循环。
'
' 判据：SUB 里取的 TIMER 与调用前顶层取的 TIMER 应当**基本相等**（毫秒/1000，
' 差 2 秒以内都算对）；取到残值就落在这个区间之外。
' EXPECT: D=1
A# = TIMER
CALL P()
END
SUB P ()
  B# = TIMER
  D = 0
  '   ⚠ 这里**故意写成嵌套 IF**：`AND` 连接两个浮点比较是**另一个已知缺陷**
  '   （见 `46-double-and-or.bas`），写成 `IF B#>=A# AND B#<=A#+2` 会把本用例
  '   变成"测 AND"而不是"测 TIMER"。
  IF B# >= A# THEN
    IF B# <= A# + 2 THEN D = 1
  END IF
  PRINT "D="; D
END SUB
