# Sega-Genesis 设备定义 - Ruby 模块
# 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
# CPU架构: Motorola 68000
# 位宽: 32位
# 时钟频率: 7670000 Hz

module Sega_Genesis

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
  # Video Display Processor (315-5313)
  VDP_BASE = 
  VDP_VDP_DATA_ADDR = 0xC00000
  VDP_VDP_CONTROL_ADDR = 0xC00004
  VDP_VDP_HVCOUNTER_ADDR = 0xC00008
  VDP_VDP_PSG_ADDR = 0xC00011
  # FM synthesis sound chip
  YM2612_BASE = 
  YM2612_YM2612_ADDR0_ADDR = 0xA04000
  YM2612_YM2612_DATA0_ADDR = 0xA04001
  YM2612_YM2612_ADDR1_ADDR = 0xA04002
  YM2612_YM2612_DATA1_ADDR = 0xA04003
  # I/O ports
  IOPORTS_BASE = 
  IOPORTS_IO_DATA1_ADDR = 0xA10002
  IOPORTS_IO_DATA2_ADDR = 0xA10004
  IOPORTS_IO_DATA3_ADDR = 0xA10006
  IOPORTS_IO_CTRL1_ADDR = 0xA10008
  IOPORTS_IO_CTRL2_ADDR = 0xA1000A
  IOPORTS_IO_CTRL3_ADDR = 0xA1000C
  # TradeMark Security System
  TMSS_BASE = 
  TMSS_TMSS_ADDR = 0xA14000
  # Z80 bus control
  Z80BUS_BASE = 
  Z80BUS_Z80_BUSREQ_ADDR = 0xA11100
  Z80BUS_Z80_RESET_ADDR = 0xA11200
  Z80BUS_Z80_YM2612_ADDR = 0xA04000

  # 中断向量定义
  INT_RESET_SP = 0  # Reset (Initial SP)
  INT_RESET_PC = 4  # Reset (Initial PC)
  INT_HBLANK = 24  # Horizontal blank interrupt
  INT_VBLANK = 28  # Vertical blank interrupt
  INT_EXTINT1 = 32  # External interrupt 1
  INT_EXTINT2 = 36  # External interrupt 2
  INT_EXTINT3 = 40  # External interrupt 3
  INT_EXTINT4 = 44  # External interrupt 4
  INT_EXTINT5 = 48  # External interrupt 5
  INT_EXTINT6 = 52  # External interrupt 6
  INT_EXTINT7 = 56  # External interrupt 7

end
