! Macintosh-128K 设备定义 - Fortran 模块
! 生成自: Apple Computer/Macintosh/Macintosh-128K
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
! CPU架构: MC68000
! 位宽: 32位
! 时钟频率: 7833600 Hz

module macintosh_128k_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: D0_ADDR = 0x00  ! Data Register 0
  integer, parameter :: D1_ADDR = 0x04  ! Data Register 1
  integer, parameter :: D2_ADDR = 0x08  ! Data Register 2
  integer, parameter :: D3_ADDR = 0x0C  ! Data Register 3
  integer, parameter :: D4_ADDR = 0x10  ! Data Register 4
  integer, parameter :: D5_ADDR = 0x14  ! Data Register 5
  integer, parameter :: D6_ADDR = 0x18  ! Data Register 6
  integer, parameter :: D7_ADDR = 0x1C  ! Data Register 7
  integer, parameter :: A0_ADDR = 0x20  ! Address Register 0
  integer, parameter :: A1_ADDR = 0x24  ! Address Register 1
  integer, parameter :: A2_ADDR = 0x28  ! Address Register 2
  integer, parameter :: A3_ADDR = 0x2C  ! Address Register 3
  integer, parameter :: A4_ADDR = 0x30  ! Address Register 4
  integer, parameter :: A5_ADDR = 0x34  ! Address Register 5
  integer, parameter :: A6_ADDR = 0x38  ! Address Register 6
  integer, parameter :: A7_ADDR = 0x3C  ! Stack Pointer (USP)
  integer, parameter :: PC_ADDR = 0x40  ! Program Counter
  integer, parameter :: SR_ADDR = 0x44  ! Status Register
  integer, parameter :: SR_C_BIT = 0  ! Carry
  integer, parameter :: SR_V_BIT = 1  ! Overflow
  integer, parameter :: SR_Z_BIT = 2  ! Zero
  integer, parameter :: SR_N_BIT = 3  ! Negative
  integer, parameter :: SR_X_BIT = 4  ! Extend
  integer, parameter :: SR_I0_BIT = 8  ! Interrupt Mask 0
  integer, parameter :: SR_I1_BIT = 9  ! Interrupt Mask 1
  integer, parameter :: SR_I2_BIT = 10  ! Interrupt Mask 2
  integer, parameter :: SR_S_BIT = 13  ! Supervisor/User
  integer, parameter :: SR_T0_BIT = 14  ! Trace Mode 0
  integer, parameter :: SR_T1_BIT = 15  ! Trace Mode 1

  ! 内存段定义
  integer, parameter :: RAM_START = 0x000000
  integer, parameter :: RAM_END = 0x01FFFF
  integer, parameter :: RAM_SIZE = 131072  ! Main RAM (128KB unified)
  integer, parameter :: ROM_START = 0x40000000
  integer, parameter :: ROM_END = 0x4001FFFF
  integer, parameter :: ROM_SIZE = 131072  ! Mac ROM (128KB)
  integer, parameter :: FRAMEBUFFER_START = 0x00400000
  integer, parameter :: FRAMEBUFFER_END = 0x00400555
  integer, parameter :: FRAMEBUFFER_SIZE = 1366  ! Screen bitmap (512x342x1 = 21792 bytes)
  integer, parameter :: FRAMEBUFFER2_START = 0x00410000
  integer, parameter :: FRAMEBUFFER2_END = 0x00410555
  integer, parameter :: FRAMEBUFFER2_SIZE = 1366  ! Shadow screen (double-buffering)
  integer, parameter :: VIA_START = 0x00E00000
  integer, parameter :: VIA_END = 0x00E0FFFF
  integer, parameter :: VIA_SIZE = 4096  ! VIA 6522 (I/O)
  integer, parameter :: SCC_START = 0x00F00000
  integer, parameter :: SCC_END = 0x00F0FFFF
  integer, parameter :: SCC_SIZE = 4096  ! SCC 8530 (serial)
  integer, parameter :: ADB_START = 0x01600000
  integer, parameter :: ADB_END = 0x0160FFFF
  integer, parameter :: ADB_SIZE = 4096  ! ADB bus
  integer, parameter :: IWM_START = 0x01E00000
  integer, parameter :: IWM_END = 0x01E0FFFF
  integer, parameter :: IWM_SIZE = 4096  ! IWM floppy controller

  ! 外设定义
  ! Versatile Interface Adapter 6522
  integer, parameter :: VIA_BASE = 0xE00000
  integer, parameter :: VIA_ORB_ADDR = 0xE00000
  integer, parameter :: VIA_ORA_ADDR = 0xE00002
  integer, parameter :: VIA_DDRB_ADDR = 0xE00004
  integer, parameter :: VIA_DDRA_ADDR = 0xE00006
  integer, parameter :: VIA_T1C_L_ADDR = 0xE00008
  integer, parameter :: VIA_T1C_H_ADDR = 0xE0000A
  integer, parameter :: VIA_T1L_L_ADDR = 0xE0000C
  integer, parameter :: VIA_T1L_H_ADDR = 0xE0000E
  integer, parameter :: VIA_T2C_L_ADDR = 0xE00010
  integer, parameter :: VIA_T2C_H_ADDR = 0xE00012
  integer, parameter :: VIA_SR_ADDR = 0xE00014
  integer, parameter :: VIA_ACR_ADDR = 0xE00016
  integer, parameter :: VIA_PCR_ADDR = 0xE00018
  integer, parameter :: VIA_IFR_ADDR = 0xE0001E
  integer, parameter :: VIA_IER_ADDR = 0xE0001E
  ! SCC 8530 Serial Communications Controller
  integer, parameter :: SCC_BASE = 0xF00000
  integer, parameter :: SCC_SCC_CHA_B_ADDR = 0xF00000
  integer, parameter :: SCC_SCC_CHA_C_ADDR = 0xF00002
  integer, parameter :: SCC_SCC_CHB_D_ADDR = 0xF00004
  integer, parameter :: SCC_SCC_CHB_CT_ADDR = 0xF00006
  ! Integrated Woz Machine - Floppy Disk Controller
  integer, parameter :: IWM_BASE = 0x1E00000
  integer, parameter :: IWM_IWM_DATA_ADDR = 0x1E00000
  integer, parameter :: IWM_IWM_MODE_ADDR = 0x1E00008
  integer, parameter :: IWM_IWM_Q6L_ADDR = 0x1E00020
  integer, parameter :: IWM_IWM_Q7L_ADDR = 0x1E00022
  integer, parameter :: IWM_IWM_Q6R_ADDR = 0x1E00024
  integer, parameter :: IWM_IWM_Q7R_ADDR = 0x1E00026
  ! Video Graphics Controller (custom Apple chip)
  integer, parameter :: VGC_BASE = 0x00F20000
  integer, parameter :: VGC_VGC_MODE_ADDR = 0x00F20000
  integer, parameter :: VGC_VGC_START_HI_ADDR = 0x00F20002
  integer, parameter :: VGC_VGC_START_LO_ADDR = 0x00F20004
  ! Apple Desktop Bus
  integer, parameter :: ADB_BASE = 0x01600000
  integer, parameter :: ADB_ADB_DATA_ADDR = 0x01600000
  integer, parameter :: ADB_ADB_STATUS_ADDR = 0x01600004
  integer, parameter :: ADB_ADB_CMD_ADDR = 0x01600008

  ! 中断向量定义
  integer, parameter :: INT_RESET = 1  ! Reset Initial SP
  integer, parameter :: INT_RESET_PC = 2  ! Reset Initial PC
  integer, parameter :: INT_IRQ1 = 24  ! VIA interrupt (level 1)
  integer, parameter :: INT_IRQ2 = 25  ! SCC interrupt (level 2)
  integer, parameter :: INT_IRQ3 = 26  ! ADB / VIA (level 3)
  integer, parameter :: INT_IRQ4 = 27  ! ADB / VIA (level 4)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! 16MHz master clock / 7.83MHz CPU clock
  integer, parameter :: PIN_FC0 = 4  ! Function Code 0
  integer, parameter :: PIN_FC1 = 5  ! Function Code 1
  integer, parameter :: PIN_FC2 = 6  ! Function Code 2
  integer, parameter :: PIN_AS = 7  ! Address Strobe
  integer, parameter :: PIN_UDS = 8  ! Upper Data Strobe
  integer, parameter :: PIN_LDS = 9  ! Lower Data Strobe
  integer, parameter :: PIN_RWB = 10  ! Read/Write
  integer, parameter :: PIN_DTACK = 11  ! Data Acknowledge
  integer, parameter :: PIN_BERR = 12  ! Bus Error
  integer, parameter :: PIN_BR = 13  ! Bus Request
  integer, parameter :: PIN_BG = 14  ! Bus Grant
  integer, parameter :: PIN_BGACK = 15  ! Bus Grant Acknowledge
  integer, parameter :: PIN_IPL0 = 16  ! Interrupt Priority 0
  integer, parameter :: PIN_IPL1 = 17  ! Interrupt Priority 1
  integer, parameter :: PIN_IPL2 = 18  ! Interrupt Priority 2
  integer, parameter :: PIN_RESET = 19  ! Reset
  integer, parameter :: PIN_HALT = 20  ! Halt
  integer, parameter :: PIN_A1_A23 = 21  ! Address Bus (24-bit)
  integer, parameter :: PIN_D0_D15 = 22  ! Data Bus (16-bit)

end module macintosh_128k_device
