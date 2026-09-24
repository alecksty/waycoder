! demo_tty.f90 —— Fortran 第二层：**彩色控制台**（tty）
!
! Fortran **没有** conio / crt 那一类可用的控制台绑定：
!   · `Lib/fortran/crt.vml` 里只有自动生成的空壳
!   · `Lib/fortran/console.f90` 的三个函数全是 `r = 0` 的空实现（没有实现体）
! 所以这一层直接输出 **ANSI 转义序列** —— 与 `Examples/c/ansi_colors.c` 同一个做法、
! 同一套效果（手机端「命令行」页由 `UI/Shared/AnsiMarkup.cs` 翻成彩色富文本）。
!
! 跑法（桌面 vmlcli）：
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_tty.f90
!
! 画面（从上到下）：
!   ① 清屏（ESC[2J + ESC[H）+ 复位属性
!   ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰
!   ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
!   ④ 光标定位：同一行先写右半段、再回到左端写左半段 —— 证明是**定位**不是顺序输出
!   ⑤ 复位成白字黑底，正常结束
!
! ═══════════════════════════════════════════════════════════════════════
!  ⚠ 这份文件为什么写成"逐字节 putchar"的样子（三条本前端的硬限制）
!
!  ① **`print *, char(27)` 发不出 ESC**。本前端的 `char()` 是**恒等映射**
!     （整数转字符 = 无操作），`print` 于是把它当整数打出来（打出 `27` 两个字符）。
!     实测：`call putchar(27)` 真的发出 0x1B ✅ / `print *, char(27)` 打出 "27" ❌
!     ⇒ 发 ESC 只能走 `call putchar(27)`。
!
!  ② **没有可用的子程序**。`contains` 里的内部子程序**传不进实参**、也**看不见
!     宿主程序的变量**（实测：`call show(97)` 里 `v` 读到 0；读宿主的全局量同样读到 0；
!     外部子程序则直接「未定义的函数 'show'」）。所以本文件**一个子程序都没有** ——
!     ANSI 序列全部内联展开，这就是它比别的语言那份长一截的原因。
!
!  ③ **没有整数转串**。`Str()` 没有、字符串 `//` 拼接没有（`'A' // 'B'` 报
!     「意外的 token: Div」）、`character :: s` 变量存不住字符串（`s = 'AB'` 读回来是 0）
!     ⇒ 序列里要发的十进制数字只能自己按位算：`48 + 数字` 就是它的 ASCII 码。
!
!  本文件里每个 `putchar` 都带行尾注释说明它发的是哪个字符，方便逐字节核对。
! ═══════════════════════════════════════════════════════════════════════

program demo_tty
  implicit none
  integer :: c
  integer :: row, fg, col

  ! ── ① 清屏 + 复位属性 ────────────────────────────────────────────
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(48)          ! 0
  call putchar(109)         ! m      ⇒ ESC[0m  复位全部属性
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(74)          ! J      ⇒ ESC[2J  清屏
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(72)          ! H      ⇒ ESC[H   光标回左上

  ! ── 标题：亮白字(97) + 蓝底(44) ─────────────────────────────────
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(57)          ! 9
  call putchar(55)          ! 7      ⇒ 「97」亮白前景
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(52)          ! 4      ⇒ 「44」蓝背景
  call putchar(109)         ! m
  print *, '=== Fortran color console demo ===  (ANSI 97;44 = bright white on blue)'

  ! ── ② 暗色 0-7，背景 7 浅灰（SGR 30-37 前景 / 47 背景）──────────
  do c = 0, 7
    row = 3 + c
    fg = 30 + c
    ! ESC[<row>;3H —— row 是 3..10，一位或两位
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    if (row >= 10) then
      call putchar(48 + row / 10)          ! 行号的十位
    end if
    call putchar(48 + row - (row / 10) * 10)   ! 行号的个位
    call putchar(59)        ! ;
    call putchar(51)        ! 3      ⇒ 列固定第 3 列
    call putchar(72)        ! H
    ! ESC[<fg>;47m
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + fg / 10)              ! 色号十位（30+c 恒为两位）
    call putchar(48 + fg - (fg / 10) * 10)  ! 色号个位
    call putchar(59)        ! ;
    call putchar(52)        ! 4
    call putchar(55)        ! 7      ⇒ 「47」浅灰背景
    call putchar(109)       ! m
    print *, '前景色', c, ' 暗色，背景 = 7 浅灰'
  end do

  ! ── ③ 亮色 8-15，背景 0 黑（SGR 90-97 前景 / 40 背景）──────────
  do c = 8, 15
    row = 4 + c
    fg = 90 + (c - 8)
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + row / 10)             ! 行号十位
    call putchar(48 + row - (row / 10) * 10) ! 行号个位
    call putchar(59)        ! ;
    call putchar(51)        ! 3
    call putchar(72)        ! H
    call putchar(27)        ! ESC
    call putchar(91)        ! [
    call putchar(48 + fg / 10)              ! 色号十位
    call putchar(48 + fg - (fg / 10) * 10)  ! 色号个位
    call putchar(59)        ! ;
    call putchar(52)        ! 4
    call putchar(48)        ! 0      ⇒ 「40」黑背景
    call putchar(109)       ! m
    print *, '前景色', c, ' 亮色，背景 = 0 黑'
  end do

  ! ── ④ 光标定位：先写右半段（第 21 行第 34 列）───────────────────
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(50)          ! 2
  call putchar(49)          ! 1      ⇒ 「21」行
  call putchar(59)          ! ;
  call putchar(51)          ! 3
  call putchar(52)          ! 4      ⇒ 「34」列
  call putchar(72)          ! H      ⇒ ESC[21;34H
  call putchar(27)          ! ESC
  call putchar(91)          ! [
  call putchar(57)          ! 9
  call putchar(51)          ! 3      ⇒ 「93」亮黄前景
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(49)          ! 1      ⇒ 「41」红背景
  call putchar(109)         ! m
  print *, '<- 先写的（第 34 列）'

  ! 再回到第 21 行第 1 列写左半段
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
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(48)          ! 0      ⇒ 「40」黑背景
  call putchar(109)         ! m
  print *, '后写的（第 1 列）-> '

  ! ── ⑤ 收尾：第 23 行第 1 列，复位成白字黑底（37 / 40）──────────
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
  call putchar(59)          ! ;
  call putchar(52)          ! 4
  call putchar(48)          ! 0      ⇒ 「40」黑背景
  call putchar(109)         ! m
  print *, '=== done（已复位为白字黑底）==='
end program demo_tty
