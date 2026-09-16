# skel.py —— VML 骨架程序（Python）。
#
# 习惯用法抄自 Examples/python/tetris.py：顶层直接写语句、裸调库函数（ui_rect / ui_present
# 就是这么调的）、颜色写 0x 十六进制。
# 两条实测约束（tetris.py 文件头）：① 列表**读可以、写不生效**（a[i] = v 之后读回来还是 0）
# —— 这里照写不规避，正是要测这一条；② 本前端没有 asm()。
# 打印不用 print()：print 的实参之间会插一个空格、每次调用都额外换行（CodeGenerator.Expressions.cs:199），
# 拼不出 "SKEL-SUM=14" 一行；所以直接调库函数 print_str（SYSCALL #1，无换行）
# + println_int（SYSCALL #6 + 换行）。两者都是「不认识的函数名 ⇒ 按标签裸 CALL」那条兜底分支。
def plus1(x):
    return x + 1

a = [1, 2, 3, 4]
s = 0
for i in range(4):
    a[i] = plus1(a[i])
    s = s + a[i]

ui_rect(10, 10, 50, 50, 0xFFFF0000, 1, 0, 0)
ui_present()
print_str("SKEL-SUM=")
println_int(s)
