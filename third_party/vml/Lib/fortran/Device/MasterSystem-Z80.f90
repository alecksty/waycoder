! Zilog-Z80 设备定义 - Fortran 模块
! 生成自: Zilog/Z80/Zilog-Z80
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
! CPU架构: Z80
! 位宽: 8位
! 时钟频率: 3580000 Hz

module zilog_z80_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: F_ADDR = 0x01  ! Flags Register
  integer, parameter :: F_C_BIT = 0  ! Carry
  integer, parameter :: F_N_BIT = 1  ! Subtract
  integer, parameter :: F_P_BIT = 2  ! Parity/Overflow
  integer, parameter :: F_H_BIT = 4  ! Half Carry
  integer, parameter :: F_Z_BIT = 6  ! Zero
  integer, parameter :: F_S_BIT = 7  ! Sign/Negative
  integer, parameter :: B_ADDR = 0x02  ! B Register
  integer, parameter :: C_ADDR = 0x03  ! C Register
  integer, parameter :: D_ADDR = 0x04  ! D Register
  integer, parameter :: E_ADDR = 0x05  ! E Register
  integer, parameter :: H_ADDR = 0x06  ! H Register
  integer, parameter :: L_ADDR = 0x07  ! L Register
  integer, parameter :: AF_ADDR = 0x08  ! Alternate AF
  integer, parameter :: BC_ADDR = 0x0A  ! Alternate BC
  integer, parameter :: DE_ADDR = 0x0C  ! Alternate DE
  integer, parameter :: HL_ADDR = 0x0E  ! Alternate HL
  integer, parameter :: IX_ADDR = 0x10  ! Index Register X
  integer, parameter :: IY_ADDR = 0x12  ! Index Register Y
  integer, parameter :: SP_ADDR = 0x14  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x16  ! Program Counter
  integer, parameter :: I_ADDR = 0x18  ! Interrupt Vector Register
  integer, parameter :: R_ADDR = 0x19  ! Memory Refresh Register
  integer, parameter :: IM_ADDR = 0x1A  ! Interrupt Mode (0/1/2)

  ! 内存段定义
  integer, parameter :: WRAM_START = 0xC000
  integer, parameter :: WRAM_END = 0xC7FF
  integer, parameter :: WRAM_SIZE = 2048  ! Work RAM (2KB internal)
  integer, parameter :: WRAM_SHADOW_START = 0xE000
  integer, parameter :: WRAM_SHADOW_END = 0xE7FF
  integer, parameter :: WRAM_SHADOW_SIZE = 2048  ! Work RAM Shadow (Echo RAM)
  integer, parameter :: VRAM_START = 0x4000
  integer, parameter :: VRAM_END = 0x7FFF
  integer, parameter :: VRAM_SIZE = 16384  ! Video RAM (16KB)
  integer, parameter :: SRAM_START = 0x8000
  integer, parameter :: SRAM_END = 0xBFFF
  integer, parameter :: SRAM_SIZE = 16384  ! Cartridge SRAM (if present)
  integer, parameter :: CART_ROM_START = 0x0000
  integer, parameter :: CART_ROM_END = 0x7FFF
  integer, parameter :: CART_ROM_SIZE = 32768  ! Cartridge ROM (up to 48KB)
  integer, parameter :: BIOS_START = 0x0000
  integer, parameter :: BIOS_END = 0x1FFF
  integer, parameter :: BIOS_SIZE = 8192  ! BIOS ROM (Master System built-in, 8KB)
  integer, parameter :: IO_REGS_START = 0x3F00
  integer, parameter :: IO_REGS_END = 0x3FFF
  integer, parameter :: IO_REGS_SIZE = 256  ! I/O Register Area

  ! 外设定义
  ! Video Display Processor (TMS9918A variant)
  integer, parameter :: VDP_BASE = 0xBE
  integer, parameter :: VDP_VDP_CTRL_ADDR = 0xBF
  integer, parameter :: VDP_VDP_DATA_ADDR = 0xBE
  integer, parameter :: VDP_VDP_STATUS_ADDR = 0xBF
  integer, parameter :: VDP_VDP_STATUS_FIFO_FULL_BIT = 0  ! VRAM to CPU Transfer Pending
  integer, parameter :: VDP_VDP_STATUS_FIFO_EMPTY_BIT = 1  ! VRAM Write FIFO Empty
  integer, parameter :: VDP_VDP_STATUS_INT_FLAG_BIT = 7  ! V-Blank / Sprite Collision Flag
  integer, parameter :: VDP_R0_ADDR = 0x00
  integer, parameter :: VDP_R0_M3_BIT = 0  ! Mode 3 Enable
  integer, parameter :: VDP_R0_M2_BIT = 1  ! Mode 2 Enable
  integer, parameter :: VDP_R0_M1_BIT = 2  ! Mode 1 Enable
  integer, parameter :: VDP_R0_DISPLAY_DISABLE_BIT = 3  ! Display Disable (1=blank screen)
  integer, parameter :: VDP_R0_VIRQ_EN_BIT = 4  ! Vertical Interrupt Enable
  integer, parameter :: VDP_R0_M4_BIT = 5  ! Mode 4 Enable (SMS2 only)
  integer, parameter :: VDP_R0_SPRITE_SHIFT_BIT = 6  ! Sprite Double Height
  integer, parameter :: VDP_R0_HVC_LATCH_BIT = 7  ! H-Counter Latch Enable
  integer, parameter :: VDP_R1_ADDR = 0x01
  integer, parameter :: VDP_R1_DISPLAY_BIT = 3  ! Display Enable (1=active)
  integer, parameter :: VDP_R1_FRAME_INT_BIT = 4  ! Frame Interrupt (V-Blank) Enable
  integer, parameter :: VDP_R1_M4_BIT = 5  ! Mode 4 (256-color)
  integer, parameter :: VDP_R1_SMS_MODE_BIT = 6  ! SMS Display Mode (vs Coleco)
  integer, parameter :: VDP_R1_EXT_VIDEO_BIT = 7  ! External Video Enable
  integer, parameter :: VDP_R2_ADDR = 0x02
  integer, parameter :: VDP_R3_ADDR = 0x03
  integer, parameter :: VDP_R4_ADDR = 0x04
  integer, parameter :: VDP_R5_ADDR = 0x05
  integer, parameter :: VDP_R6_ADDR = 0x06
  integer, parameter :: VDP_R7_ADDR = 0x07
  integer, parameter :: VDP_R8_ADDR = 0x08
  integer, parameter :: VDP_R8_HSCROLL_EN_BIT = 0  ! Horizontal Scroll Enable
  integer, parameter :: VDP_R8_VSCROLL_EN_BIT = 1  ! Vertical Scroll Enable
  integer, parameter :: VDP_R8_LINE_INT_BIT = 4  ! Line Interrupt Enable
  integer, parameter :: VDP_R8_VSCROLL_2X_BIT = 7  ! Vertical Scroll 2x Speed
  integer, parameter :: VDP_R9_ADDR = 0x09
  integer, parameter :: VDP_R10_ADDR = 0x0A
  integer, parameter :: VDP_R11_ADDR = 0x0B
  integer, parameter :: VDP_R12_ADDR = 0x0C
  integer, parameter :: VDP_R13_ADDR = 0x0D
  integer, parameter :: VDP_R14_ADDR = 0x0E
  integer, parameter :: VDP_R15_ADDR = 0x0F
  integer, parameter :: VDP_VCOUNTER_ADDR = 0x7E
  integer, parameter :: VDP_HCOUNTER_ADDR = 0x7F
  ! SN76489 Programmable Sound Generator (3 Square + 1 Noise)
  integer, parameter :: PSG_BASE = 0x7F
  integer, parameter :: PSG_CH0_FREQ_ADDR = 0x00
  integer, parameter :: PSG_CH1_FREQ_ADDR = 0x02
  integer, parameter :: PSG_CH2_FREQ_ADDR = 0x04
  integer, parameter :: PSG_CH3_CONFIG_ADDR = 0x06
  integer, parameter :: PSG_CH3_CONFIG_TYPE_BIT = 0  ! Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
  integer, parameter :: PSG_CH3_CONFIG_VOLUME_BIT = 0  ! Volume (0-15)
  integer, parameter :: PSG_CH0_VOLUME_ADDR = 0x01
  integer, parameter :: PSG_CH1_VOLUME_ADDR = 0x03
  integer, parameter :: PSG_CH2_VOLUME_ADDR = 0x05
  ! I/O Port Registers
  integer, parameter :: PORTS_BASE = 0x3F
  integer, parameter :: PORTS_PORT_A_ADDR = 0x3F
  integer, parameter :: PORTS_PORT_A_UP_BIT = 0  ! Up (0=pressed)
  integer, parameter :: PORTS_PORT_A_DOWN_BIT = 1  ! Down (0=pressed)
  integer, parameter :: PORTS_PORT_A_LEFT_BIT = 2  ! Left (0=pressed)
  integer, parameter :: PORTS_PORT_A_RIGHT_BIT = 3  ! Right (0=pressed)
  integer, parameter :: PORTS_PORT_A_TR_BIT = 4  ! Button TR (0=pressed)
  integer, parameter :: PORTS_PORT_A_TL_BIT = 5  ! Button TL (0=pressed)
  integer, parameter :: PORTS_PORT_B_ADDR = 0x3F
  integer, parameter :: PORTS_PORT_B_UP_BIT = 0  ! Up (0=pressed)
  integer, parameter :: PORTS_PORT_B_DOWN_BIT = 1  ! Down (0=pressed)
  integer, parameter :: PORTS_PORT_B_LEFT_BIT = 2  ! Left (0=pressed)
  integer, parameter :: PORTS_PORT_B_RIGHT_BIT = 3  ! Right (0=pressed)
  integer, parameter :: PORTS_PORT_B_TR_BIT = 4  ! Button TR (0=pressed)
  integer, parameter :: PORTS_PORT_B_TL_BIT = 5  ! Button TL (0=pressed)
  integer, parameter :: PORTS_PORT_A_DDR_ADDR = 0x3F
  integer, parameter :: PORTS_PORT_B_DDR_ADDR = 0x3F
  ! Sega Mapper (Memory Bank Switching)
  integer, parameter :: SEGAMAPPER_BASE = 0xFFFD
  integer, parameter :: SEGAMAPPER_ROM_BANK0_ADDR = 0xFFFD
  integer, parameter :: SEGAMAPPER_ROM_BANK1_ADDR = 0xFFFE
  integer, parameter :: SEGAMAPPER_ROM_BANK2_ADDR = 0xFFFF
  ! Memory Mapper Control
  integer, parameter :: MAPPER_BASE = 0xFFFF
  integer, parameter :: MAPPER_SRAM_BANK_ADDR = 0xFFF8

  ! 中断向量定义
  integer, parameter :: INT_NMI = 0  ! Non-Maskable Interrupt (Pause button / V-Blank)
  integer, parameter :: INT_INT_VBLANK = 1  ! V-Blank Interrupt (Frame end)
  integer, parameter :: INT_INT_LINE = 2  ! Scanline Interrupt (Line counter match)
  integer, parameter :: INT_INT_EXT = 3  ! External I/O Interrupt

  ! 引脚定义
  integer, parameter :: PIN_A = 1  ! Power Supply
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_PHI = 3  ! System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
  integer, parameter :: PIN_RESET = 4  ! Reset (active low)
  integer, parameter :: PIN_M1 = 5  ! Machine Cycle 1 (instruction fetch)
  integer, parameter :: PIN_MREQ = 6  ! Memory Request
  integer, parameter :: PIN_IORQ = 7  ! I/O Request
  integer, parameter :: PIN_RD = 8  ! Read Strobe
  integer, parameter :: PIN_WR = 9  ! Write Strobe
  integer, parameter :: PIN_HALT = 10  ! Halt State
  integer, parameter :: PIN_WAIT = 11  ! Wait State Request
  integer, parameter :: PIN_INT = 12  ! Interrupt Request (active low)
  integer, parameter :: PIN_NMI = 13  ! Non-Maskable Interrupt (active low)
  integer, parameter :: PIN_BUSRQ = 14  ! Bus Request (active low)
  integer, parameter :: PIN_BUSAK = 15  ! Bus Acknowledge (active low)
  integer, parameter :: PIN_A0 = 16  ! Address Bus Bit 0
  integer, parameter :: PIN_A1 = 17  ! Address Bus Bit 1
  integer, parameter :: PIN_A2 = 18  ! Address Bus Bit 2
  integer, parameter :: PIN_A3 = 19  ! Address Bus Bit 3
  integer, parameter :: PIN_A4 = 20  ! Address Bus Bit 4
  integer, parameter :: PIN_A5 = 21  ! Address Bus Bit 5
  integer, parameter :: PIN_A6 = 22  ! Address Bus Bit 6
  integer, parameter :: PIN_A7 = 23  ! Address Bus Bit 7
  integer, parameter :: PIN_A8 = 24  ! Address Bus Bit 8
  integer, parameter :: PIN_A9 = 25  ! Address Bus Bit 9
  integer, parameter :: PIN_A10 = 26  ! Address Bus Bit 10
  integer, parameter :: PIN_A11 = 27  ! Address Bus Bit 11
  integer, parameter :: PIN_A12 = 28  ! Address Bus Bit 12
  integer, parameter :: PIN_A13 = 29  ! Address Bus Bit 13
  integer, parameter :: PIN_A14 = 30  ! Address Bus Bit 14
  integer, parameter :: PIN_A15 = 31  ! Address Bus Bit 15
  integer, parameter :: PIN_D0 = 32  ! Data Bus Bit 0
  integer, parameter :: PIN_D1 = 33  ! Data Bus Bit 1
  integer, parameter :: PIN_D2 = 34  ! Data Bus Bit 2
  integer, parameter :: PIN_D3 = 35  ! Data Bus Bit 3
  integer, parameter :: PIN_D4 = 36  ! Data Bus Bit 4
  integer, parameter :: PIN_D5 = 37  ! Data Bus Bit 5
  integer, parameter :: PIN_D6 = 38  ! Data Bus Bit 6
  integer, parameter :: PIN_D7 = 39  ! Data Bus Bit 7
  integer, parameter :: PIN_AUDIO_OUT = 40  ! Audio Output
  integer, parameter :: PIN_VIDEO_SYNC = 41  ! Composite Video Sync
  integer, parameter :: PIN_VIDEO_OUT = 42  ! Composite Video Output

end module zilog_z80_device
