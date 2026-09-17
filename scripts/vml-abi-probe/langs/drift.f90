! 栈漂移探针（Fortran）：`**` 被编成 CALL ipow，那条路径「压了必须由调用方清」。
! 若净漏 8 字节/次，6 次求幂的漂移会把循环变量或累加器踩花。
! 判据：`DRIFT=126`（2^1+…+2^6）。2026-09-17 修复前实测 66。
program drift
  implicit none
  integer :: i
  integer :: s
  integer :: k
  s = 0
  do i = 1, 6
    s = s + (2 ** i)
  end do
  k = print_str('DRIFT=')
  print *, s
end program drift
