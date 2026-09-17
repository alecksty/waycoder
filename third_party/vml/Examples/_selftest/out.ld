// out.ld —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/ld/skel.* —— 共享库同时提供 println_str / println_int。
PRINT_STR "OUT-STR=abc"
PRINT_CHAR 10
PRINT_STR "OUT-INT="
PRINT_INT 42
PRINT_CHAR 10
PRINT_STR "OUT-PUN=hello, world"
PRINT_CHAR 10
END_PROGRAM
