\ ZX-Spectrum设备定义 - Forth文件
\ 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
\ CPU架构: Zilog Z80
\ 位宽: 8位
\ 时钟频率: 3500000 Hz

\ =========================================
\ ZX-Spectrum设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ZX-Spectrum" ;
: MANUFACTURER  S" Sinclair Research" ;
: FAMILY        S" ZX Spectrum" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Zilog Z80" ;
8 CONSTANT BITS
3500000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT A  \ Accumulator
0 CONSTANT F  \ Flags
0 CONSTANT B  \ B
0 CONSTANT C  \ C
0 CONSTANT D  \ D
0 CONSTANT E  \ E
0 CONSTANT H  \ H
0 CONSTANT L  \ L
0 CONSTANT IX  \ Index Register X
0 CONSTANT IY  \ Index Register Y
0 CONSTANT SP  \ Stack Pointer
0 CONSTANT PC  \ Program Counter
0 CONSTANT I  \ Interrupt Vector
0 CONSTANT R  \ Memory Refresh
0 CONSTANT AF  \ Alternate AF
0 CONSTANT BC  \ Alternate BC
0 CONSTANT DE  \ Alternate DE
0 CONSTANT HL  \ Alternate HL

\ 外设定义
\ Uncommitted Logic Array (video and I/O)
 CONSTANT ULA-BASE
0xFE CONSTANT ULA-ULA_PORT_FE
0xFE CONSTANT ULA-ULA_BORDER
0xFE CONSTANT ULA-ULA_BEEPER
0xFE CONSTANT ULA-ULA_MIC
\ General Instruments AY-3-8912 sound chip
 CONSTANT AY_3_8912-BASE
0xFFFD CONSTANT AY_3_8912-AY_REG_SEL
0xBFFD CONSTANT AY_3_8912-AY_DATA
0xFFFD CONSTANT AY_3_8912-AY_READ
\ 40-key rubber keyboard
 CONSTANT KEYBOARD-BASE
0xFEFE CONSTANT KEYBOARD-KEY_ROW0
0xFDFE CONSTANT KEYBOARD-KEY_ROW1
0xFBFE CONSTANT KEYBOARD-KEY_ROW2
0xF7FE CONSTANT KEYBOARD-KEY_ROW3
0xEFFE CONSTANT KEYBOARD-KEY_ROW4
0xDFFE CONSTANT KEYBOARD-KEY_ROW5
0xBFFE CONSTANT KEYBOARD-KEY_ROW6
0x7FFE CONSTANT KEYBOARD-KEY_ROW7
\ Kempston joystick interface
 CONSTANT KEMPSTON-BASE
0x1F CONSTANT KEMPSTON-KEMPSTON_JOY
\ ZX Interface 1 (RS-232 and Microdrive)
 CONSTANT INTERFACE1-BASE
0x1FFD CONSTANT INTERFACE1-IF1_STATUS
0x3FFD CONSTANT INTERFACE1-IF1_DATA
\ ZX Interface 2 (joystick and ROM cartridge)
 CONSTANT INTERFACE2-BASE
0x1F CONSTANT INTERFACE2-IF2_JOY1
0x37 CONSTANT INTERFACE2-IF2_JOY2

\ 中断向量定义
56 CONSTANT INT-IM1  \ Interrupt Mode 1
0 CONSTANT INT-RST_00  \ Restart 00h
8 CONSTANT INT-RST_08  \ Restart 08h
16 CONSTANT INT-RST_10  \ Restart 10h
24 CONSTANT INT-RST_18  \ Restart 18h
32 CONSTANT INT-RST_20  \ Restart 20h
40 CONSTANT INT-RST_28  \ Restart 28h
48 CONSTANT INT-RST_30  \ Restart 30h
56 CONSTANT INT-RST_38  \ Restart 38h

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: F@ ( -- n ) F C@ ;
: F! ( n -- ) F C! ;

: B@ ( -- n ) B C@ ;
: B! ( n -- ) B C! ;

: C@ ( -- n ) C C@ ;
: C! ( n -- ) C C! ;

: D@ ( -- n ) D C@ ;
: D! ( n -- ) D C! ;

: E@ ( -- n ) E C@ ;
: E! ( n -- ) E C! ;

: H@ ( -- n ) H C@ ;
: H! ( n -- ) H C! ;

: L@ ( -- n ) L C@ ;
: L! ( n -- ) L C! ;

: IX@ ( -- n ) IX @ ;
: IX! ( n -- ) IX ! ;

: IY@ ( -- n ) IY @ ;
: IY! ( n -- ) IY ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

: I@ ( -- n ) I C@ ;
: I! ( n -- ) I C! ;

: R@ ( -- n ) R C@ ;
: R! ( n -- ) R C! ;

: AF@ ( -- n ) AF @ ;
: AF! ( n -- ) AF ! ;

: BC@ ( -- n ) BC @ ;
: BC! ( n -- ) BC ! ;

: DE@ ( -- n ) DE @ ;
: DE! ( n -- ) DE ! ;

: HL@ ( -- n ) HL @ ;
: HL! ( n -- ) HL ! ;

\ 外设访问
\ ULA外设
: ULA-ULA_PORT_FE@ ( -- n ) ULA-ULA_PORT_FE C@ ;
: ULA-ULA_PORT_FE! ( n -- ) ULA-ULA_PORT_FE C! ;
: ULA-ULA_BORDER@ ( -- n ) ULA-ULA_BORDER C@ ;
: ULA-ULA_BORDER! ( n -- ) ULA-ULA_BORDER C! ;
: ULA-ULA_BEEPER@ ( -- n ) ULA-ULA_BEEPER C@ ;
: ULA-ULA_BEEPER! ( n -- ) ULA-ULA_BEEPER C! ;
: ULA-ULA_MIC@ ( -- n ) ULA-ULA_MIC C@ ;
: ULA-ULA_MIC! ( n -- ) ULA-ULA_MIC C! ;

\ AY-3-8912外设
: AY_3_8912-AY_REG_SEL@ ( -- n ) AY_3_8912-AY_REG_SEL C@ ;
: AY_3_8912-AY_REG_SEL! ( n -- ) AY_3_8912-AY_REG_SEL C! ;
: AY_3_8912-AY_DATA@ ( -- n ) AY_3_8912-AY_DATA C@ ;
: AY_3_8912-AY_DATA! ( n -- ) AY_3_8912-AY_DATA C! ;
: AY_3_8912-AY_READ@ ( -- n ) AY_3_8912-AY_READ C@ ;
: AY_3_8912-AY_READ! ( n -- ) AY_3_8912-AY_READ C! ;

\ Keyboard外设
: KEYBOARD-KEY_ROW0@ ( -- n ) KEYBOARD-KEY_ROW0 C@ ;
: KEYBOARD-KEY_ROW0! ( n -- ) KEYBOARD-KEY_ROW0 C! ;
: KEYBOARD-KEY_ROW1@ ( -- n ) KEYBOARD-KEY_ROW1 C@ ;
: KEYBOARD-KEY_ROW1! ( n -- ) KEYBOARD-KEY_ROW1 C! ;
: KEYBOARD-KEY_ROW2@ ( -- n ) KEYBOARD-KEY_ROW2 C@ ;
: KEYBOARD-KEY_ROW2! ( n -- ) KEYBOARD-KEY_ROW2 C! ;
: KEYBOARD-KEY_ROW3@ ( -- n ) KEYBOARD-KEY_ROW3 C@ ;
: KEYBOARD-KEY_ROW3! ( n -- ) KEYBOARD-KEY_ROW3 C! ;
: KEYBOARD-KEY_ROW4@ ( -- n ) KEYBOARD-KEY_ROW4 C@ ;
: KEYBOARD-KEY_ROW4! ( n -- ) KEYBOARD-KEY_ROW4 C! ;
: KEYBOARD-KEY_ROW5@ ( -- n ) KEYBOARD-KEY_ROW5 C@ ;
: KEYBOARD-KEY_ROW5! ( n -- ) KEYBOARD-KEY_ROW5 C! ;
: KEYBOARD-KEY_ROW6@ ( -- n ) KEYBOARD-KEY_ROW6 C@ ;
: KEYBOARD-KEY_ROW6! ( n -- ) KEYBOARD-KEY_ROW6 C! ;
: KEYBOARD-KEY_ROW7@ ( -- n ) KEYBOARD-KEY_ROW7 C@ ;
: KEYBOARD-KEY_ROW7! ( n -- ) KEYBOARD-KEY_ROW7 C! ;

\ Kempston外设
: KEMPSTON-KEMPSTON_JOY@ ( -- n ) KEMPSTON-KEMPSTON_JOY C@ ;
: KEMPSTON-KEMPSTON_JOY! ( n -- ) KEMPSTON-KEMPSTON_JOY C! ;

\ Interface1外设
: INTERFACE1-IF1_STATUS@ ( -- n ) INTERFACE1-IF1_STATUS C@ ;
: INTERFACE1-IF1_STATUS! ( n -- ) INTERFACE1-IF1_STATUS C! ;
: INTERFACE1-IF1_DATA@ ( -- n ) INTERFACE1-IF1_DATA C@ ;
: INTERFACE1-IF1_DATA! ( n -- ) INTERFACE1-IF1_DATA C! ;

\ Interface2外设
: INTERFACE2-IF2_JOY1@ ( -- n ) INTERFACE2-IF2_JOY1 C@ ;
: INTERFACE2-IF2_JOY1! ( n -- ) INTERFACE2-IF2_JOY1 C! ;
: INTERFACE2-IF2_JOY2@ ( -- n ) INTERFACE2-IF2_JOY2 C@ ;
: INTERFACE2-IF2_JOY2! ( n -- ) INTERFACE2-IF2_JOY2 C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ZX_SPECTRUM-INIT ( -- )
  \ 初始化ZX-Spectrum设备
  ." 初始化ZX-Spectrum..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 F!  \ Flags
  0 B!  \ B
  0 C!  \ C
  0 D!  \ D
  0 E!  \ E
  0 H!  \ H
  0 L!  \ L
  0 IX!  \ Index Register X
  0 IY!  \ Index Register Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 I!  \ Interrupt Vector
  0 R!  \ Memory Refresh
  0 AF!  \ Alternate AF
  0 BC!  \ Alternate BC
  0 DE!  \ Alternate DE
  0 HL!  \ Alternate HL

  \ 初始化外设
  \ 初始化ULA
  0 ULA-ULA_PORT_FE!  \ ULA_PORT_FE寄存器
  0 ULA-ULA_BORDER!  \ ULA_BORDER寄存器
  0 ULA-ULA_BEEPER!  \ ULA_BEEPER寄存器
  0 ULA-ULA_MIC!  \ ULA_MIC寄存器
  \ 初始化AY-3-8912
  0 AY_3_8912-AY_REG_SEL!  \ AY_REG_SEL寄存器
  0 AY_3_8912-AY_DATA!  \ AY_DATA寄存器
  0 AY_3_8912-AY_READ!  \ AY_READ寄存器
  \ 初始化Keyboard
  0 KEYBOARD-KEY_ROW0!  \ KEY_ROW0寄存器
  0 KEYBOARD-KEY_ROW1!  \ KEY_ROW1寄存器
  0 KEYBOARD-KEY_ROW2!  \ KEY_ROW2寄存器
  0 KEYBOARD-KEY_ROW3!  \ KEY_ROW3寄存器
  0 KEYBOARD-KEY_ROW4!  \ KEY_ROW4寄存器
  0 KEYBOARD-KEY_ROW5!  \ KEY_ROW5寄存器
  0 KEYBOARD-KEY_ROW6!  \ KEY_ROW6寄存器
  0 KEYBOARD-KEY_ROW7!  \ KEY_ROW7寄存器
  \ 初始化Kempston
  0 KEMPSTON-KEMPSTON_JOY!  \ KEMPSTON_JOY寄存器
  \ 初始化Interface1
  0 INTERFACE1-IF1_STATUS!  \ IF1_STATUS寄存器
  0 INTERFACE1-IF1_DATA!  \ IF1_DATA寄存器
  \ 初始化Interface2
  0 INTERFACE2-IF2_JOY1!  \ IF2_JOY1寄存器
  0 INTERFACE2-IF2_JOY2!  \ IF2_JOY2寄存器

  ." ZX-Spectrum初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  A@ A .R 8 .R SPACE ."  A: " A@ .
  F@ F .R 8 .R SPACE ."  F: " F@ .
  B@ B .R 8 .R SPACE ."  B: " B@ .
  C@ C .R 8 .R SPACE ."  C: " C@ .
  D@ D .R 8 .R SPACE ."  D: " D@ .
  E@ E .R 8 .R SPACE ."  E: " E@ .
  H@ H .R 8 .R SPACE ."  H: " H@ .
  L@ L .R 8 .R SPACE ."  L: " L@ .
  IX@ IX .R 8 .R SPACE ."  IX: " IX@ .
  IY@ IY .R 8 .R SPACE ."  IY: " IY@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  I@ I .R 8 .R SPACE ."  I: " I@ .
  R@ R .R 8 .R SPACE ."  R: " R@ .
  AF@ AF .R 8 .R SPACE ."  AF': " AF@ .
  BC@ BC .R 8 .R SPACE ."  BC': " BC@ .
  DE@ DE .R 8 .R SPACE ."  DE': " DE@ .
  HL@ HL .R 8 .R SPACE ."  HL': " HL@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Interrupt Mode 1
: INT-IM1-HANDLER ( -- )
  ." IM1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IM1-ENABLE ( -- )
  INT-IM1 INT-ENABLE
;

: INT-IM1-DISABLE ( -- )
  INT-IM1 INT-DISABLE
;

\ Restart 00h
: INT-RST_00-HANDLER ( -- )
  ." RST_00中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_00-ENABLE ( -- )
  INT-RST_00 INT-ENABLE
;

: INT-RST_00-DISABLE ( -- )
  INT-RST_00 INT-DISABLE
;

\ Restart 08h
: INT-RST_08-HANDLER ( -- )
  ." RST_08中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_08-ENABLE ( -- )
  INT-RST_08 INT-ENABLE
;

: INT-RST_08-DISABLE ( -- )
  INT-RST_08 INT-DISABLE
;

\ Restart 10h
: INT-RST_10-HANDLER ( -- )
  ." RST_10中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_10-ENABLE ( -- )
  INT-RST_10 INT-ENABLE
;

: INT-RST_10-DISABLE ( -- )
  INT-RST_10 INT-DISABLE
;

\ Restart 18h
: INT-RST_18-HANDLER ( -- )
  ." RST_18中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_18-ENABLE ( -- )
  INT-RST_18 INT-ENABLE
;

: INT-RST_18-DISABLE ( -- )
  INT-RST_18 INT-DISABLE
;

\ Restart 20h
: INT-RST_20-HANDLER ( -- )
  ." RST_20中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_20-ENABLE ( -- )
  INT-RST_20 INT-ENABLE
;

: INT-RST_20-DISABLE ( -- )
  INT-RST_20 INT-DISABLE
;

\ Restart 28h
: INT-RST_28-HANDLER ( -- )
  ." RST_28中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_28-ENABLE ( -- )
  INT-RST_28 INT-ENABLE
;

: INT-RST_28-DISABLE ( -- )
  INT-RST_28 INT-DISABLE
;

\ Restart 30h
: INT-RST_30-HANDLER ( -- )
  ." RST_30中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_30-ENABLE ( -- )
  INT-RST_30 INT-ENABLE
;

: INT-RST_30-DISABLE ( -- )
  INT-RST_30 INT-DISABLE
;

\ Restart 38h
: INT-RST_38-HANDLER ( -- )
  ." RST_38中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_38-ENABLE ( -- )
  INT-RST_38 INT-ENABLE
;

: INT-RST_38-DISABLE ( -- )
  INT-RST_38 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ZX_SPECTRUM-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
