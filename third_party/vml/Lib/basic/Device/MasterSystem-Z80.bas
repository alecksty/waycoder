' Zilog-Z80寄存器定义
' 生成自: Zilog/Z80/Zilog-Z80
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz

' CPU架构: Z80
' 位宽: 8位
' 时钟频率: 3580000 Hz

' 寄存器定义
' Accumulator
CONST A = 0x00

' Flags Register
CONST F = 0x01
CONST F_C = 0  ' Carry
CONST F_N = 1  ' Subtract
CONST F_P = 2  ' Parity/Overflow
CONST F_H = 4  ' Half Carry
CONST F_Z = 6  ' Zero
CONST F_S = 7  ' Sign/Negative

' B Register
CONST B = 0x02

' C Register
CONST C = 0x03

' D Register
CONST D = 0x04

' E Register
CONST E = 0x05

' H Register
CONST H = 0x06

' L Register
CONST L = 0x07

' Alternate AF
CONST AF = 0x08

' Alternate BC
CONST BC = 0x0A

' Alternate DE
CONST DE = 0x0C

' Alternate HL
CONST HL = 0x0E

' Index Register X
CONST IX = 0x10

' Index Register Y
CONST IY = 0x12

' Stack Pointer
CONST SP = 0x14

' Program Counter
CONST PC = 0x16

' Interrupt Vector Register
CONST I = 0x18

' Memory Refresh Register
CONST R = 0x19

' Interrupt Mode (0/1/2)
CONST IM = 0x1A

' 内存段定义
' Work RAM (2KB internal)
CONST WRAM_START = 0xC000
CONST WRAM_END = 0xC7FF
CONST WRAM_SIZE = 2048

' Work RAM Shadow (Echo RAM)
CONST WRAM_SHADOW_START = 0xE000
CONST WRAM_SHADOW_END = 0xE7FF
CONST WRAM_SHADOW_SIZE = 2048

' Video RAM (16KB)
CONST VRAM_START = 0x4000
CONST VRAM_END = 0x7FFF
CONST VRAM_SIZE = 16384

' Cartridge SRAM (if present)
CONST SRAM_START = 0x8000
CONST SRAM_END = 0xBFFF
CONST SRAM_SIZE = 16384

' Cartridge ROM (up to 48KB)
CONST CART_ROM_START = 0x0000
CONST CART_ROM_END = 0x7FFF
CONST CART_ROM_SIZE = 32768

' BIOS ROM (Master System built-in, 8KB)
CONST BIOS_START = 0x0000
CONST BIOS_END = 0x1FFF
CONST BIOS_SIZE = 8192

' I/O Register Area
CONST IO_REGS_START = 0x3F00
CONST IO_REGS_END = 0x3FFF
CONST IO_REGS_SIZE = 256

' 外设定义
' Video Display Processor (TMS9918A variant)
CONST VDP_BASE = 0xBE
CONST VDP_VDP_CTRL = 0xBF
CONST VDP_VDP_DATA = 0xBE
CONST VDP_VDP_STATUS = 0xBF
CONST VDP_VDP_STATUS_FIFO_FULL = 0  ' VRAM to CPU Transfer Pending
CONST VDP_VDP_STATUS_FIFO_EMPTY = 1  ' VRAM Write FIFO Empty
CONST VDP_VDP_STATUS_INT_FLAG = 7  ' V-Blank / Sprite Collision Flag
CONST VDP_R0 = 0x00
CONST VDP_R0_M3 = 0  ' Mode 3 Enable
CONST VDP_R0_M2 = 1  ' Mode 2 Enable
CONST VDP_R0_M1 = 2  ' Mode 1 Enable
CONST VDP_R0_DISPLAY_DISABLE = 3  ' Display Disable (1=blank screen)
CONST VDP_R0_VIRQ_EN = 4  ' Vertical Interrupt Enable
CONST VDP_R0_M4 = 5  ' Mode 4 Enable (SMS2 only)
CONST VDP_R0_SPRITE_SHIFT = 6  ' Sprite Double Height
CONST VDP_R0_HVC_LATCH = 7  ' H-Counter Latch Enable
CONST VDP_R1 = 0x01
CONST VDP_R1_DISPLAY = 3  ' Display Enable (1=active)
CONST VDP_R1_FRAME_INT = 4  ' Frame Interrupt (V-Blank) Enable
CONST VDP_R1_M4 = 5  ' Mode 4 (256-color)
CONST VDP_R1_SMS_MODE = 6  ' SMS Display Mode (vs Coleco)
CONST VDP_R1_EXT_VIDEO = 7  ' External Video Enable
CONST VDP_R2 = 0x02
CONST VDP_R3 = 0x03
CONST VDP_R4 = 0x04
CONST VDP_R5 = 0x05
CONST VDP_R6 = 0x06
CONST VDP_R7 = 0x07
CONST VDP_R8 = 0x08
CONST VDP_R8_HSCROLL_EN = 0  ' Horizontal Scroll Enable
CONST VDP_R8_VSCROLL_EN = 1  ' Vertical Scroll Enable
CONST VDP_R8_LINE_INT = 4  ' Line Interrupt Enable
CONST VDP_R8_VSCROLL_2X = 7  ' Vertical Scroll 2x Speed
CONST VDP_R9 = 0x09
CONST VDP_R10 = 0x0A
CONST VDP_R11 = 0x0B
CONST VDP_R12 = 0x0C
CONST VDP_R13 = 0x0D
CONST VDP_R14 = 0x0E
CONST VDP_R15 = 0x0F
CONST VDP_VCOUNTER = 0x7E
CONST VDP_HCOUNTER = 0x7F

' SN76489 Programmable Sound Generator (3 Square + 1 Noise)
CONST PSG_BASE = 0x7F
CONST PSG_CH0_FREQ = 0x00
CONST PSG_CH1_FREQ = 0x02
CONST PSG_CH2_FREQ = 0x04
CONST PSG_CH3_CONFIG = 0x06
CONST PSG_CH3_CONFIG_TYPE = 0  ' Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
CONST PSG_CH3_CONFIG_VOLUME = 0  ' Volume (0-15)
CONST PSG_CH0_VOLUME = 0x01
CONST PSG_CH1_VOLUME = 0x03
CONST PSG_CH2_VOLUME = 0x05

' I/O Port Registers
CONST PORTS_BASE = 0x3F
CONST PORTS_PORT_A = 0x3F
CONST PORTS_PORT_A_UP = 0  ' Up (0=pressed)
CONST PORTS_PORT_A_DOWN = 1  ' Down (0=pressed)
CONST PORTS_PORT_A_LEFT = 2  ' Left (0=pressed)
CONST PORTS_PORT_A_RIGHT = 3  ' Right (0=pressed)
CONST PORTS_PORT_A_TR = 4  ' Button TR (0=pressed)
CONST PORTS_PORT_A_TL = 5  ' Button TL (0=pressed)
CONST PORTS_PORT_B = 0x3F
CONST PORTS_PORT_B_UP = 0  ' Up (0=pressed)
CONST PORTS_PORT_B_DOWN = 1  ' Down (0=pressed)
CONST PORTS_PORT_B_LEFT = 2  ' Left (0=pressed)
CONST PORTS_PORT_B_RIGHT = 3  ' Right (0=pressed)
CONST PORTS_PORT_B_TR = 4  ' Button TR (0=pressed)
CONST PORTS_PORT_B_TL = 5  ' Button TL (0=pressed)
CONST PORTS_PORT_A_DDR = 0x3F
CONST PORTS_PORT_B_DDR = 0x3F

' Sega Mapper (Memory Bank Switching)
CONST SEGAMAPPER_BASE = 0xFFFD
CONST SEGAMAPPER_ROM_BANK0 = 0xFFFD
CONST SEGAMAPPER_ROM_BANK1 = 0xFFFE
CONST SEGAMAPPER_ROM_BANK2 = 0xFFFF

' Memory Mapper Control
CONST MAPPER_BASE = 0xFFFF
CONST MAPPER_SRAM_BANK = 0xFFF8

' 中断向量定义
CONST NMI_VECTOR = 0  ' Non-Maskable Interrupt (Pause button / V-Blank)
CONST INT_VBLANK_VECTOR = 1  ' V-Blank Interrupt (Frame end)
CONST INT_LINE_VECTOR = 2  ' Scanline Interrupt (Line counter match)
CONST INT_EXT_VECTOR = 3  ' External I/O Interrupt

' 引脚定义
CONST PIN_A = 1  ' Power Supply
CONST PIN_GND = 2  ' Ground
CONST PIN_PHI = 3  ' System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
CONST PIN_RESET = 4  ' Reset (active low)
CONST PIN_M1 = 5  ' Machine Cycle 1 (instruction fetch)
CONST PIN_MREQ = 6  ' Memory Request
CONST PIN_IORQ = 7  ' I/O Request
CONST PIN_RD = 8  ' Read Strobe
CONST PIN_WR = 9  ' Write Strobe
CONST PIN_HALT = 10  ' Halt State
CONST PIN_WAIT = 11  ' Wait State Request
CONST PIN_INT = 12  ' Interrupt Request (active low)
CONST PIN_NMI = 13  ' Non-Maskable Interrupt (active low)
CONST PIN_BUSRQ = 14  ' Bus Request (active low)
CONST PIN_BUSAK = 15  ' Bus Acknowledge (active low)
CONST PIN_A0 = 16  ' Address Bus Bit 0
CONST PIN_A1 = 17  ' Address Bus Bit 1
CONST PIN_A2 = 18  ' Address Bus Bit 2
CONST PIN_A3 = 19  ' Address Bus Bit 3
CONST PIN_A4 = 20  ' Address Bus Bit 4
CONST PIN_A5 = 21  ' Address Bus Bit 5
CONST PIN_A6 = 22  ' Address Bus Bit 6
CONST PIN_A7 = 23  ' Address Bus Bit 7
CONST PIN_A8 = 24  ' Address Bus Bit 8
CONST PIN_A9 = 25  ' Address Bus Bit 9
CONST PIN_A10 = 26  ' Address Bus Bit 10
CONST PIN_A11 = 27  ' Address Bus Bit 11
CONST PIN_A12 = 28  ' Address Bus Bit 12
CONST PIN_A13 = 29  ' Address Bus Bit 13
CONST PIN_A14 = 30  ' Address Bus Bit 14
CONST PIN_A15 = 31  ' Address Bus Bit 15
CONST PIN_D0 = 32  ' Data Bus Bit 0
CONST PIN_D1 = 33  ' Data Bus Bit 1
CONST PIN_D2 = 34  ' Data Bus Bit 2
CONST PIN_D3 = 35  ' Data Bus Bit 3
CONST PIN_D4 = 36  ' Data Bus Bit 4
CONST PIN_D5 = 37  ' Data Bus Bit 5
CONST PIN_D6 = 38  ' Data Bus Bit 6
CONST PIN_D7 = 39  ' Data Bus Bit 7
CONST PIN_AUDIO_OUT = 40  ' Audio Output
CONST PIN_VIDEO_SYNC = 41  ' Composite Video Sync
CONST PIN_VIDEO_OUT = 42  ' Composite Video Output

' 设备初始化子程序
SUB zilog_z80_init()
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
