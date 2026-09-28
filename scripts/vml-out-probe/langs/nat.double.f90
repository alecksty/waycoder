program p
  ! nat.double.f90 —— Fortran 的 **8 字节类型**（double precision）走一遍
  !
  ! 为什么单独立一条：`double precision` 变量**一读就编不过**过很久 ——
  ! 前端按类型选出 `MOVED`（D 组指令）却把寄存器号写成了通用号的 1/2
  ! ⇒ 汇编期的寄存器类闸直接判死（`MOVED 的第 1 个操作数要 D0-D7，给的是 R1`）。
  ! 根因是「寄存器类由助记符裁决」那轮统一漏了 FortranCompiler
  ! （`RegOf`/`BankOfOperand` 在那门里引用数为 0）。
  !
  ! 判据取**整数化之后的值**（`int(d * 100)`）而不是直接打印浮点：
  ! 各语言的浮点打印格式不同，走整数就不会把"格式差异"读成"算错了"。
  double precision :: d
  integer :: i
  d = 9.5d0
  i = int(d * 100)
  print *, i
  i = 3
  d = dble(i) * 2.0d0
  i = int(d * 10)
  print *, i
end program p
