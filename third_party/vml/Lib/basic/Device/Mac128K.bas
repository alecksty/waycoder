' Macintosh-128K寄存器定义
' 生成自: Apple Computer/Macintosh/Macintosh-128K
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display

' CPU架构: MC68000
' 位宽: 32位
' 时钟频率: 7833600 Hz

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
CONST SR_S = 13  ' Supervisor/User
CONST SR_T0 = 14  ' Trace Mode 0
CONST SR_T1 = 15  ' Trace Mode 1

' 内存段定义
' Main RAM (128KB unified)
CONST RAM_START = 0x000000
CONST RAM_END = 0x01FFFF
CONST RAM_SIZE = 131072

' Mac ROM (128KB)
CONST ROM_START = 0x40000000
CONST ROM_END = 0x4001FFFF
CONST ROM_SIZE = 131072

' Screen bitmap (512x342x1 = 21792 bytes)
CONST FRAMEBUFFER_START = 0x00400000
CONST FRAMEBUFFER_END = 0x00400555
CONST FRAMEBUFFER_SIZE = 1366

' Shadow screen (double-buffering)
CONST FRAMEBUFFER2_START = 0x00410000
CONST FRAMEBUFFER2_END = 0x00410555
CONST FRAMEBUFFER2_SIZE = 1366

' VIA 6522 (I/O)
CONST VIA_START = 0x00E00000
CONST VIA_END = 0x00E0FFFF
CONST VIA_SIZE = 4096

' SCC 8530 (serial)
CONST SCC_START = 0x00F00000
CONST SCC_END = 0x00F0FFFF
CONST SCC_SIZE = 4096

' ADB bus
CONST ADB_START = 0x01600000
CONST ADB_END = 0x0160FFFF
CONST ADB_SIZE = 4096

' IWM floppy controller
CONST IWM_START = 0x01E00000
CONST IWM_END = 0x01E0FFFF
CONST IWM_SIZE = 4096

' 外设定义
' Versatile Interface Adapter 6522
CONST VIA_BASE = 0xE00000
CONST VIA_ORB = 0xE00000
CONST VIA_ORA = 0xE00002
CONST VIA_DDRB = 0xE00004
CONST VIA_DDRA = 0xE00006
CONST VIA_T1C_L = 0xE00008
CONST VIA_T1C_H = 0xE0000A
CONST VIA_T1L_L = 0xE0000C
CONST VIA_T1L_H = 0xE0000E
CONST VIA_T2C_L = 0xE00010
CONST VIA_T2C_H = 0xE00012
CONST VIA_SR = 0xE00014
CONST VIA_ACR = 0xE00016
CONST VIA_PCR = 0xE00018
CONST VIA_IFR = 0xE0001E
CONST VIA_IER = 0xE0001E

' SCC 8530 Serial Communications Controller
CONST SCC_BASE = 0xF00000
CONST SCC_SCC_CHA_B = 0xF00000
CONST SCC_SCC_CHA_C = 0xF00002
CONST SCC_SCC_CHB_D = 0xF00004
CONST SCC_SCC_CHB_CT = 0xF00006

' Integrated Woz Machine - Floppy Disk Controller
CONST IWM_BASE = 0x1E00000
CONST IWM_IWM_DATA = 0x1E00000
CONST IWM_IWM_MODE = 0x1E00008
CONST IWM_IWM_Q6L = 0x1E00020
CONST IWM_IWM_Q7L = 0x1E00022
CONST IWM_IWM_Q6R = 0x1E00024
CONST IWM_IWM_Q7R = 0x1E00026

' Video Graphics Controller (custom Apple chip)
CONST VGC_BASE = 0x00F20000
CONST VGC_VGC_MODE = 0x00F20000
CONST VGC_VGC_START_HI = 0x00F20002
CONST VGC_VGC_START_LO = 0x00F20004

' Apple Desktop Bus
CONST ADB_BASE = 0x01600000
CONST ADB_ADB_DATA = 0x01600000
CONST ADB_ADB_STATUS = 0x01600004
CONST ADB_ADB_CMD = 0x01600008

' 中断向量定义
CONST RESET_VECTOR = 1  ' Reset Initial SP
CONST RESET_PC_VECTOR = 2  ' Reset Initial PC
CONST IRQ1_VECTOR = 24  ' VIA interrupt (level 1)
CONST IRQ2_VECTOR = 25  ' SCC interrupt (level 2)
CONST IRQ3_VECTOR = 26  ' ADB / VIA (level 3)
CONST IRQ4_VECTOR = 27  ' ADB / VIA (level 4)

' 引脚定义
CONST PIN_VCC = 1  ' +5V Power
CONST PIN_GND = 2  ' Ground
CONST PIN_CLK = 3  ' 16MHz master clock / 7.83MHz CPU clock
CONST PIN_FC0 = 4  ' Function Code 0
CONST PIN_FC1 = 5  ' Function Code 1
CONST PIN_FC2 = 6  ' Function Code 2
CONST PIN_AS = 7  ' Address Strobe
CONST PIN_UDS = 8  ' Upper Data Strobe
CONST PIN_LDS = 9  ' Lower Data Strobe
CONST PIN_RWB = 10  ' Read/Write
CONST PIN_DTACK = 11  ' Data Acknowledge
CONST PIN_BERR = 12  ' Bus Error
CONST PIN_BR = 13  ' Bus Request
CONST PIN_BG = 14  ' Bus Grant
CONST PIN_BGACK = 15  ' Bus Grant Acknowledge
CONST PIN_IPL0 = 16  ' Interrupt Priority 0
CONST PIN_IPL1 = 17  ' Interrupt Priority 1
CONST PIN_IPL2 = 18  ' Interrupt Priority 2
CONST PIN_RESET = 19  ' Reset
CONST PIN_HALT = 20  ' Halt
CONST PIN_A1_A23 = 21  ' Address Bus (24-bit)
CONST PIN_D0_D15 = 22  ' Data Bus (16-bit)

' 设备初始化子程序
SUB macintosh_128k_init()
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
