! VML 全类型转换库 — Fortran 包装器 (v1.66.44)
! 用法: use conv

module conv
  interface
    function int_to_str(val) result(s)
      integer, intent(in) :: val
      character(len=:), pointer :: s
    end function
    function str_to_int(s) result(val)
      character(len=*), intent(in) :: s
      integer :: val
    end function
    function float_to_str(f) result(s)
      real, intent(in) :: f
      character(len=:), pointer :: s
    end function
    function str_to_float(s) result(f)
      character(len=*), intent(in) :: s
      real :: f
    end function
    function double_to_str(d) result(s)
      real(8), intent(in) :: d
      character(len=:), pointer :: s
    end function
    function str_to_double(s) result(d)
      character(len=*), intent(in) :: s
      real(8) :: d
    end function
  end interface
end module conv
