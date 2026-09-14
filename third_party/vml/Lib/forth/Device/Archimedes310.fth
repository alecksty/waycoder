\ Acorn-Archimedes-A310设备定义 - Forth文件
\ 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
\ CPU架构: ARM250
\ 位宽: 32位
\ 时钟频率: 26000000 Hz

\ =========================================
\ Acorn-Archimedes-A310设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Acorn-Archimedes-A310" ;
: MANUFACTURER  S" Acorn Computers" ;
: FAMILY        S" Archimedes" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM250" ;
32 CONSTANT BITS
26000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ General Purpose Register 0
0x04 CONSTANT R1  \ General Purpose Register 1
0x08 CONSTANT R2  \ General Purpose Register 2
0x0C CONSTANT R3  \ General Purpose Register 3
0x10 CONSTANT R4  \ General Purpose Register 4
0x14 CONSTANT R5  \ General Purpose Register 5
0x18 CONSTANT R6  \ General Purpose Register 6
0x1C CONSTANT R7  \ General Purpose Register 7
0x20 CONSTANT R8  \ General Purpose Register 8
0x24 CONSTANT R9  \ General Purpose Register 9
0x28 CONSTANT R10  \ General Purpose Register 10
0x2C CONSTANT R11  \ General Purpose Register 11 (fp)
0x30 CONSTANT R12  \ General Purpose Register 12
0x34 CONSTANT SP  \ Stack Pointer (R13)
0x38 CONSTANT LR  \ Link Register (R14)
0x3C CONSTANT PC  \ Program Counter (R15)
0x40 CONSTANT PSR  \ Processor Status Register
0 CONSTANT PSR-MODE  \ Mode bits (0-4)
5 CONSTANT PSR-T  \ Thumb state
6 CONSTANT PSR-F  \ FIQ disable
7 CONSTANT PSR-I  \ IRQ disable
28 CONSTANT PSR-V  \ Overflow
29 CONSTANT PSR-C  \ Carry
30 CONSTANT PSR-Z  \ Zero
31 CONSTANT PSR-N  \ Negative

\ 内存段定义
0x00000000 CONSTANT ROM-START
0x0007FFFF CONSTANT ROM-END
524288 CONSTANT ROM-SIZE  \ RISC OS ROM (512KB)
0x00080000 CONSTANT RAM-START
0x003FFFFF CONSTANT RAM-END
3932160 CONSTANT RAM-SIZE  \ Main RAM (up to 4MB)
0x00400000 CONSTANT VRAM-START
0x007FFFFF CONSTANT VRAM-END
4194304 CONSTANT VRAM-SIZE  \ Video RAM (4MB, VIDC)
0x03000000 CONSTANT IO-START
0x0301FFFF CONSTANT IO-END
131072 CONSTANT IO-SIZE  \ I/O controller (IOC)
0x03200000 CONSTANT MEMC-START
0x0320FFFF CONSTANT MEMC-END
4096 CONSTANT MEMC-SIZE  \ Memory Controller (MEMC)
0x03400000 CONSTANT VIDC-START
0x0340FFFF CONSTANT VIDC-END
4096 CONSTANT VIDC-SIZE  \ Video Controller (VIDC)
0x03300000 CONSTANT IOMD-START
0x0330FFFF CONSTANT IOMD-END
4096 CONSTANT IOMD-SIZE  \ I/O and Memory DMA

\ 外设定义
\ I/O Controller (IOC) - Interrupt/Keyboard/RTC
0x03000000 CONSTANT IOC-BASE
0x03000000 CONSTANT IOC-IOC_TIMER1
0x03000004 CONSTANT IOC-IOC_TIMER2
0x03000008 CONSTANT IOC-IOC_IOSEL
0x0300000C CONSTANT IOC-IOC_IRQST
0x03000010 CONSTANT IOC-IOC_IRQLATCH
0x03000014 CONSTANT IOC-IOC_FIQST
0x03000018 CONSTANT IOC-IOC_FIQEN
0x0300001C CONSTANT IOC-IOC_IRQEN
0x03000020 CONSTANT IOC-IOC_KBDDATA
0x03000024 CONSTANT IOC-IOC_KBDCR
0x03000028 CONSTANT IOC-IOC_RTCDR
0x0300002C CONSTANT IOC-IOC_RTCCR
0x03000030 CONSTANT IOC-IOC_PRST
0x03000034 CONSTANT IOC-IOC_PORTA
0x03000038 CONSTANT IOC-IOC_PORTB
0x0300003C CONSTANT IOC-IOC_PORTC
\ Memory Controller (MEMC1)
0x03200000 CONSTANT MEMC-BASE
0x03200000 CONSTANT MEMC-MEMC_PT
0x03200004 CONSTANT MEMC-MEMC_CTRL
0x03200008 CONSTANT MEMC-MEMC_DRAM
0x0320000C CONSTANT MEMC-MEMC_ERR
\ Video Controller - VIDC1
0x03400000 CONSTANT VIDC-BASE
0x03400000 CONSTANT VIDC-VIDC_PALETTE
0x03400004 CONSTANT VIDC-VIDC_STARTL
0x03400008 CONSTANT VIDC-VIDC_STARTH
0x0340000C CONSTANT VIDC-VIDC_CONFIG
0x03400010 CONSTANT VIDC-VIDC_HDISP
0x03400014 CONSTANT VIDC-VIDC_VDISP
0x03400018 CONSTANT VIDC-VIDC_HSYNC
0x0340001C CONSTANT VIDC-VIDC_VSYNC
0x03400020 CONSTANT VIDC-VIDC_BORDER
0x03400024 CONSTANT VIDC-VIDC_CURSOR
0x03400028 CONSTANT VIDC-VIDC_SOUND
\ Intel 82710 Floppy Disk Controller
0x03010000 CONSTANT FDC-BASE
0x03010000 CONSTANT FDC-FDC_STATUS
0x03010000 CONSTANT FDC-FDC_COMMAND
0x03010004 CONSTANT FDC-FDC_TRACK
0x03010008 CONSTANT FDC-FDC_SECTOR
0x0301000C CONSTANT FDC-FDC_DATA
\ Serial Port (via IOC)
0x03010010 CONSTANT SERIAL-BASE
0x03010010 CONSTANT SERIAL-SERIAL_TX
0x03010014 CONSTANT SERIAL-SERIAL_RX
0x03010018 CONSTANT SERIAL-SERIAL_CTRL

\ 中断向量定义
0 CONSTANT INT-RESET  \ Reset
1 CONSTANT INT-UND  \ Undefined instruction
2 CONSTANT INT-SWI  \ Software Interrupt (SWI/SVC)
3 CONSTANT INT-PABORT  \ Prefetch Abort
4 CONSTANT INT-DABORT  \ Data Abort
5 CONSTANT INT-ADDRESS  \ Address Exception
6 CONSTANT INT-IRQ  \ IRQ interrupt (IOC)
7 CONSTANT INT-FIQ  \ FIQ interrupt (VIDC)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-CLK  \ ARM clock (26MHz)
4 CONSTANT PIN-NRESET  \ Reset (active low)
5 CONSTANT PIN-NMREQ  \ Memory Request (active low)
6 CONSTANT PIN-NIORQ  \ I/O Request (active low)
7 CONSTANT PIN-NRW  \ Read/Write (0=write, 1=read)
8 CONSTANT PIN-MAS0  \ Master address bit 0
9 CONSTANT PIN-MAS1  \ Master address bit 1
10 CONSTANT PIN-MAS2  \ Master address bit 2
11 CONSTANT PIN-LOCK  \ Bus lock
12 CONSTANT PIN-NMREQ  \ Memory request (active low)
13 CONSTANT PIN-NWAIT  \ Wait state (active low)
14 CONSTANT PIN-NIRQLINE  \ IRQ line (active low)
15 CONSTANT PIN-NFIRQLINE  \ FIQ line (active low)
16 CONSTANT PIN-A1_A25  \ Address Bus (26-bit)
17 CONSTANT PIN-D0_D31  \ Data Bus (32-bit)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 L@ ;
: R0! ( n -- ) R0 L! ;

: R1@ ( -- n ) R1 L@ ;
: R1! ( n -- ) R1 L! ;

: R2@ ( -- n ) R2 L@ ;
: R2! ( n -- ) R2 L! ;

: R3@ ( -- n ) R3 L@ ;
: R3! ( n -- ) R3 L! ;

: R4@ ( -- n ) R4 L@ ;
: R4! ( n -- ) R4 L! ;

: R5@ ( -- n ) R5 L@ ;
: R5! ( n -- ) R5 L! ;

: R6@ ( -- n ) R6 L@ ;
: R6! ( n -- ) R6 L! ;

: R7@ ( -- n ) R7 L@ ;
: R7! ( n -- ) R7 L! ;

: R8@ ( -- n ) R8 L@ ;
: R8! ( n -- ) R8 L! ;

: R9@ ( -- n ) R9 L@ ;
: R9! ( n -- ) R9 L! ;

: R10@ ( -- n ) R10 L@ ;
: R10! ( n -- ) R10 L! ;

: R11@ ( -- n ) R11 L@ ;
: R11! ( n -- ) R11 L! ;

: R12@ ( -- n ) R12 L@ ;
: R12! ( n -- ) R12 L! ;

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

: PSR@ ( -- n ) PSR L@ ;
: PSR! ( n -- ) PSR L! ;
: PSR-MODE@ ( -- flag ) PSR@ 0 BIT@ ;
: PSR-MODE! ( flag -- ) PSR@ 0 BIT! PSR! ;
: PSR-MODE-SET ( -- ) TRUE PSR-MODE! ;
: PSR-MODE-CLR ( -- ) FALSE PSR-MODE! ;
: PSR-T@ ( -- flag ) PSR@ 5 BIT@ ;
: PSR-T! ( flag -- ) PSR@ 5 BIT! PSR! ;
: PSR-T-SET ( -- ) TRUE PSR-T! ;
: PSR-T-CLR ( -- ) FALSE PSR-T! ;
: PSR-F@ ( -- flag ) PSR@ 6 BIT@ ;
: PSR-F! ( flag -- ) PSR@ 6 BIT! PSR! ;
: PSR-F-SET ( -- ) TRUE PSR-F! ;
: PSR-F-CLR ( -- ) FALSE PSR-F! ;
: PSR-I@ ( -- flag ) PSR@ 7 BIT@ ;
: PSR-I! ( flag -- ) PSR@ 7 BIT! PSR! ;
: PSR-I-SET ( -- ) TRUE PSR-I! ;
: PSR-I-CLR ( -- ) FALSE PSR-I! ;
: PSR-V@ ( -- flag ) PSR@ 28 BIT@ ;
: PSR-V! ( flag -- ) PSR@ 28 BIT! PSR! ;
: PSR-V-SET ( -- ) TRUE PSR-V! ;
: PSR-V-CLR ( -- ) FALSE PSR-V! ;
: PSR-C@ ( -- flag ) PSR@ 29 BIT@ ;
: PSR-C! ( flag -- ) PSR@ 29 BIT! PSR! ;
: PSR-C-SET ( -- ) TRUE PSR-C! ;
: PSR-C-CLR ( -- ) FALSE PSR-C! ;
: PSR-Z@ ( -- flag ) PSR@ 30 BIT@ ;
: PSR-Z! ( flag -- ) PSR@ 30 BIT! PSR! ;
: PSR-Z-SET ( -- ) TRUE PSR-Z! ;
: PSR-Z-CLR ( -- ) FALSE PSR-Z! ;
: PSR-N@ ( -- flag ) PSR@ 31 BIT@ ;
: PSR-N! ( flag -- ) PSR@ 31 BIT! PSR! ;
: PSR-N-SET ( -- ) TRUE PSR-N! ;
: PSR-N-CLR ( -- ) FALSE PSR-N! ;

\ 外设访问
\ IOC外设
: IOC-IOC_TIMER1@ ( -- n ) IOC-IOC_TIMER1 L@ ;
: IOC-IOC_TIMER1! ( n -- ) IOC-IOC_TIMER1 L! ;
: IOC-IOC_TIMER2@ ( -- n ) IOC-IOC_TIMER2 L@ ;
: IOC-IOC_TIMER2! ( n -- ) IOC-IOC_TIMER2 L! ;
: IOC-IOC_IOSEL@ ( -- n ) IOC-IOC_IOSEL L@ ;
: IOC-IOC_IOSEL! ( n -- ) IOC-IOC_IOSEL L! ;
: IOC-IOC_IRQST@ ( -- n ) IOC-IOC_IRQST L@ ;
: IOC-IOC_IRQST! ( n -- ) IOC-IOC_IRQST L! ;
: IOC-IOC_IRQLATCH@ ( -- n ) IOC-IOC_IRQLATCH L@ ;
: IOC-IOC_IRQLATCH! ( n -- ) IOC-IOC_IRQLATCH L! ;
: IOC-IOC_FIQST@ ( -- n ) IOC-IOC_FIQST L@ ;
: IOC-IOC_FIQST! ( n -- ) IOC-IOC_FIQST L! ;
: IOC-IOC_FIQEN@ ( -- n ) IOC-IOC_FIQEN L@ ;
: IOC-IOC_FIQEN! ( n -- ) IOC-IOC_FIQEN L! ;
: IOC-IOC_IRQEN@ ( -- n ) IOC-IOC_IRQEN L@ ;
: IOC-IOC_IRQEN! ( n -- ) IOC-IOC_IRQEN L! ;
: IOC-IOC_KBDDATA@ ( -- n ) IOC-IOC_KBDDATA L@ ;
: IOC-IOC_KBDDATA! ( n -- ) IOC-IOC_KBDDATA L! ;
: IOC-IOC_KBDCR@ ( -- n ) IOC-IOC_KBDCR L@ ;
: IOC-IOC_KBDCR! ( n -- ) IOC-IOC_KBDCR L! ;
: IOC-IOC_RTCDR@ ( -- n ) IOC-IOC_RTCDR L@ ;
: IOC-IOC_RTCDR! ( n -- ) IOC-IOC_RTCDR L! ;
: IOC-IOC_RTCCR@ ( -- n ) IOC-IOC_RTCCR L@ ;
: IOC-IOC_RTCCR! ( n -- ) IOC-IOC_RTCCR L! ;
: IOC-IOC_PRST@ ( -- n ) IOC-IOC_PRST L@ ;
: IOC-IOC_PRST! ( n -- ) IOC-IOC_PRST L! ;
: IOC-IOC_PORTA@ ( -- n ) IOC-IOC_PORTA L@ ;
: IOC-IOC_PORTA! ( n -- ) IOC-IOC_PORTA L! ;
: IOC-IOC_PORTB@ ( -- n ) IOC-IOC_PORTB L@ ;
: IOC-IOC_PORTB! ( n -- ) IOC-IOC_PORTB L! ;
: IOC-IOC_PORTC@ ( -- n ) IOC-IOC_PORTC L@ ;
: IOC-IOC_PORTC! ( n -- ) IOC-IOC_PORTC L! ;

\ MEMC外设
: MEMC-MEMC_PT@ ( -- n ) MEMC-MEMC_PT L@ ;
: MEMC-MEMC_PT! ( n -- ) MEMC-MEMC_PT L! ;
: MEMC-MEMC_CTRL@ ( -- n ) MEMC-MEMC_CTRL L@ ;
: MEMC-MEMC_CTRL! ( n -- ) MEMC-MEMC_CTRL L! ;
: MEMC-MEMC_DRAM@ ( -- n ) MEMC-MEMC_DRAM L@ ;
: MEMC-MEMC_DRAM! ( n -- ) MEMC-MEMC_DRAM L! ;
: MEMC-MEMC_ERR@ ( -- n ) MEMC-MEMC_ERR L@ ;
: MEMC-MEMC_ERR! ( n -- ) MEMC-MEMC_ERR L! ;

\ VIDC外设
: VIDC-VIDC_PALETTE@ ( -- n ) VIDC-VIDC_PALETTE L@ ;
: VIDC-VIDC_PALETTE! ( n -- ) VIDC-VIDC_PALETTE L! ;
: VIDC-VIDC_STARTL@ ( -- n ) VIDC-VIDC_STARTL L@ ;
: VIDC-VIDC_STARTL! ( n -- ) VIDC-VIDC_STARTL L! ;
: VIDC-VIDC_STARTH@ ( -- n ) VIDC-VIDC_STARTH L@ ;
: VIDC-VIDC_STARTH! ( n -- ) VIDC-VIDC_STARTH L! ;
: VIDC-VIDC_CONFIG@ ( -- n ) VIDC-VIDC_CONFIG L@ ;
: VIDC-VIDC_CONFIG! ( n -- ) VIDC-VIDC_CONFIG L! ;
: VIDC-VIDC_HDISP@ ( -- n ) VIDC-VIDC_HDISP L@ ;
: VIDC-VIDC_HDISP! ( n -- ) VIDC-VIDC_HDISP L! ;
: VIDC-VIDC_VDISP@ ( -- n ) VIDC-VIDC_VDISP L@ ;
: VIDC-VIDC_VDISP! ( n -- ) VIDC-VIDC_VDISP L! ;
: VIDC-VIDC_HSYNC@ ( -- n ) VIDC-VIDC_HSYNC L@ ;
: VIDC-VIDC_HSYNC! ( n -- ) VIDC-VIDC_HSYNC L! ;
: VIDC-VIDC_VSYNC@ ( -- n ) VIDC-VIDC_VSYNC L@ ;
: VIDC-VIDC_VSYNC! ( n -- ) VIDC-VIDC_VSYNC L! ;
: VIDC-VIDC_BORDER@ ( -- n ) VIDC-VIDC_BORDER L@ ;
: VIDC-VIDC_BORDER! ( n -- ) VIDC-VIDC_BORDER L! ;
: VIDC-VIDC_CURSOR@ ( -- n ) VIDC-VIDC_CURSOR L@ ;
: VIDC-VIDC_CURSOR! ( n -- ) VIDC-VIDC_CURSOR L! ;
: VIDC-VIDC_SOUND@ ( -- n ) VIDC-VIDC_SOUND L@ ;
: VIDC-VIDC_SOUND! ( n -- ) VIDC-VIDC_SOUND L! ;

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

\ SERIAL外设
: SERIAL-SERIAL_TX@ ( -- n ) SERIAL-SERIAL_TX C@ ;
: SERIAL-SERIAL_TX! ( n -- ) SERIAL-SERIAL_TX C! ;
: SERIAL-SERIAL_RX@ ( -- n ) SERIAL-SERIAL_RX C@ ;
: SERIAL-SERIAL_RX! ( n -- ) SERIAL-SERIAL_RX C! ;
: SERIAL-SERIAL_CTRL@ ( -- n ) SERIAL-SERIAL_CTRL C@ ;
: SERIAL-SERIAL_CTRL! ( n -- ) SERIAL-SERIAL_CTRL C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ACORN_ARCHIMEDES_A310-INIT ( -- )
  \ 初始化Acorn-Archimedes-A310设备
  ." 初始化Acorn-Archimedes-A310..." CR

  \ 初始化寄存器
  0 R0!  \ General Purpose Register 0
  0 R1!  \ General Purpose Register 1
  0 R2!  \ General Purpose Register 2
  0 R3!  \ General Purpose Register 3
  0 R4!  \ General Purpose Register 4
  0 R5!  \ General Purpose Register 5
  0 R6!  \ General Purpose Register 6
  0 R7!  \ General Purpose Register 7
  0 R8!  \ General Purpose Register 8
  0 R9!  \ General Purpose Register 9
  0 R10!  \ General Purpose Register 10
  0 R11!  \ General Purpose Register 11 (fp)
  0 R12!  \ General Purpose Register 12
  0 SP!  \ Stack Pointer (R13)
  0 LR!  \ Link Register (R14)
  0 PC!  \ Program Counter (R15)
  0 PSR!  \ Processor Status Register

  \ 初始化外设
  \ 初始化IOC
  0 IOC-IOC_TIMER1!  \ IOC_TIMER1寄存器
  0 IOC-IOC_TIMER2!  \ IOC_TIMER2寄存器
  0 IOC-IOC_IOSEL!  \ IOC_IOSEL寄存器
  0 IOC-IOC_IRQST!  \ IOC_IRQST寄存器
  0 IOC-IOC_IRQLATCH!  \ IOC_IRQLATCH寄存器
  0 IOC-IOC_FIQST!  \ IOC_FIQST寄存器
  0 IOC-IOC_FIQEN!  \ IOC_FIQEN寄存器
  0 IOC-IOC_IRQEN!  \ IOC_IRQEN寄存器
  0 IOC-IOC_KBDDATA!  \ IOC_KBDDATA寄存器
  0 IOC-IOC_KBDCR!  \ IOC_KBDCR寄存器
  0 IOC-IOC_RTCDR!  \ IOC_RTCDR寄存器
  0 IOC-IOC_RTCCR!  \ IOC_RTCCR寄存器
  0 IOC-IOC_PRST!  \ IOC_PRST寄存器
  0 IOC-IOC_PORTA!  \ IOC_PORTA寄存器
  0 IOC-IOC_PORTB!  \ IOC_PORTB寄存器
  0 IOC-IOC_PORTC!  \ IOC_PORTC寄存器
  \ 初始化MEMC
  0 MEMC-MEMC_PT!  \ MEMC_PT寄存器
  0 MEMC-MEMC_CTRL!  \ MEMC_CTRL寄存器
  0 MEMC-MEMC_DRAM!  \ MEMC_DRAM寄存器
  0 MEMC-MEMC_ERR!  \ MEMC_ERR寄存器
  \ 初始化VIDC
  0 VIDC-VIDC_PALETTE!  \ VIDC_PALETTE寄存器
  0 VIDC-VIDC_STARTL!  \ VIDC_STARTL寄存器
  0 VIDC-VIDC_STARTH!  \ VIDC_STARTH寄存器
  0 VIDC-VIDC_CONFIG!  \ VIDC_CONFIG寄存器
  0 VIDC-VIDC_HDISP!  \ VIDC_HDISP寄存器
  0 VIDC-VIDC_VDISP!  \ VIDC_VDISP寄存器
  0 VIDC-VIDC_HSYNC!  \ VIDC_HSYNC寄存器
  0 VIDC-VIDC_VSYNC!  \ VIDC_VSYNC寄存器
  0 VIDC-VIDC_BORDER!  \ VIDC_BORDER寄存器
  0 VIDC-VIDC_CURSOR!  \ VIDC_CURSOR寄存器
  0 VIDC-VIDC_SOUND!  \ VIDC_SOUND寄存器
  \ 初始化FDC
  0 FDC-FDC_STATUS!  \ FDC_STATUS寄存器
  0 FDC-FDC_COMMAND!  \ FDC_COMMAND寄存器
  0 FDC-FDC_TRACK!  \ FDC_TRACK寄存器
  0 FDC-FDC_SECTOR!  \ FDC_SECTOR寄存器
  0 FDC-FDC_DATA!  \ FDC_DATA寄存器
  \ 初始化SERIAL
  0 SERIAL-SERIAL_TX!  \ SERIAL_TX寄存器
  0 SERIAL-SERIAL_RX!  \ SERIAL_RX寄存器
  0 SERIAL-SERIAL_CTRL!  \ SERIAL_CTRL寄存器

  ." Acorn-Archimedes-A310初始化完成" CR
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
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  R6@ R6 .R 8 .R SPACE ."  R6: " R6@ .
  R7@ R7 .R 8 .R SPACE ."  R7: " R7@ .
  R8@ R8 .R 8 .R SPACE ."  R8: " R8@ .
  R9@ R9 .R 8 .R SPACE ."  R9: " R9@ .
  R10@ R10 .R 8 .R SPACE ."  R10: " R10@ .
  R11@ R11 .R 8 .R SPACE ."  R11: " R11@ .
  R12@ R12 .R 8 .R SPACE ."  R12: " R12@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  LR@ LR .R 8 .R SPACE ."  LR: " LR@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  PSR@ PSR .R 8 .R SPACE ."  PSR: " PSR@ .
;

\ =========================================
\ 引脚操作
\ =========================================

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

\ Undefined instruction
: INT-UND-HANDLER ( -- )
  ." UND中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-UND-ENABLE ( -- )
  INT-UND INT-ENABLE
;

: INT-UND-DISABLE ( -- )
  INT-UND INT-DISABLE
;

\ Software Interrupt (SWI/SVC)
: INT-SWI-HANDLER ( -- )
  ." SWI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SWI-ENABLE ( -- )
  INT-SWI INT-ENABLE
;

: INT-SWI-DISABLE ( -- )
  INT-SWI INT-DISABLE
;

\ Prefetch Abort
: INT-PABORT-HANDLER ( -- )
  ." PABORT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PABORT-ENABLE ( -- )
  INT-PABORT INT-ENABLE
;

: INT-PABORT-DISABLE ( -- )
  INT-PABORT INT-DISABLE
;

\ Data Abort
: INT-DABORT-HANDLER ( -- )
  ." DABORT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DABORT-ENABLE ( -- )
  INT-DABORT INT-ENABLE
;

: INT-DABORT-DISABLE ( -- )
  INT-DABORT INT-DISABLE
;

\ Address Exception
: INT-ADDRESS-HANDLER ( -- )
  ." ADDRESS中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-ADDRESS-ENABLE ( -- )
  INT-ADDRESS INT-ENABLE
;

: INT-ADDRESS-DISABLE ( -- )
  INT-ADDRESS INT-DISABLE
;

\ IRQ interrupt (IOC)
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

\ FIQ interrupt (VIDC)
: INT-FIQ-HANDLER ( -- )
  ." FIQ中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-FIQ-ENABLE ( -- )
  INT-FIQ INT-ENABLE
;

: INT-FIQ-DISABLE ( -- )
  INT-FIQ INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ACORN_ARCHIMEDES_A310-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
