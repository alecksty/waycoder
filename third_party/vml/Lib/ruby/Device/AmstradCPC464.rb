# Amstrad-CPC-464 设备定义 - Ruby 模块
# 生成自: Amstrad/CPC/Amstrad-CPC-464
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
# CPU架构: Z80A
# 位宽: 8位
# 时钟频率: 4000000 Hz

module Amstrad_CPC_464

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
  IY_ADDR = 0x14  # Index Y
  SP_ADDR = 0x16  # Stack Pointer
  PC_ADDR = 0x18  # Program Counter

  # 内存段定义
  LOWER_ROM_START = 0x0000
  LOWER_ROM_END = 0x3FFF
  LOWER_ROM_SIZE = 16384  # Lower ROM (AMSDOS / CP/M)
  RAM_BANK0_START = 0x0000
  RAM_BANK0_END = 0x3FFF
  RAM_BANK0_SIZE = 16384  # Lower RAM bank (switchable)
  RAM_MAIN_START = 0x4000
  RAM_MAIN_END = 0xBFFF
  RAM_MAIN_SIZE = 32768  # Main RAM (32KB)
  UPPER_ROM_START = 0xC000
  UPPER_ROM_END = 0xFFFF
  UPPER_ROM_SIZE = 16384  # Upper ROM (BASIC)

  # 外设定义
  # Gate Array - Custom ASIC (video/sound/RAM control)
  GA_BASE = 0x7F00
  GA_GA_MR_ADDR = 0x7F00
  GA_GA_IR_ADDR = 0x7F01
  GA_GA_R1_ADDR = 0x7F02
  GA_GA_R2_ADDR = 0x7F03
  GA_GA_R3_ADDR = 0x7F04
  GA_GA_R4_ADDR = 0x7F05
  GA_GA_R5_ADDR = 0x7F06
  GA_GA_R6_ADDR = 0x7F07
  GA_GA_R7_ADDR = 0x7F08
  # CRT Controller 6845 - Video timing
  CRTC_BASE = 0xBC00
  CRTC_CRTC_REG_ADDR = 0xBC00
  CRTC_CRTC_DATA_ADDR = 0xBD00
  CRTC_CRTC_H_TOTAL_ADDR = 0xBC01
  CRTC_CRTC_H_DISP_ADDR = 0xBC02
  CRTC_CRTC_HSYNC_POS_ADDR = 0xBC03
  CRTC_CRTC_HSYNC_WIDTH_ADDR = 0xBC04
  CRTC_CRTC_V_TOTAL_ADDR = 0xBC05
  CRTC_CRTC_V_TOTAL_ADJ_ADDR = 0xBC06
  CRTC_CRTC_V_DISP_ADDR = 0xBC07
  CRTC_CRTC_VSYNC_POS_ADDR = 0xBC08
  CRTC_CRTC_INTERLACE_ADDR = 0xBC09
  CRTC_CRTC_CURSOR_START_ADDR = 0xBC0A
  CRTC_CRTC_CURSOR_END_ADDR = 0xBC0B
  CRTC_CRTC_SA_HI_ADDR = 0xBC0C
  CRTC_CRTC_SA_LO_ADDR = 0xBC0D
  CRTC_CRTC_CURSOR_HI_ADDR = 0xBC0E
  CRTC_CRTC_CURSOR_LO_ADDR = 0xBC0F
  # AY-3-8912 Programmable Sound Generator
  PSG_BASE = 0xF400
  PSG_PSG_REG_ADDR = 0xF400
  PSG_PSG_DATA_ADDR = 0xF600
  PSG_FREQ_A_LO_ADDR = 0xF400
  PSG_FREQ_A_HI_ADDR = 0xF401
  PSG_FREQ_B_LO_ADDR = 0xF402
  PSG_FREQ_B_HI_ADDR = 0xF403
  PSG_FREQ_C_LO_ADDR = 0xF404
  PSG_FREQ_C_HI_ADDR = 0xF405
  PSG_NOISE_FREQ_ADDR = 0xF406
  PSG_ENABLE_ADDR = 0xF407
  PSG_VOL_A_ADDR = 0xF408
  PSG_VOL_B_ADDR = 0xF409
  PSG_VOL_C_ADDR = 0xF40A
  PSG_ENV_FREQ_LO_ADDR = 0xF40B
  PSG_ENV_FREQ_HI_ADDR = 0xF40C
  PSG_ENV_SHAPE_ADDR = 0xF40D
  PSG_PORT_A_ADDR = 0xF40E
  PSG_PORT_B_ADDR = 0xF40F
  # WD1772 Floppy Disk Controller (via expansion)
  FDC_BASE = 0xF800
  FDC_FDC_STATUS_ADDR = 0xF8E0
  FDC_FDC_COMMAND_ADDR = 0xF8E0
  FDC_FDC_TRACK_ADDR = 0xF8E1
  FDC_FDC_SECTOR_ADDR = 0xF8E2
  FDC_FDC_DATA_ADDR = 0xF8E3
  # Centronics Parallel Printer Port
  PRINTER_BASE = 0xEE
  PRINTER_PRN_DATA_ADDR = 0xEE
  PRINTER_PRN_STROBE_ADDR = 0xEF

  # 中断向量定义
  INT_RESET = 0  # Power-on / Reset
  INT_NMI = 1  # Non-Maskable Interrupt
  INT_INT = 2  # Gate Array interrupt (50Hz vertical blank)

  # 引脚定义
  PIN_VCC = 1  # +5V Power
  PIN_GND = 2  # Ground
  PIN_CLK = 3  # Z80 Clock (4MHz)
  PIN_A0_A15 = 4  # Address Bus
  PIN_D0_D7 = 5  # Data Bus
  PIN_MREQ = 6  # Memory Request
  PIN_IORQ = 7  # I/O Request
  PIN_RD = 8  # Read
  PIN_WR = 9  # Write
  PIN_INT = 10  # Interrupt Request
  PIN_NMI = 11  # Non-Maskable Interrupt
  PIN_RESET = 12  # Reset

end
