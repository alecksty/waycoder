# Macintosh-128K 设备定义 - Ruby 模块
# 生成自: Apple Computer/Macintosh/Macintosh-128K
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
# CPU架构: Motorola 68000
# 位宽: 32位
# 时钟频率: 7998000 Hz

module Macintosh_128K

  # 寄存器地址定义
  D0_ADDR = 0  # Data Register 0
  D1_ADDR = 0  # Data Register 1
  D2_ADDR = 0  # Data Register 2
  D3_ADDR = 0  # Data Register 3
  D4_ADDR = 0  # Data Register 4
  D5_ADDR = 0  # Data Register 5
  D6_ADDR = 0  # Data Register 6
  D7_ADDR = 0  # Data Register 7
  A0_ADDR = 0  # Address Register 0
  A1_ADDR = 0  # Address Register 1
  A2_ADDR = 0  # Address Register 2
  A3_ADDR = 0  # Address Register 3
  A4_ADDR = 0  # Address Register 4
  A5_ADDR = 0  # Address Register 5
  A6_ADDR = 0  # Address Register 6
  A7_ADDR = 0  # Address Register 7 (SP)
  PC_ADDR = 0  # Program Counter
  SR_ADDR = 0  # Status Register

  # 外设定义
  # Versatile Interface Adapter (6522)
  VIA_BASE = 
  VIA_VIA_ORB_ADDR = 0xE80000
  VIA_VIA_ORA_ADDR = 0xE80001
  VIA_VIA_DDRB_ADDR = 0xE80002
  VIA_VIA_DDRA_ADDR = 0xE80003
  VIA_VIA_T1CL_ADDR = 0xE80004
  VIA_VIA_T1CH_ADDR = 0xE80005
  VIA_VIA_T1LL_ADDR = 0xE80006
  VIA_VIA_T1LH_ADDR = 0xE80007
  VIA_VIA_T2CL_ADDR = 0xE80008
  VIA_VIA_T2CH_ADDR = 0xE80009
  VIA_VIA_SR_ADDR = 0xE8000A
  VIA_VIA_ACR_ADDR = 0xE8000B
  VIA_VIA_PCR_ADDR = 0xE8000C
  VIA_VIA_IFR_ADDR = 0xE8000D
  VIA_VIA_IER_ADDR = 0xE8000E
  VIA_VIA_ORA2_ADDR = 0xE8000F
  # Integrated Woz Machine (floppy controller)
  IWM_BASE = 
  IWM_IWM_Q6_ADDR = 0xD00000
  IWM_IWM_Q7_ADDR = 0xD00002
  IWM_IWM_PH0_ADDR = 0xD00004
  IWM_IWM_PH1_ADDR = 0xD00006
  IWM_IWM_PH2_ADDR = 0xD00008
  IWM_IWM_PH3_ADDR = 0xD0000A
  # Zilog 8530 Serial Communications Controller
  SCC_BASE = 
  SCC_SCC_CA_ADDR = 0x500000
  SCC_SCC_DA_ADDR = 0x500002
  SCC_SCC_CB_ADDR = 0x500004
  SCC_SCC_DB_ADDR = 0x500006
  # Built-in speaker
  SOUND_BASE = 
  SOUND_SOUND_VOL_ADDR = 0xE80100
  SOUND_SOUND_FREQ_ADDR = 0xE80102

  # 中断向量定义
  INT_RESET_SP = 0  # Reset (Initial SP)
  INT_RESET_PC = 4  # Reset (Initial PC)
  INT_AUTOVECTOR1 = 24  # Auto vector 1
  INT_AUTOVECTOR2 = 25  # Auto vector 2
  INT_AUTOVECTOR3 = 26  # Auto vector 3
  INT_AUTOVECTOR4 = 27  # Auto vector 4
  INT_AUTOVECTOR5 = 28  # Auto vector 5
  INT_AUTOVECTOR6 = 29  # Auto vector 6
  INT_AUTOVECTOR7 = 30  # Auto vector 7
  INT_SPURIOUS = 31  # Spurious interrupt

end
