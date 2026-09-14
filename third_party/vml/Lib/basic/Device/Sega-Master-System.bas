' Sega-Master-System寄存器定义
' 生成自: Sega/Master System/Sega-Master-System
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Sega Master System 8-bit video game console with Z80 CPU

' CPU架构: Zilog Z80
' 位宽: 8位
' 时钟频率: 3579545 Hz

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

' 外设定义
' Video Display Processor (TMS9918A)
CONST VDP_BASE = 
CONST VDP_VDP_DATA = 0xBE
CONST VDP_VDP_ADDR = 0xBF
CONST VDP_VDP_STATUS = 0xBF

' Programmable Sound Generator (SN76489)
CONST PSG_BASE = 
CONST PSG_PSG_DATA = 0x7F

' I/O ports
CONST IO_BASE = 
CONST IO_IO_PORT_A = 0xDC
CONST IO_IO_PORT_B = 0xDD
CONST IO_IO_PORT_MISC = 0xDE
CONST IO_IO_PORT_VDP = 0xDF

' Memory mapper
CONST MEMORYMAPPER_BASE = 
CONST MEMORYMAPPER_MAPPER_0 = 0xFFFC
CONST MEMORYMAPPER_MAPPER_1 = 0xFFFD
CONST MEMORYMAPPER_MAPPER_2 = 0xFFFE
CONST MEMORYMAPPER_MAPPER_3 = 0xFFFF

' FM Sound Unit (optional)
CONST FMUNIT_BASE = 
CONST FMUNIT_FM_ADDR = 0xF0
CONST FMUNIT_FM_DATA = 0xF1
CONST FMUNIT_FM_DETECT = 0xF2

' 中断向量定义
CONST RST_00_VECTOR = 0  ' Restart 00h
CONST IM1_VECTOR = 56  ' Interrupt Mode 1
CONST VBLANK_VECTOR = 56  ' Vertical blank interrupt
CONST LINE_VECTOR = 100  ' Line interrupt

' 设备初始化子程序
SUB sega_master_system_init()
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
