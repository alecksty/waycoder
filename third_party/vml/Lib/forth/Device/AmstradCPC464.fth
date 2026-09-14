\ Amstrad-CPC-464设备定义 - Forth文件
\ 生成自: Amstrad/CPC/Amstrad-CPC-464
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
\ CPU架构: Z80A
\ 位宽: 8位
\ 时钟频率: 4000000 Hz

\ =========================================
\ Amstrad-CPC-464设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Amstrad-CPC-464" ;
: MANUFACTURER  S" Amstrad" ;
: FAMILY        S" CPC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Z80A" ;
8 CONSTANT BITS
4000000 CONSTANT CLOCK-FREQ

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
0x14 CONSTANT IY  \ Index Y
0x16 CONSTANT SP  \ Stack Pointer
0x18 CONSTANT PC  \ Program Counter

\ 内存段定义
0x0000 CONSTANT LOWER_ROM-START
0x3FFF CONSTANT LOWER_ROM-END
16384 CONSTANT LOWER_ROM-SIZE  \ Lower ROM (AMSDOS / CP/M)
0x0000 CONSTANT RAM_BANK0-START
0x3FFF CONSTANT RAM_BANK0-END
16384 CONSTANT RAM_BANK0-SIZE  \ Lower RAM bank (switchable)
0x4000 CONSTANT RAM_MAIN-START
0xBFFF CONSTANT RAM_MAIN-END
32768 CONSTANT RAM_MAIN-SIZE  \ Main RAM (32KB)
0xC000 CONSTANT UPPER_ROM-START
0xFFFF CONSTANT UPPER_ROM-END
16384 CONSTANT UPPER_ROM-SIZE  \ Upper ROM (BASIC)

\ 外设定义
\ Gate Array - Custom ASIC (video/sound/RAM control)
0x7F00 CONSTANT GA-BASE
0x7F00 CONSTANT GA-GA_MR
0x7F01 CONSTANT GA-GA_IR
0x7F02 CONSTANT GA-GA_R1
0x7F03 CONSTANT GA-GA_R2
0x7F04 CONSTANT GA-GA_R3
0x7F05 CONSTANT GA-GA_R4
0x7F06 CONSTANT GA-GA_R5
0x7F07 CONSTANT GA-GA_R6
0x7F08 CONSTANT GA-GA_R7
\ CRT Controller 6845 - Video timing
0xBC00 CONSTANT CRTC-BASE
0xBC00 CONSTANT CRTC-CRTC_REG
0xBD00 CONSTANT CRTC-CRTC_DATA
0xBC01 CONSTANT CRTC-CRTC_H_TOTAL
0xBC02 CONSTANT CRTC-CRTC_H_DISP
0xBC03 CONSTANT CRTC-CRTC_HSYNC_POS
0xBC04 CONSTANT CRTC-CRTC_HSYNC_WIDTH
0xBC05 CONSTANT CRTC-CRTC_V_TOTAL
0xBC06 CONSTANT CRTC-CRTC_V_TOTAL_ADJ
0xBC07 CONSTANT CRTC-CRTC_V_DISP
0xBC08 CONSTANT CRTC-CRTC_VSYNC_POS
0xBC09 CONSTANT CRTC-CRTC_INTERLACE
0xBC0A CONSTANT CRTC-CRTC_CURSOR_START
0xBC0B CONSTANT CRTC-CRTC_CURSOR_END
0xBC0C CONSTANT CRTC-CRTC_SA_HI
0xBC0D CONSTANT CRTC-CRTC_SA_LO
0xBC0E CONSTANT CRTC-CRTC_CURSOR_HI
0xBC0F CONSTANT CRTC-CRTC_CURSOR_LO
\ AY-3-8912 Programmable Sound Generator
0xF400 CONSTANT PSG-BASE
0xF400 CONSTANT PSG-PSG_REG
0xF600 CONSTANT PSG-PSG_DATA
0xF400 CONSTANT PSG-FREQ_A_LO
0xF401 CONSTANT PSG-FREQ_A_HI
0xF402 CONSTANT PSG-FREQ_B_LO
0xF403 CONSTANT PSG-FREQ_B_HI
0xF404 CONSTANT PSG-FREQ_C_LO
0xF405 CONSTANT PSG-FREQ_C_HI
0xF406 CONSTANT PSG-NOISE_FREQ
0xF407 CONSTANT PSG-ENABLE
0xF408 CONSTANT PSG-VOL_A
0xF409 CONSTANT PSG-VOL_B
0xF40A CONSTANT PSG-VOL_C
0xF40B CONSTANT PSG-ENV_FREQ_LO
0xF40C CONSTANT PSG-ENV_FREQ_HI
0xF40D CONSTANT PSG-ENV_SHAPE
0xF40E CONSTANT PSG-PORT_A
0xF40F CONSTANT PSG-PORT_B
\ WD1772 Floppy Disk Controller (via expansion)
0xF800 CONSTANT FDC-BASE
0xF8E0 CONSTANT FDC-FDC_STATUS
0xF8E0 CONSTANT FDC-FDC_COMMAND
0xF8E1 CONSTANT FDC-FDC_TRACK
0xF8E2 CONSTANT FDC-FDC_SECTOR
0xF8E3 CONSTANT FDC-FDC_DATA
\ Centronics Parallel Printer Port
0xEE CONSTANT PRINTER-BASE
0xEE CONSTANT PRINTER-PRN_DATA
0xEF CONSTANT PRINTER-PRN_STROBE

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-on / Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt
2 CONSTANT INT-INT  \ Gate Array interrupt (50Hz vertical blank)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-CLK  \ Z80 Clock (4MHz)
4 CONSTANT PIN-A0_A15  \ Address Bus
5 CONSTANT PIN-D0_D7  \ Data Bus
6 CONSTANT PIN-MREQ  \ Memory Request
7 CONSTANT PIN-IORQ  \ I/O Request
8 CONSTANT PIN-RD  \ Read
9 CONSTANT PIN-WR  \ Write
10 CONSTANT PIN-INT  \ Interrupt Request
11 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
12 CONSTANT PIN-RESET  \ Reset

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
\ GA外设
: GA-GA_MR@ ( -- n ) GA-GA_MR C@ ;
: GA-GA_MR! ( n -- ) GA-GA_MR C! ;
: GA-GA_IR@ ( -- n ) GA-GA_IR C@ ;
: GA-GA_IR! ( n -- ) GA-GA_IR C! ;
: GA-GA_R1@ ( -- n ) GA-GA_R1 C@ ;
: GA-GA_R1! ( n -- ) GA-GA_R1 C! ;
: GA-GA_R2@ ( -- n ) GA-GA_R2 C@ ;
: GA-GA_R2! ( n -- ) GA-GA_R2 C! ;
: GA-GA_R3@ ( -- n ) GA-GA_R3 C@ ;
: GA-GA_R3! ( n -- ) GA-GA_R3 C! ;
: GA-GA_R4@ ( -- n ) GA-GA_R4 C@ ;
: GA-GA_R4! ( n -- ) GA-GA_R4 C! ;
: GA-GA_R5@ ( -- n ) GA-GA_R5 C@ ;
: GA-GA_R5! ( n -- ) GA-GA_R5 C! ;
: GA-GA_R6@ ( -- n ) GA-GA_R6 C@ ;
: GA-GA_R6! ( n -- ) GA-GA_R6 C! ;
: GA-GA_R7@ ( -- n ) GA-GA_R7 C@ ;
: GA-GA_R7! ( n -- ) GA-GA_R7 C! ;

\ CRTC外设
: CRTC-CRTC_REG@ ( -- n ) CRTC-CRTC_REG C@ ;
: CRTC-CRTC_REG! ( n -- ) CRTC-CRTC_REG C! ;
: CRTC-CRTC_DATA@ ( -- n ) CRTC-CRTC_DATA C@ ;
: CRTC-CRTC_DATA! ( n -- ) CRTC-CRTC_DATA C! ;
: CRTC-CRTC_H_TOTAL@ ( -- n ) CRTC-CRTC_H_TOTAL C@ ;
: CRTC-CRTC_H_TOTAL! ( n -- ) CRTC-CRTC_H_TOTAL C! ;
: CRTC-CRTC_H_DISP@ ( -- n ) CRTC-CRTC_H_DISP C@ ;
: CRTC-CRTC_H_DISP! ( n -- ) CRTC-CRTC_H_DISP C! ;
: CRTC-CRTC_HSYNC_POS@ ( -- n ) CRTC-CRTC_HSYNC_POS C@ ;
: CRTC-CRTC_HSYNC_POS! ( n -- ) CRTC-CRTC_HSYNC_POS C! ;
: CRTC-CRTC_HSYNC_WIDTH@ ( -- n ) CRTC-CRTC_HSYNC_WIDTH C@ ;
: CRTC-CRTC_HSYNC_WIDTH! ( n -- ) CRTC-CRTC_HSYNC_WIDTH C! ;
: CRTC-CRTC_V_TOTAL@ ( -- n ) CRTC-CRTC_V_TOTAL C@ ;
: CRTC-CRTC_V_TOTAL! ( n -- ) CRTC-CRTC_V_TOTAL C! ;
: CRTC-CRTC_V_TOTAL_ADJ@ ( -- n ) CRTC-CRTC_V_TOTAL_ADJ C@ ;
: CRTC-CRTC_V_TOTAL_ADJ! ( n -- ) CRTC-CRTC_V_TOTAL_ADJ C! ;
: CRTC-CRTC_V_DISP@ ( -- n ) CRTC-CRTC_V_DISP C@ ;
: CRTC-CRTC_V_DISP! ( n -- ) CRTC-CRTC_V_DISP C! ;
: CRTC-CRTC_VSYNC_POS@ ( -- n ) CRTC-CRTC_VSYNC_POS C@ ;
: CRTC-CRTC_VSYNC_POS! ( n -- ) CRTC-CRTC_VSYNC_POS C! ;
: CRTC-CRTC_INTERLACE@ ( -- n ) CRTC-CRTC_INTERLACE C@ ;
: CRTC-CRTC_INTERLACE! ( n -- ) CRTC-CRTC_INTERLACE C! ;
: CRTC-CRTC_CURSOR_START@ ( -- n ) CRTC-CRTC_CURSOR_START C@ ;
: CRTC-CRTC_CURSOR_START! ( n -- ) CRTC-CRTC_CURSOR_START C! ;
: CRTC-CRTC_CURSOR_END@ ( -- n ) CRTC-CRTC_CURSOR_END C@ ;
: CRTC-CRTC_CURSOR_END! ( n -- ) CRTC-CRTC_CURSOR_END C! ;
: CRTC-CRTC_SA_HI@ ( -- n ) CRTC-CRTC_SA_HI C@ ;
: CRTC-CRTC_SA_HI! ( n -- ) CRTC-CRTC_SA_HI C! ;
: CRTC-CRTC_SA_LO@ ( -- n ) CRTC-CRTC_SA_LO C@ ;
: CRTC-CRTC_SA_LO! ( n -- ) CRTC-CRTC_SA_LO C! ;
: CRTC-CRTC_CURSOR_HI@ ( -- n ) CRTC-CRTC_CURSOR_HI C@ ;
: CRTC-CRTC_CURSOR_HI! ( n -- ) CRTC-CRTC_CURSOR_HI C! ;
: CRTC-CRTC_CURSOR_LO@ ( -- n ) CRTC-CRTC_CURSOR_LO C@ ;
: CRTC-CRTC_CURSOR_LO! ( n -- ) CRTC-CRTC_CURSOR_LO C! ;

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

\ FDC外设
: FDC-FDC_STATUS@ ( -- n ) FDC-FDC_STATUS C@ ;
: FDC-FDC_STATUS! ( n -- ) FDC-FDC_STATUS C! ;
: FDC-FDC_COMMAND@ ( -- n ) FDC-FDC_COMMAND C@ ;
: FDC-FDC_COMMAND! ( n -- ) FDC-FDC_COMMAND C! ;
: FDC-FDC_TRACK@ ( -- n ) FDC-FDC_TRACK C@ ;
: FDC-FDC_TRACK! ( n -- ) FDC-FDC_TRACK C! ;
: FDC-FDC_SECTOR@ ( -- n ) FDC-FDC_SECTOR C@ ;
: FDC-FDC_SECTOR! ( n -- ) FDC-FDC_SECTOR C! ;
: FDC-FDC_DATA@ ( -- n ) FDC-FDC_DATA C@ ;
: FDC-FDC_DATA! ( n -- ) FDC-FDC_DATA C! ;

\ PRINTER外设
: PRINTER-PRN_DATA@ ( -- n ) PRINTER-PRN_DATA C@ ;
: PRINTER-PRN_DATA! ( n -- ) PRINTER-PRN_DATA C! ;
: PRINTER-PRN_STROBE@ ( -- n ) PRINTER-PRN_STROBE C@ ;
: PRINTER-PRN_STROBE! ( n -- ) PRINTER-PRN_STROBE C! ;

\ =========================================
\ 设备初始化
\ =========================================

: AMSTRAD_CPC_464-INIT ( -- )
  \ 初始化Amstrad-CPC-464设备
  ." 初始化Amstrad-CPC-464..." CR

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
  0 IY!  \ Index Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化GA
  0 GA-GA_MR!  \ GA_MR寄存器
  0 GA-GA_IR!  \ GA_IR寄存器
  0 GA-GA_R1!  \ GA_R1寄存器
  0 GA-GA_R2!  \ GA_R2寄存器
  0 GA-GA_R3!  \ GA_R3寄存器
  0 GA-GA_R4!  \ GA_R4寄存器
  0 GA-GA_R5!  \ GA_R5寄存器
  0 GA-GA_R6!  \ GA_R6寄存器
  0 GA-GA_R7!  \ GA_R7寄存器
  \ 初始化CRTC
  0 CRTC-CRTC_REG!  \ CRTC_REG寄存器
  0 CRTC-CRTC_DATA!  \ CRTC_DATA寄存器
  0 CRTC-CRTC_H_TOTAL!  \ CRTC_H_TOTAL寄存器
  0 CRTC-CRTC_H_DISP!  \ CRTC_H_DISP寄存器
  0 CRTC-CRTC_HSYNC_POS!  \ CRTC_HSYNC_POS寄存器
  0 CRTC-CRTC_HSYNC_WIDTH!  \ CRTC_HSYNC_WIDTH寄存器
  0 CRTC-CRTC_V_TOTAL!  \ CRTC_V_TOTAL寄存器
  0 CRTC-CRTC_V_TOTAL_ADJ!  \ CRTC_V_TOTAL_ADJ寄存器
  0 CRTC-CRTC_V_DISP!  \ CRTC_V_DISP寄存器
  0 CRTC-CRTC_VSYNC_POS!  \ CRTC_VSYNC_POS寄存器
  0 CRTC-CRTC_INTERLACE!  \ CRTC_INTERLACE寄存器
  0 CRTC-CRTC_CURSOR_START!  \ CRTC_CURSOR_START寄存器
  0 CRTC-CRTC_CURSOR_END!  \ CRTC_CURSOR_END寄存器
  0 CRTC-CRTC_SA_HI!  \ CRTC_SA_HI寄存器
  0 CRTC-CRTC_SA_LO!  \ CRTC_SA_LO寄存器
  0 CRTC-CRTC_CURSOR_HI!  \ CRTC_CURSOR_HI寄存器
  0 CRTC-CRTC_CURSOR_LO!  \ CRTC_CURSOR_LO寄存器
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
  \ 初始化FDC
  0 FDC-FDC_STATUS!  \ FDC_STATUS寄存器
  0 FDC-FDC_COMMAND!  \ FDC_COMMAND寄存器
  0 FDC-FDC_TRACK!  \ FDC_TRACK寄存器
  0 FDC-FDC_SECTOR!  \ FDC_SECTOR寄存器
  0 FDC-FDC_DATA!  \ FDC_DATA寄存器
  \ 初始化PRINTER
  0 PRINTER-PRN_DATA!  \ PRN_DATA寄存器
  0 PRINTER-PRN_STROBE!  \ PRN_STROBE寄存器

  ." Amstrad-CPC-464初始化完成" CR
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

\ Gate Array interrupt (50Hz vertical blank)
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
  AMSTRAD_CPC_464-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
