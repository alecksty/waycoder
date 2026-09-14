! Sharp-LR35902 设备定义 - Fortran 模块
! 生成自: Sharp/Z80/Sharp-LR35902
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
! CPU架构: LR35902
! 位宽: 8位
! 时钟频率: 4194304 Hz

module sharp_lr35902_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: B_ADDR = 0x01  ! B Register
  integer, parameter :: C_ADDR = 0x02  ! C Register
  integer, parameter :: D_ADDR = 0x03  ! D Register
  integer, parameter :: E_ADDR = 0x04  ! E Register
  integer, parameter :: F_ADDR = 0x05  ! Flags Register
  integer, parameter :: F_C_BIT = 4  ! Carry
  integer, parameter :: F_H_BIT = 5  ! Half Carry
  integer, parameter :: F_N_BIT = 6  ! Subtract
  integer, parameter :: F_Z_BIT = 7  ! Zero
  integer, parameter :: H_ADDR = 0x06  ! H Register
  integer, parameter :: L_ADDR = 0x07  ! L Register
  integer, parameter :: AF_ADDR = 0x08  ! AF Register Pair (Accumulator + Flags)
  integer, parameter :: BC_ADDR = 0x0A  ! BC Register Pair
  integer, parameter :: DE_ADDR = 0x0C  ! DE Register Pair
  integer, parameter :: HL_ADDR = 0x0E  ! HL Register Pair
  integer, parameter :: SP_ADDR = 0x10  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x12  ! Program Counter

  ! 内存段定义
  integer, parameter :: WRAM_START = 0xC000
  integer, parameter :: WRAM_END = 0xCFFF
  integer, parameter :: WRAM_SIZE = 4096  ! Work RAM (4KB)
  integer, parameter :: WRAM_SHADOW_START = 0xE000
  integer, parameter :: WRAM_SHADOW_END = 0xEFFF
  integer, parameter :: WRAM_SHADOW_SIZE = 4096  ! Work RAM Shadow (Echo RAM)
  integer, parameter :: HRAM_START = 0xFF80
  integer, parameter :: HRAM_END = 0xFFFE
  integer, parameter :: HRAM_SIZE = 127  ! High RAM (127 bytes)
  integer, parameter :: IO_REGISTERS_START = 0xFF00
  integer, parameter :: IO_REGISTERS_END = 0xFF7F
  integer, parameter :: IO_REGISTERS_SIZE = 128  ! I/O Registers
  integer, parameter :: OAM_START = 0xFE00
  integer, parameter :: OAM_END = 0xFE9F
  integer, parameter :: OAM_SIZE = 160  ! Sprite Attribute Table (OAM)
  integer, parameter :: VRAM_START = 0x8000
  integer, parameter :: VRAM_END = 0x9FFF
  integer, parameter :: VRAM_SIZE = 8192  ! Video RAM (8KB)
  integer, parameter :: BG_MAP_1_START = 0x9800
  integer, parameter :: BG_MAP_1_END = 0x9BFF
  integer, parameter :: BG_MAP_1_SIZE = 1024  ! Background Map 1
  integer, parameter :: BG_MAP_2_START = 0x9C00
  integer, parameter :: BG_MAP_2_END = 0x9FFF
  integer, parameter :: BG_MAP_2_SIZE = 1024  ! Background Map 2
  integer, parameter :: ROM_BANK0_START = 0x0000
  integer, parameter :: ROM_BANK0_END = 0x3FFF
  integer, parameter :: ROM_BANK0_SIZE = 16384  ! ROM Bank 0 (Cartridge Header)
  integer, parameter :: ROM_BANK1_START = 0x4000
  integer, parameter :: ROM_BANK1_END = 0x7FFF
  integer, parameter :: ROM_BANK1_SIZE = 16384  ! ROM Bank 1 (Switchable)
  integer, parameter :: CART_RAM_START = 0xA000
  integer, parameter :: CART_RAM_END = 0xBFFF
  integer, parameter :: CART_RAM_SIZE = 8192  ! Cartridge RAM / MBC

  ! 外设定义
  ! LCD Controller / Picture Processing Unit
  integer, parameter :: PPU_BASE = 0xFF40
  integer, parameter :: PPU_LCDC_ADDR = 0xFF40
  integer, parameter :: PPU_LCDC_BG_ENABLE_BIT = 0  ! Background Display Enable
  integer, parameter :: PPU_LCDC_SPRITE_ENABLE_BIT = 1  ! Sprite Display Enable
  integer, parameter :: PPU_LCDC_SPRITE_SIZE_BIT = 2  ! Sprite Size (0=8x8, 1=8x16)
  integer, parameter :: PPU_LCDC_BG_TILE_MAP_BIT = 3  ! BG Tile Map Area (0=9800, 1=9C00)
  integer, parameter :: PPU_LCDC_TILE_DATA_BIT = 4  ! Tile Data Area (0=8800, 1=8000)
  integer, parameter :: PPU_LCDC_WINDOW_ENABLE_BIT = 5  ! Window Display Enable
  integer, parameter :: PPU_LCDC_WINDOW_MAP_BIT = 6  ! Window Tile Map Area (0=9800, 1=9C00)
  integer, parameter :: PPU_LCDC_LCD_ENABLE_BIT = 7  ! LCD Display Enable
  integer, parameter :: PPU_STAT_ADDR = 0xFF41
  integer, parameter :: PPU_STAT_MODE_BIT = 0  ! LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
  integer, parameter :: PPU_STAT_LYC_FLAG_BIT = 2  ! LY=LYC Compare Flag
  integer, parameter :: PPU_STAT_HBLANK_IRQ_BIT = 3  ! H-Blank Interrupt Enable
  integer, parameter :: PPU_STAT_VBLANK_IRQ_BIT = 4  ! V-Blank Interrupt Enable
  integer, parameter :: PPU_STAT_OAM_IRQ_BIT = 5  ! OAM Interrupt Enable
  integer, parameter :: PPU_STAT_LYC_IRQ_BIT = 6  ! LYC Interrupt Enable
  integer, parameter :: PPU_SCY_ADDR = 0xFF42
  integer, parameter :: PPU_SCX_ADDR = 0xFF43
  integer, parameter :: PPU_LY_ADDR = 0xFF44
  integer, parameter :: PPU_LYC_ADDR = 0xFF45
  integer, parameter :: PPU_DMA_ADDR = 0xFF46
  integer, parameter :: PPU_BGP_ADDR = 0xFF47
  integer, parameter :: PPU_OBP0_ADDR = 0xFF48
  integer, parameter :: PPU_OBP1_ADDR = 0xFF49
  integer, parameter :: PPU_WY_ADDR = 0xFF4A
  integer, parameter :: PPU_WX_ADDR = 0xFF4B
  ! Audio Processing Unit
  integer, parameter :: APU_BASE = 0xFF10
  integer, parameter :: APU_NR10_ADDR = 0xFF10
  integer, parameter :: APU_NR10_SWEEP_TIME_BIT = 0  ! Sweep Time
  integer, parameter :: APU_NR10_SWEEP_INCREASE_BIT = 3  ! Sweep Increase/Decrease
  integer, parameter :: APU_NR10_SWEEP_SHIFTS_BIT = 0  ! Sweep Number of Shifts
  integer, parameter :: APU_NR11_ADDR = 0xFF11
  integer, parameter :: APU_NR12_ADDR = 0xFF12
  integer, parameter :: APU_NR13_ADDR = 0xFF13
  integer, parameter :: APU_NR14_ADDR = 0xFF14
  integer, parameter :: APU_NR21_ADDR = 0xFF16
  integer, parameter :: APU_NR22_ADDR = 0xFF17
  integer, parameter :: APU_NR23_ADDR = 0xFF18
  integer, parameter :: APU_NR24_ADDR = 0xFF19
  integer, parameter :: APU_NR30_ADDR = 0xFF1A
  integer, parameter :: APU_NR31_ADDR = 0xFF1B
  integer, parameter :: APU_NR32_ADDR = 0xFF1C
  integer, parameter :: APU_NR33_ADDR = 0xFF1D
  integer, parameter :: APU_NR34_ADDR = 0xFF1E
  integer, parameter :: APU_NR41_ADDR = 0xFF20
  integer, parameter :: APU_NR42_ADDR = 0xFF21
  integer, parameter :: APU_NR43_ADDR = 0xFF22
  integer, parameter :: APU_NR44_ADDR = 0xFF23
  integer, parameter :: APU_NR50_ADDR = 0xFF24
  integer, parameter :: APU_NR51_ADDR = 0xFF25
  integer, parameter :: APU_NR52_ADDR = 0xFF26
  integer, parameter :: APU_NR52_CH1_ON_BIT = 0  ! Channel 1 ON
  integer, parameter :: APU_NR52_CH2_ON_BIT = 1  ! Channel 2 ON
  integer, parameter :: APU_NR52_CH3_ON_BIT = 2  ! Channel 3 ON
  integer, parameter :: APU_NR52_CH4_ON_BIT = 3  ! Channel 4 ON
  integer, parameter :: APU_NR52_ALL_ON_BIT = 7  ! All Sound ON
  ! Timer Unit
  integer, parameter :: TIMER_BASE = 0xFF04
  integer, parameter :: TIMER_DIV_ADDR = 0xFF04
  integer, parameter :: TIMER_TIMA_ADDR = 0xFF05
  integer, parameter :: TIMER_TMA_ADDR = 0xFF06
  integer, parameter :: TIMER_TAC_ADDR = 0xFF07
  integer, parameter :: TIMER_TAC_TIMER_ENABLE_BIT = 2  ! Timer Enable
  integer, parameter :: TIMER_TAC_CLOCK_SEL_BIT = 0  ! Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)
  ! Joypad Controller
  integer, parameter :: JOYPAD_BASE = 0xFF00
  integer, parameter :: JOYPAD_P1_ADDR = 0xFF00
  integer, parameter :: JOYPAD_P1_A_BTN_BIT = 0  ! A Button (1=Pressed when selected)
  integer, parameter :: JOYPAD_P1_B_BTN_BIT = 1  ! B Button (1=Pressed when selected)
  integer, parameter :: JOYPAD_P1_SELECT_BIT = 2  ! Select Button (1=Pressed)
  integer, parameter :: JOYPAD_P1_START_BIT = 3  ! Start Button (1=Pressed)
  integer, parameter :: JOYPAD_P1_DIR_DOWN_BIT = 4  ! Direction Down (1=Pressed when selected)
  integer, parameter :: JOYPAD_P1_DIR_UP_BIT = 5  ! Direction Up (1=Pressed when selected)
  integer, parameter :: JOYPAD_P1_DIR_LEFT_BIT = 6  ! Direction Left (1=Pressed when selected)
  integer, parameter :: JOYPAD_P1_DIR_RIGHT_BIT = 7  ! Direction Right (1=Pressed when selected)
  ! Serial I/O (Link Cable)
  integer, parameter :: SERIAL_BASE = 0xFF01
  integer, parameter :: SERIAL_SB_ADDR = 0xFF01
  integer, parameter :: SERIAL_SC_ADDR = 0xFF02
  integer, parameter :: SERIAL_SC_TRANSFER_START_BIT = 7  ! Transfer Start
  integer, parameter :: SERIAL_SC_CLOCK_SPEED_BIT = 1  ! Clock Select (0=External, 1=Internal 8192Hz)
  ! Interrupt Flag Register
  integer, parameter :: INTERRUPT_BASE = 0xFF0F
  integer, parameter :: INTERRUPT_IF_ADDR = 0xFF0F
  integer, parameter :: INTERRUPT_IF_VBLANK_BIT = 0  ! V-Blank Interrupt Request
  integer, parameter :: INTERRUPT_IF_LCDC_BIT = 1  ! LCDC Status Interrupt Request
  integer, parameter :: INTERRUPT_IF_TIMER_BIT = 2  ! Timer Overflow Interrupt Request
  integer, parameter :: INTERRUPT_IF_SERIAL_BIT = 3  ! Serial Transfer Complete Interrupt Request
  integer, parameter :: INTERRUPT_IF_JOYPAD_BIT = 4  ! Joypad Interrupt Request
  ! Interrupt Enable Register
  integer, parameter :: IE_BASE = 0xFFFF
  integer, parameter :: IE_IE_ADDR = 0xFFFF
  integer, parameter :: IE_IE_VBLANK_IE_BIT = 0  ! V-Blank Interrupt Enable
  integer, parameter :: IE_IE_LCDC_IE_BIT = 1  ! LCDC Status Interrupt Enable
  integer, parameter :: IE_IE_TIMER_IE_BIT = 2  ! Timer Interrupt Enable
  integer, parameter :: IE_IE_SERIAL_IE_BIT = 3  ! Serial Interrupt Enable
  integer, parameter :: IE_IE_JOYPAD_IE_BIT = 4  ! Joypad Interrupt Enable

  ! 中断向量定义
  integer, parameter :: INT_VBLANK = 0  ! V-Blank Interrupt (LY=144, during vertical blanking)
  integer, parameter :: INT_LCDC_STATUS = 1  ! LCDC Status Interrupt (H-Blank/OAM/V-Count match)
  integer, parameter :: INT_TIMER_OVERFLOW = 2  ! Timer Overflow Interrupt (TIMA overflow)
  integer, parameter :: INT_SERIAL_COMPLETE = 3  ! Serial Transfer Complete Interrupt
  integer, parameter :: INT_JOYPAD = 4  ! Joypad Interrupt (button press/release)

  ! 引脚定义
  integer, parameter :: PIN_VSS = 1  ! Ground
  integer, parameter :: PIN_VDD = 2  ! Power Supply
  integer, parameter :: PIN_PHI = 3  ! System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
  integer, parameter :: PIN_RESET = 4  ! Reset Signal (active low)
  integer, parameter :: PIN_INT = 5  ! Interrupt Request
  integer, parameter :: PIN_BUSREQ = 6  ! Bus Request (external DMA access)
  integer, parameter :: PIN_A0 = 7  ! Address Bus Bit 0
  integer, parameter :: PIN_A1 = 8  ! Address Bus Bit 1
  integer, parameter :: PIN_A2 = 9  ! Address Bus Bit 2
  integer, parameter :: PIN_A3 = 10  ! Address Bus Bit 3
  integer, parameter :: PIN_A4 = 11  ! Address Bus Bit 4
  integer, parameter :: PIN_A5 = 12  ! Address Bus Bit 5
  integer, parameter :: PIN_A6 = 13  ! Address Bus Bit 6
  integer, parameter :: PIN_A7 = 14  ! Address Bus Bit 7
  integer, parameter :: PIN_A8 = 15  ! Address Bus Bit 8
  integer, parameter :: PIN_A9 = 16  ! Address Bus Bit 9
  integer, parameter :: PIN_A10 = 17  ! Address Bus Bit 10
  integer, parameter :: PIN_A11 = 18  ! Address Bus Bit 11
  integer, parameter :: PIN_A12 = 19  ! Address Bus Bit 12
  integer, parameter :: PIN_A13 = 20  ! Address Bus Bit 13
  integer, parameter :: PIN_A14 = 21  ! Address Bus Bit 14
  integer, parameter :: PIN_A15 = 22  ! Address Bus Bit 15
  integer, parameter :: PIN_D0 = 23  ! Data Bus Bit 0
  integer, parameter :: PIN_D1 = 24  ! Data Bus Bit 1
  integer, parameter :: PIN_D2 = 25  ! Data Bus Bit 2
  integer, parameter :: PIN_D3 = 26  ! Data Bus Bit 3
  integer, parameter :: PIN_D4 = 27  ! Data Bus Bit 4
  integer, parameter :: PIN_D5 = 28  ! Data Bus Bit 5
  integer, parameter :: PIN_D6 = 29  ! Data Bus Bit 6
  integer, parameter :: PIN_D7 = 30  ! Data Bus Bit 7
  integer, parameter :: PIN_RD = 31  ! Read Strobe (active low)
  integer, parameter :: PIN_WR = 32  ! Write Strobe (active low)
  integer, parameter :: PIN_CS = 33  ! Chip Select (active low)
  integer, parameter :: PIN_SOUND_OUT = 34  ! Audio Output
  integer, parameter :: PIN_LCD_DATA0 = 35  ! LCD Data Bus Bit 0
  integer, parameter :: PIN_LCD_DATA1 = 36  ! LCD Data Bus Bit 1
  integer, parameter :: PIN_LCD_DATA2 = 37  ! LCD Data Bus Bit 2
  integer, parameter :: PIN_LCD_DATA3 = 38  ! LCD Data Bus Bit 3
  integer, parameter :: PIN_LCD_DATA4 = 39  ! LCD Data Bus Bit 4
  integer, parameter :: PIN_LCD_DATA5 = 40  ! LCD Data Bus Bit 5
  integer, parameter :: PIN_LCD_DATA6 = 41  ! LCD Data Bus Bit 6
  integer, parameter :: PIN_LCD_DATA7 = 42  ! LCD Data Bus Bit 7
  integer, parameter :: PIN_IR = 43  ! Infrared Port (DMG-CGB-01)

end module sharp_lr35902_device
