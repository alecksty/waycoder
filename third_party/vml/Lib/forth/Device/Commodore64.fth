\ Commodore-64设备定义 - Forth文件
\ 生成自: Commodore/C64/Commodore-64
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
\ CPU架构: MOS-6510
\ 位宽: 8位
\ 时钟频率: 1022727 Hz

\ =========================================
\ Commodore-64设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Commodore-64" ;
: MANUFACTURER  S" Commodore" ;
: FAMILY        S" C64" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS-6510" ;
8 CONSTANT BITS
1022727 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT X  \ X Index Register
0x02 CONSTANT Y  \ Y Index Register
0x03 CONSTANT SP  \ Stack Pointer
0x04 CONSTANT PC  \ Program Counter
0x06 CONSTANT P  \ Processor Status
0 CONSTANT P-C  \ Carry Flag
1 CONSTANT P-Z  \ Zero Flag
2 CONSTANT P-I  \ Interrupt Disable
3 CONSTANT P-D  \ Decimal Mode
4 CONSTANT P-B  \ Break Flag
5 CONSTANT P-U  \ Unused
6 CONSTANT P-V  \ Overflow Flag
7 CONSTANT P-N  \ Negative Flag
0x00 CONSTANT PORT  \ I/O Port (6510 only: DDR + data)

\ 内存段定义
0x0000 CONSTANT RAM-START
0xFFFF CONSTANT RAM-END
65536 CONSTANT RAM-SIZE  \ 64KB main RAM
0xA000 CONSTANT BASIC_ROM-START
0xBFFF CONSTANT BASIC_ROM-END
8192 CONSTANT BASIC_ROM-SIZE  \ BASIC interpreter ROM
0xE000 CONSTANT KERNAL_ROM-START
0xFFFF CONSTANT KERNAL_ROM-END
8192 CONSTANT KERNAL_ROM-SIZE  \ KERNAL operating system ROM
0xD000 CONSTANT CHAR_ROM-START
0xDFFF CONSTANT CHAR_ROM-END
4096 CONSTANT CHAR_ROM-SIZE  \ Character generator ROM
0xD000 CONSTANT IO_RAM-START
0xDFFF CONSTANT IO_RAM-END
4096 CONSTANT IO_RAM-SIZE  \ I/O + RAM window (switchable)

\ 外设定义
\ Video Interface Chip II - 6567/6569
0xD000 CONSTANT VICII-BASE
0xD000 CONSTANT VICII-SP0X
0xD001 CONSTANT VICII-SP0Y
0xD002 CONSTANT VICII-SP1X
0xD003 CONSTANT VICII-SP1Y
0xD004 CONSTANT VICII-SP2X
0xD005 CONSTANT VICII-SP2Y
0xD006 CONSTANT VICII-SP3X
0xD007 CONSTANT VICII-SP3Y
0xD008 CONSTANT VICII-SP4X
0xD009 CONSTANT VICII-SP4Y
0xD00A CONSTANT VICII-SP5X
0xD00B CONSTANT VICII-SP5Y
0xD00C CONSTANT VICII-SP6X
0xD00D CONSTANT VICII-SP6Y
0xD00E CONSTANT VICII-SP7X
0xD00F CONSTANT VICII-SP7Y
0xD010 CONSTANT VICII-MSIGX
0xD011 CONSTANT VICII-SCROLY
0xD016 CONSTANT VICII-SCROLX
0xD012 CONSTANT VICII-YPSTOP
0xD013 CONSTANT VICII-LPX
0xD014 CONSTANT VICII-LPY
0xD015 CONSTANT VICII-SPENA
0xD017 CONSTANT VICII-CSPMC
0xD018 CONSTANT VICII-MM0
0xD016 CONSTANT VICII-VM01
0xD018 CONSTANT VICII-VICBAS
0xD019 CONSTANT VICII-IRQMASK
0xD01A CONSTANT VICII-IRQST
0xD01B CONSTANT VICII-SPBGPR
0xD01C CONSTANT VICII-SPMC
0xD025 CONSTANT VICII-SP1C
0xD026 CONSTANT VICII-SP2C
0xD027 CONSTANT VICII-SPBC
0xD028 CONSTANT VICII-SP1C0
0xD029 CONSTANT VICII-SP2C0
0xD02A CONSTANT VICII-SP3C0
0xD02B CONSTANT VICII-SP4C0
0xD02C CONSTANT VICII-SP5C0
0xD02D CONSTANT VICII-SP6C0
0xD02E CONSTANT VICII-SP7C0
0xD01D CONSTANT VICII-REG_FD
0xD021 CONSTANT VICII-BGCOL0
0xD022 CONSTANT VICII-BGCOL1
0xD023 CONSTANT VICII-BGCOL2
0xD024 CONSTANT VICII-BGCOL3
\ Sound Interface Device 6581/8580
0xD400 CONSTANT SID-BASE
0xD400 CONSTANT SID-FREQ1LO
0xD401 CONSTANT SID-FREQ1HI
0xD402 CONSTANT SID-PW1LO
0xD403 CONSTANT SID-PW1HI
0xD404 CONSTANT SID-CR1
0xD405 CONSTANT SID-AD1
0xD406 CONSTANT SID-SR1
0xD407 CONSTANT SID-FREQ2LO
0xD408 CONSTANT SID-FREQ2HI
0xD409 CONSTANT SID-PW2LO
0xD40A CONSTANT SID-PW2HI
0xD40B CONSTANT SID-CR2
0xD40C CONSTANT SID-AD2
0xD40D CONSTANT SID-SR2
0xD40E CONSTANT SID-FREQ3LO
0xD40F CONSTANT SID-FREQ3HI
0xD410 CONSTANT SID-PW3LO
0xD411 CONSTANT SID-PW3HI
0xD412 CONSTANT SID-CR3
0xD413 CONSTANT SID-AD3
0xD414 CONSTANT SID-SR3
0xD415 CONSTANT SID-FCH
0xD416 CONSTANT SID-FCL
0xD417 CONSTANT SID-RES_FLT
0xD418 CONSTANT SID-VOLUME
0xD419 CONSTANT SID-POTX
0xD41A CONSTANT SID-POTY
0xD41B CONSTANT SID-OSC3
0xD41C CONSTANT SID-ENV3
\ Complex Interface Adapter 1 - Keyboard/Serial
0xDC00 CONSTANT CIA1-BASE
0xDC00 CONSTANT CIA1-PRA
0xDC01 CONSTANT CIA1-PRB
0xDC02 CONSTANT CIA1-DDRA
0xDC03 CONSTANT CIA1-DDRB
0xDC04 CONSTANT CIA1-TA_LO
0xDC05 CONSTANT CIA1-TA_HI
0xDC06 CONSTANT CIA1-TB_LO
0xDC07 CONSTANT CIA1-TB_HI
0xDC08 CONSTANT CIA1-TOD_TENTH
0xDC09 CONSTANT CIA1-TOD_SEC
0xDC0A CONSTANT CIA1-TOD_MIN
0xDC0B CONSTANT CIA1-TOD_HR
0xDC0C CONSTANT CIA1-SDR
0xDC0D CONSTANT CIA1-ICR
0xDC0E CONSTANT CIA1-CRA
0xDC0F CONSTANT CIA1-CRB
\ Complex Interface Adapter 2 - Serial/Bus
0xDD00 CONSTANT CIA2-BASE
0xDD00 CONSTANT CIA2-PRA
0xDD01 CONSTANT CIA2-PRB
0xDD02 CONSTANT CIA2-DDRA
0xDD03 CONSTANT CIA2-DDRB
0xDD04 CONSTANT CIA2-TA_LO
0xDD05 CONSTANT CIA2-TA_HI
0xDD06 CONSTANT CIA2-TB_LO
0xDD07 CONSTANT CIA2-TB_HI
0xDD08 CONSTANT CIA2-TOD_TENTH
0xDD09 CONSTANT CIA2-TOD_SEC
0xDD0A CONSTANT CIA2-TOD_MIN
0xDD0B CONSTANT CIA2-TOD_HR
0xDD0C CONSTANT CIA2-SDR
0xDD0D CONSTANT CIA2-ICR
0xDD0E CONSTANT CIA2-CRA
0xDD0F CONSTANT CIA2-CRB
\ Color RAM (4-bit per char cell)
0xD800 CONSTANT COLORRAM-BASE
0xD800 CONSTANT COLORRAM-COLOR
\ IEC Serial Bus (via CIA1)
0xDC00 CONSTANT IEC-BASE
0xDC00 CONSTANT IEC-IEC_DATA
0xDC01 CONSTANT IEC-IEC_CLOCK

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-on / Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt
2 CONSTANT INT-IRQ  \ IRQ (VIC raster / CIA timer)

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-RESET  \ System Reset
4 CONSTANT PIN-CLK  \ System Clock (~1MHz)
5 CONSTANT PIN-DOTCLK  \ VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
6 CONSTANT PIN-AEC  \ Address Enable Control (VIC steals cycles)
7 CONSTANT PIN-BA  \ Bus Available (from VIC)
8 CONSTANT PIN-IRQ  \ Interrupt Request
9 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
10 CONSTANT PIN-RWB  \ Read/Write
11 CONSTANT PIN-A0_A15  \ Address Bus
12 CONSTANT PIN-D0_D7  \ Data Bus

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

: PORT@ ( -- n ) PORT C@ ;
: PORT! ( n -- ) PORT C! ;

\ 外设访问
\ VICII外设
: VICII-SP0X@ ( -- n ) VICII-SP0X C@ ;
: VICII-SP0X! ( n -- ) VICII-SP0X C! ;
: VICII-SP0Y@ ( -- n ) VICII-SP0Y C@ ;
: VICII-SP0Y! ( n -- ) VICII-SP0Y C! ;
: VICII-SP1X@ ( -- n ) VICII-SP1X C@ ;
: VICII-SP1X! ( n -- ) VICII-SP1X C! ;
: VICII-SP1Y@ ( -- n ) VICII-SP1Y C@ ;
: VICII-SP1Y! ( n -- ) VICII-SP1Y C! ;
: VICII-SP2X@ ( -- n ) VICII-SP2X C@ ;
: VICII-SP2X! ( n -- ) VICII-SP2X C! ;
: VICII-SP2Y@ ( -- n ) VICII-SP2Y C@ ;
: VICII-SP2Y! ( n -- ) VICII-SP2Y C! ;
: VICII-SP3X@ ( -- n ) VICII-SP3X C@ ;
: VICII-SP3X! ( n -- ) VICII-SP3X C! ;
: VICII-SP3Y@ ( -- n ) VICII-SP3Y C@ ;
: VICII-SP3Y! ( n -- ) VICII-SP3Y C! ;
: VICII-SP4X@ ( -- n ) VICII-SP4X C@ ;
: VICII-SP4X! ( n -- ) VICII-SP4X C! ;
: VICII-SP4Y@ ( -- n ) VICII-SP4Y C@ ;
: VICII-SP4Y! ( n -- ) VICII-SP4Y C! ;
: VICII-SP5X@ ( -- n ) VICII-SP5X C@ ;
: VICII-SP5X! ( n -- ) VICII-SP5X C! ;
: VICII-SP5Y@ ( -- n ) VICII-SP5Y C@ ;
: VICII-SP5Y! ( n -- ) VICII-SP5Y C! ;
: VICII-SP6X@ ( -- n ) VICII-SP6X C@ ;
: VICII-SP6X! ( n -- ) VICII-SP6X C! ;
: VICII-SP6Y@ ( -- n ) VICII-SP6Y C@ ;
: VICII-SP6Y! ( n -- ) VICII-SP6Y C! ;
: VICII-SP7X@ ( -- n ) VICII-SP7X C@ ;
: VICII-SP7X! ( n -- ) VICII-SP7X C! ;
: VICII-SP7Y@ ( -- n ) VICII-SP7Y C@ ;
: VICII-SP7Y! ( n -- ) VICII-SP7Y C! ;
: VICII-MSIGX@ ( -- n ) VICII-MSIGX C@ ;
: VICII-MSIGX! ( n -- ) VICII-MSIGX C! ;
: VICII-SCROLY@ ( -- n ) VICII-SCROLY C@ ;
: VICII-SCROLY! ( n -- ) VICII-SCROLY C! ;
: VICII-SCROLX@ ( -- n ) VICII-SCROLX C@ ;
: VICII-SCROLX! ( n -- ) VICII-SCROLX C! ;
: VICII-YPSTOP@ ( -- n ) VICII-YPSTOP C@ ;
: VICII-YPSTOP! ( n -- ) VICII-YPSTOP C! ;
: VICII-LPX@ ( -- n ) VICII-LPX C@ ;
: VICII-LPX! ( n -- ) VICII-LPX C! ;
: VICII-LPY@ ( -- n ) VICII-LPY C@ ;
: VICII-LPY! ( n -- ) VICII-LPY C! ;
: VICII-SPENA@ ( -- n ) VICII-SPENA C@ ;
: VICII-SPENA! ( n -- ) VICII-SPENA C! ;
: VICII-CSPMC@ ( -- n ) VICII-CSPMC C@ ;
: VICII-CSPMC! ( n -- ) VICII-CSPMC C! ;
: VICII-MM0@ ( -- n ) VICII-MM0 C@ ;
: VICII-MM0! ( n -- ) VICII-MM0 C! ;
: VICII-VM01@ ( -- n ) VICII-VM01 C@ ;
: VICII-VM01! ( n -- ) VICII-VM01 C! ;
: VICII-VICBAS@ ( -- n ) VICII-VICBAS C@ ;
: VICII-VICBAS! ( n -- ) VICII-VICBAS C! ;
: VICII-IRQMASK@ ( -- n ) VICII-IRQMASK C@ ;
: VICII-IRQMASK! ( n -- ) VICII-IRQMASK C! ;
: VICII-IRQST@ ( -- n ) VICII-IRQST C@ ;
: VICII-IRQST! ( n -- ) VICII-IRQST C! ;
: VICII-SPBGPR@ ( -- n ) VICII-SPBGPR C@ ;
: VICII-SPBGPR! ( n -- ) VICII-SPBGPR C! ;
: VICII-SPMC@ ( -- n ) VICII-SPMC C@ ;
: VICII-SPMC! ( n -- ) VICII-SPMC C! ;
: VICII-SP1C@ ( -- n ) VICII-SP1C C@ ;
: VICII-SP1C! ( n -- ) VICII-SP1C C! ;
: VICII-SP2C@ ( -- n ) VICII-SP2C C@ ;
: VICII-SP2C! ( n -- ) VICII-SP2C C! ;
: VICII-SPBC@ ( -- n ) VICII-SPBC C@ ;
: VICII-SPBC! ( n -- ) VICII-SPBC C! ;
: VICII-SP1C0@ ( -- n ) VICII-SP1C0 C@ ;
: VICII-SP1C0! ( n -- ) VICII-SP1C0 C! ;
: VICII-SP2C0@ ( -- n ) VICII-SP2C0 C@ ;
: VICII-SP2C0! ( n -- ) VICII-SP2C0 C! ;
: VICII-SP3C0@ ( -- n ) VICII-SP3C0 C@ ;
: VICII-SP3C0! ( n -- ) VICII-SP3C0 C! ;
: VICII-SP4C0@ ( -- n ) VICII-SP4C0 C@ ;
: VICII-SP4C0! ( n -- ) VICII-SP4C0 C! ;
: VICII-SP5C0@ ( -- n ) VICII-SP5C0 C@ ;
: VICII-SP5C0! ( n -- ) VICII-SP5C0 C! ;
: VICII-SP6C0@ ( -- n ) VICII-SP6C0 C@ ;
: VICII-SP6C0! ( n -- ) VICII-SP6C0 C! ;
: VICII-SP7C0@ ( -- n ) VICII-SP7C0 C@ ;
: VICII-SP7C0! ( n -- ) VICII-SP7C0 C! ;
: VICII-REG_FD@ ( -- n ) VICII-REG_FD C@ ;
: VICII-REG_FD! ( n -- ) VICII-REG_FD C! ;
: VICII-BGCOL0@ ( -- n ) VICII-BGCOL0 C@ ;
: VICII-BGCOL0! ( n -- ) VICII-BGCOL0 C! ;
: VICII-BGCOL1@ ( -- n ) VICII-BGCOL1 C@ ;
: VICII-BGCOL1! ( n -- ) VICII-BGCOL1 C! ;
: VICII-BGCOL2@ ( -- n ) VICII-BGCOL2 C@ ;
: VICII-BGCOL2! ( n -- ) VICII-BGCOL2 C! ;
: VICII-BGCOL3@ ( -- n ) VICII-BGCOL3 C@ ;
: VICII-BGCOL3! ( n -- ) VICII-BGCOL3 C! ;

\ SID外设
: SID-FREQ1LO@ ( -- n ) SID-FREQ1LO C@ ;
: SID-FREQ1LO! ( n -- ) SID-FREQ1LO C! ;
: SID-FREQ1HI@ ( -- n ) SID-FREQ1HI C@ ;
: SID-FREQ1HI! ( n -- ) SID-FREQ1HI C! ;
: SID-PW1LO@ ( -- n ) SID-PW1LO C@ ;
: SID-PW1LO! ( n -- ) SID-PW1LO C! ;
: SID-PW1HI@ ( -- n ) SID-PW1HI C@ ;
: SID-PW1HI! ( n -- ) SID-PW1HI C! ;
: SID-CR1@ ( -- n ) SID-CR1 C@ ;
: SID-CR1! ( n -- ) SID-CR1 C! ;
: SID-AD1@ ( -- n ) SID-AD1 C@ ;
: SID-AD1! ( n -- ) SID-AD1 C! ;
: SID-SR1@ ( -- n ) SID-SR1 C@ ;
: SID-SR1! ( n -- ) SID-SR1 C! ;
: SID-FREQ2LO@ ( -- n ) SID-FREQ2LO C@ ;
: SID-FREQ2LO! ( n -- ) SID-FREQ2LO C! ;
: SID-FREQ2HI@ ( -- n ) SID-FREQ2HI C@ ;
: SID-FREQ2HI! ( n -- ) SID-FREQ2HI C! ;
: SID-PW2LO@ ( -- n ) SID-PW2LO C@ ;
: SID-PW2LO! ( n -- ) SID-PW2LO C! ;
: SID-PW2HI@ ( -- n ) SID-PW2HI C@ ;
: SID-PW2HI! ( n -- ) SID-PW2HI C! ;
: SID-CR2@ ( -- n ) SID-CR2 C@ ;
: SID-CR2! ( n -- ) SID-CR2 C! ;
: SID-AD2@ ( -- n ) SID-AD2 C@ ;
: SID-AD2! ( n -- ) SID-AD2 C! ;
: SID-SR2@ ( -- n ) SID-SR2 C@ ;
: SID-SR2! ( n -- ) SID-SR2 C! ;
: SID-FREQ3LO@ ( -- n ) SID-FREQ3LO C@ ;
: SID-FREQ3LO! ( n -- ) SID-FREQ3LO C! ;
: SID-FREQ3HI@ ( -- n ) SID-FREQ3HI C@ ;
: SID-FREQ3HI! ( n -- ) SID-FREQ3HI C! ;
: SID-PW3LO@ ( -- n ) SID-PW3LO C@ ;
: SID-PW3LO! ( n -- ) SID-PW3LO C! ;
: SID-PW3HI@ ( -- n ) SID-PW3HI C@ ;
: SID-PW3HI! ( n -- ) SID-PW3HI C! ;
: SID-CR3@ ( -- n ) SID-CR3 C@ ;
: SID-CR3! ( n -- ) SID-CR3 C! ;
: SID-AD3@ ( -- n ) SID-AD3 C@ ;
: SID-AD3! ( n -- ) SID-AD3 C! ;
: SID-SR3@ ( -- n ) SID-SR3 C@ ;
: SID-SR3! ( n -- ) SID-SR3 C! ;
: SID-FCH@ ( -- n ) SID-FCH C@ ;
: SID-FCH! ( n -- ) SID-FCH C! ;
: SID-FCL@ ( -- n ) SID-FCL C@ ;
: SID-FCL! ( n -- ) SID-FCL C! ;
: SID-RES_FLT@ ( -- n ) SID-RES_FLT C@ ;
: SID-RES_FLT! ( n -- ) SID-RES_FLT C! ;
: SID-VOLUME@ ( -- n ) SID-VOLUME C@ ;
: SID-VOLUME! ( n -- ) SID-VOLUME C! ;
: SID-POTX@ ( -- n ) SID-POTX C@ ;
: SID-POTX! ( n -- ) SID-POTX C! ;
: SID-POTY@ ( -- n ) SID-POTY C@ ;
: SID-POTY! ( n -- ) SID-POTY C! ;
: SID-OSC3@ ( -- n ) SID-OSC3 C@ ;
: SID-OSC3! ( n -- ) SID-OSC3 C! ;
: SID-ENV3@ ( -- n ) SID-ENV3 C@ ;
: SID-ENV3! ( n -- ) SID-ENV3 C! ;

\ CIA1外设
: CIA1-PRA@ ( -- n ) CIA1-PRA C@ ;
: CIA1-PRA! ( n -- ) CIA1-PRA C! ;
: CIA1-PRB@ ( -- n ) CIA1-PRB C@ ;
: CIA1-PRB! ( n -- ) CIA1-PRB C! ;
: CIA1-DDRA@ ( -- n ) CIA1-DDRA C@ ;
: CIA1-DDRA! ( n -- ) CIA1-DDRA C! ;
: CIA1-DDRB@ ( -- n ) CIA1-DDRB C@ ;
: CIA1-DDRB! ( n -- ) CIA1-DDRB C! ;
: CIA1-TA_LO@ ( -- n ) CIA1-TA_LO C@ ;
: CIA1-TA_LO! ( n -- ) CIA1-TA_LO C! ;
: CIA1-TA_HI@ ( -- n ) CIA1-TA_HI C@ ;
: CIA1-TA_HI! ( n -- ) CIA1-TA_HI C! ;
: CIA1-TB_LO@ ( -- n ) CIA1-TB_LO C@ ;
: CIA1-TB_LO! ( n -- ) CIA1-TB_LO C! ;
: CIA1-TB_HI@ ( -- n ) CIA1-TB_HI C@ ;
: CIA1-TB_HI! ( n -- ) CIA1-TB_HI C! ;
: CIA1-TOD_TENTH@ ( -- n ) CIA1-TOD_TENTH C@ ;
: CIA1-TOD_TENTH! ( n -- ) CIA1-TOD_TENTH C! ;
: CIA1-TOD_SEC@ ( -- n ) CIA1-TOD_SEC C@ ;
: CIA1-TOD_SEC! ( n -- ) CIA1-TOD_SEC C! ;
: CIA1-TOD_MIN@ ( -- n ) CIA1-TOD_MIN C@ ;
: CIA1-TOD_MIN! ( n -- ) CIA1-TOD_MIN C! ;
: CIA1-TOD_HR@ ( -- n ) CIA1-TOD_HR C@ ;
: CIA1-TOD_HR! ( n -- ) CIA1-TOD_HR C! ;
: CIA1-SDR@ ( -- n ) CIA1-SDR C@ ;
: CIA1-SDR! ( n -- ) CIA1-SDR C! ;
: CIA1-ICR@ ( -- n ) CIA1-ICR C@ ;
: CIA1-ICR! ( n -- ) CIA1-ICR C! ;
: CIA1-CRA@ ( -- n ) CIA1-CRA C@ ;
: CIA1-CRA! ( n -- ) CIA1-CRA C! ;
: CIA1-CRB@ ( -- n ) CIA1-CRB C@ ;
: CIA1-CRB! ( n -- ) CIA1-CRB C! ;

\ CIA2外设
: CIA2-PRA@ ( -- n ) CIA2-PRA C@ ;
: CIA2-PRA! ( n -- ) CIA2-PRA C! ;
: CIA2-PRB@ ( -- n ) CIA2-PRB C@ ;
: CIA2-PRB! ( n -- ) CIA2-PRB C! ;
: CIA2-DDRA@ ( -- n ) CIA2-DDRA C@ ;
: CIA2-DDRA! ( n -- ) CIA2-DDRA C! ;
: CIA2-DDRB@ ( -- n ) CIA2-DDRB C@ ;
: CIA2-DDRB! ( n -- ) CIA2-DDRB C! ;
: CIA2-TA_LO@ ( -- n ) CIA2-TA_LO C@ ;
: CIA2-TA_LO! ( n -- ) CIA2-TA_LO C! ;
: CIA2-TA_HI@ ( -- n ) CIA2-TA_HI C@ ;
: CIA2-TA_HI! ( n -- ) CIA2-TA_HI C! ;
: CIA2-TB_LO@ ( -- n ) CIA2-TB_LO C@ ;
: CIA2-TB_LO! ( n -- ) CIA2-TB_LO C! ;
: CIA2-TB_HI@ ( -- n ) CIA2-TB_HI C@ ;
: CIA2-TB_HI! ( n -- ) CIA2-TB_HI C! ;
: CIA2-TOD_TENTH@ ( -- n ) CIA2-TOD_TENTH C@ ;
: CIA2-TOD_TENTH! ( n -- ) CIA2-TOD_TENTH C! ;
: CIA2-TOD_SEC@ ( -- n ) CIA2-TOD_SEC C@ ;
: CIA2-TOD_SEC! ( n -- ) CIA2-TOD_SEC C! ;
: CIA2-TOD_MIN@ ( -- n ) CIA2-TOD_MIN C@ ;
: CIA2-TOD_MIN! ( n -- ) CIA2-TOD_MIN C! ;
: CIA2-TOD_HR@ ( -- n ) CIA2-TOD_HR C@ ;
: CIA2-TOD_HR! ( n -- ) CIA2-TOD_HR C! ;
: CIA2-SDR@ ( -- n ) CIA2-SDR C@ ;
: CIA2-SDR! ( n -- ) CIA2-SDR C! ;
: CIA2-ICR@ ( -- n ) CIA2-ICR C@ ;
: CIA2-ICR! ( n -- ) CIA2-ICR C! ;
: CIA2-CRA@ ( -- n ) CIA2-CRA C@ ;
: CIA2-CRA! ( n -- ) CIA2-CRA C! ;
: CIA2-CRB@ ( -- n ) CIA2-CRB C@ ;
: CIA2-CRB! ( n -- ) CIA2-CRB C! ;

\ COLORRAM外设
: COLORRAM-COLOR@ ( -- n ) COLORRAM-COLOR C@ ;
: COLORRAM-COLOR! ( n -- ) COLORRAM-COLOR C! ;

\ IEC外设
: IEC-IEC_DATA@ ( -- n ) IEC-IEC_DATA C@ ;
: IEC-IEC_DATA! ( n -- ) IEC-IEC_DATA C! ;
: IEC-IEC_CLOCK@ ( -- n ) IEC-IEC_CLOCK C@ ;
: IEC-IEC_CLOCK! ( n -- ) IEC-IEC_CLOCK C! ;

\ =========================================
\ 设备初始化
\ =========================================

: COMMODORE_64-INIT ( -- )
  \ 初始化Commodore-64设备
  ." 初始化Commodore-64..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ X Index Register
  0 Y!  \ Y Index Register
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 P!  \ Processor Status
  0 PORT!  \ I/O Port (6510 only: DDR + data)

  \ 初始化外设
  \ 初始化VICII
  0 VICII-SP0X!  \ SP0X寄存器
  0 VICII-SP0Y!  \ SP0Y寄存器
  0 VICII-SP1X!  \ SP1X寄存器
  0 VICII-SP1Y!  \ SP1Y寄存器
  0 VICII-SP2X!  \ SP2X寄存器
  0 VICII-SP2Y!  \ SP2Y寄存器
  0 VICII-SP3X!  \ SP3X寄存器
  0 VICII-SP3Y!  \ SP3Y寄存器
  0 VICII-SP4X!  \ SP4X寄存器
  0 VICII-SP4Y!  \ SP4Y寄存器
  0 VICII-SP5X!  \ SP5X寄存器
  0 VICII-SP5Y!  \ SP5Y寄存器
  0 VICII-SP6X!  \ SP6X寄存器
  0 VICII-SP6Y!  \ SP6Y寄存器
  0 VICII-SP7X!  \ SP7X寄存器
  0 VICII-SP7Y!  \ SP7Y寄存器
  0 VICII-MSIGX!  \ MSIGX寄存器
  0 VICII-SCROLY!  \ SCROLY寄存器
  0 VICII-SCROLX!  \ SCROLX寄存器
  0 VICII-YPSTOP!  \ YPSTOP寄存器
  0 VICII-LPX!  \ LPX寄存器
  0 VICII-LPY!  \ LPY寄存器
  0 VICII-SPENA!  \ SPENA寄存器
  0 VICII-CSPMC!  \ CSPMC寄存器
  0 VICII-MM0!  \ MM0寄存器
  0 VICII-VM01!  \ VM01寄存器
  0 VICII-VICBAS!  \ VICBAS寄存器
  0 VICII-IRQMASK!  \ IRQMASK寄存器
  0 VICII-IRQST!  \ IRQST寄存器
  0 VICII-SPBGPR!  \ SPBGPR寄存器
  0 VICII-SPMC!  \ SPMC寄存器
  0 VICII-SP1C!  \ SP1C寄存器
  0 VICII-SP2C!  \ SP2C寄存器
  0 VICII-SPBC!  \ SPBC寄存器
  0 VICII-SP1C0!  \ SP1C0寄存器
  0 VICII-SP2C0!  \ SP2C0寄存器
  0 VICII-SP3C0!  \ SP3C0寄存器
  0 VICII-SP4C0!  \ SP4C0寄存器
  0 VICII-SP5C0!  \ SP5C0寄存器
  0 VICII-SP6C0!  \ SP6C0寄存器
  0 VICII-SP7C0!  \ SP7C0寄存器
  0 VICII-REG_FD!  \ REG_FD寄存器
  0 VICII-BGCOL0!  \ BGCOL0寄存器
  0 VICII-BGCOL1!  \ BGCOL1寄存器
  0 VICII-BGCOL2!  \ BGCOL2寄存器
  0 VICII-BGCOL3!  \ BGCOL3寄存器
  \ 初始化SID
  0 SID-FREQ1LO!  \ FREQ1LO寄存器
  0 SID-FREQ1HI!  \ FREQ1HI寄存器
  0 SID-PW1LO!  \ PW1LO寄存器
  0 SID-PW1HI!  \ PW1HI寄存器
  0 SID-CR1!  \ CR1寄存器
  0 SID-AD1!  \ AD1寄存器
  0 SID-SR1!  \ SR1寄存器
  0 SID-FREQ2LO!  \ FREQ2LO寄存器
  0 SID-FREQ2HI!  \ FREQ2HI寄存器
  0 SID-PW2LO!  \ PW2LO寄存器
  0 SID-PW2HI!  \ PW2HI寄存器
  0 SID-CR2!  \ CR2寄存器
  0 SID-AD2!  \ AD2寄存器
  0 SID-SR2!  \ SR2寄存器
  0 SID-FREQ3LO!  \ FREQ3LO寄存器
  0 SID-FREQ3HI!  \ FREQ3HI寄存器
  0 SID-PW3LO!  \ PW3LO寄存器
  0 SID-PW3HI!  \ PW3HI寄存器
  0 SID-CR3!  \ CR3寄存器
  0 SID-AD3!  \ AD3寄存器
  0 SID-SR3!  \ SR3寄存器
  0 SID-FCH!  \ FCH寄存器
  0 SID-FCL!  \ FCL寄存器
  0 SID-RES_FLT!  \ RES_FLT寄存器
  0 SID-VOLUME!  \ VOLUME寄存器
  0 SID-POTX!  \ POTX寄存器
  0 SID-POTY!  \ POTY寄存器
  0 SID-OSC3!  \ OSC3寄存器
  0 SID-ENV3!  \ ENV3寄存器
  \ 初始化CIA1
  0 CIA1-PRA!  \ PRA寄存器
  0 CIA1-PRB!  \ PRB寄存器
  0 CIA1-DDRA!  \ DDRA寄存器
  0 CIA1-DDRB!  \ DDRB寄存器
  0 CIA1-TA_LO!  \ TA_LO寄存器
  0 CIA1-TA_HI!  \ TA_HI寄存器
  0 CIA1-TB_LO!  \ TB_LO寄存器
  0 CIA1-TB_HI!  \ TB_HI寄存器
  0 CIA1-TOD_TENTH!  \ TOD_TENTH寄存器
  0 CIA1-TOD_SEC!  \ TOD_SEC寄存器
  0 CIA1-TOD_MIN!  \ TOD_MIN寄存器
  0 CIA1-TOD_HR!  \ TOD_HR寄存器
  0 CIA1-SDR!  \ SDR寄存器
  0 CIA1-ICR!  \ ICR寄存器
  0 CIA1-CRA!  \ CRA寄存器
  0 CIA1-CRB!  \ CRB寄存器
  \ 初始化CIA2
  0 CIA2-PRA!  \ PRA寄存器
  0 CIA2-PRB!  \ PRB寄存器
  0 CIA2-DDRA!  \ DDRA寄存器
  0 CIA2-DDRB!  \ DDRB寄存器
  0 CIA2-TA_LO!  \ TA_LO寄存器
  0 CIA2-TA_HI!  \ TA_HI寄存器
  0 CIA2-TB_LO!  \ TB_LO寄存器
  0 CIA2-TB_HI!  \ TB_HI寄存器
  0 CIA2-TOD_TENTH!  \ TOD_TENTH寄存器
  0 CIA2-TOD_SEC!  \ TOD_SEC寄存器
  0 CIA2-TOD_MIN!  \ TOD_MIN寄存器
  0 CIA2-TOD_HR!  \ TOD_HR寄存器
  0 CIA2-SDR!  \ SDR寄存器
  0 CIA2-ICR!  \ ICR寄存器
  0 CIA2-CRA!  \ CRA寄存器
  0 CIA2-CRB!  \ CRB寄存器
  \ 初始化COLORRAM
  0 COLORRAM-COLOR!  \ COLOR寄存器
  \ 初始化IEC
  0 IEC-IEC_DATA!  \ IEC_DATA寄存器
  0 IEC-IEC_CLOCK!  \ IEC_CLOCK寄存器

  ." Commodore-64初始化完成" CR
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
  PORT@ PORT .R 8 .R SPACE ."  PORT: " PORT@ .
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

\ IRQ (VIC raster / CIA timer)
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
  COMMODORE_64-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
