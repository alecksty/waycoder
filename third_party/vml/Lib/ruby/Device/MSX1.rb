# MSX1 设备定义 - Ruby 模块
# 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
# CPU架构: Z80A
# 位宽: 8位
# 时钟频率: 3579545 Hz

module MSX1

  # 寄存器地址定义
  A_ADDR = 0x00  # Accumulator
  F_ADDR = 0x01  # Flags
  F_C_BIT = 0  # Carry
  F_N_BIT = 1  # Subtract
  F_PV_BIT = 2  # Parity/Overflow
  F_H_BIT = 4  # Half Carry
  F_Z_BIT = 6  # Zero
  F_S_BIT = 7  # Sign
  B_ADDR = 0x02  # B Register
  C_ADDR = 0x03  # C Register
  D_ADDR = 0x04  # D Register
  E_ADDR = 0x05  # E Register
  H_ADDR = 0x06  # H Register
  L_ADDR = 0x07  # L Register
  AF_ADDR = 0x08  # Alternate AF
  BC_ADDR = 0x0A  # Alternate BC
  DE_ADDR = 0x0C  # Alternate DE
  HL_ADDR = 0x0E  # Alternate HL
  I_ADDR = 0x10  # Interrupt Vector
  R_ADDR = 0x11  # Refresh
  IX_ADDR = 0x12  # Index X
  IY_ADDR = 0x14  # Index Y (usually = 0xF38F)
  SP_ADDR = 0x16  # Stack Pointer
  PC_ADDR = 0x18  # Program Counter

  # 内存段定义
  SLOT0_ROM_START = 0x0000
  SLOT0_ROM_END = 0x7FFF
  SLOT0_ROM_SIZE = 32768  # Cartridge/SUB-ROM / Main-ROM
  SYSROM_START = 0x0000
  SYSROM_END = 0x3FFF
  SYSROM_SIZE = 16384  # MSX-BIOS ROM
  EXTROM_START = 0x4000
  EXTROM_END = 0x7FFF
  EXTROM_SIZE = 16384  # Extension ROM (cartridge)
  MAIN_RAM_START = 0x4000
  MAIN_RAM_END = 0xC000
  MAIN_RAM_SIZE = 32768  # Main RAM (32KB working area)
  WORK_RAM_START = 0xC000
  WORK_RAM_END = 0xFFFF
  WORK_RAM_SIZE = 16384  # Work RAM (16KB)
  SYSVAR_START = 0xF000
  SYSVAR_END = 0xFCA0
  SYSVAR_SIZE = 3232  # System variables area
  SLOTS_START = 0x8000
  SLOTS_END = 0xFFFF
  SLOTS_SIZE = 32768  # Slot-mapped memory

  # 外设定义
  # TMS9918A Video Display Processor
  VDP_BASE = 0x98
  VDP_VDP_REG0_ADDR = 0x99
  VDP_VDP_REG1_ADDR = 0x99
  VDP_VDP_REG2_ADDR = 0x99
  VDP_VDP_REG3_ADDR = 0x99
  VDP_VDP_REG4_ADDR = 0x99
  VDP_VDP_REG5_ADDR = 0x99
  VDP_VDP_REG6_ADDR = 0x99
  VDP_VDP_REG7_ADDR = 0x99
  VDP_VDP_STATUS_ADDR = 0x99
  VDP_VDP_DATA_ADDR = 0x98
  VDP_VDP_POT_ADDR = 0x98
  # AY-3-8910 Programmable Sound Generator
  PSG_BASE = 0xA0
  PSG_PSG_REG_ADDR = 0xA1
  PSG_PSG_DATA_ADDR = 0xA3
  PSG_FREQ_A_LO_ADDR = 0xA0
  PSG_FREQ_A_HI_ADDR = 0xA1
  PSG_FREQ_B_LO_ADDR = 0xA2
  PSG_FREQ_B_HI_ADDR = 0xA3
  PSG_FREQ_C_LO_ADDR = 0xA4
  PSG_FREQ_C_HI_ADDR = 0xA5
  PSG_NOISE_FREQ_ADDR = 0xA6
  PSG_ENABLE_ADDR = 0xA7
  PSG_VOL_A_ADDR = 0xA8
  PSG_VOL_B_ADDR = 0xA9
  PSG_VOL_C_ADDR = 0xAA
  PSG_ENV_FREQ_LO_ADDR = 0xAB
  PSG_ENV_FREQ_HI_ADDR = 0xAC
  PSG_ENV_SHAPE_ADDR = 0xAD
  PSG_PORT_A_ADDR = 0xAE
  PSG_PORT_B_ADDR = 0xAF
  # PPI 8255 Programmable Peripheral Interface
  PPI_BASE = 0xA8
  PPI_PPI_PA_ADDR = 0xA8
  PPI_PPI_PB_ADDR = 0xA9
  PPI_PPI_PC_ADDR = 0xAA
  PPI_PPI_CTRL_ADDR = 0xAB
  # MSX Slot Expansion System
  SLOTEXP_BASE = 0x0000
  SLOTEXP_SLOT0_ADDR = 0xFCC0
  SLOTEXP_SLOT1_ADDR = 0xFCC1
  SLOTEXP_SLOT2_ADDR = 0xFCC2
  SLOTEXP_SLOT3_ADDR = 0xFCC3
  SLOTEXP_EXPTBL0_ADDR = 0xFCC4
  SLOTEXP_EXPTBL1_ADDR = 0xFCC5
  SLOTEXP_EXPTBL2_ADDR = 0xFCC6
  SLOTEXP_EXPTBL3_ADDR = 0xFCC7

  # 中断向量定义
  INT_RESET = 0  # Power-on / Reset
  INT_NMI = 1  # Non-Maskable Interrupt
  INT_INT = 2  # VDP Vertical Interrupt (frame)

  # 引脚定义
  PIN_VCC = 1  # +5V Power
  PIN_GND = 2  # Ground
  PIN_CLK = 3  # Z80 Clock (3.58MHz)
  PIN_A0_A15 = 4  # Address Bus
  PIN_D0_D7 = 5  # Data Bus
  PIN_MREQ = 6  # Memory Request
  PIN_IORQ = 7  # I/O Request
  PIN_RD = 8  # Read
  PIN_WR = 9  # Write
  PIN_INT = 10  # Interrupt Request
  PIN_NMI = 11  # Non-Maskable Interrupt
  PIN_RESET = 12  # Reset
  PIN_SLTSL = 13  # Slot select (for memory mapping)
  PIN_WAIT = 14  # Wait (for slow I/O)

end
