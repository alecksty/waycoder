' Sharp-LR35902寄存器定义
' 生成自: Sharp/Z80/Sharp-LR35902
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz

' CPU架构: LR35902
' 位宽: 8位
' 时钟频率: 4194304 Hz

' 寄存器定义
' Accumulator
CONST A = 0x00

' B Register
CONST B = 0x01

' C Register
CONST C = 0x02

' D Register
CONST D = 0x03

' E Register
CONST E = 0x04

' Flags Register
CONST F = 0x05
CONST F_C = 4  ' Carry
CONST F_H = 5  ' Half Carry
CONST F_N = 6  ' Subtract
CONST F_Z = 7  ' Zero

' H Register
CONST H = 0x06

' L Register
CONST L = 0x07

' AF Register Pair (Accumulator + Flags)
CONST AF = 0x08

' BC Register Pair
CONST BC = 0x0A

' DE Register Pair
CONST DE = 0x0C

' HL Register Pair
CONST HL = 0x0E

' Stack Pointer
CONST SP = 0x10

' Program Counter
CONST PC = 0x12

' 内存段定义
' Work RAM (4KB)
CONST WRAM_START = 0xC000
CONST WRAM_END = 0xCFFF
CONST WRAM_SIZE = 4096

' Work RAM Shadow (Echo RAM)
CONST WRAM_SHADOW_START = 0xE000
CONST WRAM_SHADOW_END = 0xEFFF
CONST WRAM_SHADOW_SIZE = 4096

' High RAM (127 bytes)
CONST HRAM_START = 0xFF80
CONST HRAM_END = 0xFFFE
CONST HRAM_SIZE = 127

' I/O Registers
CONST IO_REGISTERS_START = 0xFF00
CONST IO_REGISTERS_END = 0xFF7F
CONST IO_REGISTERS_SIZE = 128

' Sprite Attribute Table (OAM)
CONST OAM_START = 0xFE00
CONST OAM_END = 0xFE9F
CONST OAM_SIZE = 160

' Video RAM (8KB)
CONST VRAM_START = 0x8000
CONST VRAM_END = 0x9FFF
CONST VRAM_SIZE = 8192

' Background Map 1
CONST BG_MAP_1_START = 0x9800
CONST BG_MAP_1_END = 0x9BFF
CONST BG_MAP_1_SIZE = 1024

' Background Map 2
CONST BG_MAP_2_START = 0x9C00
CONST BG_MAP_2_END = 0x9FFF
CONST BG_MAP_2_SIZE = 1024

' ROM Bank 0 (Cartridge Header)
CONST ROM_BANK0_START = 0x0000
CONST ROM_BANK0_END = 0x3FFF
CONST ROM_BANK0_SIZE = 16384

' ROM Bank 1 (Switchable)
CONST ROM_BANK1_START = 0x4000
CONST ROM_BANK1_END = 0x7FFF
CONST ROM_BANK1_SIZE = 16384

' Cartridge RAM / MBC
CONST CART_RAM_START = 0xA000
CONST CART_RAM_END = 0xBFFF
CONST CART_RAM_SIZE = 8192

' 外设定义
' LCD Controller / Picture Processing Unit
CONST PPU_BASE = 0xFF40
CONST PPU_LCDC = 0xFF40
CONST PPU_LCDC_BG_ENABLE = 0  ' Background Display Enable
CONST PPU_LCDC_SPRITE_ENABLE = 1  ' Sprite Display Enable
CONST PPU_LCDC_SPRITE_SIZE = 2  ' Sprite Size (0=8x8, 1=8x16)
CONST PPU_LCDC_BG_TILE_MAP = 3  ' BG Tile Map Area (0=9800, 1=9C00)
CONST PPU_LCDC_TILE_DATA = 4  ' Tile Data Area (0=8800, 1=8000)
CONST PPU_LCDC_WINDOW_ENABLE = 5  ' Window Display Enable
CONST PPU_LCDC_WINDOW_MAP = 6  ' Window Tile Map Area (0=9800, 1=9C00)
CONST PPU_LCDC_LCD_ENABLE = 7  ' LCD Display Enable
CONST PPU_STAT = 0xFF41
CONST PPU_STAT_MODE = 0  ' LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
CONST PPU_STAT_LYC_FLAG = 2  ' LY=LYC Compare Flag
CONST PPU_STAT_HBLANK_IRQ = 3  ' H-Blank Interrupt Enable
CONST PPU_STAT_VBLANK_IRQ = 4  ' V-Blank Interrupt Enable
CONST PPU_STAT_OAM_IRQ = 5  ' OAM Interrupt Enable
CONST PPU_STAT_LYC_IRQ = 6  ' LYC Interrupt Enable
CONST PPU_SCY = 0xFF42
CONST PPU_SCX = 0xFF43
CONST PPU_LY = 0xFF44
CONST PPU_LYC = 0xFF45
CONST PPU_DMA = 0xFF46
CONST PPU_BGP = 0xFF47
CONST PPU_OBP0 = 0xFF48
CONST PPU_OBP1 = 0xFF49
CONST PPU_WY = 0xFF4A
CONST PPU_WX = 0xFF4B

' Audio Processing Unit
CONST APU_BASE = 0xFF10
CONST APU_NR10 = 0xFF10
CONST APU_NR10_SWEEP_TIME = 0  ' Sweep Time
CONST APU_NR10_SWEEP_INCREASE = 3  ' Sweep Increase/Decrease
CONST APU_NR10_SWEEP_SHIFTS = 0  ' Sweep Number of Shifts
CONST APU_NR11 = 0xFF11
CONST APU_NR12 = 0xFF12
CONST APU_NR13 = 0xFF13
CONST APU_NR14 = 0xFF14
CONST APU_NR21 = 0xFF16
CONST APU_NR22 = 0xFF17
CONST APU_NR23 = 0xFF18
CONST APU_NR24 = 0xFF19
CONST APU_NR30 = 0xFF1A
CONST APU_NR31 = 0xFF1B
CONST APU_NR32 = 0xFF1C
CONST APU_NR33 = 0xFF1D
CONST APU_NR34 = 0xFF1E
CONST APU_NR41 = 0xFF20
CONST APU_NR42 = 0xFF21
CONST APU_NR43 = 0xFF22
CONST APU_NR44 = 0xFF23
CONST APU_NR50 = 0xFF24
CONST APU_NR51 = 0xFF25
CONST APU_NR52 = 0xFF26
CONST APU_NR52_CH1_ON = 0  ' Channel 1 ON
CONST APU_NR52_CH2_ON = 1  ' Channel 2 ON
CONST APU_NR52_CH3_ON = 2  ' Channel 3 ON
CONST APU_NR52_CH4_ON = 3  ' Channel 4 ON
CONST APU_NR52_ALL_ON = 7  ' All Sound ON

' Timer Unit
CONST TIMER_BASE = 0xFF04
CONST TIMER_DIV = 0xFF04
CONST TIMER_TIMA = 0xFF05
CONST TIMER_TMA = 0xFF06
CONST TIMER_TAC = 0xFF07
CONST TIMER_TAC_TIMER_ENABLE = 2  ' Timer Enable
CONST TIMER_TAC_CLOCK_SEL = 0  ' Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)

' Joypad Controller
CONST JOYPAD_BASE = 0xFF00
CONST JOYPAD_P1 = 0xFF00
CONST JOYPAD_P1_A_BTN = 0  ' A Button (1=Pressed when selected)
CONST JOYPAD_P1_B_BTN = 1  ' B Button (1=Pressed when selected)
CONST JOYPAD_P1_SELECT = 2  ' Select Button (1=Pressed)
CONST JOYPAD_P1_START = 3  ' Start Button (1=Pressed)
CONST JOYPAD_P1_DIR_DOWN = 4  ' Direction Down (1=Pressed when selected)
CONST JOYPAD_P1_DIR_UP = 5  ' Direction Up (1=Pressed when selected)
CONST JOYPAD_P1_DIR_LEFT = 6  ' Direction Left (1=Pressed when selected)
CONST JOYPAD_P1_DIR_RIGHT = 7  ' Direction Right (1=Pressed when selected)

' Serial I/O (Link Cable)
CONST SERIAL_BASE = 0xFF01
CONST SERIAL_SB = 0xFF01
CONST SERIAL_SC = 0xFF02
CONST SERIAL_SC_TRANSFER_START = 7  ' Transfer Start
CONST SERIAL_SC_CLOCK_SPEED = 1  ' Clock Select (0=External, 1=Internal 8192Hz)

' Interrupt Flag Register
CONST INTERRUPT_BASE = 0xFF0F
CONST INTERRUPT_IF = 0xFF0F
CONST INTERRUPT_IF_VBLANK = 0  ' V-Blank Interrupt Request
CONST INTERRUPT_IF_LCDC = 1  ' LCDC Status Interrupt Request
CONST INTERRUPT_IF_TIMER = 2  ' Timer Overflow Interrupt Request
CONST INTERRUPT_IF_SERIAL = 3  ' Serial Transfer Complete Interrupt Request
CONST INTERRUPT_IF_JOYPAD = 4  ' Joypad Interrupt Request

' Interrupt Enable Register
CONST IE_BASE = 0xFFFF
CONST IE_IE = 0xFFFF
CONST IE_IE_VBLANK_IE = 0  ' V-Blank Interrupt Enable
CONST IE_IE_LCDC_IE = 1  ' LCDC Status Interrupt Enable
CONST IE_IE_TIMER_IE = 2  ' Timer Interrupt Enable
CONST IE_IE_SERIAL_IE = 3  ' Serial Interrupt Enable
CONST IE_IE_JOYPAD_IE = 4  ' Joypad Interrupt Enable

' 中断向量定义
CONST VBLANK_VECTOR = 0  ' V-Blank Interrupt (LY=144, during vertical blanking)
CONST LCDC_STATUS_VECTOR = 1  ' LCDC Status Interrupt (H-Blank/OAM/V-Count match)
CONST TIMER_OVERFLOW_VECTOR = 2  ' Timer Overflow Interrupt (TIMA overflow)
CONST SERIAL_COMPLETE_VECTOR = 3  ' Serial Transfer Complete Interrupt
CONST JOYPAD_VECTOR = 4  ' Joypad Interrupt (button press/release)

' 引脚定义
CONST PIN_VSS = 1  ' Ground
CONST PIN_VDD = 2  ' Power Supply
CONST PIN_PHI = 3  ' System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
CONST PIN_RESET = 4  ' Reset Signal (active low)
CONST PIN_INT = 5  ' Interrupt Request
CONST PIN_BUSREQ = 6  ' Bus Request (external DMA access)
CONST PIN_A0 = 7  ' Address Bus Bit 0
CONST PIN_A1 = 8  ' Address Bus Bit 1
CONST PIN_A2 = 9  ' Address Bus Bit 2
CONST PIN_A3 = 10  ' Address Bus Bit 3
CONST PIN_A4 = 11  ' Address Bus Bit 4
CONST PIN_A5 = 12  ' Address Bus Bit 5
CONST PIN_A6 = 13  ' Address Bus Bit 6
CONST PIN_A7 = 14  ' Address Bus Bit 7
CONST PIN_A8 = 15  ' Address Bus Bit 8
CONST PIN_A9 = 16  ' Address Bus Bit 9
CONST PIN_A10 = 17  ' Address Bus Bit 10
CONST PIN_A11 = 18  ' Address Bus Bit 11
CONST PIN_A12 = 19  ' Address Bus Bit 12
CONST PIN_A13 = 20  ' Address Bus Bit 13
CONST PIN_A14 = 21  ' Address Bus Bit 14
CONST PIN_A15 = 22  ' Address Bus Bit 15
CONST PIN_D0 = 23  ' Data Bus Bit 0
CONST PIN_D1 = 24  ' Data Bus Bit 1
CONST PIN_D2 = 25  ' Data Bus Bit 2
CONST PIN_D3 = 26  ' Data Bus Bit 3
CONST PIN_D4 = 27  ' Data Bus Bit 4
CONST PIN_D5 = 28  ' Data Bus Bit 5
CONST PIN_D6 = 29  ' Data Bus Bit 6
CONST PIN_D7 = 30  ' Data Bus Bit 7
CONST PIN_RD = 31  ' Read Strobe (active low)
CONST PIN_WR = 32  ' Write Strobe (active low)
CONST PIN_CS = 33  ' Chip Select (active low)
CONST PIN_SOUND_OUT = 34  ' Audio Output
CONST PIN_LCD_DATA0 = 35  ' LCD Data Bus Bit 0
CONST PIN_LCD_DATA1 = 36  ' LCD Data Bus Bit 1
CONST PIN_LCD_DATA2 = 37  ' LCD Data Bus Bit 2
CONST PIN_LCD_DATA3 = 38  ' LCD Data Bus Bit 3
CONST PIN_LCD_DATA4 = 39  ' LCD Data Bus Bit 4
CONST PIN_LCD_DATA5 = 40  ' LCD Data Bus Bit 5
CONST PIN_LCD_DATA6 = 41  ' LCD Data Bus Bit 6
CONST PIN_LCD_DATA7 = 42  ' LCD Data Bus Bit 7
CONST PIN_IR = 43  ' Infrared Port (DMG-CGB-01)

' 设备初始化子程序
SUB sharp_lr35902_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
