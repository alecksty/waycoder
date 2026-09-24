! demo_std.f90 —— Fortran 第一层：**标准输入输出**（std）
!
! 这一层只用 Fortran 自己的 `print`，不碰 Crt / 图形 / 宿主 `ui_*` ——
! 所以它在任何后端上都是同一份行为，也是四份 demo 里唯一有"逐字节确定性输出"
! 判据的一份（另外几份只能验"编得过、跑得完、不挂死"）。
!
! 跑法（桌面 vmlcli）：
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_std.f90
!
! 期望输出（逐字节）：
!   === Fortran 标准输出 demo ===
!   字符串: 你好，世界
!   整数: 42
!   计算: 7 * 6 = 42
!   整除: 17 / 5 = 3
!   取余: mod(17,5) = 2
!   循环求和: 1..10 = 55
!   阶乘: 10! = 3628800
!   实数: 2.5 * 4.0 = 10
!   === done ===
!
! ◆ 一条**已修**的缺陷，留在这儿备查（本文件现在敢直接打浮点了，就是因为它修好了）
!
!     以前：`print *, 2.5` ⇒ `1075838976`、`print *, x * 4.0` ⇒ `1092616192`
!     （1075838976 = 0x40200000 = 2.5f 的位模式、1092616192 = 0x41200000 = 10.0f 的）
!     —— 值算对了，只是 `print` 一律走**整数格式**那条路。
!
!     真身：`CompilerBase/CodeGeneratorBase.EmitPrintArgs` 只把 `isArgString` 转给
!     `EmitPrintArg`，**`isFloat` 走的是默认值 `false`** ⇒ 每个实参都 `CALL print_int`。
!     修法：给 `EmitPrintArgs` 追加了一个可选参数 `isArgFloat`（追加在末尾，
!     老调用点的位置参数一个都不用动），Fortran 的 `GeneratePrint` 用现成的
!     `GetExprType(...) == F32/F64` 传进去。判据：`print *, 2.5` ⇒ `2.5`、
!     `print *, x * 4.0`（`x=2.5`）⇒ `10`。
!
! ◆ ⚠ **仍未修**：`double precision` 的 **`d0` 后缀字面量恒为 0**
!
!     double precision :: d
!     print *, 1.25d0            ⇒ 0     （应 1.25）
!     d = 9.5d0 / print *, d     ⇒ 0     （应 9.5）
!     而 `d = 1.25`（不带后缀）⇒ 1.25 ✅
!
!   ⇒ 所以本文件里**一个 `d0` 字面量都没有**；要写双精度就写不带后缀的
!     十进制字面量（那个是好的）。这条已记进 `FRONTEND_DEFECTS.md`。
!
! ◆ 三条本前端的写法要求（都是实测出来的，不是风格偏好）
!
!   ① **每条语句顶格写不受限，但 `print` 的输出格式是"列表式"** —— 每个实参之间
!      由运行时自己补分隔（本平台是空格），所以 `print *, 'a', 'b'` 打出 `a b`。
!      要紧凑就**拼进同一个字符串字面量**。
!
!   ② **整数 `/` 是整除**（Fortran 的语义本来就如此）：`17 / 5` = 3。
!      取余用内建的 `mod(a, b)` —— 前端把它编成 VML 的原生 `MOD` 指令，
!      不走库调用（否则会去链一个不存在的 `func_mod`，实测炸过）。
!
!   ③ `call xxx(...)` 与 `k = xxx(...)` **不是同一件事**：
!      · `call putchar(27)` 编成 `CALL putchar`（裸名，找得到库里的实现）
!      · 但 `call ui_rect(...)` 会编成 `CALL sub_ui_rect`，而**链接器只剥
!        `func_`/`word_`/`method_`/`var_` 四种前缀，不含 `sub_`** ⇒ 碰不到 UI 库。
!      所以调库函数一律写成**函数调用表达式**：`k = ui_rect(...)`（编成 `func_ui_rect`）。
!      本文件不涉及，但 `demo_ui.f90` 全靠这一条。

program demo_std
  implicit none
  integer :: i
  integer :: sum
  integer :: fact

  print *, '=== Fortran 标准输出 demo ==='

  ! ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通）
  print *, '字符串: 你好，世界'

  ! ② 整数
  print *, '整数: 42'

  ! ③ 整数运算
  print *, '计算: 7 * 6 = 42'

  ! ④ 整数除法与取余（`/` 整除；取余用内建 `mod`）
  print *, '整除: 17 / 5 = 3'
  print *, '取余: mod(17,5) = 2'

  ! ⑤ 用变量真算一遍 —— 上面几行是字面量，这几行让编译器真的去算
  sum = 0
  do i = 1, 10
    sum = sum + i
  end do
  print *, '循环求和: 1..10 =', sum

  fact = 1
  do i = 1, 10
    fact = fact * i
  end do
  print *, '阶乘: 10! =', fact

  ! ⑥ 实数：`print` 现在能正确打浮点了（见文件头那条"已修"）
  print *, '实数: 2.5 * 4.0 =', 2.5 * 4.0

  print *, '=== done ==='
end program demo_std
