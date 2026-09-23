' SUB 里的 `FOR` 用**模块级**循环变量时，循环变量的地址**不能跨循环体保存**。
'
' 生成器把模块级变量的地址算进 **R3**（`EmitStaticAddr` 只能算到寄存器、给不了地址串），
' 而 `NEXT` 之后那句"重新读循环变量"复用了循环体**之前**取回的 "R3" ——
' 循环体整个跑在中间，R3 早被踩烂（本前端最常用的临时寄存器：数组赋值先把值落 R3、
' 图形/文本码到处在用），于是拿残留值当地址读 4 字节。
' 实测 GORILLA.BAS：`SUB PlaceGorillas` 里 `FOR i = 1 TO 2` 的 `i` 是模块级变量，
' 循环体是 `PUT`（图形）⇒ 地址是循环体留下的**浮点位型**（每次跑都不同），
' 当场「内存错误(PC=…): MOVE @R0, @3 — 地址=C00000xx」。
'
' 判据：循环体里把 R3 用成一个**必然越界的地址**（数组赋值的值寄存器就是 R3），
' 修好前这里必崩（负地址/越界），修好后应当正常跑完并打印。
'
' ⚠ 这个形状现在**必须显式写 `DIM SHARED i`** 才成立（v0.96.4xx 起）：
'   没有 SHARED 的循环计数器一律是 **SUB 自己的局部变量**（见下面 `SUB Loc` 与
'   `48-sub-for-shadow.bas`）—— 那种情况下地址根本不进 R3，这条判据也就压不到东西了。
'   所以本节拆成两半：`DIM SHARED` 那份继续压 R3 的坑，非 SHARED 那份压"计数器是局部的"。
DIM G(1 TO 3)
DIM SHARED i
DIM n

SUB Fill ()
  FOR i = 1 TO 3
    G(i) = 100000000
  NEXT i
END SUB

' 同名但**没有 SHARED** ⇒ 这是 SUB 自己的计数器，跨不过 SUB 边界
SUB Loc ()
  FOR n = 1 TO 3
    G(n) = G(n) + 1
  NEXT n
END SUB

CALL Fill
PRINT "I="; i
PRINT "G1="; G(1)
PRINT "G3="; G(3)
CALL Loc
PRINT "N="; n
PRINT "G1B="; G(1)
' EXPECT: I=4|G1=100000000|G3=100000000|N=0|G1B=100000001
