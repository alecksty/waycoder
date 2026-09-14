' 8086寄存器定义
' 生成自: Intel/x86/8086
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: 16-bit microprocessor, first x86 processor

' CPU架构: x86
' 位宽: 16位
' 时钟频率: 5000000 Hz

' 寄存器定义
' Accumulator
CONST AX = 0
CONST AX_AH = 8  ' High byte of AX
CONST AX_AL = 0  ' Low byte of AX

' Base
CONST BX = 1
CONST BX_BH = 8  ' High byte of BX
CONST BX_BL = 0  ' Low byte of BX

' Counter
CONST CX = 2
CONST CX_CH = 8  ' High byte of CX
CONST CX_CL = 0  ' Low byte of CX

' Data
CONST DX = 3
CONST DX_DH = 8  ' High byte of DX
CONST DX_DL = 0  ' Low byte of DX

' Source Index
CONST SI = 4

' Destination Index
CONST DI = 5

' Base Pointer
CONST BP = 6

' Stack Pointer
CONST SP = 7

' Instruction Pointer
CONST IP = 8

' Code Segment
CONST CS = 9

' Data Segment
CONST DS = 10

' Extra Segment
CONST ES = 11

' Stack Segment
CONST SS = 12

' Flags Register
CONST FLAGS = 13
CONST FLAGS_CF = 0  ' Carry Flag
CONST FLAGS_PF = 2  ' Parity Flag
CONST FLAGS_AF = 4  ' Auxiliary Flag
CONST FLAGS_ZF = 6  ' Zero Flag
CONST FLAGS_SF = 7  ' Sign Flag
CONST FLAGS_TF = 8  ' Trap Flag
CONST FLAGS_IF = 9  ' Interrupt Enable Flag
CONST FLAGS_DF = 10  ' Direction Flag
CONST FLAGS_OF = 11  ' Overflow Flag

' 内存段定义
' 1MB address space
CONST CODE_START = 0x00000
CONST CODE_END = 0xFFFFF
CONST CODE_SIZE = 1048576

' Data memory
CONST DATA_START = 0x00000
CONST DATA_END = 0xFFFFF
CONST DATA_SIZE = 1048576

' Stack memory
CONST STACK_START = 0xF0000
CONST STACK_END = 0xFFFFF
CONST STACK_SIZE = 65536

' BIOS ROM
CONST BIOS_START = 0xF0000
CONST BIOS_END = 0xFFFFF
CONST BIOS_SIZE = 65536

' 外设定义
' Programmable Interrupt Controller
CONST PIC_BASE = 0x0020
CONST PIC_PIC1_CMD = 0x0020
CONST PIC_PIC1_DATA = 0x0021
CONST PIC_PIC2_CMD = 0x00A0
CONST PIC_PIC2_DATA = 0x00A1

' Programmable Interval Timer
CONST PIT_BASE = 0x0040
CONST PIT_PIT_CH0 = 0x0040
CONST PIT_PIT_CH1 = 0x0041
CONST PIT_PIT_CH2 = 0x0042
CONST PIT_PIT_CMD = 0x0043

' Programmable Peripheral Interface
CONST PPI_BASE = 0x0060
CONST PPI_PPI_PA = 0x0060
CONST PPI_PPI_PB = 0x0061
CONST PPI_PPI_PC = 0x0062
CONST PPI_PPI_CMD = 0x0063

' 中断向量定义
CONST DIVIDE_ERROR_VECTOR = 0  ' Divide by zero
CONST DEBUG_VECTOR = 1  ' Single step
CONST NMI_VECTOR = 2  ' Non-maskable interrupt
CONST BREAKPOINT_VECTOR = 3  ' Breakpoint
CONST OVERFLOW_VECTOR = 4  ' INTO detected overflow
CONST IRQ0_VECTOR = 8  ' Timer interrupt
CONST IRQ1_VECTOR = 9  ' Keyboard interrupt
CONST IRQ2_VECTOR = 10  ' Cascade
CONST IRQ3_VECTOR = 11  ' COM2
CONST IRQ4_VECTOR = 12  ' COM1
CONST IRQ5_VECTOR = 13  ' LPT2
CONST IRQ6_VECTOR = 14  ' Floppy disk
CONST IRQ7_VECTOR = 15  ' LPT1

' 设备初始化子程序
SUB _8086_init()
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
