' nat.bas —— **NATIVE 声明后面必须还能有语句**（BASIC 前端）
'
' 钉的是一个「零输出、零报错」的静默缺陷：`NATIVE FUNCTION f() AS INTEGER` 这种
' **外部符号声明**本来没有「体」，但解析器照旧进体循环、一路吃到 `END FUNCTION`。
' 不写 `END` 时它就吃到 **EOF** ⇒ **该声明后面整个程序都被当成了它的体**；
' 而 NATIVE 的体**不生成代码** ⇒ 主程序一条语句都不剩 ⇒ 编出一份
' 「完全合法、什么也不做」的程序：退出码 0、屏幕上没有一个字。
'
' 实测 `Examples/basic/sysinfo.bas` 就是这么坏的（`statements=1 FunctionDeclaration=1`）。
'
' 两种写法都要覆盖，因为修完之后它们是**两条不同的分支**：
'   · `f_dummy`：不写 END（正解是「NATIVE 不吃后面的语句」）
'   · `g_dummy`：带空体 `END SUB`（既有例子的土办法，向后兼容）
' 少测任何一条，另一条坏了都看不出来。
'
' 判据就是下面三行 PRINT —— 它们被吞掉时本探针输出为空、run-langs.sh 直接报 FAIL。
' dummy 只声明不调用（调了会链接失败：本探针不打算真提供这两个符号）。

NATIVE FUNCTION f_dummy() AS INTEGER

NATIVE SUB g_dummy()
END SUB

PRINT "OUT-STR=abc"
PRINT "OUT-INT="; 42
PRINT "OUT-PUN=hello, world"
