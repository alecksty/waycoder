' 无参函数的**裸名调用**（不写括号）：QBasic 里 `X = F` 与 `X = F()` 等价。
'
' 前端原来一律当"普通标识符" ⇒ 取的是**同名变量**（多半从没被赋过值 = 0），
' **一个错都不报**。实测原型：GORILLA.BAS 的 `MachSpeed = CalcDelay`
' （`DECLARE FUNCTION CalcDelay! ()` 登记的是带 `!` 的名字，调用点却不带后缀）
' ⇒ MachSpeed 恒为 0 ⇒ `SUB Rest` 的等待时间 `MachSpeed * t# / SPEEDCONST` 恒为 0
' ⇒ 香蕉每走一步都退化成"等到下一秒"。
'
' 判据：`X = F` 与 `PRINT F` 都必须拿到 7。
' EXPECT: P=7|Q=7
X = F
PRINT "P="; X
PRINT "Q="; F
END
FUNCTION F ()
  F = 7
END FUNCTION
