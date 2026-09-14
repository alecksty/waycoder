' Sega-Genesis寄存器定义
' 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU

' CPU架构: Motorola 68000
' 位宽: 32位
' 时钟频率: 7670000 Hz

' 寄存器定义
' Data Register 0
CONST D0 = 0

' Data Register 1
CONST D1 = 0

' Data Register 2
CONST D2 = 0

' Data Register 3
CONST D3 = 0

' Data Register 4
CONST D4 = 0

' Data Register 5
CONST D5 = 0

' Data Register 6
CONST D6 = 0

' Data Register 7
CONST D7 = 0

' Address Register 0
CONST A0 = 0

' Address Register 1
CONST A1 = 0

' Address Register 2
CONST A2 = 0

' Address Register 3
CONST A3 = 0

' Address Register 4
CONST A4 = 0

' Address Register 5
CONST A5 = 0

' Address Register 6
CONST A6 = 0

' Address Register 7 (SP)
CONST A7 = 0

' Program Counter
CONST PC = 0

' Status Register
CONST SR = 0

' 外设定义
' Video Display Processor (315-5313)
CONST VDP_BASE = 
CONST VDP_VDP_DATA = 0xC00000
CONST VDP_VDP_CONTROL = 0xC00004
CONST VDP_VDP_HVCOUNTER = 0xC00008
CONST VDP_VDP_PSG = 0xC00011

' FM synthesis sound chip
CONST YM2612_BASE = 
CONST YM2612_YM2612_ADDR0 = 0xA04000
CONST YM2612_YM2612_DATA0 = 0xA04001
CONST YM2612_YM2612_ADDR1 = 0xA04002
CONST YM2612_YM2612_DATA1 = 0xA04003

' I/O ports
CONST IOPORTS_BASE = 
CONST IOPORTS_IO_DATA1 = 0xA10002
CONST IOPORTS_IO_DATA2 = 0xA10004
CONST IOPORTS_IO_DATA3 = 0xA10006
CONST IOPORTS_IO_CTRL1 = 0xA10008
CONST IOPORTS_IO_CTRL2 = 0xA1000A
CONST IOPORTS_IO_CTRL3 = 0xA1000C

' TradeMark Security System
CONST TMSS_BASE = 
CONST TMSS_TMSS = 0xA14000

' Z80 bus control
CONST Z80BUS_BASE = 
CONST Z80BUS_Z80_BUSREQ = 0xA11100
CONST Z80BUS_Z80_RESET = 0xA11200
CONST Z80BUS_Z80_YM2612 = 0xA04000

' 中断向量定义
CONST RESET_SP_VECTOR = 0  ' Reset (Initial SP)
CONST RESET_PC_VECTOR = 4  ' Reset (Initial PC)
CONST HBLANK_VECTOR = 24  ' Horizontal blank interrupt
CONST VBLANK_VECTOR = 28  ' Vertical blank interrupt
CONST EXTINT1_VECTOR = 32  ' External interrupt 1
CONST EXTINT2_VECTOR = 36  ' External interrupt 2
CONST EXTINT3_VECTOR = 40  ' External interrupt 3
CONST EXTINT4_VECTOR = 44  ' External interrupt 4
CONST EXTINT5_VECTOR = 48  ' External interrupt 5
CONST EXTINT6_VECTOR = 52  ' External interrupt 6
CONST EXTINT7_VECTOR = 56  ' External interrupt 7

' 设备初始化子程序
SUB sega_genesis_init()
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
