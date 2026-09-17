# 栈漂移探针（Python）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
# 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
#
# 打印不用 print()：它的实参之间会插一个空格、每次调用还额外换行（CodeGenerator.Expressions.cs:199），
# 拼不出 "DRIFT=126" 一行 —— 与 corpus/python/skel.py 同一处置，改用 print_str + println_int。
#
# ⚠ 审计记「Python 的被调方仍从 R0-R3 拷形参、而调用点只压栈 ⇒ 只有 arg0 侥幸正确」。
#    注意 arg0 这里是常量 2，若 R0 恰好残留 2 就会假绿；判据 126 对「第 2 参读成表里剩下的东西」
#    依然敏感（第 2 参是循环变量 i，必须是 1..6 的实时值）。
def main():
    s = 0
    for i in range(1, 7):
        s = s + ipow(2, i)
    print_str("DRIFT=")
    println_int(s)

main()
