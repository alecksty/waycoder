' 8051寄存器定义
' 生成自: Intel/MCS-51/8051
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines

' CPU架构: MCS-51
' 位宽: 8位
' 时钟频率: 11059200 Hz

' 寄存器定义
' Accumulator
CONST ACC = 0xE0

' B Register
CONST B = 0xF0

' Program Status Word
CONST PSW = 0xD0
CONST PSW_P = 0  ' Parity Flag
CONST PSW_OV = 2  ' Overflow Flag
CONST PSW_RS0 = 3  ' Register Bank Select 0
CONST PSW_RS1 = 4  ' Register Bank Select 1
CONST PSW_F0 = 5  ' Flag 0
CONST PSW_AC = 6  ' Auxiliary Carry Flag
CONST PSW_CY = 7  ' Carry Flag

' Stack Pointer
CONST SP = 0x81

' Data Pointer (DPL/DPH)
CONST DPTR = 0x82

' 内存段定义
' Program Memory
CONST CODE_START = 0x0000
CONST CODE_END = 0x0FFF
CONST CODE_SIZE = 4096

' Internal Data Memory
CONST IDATA_START = 0x00
CONST IDATA_END = 0x7F
CONST IDATA_SIZE = 128

' Special Function Registers
CONST SFR_START = 0x80
CONST SFR_END = 0xFF
CONST SFR_SIZE = 128

' External Data Memory
CONST XDATA_START = 0x0000
CONST XDATA_END = 0xFFFF
CONST XDATA_SIZE = 65536

' 外设定义
' Port 0
CONST PORT0_BASE = 0x80
CONST PORT0_P0 = 0x80

' Port 1
CONST PORT1_BASE = 0x90
CONST PORT1_P1 = 0x90

' Port 2
CONST PORT2_BASE = 0xA0
CONST PORT2_P2 = 0xA0

' Port 3
CONST PORT3_BASE = 0xB0
CONST PORT3_P3 = 0xB0

' Timer/Counter 0
CONST TIMER0_BASE = 0x8A
CONST TIMER0_TH0 = 0x8C
CONST TIMER0_TL0 = 0x8A
CONST TIMER0_TMOD = 0x89
CONST TIMER0_TMOD_M0_0 = 0  ' Timer 0 Mode bit 0
CONST TIMER0_TMOD_M1_0 = 1  ' Timer 0 Mode bit 1
CONST TIMER0_TMOD_C_T0 = 2  ' Timer 0 Counter/Timer Select
CONST TIMER0_TMOD_GATE0 = 3  ' Timer 0 Gate Control
CONST TIMER0_TCON = 0x88
CONST TIMER0_TCON_TR0 = 4  ' Timer 0 Run Control
CONST TIMER0_TCON_TF0 = 5  ' Timer 0 Overflow Flag

' Serial Port
CONST UART_BASE = 0x98
CONST UART_SBUF = 0x99
CONST UART_SCON = 0x98
CONST UART_SCON_RI = 0  ' Receive Interrupt Flag
CONST UART_SCON_TI = 1  ' Transmit Interrupt Flag
CONST UART_SCON_REN = 4  ' Receive Enable
CONST UART_SCON_SM0 = 6  ' Serial Mode bit 0
CONST UART_SCON_SM1 = 7  ' Serial Mode bit 1

' 中断向量定义
CONST RESET_VECTOR = 0  ' Reset Vector
CONST INT0_VECTOR = 1  ' External Interrupt 0
CONST TIMER0_VECTOR = 2  ' Timer 0 Interrupt
CONST INT1_VECTOR = 3  ' External Interrupt 1
CONST TIMER1_VECTOR = 4  ' Timer 1 Interrupt
CONST UART_VECTOR = 5  ' Serial Port Interrupt

' 引脚定义
CONST PIN_P1_0 = 1  ' Port 1, bit 0
CONST PIN_P1_1 = 2  ' Port 1, bit 1
CONST PIN_P1_2 = 3  ' Port 1, bit 2
CONST PIN_P1_3 = 4  ' Port 1, bit 3
CONST PIN_P1_4 = 5  ' Port 1, bit 4
CONST PIN_P1_5 = 6  ' Port 1, bit 5
CONST PIN_P1_6 = 7  ' Port 1, bit 6
CONST PIN_P1_7 = 8  ' Port 1, bit 7
CONST PIN_RST = 9  ' Reset Pin
CONST PIN_RX = 10  ' Serial Receive (P3.0)
CONST PIN_TX = 11  ' Serial Transmit (P3.1)
CONST PIN_INT0 = 12  ' External Interrupt 0 (P3.2)
CONST PIN_INT1 = 13  ' External Interrupt 1 (P3.3)
CONST PIN_T0 = 14  ' Timer 0 Input (P3.4)
CONST PIN_T1 = 15  ' Timer 1 Input (P3.5)
CONST PIN_WR = 16  ' External Memory Write Strobe (P3.6)
CONST PIN_RD = 17  ' External Memory Read Strobe (P3.7)
CONST PIN_XTAL1 = 18  ' Crystal Oscillator Input
CONST PIN_XTAL2 = 19  ' Crystal Oscillator Output
CONST PIN_VCC = 20  ' Power Supply (+5V)
CONST PIN_GND = 21  ' Ground

' 设备初始化子程序
SUB _8051_init()
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
