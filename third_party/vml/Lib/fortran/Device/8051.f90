! 8051 设备定义 - Fortran 模块
! 生成自: Intel/MCS-51/8051
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
! CPU架构: MCS-51
! 位宽: 8位
! 时钟频率: 11059200 Hz

module 8051_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: ACC_ADDR = 0xE0  ! Accumulator
  integer, parameter :: B_ADDR = 0xF0  ! B Register
  integer, parameter :: PSW_ADDR = 0xD0  ! Program Status Word
  integer, parameter :: PSW_P_BIT = 0  ! Parity Flag
  integer, parameter :: PSW_OV_BIT = 2  ! Overflow Flag
  integer, parameter :: PSW_RS0_BIT = 3  ! Register Bank Select 0
  integer, parameter :: PSW_RS1_BIT = 4  ! Register Bank Select 1
  integer, parameter :: PSW_F0_BIT = 5  ! Flag 0
  integer, parameter :: PSW_AC_BIT = 6  ! Auxiliary Carry Flag
  integer, parameter :: PSW_CY_BIT = 7  ! Carry Flag
  integer, parameter :: SP_ADDR = 0x81  ! Stack Pointer
  integer, parameter :: DPTR_ADDR = 0x82  ! Data Pointer (DPL/DPH)

  ! 内存段定义
  integer, parameter :: CODE_START = 0x0000
  integer, parameter :: CODE_END = 0x0FFF
  integer, parameter :: CODE_SIZE = 4096  ! Program Memory
  integer, parameter :: IDATA_START = 0x00
  integer, parameter :: IDATA_END = 0x7F
  integer, parameter :: IDATA_SIZE = 128  ! Internal Data Memory
  integer, parameter :: SFR_START = 0x80
  integer, parameter :: SFR_END = 0xFF
  integer, parameter :: SFR_SIZE = 128  ! Special Function Registers
  integer, parameter :: XDATA_START = 0x0000
  integer, parameter :: XDATA_END = 0xFFFF
  integer, parameter :: XDATA_SIZE = 65536  ! External Data Memory

  ! 外设定义
  ! Port 0
  integer, parameter :: PORT0_BASE = 0x80
  integer, parameter :: PORT0_P0_ADDR = 0x80
  ! Port 1
  integer, parameter :: PORT1_BASE = 0x90
  integer, parameter :: PORT1_P1_ADDR = 0x90
  ! Port 2
  integer, parameter :: PORT2_BASE = 0xA0
  integer, parameter :: PORT2_P2_ADDR = 0xA0
  ! Port 3
  integer, parameter :: PORT3_BASE = 0xB0
  integer, parameter :: PORT3_P3_ADDR = 0xB0
  ! Timer/Counter 0
  integer, parameter :: TIMER0_BASE = 0x8A
  integer, parameter :: TIMER0_TH0_ADDR = 0x8C
  integer, parameter :: TIMER0_TL0_ADDR = 0x8A
  integer, parameter :: TIMER0_TMOD_ADDR = 0x89
  integer, parameter :: TIMER0_TMOD_M0_0_BIT = 0  ! Timer 0 Mode bit 0
  integer, parameter :: TIMER0_TMOD_M1_0_BIT = 1  ! Timer 0 Mode bit 1
  integer, parameter :: TIMER0_TMOD_C_T0_BIT = 2  ! Timer 0 Counter/Timer Select
  integer, parameter :: TIMER0_TMOD_GATE0_BIT = 3  ! Timer 0 Gate Control
  integer, parameter :: TIMER0_TCON_ADDR = 0x88
  integer, parameter :: TIMER0_TCON_TR0_BIT = 4  ! Timer 0 Run Control
  integer, parameter :: TIMER0_TCON_TF0_BIT = 5  ! Timer 0 Overflow Flag
  ! Serial Port
  integer, parameter :: UART_BASE = 0x98
  integer, parameter :: UART_SBUF_ADDR = 0x99
  integer, parameter :: UART_SCON_ADDR = 0x98
  integer, parameter :: UART_SCON_RI_BIT = 0  ! Receive Interrupt Flag
  integer, parameter :: UART_SCON_TI_BIT = 1  ! Transmit Interrupt Flag
  integer, parameter :: UART_SCON_REN_BIT = 4  ! Receive Enable
  integer, parameter :: UART_SCON_SM0_BIT = 6  ! Serial Mode bit 0
  integer, parameter :: UART_SCON_SM1_BIT = 7  ! Serial Mode bit 1

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Reset Vector
  integer, parameter :: INT_INT0 = 1  ! External Interrupt 0
  integer, parameter :: INT_TIMER0 = 2  ! Timer 0 Interrupt
  integer, parameter :: INT_INT1 = 3  ! External Interrupt 1
  integer, parameter :: INT_TIMER1 = 4  ! Timer 1 Interrupt
  integer, parameter :: INT_UART = 5  ! Serial Port Interrupt

  ! 引脚定义
  integer, parameter :: PIN_P1_0 = 1  ! Port 1, bit 0
  integer, parameter :: PIN_P1_1 = 2  ! Port 1, bit 1
  integer, parameter :: PIN_P1_2 = 3  ! Port 1, bit 2
  integer, parameter :: PIN_P1_3 = 4  ! Port 1, bit 3
  integer, parameter :: PIN_P1_4 = 5  ! Port 1, bit 4
  integer, parameter :: PIN_P1_5 = 6  ! Port 1, bit 5
  integer, parameter :: PIN_P1_6 = 7  ! Port 1, bit 6
  integer, parameter :: PIN_P1_7 = 8  ! Port 1, bit 7
  integer, parameter :: PIN_RST = 9  ! Reset Pin
  integer, parameter :: PIN_RX = 10  ! Serial Receive (P3.0)
  integer, parameter :: PIN_TX = 11  ! Serial Transmit (P3.1)
  integer, parameter :: PIN_INT0 = 12  ! External Interrupt 0 (P3.2)
  integer, parameter :: PIN_INT1 = 13  ! External Interrupt 1 (P3.3)
  integer, parameter :: PIN_T0 = 14  ! Timer 0 Input (P3.4)
  integer, parameter :: PIN_T1 = 15  ! Timer 1 Input (P3.5)
  integer, parameter :: PIN_WR = 16  ! External Memory Write Strobe (P3.6)
  integer, parameter :: PIN_RD = 17  ! External Memory Read Strobe (P3.7)
  integer, parameter :: PIN_XTAL1 = 18  ! Crystal Oscillator Input
  integer, parameter :: PIN_XTAL2 = 19  ! Crystal Oscillator Output
  integer, parameter :: PIN_VCC = 20  ! Power Supply (+5V)
  integer, parameter :: PIN_GND = 21  ! Ground

end module 8051_device
