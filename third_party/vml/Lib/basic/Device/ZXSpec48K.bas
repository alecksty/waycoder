' ZX-Spectrum-48K寄存器定义
' 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics

' CPU架构: Z80A
' 位宽: 8位
' 时钟频率: 3500000 Hz

' 寄存器定义
' Accumulator
CONST A = 0x00

' Flags Register
CONST F = 0x01
CONST F_C = 0  ' Carry
CONST F_N = 1  ' Add/Subtract
CONST F_PV = 2  ' Parity/Overflow
CONST F_H = 4  ' Half Carry
CONST F_Z = 6  ' Zero
CONST F_S = 7  ' Sign

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

' Interrupt Vector Register
CONST I = 0x10

' Refresh Counter
CONST R = 0x11

' Index X
CONST IX = 0x12

' Index Y
CONST IY = 0x14

' Stack Pointer
CONST SP = 0x16

' Program Counter
CONST PC = 0x18

' 内存段定义
' 48KB ZX Spectrum ROM (BASIC + monitor)
CONST ROM_START = 0x0000
CONST ROM_END = 0x3FFF
CONST ROM_SIZE = 16384

' Display file (256x192 bitmap)
CONST VIDEO_RAM_START = 0x4000
CONST VIDEO_RAM_END = 0x57FF
CONST VIDEO_RAM_SIZE = 6144

' Attribute file (32x24 color cells)
CONST ATTR_RAM_START = 0x5800
CONST ATTR_RAM_END = 0x5AFF
CONST ATTR_RAM_SIZE = 768

' User RAM (40KB)
CONST USER_RAM_START = 0x5B00
CONST USER_RAM_END = 0xFFFF
CONST USER_RAM_SIZE = 40960

' 外设定义
' Uncommitted Logic Array - Sinclair custom IC
CONST ULA_BASE = 0xFE
CONST ULA_BORDER = 0xFE
CONST ULA_KBD_ROW0 = 0xFE
CONST ULA_KBD_ROW1 = 0xFE
CONST ULA_KBD_ROW2 = 0xFE
CONST ULA_KBD_ROW3 = 0xFE
CONST ULA_KBD_ROW4 = 0xFE
CONST ULA_KBD_ROW5 = 0xFE
CONST ULA_KBD_ROW6 = 0xFE
CONST ULA_KBD_ROW7 = 0xFE
CONST ULA_KBD_ROW8 = 0xFE

' Keyboard Matrix (40 keys, 8 rows x 5 cols)
CONST KEYBOARD_BASE = 0xFE
CONST KEYBOARD_KBD_IN = 0xFE

' Internal Beeper
CONST BEEPER_BASE = 0xFE
CONST BEEPER_BEEP = 0xFE

' Tape Interface
CONST TAPE_BASE = 0xFE
CONST TAPE_EAR_IN = 0xFE
CONST TAPE_MIC_OUT = 0xFE

' Kempston Joystick Interface
CONST JOYSTICK_BASE = 0xF7FE
CONST JOYSTICK_KEMPSTON = 0xF7FE

' 中断向量定义
CONST RESET_VECTOR = 0  ' Power-on / Reset
CONST NMI_VECTOR = 1  ' Non-Maskable Interrupt (BREAK key)
CONST INT_VECTOR = 2  ' Maskable Interrupt (ULA vertical blank, 50Hz)

' 引脚定义
CONST PIN_VCC = 1  ' +5V Power
CONST PIN_GND = 2  ' Ground
CONST PIN_CLK = 3  ' Z80 Clock (3.5MHz)
CONST PIN_M1 = 4  ' Machine Cycle 1
CONST PIN_MREQ = 5  ' Memory Request
CONST PIN_IORQ = 6  ' I/O Request
CONST PIN_RD = 7  ' Read
CONST PIN_WR = 8  ' Write
CONST PIN_HALT = 9  ' Halt State
CONST PIN_BUSAK = 10  ' Bus Acknowledge
CONST PIN_WAIT = 11  ' Wait State (ULA inserts)
CONST PIN_INT = 12  ' Interrupt Request
CONST PIN_NMI = 13  ' Non-Maskable Interrupt
CONST PIN_RESET = 14  ' Reset
CONST PIN_A0_A15 = 15  ' Address Bus (16-bit)
CONST PIN_D0_D7 = 16  ' Data Bus (8-bit)

' 设备初始化子程序
SUB zx_spectrum_48k_init()
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
