! out.f90 —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
!
! ⚠ 本前端**只支持表控输出 `print *`**，不支持格式化 `print '(A,I0)'` ——
!   写成后者编译期直接抛 `Fortran parse error: expected * after print (got StringLiteral)`。
! ⚠ 表控输出会在各项之间**插分隔符**（Fortran 的 list-directed 语义），
!   所以本探针的期望见 out.f90.expect（不能套默认那三行）。
program p
  print *, 'OUT-STR=abc'
  print *, 'OUT-INT=', 42
  print *, 'OUT-PUN=hello, world'
end program p
