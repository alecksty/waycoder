! PIC32MX170F256B 设备定义 - Fortran 模块
! 生成自: Microchip/PIC32/PIC32MX170F256B
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
! CPU架构: MIPS32-M4K
! 位宽: 32位
! 时钟频率: 50000000 Hz

module pic32mx170f256b_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: _0_ADDR = 0x00  ! Hard-wired zero
  integer, parameter :: _1_ADDR = 0x04  ! AT
  integer, parameter :: _2_ADDR = 0x08  ! V0
  integer, parameter :: _3_ADDR = 0x0C  ! V1
  integer, parameter :: _4_ADDR = 0x10  ! A0
  integer, parameter :: _5_ADDR = 0x14  ! A1
  integer, parameter :: _29_ADDR = 0x74  ! Stack Pointer (SP)
  integer, parameter :: _31_ADDR = 0x7C  ! Return Address (RA)
  integer, parameter :: PC_ADDR = 0x80  ! Program Counter

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x9D000000
  integer, parameter :: FLASH_END = 0x9D03FFFF
  integer, parameter :: FLASH_SIZE = 262144  ! Program Flash
  integer, parameter :: SRAM_START = 0xA0000000
  integer, parameter :: SRAM_END = 0xA000FFFF
  integer, parameter :: SRAM_SIZE = 65536  ! 
  integer, parameter :: PERIPHERAL_START = 0xBF800000
  integer, parameter :: PERIPHERAL_END = 0xBF8FFFFF
  integer, parameter :: PERIPHERAL_SIZE = 1048576  ! 
  integer, parameter :: BOOTFLASH_START = 0xBFC00000
  integer, parameter :: BOOTFLASH_END = 0xBFC02FFF
  integer, parameter :: BOOTFLASH_SIZE = 12288  ! Boot Flash

  ! 外设定义
  ! General Purpose I/O Port A
  integer, parameter :: PORTA_BASE = 0xBF886000
  integer, parameter :: PORTA_TRISA_ADDR = 0x00
  integer, parameter :: PORTA_PORTA_ADDR = 0x10
  integer, parameter :: PORTA_LATA_ADDR = 0x20
  integer, parameter :: PORTA_ODCA_ADDR = 0x30
  ! General Purpose I/O Port B
  integer, parameter :: PORTB_BASE = 0xBF886100
  integer, parameter :: PORTB_TRISB_ADDR = 0x00
  integer, parameter :: PORTB_PORTB_ADDR = 0x10
  integer, parameter :: PORTB_LATB_ADDR = 0x20
  integer, parameter :: PORTB_ODCB_ADDR = 0x30
  ! UART1
  integer, parameter :: UART1_BASE = 0xBF822000
  integer, parameter :: UART1_UXMODE_ADDR = 0x00
  integer, parameter :: UART1_UXSTA_ADDR = 0x04
  integer, parameter :: UART1_UXTXREG_ADDR = 0x08
  integer, parameter :: UART1_UXRXREG_ADDR = 0x0C
  integer, parameter :: UART1_UXBRG_ADDR = 0x10

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_UART1 = 8  ! UART1 Interrupt

end module pic32mx170f256b_device
