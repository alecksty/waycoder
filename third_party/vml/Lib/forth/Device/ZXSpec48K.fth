\ ZX-Spectrum-48K设备定义 - Forth文件
\ 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
\ CPU架构: Z80A
\ 位宽: 8位
\ 时钟频率: 3500000 Hz

\ =========================================
\ ZX-Spectrum-48K设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ZX-Spectrum-48K" ;
: MANUFACTURER  S" Sinclair Research" ;
: FAMILY        S" ZX Spectrum" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Z80A" ;
8 CONSTANT BITS
3500000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT F  \ Flags Register
0 CONSTANT F-C  \ Carry
1 CONSTANT F-N  \ Add/Subtract
2 CONSTANT F-PV  \ Parity/Overflow
4 CONSTANT F-H  \ Half Carry
6 CONSTANT F-Z  \ Zero
7 CONSTANT F-S  \ Sign
0x02 CONSTANT B  \ B Register
0x03 CONSTANT C  \ C Register
0x04 CONSTANT D  \ D Register
0x05 CONSTANT E  \ E Register
0x06 CONSTANT H  \ H Register
0x07 CONSTANT L  \ L Register
0x08 CONSTANT AF  \ Alternate AF
0x0A CONSTANT BC  \ Alternate BC
0x0C CONSTANT DE  \ Alternate DE
0x0E CONSTANT HL  \ Alternate HL
0x10 CONSTANT I  \ Interrupt Vector Register
0x11 CONSTANT R  \ Refresh Counter
0x12 CONSTANT IX  \ Index X
0x14 CONSTANT IY  \ Index Y
0x16 CONSTANT SP  \ Stack Pointer
0x18 CONSTANT PC  \ Program Counter

\ 内存段定义
0x0000 CONSTANT ROM-START
0x3FFF CONSTANT ROM-END
16384 CONSTANT ROM-SIZE  \ 48KB ZX Spectrum ROM (BASIC + monitor)
0x4000 CONSTANT VIDEO_RAM-START
0x57FF CONSTANT VIDEO_RAM-END
6144 CONSTANT VIDEO_RAM-SIZE  \ Display file (256x192 bitmap)
0x5800 CONSTANT ATTR_RAM-START
0x5AFF CONSTANT ATTR_RAM-END
768 CONSTANT ATTR_RAM-SIZE  \ Attribute file (32x24 color cells)
0x5B00 CONSTANT USER_RAM-START
0xFFFF CONSTANT USER_RAM-END
40960 CONSTANT USER_RAM-SIZE  \ User RAM (40KB)

\ 外设定义
\ Uncommitted Logic Array - Sinclair custom IC
0xFE CONSTANT ULA-BASE
0xFE CONSTANT ULA-BORDER
0xFE CONSTANT ULA-KBD_ROW0
0xFE CONSTANT ULA-KBD_ROW1
0xFE CONSTANT ULA-KBD_ROW2
0xFE CONSTANT ULA-KBD_ROW3
0xFE CONSTANT ULA-KBD_ROW4
0xFE CONSTANT ULA-KBD_ROW5
0xFE CONSTANT ULA-KBD_ROW6
0xFE CONSTANT ULA-KBD_ROW7
0xFE CONSTANT ULA-KBD_ROW8
\ Keyboard Matrix (40 keys, 8 rows x 5 cols)
0xFE CONSTANT KEYBOARD-BASE
0xFE CONSTANT KEYBOARD-KBD_IN
\ Internal Beeper
0xFE CONSTANT BEEPER-BASE
0xFE CONSTANT BEEPER-BEEP
\ Tape Interface
0xFE CONSTANT TAPE-BASE
0xFE CONSTANT TAPE-EAR_IN
0xFE CONSTANT TAPE-MIC_OUT
\ Kempston Joystick Interface
0xF7FE CONSTANT JOYSTICK-BASE
0xF7FE CONSTANT JOYSTICK-KEMPSTON

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-on / Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt (BREAK key)
2 CONSTANT INT-INT  \ Maskable Interrupt (ULA vertical blank, 50Hz)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-CLK  \ Z80 Clock (3.5MHz)
4 CONSTANT PIN-M1  \ Machine Cycle 1
5 CONSTANT PIN-MREQ  \ Memory Request
6 CONSTANT PIN-IORQ  \ I/O Request
7 CONSTANT PIN-RD  \ Read
8 CONSTANT PIN-WR  \ Write
9 CONSTANT PIN-HALT  \ Halt State
10 CONSTANT PIN-BUSAK  \ Bus Acknowledge
11 CONSTANT PIN-WAIT  \ Wait State (ULA inserts)
12 CONSTANT PIN-INT  \ Interrupt Request
13 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
14 CONSTANT PIN-RESET  \ Reset
15 CONSTANT PIN-A0_A15  \ Address Bus (16-bit)
16 CONSTANT PIN-D0_D7  \ Data Bus (8-bit)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: F@ ( -- n ) F C@ ;
: F! ( n -- ) F C! ;
: F-C@ ( -- flag ) F@ 0 BIT@ ;
: F-C! ( flag -- ) F@ 0 BIT! F! ;
: F-C-SET ( -- ) TRUE F-C! ;
: F-C-CLR ( -- ) FALSE F-C! ;
: F-N@ ( -- flag ) F@ 1 BIT@ ;
: F-N! ( flag -- ) F@ 1 BIT! F! ;
: F-N-SET ( -- ) TRUE F-N! ;
: F-N-CLR ( -- ) FALSE F-N! ;
: F-PV@ ( -- flag ) F@ 2 BIT@ ;
: F-PV! ( flag -- ) F@ 2 BIT! F! ;
: F-PV-SET ( -- ) TRUE F-PV! ;
: F-PV-CLR ( -- ) FALSE F-PV! ;
: F-H@ ( -- flag ) F@ 4 BIT@ ;
: F-H! ( flag -- ) F@ 4 BIT! F! ;
: F-H-SET ( -- ) TRUE F-H! ;
: F-H-CLR ( -- ) FALSE F-H! ;
: F-Z@ ( -- flag ) F@ 6 BIT@ ;
: F-Z! ( flag -- ) F@ 6 BIT! F! ;
: F-Z-SET ( -- ) TRUE F-Z! ;
: F-Z-CLR ( -- ) FALSE F-Z! ;
: F-S@ ( -- flag ) F@ 7 BIT@ ;
: F-S! ( flag -- ) F@ 7 BIT! F! ;
: F-S-SET ( -- ) TRUE F-S! ;
: F-S-CLR ( -- ) FALSE F-S! ;

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

: AF@ ( -- n ) AF @ ;
: AF! ( n -- ) AF ! ;

: BC@ ( -- n ) BC @ ;
: BC! ( n -- ) BC ! ;

: DE@ ( -- n ) DE @ ;
: DE! ( n -- ) DE ! ;

: HL@ ( -- n ) HL @ ;
: HL! ( n -- ) HL ! ;

: I@ ( -- n ) I C@ ;
: I! ( n -- ) I C! ;

: R@ ( -- n ) R C@ ;
: R! ( n -- ) R C! ;

: IX@ ( -- n ) IX @ ;
: IX! ( n -- ) IX ! ;

: IY@ ( -- n ) IY @ ;
: IY! ( n -- ) IY ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

\ 外设访问
\ ULA外设
: ULA-BORDER@ ( -- n ) ULA-BORDER C@ ;
: ULA-BORDER! ( n -- ) ULA-BORDER C! ;
: ULA-KBD_ROW0@ ( -- n ) ULA-KBD_ROW0 C@ ;
: ULA-KBD_ROW0! ( n -- ) ULA-KBD_ROW0 C! ;
: ULA-KBD_ROW1@ ( -- n ) ULA-KBD_ROW1 C@ ;
: ULA-KBD_ROW1! ( n -- ) ULA-KBD_ROW1 C! ;
: ULA-KBD_ROW2@ ( -- n ) ULA-KBD_ROW2 C@ ;
: ULA-KBD_ROW2! ( n -- ) ULA-KBD_ROW2 C! ;
: ULA-KBD_ROW3@ ( -- n ) ULA-KBD_ROW3 C@ ;
: ULA-KBD_ROW3! ( n -- ) ULA-KBD_ROW3 C! ;
: ULA-KBD_ROW4@ ( -- n ) ULA-KBD_ROW4 C@ ;
: ULA-KBD_ROW4! ( n -- ) ULA-KBD_ROW4 C! ;
: ULA-KBD_ROW5@ ( -- n ) ULA-KBD_ROW5 C@ ;
: ULA-KBD_ROW5! ( n -- ) ULA-KBD_ROW5 C! ;
: ULA-KBD_ROW6@ ( -- n ) ULA-KBD_ROW6 C@ ;
: ULA-KBD_ROW6! ( n -- ) ULA-KBD_ROW6 C! ;
: ULA-KBD_ROW7@ ( -- n ) ULA-KBD_ROW7 C@ ;
: ULA-KBD_ROW7! ( n -- ) ULA-KBD_ROW7 C! ;
: ULA-KBD_ROW8@ ( -- n ) ULA-KBD_ROW8 C@ ;
: ULA-KBD_ROW8! ( n -- ) ULA-KBD_ROW8 C! ;

\ KEYBOARD外设
: KEYBOARD-KBD_IN@ ( -- n ) KEYBOARD-KBD_IN C@ ;
: KEYBOARD-KBD_IN! ( n -- ) KEYBOARD-KBD_IN C! ;

\ BEEPER外设
: BEEPER-BEEP@ ( -- n ) BEEPER-BEEP C@ ;
: BEEPER-BEEP! ( n -- ) BEEPER-BEEP C! ;

\ TAPE外设
: TAPE-EAR_IN@ ( -- n ) TAPE-EAR_IN C@ ;
: TAPE-EAR_IN! ( n -- ) TAPE-EAR_IN C! ;
: TAPE-MIC_OUT@ ( -- n ) TAPE-MIC_OUT C@ ;
: TAPE-MIC_OUT! ( n -- ) TAPE-MIC_OUT C! ;

\ JOYSTICK外设
: JOYSTICK-KEMPSTON@ ( -- n ) JOYSTICK-KEMPSTON C@ ;
: JOYSTICK-KEMPSTON! ( n -- ) JOYSTICK-KEMPSTON C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ZX_SPECTRUM_48K-INIT ( -- )
  \ 初始化ZX-Spectrum-48K设备
  ." 初始化ZX-Spectrum-48K..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 F!  \ Flags Register
  0 B!  \ B Register
  0 C!  \ C Register
  0 D!  \ D Register
  0 E!  \ E Register
  0 H!  \ H Register
  0 L!  \ L Register
  0 AF!  \ Alternate AF
  0 BC!  \ Alternate BC
  0 DE!  \ Alternate DE
  0 HL!  \ Alternate HL
  0 I!  \ Interrupt Vector Register
  0 R!  \ Refresh Counter
  0 IX!  \ Index X
  0 IY!  \ Index Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化ULA
  0 ULA-BORDER!  \ BORDER寄存器
  0 ULA-KBD_ROW0!  \ KBD_ROW0寄存器
  0 ULA-KBD_ROW1!  \ KBD_ROW1寄存器
  0 ULA-KBD_ROW2!  \ KBD_ROW2寄存器
  0 ULA-KBD_ROW3!  \ KBD_ROW3寄存器
  0 ULA-KBD_ROW4!  \ KBD_ROW4寄存器
  0 ULA-KBD_ROW5!  \ KBD_ROW5寄存器
  0 ULA-KBD_ROW6!  \ KBD_ROW6寄存器
  0 ULA-KBD_ROW7!  \ KBD_ROW7寄存器
  0 ULA-KBD_ROW8!  \ KBD_ROW8寄存器
  \ 初始化KEYBOARD
  0 KEYBOARD-KBD_IN!  \ KBD_IN寄存器
  \ 初始化BEEPER
  0 BEEPER-BEEP!  \ BEEP寄存器
  \ 初始化TAPE
  0 TAPE-EAR_IN!  \ EAR_IN寄存器
  0 TAPE-MIC_OUT!  \ MIC_OUT寄存器
  \ 初始化JOYSTICK
  0 JOYSTICK-KEMPSTON!  \ KEMPSTON寄存器

  ." ZX-Spectrum-48K初始化完成" CR
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
  AF@ AF .R 8 .R SPACE ."  AF': " AF@ .
  BC@ BC .R 8 .R SPACE ."  BC': " BC@ .
  DE@ DE .R 8 .R SPACE ."  DE': " DE@ .
  HL@ HL .R 8 .R SPACE ."  HL': " HL@ .
  I@ I .R 8 .R SPACE ."  I: " I@ .
  R@ R .R 8 .R SPACE ."  R: " R@ .
  IX@ IX .R 8 .R SPACE ."  IX: " IX@ .
  IY@ IY .R 8 .R SPACE ."  IY: " IY@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Power-on / Reset
: INT-RESET-HANDLER ( -- )
  ." RESET中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ Non-Maskable Interrupt (BREAK key)
: INT-NMI-HANDLER ( -- )
  ." NMI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NMI-ENABLE ( -- )
  INT-NMI INT-ENABLE
;

: INT-NMI-DISABLE ( -- )
  INT-NMI INT-DISABLE
;

\ Maskable Interrupt (ULA vertical blank, 50Hz)
: INT-INT-HANDLER ( -- )
  ." INT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT-ENABLE ( -- )
  INT-INT INT-ENABLE
;

: INT-INT-DISABLE ( -- )
  INT-INT INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ZX_SPECTRUM_48K-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
