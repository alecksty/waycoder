' f.bas —— 浮点：常量、运算、负数、十六进制
' ⚠ 判据是**逐行比**（标签自己占一行）⇒ 用两条 PRINT；
'   `PRINT "x="; v` 会把标签和值打在同一行（BASIC 的分号是"接在同一行"）。
PRINT "F-MUL="
PRINT INT(3.14# * 2.0# * 100.0#)
PRINT "F-NEG="
PRINT INT(-0.5# * 100.0#)
PRINT "F-HEX="
PRINT INT(&H10 * 100.0#)
