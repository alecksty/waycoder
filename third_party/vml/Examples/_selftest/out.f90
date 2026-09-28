! out.f90 —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
! out.f90 -- VML cross-language "output" probe (exactly three lines, see run-langs.sh)
!
! ⚠ 本前端**只支持表控输出 `print *`**，不支持格式化 `print '(A,I0)'` ——
! ⚠ This frontend **only supports list-directed output `print *`**, not formatted `print '(A,I0)'` --
!   写成后者编译期直接抛 `Fortran parse error: expected * after print (got StringLiteral)`。
!   writing the latter makes the compiler throw `Fortran parse error: expected * after print (got StringLiteral)` at compile time.
! ⚠ 表控输出会在各项之间**插分隔符**（Fortran 的 list-directed 语义），
! ⚠ List-directed output **inserts separators** between items (Fortran's list-directed semantics),
!   所以本探针的期望见 out.f90.expect（不能套默认那三行）。
!   so this probe's expectation lives in out.f90.expect (the default three lines do not apply here).
program p
  print *, 'OUT-STR=abc'
  print *, 'OUT-INT=', 42
  print *, 'OUT-PUN=hello, world'
end program p
