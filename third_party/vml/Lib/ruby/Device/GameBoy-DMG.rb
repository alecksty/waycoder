# Sharp-LR35902 设备定义 - Ruby 模块
# 生成自: Sharp/Z80/Sharp-LR35902
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
# CPU架构: LR35902
# 位宽: 8位
# 时钟频率: 4194304 Hz

module Sharp_LR35902

  # 寄存器地址定义
  A_ADDR = 0x00  # Accumulator
  B_ADDR = 0x01  # B Register
  C_ADDR = 0x02  # C Register
  D_ADDR = 0x03  # D Register
  E_ADDR = 0x04  # E Register
  F_ADDR = 0x05  # Flags Register
  F_C_BIT = 4  # Carry
  F_H_BIT = 5  # Half Carry
  F_N_BIT = 6  # Subtract
  F_Z_BIT = 7  # Zero
  H_ADDR = 0x06  # H Register
  L_ADDR = 0x07  # L Register
  AF_ADDR = 0x08  # AF Register Pair (Accumulator + Flags)
  BC_ADDR = 0x0A  # BC Register Pair
  DE_ADDR = 0x0C  # DE Register Pair
  HL_ADDR = 0x0E  # HL Register Pair
  SP_ADDR = 0x10  # Stack Pointer
  PC_ADDR = 0x12  # Program Counter

  # 内存段定义
  WRAM_START = 0xC000
  WRAM_END = 0xCFFF
  WRAM_SIZE = 4096  # Work RAM (4KB)
  WRAM_SHADOW_START = 0xE000
  WRAM_SHADOW_END = 0xEFFF
  WRAM_SHADOW_SIZE = 4096  # Work RAM Shadow (Echo RAM)
  HRAM_START = 0xFF80
  HRAM_END = 0xFFFE
  HRAM_SIZE = 127  # High RAM (127 bytes)
  IO_REGISTERS_START = 0xFF00
  IO_REGISTERS_END = 0xFF7F
  IO_REGISTERS_SIZE = 128  # I/O Registers
  OAM_START = 0xFE00
  OAM_END = 0xFE9F
  OAM_SIZE = 160  # Sprite Attribute Table (OAM)
  VRAM_START = 0x8000
  VRAM_END = 0x9FFF
  VRAM_SIZE = 8192  # Video RAM (8KB)
  BG_MAP_1_START = 0x9800
  BG_MAP_1_END = 0x9BFF
  BG_MAP_1_SIZE = 1024  # Background Map 1
  BG_MAP_2_START = 0x9C00
  BG_MAP_2_END = 0x9FFF
  BG_MAP_2_SIZE = 1024  # Background Map 2
  ROM_BANK0_START = 0x0000
  ROM_BANK0_END = 0x3FFF
  ROM_BANK0_SIZE = 16384  # ROM Bank 0 (Cartridge Header)
  ROM_BANK1_START = 0x4000
  ROM_BANK1_END = 0x7FFF
  ROM_BANK1_SIZE = 16384  # ROM Bank 1 (Switchable)
  CART_RAM_START = 0xA000
  CART_RAM_END = 0xBFFF
  CART_RAM_SIZE = 8192  # Cartridge RAM / MBC

  # 外设定义
  # LCD Controller / Picture Processing Unit
  PPU_BASE = 0xFF40
  PPU_LCDC_ADDR = 0xFF40
  PPU_LCDC_BG_ENABLE_BIT = 0  # Background Display Enable
  PPU_LCDC_SPRITE_ENABLE_BIT = 1  # Sprite Display Enable
  PPU_LCDC_SPRITE_SIZE_BIT = 2  # Sprite Size (0=8x8, 1=8x16)
  PPU_LCDC_BG_TILE_MAP_BIT = 3  # BG Tile Map Area (0=9800, 1=9C00)
  PPU_LCDC_TILE_DATA_BIT = 4  # Tile Data Area (0=8800, 1=8000)
  PPU_LCDC_WINDOW_ENABLE_BIT = 5  # Window Display Enable
  PPU_LCDC_WINDOW_MAP_BIT = 6  # Window Tile Map Area (0=9800, 1=9C00)
  PPU_LCDC_LCD_ENABLE_BIT = 7  # LCD Display Enable
  PPU_STAT_ADDR = 0xFF41
  PPU_STAT_MODE_BIT = 0  # LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
  PPU_STAT_LYC_FLAG_BIT = 2  # LY=LYC Compare Flag
  PPU_STAT_HBLANK_IRQ_BIT = 3  # H-Blank Interrupt Enable
  PPU_STAT_VBLANK_IRQ_BIT = 4  # V-Blank Interrupt Enable
  PPU_STAT_OAM_IRQ_BIT = 5  # OAM Interrupt Enable
  PPU_STAT_LYC_IRQ_BIT = 6  # LYC Interrupt Enable
  PPU_SCY_ADDR = 0xFF42
  PPU_SCX_ADDR = 0xFF43
  PPU_LY_ADDR = 0xFF44
  PPU_LYC_ADDR = 0xFF45
  PPU_DMA_ADDR = 0xFF46
  PPU_BGP_ADDR = 0xFF47
  PPU_OBP0_ADDR = 0xFF48
  PPU_OBP1_ADDR = 0xFF49
  PPU_WY_ADDR = 0xFF4A
  PPU_WX_ADDR = 0xFF4B
  # Audio Processing Unit
  APU_BASE = 0xFF10
  APU_NR10_ADDR = 0xFF10
  APU_NR10_SWEEP_TIME_BIT = 0  # Sweep Time
  APU_NR10_SWEEP_INCREASE_BIT = 3  # Sweep Increase/Decrease
  APU_NR10_SWEEP_SHIFTS_BIT = 0  # Sweep Number of Shifts
  APU_NR11_ADDR = 0xFF11
  APU_NR12_ADDR = 0xFF12
  APU_NR13_ADDR = 0xFF13
  APU_NR14_ADDR = 0xFF14
  APU_NR21_ADDR = 0xFF16
  APU_NR22_ADDR = 0xFF17
  APU_NR23_ADDR = 0xFF18
  APU_NR24_ADDR = 0xFF19
  APU_NR30_ADDR = 0xFF1A
  APU_NR31_ADDR = 0xFF1B
  APU_NR32_ADDR = 0xFF1C
  APU_NR33_ADDR = 0xFF1D
  APU_NR34_ADDR = 0xFF1E
  APU_NR41_ADDR = 0xFF20
  APU_NR42_ADDR = 0xFF21
  APU_NR43_ADDR = 0xFF22
  APU_NR44_ADDR = 0xFF23
  APU_NR50_ADDR = 0xFF24
  APU_NR51_ADDR = 0xFF25
  APU_NR52_ADDR = 0xFF26
  APU_NR52_CH1_ON_BIT = 0  # Channel 1 ON
  APU_NR52_CH2_ON_BIT = 1  # Channel 2 ON
  APU_NR52_CH3_ON_BIT = 2  # Channel 3 ON
  APU_NR52_CH4_ON_BIT = 3  # Channel 4 ON
  APU_NR52_ALL_ON_BIT = 7  # All Sound ON
  # Timer Unit
  TIMER_BASE = 0xFF04
  TIMER_DIV_ADDR = 0xFF04
  TIMER_TIMA_ADDR = 0xFF05
  TIMER_TMA_ADDR = 0xFF06
  TIMER_TAC_ADDR = 0xFF07
  TIMER_TAC_TIMER_ENABLE_BIT = 2  # Timer Enable
  TIMER_TAC_CLOCK_SEL_BIT = 0  # Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)
  # Joypad Controller
  JOYPAD_BASE = 0xFF00
  JOYPAD_P1_ADDR = 0xFF00
  JOYPAD_P1_A_BTN_BIT = 0  # A Button (1=Pressed when selected)
  JOYPAD_P1_B_BTN_BIT = 1  # B Button (1=Pressed when selected)
  JOYPAD_P1_SELECT_BIT = 2  # Select Button (1=Pressed)
  JOYPAD_P1_START_BIT = 3  # Start Button (1=Pressed)
  JOYPAD_P1_DIR_DOWN_BIT = 4  # Direction Down (1=Pressed when selected)
  JOYPAD_P1_DIR_UP_BIT = 5  # Direction Up (1=Pressed when selected)
  JOYPAD_P1_DIR_LEFT_BIT = 6  # Direction Left (1=Pressed when selected)
  JOYPAD_P1_DIR_RIGHT_BIT = 7  # Direction Right (1=Pressed when selected)
  # Serial I/O (Link Cable)
  SERIAL_BASE = 0xFF01
  SERIAL_SB_ADDR = 0xFF01
  SERIAL_SC_ADDR = 0xFF02
  SERIAL_SC_TRANSFER_START_BIT = 7  # Transfer Start
  SERIAL_SC_CLOCK_SPEED_BIT = 1  # Clock Select (0=External, 1=Internal 8192Hz)
  # Interrupt Flag Register
  INTERRUPT_BASE = 0xFF0F
  INTERRUPT_IF_ADDR = 0xFF0F
  INTERRUPT_IF_VBLANK_BIT = 0  # V-Blank Interrupt Request
  INTERRUPT_IF_LCDC_BIT = 1  # LCDC Status Interrupt Request
  INTERRUPT_IF_TIMER_BIT = 2  # Timer Overflow Interrupt Request
  INTERRUPT_IF_SERIAL_BIT = 3  # Serial Transfer Complete Interrupt Request
  INTERRUPT_IF_JOYPAD_BIT = 4  # Joypad Interrupt Request
  # Interrupt Enable Register
  IE_BASE = 0xFFFF
  IE_IE_ADDR = 0xFFFF
  IE_IE_VBLANK_IE_BIT = 0  # V-Blank Interrupt Enable
  IE_IE_LCDC_IE_BIT = 1  # LCDC Status Interrupt Enable
  IE_IE_TIMER_IE_BIT = 2  # Timer Interrupt Enable
  IE_IE_SERIAL_IE_BIT = 3  # Serial Interrupt Enable
  IE_IE_JOYPAD_IE_BIT = 4  # Joypad Interrupt Enable

  # 中断向量定义
  INT_VBLANK = 0  # V-Blank Interrupt (LY=144, during vertical blanking)
  INT_LCDC_STATUS = 1  # LCDC Status Interrupt (H-Blank/OAM/V-Count match)
  INT_TIMER_OVERFLOW = 2  # Timer Overflow Interrupt (TIMA overflow)
  INT_SERIAL_COMPLETE = 3  # Serial Transfer Complete Interrupt
  INT_JOYPAD = 4  # Joypad Interrupt (button press/release)

  # 引脚定义
  PIN_VSS = 1  # Ground
  PIN_VDD = 2  # Power Supply
  PIN_PHI = 3  # System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
  PIN_RESET = 4  # Reset Signal (active low)
  PIN_INT = 5  # Interrupt Request
  PIN_BUSREQ = 6  # Bus Request (external DMA access)
  PIN_A0 = 7  # Address Bus Bit 0
  PIN_A1 = 8  # Address Bus Bit 1
  PIN_A2 = 9  # Address Bus Bit 2
  PIN_A3 = 10  # Address Bus Bit 3
  PIN_A4 = 11  # Address Bus Bit 4
  PIN_A5 = 12  # Address Bus Bit 5
  PIN_A6 = 13  # Address Bus Bit 6
  PIN_A7 = 14  # Address Bus Bit 7
  PIN_A8 = 15  # Address Bus Bit 8
  PIN_A9 = 16  # Address Bus Bit 9
  PIN_A10 = 17  # Address Bus Bit 10
  PIN_A11 = 18  # Address Bus Bit 11
  PIN_A12 = 19  # Address Bus Bit 12
  PIN_A13 = 20  # Address Bus Bit 13
  PIN_A14 = 21  # Address Bus Bit 14
  PIN_A15 = 22  # Address Bus Bit 15
  PIN_D0 = 23  # Data Bus Bit 0
  PIN_D1 = 24  # Data Bus Bit 1
  PIN_D2 = 25  # Data Bus Bit 2
  PIN_D3 = 26  # Data Bus Bit 3
  PIN_D4 = 27  # Data Bus Bit 4
  PIN_D5 = 28  # Data Bus Bit 5
  PIN_D6 = 29  # Data Bus Bit 6
  PIN_D7 = 30  # Data Bus Bit 7
  PIN_RD = 31  # Read Strobe (active low)
  PIN_WR = 32  # Write Strobe (active low)
  PIN_CS = 33  # Chip Select (active low)
  PIN_SOUND_OUT = 34  # Audio Output
  PIN_LCD_DATA0 = 35  # LCD Data Bus Bit 0
  PIN_LCD_DATA1 = 36  # LCD Data Bus Bit 1
  PIN_LCD_DATA2 = 37  # LCD Data Bus Bit 2
  PIN_LCD_DATA3 = 38  # LCD Data Bus Bit 3
  PIN_LCD_DATA4 = 39  # LCD Data Bus Bit 4
  PIN_LCD_DATA5 = 40  # LCD Data Bus Bit 5
  PIN_LCD_DATA6 = 41  # LCD Data Bus Bit 6
  PIN_LCD_DATA7 = 42  # LCD Data Bus Bit 7
  PIN_IR = 43  # Infrared Port (DMG-CGB-01)

end
