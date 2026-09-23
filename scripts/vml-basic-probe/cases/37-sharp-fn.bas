' `FUNCTION f#` / `FUNCTION f!` 的**符号名不能带 `#`/`!`**。
'
' `VMLAssembler.IsValidLabel` 只认字母/数字/`_`/`$`，于是 `func_getnum#` 不是标签
' ⇒ 被当成**立即数**（`Operand(IMMEDIATE, "func_getnum#")`）⇒ 运行期
' `ExecuteCall` 的 `(int)operand.Value` 抛
' `InvalidCastException: Unable to cast 'System.String' to 'System.Int32'`。
' 阴险处：**只在真的调到那个函数时才炸**（GORILLA.BAS 前面全跑得动，
' 走到 `DoShot` 里的 `GetNum#(2, …)` 才崩）。
FUNCTION GetNum# (Row, Col)
  GetNum# = Row * 10 + Col
END FUNCTION

FUNCTION Delay! ()
  Delay! = 7
END FUNCTION

SUB UseIt ()
  PRINT "S="; GetNum#(3, 4)
END SUB

PRINT "A="; GetNum#(1, 2)
CALL UseIt
PRINT "B="; Delay!()
' EXPECT: A=12|S=34|B=7
