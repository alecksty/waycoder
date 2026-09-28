! demo_tty.f90 —— Fortran 第二层：**彩色控制台**（tty）
! demo_tty.f90 -- Fortran layer two: **color console** (tty)
!
! Fortran **没有** conio / crt 那一类可用的控制台绑定：
! Fortran has **no** usable console binding of the conio / crt kind:
!   · `Lib/fortran/crt.vml` 里只有自动生成的空壳
!   · `Lib/fortran/crt.vml` holds only an auto-generated empty shell
!   · `Lib/fortran/console.f90` 的三个函数全是 `r = 0` 的空实现（没有实现体）
!   · all three functions in `Lib/fortran/console.f90` are empty `r = 0` stubs (no body at all)
! 所以这一层直接输出 **ANSI 转义序列** —— 与 `Examples/c/ansi_colors.c` 同一个做法、
! So this layer emits **ANSI escape sequences** directly -- the same approach as `Examples/c/ansi_colors.c`,
! 同一套效果（手机端「命令行」页由 `UI/Shared/AnsiMarkup.cs` 翻成彩色富文本）。
! with the same effect (on mobile the "command line" page is turned into colored rich text by `UI/Shared/AnsiMarkup.cs`).
!
! 跑法（桌面 vmlcli）：
! How to run (desktop vmlcli):
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_tty.f90
!
! 画面（从上到下）：
! What appears on screen (top to bottom):
!   ① 清屏（ESC[2J + ESC[H）+ 复位属性
!   ① Clear the screen (ESC[2J + ESC[H) + reset attributes
!   ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰
!   ② 8 dark foreground colors (color numbers 0-7), background = 7 light gray
!   ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
!   ③ 8 bright foreground colors (color numbers 8-15), background = 0 black
!   ④ 光标定位：同一行先写右半段、再回到左端写左半段 —— 证明是**定位**不是顺序输出
!   ④ Cursor positioning: on the same row write the right half first, then go back to the left end and write the left half -- proving it is **positioning**, not sequential output
!   ⑤ 复位成白字黑底，正常结束
!   ⑤ Reset to white on black, then exit normally
!
! ═══════════════════════════════════════════════════════════════════════
!  ⚠ 这份文件为什么写成"逐字节 putchar"的样子（三条本前端的硬限制）
!  ⚠ Why this file is written as "byte-by-byte putchar" (three hard limitations of this frontend)
!
!  ① **`print *, char(27)` 发不出 ESC**。本前端的 `char()` 是**恒等映射**
!  ① **`print *, char(27)` cannot emit ESC**. In this frontend `char()` is the **identity mapping**
!     （整数转字符 = 无操作），`print` 于是把它当整数打出来（打出 `27` 两个字符）。
!     (integer-to-character is a no-op), so `print` prints it as an integer (the two characters `27`).
!     实测：`call putchar(27)` 真的发出 0x1B ✅ / `print *, char(27)` 打出 "27" ❌
!     Measured: `call putchar(27)` really emits 0x1B OK / `print *, char(27)` prints "27" (wrong)
!     ⇒ 发 ESC 只能走 `call putchar(27)`。
!     => To emit ESC there is no other route than `call putchar(27)`.
!
!  ② **没有可用的子程序**。`contains` 里的内部子程序**传不进实参**、也**看不见
!  ② **No usable subroutines**. A contained subroutine **cannot receive arguments** and **cannot see
!     宿主程序的变量**（实测：`call show(97)` 里 `v` 读到 0；读宿主的全局量同样读到 0；
!     the host program's variables** (measured: inside `call show(97)` `v` reads 0; reading the host's globals also gives 0;
!     外部子程序则直接「未定义的函数 'show'」）。所以本文件**一个子程序都没有** ——
!     an external subroutine just reports "undefined function 'show'"). So this file has **not a single subroutine** --
!     ANSI 序列全部内联展开，这就是它比别的语言那份长一截的原因。
!     every ANSI sequence is expanded inline, which is why it is a good deal longer than the other languages' versions.
!
!  ③ **没有整数转串**。`Str()` 没有、字符串 `//` 拼接没有（`'A' // 'B'` 报
!  ③ **No integer-to-string conversion**. There is no `Str()`, no string `//` concatenation (`'A' // 'B'` reports
!     「意外的 token: Div」）、`character :: s` 变量存不住字符串（`s = 'AB'` 读回来是 0）
!     "unexpected token: Div"), and a `character :: s` variable cannot hold a string (`s = 'AB'` reads back as 0)
!     ⇒ 序列里要发的十进制数字只能自己按位算：`48 + 数字` 就是它的 ASCII 码。
!     => the decimal digits the sequences need must be computed digit by digit: `48 + digit` is their ASCII code.
!
!  本文件里每个 `putchar` 都带行尾注释说明它发的是哪个字符，方便逐字节核对。
!  Every `putchar` in this file carries a trailing comment saying which character it emits, making byte-by-byte checking easy.
! ═══════════════════════════════════════════════════════════════════════

program demo_tty
  implicit none
  integer :: c
  integer :: row, fg, col

  ! ── ① 清屏 + 复位属性 ────────────────────────────────────────────
  ! -- ① Clear the screen + reset attributes --
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(48)          ! 0
  call putchar(109)         ! m      ⇒ ESC[0m  复位全部属性
  ! m      => ESC[0m  reset all attributes
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(74)          ! J      ⇒ ESC[2J  清屏
  ! J      => ESC[2J  clear the screen
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(72)          ! H      ⇒ ESC[H   光标回左上
  ! H      => ESC[H   cursor back to the top left

  ! ── 标题：亮白字(97) + 蓝底(44) ─────────────────────────────────
  ! -- Title: bright white text (97) + blue background (44) --
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(57)          ! 9
  call putchar(55)          ! 7      ⇒ 「97」亮白前景
  ! 7      => "97" bright white foreground
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(52)          ! 4      ⇒ 「44」蓝背景
  ! 4      => "44" blue background
  call putchar(109)         ! m
  print *, '=== Fortran color console demo ===  (ANSI 97;44 = bright white on blue)'

  ! ── ② 暗色 0-7，背景 7 浅灰（SGR 30-37 前景 / 47 背景）──────────
  ! -- ② Dark colors 0-7, background 7 light gray (SGR 30-37 foreground / 47 background) --
  do c = 0, 7
    row = 3 + c
    fg = 30 + c
    ! ESC[<row>;3H —— row 是 3..10，一位或两位
    ! ESC[<row>;3H -- row is 3..10, one or two digits
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    if (row >= 10) then
      call putchar(48 + row / 10)          ! 行号的十位
      ! tens digit of the row number
    end if
    call putchar(48 + row - (row / 10) * 10)   ! 行号的个位
    ! ones digit of the row number
    call putchar(59)        ! ;
    call putchar(51)        ! 3      ⇒ 列固定第 3 列
    ! 3      => the column is always column 3
    call putchar(72)        ! H
    ! ESC[<fg>;47m
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + fg / 10)              ! 色号十位（30+c 恒为两位）
    ! tens digit of the color number (30+c is always two digits)
    call putchar(48 + fg - (fg / 10) * 10)  ! 色号个位
    ! ones digit of the color number
    call putchar(59)        ! ;
    call putchar(52)        ! 4
    call putchar(55)        ! 7      ⇒ 「47」浅灰背景
    ! 7      => "47" light gray background
    call putchar(109)       ! m
    print *, '前景色', c, ' 暗色，背景 = 7 浅灰'
  end do

  ! ── ③ 亮色 8-15，背景 0 黑（SGR 90-97 前景 / 40 背景）──────────
  ! -- ③ Bright colors 8-15, background 0 black (SGR 90-97 foreground / 40 background) --
  do c = 8, 15
    row = 4 + c
    fg = 90 + (c - 8)
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + row / 10)             ! 行号十位
    ! tens digit of the row number
    call putchar(48 + row - (row / 10) * 10) ! 行号个位
    ! ones digit of the row number
    call putchar(59)        ! ;
    call putchar(51)        ! 3
    call putchar(72)        ! H
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + fg / 10)              ! 色号十位
    ! tens digit of the color number
    call putchar(48 + fg - (fg / 10) * 10)  ! 色号个位
    ! ones digit of the color number
    call putchar(59)        ! ;
    call putchar(52)        ! 4
    call putchar(48)        ! 0      ⇒ 「40」黑背景
    ! 0      => "40" black background
    call putchar(109)       ! m
    print *, '前景色', c, ' 亮色，背景 = 0 黑'
  end do

  ! ── ④ 光标定位：先写右半段（第 21 行第 34 列）───────────────────
  ! -- ④ Cursor positioning: write the right half first (row 21, column 34) --
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(49)          ! 1      ⇒ 「21」行
  ! 1      => "21" row
  call putchar(59)          ! ;
  call putchar(51)          ! 3
  call putchar(52)          ! 4      ⇒ 「34」列
  ! 4      => "34" column
  call putchar(72)          ! H      ⇒ ESC[21;34H
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(57)          ! 9
  call putchar(51)          ! 3      ⇒ 「93」亮黄前景
  ! 3      => "93" bright yellow foreground
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(49)          ! 1      ⇒ 「41」红背景
  ! 1      => "41" red background
  call putchar(109)         ! m
  print *, '<- 先写的（第 34 列）'

  ! 再回到第 21 行第 1 列写左半段
  ! Then go back to row 21, column 1 and write the left half
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(49)          ! 1
  call putchar(59)          ! ;
  call putchar(49)          ! 1      ⇒ ESC[21;1H
  call putchar(72)          ! H
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(57)          ! 9
  call putchar(54)          ! 6      ⇒ 「96」亮青前景
  ! 6      => "96" bright cyan foreground
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(48)          ! 0      ⇒ 「40」黑背景
  ! 0      => "40" black background
  call putchar(109)         ! m
  print *, '后写的（第 1 列）-> '

  ! ── ⑤ 收尾：第 23 行第 1 列，复位成白字黑底（37 / 40）──────────
  ! -- ⑤ Wrap-up: row 23, column 1, reset to white on black (37 / 40) --
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(51)          ! 3
  call putchar(59)          ! ;
  call putchar(49)          ! 1      ⇒ ESC[23;1H
  call putchar(72)          ! H
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(51)          ! 3
  call putchar(55)          ! 7      ⇒ 「37」白前景
  ! 7      => "37" white foreground
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(48)          ! 0      ⇒ 「40」黑背景
  ! 0      => "40" black background
  call putchar(109)         ! m
  print *, '=== done（已复位为白字黑底）==='
end program demo_tty
