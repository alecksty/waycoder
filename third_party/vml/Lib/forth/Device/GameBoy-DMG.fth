\ Sharp-LR35902设备定义 - Forth文件
\ 生成自: Sharp/Z80/Sharp-LR35902
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
\ CPU架构: LR35902
\ 位宽: 8位
\ 时钟频率: 4194304 Hz

\ =========================================
\ Sharp-LR35902设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Sharp-LR35902" ;
: MANUFACTURER  S" Sharp" ;
: FAMILY        S" Z80" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" LR35902" ;
8 CONSTANT BITS
4194304 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT B  \ B Register
0x02 CONSTANT C  \ C Register
0x03 CONSTANT D  \ D Register
0x04 CONSTANT E  \ E Register
0x05 CONSTANT F  \ Flags Register
4 CONSTANT F-C  \ Carry
5 CONSTANT F-H  \ Half Carry
6 CONSTANT F-N  \ Subtract
7 CONSTANT F-Z  \ Zero
0x06 CONSTANT H  \ H Register
0x07 CONSTANT L  \ L Register
0x08 CONSTANT AF  \ AF Register Pair (Accumulator + Flags)
0x0A CONSTANT BC  \ BC Register Pair
0x0C CONSTANT DE  \ DE Register Pair
0x0E CONSTANT HL  \ HL Register Pair
0x10 CONSTANT SP  \ Stack Pointer
0x12 CONSTANT PC  \ Program Counter

\ 内存段定义
0xC000 CONSTANT WRAM-START
0xCFFF CONSTANT WRAM-END
4096 CONSTANT WRAM-SIZE  \ Work RAM (4KB)
0xE000 CONSTANT WRAM_SHADOW-START
0xEFFF CONSTANT WRAM_SHADOW-END
4096 CONSTANT WRAM_SHADOW-SIZE  \ Work RAM Shadow (Echo RAM)
0xFF80 CONSTANT HRAM-START
0xFFFE CONSTANT HRAM-END
127 CONSTANT HRAM-SIZE  \ High RAM (127 bytes)
0xFF00 CONSTANT IO_REGISTERS-START
0xFF7F CONSTANT IO_REGISTERS-END
128 CONSTANT IO_REGISTERS-SIZE  \ I/O Registers
0xFE00 CONSTANT OAM-START
0xFE9F CONSTANT OAM-END
160 CONSTANT OAM-SIZE  \ Sprite Attribute Table (OAM)
0x8000 CONSTANT VRAM-START
0x9FFF CONSTANT VRAM-END
8192 CONSTANT VRAM-SIZE  \ Video RAM (8KB)
0x9800 CONSTANT BG_MAP_1-START
0x9BFF CONSTANT BG_MAP_1-END
1024 CONSTANT BG_MAP_1-SIZE  \ Background Map 1
0x9C00 CONSTANT BG_MAP_2-START
0x9FFF CONSTANT BG_MAP_2-END
1024 CONSTANT BG_MAP_2-SIZE  \ Background Map 2
0x0000 CONSTANT ROM_BANK0-START
0x3FFF CONSTANT ROM_BANK0-END
16384 CONSTANT ROM_BANK0-SIZE  \ ROM Bank 0 (Cartridge Header)
0x4000 CONSTANT ROM_BANK1-START
0x7FFF CONSTANT ROM_BANK1-END
16384 CONSTANT ROM_BANK1-SIZE  \ ROM Bank 1 (Switchable)
0xA000 CONSTANT CART_RAM-START
0xBFFF CONSTANT CART_RAM-END
8192 CONSTANT CART_RAM-SIZE  \ Cartridge RAM / MBC

\ 外设定义
\ LCD Controller / Picture Processing Unit
0xFF40 CONSTANT PPU-BASE
0xFF40 CONSTANT PPU-LCDC
0 CONSTANT PPU-LCDC-BG_ENABLE  \ Background Display Enable
1 CONSTANT PPU-LCDC-SPRITE_ENABLE  \ Sprite Display Enable
2 CONSTANT PPU-LCDC-SPRITE_SIZE  \ Sprite Size (0=8x8, 1=8x16)
3 CONSTANT PPU-LCDC-BG_TILE_MAP  \ BG Tile Map Area (0=9800, 1=9C00)
4 CONSTANT PPU-LCDC-TILE_DATA  \ Tile Data Area (0=8800, 1=8000)
5 CONSTANT PPU-LCDC-WINDOW_ENABLE  \ Window Display Enable
6 CONSTANT PPU-LCDC-WINDOW_MAP  \ Window Tile Map Area (0=9800, 1=9C00)
7 CONSTANT PPU-LCDC-LCD_ENABLE  \ LCD Display Enable
0xFF41 CONSTANT PPU-STAT
0 CONSTANT PPU-STAT-MODE  \ LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
2 CONSTANT PPU-STAT-LYC_FLAG  \ LY=LYC Compare Flag
3 CONSTANT PPU-STAT-HBLANK_IRQ  \ H-Blank Interrupt Enable
4 CONSTANT PPU-STAT-VBLANK_IRQ  \ V-Blank Interrupt Enable
5 CONSTANT PPU-STAT-OAM_IRQ  \ OAM Interrupt Enable
6 CONSTANT PPU-STAT-LYC_IRQ  \ LYC Interrupt Enable
0xFF42 CONSTANT PPU-SCY
0xFF43 CONSTANT PPU-SCX
0xFF44 CONSTANT PPU-LY
0xFF45 CONSTANT PPU-LYC
0xFF46 CONSTANT PPU-DMA
0xFF47 CONSTANT PPU-BGP
0xFF48 CONSTANT PPU-OBP0
0xFF49 CONSTANT PPU-OBP1
0xFF4A CONSTANT PPU-WY
0xFF4B CONSTANT PPU-WX
\ Audio Processing Unit
0xFF10 CONSTANT APU-BASE
0xFF10 CONSTANT APU-NR10
0 CONSTANT APU-NR10-SWEEP_TIME  \ Sweep Time
3 CONSTANT APU-NR10-SWEEP_INCREASE  \ Sweep Increase/Decrease
0 CONSTANT APU-NR10-SWEEP_SHIFTS  \ Sweep Number of Shifts
0xFF11 CONSTANT APU-NR11
0xFF12 CONSTANT APU-NR12
0xFF13 CONSTANT APU-NR13
0xFF14 CONSTANT APU-NR14
0xFF16 CONSTANT APU-NR21
0xFF17 CONSTANT APU-NR22
0xFF18 CONSTANT APU-NR23
0xFF19 CONSTANT APU-NR24
0xFF1A CONSTANT APU-NR30
0xFF1B CONSTANT APU-NR31
0xFF1C CONSTANT APU-NR32
0xFF1D CONSTANT APU-NR33
0xFF1E CONSTANT APU-NR34
0xFF20 CONSTANT APU-NR41
0xFF21 CONSTANT APU-NR42
0xFF22 CONSTANT APU-NR43
0xFF23 CONSTANT APU-NR44
0xFF24 CONSTANT APU-NR50
0xFF25 CONSTANT APU-NR51
0xFF26 CONSTANT APU-NR52
0 CONSTANT APU-NR52-CH1_ON  \ Channel 1 ON
1 CONSTANT APU-NR52-CH2_ON  \ Channel 2 ON
2 CONSTANT APU-NR52-CH3_ON  \ Channel 3 ON
3 CONSTANT APU-NR52-CH4_ON  \ Channel 4 ON
7 CONSTANT APU-NR52-ALL_ON  \ All Sound ON
\ Timer Unit
0xFF04 CONSTANT TIMER-BASE
0xFF04 CONSTANT TIMER-DIV
0xFF05 CONSTANT TIMER-TIMA
0xFF06 CONSTANT TIMER-TMA
0xFF07 CONSTANT TIMER-TAC
2 CONSTANT TIMER-TAC-TIMER_ENABLE  \ Timer Enable
0 CONSTANT TIMER-TAC-CLOCK_SEL  \ Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)
\ Joypad Controller
0xFF00 CONSTANT JOYPAD-BASE
0xFF00 CONSTANT JOYPAD-P1
0 CONSTANT JOYPAD-P1-A_BTN  \ A Button (1=Pressed when selected)
1 CONSTANT JOYPAD-P1-B_BTN  \ B Button (1=Pressed when selected)
2 CONSTANT JOYPAD-P1-SELECT  \ Select Button (1=Pressed)
3 CONSTANT JOYPAD-P1-START  \ Start Button (1=Pressed)
4 CONSTANT JOYPAD-P1-DIR_DOWN  \ Direction Down (1=Pressed when selected)
5 CONSTANT JOYPAD-P1-DIR_UP  \ Direction Up (1=Pressed when selected)
6 CONSTANT JOYPAD-P1-DIR_LEFT  \ Direction Left (1=Pressed when selected)
7 CONSTANT JOYPAD-P1-DIR_RIGHT  \ Direction Right (1=Pressed when selected)
\ Serial I/O (Link Cable)
0xFF01 CONSTANT SERIAL-BASE
0xFF01 CONSTANT SERIAL-SB
0xFF02 CONSTANT SERIAL-SC
7 CONSTANT SERIAL-SC-TRANSFER_START  \ Transfer Start
1 CONSTANT SERIAL-SC-CLOCK_SPEED  \ Clock Select (0=External, 1=Internal 8192Hz)
\ Interrupt Flag Register
0xFF0F CONSTANT INTERRUPT-BASE
0xFF0F CONSTANT INTERRUPT-IF
0 CONSTANT INTERRUPT-IF-VBLANK  \ V-Blank Interrupt Request
1 CONSTANT INTERRUPT-IF-LCDC  \ LCDC Status Interrupt Request
2 CONSTANT INTERRUPT-IF-TIMER  \ Timer Overflow Interrupt Request
3 CONSTANT INTERRUPT-IF-SERIAL  \ Serial Transfer Complete Interrupt Request
4 CONSTANT INTERRUPT-IF-JOYPAD  \ Joypad Interrupt Request
\ Interrupt Enable Register
0xFFFF CONSTANT IE-BASE
0xFFFF CONSTANT IE-IE
0 CONSTANT IE-IE-VBLANK_IE  \ V-Blank Interrupt Enable
1 CONSTANT IE-IE-LCDC_IE  \ LCDC Status Interrupt Enable
2 CONSTANT IE-IE-TIMER_IE  \ Timer Interrupt Enable
3 CONSTANT IE-IE-SERIAL_IE  \ Serial Interrupt Enable
4 CONSTANT IE-IE-JOYPAD_IE  \ Joypad Interrupt Enable

\ 中断向量定义
0 CONSTANT INT-VBLANK  \ V-Blank Interrupt (LY=144, during vertical blanking)
1 CONSTANT INT-LCDC_STATUS  \ LCDC Status Interrupt (H-Blank/OAM/V-Count match)
2 CONSTANT INT-TIMER_OVERFLOW  \ Timer Overflow Interrupt (TIMA overflow)
3 CONSTANT INT-SERIAL_COMPLETE  \ Serial Transfer Complete Interrupt
4 CONSTANT INT-JOYPAD  \ Joypad Interrupt (button press/release)

\ 引脚定义
1 CONSTANT PIN-VSS  \ Ground
2 CONSTANT PIN-VDD  \ Power Supply
3 CONSTANT PIN-PHI  \ System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
4 CONSTANT PIN-RESET  \ Reset Signal (active low)
5 CONSTANT PIN-INT  \ Interrupt Request
6 CONSTANT PIN-BUSREQ  \ Bus Request (external DMA access)
7 CONSTANT PIN-A0  \ Address Bus Bit 0
8 CONSTANT PIN-A1  \ Address Bus Bit 1
9 CONSTANT PIN-A2  \ Address Bus Bit 2
10 CONSTANT PIN-A3  \ Address Bus Bit 3
11 CONSTANT PIN-A4  \ Address Bus Bit 4
12 CONSTANT PIN-A5  \ Address Bus Bit 5
13 CONSTANT PIN-A6  \ Address Bus Bit 6
14 CONSTANT PIN-A7  \ Address Bus Bit 7
15 CONSTANT PIN-A8  \ Address Bus Bit 8
16 CONSTANT PIN-A9  \ Address Bus Bit 9
17 CONSTANT PIN-A10  \ Address Bus Bit 10
18 CONSTANT PIN-A11  \ Address Bus Bit 11
19 CONSTANT PIN-A12  \ Address Bus Bit 12
20 CONSTANT PIN-A13  \ Address Bus Bit 13
21 CONSTANT PIN-A14  \ Address Bus Bit 14
22 CONSTANT PIN-A15  \ Address Bus Bit 15
23 CONSTANT PIN-D0  \ Data Bus Bit 0
24 CONSTANT PIN-D1  \ Data Bus Bit 1
25 CONSTANT PIN-D2  \ Data Bus Bit 2
26 CONSTANT PIN-D3  \ Data Bus Bit 3
27 CONSTANT PIN-D4  \ Data Bus Bit 4
28 CONSTANT PIN-D5  \ Data Bus Bit 5
29 CONSTANT PIN-D6  \ Data Bus Bit 6
30 CONSTANT PIN-D7  \ Data Bus Bit 7
31 CONSTANT PIN-RD  \ Read Strobe (active low)
32 CONSTANT PIN-WR  \ Write Strobe (active low)
33 CONSTANT PIN-CS  \ Chip Select (active low)
34 CONSTANT PIN-SOUND_OUT  \ Audio Output
35 CONSTANT PIN-LCD_DATA0  \ LCD Data Bus Bit 0
36 CONSTANT PIN-LCD_DATA1  \ LCD Data Bus Bit 1
37 CONSTANT PIN-LCD_DATA2  \ LCD Data Bus Bit 2
38 CONSTANT PIN-LCD_DATA3  \ LCD Data Bus Bit 3
39 CONSTANT PIN-LCD_DATA4  \ LCD Data Bus Bit 4
40 CONSTANT PIN-LCD_DATA5  \ LCD Data Bus Bit 5
41 CONSTANT PIN-LCD_DATA6  \ LCD Data Bus Bit 6
42 CONSTANT PIN-LCD_DATA7  \ LCD Data Bus Bit 7
43 CONSTANT PIN-IR  \ Infrared Port (DMG-CGB-01)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: B@ ( -- n ) B C@ ;
: B! ( n -- ) B C! ;

: C@ ( -- n ) C C@ ;
: C! ( n -- ) C C! ;

: D@ ( -- n ) D C@ ;
: D! ( n -- ) D C! ;

: E@ ( -- n ) E C@ ;
: E! ( n -- ) E C! ;

: F@ ( -- n ) F C@ ;
: F! ( n -- ) F C! ;
: F-C@ ( -- flag ) F@ 4 BIT@ ;
: F-C! ( flag -- ) F@ 4 BIT! F! ;
: F-C-SET ( -- ) TRUE F-C! ;
: F-C-CLR ( -- ) FALSE F-C! ;
: F-H@ ( -- flag ) F@ 5 BIT@ ;
: F-H! ( flag -- ) F@ 5 BIT! F! ;
: F-H-SET ( -- ) TRUE F-H! ;
: F-H-CLR ( -- ) FALSE F-H! ;
: F-N@ ( -- flag ) F@ 6 BIT@ ;
: F-N! ( flag -- ) F@ 6 BIT! F! ;
: F-N-SET ( -- ) TRUE F-N! ;
: F-N-CLR ( -- ) FALSE F-N! ;
: F-Z@ ( -- flag ) F@ 7 BIT@ ;
: F-Z! ( flag -- ) F@ 7 BIT! F! ;
: F-Z-SET ( -- ) TRUE F-Z! ;
: F-Z-CLR ( -- ) FALSE F-Z! ;

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

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

\ 外设访问
\ PPU外设
: PPU-LCDC@ ( -- n ) PPU-LCDC C@ ;
: PPU-LCDC! ( n -- ) PPU-LCDC C! ;
: PPU-LCDC-BG_ENABLE@ ( -- flag ) PPU-LCDC@ 0 BIT@ ;
: PPU-LCDC-BG_ENABLE! ( flag -- ) PPU-LCDC@ 0 BIT! PPU-LCDC! ;
: PPU-LCDC-SPRITE_ENABLE@ ( -- flag ) PPU-LCDC@ 1 BIT@ ;
: PPU-LCDC-SPRITE_ENABLE! ( flag -- ) PPU-LCDC@ 1 BIT! PPU-LCDC! ;
: PPU-LCDC-SPRITE_SIZE@ ( -- flag ) PPU-LCDC@ 2 BIT@ ;
: PPU-LCDC-SPRITE_SIZE! ( flag -- ) PPU-LCDC@ 2 BIT! PPU-LCDC! ;
: PPU-LCDC-BG_TILE_MAP@ ( -- flag ) PPU-LCDC@ 3 BIT@ ;
: PPU-LCDC-BG_TILE_MAP! ( flag -- ) PPU-LCDC@ 3 BIT! PPU-LCDC! ;
: PPU-LCDC-TILE_DATA@ ( -- flag ) PPU-LCDC@ 4 BIT@ ;
: PPU-LCDC-TILE_DATA! ( flag -- ) PPU-LCDC@ 4 BIT! PPU-LCDC! ;
: PPU-LCDC-WINDOW_ENABLE@ ( -- flag ) PPU-LCDC@ 5 BIT@ ;
: PPU-LCDC-WINDOW_ENABLE! ( flag -- ) PPU-LCDC@ 5 BIT! PPU-LCDC! ;
: PPU-LCDC-WINDOW_MAP@ ( -- flag ) PPU-LCDC@ 6 BIT@ ;
: PPU-LCDC-WINDOW_MAP! ( flag -- ) PPU-LCDC@ 6 BIT! PPU-LCDC! ;
: PPU-LCDC-LCD_ENABLE@ ( -- flag ) PPU-LCDC@ 7 BIT@ ;
: PPU-LCDC-LCD_ENABLE! ( flag -- ) PPU-LCDC@ 7 BIT! PPU-LCDC! ;
: PPU-STAT@ ( -- n ) PPU-STAT C@ ;
: PPU-STAT! ( n -- ) PPU-STAT C! ;
: PPU-STAT-MODE@ ( -- flag ) PPU-STAT@ 0 BIT@ ;
: PPU-STAT-MODE! ( flag -- ) PPU-STAT@ 0 BIT! PPU-STAT! ;
: PPU-STAT-LYC_FLAG@ ( -- flag ) PPU-STAT@ 2 BIT@ ;
: PPU-STAT-LYC_FLAG! ( flag -- ) PPU-STAT@ 2 BIT! PPU-STAT! ;
: PPU-STAT-HBLANK_IRQ@ ( -- flag ) PPU-STAT@ 3 BIT@ ;
: PPU-STAT-HBLANK_IRQ! ( flag -- ) PPU-STAT@ 3 BIT! PPU-STAT! ;
: PPU-STAT-VBLANK_IRQ@ ( -- flag ) PPU-STAT@ 4 BIT@ ;
: PPU-STAT-VBLANK_IRQ! ( flag -- ) PPU-STAT@ 4 BIT! PPU-STAT! ;
: PPU-STAT-OAM_IRQ@ ( -- flag ) PPU-STAT@ 5 BIT@ ;
: PPU-STAT-OAM_IRQ! ( flag -- ) PPU-STAT@ 5 BIT! PPU-STAT! ;
: PPU-STAT-LYC_IRQ@ ( -- flag ) PPU-STAT@ 6 BIT@ ;
: PPU-STAT-LYC_IRQ! ( flag -- ) PPU-STAT@ 6 BIT! PPU-STAT! ;
: PPU-SCY@ ( -- n ) PPU-SCY C@ ;
: PPU-SCY! ( n -- ) PPU-SCY C! ;
: PPU-SCX@ ( -- n ) PPU-SCX C@ ;
: PPU-SCX! ( n -- ) PPU-SCX C! ;
: PPU-LY@ ( -- n ) PPU-LY C@ ;
: PPU-LY! ( n -- ) PPU-LY C! ;
: PPU-LYC@ ( -- n ) PPU-LYC C@ ;
: PPU-LYC! ( n -- ) PPU-LYC C! ;
: PPU-DMA@ ( -- n ) PPU-DMA C@ ;
: PPU-DMA! ( n -- ) PPU-DMA C! ;
: PPU-BGP@ ( -- n ) PPU-BGP C@ ;
: PPU-BGP! ( n -- ) PPU-BGP C! ;
: PPU-OBP0@ ( -- n ) PPU-OBP0 C@ ;
: PPU-OBP0! ( n -- ) PPU-OBP0 C! ;
: PPU-OBP1@ ( -- n ) PPU-OBP1 C@ ;
: PPU-OBP1! ( n -- ) PPU-OBP1 C! ;
: PPU-WY@ ( -- n ) PPU-WY C@ ;
: PPU-WY! ( n -- ) PPU-WY C! ;
: PPU-WX@ ( -- n ) PPU-WX C@ ;
: PPU-WX! ( n -- ) PPU-WX C! ;

\ apu外设
: APU-NR10@ ( -- n ) APU-NR10 C@ ;
: APU-NR10! ( n -- ) APU-NR10 C! ;
: APU-NR10-SWEEP_TIME@ ( -- flag ) APU-NR10@ 0 BIT@ ;
: APU-NR10-SWEEP_TIME! ( flag -- ) APU-NR10@ 0 BIT! APU-NR10! ;
: APU-NR10-SWEEP_INCREASE@ ( -- flag ) APU-NR10@ 3 BIT@ ;
: APU-NR10-SWEEP_INCREASE! ( flag -- ) APU-NR10@ 3 BIT! APU-NR10! ;
: APU-NR10-SWEEP_SHIFTS@ ( -- flag ) APU-NR10@ 0 BIT@ ;
: APU-NR10-SWEEP_SHIFTS! ( flag -- ) APU-NR10@ 0 BIT! APU-NR10! ;
: APU-NR11@ ( -- n ) APU-NR11 C@ ;
: APU-NR11! ( n -- ) APU-NR11 C! ;
: APU-NR12@ ( -- n ) APU-NR12 C@ ;
: APU-NR12! ( n -- ) APU-NR12 C! ;
: APU-NR13@ ( -- n ) APU-NR13 C@ ;
: APU-NR13! ( n -- ) APU-NR13 C! ;
: APU-NR14@ ( -- n ) APU-NR14 C@ ;
: APU-NR14! ( n -- ) APU-NR14 C! ;
: APU-NR21@ ( -- n ) APU-NR21 C@ ;
: APU-NR21! ( n -- ) APU-NR21 C! ;
: APU-NR22@ ( -- n ) APU-NR22 C@ ;
: APU-NR22! ( n -- ) APU-NR22 C! ;
: APU-NR23@ ( -- n ) APU-NR23 C@ ;
: APU-NR23! ( n -- ) APU-NR23 C! ;
: APU-NR24@ ( -- n ) APU-NR24 C@ ;
: APU-NR24! ( n -- ) APU-NR24 C! ;
: APU-NR30@ ( -- n ) APU-NR30 C@ ;
: APU-NR30! ( n -- ) APU-NR30 C! ;
: APU-NR31@ ( -- n ) APU-NR31 C@ ;
: APU-NR31! ( n -- ) APU-NR31 C! ;
: APU-NR32@ ( -- n ) APU-NR32 C@ ;
: APU-NR32! ( n -- ) APU-NR32 C! ;
: APU-NR33@ ( -- n ) APU-NR33 C@ ;
: APU-NR33! ( n -- ) APU-NR33 C! ;
: APU-NR34@ ( -- n ) APU-NR34 C@ ;
: APU-NR34! ( n -- ) APU-NR34 C! ;
: APU-NR41@ ( -- n ) APU-NR41 C@ ;
: APU-NR41! ( n -- ) APU-NR41 C! ;
: APU-NR42@ ( -- n ) APU-NR42 C@ ;
: APU-NR42! ( n -- ) APU-NR42 C! ;
: APU-NR43@ ( -- n ) APU-NR43 C@ ;
: APU-NR43! ( n -- ) APU-NR43 C! ;
: APU-NR44@ ( -- n ) APU-NR44 C@ ;
: APU-NR44! ( n -- ) APU-NR44 C! ;
: APU-NR50@ ( -- n ) APU-NR50 C@ ;
: APU-NR50! ( n -- ) APU-NR50 C! ;
: APU-NR51@ ( -- n ) APU-NR51 C@ ;
: APU-NR51! ( n -- ) APU-NR51 C! ;
: APU-NR52@ ( -- n ) APU-NR52 C@ ;
: APU-NR52! ( n -- ) APU-NR52 C! ;
: APU-NR52-CH1_ON@ ( -- flag ) APU-NR52@ 0 BIT@ ;
: APU-NR52-CH1_ON! ( flag -- ) APU-NR52@ 0 BIT! APU-NR52! ;
: APU-NR52-CH2_ON@ ( -- flag ) APU-NR52@ 1 BIT@ ;
: APU-NR52-CH2_ON! ( flag -- ) APU-NR52@ 1 BIT! APU-NR52! ;
: APU-NR52-CH3_ON@ ( -- flag ) APU-NR52@ 2 BIT@ ;
: APU-NR52-CH3_ON! ( flag -- ) APU-NR52@ 2 BIT! APU-NR52! ;
: APU-NR52-CH4_ON@ ( -- flag ) APU-NR52@ 3 BIT@ ;
: APU-NR52-CH4_ON! ( flag -- ) APU-NR52@ 3 BIT! APU-NR52! ;
: APU-NR52-ALL_ON@ ( -- flag ) APU-NR52@ 7 BIT@ ;
: APU-NR52-ALL_ON! ( flag -- ) APU-NR52@ 7 BIT! APU-NR52! ;

\ TIMER外设
: TIMER-DIV@ ( -- n ) TIMER-DIV C@ ;
: TIMER-DIV! ( n -- ) TIMER-DIV C! ;
: TIMER-TIMA@ ( -- n ) TIMER-TIMA C@ ;
: TIMER-TIMA! ( n -- ) TIMER-TIMA C! ;
: TIMER-TMA@ ( -- n ) TIMER-TMA C@ ;
: TIMER-TMA! ( n -- ) TIMER-TMA C! ;
: TIMER-TAC@ ( -- n ) TIMER-TAC C@ ;
: TIMER-TAC! ( n -- ) TIMER-TAC C! ;
: TIMER-TAC-TIMER_ENABLE@ ( -- flag ) TIMER-TAC@ 2 BIT@ ;
: TIMER-TAC-TIMER_ENABLE! ( flag -- ) TIMER-TAC@ 2 BIT! TIMER-TAC! ;
: TIMER-TAC-CLOCK_SEL@ ( -- flag ) TIMER-TAC@ 0 BIT@ ;
: TIMER-TAC-CLOCK_SEL! ( flag -- ) TIMER-TAC@ 0 BIT! TIMER-TAC! ;

\ JOYPAD外设
: JOYPAD-P1@ ( -- n ) JOYPAD-P1 C@ ;
: JOYPAD-P1! ( n -- ) JOYPAD-P1 C! ;
: JOYPAD-P1-A_BTN@ ( -- flag ) JOYPAD-P1@ 0 BIT@ ;
: JOYPAD-P1-A_BTN! ( flag -- ) JOYPAD-P1@ 0 BIT! JOYPAD-P1! ;
: JOYPAD-P1-B_BTN@ ( -- flag ) JOYPAD-P1@ 1 BIT@ ;
: JOYPAD-P1-B_BTN! ( flag -- ) JOYPAD-P1@ 1 BIT! JOYPAD-P1! ;
: JOYPAD-P1-SELECT@ ( -- flag ) JOYPAD-P1@ 2 BIT@ ;
: JOYPAD-P1-SELECT! ( flag -- ) JOYPAD-P1@ 2 BIT! JOYPAD-P1! ;
: JOYPAD-P1-START@ ( -- flag ) JOYPAD-P1@ 3 BIT@ ;
: JOYPAD-P1-START! ( flag -- ) JOYPAD-P1@ 3 BIT! JOYPAD-P1! ;
: JOYPAD-P1-DIR_DOWN@ ( -- flag ) JOYPAD-P1@ 4 BIT@ ;
: JOYPAD-P1-DIR_DOWN! ( flag -- ) JOYPAD-P1@ 4 BIT! JOYPAD-P1! ;
: JOYPAD-P1-DIR_UP@ ( -- flag ) JOYPAD-P1@ 5 BIT@ ;
: JOYPAD-P1-DIR_UP! ( flag -- ) JOYPAD-P1@ 5 BIT! JOYPAD-P1! ;
: JOYPAD-P1-DIR_LEFT@ ( -- flag ) JOYPAD-P1@ 6 BIT@ ;
: JOYPAD-P1-DIR_LEFT! ( flag -- ) JOYPAD-P1@ 6 BIT! JOYPAD-P1! ;
: JOYPAD-P1-DIR_RIGHT@ ( -- flag ) JOYPAD-P1@ 7 BIT@ ;
: JOYPAD-P1-DIR_RIGHT! ( flag -- ) JOYPAD-P1@ 7 BIT! JOYPAD-P1! ;

\ SERIAL外设
: SERIAL-SB@ ( -- n ) SERIAL-SB C@ ;
: SERIAL-SB! ( n -- ) SERIAL-SB C! ;
: SERIAL-SC@ ( -- n ) SERIAL-SC C@ ;
: SERIAL-SC! ( n -- ) SERIAL-SC C! ;
: SERIAL-SC-TRANSFER_START@ ( -- flag ) SERIAL-SC@ 7 BIT@ ;
: SERIAL-SC-TRANSFER_START! ( flag -- ) SERIAL-SC@ 7 BIT! SERIAL-SC! ;
: SERIAL-SC-CLOCK_SPEED@ ( -- flag ) SERIAL-SC@ 1 BIT@ ;
: SERIAL-SC-CLOCK_SPEED! ( flag -- ) SERIAL-SC@ 1 BIT! SERIAL-SC! ;

\ INTERRUPT外设
: INTERRUPT-IF@ ( -- n ) INTERRUPT-IF C@ ;
: INTERRUPT-IF! ( n -- ) INTERRUPT-IF C! ;
: INTERRUPT-IF-VBLANK@ ( -- flag ) INTERRUPT-IF@ 0 BIT@ ;
: INTERRUPT-IF-VBLANK! ( flag -- ) INTERRUPT-IF@ 0 BIT! INTERRUPT-IF! ;
: INTERRUPT-IF-LCDC@ ( -- flag ) INTERRUPT-IF@ 1 BIT@ ;
: INTERRUPT-IF-LCDC! ( flag -- ) INTERRUPT-IF@ 1 BIT! INTERRUPT-IF! ;
: INTERRUPT-IF-TIMER@ ( -- flag ) INTERRUPT-IF@ 2 BIT@ ;
: INTERRUPT-IF-TIMER! ( flag -- ) INTERRUPT-IF@ 2 BIT! INTERRUPT-IF! ;
: INTERRUPT-IF-SERIAL@ ( -- flag ) INTERRUPT-IF@ 3 BIT@ ;
: INTERRUPT-IF-SERIAL! ( flag -- ) INTERRUPT-IF@ 3 BIT! INTERRUPT-IF! ;
: INTERRUPT-IF-JOYPAD@ ( -- flag ) INTERRUPT-IF@ 4 BIT@ ;
: INTERRUPT-IF-JOYPAD! ( flag -- ) INTERRUPT-IF@ 4 BIT! INTERRUPT-IF! ;

\ IE外设
: IE-IE@ ( -- n ) IE-IE C@ ;
: IE-IE! ( n -- ) IE-IE C! ;
: IE-IE-VBLANK_IE@ ( -- flag ) IE-IE@ 0 BIT@ ;
: IE-IE-VBLANK_IE! ( flag -- ) IE-IE@ 0 BIT! IE-IE! ;
: IE-IE-LCDC_IE@ ( -- flag ) IE-IE@ 1 BIT@ ;
: IE-IE-LCDC_IE! ( flag -- ) IE-IE@ 1 BIT! IE-IE! ;
: IE-IE-TIMER_IE@ ( -- flag ) IE-IE@ 2 BIT@ ;
: IE-IE-TIMER_IE! ( flag -- ) IE-IE@ 2 BIT! IE-IE! ;
: IE-IE-SERIAL_IE@ ( -- flag ) IE-IE@ 3 BIT@ ;
: IE-IE-SERIAL_IE! ( flag -- ) IE-IE@ 3 BIT! IE-IE! ;
: IE-IE-JOYPAD_IE@ ( -- flag ) IE-IE@ 4 BIT@ ;
: IE-IE-JOYPAD_IE! ( flag -- ) IE-IE@ 4 BIT! IE-IE! ;

\ =========================================
\ 设备初始化
\ =========================================

: SHARP_LR35902-INIT ( -- )
  \ 初始化Sharp-LR35902设备
  ." 初始化Sharp-LR35902..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 B!  \ B Register
  0 C!  \ C Register
  0 D!  \ D Register
  0 E!  \ E Register
  0 F!  \ Flags Register
  0 H!  \ H Register
  0 L!  \ L Register
  0 AF!  \ AF Register Pair (Accumulator + Flags)
  0 BC!  \ BC Register Pair
  0 DE!  \ DE Register Pair
  0 HL!  \ HL Register Pair
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化PPU
  0 PPU-LCDC!  \ LCDC寄存器
  0 PPU-STAT!  \ STAT寄存器
  0 PPU-SCY!  \ SCY寄存器
  0 PPU-SCX!  \ SCX寄存器
  0 PPU-LY!  \ LY寄存器
  0 PPU-LYC!  \ LYC寄存器
  0 PPU-DMA!  \ DMA寄存器
  0 PPU-BGP!  \ BGP寄存器
  0 PPU-OBP0!  \ OBP0寄存器
  0 PPU-OBP1!  \ OBP1寄存器
  0 PPU-WY!  \ WY寄存器
  0 PPU-WX!  \ WX寄存器
  \ 初始化apu
  0 APU-NR10!  \ NR10寄存器
  0 APU-NR11!  \ NR11寄存器
  0 APU-NR12!  \ NR12寄存器
  0 APU-NR13!  \ NR13寄存器
  0 APU-NR14!  \ NR14寄存器
  0 APU-NR21!  \ NR21寄存器
  0 APU-NR22!  \ NR22寄存器
  0 APU-NR23!  \ NR23寄存器
  0 APU-NR24!  \ NR24寄存器
  0 APU-NR30!  \ NR30寄存器
  0 APU-NR31!  \ NR31寄存器
  0 APU-NR32!  \ NR32寄存器
  0 APU-NR33!  \ NR33寄存器
  0 APU-NR34!  \ NR34寄存器
  0 APU-NR41!  \ NR41寄存器
  0 APU-NR42!  \ NR42寄存器
  0 APU-NR43!  \ NR43寄存器
  0 APU-NR44!  \ NR44寄存器
  0 APU-NR50!  \ NR50寄存器
  0 APU-NR51!  \ NR51寄存器
  0 APU-NR52!  \ NR52寄存器
  \ 初始化TIMER
  0 TIMER-DIV!  \ DIV寄存器
  0 TIMER-TIMA!  \ TIMA寄存器
  0 TIMER-TMA!  \ TMA寄存器
  0 TIMER-TAC!  \ TAC寄存器
  \ 初始化JOYPAD
  0 JOYPAD-P1!  \ P1寄存器
  \ 初始化SERIAL
  0 SERIAL-SB!  \ SB寄存器
  0 SERIAL-SC!  \ SC寄存器
  \ 初始化INTERRUPT
  0 INTERRUPT-IF!  \ IF寄存器
  \ 初始化IE
  0 IE-IE!  \ IE寄存器

  ." Sharp-LR35902初始化完成" CR
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
  B@ B .R 8 .R SPACE ."  B: " B@ .
  C@ C .R 8 .R SPACE ."  C: " C@ .
  D@ D .R 8 .R SPACE ."  D: " D@ .
  E@ E .R 8 .R SPACE ."  E: " E@ .
  F@ F .R 8 .R SPACE ."  F: " F@ .
  H@ H .R 8 .R SPACE ."  H: " H@ .
  L@ L .R 8 .R SPACE ."  L: " L@ .
  AF@ AF .R 8 .R SPACE ."  AF: " AF@ .
  BC@ BC .R 8 .R SPACE ."  BC: " BC@ .
  DE@ DE .R 8 .R SPACE ."  DE: " DE@ .
  HL@ HL .R 8 .R SPACE ."  HL: " HL@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
;

\ =========================================
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ V-Blank Interrupt (LY=144, during vertical blanking)
: INT-VBLANK-HANDLER ( -- )
  ." VBLANK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-VBLANK-ENABLE ( -- )
  INT-VBLANK INT-ENABLE
;

: INT-VBLANK-DISABLE ( -- )
  INT-VBLANK INT-DISABLE
;

\ LCDC Status Interrupt (H-Blank/OAM/V-Count match)
: INT-LCDC_STATUS-HANDLER ( -- )
  ." LCDC_STATUS中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-LCDC_STATUS-ENABLE ( -- )
  INT-LCDC_STATUS INT-ENABLE
;

: INT-LCDC_STATUS-DISABLE ( -- )
  INT-LCDC_STATUS INT-DISABLE
;

\ Timer Overflow Interrupt (TIMA overflow)
: INT-TIMER_OVERFLOW-HANDLER ( -- )
  ." TIMER_OVERFLOW中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TIMER_OVERFLOW-ENABLE ( -- )
  INT-TIMER_OVERFLOW INT-ENABLE
;

: INT-TIMER_OVERFLOW-DISABLE ( -- )
  INT-TIMER_OVERFLOW INT-DISABLE
;

\ Serial Transfer Complete Interrupt
: INT-SERIAL_COMPLETE-HANDLER ( -- )
  ." SERIAL_COMPLETE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERIAL_COMPLETE-ENABLE ( -- )
  INT-SERIAL_COMPLETE INT-ENABLE
;

: INT-SERIAL_COMPLETE-DISABLE ( -- )
  INT-SERIAL_COMPLETE INT-DISABLE
;

\ Joypad Interrupt (button press/release)
: INT-JOYPAD-HANDLER ( -- )
  ." JOYPAD中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-JOYPAD-ENABLE ( -- )
  INT-JOYPAD INT-ENABLE
;

: INT-JOYPAD-DISABLE ( -- )
  INT-JOYPAD INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  SHARP_LR35902-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
