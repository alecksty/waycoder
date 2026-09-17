# out.r —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
# 写法照 corpus/r/skel.* —— 共享库同时提供 println_str / println_int。
cat("OUT-STR=abc")
cat("
")
cat("OUT-INT=")
print(42)
cat("
")
cat("OUT-PUN=hello, world")
cat("
")
