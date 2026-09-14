\ MSX1设备定义 - Forth文件
\ 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
\ CPU架构: Z80A
\ 位宽: 8位
\ 时钟频率: 3579545 Hz

\ =========================================
\ MSX1设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MSX1" ;
: MANUFACTURER  S" Various (ASCII/Awanaga/MSX Association)" ;
: FAMILY        S" MSX" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Z80A" ;
8 CONSTANT BITS
3579545 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT F  \ Flags
0 CONSTANT F-C  \ Carry
1 CONSTANT F-N  \ Subtract
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
0x10 CONSTANT I  \ Interrupt Vector
0x11 CONSTANT R  \ Refresh
0x12 CONSTANT IX  \ Index X
0x14 CONSTANT IY  \ Index Y (usually = 0xF38F)
0x16 CONSTANT SP  \ Stack Pointer
0x18 CONSTANT PC  \ Program Counter

\ 内存段定义
0x0000 CONSTANT SLOT0_ROM-START
0x7FFF CONSTANT SLOT0_ROM-END
32768 CONSTANT SLOT0_ROM-SIZE  \ Cartridge/SUB-ROM / Main-ROM
0x0000 CONSTANT SYSROM-START
0x3FFF CONSTANT SYSROM-END
16384 CONSTANT SYSROM-SIZE  \ MSX-BIOS ROM
0x4000 CONSTANT EXTROM-START
0x7FFF CONSTANT EXTROM-END
16384 CONSTANT EXTROM-SIZE  \ Extension ROM (cartridge)
0x4000 CONSTANT MAIN_RAM-START
0xC000 CONSTANT MAIN_RAM-END
32768 CONSTANT MAIN_RAM-SIZE  \ Main RAM (32KB working area)
0xC000 CONSTANT WORK_RAM-START
0xFFFF CONSTANT WORK_RAM-END
16384 CONSTANT WORK_RAM-SIZE  \ Work RAM (16KB)
0xF000 CONSTANT SYSVAR-START
0xFCA0 CONSTANT SYSVAR-END
3232 CONSTANT SYSVAR-SIZE  \ System variables area
0x8000 CONSTANT SLOTS-START
0xFFFF CONSTANT SLOTS-END
32768 CONSTANT SLOTS-SIZE  \ Slot-mapped memory

\ 外设定义
\ TMS9918A Video Display Processor
0x98 CONSTANT VDP-BASE
0x99 CONSTANT VDP-VDP_REG0
0x99 CONSTANT VDP-VDP_REG1
0x99 CONSTANT VDP-VDP_REG2
0x99 CONSTANT VDP-VDP_REG3
0x99 CONSTANT VDP-VDP_REG4
0x99 CONSTANT VDP-VDP_REG5
0x99 CONSTANT VDP-VDP_REG6
0x99 CONSTANT VDP-VDP_REG7
0x99 CONSTANT VDP-VDP_STATUS
0x98 CONSTANT VDP-VDP_DATA
0x98 CONSTANT VDP-VDP_POT
\ AY-3-8910 Programmable Sound Generator
0xA0 CONSTANT PSG-BASE
0xA1 CONSTANT PSG-PSG_REG
0xA3 CONSTANT PSG-PSG_DATA
0xA0 CONSTANT PSG-FREQ_A_LO
0xA1 CONSTANT PSG-FREQ_A_HI
0xA2 CONSTANT PSG-FREQ_B_LO
0xA3 CONSTANT PSG-FREQ_B_HI
0xA4 CONSTANT PSG-FREQ_C_LO
0xA5 CONSTANT PSG-FREQ_C_HI
0xA6 CONSTANT PSG-NOISE_FREQ
0xA7 CONSTANT PSG-ENABLE
0xA8 CONSTANT PSG-VOL_A
0xA9 CONSTANT PSG-VOL_B
0xAA CONSTANT PSG-VOL_C
0xAB CONSTANT PSG-ENV_FREQ_LO
0xAC CONSTANT PSG-ENV_FREQ_HI
0xAD CONSTANT PSG-ENV_SHAPE
0xAE CONSTANT PSG-PORT_A
0xAF CONSTANT PSG-PORT_B
\ PPI 8255 Programmable Peripheral Interface
0xA8 CONSTANT PPI-BASE
0xA8 CONSTANT PPI-PPI_PA
0xA9 CONSTANT PPI-PPI_PB
0xAA CONSTANT PPI-PPI_PC
0xAB CONSTANT PPI-PPI_CTRL
\ MSX Slot Expansion System
0x0000 CONSTANT SLOTEXP-BASE
0xFCC0 CONSTANT SLOTEXP-SLOT0
0xFCC1 CONSTANT SLOTEXP-SLOT1
0xFCC2 CONSTANT SLOTEXP-SLOT2
0xFCC3 CONSTANT SLOTEXP-SLOT3
0xFCC4 CONSTANT SLOTEXP-EXPTBL0
0xFCC5 CONSTANT SLOTEXP-EXPTBL1
0xFCC6 CONSTANT SLOTEXP-EXPTBL2
0xFCC7 CONSTANT SLOTEXP-EXPTBL3

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-on / Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt
2 CONSTANT INT-INT  \ VDP Vertical Interrupt (frame)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-CLK  \ Z80 Clock (3.58MHz)
4 CONSTANT PIN-A0_A15  \ Address Bus
5 CONSTANT PIN-D0_D7  \ Data Bus
6 CONSTANT PIN-MREQ  \ Memory Request
7 CONSTANT PIN-IORQ  \ I/O Request
8 CONSTANT PIN-RD  \ Read
9 CONSTANT PIN-WR  \ Write
10 CONSTANT PIN-INT  \ Interrupt Request
11 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
12 CONSTANT PIN-RESET  \ Reset
13 CONSTANT PIN-SLTSL  \ Slot select (for memory mapping)
14 CONSTANT PIN-WAIT  \ Wait (for slow I/O)

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
\ VDP外设
: VDP-VDP_REG0@ ( -- n ) VDP-VDP_REG0 C@ ;
: VDP-VDP_REG0! ( n -- ) VDP-VDP_REG0 C! ;
: VDP-VDP_REG1@ ( -- n ) VDP-VDP_REG1 C@ ;
: VDP-VDP_REG1! ( n -- ) VDP-VDP_REG1 C! ;
: VDP-VDP_REG2@ ( -- n ) VDP-VDP_REG2 C@ ;
: VDP-VDP_REG2! ( n -- ) VDP-VDP_REG2 C! ;
: VDP-VDP_REG3@ ( -- n ) VDP-VDP_REG3 C@ ;
: VDP-VDP_REG3! ( n -- ) VDP-VDP_REG3 C! ;
: VDP-VDP_REG4@ ( -- n ) VDP-VDP_REG4 C@ ;
: VDP-VDP_REG4! ( n -- ) VDP-VDP_REG4 C! ;
: VDP-VDP_REG5@ ( -- n ) VDP-VDP_REG5 C@ ;
: VDP-VDP_REG5! ( n -- ) VDP-VDP_REG5 C! ;
: VDP-VDP_REG6@ ( -- n ) VDP-VDP_REG6 C@ ;
: VDP-VDP_REG6! ( n -- ) VDP-VDP_REG6 C! ;
: VDP-VDP_REG7@ ( -- n ) VDP-VDP_REG7 C@ ;
: VDP-VDP_REG7! ( n -- ) VDP-VDP_REG7 C! ;
: VDP-VDP_STATUS@ ( -- n ) VDP-VDP_STATUS C@ ;
: VDP-VDP_STATUS! ( n -- ) VDP-VDP_STATUS C! ;
: VDP-VDP_DATA@ ( -- n ) VDP-VDP_DATA C@ ;
: VDP-VDP_DATA! ( n -- ) VDP-VDP_DATA C! ;
: VDP-VDP_POT@ ( -- n ) VDP-VDP_POT C@ ;
: VDP-VDP_POT! ( n -- ) VDP-VDP_POT C! ;

\ PSG外设
: PSG-PSG_REG@ ( -- n ) PSG-PSG_REG C@ ;
: PSG-PSG_REG! ( n -- ) PSG-PSG_REG C! ;
: PSG-PSG_DATA@ ( -- n ) PSG-PSG_DATA C@ ;
: PSG-PSG_DATA! ( n -- ) PSG-PSG_DATA C! ;
: PSG-FREQ_A_LO@ ( -- n ) PSG-FREQ_A_LO C@ ;
: PSG-FREQ_A_LO! ( n -- ) PSG-FREQ_A_LO C! ;
: PSG-FREQ_A_HI@ ( -- n ) PSG-FREQ_A_HI C@ ;
: PSG-FREQ_A_HI! ( n -- ) PSG-FREQ_A_HI C! ;
: PSG-FREQ_B_LO@ ( -- n ) PSG-FREQ_B_LO C@ ;
: PSG-FREQ_B_LO! ( n -- ) PSG-FREQ_B_LO C! ;
: PSG-FREQ_B_HI@ ( -- n ) PSG-FREQ_B_HI C@ ;
: PSG-FREQ_B_HI! ( n -- ) PSG-FREQ_B_HI C! ;
: PSG-FREQ_C_LO@ ( -- n ) PSG-FREQ_C_LO C@ ;
: PSG-FREQ_C_LO! ( n -- ) PSG-FREQ_C_LO C! ;
: PSG-FREQ_C_HI@ ( -- n ) PSG-FREQ_C_HI C@ ;
: PSG-FREQ_C_HI! ( n -- ) PSG-FREQ_C_HI C! ;
: PSG-NOISE_FREQ@ ( -- n ) PSG-NOISE_FREQ C@ ;
: PSG-NOISE_FREQ! ( n -- ) PSG-NOISE_FREQ C! ;
: PSG-ENABLE@ ( -- n ) PSG-ENABLE C@ ;
: PSG-ENABLE! ( n -- ) PSG-ENABLE C! ;
: PSG-VOL_A@ ( -- n ) PSG-VOL_A C@ ;
: PSG-VOL_A! ( n -- ) PSG-VOL_A C! ;
: PSG-VOL_B@ ( -- n ) PSG-VOL_B C@ ;
: PSG-VOL_B! ( n -- ) PSG-VOL_B C! ;
: PSG-VOL_C@ ( -- n ) PSG-VOL_C C@ ;
: PSG-VOL_C! ( n -- ) PSG-VOL_C C! ;
: PSG-ENV_FREQ_LO@ ( -- n ) PSG-ENV_FREQ_LO C@ ;
: PSG-ENV_FREQ_LO! ( n -- ) PSG-ENV_FREQ_LO C! ;
: PSG-ENV_FREQ_HI@ ( -- n ) PSG-ENV_FREQ_HI C@ ;
: PSG-ENV_FREQ_HI! ( n -- ) PSG-ENV_FREQ_HI C! ;
: PSG-ENV_SHAPE@ ( -- n ) PSG-ENV_SHAPE C@ ;
: PSG-ENV_SHAPE! ( n -- ) PSG-ENV_SHAPE C! ;
: PSG-PORT_A@ ( -- n ) PSG-PORT_A C@ ;
: PSG-PORT_A! ( n -- ) PSG-PORT_A C! ;
: PSG-PORT_B@ ( -- n ) PSG-PORT_B C@ ;
: PSG-PORT_B! ( n -- ) PSG-PORT_B C! ;

\ PPI外设
: PPI-PPI_PA@ ( -- n ) PPI-PPI_PA C@ ;
: PPI-PPI_PA! ( n -- ) PPI-PPI_PA C! ;
: PPI-PPI_PB@ ( -- n ) PPI-PPI_PB C@ ;
: PPI-PPI_PB! ( n -- ) PPI-PPI_PB C! ;
: PPI-PPI_PC@ ( -- n ) PPI-PPI_PC C@ ;
: PPI-PPI_PC! ( n -- ) PPI-PPI_PC C! ;
: PPI-PPI_CTRL@ ( -- n ) PPI-PPI_CTRL C@ ;
: PPI-PPI_CTRL! ( n -- ) PPI-PPI_CTRL C! ;

\ SLOTEXP外设
: SLOTEXP-SLOT0@ ( -- n ) SLOTEXP-SLOT0 C@ ;
: SLOTEXP-SLOT0! ( n -- ) SLOTEXP-SLOT0 C! ;
: SLOTEXP-SLOT1@ ( -- n ) SLOTEXP-SLOT1 C@ ;
: SLOTEXP-SLOT1! ( n -- ) SLOTEXP-SLOT1 C! ;
: SLOTEXP-SLOT2@ ( -- n ) SLOTEXP-SLOT2 C@ ;
: SLOTEXP-SLOT2! ( n -- ) SLOTEXP-SLOT2 C! ;
: SLOTEXP-SLOT3@ ( -- n ) SLOTEXP-SLOT3 C@ ;
: SLOTEXP-SLOT3! ( n -- ) SLOTEXP-SLOT3 C! ;
: SLOTEXP-EXPTBL0@ ( -- n ) SLOTEXP-EXPTBL0 C@ ;
: SLOTEXP-EXPTBL0! ( n -- ) SLOTEXP-EXPTBL0 C! ;
: SLOTEXP-EXPTBL1@ ( -- n ) SLOTEXP-EXPTBL1 C@ ;
: SLOTEXP-EXPTBL1! ( n -- ) SLOTEXP-EXPTBL1 C! ;
: SLOTEXP-EXPTBL2@ ( -- n ) SLOTEXP-EXPTBL2 C@ ;
: SLOTEXP-EXPTBL2! ( n -- ) SLOTEXP-EXPTBL2 C! ;
: SLOTEXP-EXPTBL3@ ( -- n ) SLOTEXP-EXPTBL3 C@ ;
: SLOTEXP-EXPTBL3! ( n -- ) SLOTEXP-EXPTBL3 C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MSX1-INIT ( -- )
  \ 初始化MSX1设备
  ." 初始化MSX1..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 F!  \ Flags
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
  0 I!  \ Interrupt Vector
  0 R!  \ Refresh
  0 IX!  \ Index X
  0 IY!  \ Index Y (usually = 0xF38F)
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化VDP
  0 VDP-VDP_REG0!  \ VDP_REG0寄存器
  0 VDP-VDP_REG1!  \ VDP_REG1寄存器
  0 VDP-VDP_REG2!  \ VDP_REG2寄存器
  0 VDP-VDP_REG3!  \ VDP_REG3寄存器
  0 VDP-VDP_REG4!  \ VDP_REG4寄存器
  0 VDP-VDP_REG5!  \ VDP_REG5寄存器
  0 VDP-VDP_REG6!  \ VDP_REG6寄存器
  0 VDP-VDP_REG7!  \ VDP_REG7寄存器
  0 VDP-VDP_STATUS!  \ VDP_STATUS寄存器
  0 VDP-VDP_DATA!  \ VDP_DATA寄存器
  0 VDP-VDP_POT!  \ VDP_POT寄存器
  \ 初始化PSG
  0 PSG-PSG_REG!  \ PSG_REG寄存器
  0 PSG-PSG_DATA!  \ PSG_DATA寄存器
  0 PSG-FREQ_A_LO!  \ FREQ_A_LO寄存器
  0 PSG-FREQ_A_HI!  \ FREQ_A_HI寄存器
  0 PSG-FREQ_B_LO!  \ FREQ_B_LO寄存器
  0 PSG-FREQ_B_HI!  \ FREQ_B_HI寄存器
  0 PSG-FREQ_C_LO!  \ FREQ_C_LO寄存器
  0 PSG-FREQ_C_HI!  \ FREQ_C_HI寄存器
  0 PSG-NOISE_FREQ!  \ NOISE_FREQ寄存器
  0 PSG-ENABLE!  \ ENABLE寄存器
  0 PSG-VOL_A!  \ VOL_A寄存器
  0 PSG-VOL_B!  \ VOL_B寄存器
  0 PSG-VOL_C!  \ VOL_C寄存器
  0 PSG-ENV_FREQ_LO!  \ ENV_FREQ_LO寄存器
  0 PSG-ENV_FREQ_HI!  \ ENV_FREQ_HI寄存器
  0 PSG-ENV_SHAPE!  \ ENV_SHAPE寄存器
  0 PSG-PORT_A!  \ PORT_A寄存器
  0 PSG-PORT_B!  \ PORT_B寄存器
  \ 初始化PPI
  0 PPI-PPI_PA!  \ PPI_PA寄存器
  0 PPI-PPI_PB!  \ PPI_PB寄存器
  0 PPI-PPI_PC!  \ PPI_PC寄存器
  0 PPI-PPI_CTRL!  \ PPI_CTRL寄存器
  \ 初始化SLOTEXP
  0 SLOTEXP-SLOT0!  \ SLOT0寄存器
  0 SLOTEXP-SLOT1!  \ SLOT1寄存器
  0 SLOTEXP-SLOT2!  \ SLOT2寄存器
  0 SLOTEXP-SLOT3!  \ SLOT3寄存器
  0 SLOTEXP-EXPTBL0!  \ EXPTBL0寄存器
  0 SLOTEXP-EXPTBL1!  \ EXPTBL1寄存器
  0 SLOTEXP-EXPTBL2!  \ EXPTBL2寄存器
  0 SLOTEXP-EXPTBL3!  \ EXPTBL3寄存器

  ." MSX1初始化完成" CR
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

\ Non-Maskable Interrupt
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

\ VDP Vertical Interrupt (frame)
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
  MSX1-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
