' **全局变量的字节偏移不许被后续赋值改写**。
'
' `GetVarByteOffset` 原来是现算 `GetVarByteSize(GetVariableType(名))`，而
' `GetVariableType` 会被后面的赋值改写：`gravity# = VAL(grav$)`（VAL 返回 Single）
' 把 `gravity#` 从 Double(8B) 改成 Single(4B) ⇒ **它之后所有全局变量少 4 字节**，
' 而主程序那份地址早已编进指令里 ⇒ 主程序与 SUB 对"全局变量在哪"各执一词。
'
' 实测 GORILLA.BAS：`Mode` 在主程序里读 `#21988`、在 `MakeCityScape` 里读 `#21984`
' （= 主程序里 `ScrWidth` 的槽）⇒ `IF Mode = 9` 走 else 分支
' ⇒ `BottomLine` 335→190、`HtInc` 10→6 ⇒ 整座城市画到屏幕外，一个错都不报。
'
' 判据：主程序读一遍、SUB 里读一遍，两次必须一致（且为写入值）。
DIM SHARED gravity#
DIM SHARED Mode
DIM SHARED ScrWidth
DIM SHARED TailMark

SUB SetG (v)
  gravity# = v * 1.0
END SUB

SUB Check ()
  IF Mode = 9 THEN PRINT "M9="; 1 ELSE PRINT "M9="; 0
  PRINT "W="; ScrWidth
  PRINT "T="; TailMark
END SUB

Mode = 9
ScrWidth = 640
TailMark = 12345
CALL SetG(9)
PRINT "MAINW="; ScrWidth
PRINT "MAINT="; TailMark
CALL Check
' EXPECT: MAINW=640|MAINT=12345|M9=1|W=640|T=12345
