\ Zilog-Z80设备定义 - Forth文件
\ 生成自: Zilog/Z80/Zilog-Z80
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
\ CPU架构: Z80
\ 位宽: 8位
\ 时钟频率: 3580000 Hz

\ =========================================
\ Zilog-Z80设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Zilog-Z80" ;
: MANUFACTURER  S" Zilog" ;
: FAMILY        S" Z80" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Z80" ;
8 CONSTANT BITS
3580000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT F  \ Flags Register
0 CONSTANT F-C  \ Carry
1 CONSTANT F-N  \ Subtract
2 CONSTANT F-P  \ Parity/Overflow
4 CONSTANT F-H  \ Half Carry
6 CONSTANT F-Z  \ Zero
7 CONSTANT F-S  \ Sign/Negative
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
0x10 CONSTANT IX  \ Index Register X
0x12 CONSTANT IY  \ Index Register Y
0x14 CONSTANT SP  \ Stack Pointer
0x16 CONSTANT PC  \ Program Counter
0x18 CONSTANT I  \ Interrupt Vector Register
0x19 CONSTANT R  \ Memory Refresh Register
0x1A CONSTANT IM  \ Interrupt Mode (0/1/2)

\ 内存段定义
0xC000 CONSTANT WRAM-START
0xC7FF CONSTANT WRAM-END
2048 CONSTANT WRAM-SIZE  \ Work RAM (2KB internal)
0xE000 CONSTANT WRAM_SHADOW-START
0xE7FF CONSTANT WRAM_SHADOW-END
2048 CONSTANT WRAM_SHADOW-SIZE  \ Work RAM Shadow (Echo RAM)
0x4000 CONSTANT VRAM-START
0x7FFF CONSTANT VRAM-END
16384 CONSTANT VRAM-SIZE  \ Video RAM (16KB)
0x8000 CONSTANT SRAM-START
0xBFFF CONSTANT SRAM-END
16384 CONSTANT SRAM-SIZE  \ Cartridge SRAM (if present)
0x0000 CONSTANT CART_ROM-START
0x7FFF CONSTANT CART_ROM-END
32768 CONSTANT CART_ROM-SIZE  \ Cartridge ROM (up to 48KB)
0x0000 CONSTANT BIOS-START
0x1FFF CONSTANT BIOS-END
8192 CONSTANT BIOS-SIZE  \ BIOS ROM (Master System built-in, 8KB)
0x3F00 CONSTANT IO_REGS-START
0x3FFF CONSTANT IO_REGS-END
256 CONSTANT IO_REGS-SIZE  \ I/O Register Area

\ 外设定义
\ Video Display Processor (TMS9918A variant)
0xBE CONSTANT VDP-BASE
0xBF CONSTANT VDP-VDP_CTRL
0xBE CONSTANT VDP-VDP_DATA
0xBF CONSTANT VDP-VDP_STATUS
0 CONSTANT VDP-VDP_STATUS-FIFO_FULL  \ VRAM to CPU Transfer Pending
1 CONSTANT VDP-VDP_STATUS-FIFO_EMPTY  \ VRAM Write FIFO Empty
7 CONSTANT VDP-VDP_STATUS-INT_FLAG  \ V-Blank / Sprite Collision Flag
0x00 CONSTANT VDP-R0
0 CONSTANT VDP-R0-M3  \ Mode 3 Enable
1 CONSTANT VDP-R0-M2  \ Mode 2 Enable
2 CONSTANT VDP-R0-M1  \ Mode 1 Enable
3 CONSTANT VDP-R0-DISPLAY_DISABLE  \ Display Disable (1=blank screen)
4 CONSTANT VDP-R0-VIRQ_EN  \ Vertical Interrupt Enable
5 CONSTANT VDP-R0-M4  \ Mode 4 Enable (SMS2 only)
6 CONSTANT VDP-R0-SPRITE_SHIFT  \ Sprite Double Height
7 CONSTANT VDP-R0-HVC_LATCH  \ H-Counter Latch Enable
0x01 CONSTANT VDP-R1
3 CONSTANT VDP-R1-DISPLAY  \ Display Enable (1=active)
4 CONSTANT VDP-R1-FRAME_INT  \ Frame Interrupt (V-Blank) Enable
5 CONSTANT VDP-R1-M4  \ Mode 4 (256-color)
6 CONSTANT VDP-R1-SMS_MODE  \ SMS Display Mode (vs Coleco)
7 CONSTANT VDP-R1-EXT_VIDEO  \ External Video Enable
0x02 CONSTANT VDP-R2
0x03 CONSTANT VDP-R3
0x04 CONSTANT VDP-R4
0x05 CONSTANT VDP-R5
0x06 CONSTANT VDP-R6
0x07 CONSTANT VDP-R7
0x08 CONSTANT VDP-R8
0 CONSTANT VDP-R8-HSCROLL_EN  \ Horizontal Scroll Enable
1 CONSTANT VDP-R8-VSCROLL_EN  \ Vertical Scroll Enable
4 CONSTANT VDP-R8-LINE_INT  \ Line Interrupt Enable
7 CONSTANT VDP-R8-VSCROLL_2X  \ Vertical Scroll 2x Speed
0x09 CONSTANT VDP-R9
0x0A CONSTANT VDP-R10
0x0B CONSTANT VDP-R11
0x0C CONSTANT VDP-R12
0x0D CONSTANT VDP-R13
0x0E CONSTANT VDP-R14
0x0F CONSTANT VDP-R15
0x7E CONSTANT VDP-VCOUNTER
0x7F CONSTANT VDP-HCOUNTER
\ SN76489 Programmable Sound Generator (3 Square + 1 Noise)
0x7F CONSTANT PSG-BASE
0x00 CONSTANT PSG-CH0_FREQ
0x02 CONSTANT PSG-CH1_FREQ
0x04 CONSTANT PSG-CH2_FREQ
0x06 CONSTANT PSG-CH3_CONFIG
0 CONSTANT PSG-CH3_CONFIG-TYPE  \ Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
0 CONSTANT PSG-CH3_CONFIG-VOLUME  \ Volume (0-15)
0x01 CONSTANT PSG-CH0_VOLUME
0x03 CONSTANT PSG-CH1_VOLUME
0x05 CONSTANT PSG-CH2_VOLUME
\ I/O Port Registers
0x3F CONSTANT PORTS-BASE
0x3F CONSTANT PORTS-PORT_A
0 CONSTANT PORTS-PORT_A-UP  \ Up (0=pressed)
1 CONSTANT PORTS-PORT_A-DOWN  \ Down (0=pressed)
2 CONSTANT PORTS-PORT_A-LEFT  \ Left (0=pressed)
3 CONSTANT PORTS-PORT_A-RIGHT  \ Right (0=pressed)
4 CONSTANT PORTS-PORT_A-TR  \ Button TR (0=pressed)
5 CONSTANT PORTS-PORT_A-TL  \ Button TL (0=pressed)
0x3F CONSTANT PORTS-PORT_B
0 CONSTANT PORTS-PORT_B-UP  \ Up (0=pressed)
1 CONSTANT PORTS-PORT_B-DOWN  \ Down (0=pressed)
2 CONSTANT PORTS-PORT_B-LEFT  \ Left (0=pressed)
3 CONSTANT PORTS-PORT_B-RIGHT  \ Right (0=pressed)
4 CONSTANT PORTS-PORT_B-TR  \ Button TR (0=pressed)
5 CONSTANT PORTS-PORT_B-TL  \ Button TL (0=pressed)
0x3F CONSTANT PORTS-PORT_A_DDR
0x3F CONSTANT PORTS-PORT_B_DDR
\ Sega Mapper (Memory Bank Switching)
0xFFFD CONSTANT SEGAMAPPER-BASE
0xFFFD CONSTANT SEGAMAPPER-ROM_BANK0
0xFFFE CONSTANT SEGAMAPPER-ROM_BANK1
0xFFFF CONSTANT SEGAMAPPER-ROM_BANK2
\ Memory Mapper Control
0xFFFF CONSTANT MAPPER-BASE
0xFFF8 CONSTANT MAPPER-SRAM_BANK

\ 中断向量定义
0 CONSTANT INT-NMI  \ Non-Maskable Interrupt (Pause button / V-Blank)
1 CONSTANT INT-INT_VBLANK  \ V-Blank Interrupt (Frame end)
2 CONSTANT INT-INT_LINE  \ Scanline Interrupt (Line counter match)
3 CONSTANT INT-INT_EXT  \ External I/O Interrupt

\ 引脚定义
1 CONSTANT PIN-A  \ Power Supply
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-PHI  \ System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
4 CONSTANT PIN-RESET  \ Reset (active low)
5 CONSTANT PIN-M1  \ Machine Cycle 1 (instruction fetch)
6 CONSTANT PIN-MREQ  \ Memory Request
7 CONSTANT PIN-IORQ  \ I/O Request
8 CONSTANT PIN-RD  \ Read Strobe
9 CONSTANT PIN-WR  \ Write Strobe
10 CONSTANT PIN-HALT  \ Halt State
11 CONSTANT PIN-WAIT  \ Wait State Request
12 CONSTANT PIN-INT  \ Interrupt Request (active low)
13 CONSTANT PIN-NMI  \ Non-Maskable Interrupt (active low)
14 CONSTANT PIN-BUSRQ  \ Bus Request (active low)
15 CONSTANT PIN-BUSAK  \ Bus Acknowledge (active low)
16 CONSTANT PIN-A0  \ Address Bus Bit 0
17 CONSTANT PIN-A1  \ Address Bus Bit 1
18 CONSTANT PIN-A2  \ Address Bus Bit 2
19 CONSTANT PIN-A3  \ Address Bus Bit 3
20 CONSTANT PIN-A4  \ Address Bus Bit 4
21 CONSTANT PIN-A5  \ Address Bus Bit 5
22 CONSTANT PIN-A6  \ Address Bus Bit 6
23 CONSTANT PIN-A7  \ Address Bus Bit 7
24 CONSTANT PIN-A8  \ Address Bus Bit 8
25 CONSTANT PIN-A9  \ Address Bus Bit 9
26 CONSTANT PIN-A10  \ Address Bus Bit 10
27 CONSTANT PIN-A11  \ Address Bus Bit 11
28 CONSTANT PIN-A12  \ Address Bus Bit 12
29 CONSTANT PIN-A13  \ Address Bus Bit 13
30 CONSTANT PIN-A14  \ Address Bus Bit 14
31 CONSTANT PIN-A15  \ Address Bus Bit 15
32 CONSTANT PIN-D0  \ Data Bus Bit 0
33 CONSTANT PIN-D1  \ Data Bus Bit 1
34 CONSTANT PIN-D2  \ Data Bus Bit 2
35 CONSTANT PIN-D3  \ Data Bus Bit 3
36 CONSTANT PIN-D4  \ Data Bus Bit 4
37 CONSTANT PIN-D5  \ Data Bus Bit 5
38 CONSTANT PIN-D6  \ Data Bus Bit 6
39 CONSTANT PIN-D7  \ Data Bus Bit 7
40 CONSTANT PIN-AUDIO_OUT  \ Audio Output
41 CONSTANT PIN-VIDEO_SYNC  \ Composite Video Sync
42 CONSTANT PIN-VIDEO_OUT  \ Composite Video Output

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
: F-P@ ( -- flag ) F@ 2 BIT@ ;
: F-P! ( flag -- ) F@ 2 BIT! F! ;
: F-P-SET ( -- ) TRUE F-P! ;
: F-P-CLR ( -- ) FALSE F-P! ;
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

: IM@ ( -- n ) IM C@ ;
: IM! ( n -- ) IM C! ;

\ 外设访问
\ VDP外设
: VDP-VDP_CTRL@ ( -- n ) VDP-VDP_CTRL C@ ;
: VDP-VDP_CTRL! ( n -- ) VDP-VDP_CTRL C! ;
: VDP-VDP_DATA@ ( -- n ) VDP-VDP_DATA C@ ;
: VDP-VDP_DATA! ( n -- ) VDP-VDP_DATA C! ;
: VDP-VDP_STATUS@ ( -- n ) VDP-VDP_STATUS C@ ;
: VDP-VDP_STATUS! ( n -- ) VDP-VDP_STATUS C! ;
: VDP-VDP_STATUS-FIFO_FULL@ ( -- flag ) VDP-VDP_STATUS@ 0 BIT@ ;
: VDP-VDP_STATUS-FIFO_FULL! ( flag -- ) VDP-VDP_STATUS@ 0 BIT! VDP-VDP_STATUS! ;
: VDP-VDP_STATUS-FIFO_EMPTY@ ( -- flag ) VDP-VDP_STATUS@ 1 BIT@ ;
: VDP-VDP_STATUS-FIFO_EMPTY! ( flag -- ) VDP-VDP_STATUS@ 1 BIT! VDP-VDP_STATUS! ;
: VDP-VDP_STATUS-INT_FLAG@ ( -- flag ) VDP-VDP_STATUS@ 7 BIT@ ;
: VDP-VDP_STATUS-INT_FLAG! ( flag -- ) VDP-VDP_STATUS@ 7 BIT! VDP-VDP_STATUS! ;
: VDP-R0@ ( -- n ) VDP-R0 C@ ;
: VDP-R0! ( n -- ) VDP-R0 C! ;
: VDP-R0-M3@ ( -- flag ) VDP-R0@ 0 BIT@ ;
: VDP-R0-M3! ( flag -- ) VDP-R0@ 0 BIT! VDP-R0! ;
: VDP-R0-M2@ ( -- flag ) VDP-R0@ 1 BIT@ ;
: VDP-R0-M2! ( flag -- ) VDP-R0@ 1 BIT! VDP-R0! ;
: VDP-R0-M1@ ( -- flag ) VDP-R0@ 2 BIT@ ;
: VDP-R0-M1! ( flag -- ) VDP-R0@ 2 BIT! VDP-R0! ;
: VDP-R0-DISPLAY_DISABLE@ ( -- flag ) VDP-R0@ 3 BIT@ ;
: VDP-R0-DISPLAY_DISABLE! ( flag -- ) VDP-R0@ 3 BIT! VDP-R0! ;
: VDP-R0-VIRQ_EN@ ( -- flag ) VDP-R0@ 4 BIT@ ;
: VDP-R0-VIRQ_EN! ( flag -- ) VDP-R0@ 4 BIT! VDP-R0! ;
: VDP-R0-M4@ ( -- flag ) VDP-R0@ 5 BIT@ ;
: VDP-R0-M4! ( flag -- ) VDP-R0@ 5 BIT! VDP-R0! ;
: VDP-R0-SPRITE_SHIFT@ ( -- flag ) VDP-R0@ 6 BIT@ ;
: VDP-R0-SPRITE_SHIFT! ( flag -- ) VDP-R0@ 6 BIT! VDP-R0! ;
: VDP-R0-HVC_LATCH@ ( -- flag ) VDP-R0@ 7 BIT@ ;
: VDP-R0-HVC_LATCH! ( flag -- ) VDP-R0@ 7 BIT! VDP-R0! ;
: VDP-R1@ ( -- n ) VDP-R1 C@ ;
: VDP-R1! ( n -- ) VDP-R1 C! ;
: VDP-R1-DISPLAY@ ( -- flag ) VDP-R1@ 3 BIT@ ;
: VDP-R1-DISPLAY! ( flag -- ) VDP-R1@ 3 BIT! VDP-R1! ;
: VDP-R1-FRAME_INT@ ( -- flag ) VDP-R1@ 4 BIT@ ;
: VDP-R1-FRAME_INT! ( flag -- ) VDP-R1@ 4 BIT! VDP-R1! ;
: VDP-R1-M4@ ( -- flag ) VDP-R1@ 5 BIT@ ;
: VDP-R1-M4! ( flag -- ) VDP-R1@ 5 BIT! VDP-R1! ;
: VDP-R1-SMS_MODE@ ( -- flag ) VDP-R1@ 6 BIT@ ;
: VDP-R1-SMS_MODE! ( flag -- ) VDP-R1@ 6 BIT! VDP-R1! ;
: VDP-R1-EXT_VIDEO@ ( -- flag ) VDP-R1@ 7 BIT@ ;
: VDP-R1-EXT_VIDEO! ( flag -- ) VDP-R1@ 7 BIT! VDP-R1! ;
: VDP-R2@ ( -- n ) VDP-R2 C@ ;
: VDP-R2! ( n -- ) VDP-R2 C! ;
: VDP-R3@ ( -- n ) VDP-R3 C@ ;
: VDP-R3! ( n -- ) VDP-R3 C! ;
: VDP-R4@ ( -- n ) VDP-R4 C@ ;
: VDP-R4! ( n -- ) VDP-R4 C! ;
: VDP-R5@ ( -- n ) VDP-R5 C@ ;
: VDP-R5! ( n -- ) VDP-R5 C! ;
: VDP-R6@ ( -- n ) VDP-R6 C@ ;
: VDP-R6! ( n -- ) VDP-R6 C! ;
: VDP-R7@ ( -- n ) VDP-R7 C@ ;
: VDP-R7! ( n -- ) VDP-R7 C! ;
: VDP-R8@ ( -- n ) VDP-R8 C@ ;
: VDP-R8! ( n -- ) VDP-R8 C! ;
: VDP-R8-HSCROLL_EN@ ( -- flag ) VDP-R8@ 0 BIT@ ;
: VDP-R8-HSCROLL_EN! ( flag -- ) VDP-R8@ 0 BIT! VDP-R8! ;
: VDP-R8-VSCROLL_EN@ ( -- flag ) VDP-R8@ 1 BIT@ ;
: VDP-R8-VSCROLL_EN! ( flag -- ) VDP-R8@ 1 BIT! VDP-R8! ;
: VDP-R8-LINE_INT@ ( -- flag ) VDP-R8@ 4 BIT@ ;
: VDP-R8-LINE_INT! ( flag -- ) VDP-R8@ 4 BIT! VDP-R8! ;
: VDP-R8-VSCROLL_2X@ ( -- flag ) VDP-R8@ 7 BIT@ ;
: VDP-R8-VSCROLL_2X! ( flag -- ) VDP-R8@ 7 BIT! VDP-R8! ;
: VDP-R9@ ( -- n ) VDP-R9 C@ ;
: VDP-R9! ( n -- ) VDP-R9 C! ;
: VDP-R10@ ( -- n ) VDP-R10 C@ ;
: VDP-R10! ( n -- ) VDP-R10 C! ;
: VDP-R11@ ( -- n ) VDP-R11 C@ ;
: VDP-R11! ( n -- ) VDP-R11 C! ;
: VDP-R12@ ( -- n ) VDP-R12 C@ ;
: VDP-R12! ( n -- ) VDP-R12 C! ;
: VDP-R13@ ( -- n ) VDP-R13 C@ ;
: VDP-R13! ( n -- ) VDP-R13 C! ;
: VDP-R14@ ( -- n ) VDP-R14 C@ ;
: VDP-R14! ( n -- ) VDP-R14 C! ;
: VDP-R15@ ( -- n ) VDP-R15 C@ ;
: VDP-R15! ( n -- ) VDP-R15 C! ;
: VDP-VCOUNTER@ ( -- n ) VDP-VCOUNTER C@ ;
: VDP-VCOUNTER! ( n -- ) VDP-VCOUNTER C! ;
: VDP-HCOUNTER@ ( -- n ) VDP-HCOUNTER C@ ;
: VDP-HCOUNTER! ( n -- ) VDP-HCOUNTER C! ;

\ PSG外设
: PSG-CH0_FREQ@ ( -- n ) PSG-CH0_FREQ C@ ;
: PSG-CH0_FREQ! ( n -- ) PSG-CH0_FREQ C! ;
: PSG-CH1_FREQ@ ( -- n ) PSG-CH1_FREQ C@ ;
: PSG-CH1_FREQ! ( n -- ) PSG-CH1_FREQ C! ;
: PSG-CH2_FREQ@ ( -- n ) PSG-CH2_FREQ C@ ;
: PSG-CH2_FREQ! ( n -- ) PSG-CH2_FREQ C! ;
: PSG-CH3_CONFIG@ ( -- n ) PSG-CH3_CONFIG C@ ;
: PSG-CH3_CONFIG! ( n -- ) PSG-CH3_CONFIG C! ;
: PSG-CH3_CONFIG-TYPE@ ( -- flag ) PSG-CH3_CONFIG@ 0 BIT@ ;
: PSG-CH3_CONFIG-TYPE! ( flag -- ) PSG-CH3_CONFIG@ 0 BIT! PSG-CH3_CONFIG! ;
: PSG-CH3_CONFIG-VOLUME@ ( -- flag ) PSG-CH3_CONFIG@ 0 BIT@ ;
: PSG-CH3_CONFIG-VOLUME! ( flag -- ) PSG-CH3_CONFIG@ 0 BIT! PSG-CH3_CONFIG! ;
: PSG-CH0_VOLUME@ ( -- n ) PSG-CH0_VOLUME C@ ;
: PSG-CH0_VOLUME! ( n -- ) PSG-CH0_VOLUME C! ;
: PSG-CH1_VOLUME@ ( -- n ) PSG-CH1_VOLUME C@ ;
: PSG-CH1_VOLUME! ( n -- ) PSG-CH1_VOLUME C! ;
: PSG-CH2_VOLUME@ ( -- n ) PSG-CH2_VOLUME C@ ;
: PSG-CH2_VOLUME! ( n -- ) PSG-CH2_VOLUME C! ;

\ PORTS外设
: PORTS-PORT_A@ ( -- n ) PORTS-PORT_A C@ ;
: PORTS-PORT_A! ( n -- ) PORTS-PORT_A C! ;
: PORTS-PORT_A-UP@ ( -- flag ) PORTS-PORT_A@ 0 BIT@ ;
: PORTS-PORT_A-UP! ( flag -- ) PORTS-PORT_A@ 0 BIT! PORTS-PORT_A! ;
: PORTS-PORT_A-DOWN@ ( -- flag ) PORTS-PORT_A@ 1 BIT@ ;
: PORTS-PORT_A-DOWN! ( flag -- ) PORTS-PORT_A@ 1 BIT! PORTS-PORT_A! ;
: PORTS-PORT_A-LEFT@ ( -- flag ) PORTS-PORT_A@ 2 BIT@ ;
: PORTS-PORT_A-LEFT! ( flag -- ) PORTS-PORT_A@ 2 BIT! PORTS-PORT_A! ;
: PORTS-PORT_A-RIGHT@ ( -- flag ) PORTS-PORT_A@ 3 BIT@ ;
: PORTS-PORT_A-RIGHT! ( flag -- ) PORTS-PORT_A@ 3 BIT! PORTS-PORT_A! ;
: PORTS-PORT_A-TR@ ( -- flag ) PORTS-PORT_A@ 4 BIT@ ;
: PORTS-PORT_A-TR! ( flag -- ) PORTS-PORT_A@ 4 BIT! PORTS-PORT_A! ;
: PORTS-PORT_A-TL@ ( -- flag ) PORTS-PORT_A@ 5 BIT@ ;
: PORTS-PORT_A-TL! ( flag -- ) PORTS-PORT_A@ 5 BIT! PORTS-PORT_A! ;
: PORTS-PORT_B@ ( -- n ) PORTS-PORT_B C@ ;
: PORTS-PORT_B! ( n -- ) PORTS-PORT_B C! ;
: PORTS-PORT_B-UP@ ( -- flag ) PORTS-PORT_B@ 0 BIT@ ;
: PORTS-PORT_B-UP! ( flag -- ) PORTS-PORT_B@ 0 BIT! PORTS-PORT_B! ;
: PORTS-PORT_B-DOWN@ ( -- flag ) PORTS-PORT_B@ 1 BIT@ ;
: PORTS-PORT_B-DOWN! ( flag -- ) PORTS-PORT_B@ 1 BIT! PORTS-PORT_B! ;
: PORTS-PORT_B-LEFT@ ( -- flag ) PORTS-PORT_B@ 2 BIT@ ;
: PORTS-PORT_B-LEFT! ( flag -- ) PORTS-PORT_B@ 2 BIT! PORTS-PORT_B! ;
: PORTS-PORT_B-RIGHT@ ( -- flag ) PORTS-PORT_B@ 3 BIT@ ;
: PORTS-PORT_B-RIGHT! ( flag -- ) PORTS-PORT_B@ 3 BIT! PORTS-PORT_B! ;
: PORTS-PORT_B-TR@ ( -- flag ) PORTS-PORT_B@ 4 BIT@ ;
: PORTS-PORT_B-TR! ( flag -- ) PORTS-PORT_B@ 4 BIT! PORTS-PORT_B! ;
: PORTS-PORT_B-TL@ ( -- flag ) PORTS-PORT_B@ 5 BIT@ ;
: PORTS-PORT_B-TL! ( flag -- ) PORTS-PORT_B@ 5 BIT! PORTS-PORT_B! ;
: PORTS-PORT_A_DDR@ ( -- n ) PORTS-PORT_A_DDR C@ ;
: PORTS-PORT_A_DDR! ( n -- ) PORTS-PORT_A_DDR C! ;
: PORTS-PORT_B_DDR@ ( -- n ) PORTS-PORT_B_DDR C@ ;
: PORTS-PORT_B_DDR! ( n -- ) PORTS-PORT_B_DDR C! ;

\ SegaMapper外设
: SEGAMAPPER-ROM_BANK0@ ( -- n ) SEGAMAPPER-ROM_BANK0 C@ ;
: SEGAMAPPER-ROM_BANK0! ( n -- ) SEGAMAPPER-ROM_BANK0 C! ;
: SEGAMAPPER-ROM_BANK1@ ( -- n ) SEGAMAPPER-ROM_BANK1 C@ ;
: SEGAMAPPER-ROM_BANK1! ( n -- ) SEGAMAPPER-ROM_BANK1 C! ;
: SEGAMAPPER-ROM_BANK2@ ( -- n ) SEGAMAPPER-ROM_BANK2 C@ ;
: SEGAMAPPER-ROM_BANK2! ( n -- ) SEGAMAPPER-ROM_BANK2 C! ;

\ MAPPER外设
: MAPPER-SRAM_BANK@ ( -- n ) MAPPER-SRAM_BANK C@ ;
: MAPPER-SRAM_BANK! ( n -- ) MAPPER-SRAM_BANK C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ZILOG_Z80-INIT ( -- )
  \ 初始化Zilog-Z80设备
  ." 初始化Zilog-Z80..." CR

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
  0 IX!  \ Index Register X
  0 IY!  \ Index Register Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 I!  \ Interrupt Vector Register
  0 R!  \ Memory Refresh Register
  0 IM!  \ Interrupt Mode (0/1/2)

  \ 初始化外设
  \ 初始化VDP
  0 VDP-VDP_CTRL!  \ VDP_CTRL寄存器
  0 VDP-VDP_DATA!  \ VDP_DATA寄存器
  0 VDP-VDP_STATUS!  \ VDP_STATUS寄存器
  0 VDP-R0!  \ R0寄存器
  0 VDP-R1!  \ R1寄存器
  0 VDP-R2!  \ R2寄存器
  0 VDP-R3!  \ R3寄存器
  0 VDP-R4!  \ R4寄存器
  0 VDP-R5!  \ R5寄存器
  0 VDP-R6!  \ R6寄存器
  0 VDP-R7!  \ R7寄存器
  0 VDP-R8!  \ R8寄存器
  0 VDP-R9!  \ R9寄存器
  0 VDP-R10!  \ R10寄存器
  0 VDP-R11!  \ R11寄存器
  0 VDP-R12!  \ R12寄存器
  0 VDP-R13!  \ R13寄存器
  0 VDP-R14!  \ R14寄存器
  0 VDP-R15!  \ R15寄存器
  0 VDP-VCOUNTER!  \ VCOUNTER寄存器
  0 VDP-HCOUNTER!  \ HCOUNTER寄存器
  \ 初始化PSG
  0 PSG-CH0_FREQ!  \ CH0_FREQ寄存器
  0 PSG-CH1_FREQ!  \ CH1_FREQ寄存器
  0 PSG-CH2_FREQ!  \ CH2_FREQ寄存器
  0 PSG-CH3_CONFIG!  \ CH3_CONFIG寄存器
  0 PSG-CH0_VOLUME!  \ CH0_VOLUME寄存器
  0 PSG-CH1_VOLUME!  \ CH1_VOLUME寄存器
  0 PSG-CH2_VOLUME!  \ CH2_VOLUME寄存器
  \ 初始化PORTS
  0 PORTS-PORT_A!  \ PORT_A寄存器
  0 PORTS-PORT_B!  \ PORT_B寄存器
  0 PORTS-PORT_A_DDR!  \ PORT_A_DDR寄存器
  0 PORTS-PORT_B_DDR!  \ PORT_B_DDR寄存器
  \ 初始化SegaMapper
  0 SEGAMAPPER-ROM_BANK0!  \ ROM_BANK0寄存器
  0 SEGAMAPPER-ROM_BANK1!  \ ROM_BANK1寄存器
  0 SEGAMAPPER-ROM_BANK2!  \ ROM_BANK2寄存器
  \ 初始化MAPPER
  0 MAPPER-SRAM_BANK!  \ SRAM_BANK寄存器

  ." Zilog-Z80初始化完成" CR
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
  AF@ AF .R 8 .R SPACE ."  AF_: " AF@ .
  BC@ BC .R 8 .R SPACE ."  BC_: " BC@ .
  DE@ DE .R 8 .R SPACE ."  DE_: " DE@ .
  HL@ HL .R 8 .R SPACE ."  HL_: " HL@ .
  IX@ IX .R 8 .R SPACE ."  IX: " IX@ .
  IY@ IY .R 8 .R SPACE ."  IY: " IY@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  I@ I .R 8 .R SPACE ."  I: " I@ .
  R@ R .R 8 .R SPACE ."  R: " R@ .
  IM@ IM .R 8 .R SPACE ."  IM: " IM@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Non-Maskable Interrupt (Pause button / V-Blank)
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

\ V-Blank Interrupt (Frame end)
: INT-INT_VBLANK-HANDLER ( -- )
  ." INT_VBLANK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT_VBLANK-ENABLE ( -- )
  INT-INT_VBLANK INT-ENABLE
;

: INT-INT_VBLANK-DISABLE ( -- )
  INT-INT_VBLANK INT-DISABLE
;

\ Scanline Interrupt (Line counter match)
: INT-INT_LINE-HANDLER ( -- )
  ." INT_LINE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT_LINE-ENABLE ( -- )
  INT-INT_LINE INT-ENABLE
;

: INT-INT_LINE-DISABLE ( -- )
  INT-INT_LINE INT-DISABLE
;

\ External I/O Interrupt
: INT-INT_EXT-HANDLER ( -- )
  ." INT_EXT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT_EXT-ENABLE ( -- )
  INT-INT_EXT INT-ENABLE
;

: INT-INT_EXT-DISABLE ( -- )
  INT-INT_EXT INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ZILOG_Z80-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
