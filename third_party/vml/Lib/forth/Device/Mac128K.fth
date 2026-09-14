\ Macintosh-128K设备定义 - Forth文件
\ 生成自: Apple Computer/Macintosh/Macintosh-128K
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
\ CPU架构: MC68000
\ 位宽: 32位
\ 时钟频率: 7833600 Hz

\ =========================================
\ Macintosh-128K设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Macintosh-128K" ;
: MANUFACTURER  S" Apple Computer" ;
: FAMILY        S" Macintosh" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MC68000" ;
32 CONSTANT BITS
7833600 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT D0  \ Data Register 0
0x04 CONSTANT D1  \ Data Register 1
0x08 CONSTANT D2  \ Data Register 2
0x0C CONSTANT D3  \ Data Register 3
0x10 CONSTANT D4  \ Data Register 4
0x14 CONSTANT D5  \ Data Register 5
0x18 CONSTANT D6  \ Data Register 6
0x1C CONSTANT D7  \ Data Register 7
0x20 CONSTANT A0  \ Address Register 0
0x24 CONSTANT A1  \ Address Register 1
0x28 CONSTANT A2  \ Address Register 2
0x2C CONSTANT A3  \ Address Register 3
0x30 CONSTANT A4  \ Address Register 4
0x34 CONSTANT A5  \ Address Register 5
0x38 CONSTANT A6  \ Address Register 6
0x3C CONSTANT A7  \ Stack Pointer (USP)
0x40 CONSTANT PC  \ Program Counter
0x44 CONSTANT SR  \ Status Register
0 CONSTANT SR-C  \ Carry
1 CONSTANT SR-V  \ Overflow
2 CONSTANT SR-Z  \ Zero
3 CONSTANT SR-N  \ Negative
4 CONSTANT SR-X  \ Extend
8 CONSTANT SR-I0  \ Interrupt Mask 0
9 CONSTANT SR-I1  \ Interrupt Mask 1
10 CONSTANT SR-I2  \ Interrupt Mask 2
13 CONSTANT SR-S  \ Supervisor/User
14 CONSTANT SR-T0  \ Trace Mode 0
15 CONSTANT SR-T1  \ Trace Mode 1

\ 内存段定义
0x000000 CONSTANT RAM-START
0x01FFFF CONSTANT RAM-END
131072 CONSTANT RAM-SIZE  \ Main RAM (128KB unified)
0x40000000 CONSTANT ROM-START
0x4001FFFF CONSTANT ROM-END
131072 CONSTANT ROM-SIZE  \ Mac ROM (128KB)
0x00400000 CONSTANT FRAMEBUFFER-START
0x00400555 CONSTANT FRAMEBUFFER-END
1366 CONSTANT FRAMEBUFFER-SIZE  \ Screen bitmap (512x342x1 = 21792 bytes)
0x00410000 CONSTANT FRAMEBUFFER2-START
0x00410555 CONSTANT FRAMEBUFFER2-END
1366 CONSTANT FRAMEBUFFER2-SIZE  \ Shadow screen (double-buffering)
0x00E00000 CONSTANT VIA-START
0x00E0FFFF CONSTANT VIA-END
4096 CONSTANT VIA-SIZE  \ VIA 6522 (I/O)
0x00F00000 CONSTANT SCC-START
0x00F0FFFF CONSTANT SCC-END
4096 CONSTANT SCC-SIZE  \ SCC 8530 (serial)
0x01600000 CONSTANT ADB-START
0x0160FFFF CONSTANT ADB-END
4096 CONSTANT ADB-SIZE  \ ADB bus
0x01E00000 CONSTANT IWM-START
0x01E0FFFF CONSTANT IWM-END
4096 CONSTANT IWM-SIZE  \ IWM floppy controller

\ 外设定义
\ Versatile Interface Adapter 6522
0xE00000 CONSTANT VIA-BASE
0xE00000 CONSTANT VIA-ORB
0xE00002 CONSTANT VIA-ORA
0xE00004 CONSTANT VIA-DDRB
0xE00006 CONSTANT VIA-DDRA
0xE00008 CONSTANT VIA-T1C_L
0xE0000A CONSTANT VIA-T1C_H
0xE0000C CONSTANT VIA-T1L_L
0xE0000E CONSTANT VIA-T1L_H
0xE00010 CONSTANT VIA-T2C_L
0xE00012 CONSTANT VIA-T2C_H
0xE00014 CONSTANT VIA-SR
0xE00016 CONSTANT VIA-ACR
0xE00018 CONSTANT VIA-PCR
0xE0001E CONSTANT VIA-IFR
0xE0001E CONSTANT VIA-IER
\ SCC 8530 Serial Communications Controller
0xF00000 CONSTANT SCC-BASE
0xF00000 CONSTANT SCC-SCC_CHA_B
0xF00002 CONSTANT SCC-SCC_CHA_C
0xF00004 CONSTANT SCC-SCC_CHB_D
0xF00006 CONSTANT SCC-SCC_CHB_CT
\ Integrated Woz Machine - Floppy Disk Controller
0x1E00000 CONSTANT IWM-BASE
0x1E00000 CONSTANT IWM-IWM_DATA
0x1E00008 CONSTANT IWM-IWM_MODE
0x1E00020 CONSTANT IWM-IWM_Q6L
0x1E00022 CONSTANT IWM-IWM_Q7L
0x1E00024 CONSTANT IWM-IWM_Q6R
0x1E00026 CONSTANT IWM-IWM_Q7R
\ Video Graphics Controller (custom Apple chip)
0x00F20000 CONSTANT VGC-BASE
0x00F20000 CONSTANT VGC-VGC_MODE
0x00F20002 CONSTANT VGC-VGC_START_HI
0x00F20004 CONSTANT VGC-VGC_START_LO
\ Apple Desktop Bus
0x01600000 CONSTANT ADB-BASE
0x01600000 CONSTANT ADB-ADB_DATA
0x01600004 CONSTANT ADB-ADB_STATUS
0x01600008 CONSTANT ADB-ADB_CMD

\ 中断向量定义
1 CONSTANT INT-RESET  \ Reset Initial SP
2 CONSTANT INT-RESET_PC  \ Reset Initial PC
24 CONSTANT INT-IRQ1  \ VIA interrupt (level 1)
25 CONSTANT INT-IRQ2  \ SCC interrupt (level 2)
26 CONSTANT INT-IRQ3  \ ADB / VIA (level 3)
27 CONSTANT INT-IRQ4  \ ADB / VIA (level 4)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-CLK  \ 16MHz master clock / 7.83MHz CPU clock
4 CONSTANT PIN-FC0  \ Function Code 0
5 CONSTANT PIN-FC1  \ Function Code 1
6 CONSTANT PIN-FC2  \ Function Code 2
7 CONSTANT PIN-AS  \ Address Strobe
8 CONSTANT PIN-UDS  \ Upper Data Strobe
9 CONSTANT PIN-LDS  \ Lower Data Strobe
10 CONSTANT PIN-RWB  \ Read/Write
11 CONSTANT PIN-DTACK  \ Data Acknowledge
12 CONSTANT PIN-BERR  \ Bus Error
13 CONSTANT PIN-BR  \ Bus Request
14 CONSTANT PIN-BG  \ Bus Grant
15 CONSTANT PIN-BGACK  \ Bus Grant Acknowledge
16 CONSTANT PIN-IPL0  \ Interrupt Priority 0
17 CONSTANT PIN-IPL1  \ Interrupt Priority 1
18 CONSTANT PIN-IPL2  \ Interrupt Priority 2
19 CONSTANT PIN-RESET  \ Reset
20 CONSTANT PIN-HALT  \ Halt
21 CONSTANT PIN-A1_A23  \ Address Bus (24-bit)
22 CONSTANT PIN-D0_D15  \ Data Bus (16-bit)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: D0@ ( -- n ) D0 L@ ;
: D0! ( n -- ) D0 L! ;

: D1@ ( -- n ) D1 L@ ;
: D1! ( n -- ) D1 L! ;

: D2@ ( -- n ) D2 L@ ;
: D2! ( n -- ) D2 L! ;

: D3@ ( -- n ) D3 L@ ;
: D3! ( n -- ) D3 L! ;

: D4@ ( -- n ) D4 L@ ;
: D4! ( n -- ) D4 L! ;

: D5@ ( -- n ) D5 L@ ;
: D5! ( n -- ) D5 L! ;

: D6@ ( -- n ) D6 L@ ;
: D6! ( n -- ) D6 L! ;

: D7@ ( -- n ) D7 L@ ;
: D7! ( n -- ) D7 L! ;

: A0@ ( -- n ) A0 L@ ;
: A0! ( n -- ) A0 L! ;

: A1@ ( -- n ) A1 L@ ;
: A1! ( n -- ) A1 L! ;

: A2@ ( -- n ) A2 L@ ;
: A2! ( n -- ) A2 L! ;

: A3@ ( -- n ) A3 L@ ;
: A3! ( n -- ) A3 L! ;

: A4@ ( -- n ) A4 L@ ;
: A4! ( n -- ) A4 L! ;

: A5@ ( -- n ) A5 L@ ;
: A5! ( n -- ) A5 L! ;

: A6@ ( -- n ) A6 L@ ;
: A6! ( n -- ) A6 L! ;

: A7@ ( -- n ) A7 L@ ;
: A7! ( n -- ) A7 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

: SR@ ( -- n ) SR @ ;
: SR! ( n -- ) SR ! ;
: SR-C@ ( -- flag ) SR@ 0 BIT@ ;
: SR-C! ( flag -- ) SR@ 0 BIT! SR! ;
: SR-C-SET ( -- ) TRUE SR-C! ;
: SR-C-CLR ( -- ) FALSE SR-C! ;
: SR-V@ ( -- flag ) SR@ 1 BIT@ ;
: SR-V! ( flag -- ) SR@ 1 BIT! SR! ;
: SR-V-SET ( -- ) TRUE SR-V! ;
: SR-V-CLR ( -- ) FALSE SR-V! ;
: SR-Z@ ( -- flag ) SR@ 2 BIT@ ;
: SR-Z! ( flag -- ) SR@ 2 BIT! SR! ;
: SR-Z-SET ( -- ) TRUE SR-Z! ;
: SR-Z-CLR ( -- ) FALSE SR-Z! ;
: SR-N@ ( -- flag ) SR@ 3 BIT@ ;
: SR-N! ( flag -- ) SR@ 3 BIT! SR! ;
: SR-N-SET ( -- ) TRUE SR-N! ;
: SR-N-CLR ( -- ) FALSE SR-N! ;
: SR-X@ ( -- flag ) SR@ 4 BIT@ ;
: SR-X! ( flag -- ) SR@ 4 BIT! SR! ;
: SR-X-SET ( -- ) TRUE SR-X! ;
: SR-X-CLR ( -- ) FALSE SR-X! ;
: SR-I0@ ( -- flag ) SR@ 8 BIT@ ;
: SR-I0! ( flag -- ) SR@ 8 BIT! SR! ;
: SR-I0-SET ( -- ) TRUE SR-I0! ;
: SR-I0-CLR ( -- ) FALSE SR-I0! ;
: SR-I1@ ( -- flag ) SR@ 9 BIT@ ;
: SR-I1! ( flag -- ) SR@ 9 BIT! SR! ;
: SR-I1-SET ( -- ) TRUE SR-I1! ;
: SR-I1-CLR ( -- ) FALSE SR-I1! ;
: SR-I2@ ( -- flag ) SR@ 10 BIT@ ;
: SR-I2! ( flag -- ) SR@ 10 BIT! SR! ;
: SR-I2-SET ( -- ) TRUE SR-I2! ;
: SR-I2-CLR ( -- ) FALSE SR-I2! ;
: SR-S@ ( -- flag ) SR@ 13 BIT@ ;
: SR-S! ( flag -- ) SR@ 13 BIT! SR! ;
: SR-S-SET ( -- ) TRUE SR-S! ;
: SR-S-CLR ( -- ) FALSE SR-S! ;
: SR-T0@ ( -- flag ) SR@ 14 BIT@ ;
: SR-T0! ( flag -- ) SR@ 14 BIT! SR! ;
: SR-T0-SET ( -- ) TRUE SR-T0! ;
: SR-T0-CLR ( -- ) FALSE SR-T0! ;
: SR-T1@ ( -- flag ) SR@ 15 BIT@ ;
: SR-T1! ( flag -- ) SR@ 15 BIT! SR! ;
: SR-T1-SET ( -- ) TRUE SR-T1! ;
: SR-T1-CLR ( -- ) FALSE SR-T1! ;

\ 外设访问
\ VIA外设
: VIA-ORB@ ( -- n ) VIA-ORB C@ ;
: VIA-ORB! ( n -- ) VIA-ORB C! ;
: VIA-ORA@ ( -- n ) VIA-ORA C@ ;
: VIA-ORA! ( n -- ) VIA-ORA C! ;
: VIA-DDRB@ ( -- n ) VIA-DDRB C@ ;
: VIA-DDRB! ( n -- ) VIA-DDRB C! ;
: VIA-DDRA@ ( -- n ) VIA-DDRA C@ ;
: VIA-DDRA! ( n -- ) VIA-DDRA C! ;
: VIA-T1C_L@ ( -- n ) VIA-T1C_L @ ;
: VIA-T1C_L! ( n -- ) VIA-T1C_L ! ;
: VIA-T1C_H@ ( -- n ) VIA-T1C_H @ ;
: VIA-T1C_H! ( n -- ) VIA-T1C_H ! ;
: VIA-T1L_L@ ( -- n ) VIA-T1L_L @ ;
: VIA-T1L_L! ( n -- ) VIA-T1L_L ! ;
: VIA-T1L_H@ ( -- n ) VIA-T1L_H @ ;
: VIA-T1L_H! ( n -- ) VIA-T1L_H ! ;
: VIA-T2C_L@ ( -- n ) VIA-T2C_L @ ;
: VIA-T2C_L! ( n -- ) VIA-T2C_L ! ;
: VIA-T2C_H@ ( -- n ) VIA-T2C_H @ ;
: VIA-T2C_H! ( n -- ) VIA-T2C_H ! ;
: VIA-SR@ ( -- n ) VIA-SR C@ ;
: VIA-SR! ( n -- ) VIA-SR C! ;
: VIA-ACR@ ( -- n ) VIA-ACR C@ ;
: VIA-ACR! ( n -- ) VIA-ACR C! ;
: VIA-PCR@ ( -- n ) VIA-PCR C@ ;
: VIA-PCR! ( n -- ) VIA-PCR C! ;
: VIA-IFR@ ( -- n ) VIA-IFR C@ ;
: VIA-IFR! ( n -- ) VIA-IFR C! ;
: VIA-IER@ ( -- n ) VIA-IER C@ ;
: VIA-IER! ( n -- ) VIA-IER C! ;

\ SCC外设
: SCC-SCC_CHA_B@ ( -- n ) SCC-SCC_CHA_B C@ ;
: SCC-SCC_CHA_B! ( n -- ) SCC-SCC_CHA_B C! ;
: SCC-SCC_CHA_C@ ( -- n ) SCC-SCC_CHA_C C@ ;
: SCC-SCC_CHA_C! ( n -- ) SCC-SCC_CHA_C C! ;
: SCC-SCC_CHB_D@ ( -- n ) SCC-SCC_CHB_D C@ ;
: SCC-SCC_CHB_D! ( n -- ) SCC-SCC_CHB_D C! ;
: SCC-SCC_CHB_CT@ ( -- n ) SCC-SCC_CHB_CT C@ ;
: SCC-SCC_CHB_CT! ( n -- ) SCC-SCC_CHB_CT C! ;

\ IWM外设
: IWM-IWM_DATA@ ( -- n ) IWM-IWM_DATA C@ ;
: IWM-IWM_DATA! ( n -- ) IWM-IWM_DATA C! ;
: IWM-IWM_MODE@ ( -- n ) IWM-IWM_MODE C@ ;
: IWM-IWM_MODE! ( n -- ) IWM-IWM_MODE C! ;
: IWM-IWM_Q6L@ ( -- n ) IWM-IWM_Q6L C@ ;
: IWM-IWM_Q6L! ( n -- ) IWM-IWM_Q6L C! ;
: IWM-IWM_Q7L@ ( -- n ) IWM-IWM_Q7L C@ ;
: IWM-IWM_Q7L! ( n -- ) IWM-IWM_Q7L C! ;
: IWM-IWM_Q6R@ ( -- n ) IWM-IWM_Q6R C@ ;
: IWM-IWM_Q6R! ( n -- ) IWM-IWM_Q6R C! ;
: IWM-IWM_Q7R@ ( -- n ) IWM-IWM_Q7R C@ ;
: IWM-IWM_Q7R! ( n -- ) IWM-IWM_Q7R C! ;

\ VGC外设
: VGC-VGC_MODE@ ( -- n ) VGC-VGC_MODE C@ ;
: VGC-VGC_MODE! ( n -- ) VGC-VGC_MODE C! ;
: VGC-VGC_START_HI@ ( -- n ) VGC-VGC_START_HI C@ ;
: VGC-VGC_START_HI! ( n -- ) VGC-VGC_START_HI C! ;
: VGC-VGC_START_LO@ ( -- n ) VGC-VGC_START_LO C@ ;
: VGC-VGC_START_LO! ( n -- ) VGC-VGC_START_LO C! ;

\ ADB外设
: ADB-ADB_DATA@ ( -- n ) ADB-ADB_DATA C@ ;
: ADB-ADB_DATA! ( n -- ) ADB-ADB_DATA C! ;
: ADB-ADB_STATUS@ ( -- n ) ADB-ADB_STATUS C@ ;
: ADB-ADB_STATUS! ( n -- ) ADB-ADB_STATUS C! ;
: ADB-ADB_CMD@ ( -- n ) ADB-ADB_CMD C@ ;
: ADB-ADB_CMD! ( n -- ) ADB-ADB_CMD C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MACINTOSH_128K-INIT ( -- )
  \ 初始化Macintosh-128K设备
  ." 初始化Macintosh-128K..." CR

  \ 初始化寄存器
  0 D0!  \ Data Register 0
  0 D1!  \ Data Register 1
  0 D2!  \ Data Register 2
  0 D3!  \ Data Register 3
  0 D4!  \ Data Register 4
  0 D5!  \ Data Register 5
  0 D6!  \ Data Register 6
  0 D7!  \ Data Register 7
  0 A0!  \ Address Register 0
  0 A1!  \ Address Register 1
  0 A2!  \ Address Register 2
  0 A3!  \ Address Register 3
  0 A4!  \ Address Register 4
  0 A5!  \ Address Register 5
  0 A6!  \ Address Register 6
  0 A7!  \ Stack Pointer (USP)
  0 PC!  \ Program Counter
  0 SR!  \ Status Register

  \ 初始化外设
  \ 初始化VIA
  0 VIA-ORB!  \ ORB寄存器
  0 VIA-ORA!  \ ORA寄存器
  0 VIA-DDRB!  \ DDRB寄存器
  0 VIA-DDRA!  \ DDRA寄存器
  0 VIA-T1C_L!  \ T1C_L寄存器
  0 VIA-T1C_H!  \ T1C_H寄存器
  0 VIA-T1L_L!  \ T1L_L寄存器
  0 VIA-T1L_H!  \ T1L_H寄存器
  0 VIA-T2C_L!  \ T2C_L寄存器
  0 VIA-T2C_H!  \ T2C_H寄存器
  0 VIA-SR!  \ SR寄存器
  0 VIA-ACR!  \ ACR寄存器
  0 VIA-PCR!  \ PCR寄存器
  0 VIA-IFR!  \ IFR寄存器
  0 VIA-IER!  \ IER寄存器
  \ 初始化SCC
  0 SCC-SCC_CHA_B!  \ SCC_CHA_B寄存器
  0 SCC-SCC_CHA_C!  \ SCC_CHA_C寄存器
  0 SCC-SCC_CHB_D!  \ SCC_CHB_D寄存器
  0 SCC-SCC_CHB_CT!  \ SCC_CHB_CT寄存器
  \ 初始化IWM
  0 IWM-IWM_DATA!  \ IWM_DATA寄存器
  0 IWM-IWM_MODE!  \ IWM_MODE寄存器
  0 IWM-IWM_Q6L!  \ IWM_Q6L寄存器
  0 IWM-IWM_Q7L!  \ IWM_Q7L寄存器
  0 IWM-IWM_Q6R!  \ IWM_Q6R寄存器
  0 IWM-IWM_Q7R!  \ IWM_Q7R寄存器
  \ 初始化VGC
  0 VGC-VGC_MODE!  \ VGC_MODE寄存器
  0 VGC-VGC_START_HI!  \ VGC_START_HI寄存器
  0 VGC-VGC_START_LO!  \ VGC_START_LO寄存器
  \ 初始化ADB
  0 ADB-ADB_DATA!  \ ADB_DATA寄存器
  0 ADB-ADB_STATUS!  \ ADB_STATUS寄存器
  0 ADB-ADB_CMD!  \ ADB_CMD寄存器

  ." Macintosh-128K初始化完成" CR
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
  D0@ D0 .R 8 .R SPACE ."  D0: " D0@ .
  D1@ D1 .R 8 .R SPACE ."  D1: " D1@ .
  D2@ D2 .R 8 .R SPACE ."  D2: " D2@ .
  D3@ D3 .R 8 .R SPACE ."  D3: " D3@ .
  D4@ D4 .R 8 .R SPACE ."  D4: " D4@ .
  D5@ D5 .R 8 .R SPACE ."  D5: " D5@ .
  D6@ D6 .R 8 .R SPACE ."  D6: " D6@ .
  D7@ D7 .R 8 .R SPACE ."  D7: " D7@ .
  A0@ A0 .R 8 .R SPACE ."  A0: " A0@ .
  A1@ A1 .R 8 .R SPACE ."  A1: " A1@ .
  A2@ A2 .R 8 .R SPACE ."  A2: " A2@ .
  A3@ A3 .R 8 .R SPACE ."  A3: " A3@ .
  A4@ A4 .R 8 .R SPACE ."  A4: " A4@ .
  A5@ A5 .R 8 .R SPACE ."  A5: " A5@ .
  A6@ A6 .R 8 .R SPACE ."  A6: " A6@ .
  A7@ A7 .R 8 .R SPACE ."  A7: " A7@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  SR@ SR .R 8 .R SPACE ."  SR: " SR@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Reset Initial SP
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

\ Reset Initial PC
: INT-RESET_PC-HANDLER ( -- )
  ." RESET_PC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET_PC-ENABLE ( -- )
  INT-RESET_PC INT-ENABLE
;

: INT-RESET_PC-DISABLE ( -- )
  INT-RESET_PC INT-DISABLE
;

\ VIA interrupt (level 1)
: INT-IRQ1-HANDLER ( -- )
  ." IRQ1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ1-ENABLE ( -- )
  INT-IRQ1 INT-ENABLE
;

: INT-IRQ1-DISABLE ( -- )
  INT-IRQ1 INT-DISABLE
;

\ SCC interrupt (level 2)
: INT-IRQ2-HANDLER ( -- )
  ." IRQ2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ2-ENABLE ( -- )
  INT-IRQ2 INT-ENABLE
;

: INT-IRQ2-DISABLE ( -- )
  INT-IRQ2 INT-DISABLE
;

\ ADB / VIA (level 3)
: INT-IRQ3-HANDLER ( -- )
  ." IRQ3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ3-ENABLE ( -- )
  INT-IRQ3 INT-ENABLE
;

: INT-IRQ3-DISABLE ( -- )
  INT-IRQ3 INT-DISABLE
;

\ ADB / VIA (level 4)
: INT-IRQ4-HANDLER ( -- )
  ." IRQ4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ4-ENABLE ( -- )
  INT-IRQ4 INT-ENABLE
;

: INT-IRQ4-DISABLE ( -- )
  INT-IRQ4 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  MACINTOSH_128K-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
