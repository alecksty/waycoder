! VML Standard I/O library — Fortran

module stdio
  implicit none
contains
  function putchar(c) result(r)
    integer, intent(in) :: c
    integer :: r
    r = c
  end function

  function getchar() result(r)
    integer :: r
    r = 0
  end function
end module stdio
