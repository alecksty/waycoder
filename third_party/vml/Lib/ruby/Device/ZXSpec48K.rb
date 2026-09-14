# ZX-Spectrum-48K 设备定义 - Ruby 模块
# 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
# CPU架构: Z80A
# 位宽: 8位
# 时钟频率: 3500000 Hz

module ZX_Spectrum_48K

  # 寄存器地址定义
  A_ADDR = 0x00  # Accumulator
  F_ADDR = 0x01  # Flags Register
  F_C_BIT = 0  # Carry
  F_N_BIT = 1  # Add/Subtract
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
  I_ADDR = 0x10  # Interrupt Vector Register
  R_ADDR = 0x11  # Refresh Counter
  IX_ADDR = 0x12  # Index X
  IY_ADDR = 0x14  # Index Y
  SP_ADDR = 0x16  # Stack Pointer
  PC_ADDR = 0x18  # Program Counter

  # 内存段定义
  ROM_START = 0x0000
  ROM_END = 0x3FFF
  ROM_SIZE = 16384  # 48KB ZX Spectrum ROM (BASIC + monitor)
  VIDEO_RAM_START = 0x4000
  VIDEO_RAM_END = 0x57FF
  VIDEO_RAM_SIZE = 6144  # Display file (256x192 bitmap)
  ATTR_RAM_START = 0x5800
  ATTR_RAM_END = 0x5AFF
  ATTR_RAM_SIZE = 768  # Attribute file (32x24 color cells)
  USER_RAM_START = 0x5B00
  USER_RAM_END = 0xFFFF
  USER_RAM_SIZE = 40960  # User RAM (40KB)

  # 外设定义
  # Uncommitted Logic Array - Sinclair custom IC
  ULA_BASE = 0xFE
  ULA_BORDER_ADDR = 0xFE
  ULA_KBD_ROW0_ADDR = 0xFE
  ULA_KBD_ROW1_ADDR = 0xFE
  ULA_KBD_ROW2_ADDR = 0xFE
  ULA_KBD_ROW3_ADDR = 0xFE
  ULA_KBD_ROW4_ADDR = 0xFE
  ULA_KBD_ROW5_ADDR = 0xFE
  ULA_KBD_ROW6_ADDR = 0xFE
  ULA_KBD_ROW7_ADDR = 0xFE
  ULA_KBD_ROW8_ADDR = 0xFE
  # Keyboard Matrix (40 keys, 8 rows x 5 cols)
  KEYBOARD_BASE = 0xFE
  KEYBOARD_KBD_IN_ADDR = 0xFE
  # Internal Beeper
  BEEPER_BASE = 0xFE
  BEEPER_BEEP_ADDR = 0xFE
  # Tape Interface
  TAPE_BASE = 0xFE
  TAPE_EAR_IN_ADDR = 0xFE
  TAPE_MIC_OUT_ADDR = 0xFE
  # Kempston Joystick Interface
  JOYSTICK_BASE = 0xF7FE
  JOYSTICK_KEMPSTON_ADDR = 0xF7FE

  # 中断向量定义
  INT_RESET = 0  # Power-on / Reset
  INT_NMI = 1  # Non-Maskable Interrupt (BREAK key)
  INT_INT = 2  # Maskable Interrupt (ULA vertical blank, 50Hz)

  # 引脚定义
  PIN_VCC = 1  # +5V Power
  PIN_GND = 2  # Ground
  PIN_CLK = 3  # Z80 Clock (3.5MHz)
  PIN_M1 = 4  # Machine Cycle 1
  PIN_MREQ = 5  # Memory Request
  PIN_IORQ = 6  # I/O Request
  PIN_RD = 7  # Read
  PIN_WR = 8  # Write
  PIN_HALT = 9  # Halt State
  PIN_BUSAK = 10  # Bus Acknowledge
  PIN_WAIT = 11  # Wait State (ULA inserts)
  PIN_INT = 12  # Interrupt Request
  PIN_NMI = 13  # Non-Maskable Interrupt
  PIN_RESET = 14  # Reset
  PIN_A0_A15 = 15  # Address Bus (16-bit)
  PIN_D0_D7 = 16  # Data Bus (8-bit)

end
