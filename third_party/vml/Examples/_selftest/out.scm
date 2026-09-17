; out.scm —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
; 写法照 corpus/scm/skel.* —— 共享库同时提供 println_str / println_int。
(display "OUT-STR=abc")
(newline)
(display "OUT-INT=")
(display 42)
(newline)
(display "OUT-PUN=hello, world")
(newline)
