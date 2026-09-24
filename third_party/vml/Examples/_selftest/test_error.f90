! test_error.f90 —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
!
! 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
! 预期：**1 个错误 + 1 个警告**（f90 是这五门语言里唯一能同时产出两者的）。
! 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
!
! 两条诊断各有来路：
!   ① `undefined_var` → 没写 `implicit none` 时未声明变量是**隐式声明**（合法语义），
!      故报 warning [CodeGen_UndefinedVariable]；若加一行 `implicit none` 会变成 error。
!   ② `call undefined_sub()` → 前端不做函数存在性检查，留到链接期报 error。
!   （ForTran 没有未使用变量警告 —— 那是 C 前端独有的。）
program p
  integer :: unused
  integer :: x
  unused = 5
  x = undefined_var
  print *, x
  call undefined_sub()
end program p
