' IBM-PC-5150寄存器定义
' 生成自: IBM/Personal Computer/IBM-PC-5150
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: Original IBM Personal Computer Model 5150

' CPU架构: x86
' 位宽: 16位
' 时钟频率: 4772727 Hz

' 寄存器定义
' Accumulator Register
CONST AX = 0x0

' Base Register
CONST BX = 0x1

' Count Register
CONST CX = 0x2

' Data Register
CONST DX = 0x3

' Source Index
CONST SI = 0x4

' Destination Index
CONST DI = 0x5

' Base Pointer
CONST BP = 0x6

' Stack Pointer
CONST SP = 0x7

' Code Segment
CONST CS = 0x8

' Data Segment
CONST DS = 0x9

' Extra Segment
CONST ES = 0xA

' Stack Segment
CONST SS = 0xB

' Instruction Pointer
CONST IP = 0xC

' Flags Register
CONST FLAGS = 0xD
CONST FLAGS_CF = 0  ' Carry Flag
CONST FLAGS_PF = 2  ' Parity Flag
CONST FLAGS_AF = 4  ' Auxiliary Carry Flag
CONST FLAGS_ZF = 6  ' Zero Flag
CONST FLAGS_SF = 7  ' Sign Flag
CONST FLAGS_TF = 8  ' Trap Flag
CONST FLAGS_IF = 9  ' Interrupt Enable Flag
CONST FLAGS_DF = 10  ' Direction Flag
CONST FLAGS_OF = 11  ' Overflow Flag

' 内存段定义
' BIOS ROM
CONST BIOS_START = 0xF0000
CONST BIOS_END = 0xFFFFF
CONST BIOS_SIZE = 65536

' Video Memory
CONST VIDEO_START = 0xB8000
CONST VIDEO_END = 0xBFFFF
CONST VIDEO_SIZE = 32768

' Conventional Memory (640KB)
CONST CONVENTIONAL_START = 0x00000
CONST CONVENTIONAL_END = 0x9FFFF
CONST CONVENTIONAL_SIZE = 640

' Extended Memory (64KB)
CONST EXTENDED_START = 0x100000
CONST EXTENDED_END = 0x10FFFF
CONST EXTENDED_SIZE = 64

' 外设定义
' Programmable Interrupt Controller
CONST PIC_BASE = 0x20
CONST PIC_PIC1_CMD = 0x20
CONST PIC_PIC1_DATA = 0x21
CONST PIC_PIC2_CMD = 0xA0
CONST PIC_PIC2_DATA = 0xA1

' Programmable Interval Timer
CONST PIT_BASE = 0x40
CONST PIT_PIT_CH0 = 0x40
CONST PIT_PIT_CH1 = 0x41
CONST PIT_PIT_CH2 = 0x42
CONST PIT_PIT_CTRL = 0x43

' Programmable Peripheral Interface
CONST PPI_BASE = 0x60
CONST PPI_PPI_PA = 0x60
CONST PPI_PPI_PB = 0x61
CONST PPI_PPI_PC = 0x62
CONST PPI_PPI_CTRL = 0x63

' Direct Memory Access Controller
CONST DMA_BASE = 0x00
CONST DMA_DMA_CH0_ADDR = 0x00
CONST DMA_DMA_CH0_COUNT = 0x01
CONST DMA_DMA_CMD = 0x08
CONST DMA_DMA_MASK = 0x0A
CONST DMA_DMA_MODE = 0x0B

' Color Graphics Adapter
CONST CGA_BASE = 0x3D4
CONST CGA_CGA_INDEX = 0x3D4
CONST CGA_CGA_DATA = 0x3D5
CONST CGA_CGA_MODE = 0x3D8
CONST CGA_CGA_COLOR = 0x3D9

' 中断向量定义
CONST DIVIDE_ERROR_VECTOR = 0  ' Divide Error
CONST SINGLE_STEP_VECTOR = 1  ' Single Step
CONST NMI_VECTOR = 2  ' Non-Maskable Interrupt
CONST BREAKPOINT_VECTOR = 3  ' Breakpoint
CONST OVERFLOW_VECTOR = 4  ' Overflow
CONST PRINT_SCREEN_VECTOR = 5  ' Print Screen
CONST IRQ0_VECTOR = 8  ' Timer Interrupt
CONST IRQ1_VECTOR = 9  ' Keyboard Interrupt
CONST IRQ2_VECTOR = 10  ' Cascade (8259A)
CONST IRQ3_VECTOR = 11  ' COM2
CONST IRQ4_VECTOR = 12  ' COM1
CONST IRQ5_VECTOR = 13  ' LPT2
CONST IRQ6_VECTOR = 14  ' Floppy Disk
CONST IRQ7_VECTOR = 15  ' LPT1
CONST IRQ8_VECTOR = 16  ' Real Time Clock
CONST IRQ11_VECTOR = 19  ' Reserved
CONST IRQ13_VECTOR = 21  ' Coprocessor
CONST IRQ15_VECTOR = 31  ' Reserved

' 引脚定义
CONST PIN_VCC = 1  ' +5V Power Supply
CONST PIN_GND = 2  ' Ground
CONST PIN_RESET = 3  ' System Reset
CONST PIN_CLK = 4  ' System Clock (4.77MHz)
CONST PIN_READY = 5  ' CPU Ready Signal
CONST PIN_NMI = 6  ' Non-Maskable Interrupt
CONST PIN_INTR = 7  ' Interrupt Request
CONST PIN_HLDA = 8  ' Hold Acknowledge
CONST PIN_HOLD = 9  ' Hold Request
CONST PIN_MEMR = 10  ' Memory Read
CONST PIN_MEMW = 11  ' Memory Write
CONST PIN_IOR = 12  ' I/O Read
CONST PIN_IOW = 13  ' I/O Write
CONST PIN_ALE = 14  ' Address Latch Enable
CONST PIN_DTR = 15  ' Data Terminal Ready (Serial)
CONST PIN_RTS = 16  ' Request To Send (Serial)
CONST PIN_CTS = 17  ' Clear To Send (Serial)
CONST PIN_DSR = 18  ' Data Set Ready (Serial)
CONST PIN_RI = 19  ' Ring Indicator (Serial)
CONST PIN_DCD = 20  ' Data Carrier Detect (Serial)

' 设备初始化子程序
SUB ibm_pc_5150_init()
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
