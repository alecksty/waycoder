program p
  implicit none
  integer :: a, b
  a = 10
  b = 2
  if ((a / b) > 3) then
    print *, "DIV-OK"
  end if
end program p
