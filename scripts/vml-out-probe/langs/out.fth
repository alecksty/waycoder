\ out.fth —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
\ 写法照 corpus/fth/skel.* —— 共享库同时提供 println_str / println_int。
." OUT-STR=abc" CR
." OUT-INT=" 42 . CR
." OUT-PUN=hello, world" CR
