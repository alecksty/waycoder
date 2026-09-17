' 栈漂移探针（BASIC）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
' 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
'
' 外部函数必须用 `NATIVE` + **空体** 才是裸标签（BasicCompiler/CodeGenerator.Sub.cs:1456-1463），
' 否则会被加上 func_ 前缀、链接期找不到 —— 照 corpus/basic/skel.bas 的既有写法。
' 打印用语言自己的 `PRINT "…"; s`（分号不加分隔符、末尾自动补换行）。

NATIVE FUNCTION ipow(base AS INTEGER, exp AS INTEGER) AS INTEGER
END FUNCTION

DIM i AS INTEGER
DIM s AS INTEGER

s = 0
FOR i = 1 TO 6
    s = s + ipow(2, i)
NEXT
PRINT "DRIFT="; s
