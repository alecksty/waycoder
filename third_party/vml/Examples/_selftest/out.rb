# out.rb —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
# 写法照 corpus/rb/skel.* —— 共享库同时提供 println_str / println_int。
puts("OUT-STR=abc")
print("OUT-INT=")
puts(42)
puts("OUT-PUN=hello, world")
