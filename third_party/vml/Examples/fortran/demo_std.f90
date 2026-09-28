! demo_std.f90 —— Fortran 第一层：**标准输入输出**（std）
! demo_std.f90 -- Fortran layer one: **standard output** (std)
!
! 这一层只用 Fortran 自己的 `print`，不碰 Crt / 图形 / 宿主 `ui_*` ——
! This layer uses only Fortran's own `print`, touching neither Crt / graphics / the host `ui_*` --
! 所以它在任何后端上都是同一份行为，也是四份 demo 里唯一有"逐字节确定性输出"
! so its behavior is identical on every backend, and it is the only one of the four demos with a
! 判据的一份（另外几份只能验"编得过、跑得完、不挂死"）。
! "byte-for-byte deterministic output" criterion (the others can only verify "it compiles, it finishes, it does not hang").
!
! 跑法（桌面 vmlcli）：
! How to run (desktop vmlcli):
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_std.f90
!
! 期望输出（逐字节）：
! Expected output (byte-for-byte; each line below is printed in Chinese and the line under it is its translation):
!   === Fortran 标准输出 demo ===
!   === Fortran std output demo ===
!   字符串: 你好，世界
!   String: hello, world
!   整数: 42
!   Integer: 42
!   计算: 7 * 6 = 42
!   Computed: 7 * 6 = 42
!   整除: 17 / 5 = 3
!   Integer division: 17 / 5 = 3
!   取余: mod(17,5) = 2
!   Remainder: mod(17,5) = 2
!   循环求和: 1..10 = 55
!   Loop sum: 1..10 = 55
!   阶乘: 10! = 3628800
!   Factorial: 10! = 3628800
!   实数: 2.5 * 4.0 = 10
!   Real: 2.5 * 4.0 = 10
!   === done ===
!
! ◆ 一条**已修**的缺陷，留在这儿备查（本文件现在敢直接打浮点了，就是因为它修好了）
! ◆ One **already fixed** defect, kept here for reference (this file now dares to print floats directly because it was fixed)
!
!     以前：`print *, 2.5` ⇒ `1075838976`、`print *, x * 4.0` ⇒ `1092616192`
!     Before: `print *, 2.5` => `1075838976`, `print *, x * 4.0` => `1092616192`
!     （1075838976 = 0x40200000 = 2.5f 的位模式、1092616192 = 0x41200000 = 10.0f 的）
!     (1075838976 = 0x40200000 = the bit pattern of 2.5f, 1092616192 = 0x41200000 = that of 10.0f)
!     —— 值算对了，只是 `print` 一律走**整数格式**那条路。
!     -- the value was computed correctly, but `print` always took the **integer format** path.
!
!     真身：`CompilerBase/CodeGeneratorBase.EmitPrintArgs` 只把 `isArgString` 转给
!     Root cause: `CompilerBase/CodeGeneratorBase.EmitPrintArgs` passed only `isArgString` on to
!     `EmitPrintArg`，**`isFloat` 走的是默认值 `false`** ⇒ 每个实参都 `CALL print_int`。
!     `EmitPrintArg`, and **`isFloat` took its default value `false`** => every argument did `CALL print_int`.
!     修法：给 `EmitPrintArgs` 追加了一个可选参数 `isArgFloat`（追加在末尾，
!     Fix: an optional parameter `isArgFloat` was appended to `EmitPrintArgs` (appended at the end,
!     老调用点的位置参数一个都不用动），Fortran 的 `GeneratePrint` 用现成的
!     so no positional argument at the old call sites has to change), and Fortran's `GeneratePrint` passes it in
!     `GetExprType(...) == F32/F64` 传进去。判据：`print *, 2.5` ⇒ `2.5`、
!     using the ready-made `GetExprType(...) == F32/F64`. Criterion: `print *, 2.5` => `2.5`,
!     `print *, x * 4.0`（`x=2.5`）⇒ `10`。
!
! ◆ ⚠ **仍未修**：`double precision` 的 **`d0` 后缀字面量恒为 0**
! ◆ ⚠ **Still not fixed**: for `double precision`, a **literal with the `d0` suffix is always 0**
!
!     double precision :: d
!     print *, 1.25d0            ⇒ 0     （应 1.25）
!     print *, 1.25d0            => 0     (should be 1.25)
!     d = 9.5d0 / print *, d     ⇒ 0     （应 9.5）
!     d = 9.5d0 / print *, d     => 0     (should be 9.5)
!     而 `d = 1.25`（不带后缀）⇒ 1.25 ✅
!     while `d = 1.25` (no suffix) => 1.25 OK
!
!   ⇒ 所以本文件里**一个 `d0` 字面量都没有**；要写双精度就写不带后缀的
!   => So this file contains **not a single `d0` literal**; to write double precision, write a suffix-less
!     十进制字面量（那个是好的）。这条已记进 `FRONTEND_DEFECTS.md`。
!     decimal literal (that one works). This is recorded in `FRONTEND_DEFECTS.md`.
!
! ◆ 三条本前端的写法要求（都是实测出来的，不是风格偏好）
! ◆ Three style requirements of this frontend (all measured, not style preferences)
!
!   ① **每条语句顶格写不受限，但 `print` 的输出格式是"列表式"** —— 每个实参之间
!   ① **Statements may start at column one without restriction, but `print`'s output format is "list-directed"** -- between each argument
!      由运行时自己补分隔（本平台是空格），所以 `print *, 'a', 'b'` 打出 `a b`。
!      the runtime inserts its own separator (a space on this platform), so `print *, 'a', 'b'` prints `a b`.
!      要紧凑就**拼进同一个字符串字面量**。
!      For a compact result, **put it all into one string literal**.
!
!   ② **整数 `/` 是整除**（Fortran 的语义本来就如此）：`17 / 5` = 3。
!   ② **Integer `/` is integer division** (that is Fortran's semantics anyway): `17 / 5` = 3.
!      取余用内建的 `mod(a, b)` —— 前端把它编成 VML 的原生 `MOD` 指令，
!      Use the built-in `mod(a, b)` for the remainder -- the frontend compiles it to VML's native `MOD` instruction
!      不走库调用（否则会去链一个不存在的 `func_mod`，实测炸过）。
!      instead of a library call (otherwise it links a nonexistent `func_mod`, which crashed in testing).
!
!   ③ `call xxx(...)` 与 `k = xxx(...)` **不是同一件事**：
!   ③ `call xxx(...)` and `k = xxx(...)` **are not the same thing**:
!      · `call putchar(27)` 编成 `CALL putchar`（裸名，找得到库里的实现）
!      · `call putchar(27)` compiles to `CALL putchar` (the bare name, which finds the library implementation)
!      · 但 `call ui_rect(...)` 会编成 `CALL sub_ui_rect`，而**链接器只剥
!      · but `call ui_rect(...)` compiles to `CALL sub_ui_rect`, and **the linker strips only
!        `func_`/`word_`/`method_`/`var_` 四种前缀，不含 `sub_`** ⇒ 碰不到 UI 库。
!        the four prefixes `func_`/`word_`/`method_`/`var_`, not `sub_`** => it never reaches the UI library.
!      所以调库函数一律写成**函数调用表达式**：`k = ui_rect(...)`（编成 `func_ui_rect`）。
!      So library functions are always written as a **function-call expression**: `k = ui_rect(...)` (compiles to `func_ui_rect`).
!      本文件不涉及，但 `demo_ui.f90` 全靠这一条。
!      This file is not affected, but `demo_ui.f90` depends entirely on this rule.

program demo_std
  implicit none
  integer :: i
  integer :: sum
  integer :: fact
  integer :: lang

  ! 界面语言：0 = 中文 / 1 = 英文（ui_get_language 是 syscall，开局查一次）
  ! UI language: 0 = Chinese / 1 = English (ui_get_language is a syscall, queried once at startup)
  lang = ui_get_language()

  if (lang == 0) print *, '=== Fortran 标准输出 demo ==='
  if (lang /= 0) print *, '=== Fortran std output demo ==='

  ! ① 字符串字面量（含中文 —— 源码按 UTF-8 存，词法器直通）
  ! ① String literal (contains Chinese -- the source is stored as UTF-8 and the lexer passes it through)
  if (lang == 0) print *, '字符串: 你好，世界'
  if (lang /= 0) print *, 'String: hello, world'

  ! ② 整数
  ! ② Integer
  if (lang == 0) print *, '整数: 42'
  if (lang /= 0) print *, 'Integer: 42'

  ! ③ 整数运算
  ! ③ Integer arithmetic
  if (lang == 0) print *, '计算: 7 * 6 = 42'
  if (lang /= 0) print *, 'Computed: 7 * 6 = 42'

  ! ④ 整数除法与取余（`/` 整除；取余用内建 `mod`）
  ! ④ Integer division and remainder (`/` is integer division; the remainder uses the built-in `mod`)
  if (lang == 0) print *, '整除: 17 / 5 = 3'
  if (lang /= 0) print *, 'Integer division: 17 / 5 = 3'
  if (lang == 0) print *, '取余: mod(17,5) = 2'
  if (lang /= 0) print *, 'Remainder: mod(17,5) = 2'

  ! ⑤ 用变量真算一遍 —— 上面几行是字面量，这几行让编译器真的去算
  ! ⑤ Actually compute it with variables -- the lines above are literals, these make the compiler really compute
  sum = 0
  do i = 1, 10
    sum = sum + i
  end do
  if (lang == 0) print *, '循环求和: 1..10 =', sum
  if (lang /= 0) print *, 'Loop sum: 1..10 =', sum

  fact = 1
  do i = 1, 10
    fact = fact * i
  end do
  if (lang == 0) print *, '阶乘: 10! =', fact
  if (lang /= 0) print *, 'Factorial: 10! =', fact

  ! ⑥ 实数：`print` 现在能正确打浮点了（见文件头那条"已修"）
  ! ⑥ Reals: `print` can now print floats correctly (see the "already fixed" item in the file header)
  if (lang == 0) print *, '实数: 2.5 * 4.0 =', 2.5 * 4.0
  if (lang /= 0) print *, 'Real: 2.5 * 4.0 =', 2.5 * 4.0

  print *, '=== done ==='
end program demo_std
