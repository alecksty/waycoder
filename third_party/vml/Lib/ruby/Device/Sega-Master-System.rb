# Sega-Master-System 设备定义 - Ruby 模块
# 生成自: Sega/Master System/Sega-Master-System
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Sega Master System 8-bit video game console with Z80 CPU
# CPU架构: Zilog Z80
# 位宽: 8位
# 时钟频率: 3579545 Hz

module Sega_Master_System

  # 寄存器地址定义
  A_ADDR = 0  # Accumulator
  F_ADDR = 0  # Flags
  B_ADDR = 0  # B
  C_ADDR = 0  # C
  D_ADDR = 0  # D
  E_ADDR = 0  # E
  H_ADDR = 0  # H
  L_ADDR = 0  # L
  IX_ADDR = 0  # Index Register X
  IY_ADDR = 0  # Index Register Y
  SP_ADDR = 0  # Stack Pointer
  PC_ADDR = 0  # Program Counter
  I_ADDR = 0  # Interrupt Vector
  R_ADDR = 0  # Memory Refresh

  # 外设定义
  # Video Display Processor (TMS9918A)
  VDP_BASE = 
  VDP_VDP_DATA_ADDR = 0xBE
  VDP_VDP_ADDR_ADDR = 0xBF
  VDP_VDP_STATUS_ADDR = 0xBF
  # Programmable Sound Generator (SN76489)
  PSG_BASE = 
  PSG_PSG_DATA_ADDR = 0x7F
  # I/O ports
  IO_BASE = 
  IO_IO_PORT_A_ADDR = 0xDC
  IO_IO_PORT_B_ADDR = 0xDD
  IO_IO_PORT_MISC_ADDR = 0xDE
  IO_IO_PORT_VDP_ADDR = 0xDF
  # Memory mapper
  MEMORYMAPPER_BASE = 
  MEMORYMAPPER_MAPPER_0_ADDR = 0xFFFC
  MEMORYMAPPER_MAPPER_1_ADDR = 0xFFFD
  MEMORYMAPPER_MAPPER_2_ADDR = 0xFFFE
  MEMORYMAPPER_MAPPER_3_ADDR = 0xFFFF
  # FM Sound Unit (optional)
  FMUNIT_BASE = 
  FMUNIT_FM_ADDR_ADDR = 0xF0
  FMUNIT_FM_DATA_ADDR = 0xF1
  FMUNIT_FM_DETECT_ADDR = 0xF2

  # 中断向量定义
  INT_RST_00 = 0  # Restart 00h
  INT_IM1 = 56  # Interrupt Mode 1
  INT_VBLANK = 56  # Vertical blank interrupt
  INT_LINE = 100  # Line interrupt

end
