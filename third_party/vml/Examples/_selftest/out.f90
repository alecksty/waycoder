! out.f90 —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
! 写法照 corpus/f90/skel.* —— 共享库同时提供 println_str / println_int。
program p
  print '(A)', 'OUT-STR=abc'
  print '(A,I0)', 'OUT-INT=', 42
  print '(A)', 'OUT-PUN=hello, world'
end program p
