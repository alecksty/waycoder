# Macintosh-128K 设备定义 - Ruby 模块
# 生成自: Apple Computer/Macintosh/Macintosh-128K
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
# CPU架构: MC68000
# 位宽: 32位
# 时钟频率: 7833600 Hz

module Macintosh_128K

  # 寄存器地址定义
  D0_ADDR = 0x00  # Data Register 0
  D1_ADDR = 0x04  # Data Register 1
  D2_ADDR = 0x08  # Data Register 2
  D3_ADDR = 0x0C  # Data Register 3
  D4_ADDR = 0x10  # Data Register 4
  D5_ADDR = 0x14  # Data Register 5
  D6_ADDR = 0x18  # Data Register 6
  D7_ADDR = 0x1C  # Data Register 7
  A0_ADDR = 0x20  # Address Register 0
  A1_ADDR = 0x24  # Address Register 1
  A2_ADDR = 0x28  # Address Register 2
  A3_ADDR = 0x2C  # Address Register 3
  A4_ADDR = 0x30  # Address Register 4
  A5_ADDR = 0x34  # Address Register 5
  A6_ADDR = 0x38  # Address Register 6
  A7_ADDR = 0x3C  # Stack Pointer (USP)
  PC_ADDR = 0x40  # Program Counter
  SR_ADDR = 0x44  # Status Register
  SR_C_BIT = 0  # Carry
  SR_V_BIT = 1  # Overflow
  SR_Z_BIT = 2  # Zero
  SR_N_BIT = 3  # Negative
  SR_X_BIT = 4  # Extend
  SR_I0_BIT = 8  # Interrupt Mask 0
  SR_I1_BIT = 9  # Interrupt Mask 1
  SR_I2_BIT = 10  # Interrupt Mask 2
  SR_S_BIT = 13  # Supervisor/User
  SR_T0_BIT = 14  # Trace Mode 0
  SR_T1_BIT = 15  # Trace Mode 1

  # 内存段定义
  RAM_START = 0x000000
  RAM_END = 0x01FFFF
  RAM_SIZE = 131072  # Main RAM (128KB unified)
  ROM_START = 0x40000000
  ROM_END = 0x4001FFFF
  ROM_SIZE = 131072  # Mac ROM (128KB)
  FRAMEBUFFER_START = 0x00400000
  FRAMEBUFFER_END = 0x00400555
  FRAMEBUFFER_SIZE = 1366  # Screen bitmap (512x342x1 = 21792 bytes)
  FRAMEBUFFER2_START = 0x00410000
  FRAMEBUFFER2_END = 0x00410555
  FRAMEBUFFER2_SIZE = 1366  # Shadow screen (double-buffering)
  VIA_START = 0x00E00000
  VIA_END = 0x00E0FFFF
  VIA_SIZE = 4096  # VIA 6522 (I/O)
  SCC_START = 0x00F00000
  SCC_END = 0x00F0FFFF
  SCC_SIZE = 4096  # SCC 8530 (serial)
  ADB_START = 0x01600000
  ADB_END = 0x0160FFFF
  ADB_SIZE = 4096  # ADB bus
  IWM_START = 0x01E00000
  IWM_END = 0x01E0FFFF
  IWM_SIZE = 4096  # IWM floppy controller

  # 外设定义
  # Versatile Interface Adapter 6522
  VIA_BASE = 0xE00000
  VIA_ORB_ADDR = 0xE00000
  VIA_ORA_ADDR = 0xE00002
  VIA_DDRB_ADDR = 0xE00004
  VIA_DDRA_ADDR = 0xE00006
  VIA_T1C_L_ADDR = 0xE00008
  VIA_T1C_H_ADDR = 0xE0000A
  VIA_T1L_L_ADDR = 0xE0000C
  VIA_T1L_H_ADDR = 0xE0000E
  VIA_T2C_L_ADDR = 0xE00010
  VIA_T2C_H_ADDR = 0xE00012
  VIA_SR_ADDR = 0xE00014
  VIA_ACR_ADDR = 0xE00016
  VIA_PCR_ADDR = 0xE00018
  VIA_IFR_ADDR = 0xE0001E
  VIA_IER_ADDR = 0xE0001E
  # SCC 8530 Serial Communications Controller
  SCC_BASE = 0xF00000
  SCC_SCC_CHA_B_ADDR = 0xF00000
  SCC_SCC_CHA_C_ADDR = 0xF00002
  SCC_SCC_CHB_D_ADDR = 0xF00004
  SCC_SCC_CHB_CT_ADDR = 0xF00006
  # Integrated Woz Machine - Floppy Disk Controller
  IWM_BASE = 0x1E00000
  IWM_IWM_DATA_ADDR = 0x1E00000
  IWM_IWM_MODE_ADDR = 0x1E00008
  IWM_IWM_Q6L_ADDR = 0x1E00020
  IWM_IWM_Q7L_ADDR = 0x1E00022
  IWM_IWM_Q6R_ADDR = 0x1E00024
  IWM_IWM_Q7R_ADDR = 0x1E00026
  # Video Graphics Controller (custom Apple chip)
  VGC_BASE = 0x00F20000
  VGC_VGC_MODE_ADDR = 0x00F20000
  VGC_VGC_START_HI_ADDR = 0x00F20002
  VGC_VGC_START_LO_ADDR = 0x00F20004
  # Apple Desktop Bus
  ADB_BASE = 0x01600000
  ADB_ADB_DATA_ADDR = 0x01600000
  ADB_ADB_STATUS_ADDR = 0x01600004
  ADB_ADB_CMD_ADDR = 0x01600008

  # 中断向量定义
  INT_RESET = 1  # Reset Initial SP
  INT_RESET_PC = 2  # Reset Initial PC
  INT_IRQ1 = 24  # VIA interrupt (level 1)
  INT_IRQ2 = 25  # SCC interrupt (level 2)
  INT_IRQ3 = 26  # ADB / VIA (level 3)
  INT_IRQ4 = 27  # ADB / VIA (level 4)

  # 引脚定义
  PIN_VCC = 1  # +5V Power
  PIN_GND = 2  # Ground
  PIN_CLK = 3  # 16MHz master clock / 7.83MHz CPU clock
  PIN_FC0 = 4  # Function Code 0
  PIN_FC1 = 5  # Function Code 1
  PIN_FC2 = 6  # Function Code 2
  PIN_AS = 7  # Address Strobe
  PIN_UDS = 8  # Upper Data Strobe
  PIN_LDS = 9  # Lower Data Strobe
  PIN_RWB = 10  # Read/Write
  PIN_DTACK = 11  # Data Acknowledge
  PIN_BERR = 12  # Bus Error
  PIN_BR = 13  # Bus Request
  PIN_BG = 14  # Bus Grant
  PIN_BGACK = 15  # Bus Grant Acknowledge
  PIN_IPL0 = 16  # Interrupt Priority 0
  PIN_IPL1 = 17  # Interrupt Priority 1
  PIN_IPL2 = 18  # Interrupt Priority 2
  PIN_RESET = 19  # Reset
  PIN_HALT = 20  # Halt
  PIN_A1_A23 = 21  # Address Bus (24-bit)
  PIN_D0_D15 = 22  # Data Bus (16-bit)

end
