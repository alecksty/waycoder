' Motorola-68000寄存器定义
' 生成自: Motorola/68000/Motorola-68000
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh

' CPU架构: MC68000
' 位宽: 32位
' 时钟频率: 7670452 Hz

' 寄存器定义
' Data Register 0
CONST D0 = 0x00

' Data Register 1
CONST D1 = 0x04

' Data Register 2
CONST D2 = 0x08

' Data Register 3
CONST D3 = 0x0C

' Data Register 4
CONST D4 = 0x10

' Data Register 5
CONST D5 = 0x14

' Data Register 6
CONST D6 = 0x18

' Data Register 7
CONST D7 = 0x1C

' Address Register 0
CONST A0 = 0x20

' Address Register 1
CONST A1 = 0x24

' Address Register 2
CONST A2 = 0x28

' Address Register 3
CONST A3 = 0x2C

' Address Register 4
CONST A4 = 0x30

' Address Register 5
CONST A5 = 0x34

' Address Register 6
CONST A6 = 0x38

' Stack Pointer (USP)
CONST A7 = 0x3C

' Program Counter
CONST PC = 0x40

' Status Register
CONST SR = 0x44
CONST SR_C = 0  ' Carry
CONST SR_V = 1  ' Overflow
CONST SR_Z = 2  ' Zero
CONST SR_N = 3  ' Negative
CONST SR_X = 4  ' Extend
CONST SR_I0 = 8  ' Interrupt Mask 0
CONST SR_I1 = 9  ' Interrupt Mask 1
CONST SR_I2 = 10  ' Interrupt Mask 2
CONST SR_M = 11  ' Master/Interrupt
CONST SR_S = 13  ' Supervisor/User
CONST SR_T0 = 14  ' Trace Mode 0
CONST SR_T1 = 15  ' Trace Mode 1

' 内存段定义
' System RAM (4MB)
CONST RAM_START = 0x000000
CONST RAM_END = 0x3FFFFF
CONST RAM_SIZE = 4194304

' Cartridge ROM
CONST ROM_START = 0x000000
CONST ROM_END = 0x3FFFFF
CONST ROM_SIZE = 4194304

' I/O Register Area
CONST IO_START = 0xA00000
CONST IO_END = 0xA1FFFF
CONST IO_SIZE = 131072

' VDP Registers
CONST VDP_START = 0xC00000
CONST VDP_END = 0xC0001F
CONST VDP_SIZE = 32

' Video RAM (256KB)
CONST VRAM_START = 0xE00000
CONST VRAM_END = 0xE3FFFF
CONST VRAM_SIZE = 262144

' 外设定义
' Video Display Processor (TMS9918A variant)
CONST VDP_BASE = 0xC00000
CONST VDP_DATA = 0x00
CONST VDP_CTRL = 0x04
CONST VDP_HVCOUNT = 0x08
CONST VDP_HVB_STATUS = 0x0A

' Programmable Sound Generator (AY-3-8910)
CONST PSG_BASE = 0xC00011
CONST PSG_CH_A_FREQ = 0x00
CONST PSG_CH_A_VOL = 0x08
CONST PSG_CH_B_FREQ = 0x02
CONST PSG_CH_B_VOL = 0x09
CONST PSG_CH_C_FREQ = 0x04
CONST PSG_CH_C_VOL = 0x0A
CONST PSG_NOISE_FREQ = 0x06
CONST PSG_MIXER = 0x07
CONST PSG_ENV_FREQ = 0x0D
CONST PSG_ENV_SHAPE = 0x0B

' Z80 Secondary CPU (Sound)
CONST Z80_BASE = 0xA00000
CONST Z80_Z80_RESET = 0x00
CONST Z80_Z80_BUSREQ = 0x04
CONST Z80_Z80_STATUS = 0x08

' Bank Register
CONST BANK_REG_BASE = 0xA12000
CONST BANK_REG_ROM_BANK = 0x00
CONST BANK_REG_RAM_BANK = 0x04

' Hardware Version
CONST HW_VERSION_BASE = 0xA10001
CONST HW_VERSION_VERSION = 0x00

' Controller Port 1
CONST CONTROLLER1_BASE = 0xA10003
CONST CONTROLLER1_DATA = 0x00
CONST CONTROLLER1_CTRL = 0x04

' Controller Port 2
CONST CONTROLLER2_BASE = 0xA10005
CONST CONTROLLER2_DATA = 0x00
CONST CONTROLLER2_CTRL = 0x04

' External Port
CONST EXT_PORT_BASE = 0xA10007
CONST EXT_PORT_DATA = 0x00

' DMA Controller
CONST DMA_BASE = 0xA10008
CONST DMA_SOURCE = 0x00
CONST DMA_DEST = 0x04
CONST DMA_COUNT = 0x08
CONST DMA_CTRL = 0x0A

' Hardware Timer
CONST TIMER_BASE = 0xA1000E
CONST TIMER_H_COUNTER = 0x00
CONST TIMER_V_COUNTER = 0x04

' 中断向量定义
CONST RESET_SP_VECTOR = 1  ' Reset Initial Stack Pointer
CONST RESET_PC_VECTOR = 2  ' Reset Initial PC
CONST BUS_ERROR_VECTOR = 3  ' Bus Error
CONST ADDRESS_ERROR_VECTOR = 4  ' Address Error
CONST ILLEGAL_INSTR_VECTOR = 5  ' Illegal Instruction
CONST ZERO_DIVIDE_VECTOR = 6  ' Zero Divide
CONST CHK_EXCEPTION_VECTOR = 7  ' CHK Exception
CONST TRAPV_VECTOR = 8  ' TRAPV Exception
CONST PRIVILEGE_VECTOR = 9  ' Privilege Violation
CONST TRACE_VECTOR = 10  ' Trace
CONST LINE_A_VECTOR = 11  ' Line 1010 Emulator
CONST LINE_F_VECTOR = 12  ' Line 1111 Emulator
CONST IRQ1_VECTOR = 24  ' External Interrupt 1 (H-Blank)
CONST IRQ2_VECTOR = 25  ' External Interrupt 2 (V-Blank)
CONST IRQ3_VECTOR = 26  ' External Interrupt 3
CONST IRQ4_VECTOR = 27  ' External Interrupt 4 (D-Req)
CONST IRQ5_VECTOR = 28  ' External Interrupt 5
CONST IRQ6_VECTOR = 29  ' External Interrupt 6
CONST IRQ7_VECTOR = 30  ' External Interrupt 7
CONST TRAP0_VECTOR = 32  ' TRAP #0
CONST TRAP1_VECTOR = 33  ' TRAP #1
CONST TRAP15_VECTOR = 47  ' TRAP #15

' 设备初始化子程序
SUB motorola_68000_init()
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
