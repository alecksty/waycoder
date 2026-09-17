! skel.f90 —— Fortran 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
!
! 取材 Examples/fortran/math/main.f90（`program … implicit none … end program`）与
! Examples/fortran/bench.f90（`do i = 1, 4 … end do`）；前端实现 VMLPrepares/FortranCompiler/。
!
! 已修（原本跑不过，现在这两条都是前端的回归点）：
!  ① 数组**读**：表达式里的 `a(i)` 原先被解析成 FuncCallNode → `CALL func_a`
!     → 链接期「未找到标签: func_a」、运行期 KeyNotFound。同款缺陷在**上游真例子**
!     Examples/fortran/sorting/main.f90（`arr(j)`）上同样复现 ⇒ 是前端缺陷，不是语料写错。
!     现在 Parser 按「声明成了数组」判成 ArrayElemNode（见 ArrayElemNode / IsDeclaredArray），
!     并且 `integer :: a(4)` 会真的留出 4×4 字节栈帧（GenerateVarDecl → GetVarByteSize）。
!  ② `call ui_rect(...)` 会被编成 `CALL sub_ui_rect`，而链接器只剥
!     func_/word_/method_/var_ 四种前缀（LibraryLinker.cs:162-182），**不含 sub_**。
!     所以这里改用函数调用表达式 `k = ui_rect(...)` → `CALL func_ui_rect` → 可解析到
!     lib_vmlui_ui_rect（实参逆序压栈，Expressions.cs:196-205）。这是偏离标准 Fortran
!     的一处，纯粹为了碰得到 UI 库。
!
! 有意偏离（不是缺陷）：
!  ③ 打印**不用** `print *, 'SKEL-SUM=', s`：Fortran 的列表输出在列表项之间本来就插
!     分隔符（correct Fortran 会打出 `SKEL-SUM= 14`），拿不到逐字节的 `SKEL-SUM=14`。
!     这是语料的期望错，不是前端缺陷。照 Go/Python/Kotlin/Java/C# 语料的既有做法：
!     先打一个**不带换行**的字符串（`k = print_str(...)`，k 只是接住返回值），
!     再单独打这个数（单个列表项不插分隔符）⇒ 输出恰好 `SKEL-SUM=14`。
!  ④ Fortran 数组 1-based ⇒ 下标写 1..4，循环 do i = 1, 4。

program skel
  implicit none
  integer :: a(4)
  integer :: i
  integer :: s
  integer :: k

  a(1) = 1
  a(2) = 2
  a(3) = 3
  a(4) = 4
  s = 0
  do i = 1, 4
    a(i) = inc(a(i))
    s = s + a(i)
  end do
  k = print_str('SKEL-SUM=')
  print *, s
  k = ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
  k = ui_present()
contains
  integer function inc(x)
    integer :: x
    inc = x + 1
  end function inc
end program skel
