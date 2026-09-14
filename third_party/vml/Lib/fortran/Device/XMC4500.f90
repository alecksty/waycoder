! XMC4500 设备定义 - Fortran 模块
! 生成自: Infineon/XMC4000/XMC4500
! 版本: 1.0
! 日期: 2026-04-28
! 作者: VML Team
! 描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
! CPU架构: ARM-Cortex-M4
! 位宽: 32位
! 时钟频率: 120000000 Hz

module xmc4500_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! 
  integer, parameter :: R1_ADDR = 0x04  ! 
  integer, parameter :: R2_ADDR = 0x08  ! 
  integer, parameter :: R3_ADDR = 0x0C  ! 
  integer, parameter :: R4_ADDR = 0x10  ! 
  integer, parameter :: R5_ADDR = 0x14  ! 
  integer, parameter :: SP_ADDR = 0x34  ! 
  integer, parameter :: LR_ADDR = 0x38  ! 
  integer, parameter :: PC_ADDR = 0x3C  ! 

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x08000000
  integer, parameter :: FLASH_END = 0x080FFFFF
  integer, parameter :: FLASH_SIZE = 1048576  ! 
  integer, parameter :: SRAM_START = 0x1FF00000
  integer, parameter :: SRAM_END = 0x1FF0FFFF
  integer, parameter :: SRAM_SIZE = 65536  ! 
  integer, parameter :: SRAM_COM_START = 0x20000000
  integer, parameter :: SRAM_COM_END = 0x20007FFF
  integer, parameter :: SRAM_COM_SIZE = 32768  ! Communication Memory
  integer, parameter :: SRAM_CPU_START = 0x20010000
  integer, parameter :: SRAM_CPU_END = 0x2001FFFF
  integer, parameter :: SRAM_CPU_SIZE = 65536  ! CPU SRAM
  integer, parameter :: PERIPHERAL_START = 0x40000000
  integer, parameter :: PERIPHERAL_END = 0x4FFFFFFF
  integer, parameter :: PERIPHERAL_SIZE = 268435456  ! 

  ! 外设定义
  ! System Control Unit
  integer, parameter :: SCU_BASE = 0x40020000
  integer, parameter :: SCU_CLKCR_ADDR = 0x00
  integer, parameter :: SCU_CLKCR_PCLK_SEL_BIT = 0  ! CPU clock selection
  integer, parameter :: SCU_CLKCR_FBKDIV_BIT = 16  ! Feedback divider
  integer, parameter :: SCU_PLLCONFIG_ADDR = 0x04
  integer, parameter :: SCU_OSCHPCTRL_ADDR = 0x08
  integer, parameter :: SCU_CGATSET0_ADDR = 0x20
  integer, parameter :: SCU_CGATSET0_CG_GATE_GPIO_BIT = 4  ! GPIO gate enable
  integer, parameter :: SCU_CGATCLR0_ADDR = 0x24
  ! Port 0
  integer, parameter :: PORT0_BASE = 0x48000000
  integer, parameter :: PORT0_OUT_ADDR = 0x00
  integer, parameter :: PORT0_OMR_ADDR = 0x04
  integer, parameter :: PORT0_IOCR0_ADDR = 0x10
  integer, parameter :: PORT0_IOCR4_ADDR = 0x14
  integer, parameter :: PORT0_IOCR8_ADDR = 0x18
  integer, parameter :: PORT0_IOCR12_ADDR = 0x1C
  integer, parameter :: PORT0_IN_ADDR = 0x24
  ! Port 1
  integer, parameter :: PORT1_BASE = 0x48010000
  integer, parameter :: PORT1_OUT_ADDR = 0x00
  integer, parameter :: PORT1_OMR_ADDR = 0x04
  integer, parameter :: PORT1_IOCR0_ADDR = 0x10
  integer, parameter :: PORT1_IOCR4_ADDR = 0x14
  integer, parameter :: PORT1_IOCR8_ADDR = 0x18
  integer, parameter :: PORT1_IOCR12_ADDR = 0x1C
  integer, parameter :: PORT1_IN_ADDR = 0x24
  ! Port 2
  integer, parameter :: PORT2_BASE = 0x48020000
  integer, parameter :: PORT2_OUT_ADDR = 0x00
  integer, parameter :: PORT2_OMR_ADDR = 0x04
  integer, parameter :: PORT2_IOCR0_ADDR = 0x10
  integer, parameter :: PORT2_IOCR4_ADDR = 0x14
  integer, parameter :: PORT2_IN_ADDR = 0x24
  ! Universal Serial Interface 0 (UART)
  integer, parameter :: USIC0_BASE = 0x48030000
  integer, parameter :: USIC0_CCR_ADDR = 0x00
  integer, parameter :: USIC0_PCR_ADDR = 0x04
  integer, parameter :: USIC0_RBUF_ADDR = 0x08
  integer, parameter :: USIC0_TBUF_ADDR = 0x0C
  integer, parameter :: USIC0_BRG_ADDR = 0x10

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! 
  integer, parameter :: INT_SVCALL = 11  ! 
  integer, parameter :: INT_USIC0_SR0 = 12  ! USIC0 Service Request 0

end module xmc4500_device
