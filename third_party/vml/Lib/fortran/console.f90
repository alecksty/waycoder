! VML Console library — Fortran

module console
  implicit none
contains
  function console_clear() result(r)
    integer :: r
    r = 0
  end function

  function console_gotoxy(x, y) result(r)
    integer, intent(in) :: x, y
    integer :: r
    r = 0
  end function

  function console_textcolor(fg, bg) result(r)
    integer, intent(in) :: fg, bg
    integer :: r
    r = 0
  end function
end module console
