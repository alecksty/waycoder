' 形参默认按引用（QBasic 语义）+ 非左值实参的临时量 + 常量不被写穿。
'
' 钉三样东西，每一样都曾经是"跑了才知道"的静默错：
'
' ① **默认 BYREF**。QBasic 的 SUB/FUNCTION 形参默认按引用，所以
'    `SUB GetInputs (…, NumGames)` 里 `NumGames = 3` 必须传回调用方。
'    默认 BYVAL 时它传不回 ⇒ 调用方读到 0 ⇒ `FOR i = 1 TO NumGames` 一次都不跑。
'    GORILLA.BAS 的 PlayGame 整局不画一个像素就是这个（场景 0 图元）。
'
' ② **非左值实参要有临时量**。`addone 3` / `addone x + 1` 这种实参取不了地址。
'    QBasic 的做法是造个临时量把它的地址传出去；不造就会把**字面量当地址**交给被调方
'    （写下去是野地址）。早期正是拿"字面量传不了引用"当理由默认了 BYVAL。
'
' ③ **常量不能被写穿**。`CONST K = 8` 传进 SUB，SUB 里的赋值只能改临时量。
'    `SUNHAPPY`/`FALSE` 这类在 GORILLA.BAS 里是常量实参，被写穿就是把常量区改了。
'
' 附带锁住 **CRT 图形语句在 SUB 里的那条老崩溃**（同族，判据是"跑得完"）：
'    `COLOR 7` 会调库里的 `CRT_TEXTCOLOR`，而那个函数曾把形参 `c` 赋值
'    ⇒ 编成对 `[R12+12]` 的写 ⇒ 落到调用方保存的 BP 上 ⇒ 返回后 `R12=7`
'    ⇒ 下一条 `[R12-12]` 内存错误（PC=045B）。这条是它的最小复现。
SUB setit (n)
    n = 42
END SUB

SUB setstr (s)
    s = "hi"
END SUB

SUB addone (n)
    n = n + 1
END SUB

SUB dotwice (a, b)
    a = a * 2
    b = b * 2
END SUB

SUB colorbox ()
    COLOR 7
END SUB

DIM x AS INTEGER
DIM t AS STRING
DIM i AS INTEGER
DIM j AS INTEGER

x = 0
setit x
PRINT "BYREF="; x

t = ""
setstr t
PRINT "STR="; t

i = 5
addone i
PRINT "SUB="; i
addone 3
addone i + 1
PRINT "TEMP="; i
addone i
PRINT "SUB2="; i

CONST K = 8
addone K
PRINT "CONST="; K

i = 3
j = 4
dotwice i, j
PRINT "MULTI="; i; "|"; j

' CRT 语句在 SUB 内（老崩溃点）
colorbox
PRINT "CRT=ok"
' EXPECT: BYREF=42|STR=hi|SUB=6|TEMP=6|SUB2=7|CONST=8|MULTI=6|8|CRT=ok
