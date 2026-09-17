// out.f90 —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
program p
  print '(A)', 'OUT-STR=abc'
  print '(I0)', 42
  print '(A)', 'OUT-PUN=hello, world'
end program p
