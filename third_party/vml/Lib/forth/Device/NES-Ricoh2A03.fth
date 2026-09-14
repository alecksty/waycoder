\ Ricoh-2A03设备定义 - Forth文件
\ 生成自: Ricoh/MOS-6502/Ricoh-2A03
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
\ CPU架构: MOS-6502
\ 位宽: 8位
\ 时钟频率: 10765930 Hz

\ =========================================
\ Ricoh-2A03设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Ricoh-2A03" ;
: MANUFACTURER  S" Ricoh" ;
: FAMILY        S" MOS-6502" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS-6502" ;
8 CONSTANT BITS
10765930 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT X  \ X Index
0x02 CONSTANT Y  \ Y Index
0x03 CONSTANT SP  \ Stack Pointer
0x04 CONSTANT PC  \ Program Counter (16-bit)
0x06 CONSTANT P  \ Processor Status
0 CONSTANT P-C  \ Carry
1 CONSTANT P-Z  \ Zero
2 CONSTANT P-I  \ Interrupt Disable
3 CONSTANT P-D  \ Decimal Mode
4 CONSTANT P-B  \ Break
5 CONSTANT P-U  \ Unused
6 CONSTANT P-V  \ Overflow
7 CONSTANT P-N  \ Negative

\ 内存段定义
0x0000 CONSTANT CPU_RAM-START
0x07FF CONSTANT CPU_RAM-END
2048 CONSTANT CPU_RAM-SIZE  \ CPU 2KB RAM (mirrored)
0x2000 CONSTANT PPU_REGISTERS-START
0x3FFF CONSTANT PPU_REGISTERS-END
8192 CONSTANT PPU_REGISTERS-SIZE  \ PPU Registers (mirrored every 8 bytes)
0x4000 CONSTANT APU_REGISTERS-START
0x401F CONSTANT APU_REGISTERS-END
32 CONSTANT APU_REGISTERS-SIZE  \ APU and I/O Registers
0x4020 CONSTANT EXPANSION-START
0x5FFF CONSTANT EXPANSION-END
8160 CONSTANT EXPANSION-SIZE  \ Expansion ROM
0x6000 CONSTANT SRAM-START
0x7FFF CONSTANT SRAM-END
8192 CONSTANT SRAM-SIZE  \ Save RAM
0x8000 CONSTANT PRG_ROM_LOW-START
0xBFFF CONSTANT PRG_ROM_LOW-END
16384 CONSTANT PRG_ROM_LOW-SIZE  \ PRG ROM Lower Bank (16KB)
0xC000 CONSTANT PRG_ROM_HIGH-START
0xFFFF CONSTANT PRG_ROM_HIGH-END
16384 CONSTANT PRG_ROM_HIGH-SIZE  \ PRG ROM Higher Bank (16KB)

\ 外设定义
\ Picture Processing Unit
0x2000 CONSTANT PPU-BASE
0x2000 CONSTANT PPU-PPUCTRL
0x2001 CONSTANT PPU-PPUMASK
0x2002 CONSTANT PPU-PPUSTATUS
0x2003 CONSTANT PPU-OAMADDR
0x2004 CONSTANT PPU-OAMDATA
0x2005 CONSTANT PPU-PPUSCROLL
0x2006 CONSTANT PPU-PPUADDR
0x2007 CONSTANT PPU-PPUDATA
\ Audio Processing Unit
0x4000 CONSTANT APU-BASE
0x4000 CONSTANT APU-PULSE1_VOL
0x4001 CONSTANT APU-PULSE1_SWEEP
0x4002 CONSTANT APU-PULSE1_LO
0x4003 CONSTANT APU-PULSE1_HI
0x4004 CONSTANT APU-PULSE2_VOL
0x4005 CONSTANT APU-PULSE2_SWEEP
0x4006 CONSTANT APU-PULSE2_LO
0x4007 CONSTANT APU-PULSE2_HI
0x4008 CONSTANT APU-TRIANGLE
0x400B CONSTANT APU-TRIANGLE_HI
0x400C CONSTANT APU-NOISE_VOL
0x400E CONSTANT APU-NOISE_HI
0x400F CONSTANT APU-NOISE_LENGTH
0x4010 CONSTANT APU-DMC_RATE
0x4011 CONSTANT APU-DMC_RAW
0x4012 CONSTANT APU-DMC_START
0x4013 CONSTANT APU-DMC_LENGTH
0x4014 CONSTANT APU-OAMDMA
0x4015 CONSTANT APU-SNDCHN
0x4016 CONSTANT APU-JOY1
0x4017 CONSTANT APU-JOY2
\ Controller Port 1
0x4016 CONSTANT INPUT1-BASE
0x4016 CONSTANT INPUT1-JOYPAD1
\ Controller Port 2
0x4017 CONSTANT INPUT2-BASE
0x4017 CONSTANT INPUT2-JOYPAD2

\ 中断向量定义
0 CONSTANT INT-RESET  \ Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt (VBlank)
2 CONSTANT INT-IRQ  \ IRQ / BRK

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: X@ ( -- n ) X C@ ;
: X! ( n -- ) X C! ;

: Y@ ( -- n ) Y C@ ;
: Y! ( n -- ) Y C! ;

: SP@ ( -- n ) SP C@ ;
: SP! ( n -- ) SP C! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

: P@ ( -- n ) P C@ ;
: P! ( n -- ) P C! ;
: P-C@ ( -- flag ) P@ 0 BIT@ ;
: P-C! ( flag -- ) P@ 0 BIT! P! ;
: P-C-SET ( -- ) TRUE P-C! ;
: P-C-CLR ( -- ) FALSE P-C! ;
: P-Z@ ( -- flag ) P@ 1 BIT@ ;
: P-Z! ( flag -- ) P@ 1 BIT! P! ;
: P-Z-SET ( -- ) TRUE P-Z! ;
: P-Z-CLR ( -- ) FALSE P-Z! ;
: P-I@ ( -- flag ) P@ 2 BIT@ ;
: P-I! ( flag -- ) P@ 2 BIT! P! ;
: P-I-SET ( -- ) TRUE P-I! ;
: P-I-CLR ( -- ) FALSE P-I! ;
: P-D@ ( -- flag ) P@ 3 BIT@ ;
: P-D! ( flag -- ) P@ 3 BIT! P! ;
: P-D-SET ( -- ) TRUE P-D! ;
: P-D-CLR ( -- ) FALSE P-D! ;
: P-B@ ( -- flag ) P@ 4 BIT@ ;
: P-B! ( flag -- ) P@ 4 BIT! P! ;
: P-B-SET ( -- ) TRUE P-B! ;
: P-B-CLR ( -- ) FALSE P-B! ;
: P-U@ ( -- flag ) P@ 5 BIT@ ;
: P-U! ( flag -- ) P@ 5 BIT! P! ;
: P-U-SET ( -- ) TRUE P-U! ;
: P-U-CLR ( -- ) FALSE P-U! ;
: P-V@ ( -- flag ) P@ 6 BIT@ ;
: P-V! ( flag -- ) P@ 6 BIT! P! ;
: P-V-SET ( -- ) TRUE P-V! ;
: P-V-CLR ( -- ) FALSE P-V! ;
: P-N@ ( -- flag ) P@ 7 BIT@ ;
: P-N! ( flag -- ) P@ 7 BIT! P! ;
: P-N-SET ( -- ) TRUE P-N! ;
: P-N-CLR ( -- ) FALSE P-N! ;

\ 外设访问
\ PPU外设
: PPU-PPUCTRL@ ( -- n ) PPU-PPUCTRL C@ ;
: PPU-PPUCTRL! ( n -- ) PPU-PPUCTRL C! ;
: PPU-PPUMASK@ ( -- n ) PPU-PPUMASK C@ ;
: PPU-PPUMASK! ( n -- ) PPU-PPUMASK C! ;
: PPU-PPUSTATUS@ ( -- n ) PPU-PPUSTATUS C@ ;
: PPU-PPUSTATUS! ( n -- ) PPU-PPUSTATUS C! ;
: PPU-OAMADDR@ ( -- n ) PPU-OAMADDR C@ ;
: PPU-OAMADDR! ( n -- ) PPU-OAMADDR C! ;
: PPU-OAMDATA@ ( -- n ) PPU-OAMDATA C@ ;
: PPU-OAMDATA! ( n -- ) PPU-OAMDATA C! ;
: PPU-PPUSCROLL@ ( -- n ) PPU-PPUSCROLL C@ ;
: PPU-PPUSCROLL! ( n -- ) PPU-PPUSCROLL C! ;
: PPU-PPUADDR@ ( -- n ) PPU-PPUADDR C@ ;
: PPU-PPUADDR! ( n -- ) PPU-PPUADDR C! ;
: PPU-PPUDATA@ ( -- n ) PPU-PPUDATA C@ ;
: PPU-PPUDATA! ( n -- ) PPU-PPUDATA C! ;

\ APU外设
: APU-PULSE1_VOL@ ( -- n ) APU-PULSE1_VOL C@ ;
: APU-PULSE1_VOL! ( n -- ) APU-PULSE1_VOL C! ;
: APU-PULSE1_SWEEP@ ( -- n ) APU-PULSE1_SWEEP C@ ;
: APU-PULSE1_SWEEP! ( n -- ) APU-PULSE1_SWEEP C! ;
: APU-PULSE1_LO@ ( -- n ) APU-PULSE1_LO C@ ;
: APU-PULSE1_LO! ( n -- ) APU-PULSE1_LO C! ;
: APU-PULSE1_HI@ ( -- n ) APU-PULSE1_HI C@ ;
: APU-PULSE1_HI! ( n -- ) APU-PULSE1_HI C! ;
: APU-PULSE2_VOL@ ( -- n ) APU-PULSE2_VOL C@ ;
: APU-PULSE2_VOL! ( n -- ) APU-PULSE2_VOL C! ;
: APU-PULSE2_SWEEP@ ( -- n ) APU-PULSE2_SWEEP C@ ;
: APU-PULSE2_SWEEP! ( n -- ) APU-PULSE2_SWEEP C! ;
: APU-PULSE2_LO@ ( -- n ) APU-PULSE2_LO C@ ;
: APU-PULSE2_LO! ( n -- ) APU-PULSE2_LO C! ;
: APU-PULSE2_HI@ ( -- n ) APU-PULSE2_HI C@ ;
: APU-PULSE2_HI! ( n -- ) APU-PULSE2_HI C! ;
: APU-TRIANGLE@ ( -- n ) APU-TRIANGLE C@ ;
: APU-TRIANGLE! ( n -- ) APU-TRIANGLE C! ;
: APU-TRIANGLE_HI@ ( -- n ) APU-TRIANGLE_HI C@ ;
: APU-TRIANGLE_HI! ( n -- ) APU-TRIANGLE_HI C! ;
: APU-NOISE_VOL@ ( -- n ) APU-NOISE_VOL C@ ;
: APU-NOISE_VOL! ( n -- ) APU-NOISE_VOL C! ;
: APU-NOISE_HI@ ( -- n ) APU-NOISE_HI C@ ;
: APU-NOISE_HI! ( n -- ) APU-NOISE_HI C! ;
: APU-NOISE_LENGTH@ ( -- n ) APU-NOISE_LENGTH C@ ;
: APU-NOISE_LENGTH! ( n -- ) APU-NOISE_LENGTH C! ;
: APU-DMC_RATE@ ( -- n ) APU-DMC_RATE C@ ;
: APU-DMC_RATE! ( n -- ) APU-DMC_RATE C! ;
: APU-DMC_RAW@ ( -- n ) APU-DMC_RAW C@ ;
: APU-DMC_RAW! ( n -- ) APU-DMC_RAW C! ;
: APU-DMC_START@ ( -- n ) APU-DMC_START C@ ;
: APU-DMC_START! ( n -- ) APU-DMC_START C! ;
: APU-DMC_LENGTH@ ( -- n ) APU-DMC_LENGTH C@ ;
: APU-DMC_LENGTH! ( n -- ) APU-DMC_LENGTH C! ;
: APU-OAMDMA@ ( -- n ) APU-OAMDMA C@ ;
: APU-OAMDMA! ( n -- ) APU-OAMDMA C! ;
: APU-SNDCHN@ ( -- n ) APU-SNDCHN C@ ;
: APU-SNDCHN! ( n -- ) APU-SNDCHN C! ;
: APU-JOY1@ ( -- n ) APU-JOY1 C@ ;
: APU-JOY1! ( n -- ) APU-JOY1 C! ;
: APU-JOY2@ ( -- n ) APU-JOY2 C@ ;
: APU-JOY2! ( n -- ) APU-JOY2 C! ;

\ INPUT1外设
: INPUT1-JOYPAD1@ ( -- n ) INPUT1-JOYPAD1 C@ ;
: INPUT1-JOYPAD1! ( n -- ) INPUT1-JOYPAD1 C! ;

\ INPUT2外设
: INPUT2-JOYPAD2@ ( -- n ) INPUT2-JOYPAD2 C@ ;
: INPUT2-JOYPAD2! ( n -- ) INPUT2-JOYPAD2 C! ;

\ =========================================
\ 设备初始化
\ =========================================

: RICOH_2A03-INIT ( -- )
  \ 初始化Ricoh-2A03设备
  ." 初始化Ricoh-2A03..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ X Index
  0 Y!  \ Y Index
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter (16-bit)
  0 P!  \ Processor Status

  \ 初始化外设
  \ 初始化PPU
  0 PPU-PPUCTRL!  \ PPUCTRL寄存器
  0 PPU-PPUMASK!  \ PPUMASK寄存器
  0 PPU-PPUSTATUS!  \ PPUSTATUS寄存器
  0 PPU-OAMADDR!  \ OAMADDR寄存器
  0 PPU-OAMDATA!  \ OAMDATA寄存器
  0 PPU-PPUSCROLL!  \ PPUSCROLL寄存器
  0 PPU-PPUADDR!  \ PPUADDR寄存器
  0 PPU-PPUDATA!  \ PPUDATA寄存器
  \ 初始化APU
  0 APU-PULSE1_VOL!  \ PULSE1_VOL寄存器
  0 APU-PULSE1_SWEEP!  \ PULSE1_SWEEP寄存器
  0 APU-PULSE1_LO!  \ PULSE1_LO寄存器
  0 APU-PULSE1_HI!  \ PULSE1_HI寄存器
  0 APU-PULSE2_VOL!  \ PULSE2_VOL寄存器
  0 APU-PULSE2_SWEEP!  \ PULSE2_SWEEP寄存器
  0 APU-PULSE2_LO!  \ PULSE2_LO寄存器
  0 APU-PULSE2_HI!  \ PULSE2_HI寄存器
  0 APU-TRIANGLE!  \ TRIANGLE寄存器
  0 APU-TRIANGLE_HI!  \ TRIANGLE_HI寄存器
  0 APU-NOISE_VOL!  \ NOISE_VOL寄存器
  0 APU-NOISE_HI!  \ NOISE_HI寄存器
  0 APU-NOISE_LENGTH!  \ NOISE_LENGTH寄存器
  0 APU-DMC_RATE!  \ DMC_RATE寄存器
  0 APU-DMC_RAW!  \ DMC_RAW寄存器
  0 APU-DMC_START!  \ DMC_START寄存器
  0 APU-DMC_LENGTH!  \ DMC_LENGTH寄存器
  0 APU-OAMDMA!  \ OAMDMA寄存器
  0 APU-SNDCHN!  \ SNDCHN寄存器
  0 APU-JOY1!  \ JOY1寄存器
  0 APU-JOY2!  \ JOY2寄存器
  \ 初始化INPUT1
  0 INPUT1-JOYPAD1!  \ JOYPAD1寄存器
  \ 初始化INPUT2
  0 INPUT2-JOYPAD2!  \ JOYPAD2寄存器

  ." Ricoh-2A03初始化完成" CR
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
  X@ X .R 8 .R SPACE ."  X: " X@ .
  Y@ Y .R 8 .R SPACE ."  Y: " Y@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  P@ P .R 8 .R SPACE ."  P: " P@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Reset
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

\ Non-Maskable Interrupt (VBlank)
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

\ IRQ / BRK
: INT-IRQ-HANDLER ( -- )
  ." IRQ中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ-ENABLE ( -- )
  INT-IRQ INT-ENABLE
;

: INT-IRQ-DISABLE ( -- )
  INT-IRQ INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  RICOH_2A03-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
