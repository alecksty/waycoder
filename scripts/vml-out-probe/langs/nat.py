# nat.py —— 测**这门语言自己的**标准输出函数（与 out.py 的共享库那条路分开）
# ⚠ 注释前缀必须是 `#`（第一版写成了 `//` ⇒ 编译期直接抛 CompilationException）
# ⚠ Python 的 print(a, b) 在操作数间**插一个空格**，所以本探针的期望是
#   `OUT-INT= 42`（见 nat.py.expect）—— 各语言的分隔语义不同，期望必须分开。
#   实测当前打出的是 ` 1036`（字面量丢了、整数也不对）⇒ **多实参 print 是坏的**，
#   `.expect` 按**正确语义**写，让它继续红着当信号。
print("OUT-STR=abc")
print("OUT-INT=", 42)
print("OUT-PUN=hello, world")
