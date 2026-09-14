' ZX-Spectrum寄存器定义
' 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics

' CPU架构: Zilog Z80
' 位宽: 8位
' 时钟频率: 3500000 Hz

' 寄存器定义
' Accumulator
CONST A = 0

' Flags
CONST F = 0

' B
CONST B = 0

' C
CONST C = 0

' D
CONST D = 0

' E
CONST E = 0

' H
CONST H = 0

' L
CONST L = 0

' Index Register X
CONST IX = 0

' Index Register Y
CONST IY = 0

' Stack Pointer
CONST SP = 0

' Program Counter
CONST PC = 0

' Interrupt Vector
CONST I = 0

' Memory Refresh
CONST R = 0

' Alternate AF
CONST AF = 0

' Alternate BC
CONST BC = 0

' Alternate DE
CONST DE = 0

' Alternate HL
CONST HL = 0

' 外设定义
' Uncommitted Logic Array (video and I/O)
CONST ULA_BASE = 
CONST ULA_ULA_PORT_FE = 0xFE
CONST ULA_ULA_BORDER = 0xFE
CONST ULA_ULA_BEEPER = 0xFE
CONST ULA_ULA_MIC = 0xFE

' General Instruments AY-3-8912 sound chip
CONST AY_3_8912_BASE = 
CONST AY_3_8912_AY_REG_SEL = 0xFFFD
CONST AY_3_8912_AY_DATA = 0xBFFD
CONST AY_3_8912_AY_READ = 0xFFFD

' 40-key rubber keyboard
CONST KEYBOARD_BASE = 
CONST KEYBOARD_KEY_ROW0 = 0xFEFE
CONST KEYBOARD_KEY_ROW1 = 0xFDFE
CONST KEYBOARD_KEY_ROW2 = 0xFBFE
CONST KEYBOARD_KEY_ROW3 = 0xF7FE
CONST KEYBOARD_KEY_ROW4 = 0xEFFE
CONST KEYBOARD_KEY_ROW5 = 0xDFFE
CONST KEYBOARD_KEY_ROW6 = 0xBFFE
CONST KEYBOARD_KEY_ROW7 = 0x7FFE

' Kempston joystick interface
CONST KEMPSTON_BASE = 
CONST KEMPSTON_KEMPSTON_JOY = 0x1F

' ZX Interface 1 (RS-232 and Microdrive)
CONST INTERFACE1_BASE = 
CONST INTERFACE1_IF1_STATUS = 0x1FFD
CONST INTERFACE1_IF1_DATA = 0x3FFD

' ZX Interface 2 (joystick and ROM cartridge)
CONST INTERFACE2_BASE = 
CONST INTERFACE2_IF2_JOY1 = 0x1F
CONST INTERFACE2_IF2_JOY2 = 0x37

' 中断向量定义
CONST IM1_VECTOR = 56  ' Interrupt Mode 1
CONST RST_00_VECTOR = 0  ' Restart 00h
CONST RST_08_VECTOR = 8  ' Restart 08h
CONST RST_10_VECTOR = 16  ' Restart 10h
CONST RST_18_VECTOR = 24  ' Restart 18h
CONST RST_20_VECTOR = 32  ' Restart 20h
CONST RST_28_VECTOR = 40  ' Restart 28h
CONST RST_30_VECTOR = 48  ' Restart 30h
CONST RST_38_VECTOR = 56  ' Restart 38h

' 设备初始化子程序
SUB zx_spectrum_init()
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
