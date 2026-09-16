! skel.f90 —— Fortran 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
!
! 取材 Examples/fortran/math/main.f90（`program … implicit none … end program`）与
! Examples/fortran/bench.f90（`do i = 1, 4 … end do`）；前端实现 VMLPrepares/FortranCompiler/。
!
! 故意踩 / 有意偏离（跑不过=产品缺陷，不是语料写错）：
!  ① 数组**读**不可能：表达式里的 `a(i)` 被解析成 FuncCallNode（Parser.cs:820），
!     会编成 `CALL func_a` —— 前端没有「数组标量读」的代码路径。写是有的
!     （Statements.cs:418-452），但 `integer :: a(4)` 只分配 **1 个** 4 字节栈槽
!     （GenerateVarDecl → AllocStackSlot）⇒ 写也是踩坏邻居。这是最可能的死点。
!  ② `call ui_rect(...)` 会被编成 `CALL sub_ui_rect`，而链接器只剥
!     func_/word_/method_/var_ 四种前缀（LibraryLinker.cs:162-182），**不含 sub_**。
!     所以这里改用函数调用表达式 `k = ui_rect(...)` → `CALL func_ui_rect` → 可解析到
!     lib_vmlui_ui_rect（实参逆序压栈，Expressions.cs:196-205）。这是偏离标准 Fortran
!     的一处，纯粹为了碰得到 UI 库；若这条也不通，Fortran 就没有裸标签调用的路。
!  ③ `print *,` 在实参之间**强行插一个空格**（CompilerBase/CodeGeneratorBase.cs:610-625
!     addSpace 默认 true）⇒ 只能打出 `SKEL-SUM= 14`，拿不到逐字节的 `SKEL-SUM=14`。
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
  print *, 'SKEL-SUM=', s
  k = ui_rect(10, 10, 50, 50, -65536, 1, 0, 0)
  k = ui_present()
contains
  integer function inc(x)
    integer :: x
    inc = x + 1
  end function inc
end program skel
