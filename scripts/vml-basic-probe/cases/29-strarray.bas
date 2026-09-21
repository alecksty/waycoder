' 字符串数组元素在「表达式位置」被当成整型读 —— 读回的是那个串的地址。
'
' 起因：要给 1970~80 年代的"打字机式"老 BASIC 游戏做一层文本控制台垫层，
' 而屏幕缓冲天然要用字符串数组。此前 `07-str.bas` 只覆盖了**标量**字符串
' （`DIM a AS STRING`），`DIM x(n) AS STRING` 一条判据都没有。
'
' 实测把"存 / 取 / 传"三环拆开，坏点收得很干净 —— **只有直接进表达式那一环坏**：
'   · `LEN(s(0))`            ✅ 5        （当字符串用，对）
'   · `t$ = s(0)`            ✅ alpha    （赋给字符串左值，对）
'   · `PRINT s(0)`           ❌ 1024     （当整数读，是那个串的地址）
'   · `(s(0) = "alpha")`     ❌ 0        （应 -1）
' 下标本身不串、SUB 内读写模块级字符串数组也正常（见下方 S-* 三项）。
'
' 与 C 前端修过的那条同源（`InferExpressionType(ArrayAccess)` 只查 `variableTypes`，
' 查不到就退化成 `ExprType.Int`）—— 即**元素类型推断没进表达式求值那条路**。
'
' **绕法**（便宜且机械，垫层已按此写）：每次读取先过一次标量临时变量 ——
'   `tt$ = s(i)` 然后只用 `tt$`。存（`s(i) = "..."`）本来就是好的。
DIM s(4) AS STRING
s(0) = "alpha"
s(1) = "beta"
PRINT "LEN0="; LEN(s(0))                 ' 可用的那一环
t$ = s(0)
PRINT "SCALAR="; t$                      ' 可用的那一环
PRINT "DIRECT="; s(0)                    ' 坏点：期望 alpha
PRINT "CMP="; (s(0) = "alpha")           ' 坏点：期望 -1
SUB inSub()
    s(2) = "gamma"
    PRINT "S-LEN="; LEN(s(2))
    u$ = s(2)
    PRINT "S-SCALAR="; u$
END SUB
inSub()
DIM m(2, 2) AS STRING
m(1, 1) = "two-d"
w$ = m(1, 1)
PRINT "M11="; w$
' KNOWN-RED: 元素直接进表达式读成整数（地址），见 FRONTEND_DEFECTS.md
' EXPECT: LEN0=5|SCALAR=alpha|DIRECT=alpha|CMP=-1|S-LEN=5|S-SCALAR=gamma|M11=two-d
