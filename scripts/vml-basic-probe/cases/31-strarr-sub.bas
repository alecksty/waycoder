' 字符串数组在 **SUB 内**「写后即读」读到的是**旧值**（写要晚一拍才可见）。
'
' 与 `29-strarray.bas`（元素直接进表达式被读成整数）是**同一族但不同环**：
' 29 那条在顶层也复现；这一条**只在 SUB 内发作** —— 顶层写后即读是好的
' （`T1=x|T2=y|T3=z`），一进 SUB 就变成"读到的永远是上一个值"。
'
' 后果不是报错，而是**累积类代码静默丢数据**：本用例里逐字符拼一行，
' 第一个字符必丢（`RESULT=bc` 而不是 `abc`）—— 因为第二轮读回来的是空串，
' 于是把第一轮写的 `a` 覆盖掉。
'
' 发现经过：控制台垫层 `_tty.bas` 的 `ttyPn` 就是"读当前行 → 追加一个字符 → 写回"，
' 在 SUB 里 ⇒ 屏幕缓冲一个字符都存不住（实测 `LEN(行0)=0`、光标却往前跑了 11 行）。
'
' **绕法**：屏缓这类读写别用数组，改成模块级标量字符串 + `SELECT CASE` 分派；
' 或者干脆不缓冲 —— 老游戏的输出是顺序的，画完就上屏、不必回读。
DIM arr(4) AS STRING
DIM t AS STRING
DIM i AS INTEGER
SUB fill(s AS STRING)
    DIM ch AS STRING
    i = 1
    WHILE i <= LEN(s)
        t = arr(0)
        ch = MID$(s, i, 1)
        arr(0) = t + ch
        i = i + 1
    WEND
END SUB
fill("abc")
t = arr(0)
PRINT "INSUB="; t
DIM b(2) AS STRING
b(0) = "x"
t = b(0)
PRINT "T1="; t
' KNOWN-RED: SUB 内字符串数组写后即读读到旧值（累积丢数据）
' EXPECT: INSUB=abc|T1=x
