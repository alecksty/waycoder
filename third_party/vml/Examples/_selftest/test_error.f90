! test_error.f90 —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
! test_error.f90 -- VML diagnostic self-test case (**deliberately contains errors, not part of the compile-cleanliness check**)
!
! 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
! Purpose: verify the compiler's error/warning output, the diagnostic list, and whether the editor can show a squiggle.
! 预期：**1 个错误 + 1 个警告**（f90 是这五门语言里唯一能同时产出两者的）。
! Expected: **1 error + 1 warning** (f90 is the only one of these five languages that can produce both).
! 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
! Constraints: deterministic -- no randomness, no file reads, no interaction, no infinite loops; the error comes from the source itself.
!
! 两条诊断各有来路：
! The two diagnostics each have their own origin:
!   ① `undefined_var` → 没写 `implicit none` 时未声明变量是**隐式声明**（合法语义），
!   (1) `undefined_var` -> without `implicit none`, an undeclared variable is an **implicit declaration** (legal semantics),
!      故报 warning [CodeGen_UndefinedVariable]；若加一行 `implicit none` 会变成 error。
!      so it reports warning [CodeGen_UndefinedVariable]; adding a line `implicit none` turns it into an error.
!   ② `call undefined_sub()` → 前端不做函数存在性检查，留到链接期报 error。
!   (2) `call undefined_sub()` -> the frontend does not check function existence, it is left for the linker to report as an error.
!   （ForTran 没有未使用变量警告 —— 那是 C 前端独有的。）
!   (ForTran has no unused-variable warning -- that one is unique to the C frontend.)
program p
  integer :: unused
  integer :: x
  unused = 5
  x = undefined_var
  print *, x
  call undefined_sub()
end program p
