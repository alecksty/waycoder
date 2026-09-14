# 8051 设备定义 - R 脚本
# 生成自: Intel/MCS-51/8051
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
# CPU架构: MCS-51
# 位宽: 8位
# 时钟频率: 11059200 Hz

# 寄存器地址定义
ACC_ADDR <- 0xE0  # Accumulator
B_ADDR <- 0xF0  # B Register
PSW_ADDR <- 0xD0  # Program Status Word
PSW_P_BIT <- 0  # Parity Flag
PSW_OV_BIT <- 2  # Overflow Flag
PSW_RS0_BIT <- 3  # Register Bank Select 0
PSW_RS1_BIT <- 4  # Register Bank Select 1
PSW_F0_BIT <- 5  # Flag 0
PSW_AC_BIT <- 6  # Auxiliary Carry Flag
PSW_CY_BIT <- 7  # Carry Flag
SP_ADDR <- 0x81  # Stack Pointer
DPTR_ADDR <- 0x82  # Data Pointer (DPL/DPH)

# 内存段定义
CODE_START <- 0x0000
CODE_END <- 0x0FFF
CODE_SIZE <- 4096  # Program Memory
IDATA_START <- 0x00
IDATA_END <- 0x7F
IDATA_SIZE <- 128  # Internal Data Memory
SFR_START <- 0x80
SFR_END <- 0xFF
SFR_SIZE <- 128  # Special Function Registers
XDATA_START <- 0x0000
XDATA_END <- 0xFFFF
XDATA_SIZE <- 65536  # External Data Memory

# 外设定义
# Port 0
PORT0_BASE <- 0x80
PORT0_P0_ADDR <- 0x80
# Port 1
PORT1_BASE <- 0x90
PORT1_P1_ADDR <- 0x90
# Port 2
PORT2_BASE <- 0xA0
PORT2_P2_ADDR <- 0xA0
# Port 3
PORT3_BASE <- 0xB0
PORT3_P3_ADDR <- 0xB0
# Timer/Counter 0
TIMER0_BASE <- 0x8A
TIMER0_TH0_ADDR <- 0x8C
TIMER0_TL0_ADDR <- 0x8A
TIMER0_TMOD_ADDR <- 0x89
TIMER0_TMOD_M0_0_BIT <- 0  # Timer 0 Mode bit 0
TIMER0_TMOD_M1_0_BIT <- 1  # Timer 0 Mode bit 1
TIMER0_TMOD_C_T0_BIT <- 2  # Timer 0 Counter/Timer Select
TIMER0_TMOD_GATE0_BIT <- 3  # Timer 0 Gate Control
TIMER0_TCON_ADDR <- 0x88
TIMER0_TCON_TR0_BIT <- 4  # Timer 0 Run Control
TIMER0_TCON_TF0_BIT <- 5  # Timer 0 Overflow Flag
# Serial Port
UART_BASE <- 0x98
UART_SBUF_ADDR <- 0x99
UART_SCON_ADDR <- 0x98
UART_SCON_RI_BIT <- 0  # Receive Interrupt Flag
UART_SCON_TI_BIT <- 1  # Transmit Interrupt Flag
UART_SCON_REN_BIT <- 4  # Receive Enable
UART_SCON_SM0_BIT <- 6  # Serial Mode bit 0
UART_SCON_SM1_BIT <- 7  # Serial Mode bit 1

# 中断向量定义
INT_RESET <- 0  # Reset Vector
INT_INT0 <- 1  # External Interrupt 0
INT_TIMER0 <- 2  # Timer 0 Interrupt
INT_INT1 <- 3  # External Interrupt 1
INT_TIMER1 <- 4  # Timer 1 Interrupt
INT_UART <- 5  # Serial Port Interrupt

# 引脚定义
PIN_P1_0 <- 1  # Port 1, bit 0
PIN_P1_1 <- 2  # Port 1, bit 1
PIN_P1_2 <- 3  # Port 1, bit 2
PIN_P1_3 <- 4  # Port 1, bit 3
PIN_P1_4 <- 5  # Port 1, bit 4
PIN_P1_5 <- 6  # Port 1, bit 5
PIN_P1_6 <- 7  # Port 1, bit 6
PIN_P1_7 <- 8  # Port 1, bit 7
PIN_RST <- 9  # Reset Pin
PIN_RX <- 10  # Serial Receive (P3.0)
PIN_TX <- 11  # Serial Transmit (P3.1)
PIN_INT0 <- 12  # External Interrupt 0 (P3.2)
PIN_INT1 <- 13  # External Interrupt 1 (P3.3)
PIN_T0 <- 14  # Timer 0 Input (P3.4)
PIN_T1 <- 15  # Timer 1 Input (P3.5)
PIN_WR <- 16  # External Memory Write Strobe (P3.6)
PIN_RD <- 17  # External Memory Read Strobe (P3.7)
PIN_XTAL1 <- 18  # Crystal Oscillator Input
PIN_XTAL2 <- 19  # Crystal Oscillator Output
PIN_VCC <- 20  # Power Supply (+5V)
PIN_GND <- 21  # Ground

