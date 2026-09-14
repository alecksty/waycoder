! NeoGeo-68000 设备定义 - Fortran 模块
! 生成自: SNK/M68K/NeoGeo-68000
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
! CPU架构: MC68000
! 位宽: 32位
! 时钟频率: 12000000 Hz

module neogeo_68000_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: D0_ADDR = 0x00  ! Data Register 0
  integer, parameter :: D1_ADDR = 0x04  ! Data Register 1
  integer, parameter :: D2_ADDR = 0x08  ! Data Register 2
  integer, parameter :: D3_ADDR = 0x0C  ! Data Register 3
  integer, parameter :: D4_ADDR = 0x10  ! Data Register 4
  integer, parameter :: D5_ADDR = 0x14  ! Data Register 5
  integer, parameter :: D6_ADDR = 0x18  ! Data Register 6
  integer, parameter :: D7_ADDR = 0x1C  ! Data Register 7
  integer, parameter :: A0_ADDR = 0x20  ! Address Register 0
  integer, parameter :: A1_ADDR = 0x24  ! Address Register 1
  integer, parameter :: A2_ADDR = 0x28  ! Address Register 2
  integer, parameter :: A3_ADDR = 0x2C  ! Address Register 3
  integer, parameter :: A4_ADDR = 0x30  ! Address Register 4
  integer, parameter :: A5_ADDR = 0x34  ! Address Register 5
  integer, parameter :: A6_ADDR = 0x38  ! Address Register 6
  integer, parameter :: A7_ADDR = 0x3C  ! User Stack Pointer (USP)
  integer, parameter :: SP_ADDR = 0x3C  ! Supervisor Stack Pointer (SSP)
  integer, parameter :: PC_ADDR = 0x40  ! Program Counter
  integer, parameter :: SR_ADDR = 0x44  ! Status Register
  integer, parameter :: SR_C_BIT = 0  ! Carry
  integer, parameter :: SR_V_BIT = 1  ! Overflow
  integer, parameter :: SR_Z_BIT = 2  ! Zero
  integer, parameter :: SR_N_BIT = 3  ! Negative
  integer, parameter :: SR_X_BIT = 4  ! Extend
  integer, parameter :: SR_I0_BIT = 8  ! Interrupt Mask 0
  integer, parameter :: SR_I1_BIT = 9  ! Interrupt Mask 1
  integer, parameter :: SR_I2_BIT = 10  ! Interrupt Mask 2
  integer, parameter :: SR_M_BIT = 11  ! Master/Interrupt
  integer, parameter :: SR_S_BIT = 13  ! Supervisor/User
  integer, parameter :: SR_T0_BIT = 14  ! Trace Mode 0
  integer, parameter :: SR_T1_BIT = 15  ! Trace Mode 1

  ! 内存段定义
  integer, parameter :: WORK_RAM_START = 0x100000
  integer, parameter :: WORK_RAM_END = 0x10FFFF
  integer, parameter :: WORK_RAM_SIZE = 65536  ! Work RAM (64KB)
  integer, parameter :: BACKUP_RAM_START = 0x200000
  integer, parameter :: BACKUP_RAM_END = 0x20FFFF
  integer, parameter :: BACKUP_RAM_SIZE = 65536  ! Backup SRAM (battery-backed, 64KB)
  integer, parameter :: FIX_ROM_START = 0x000000
  integer, parameter :: FIX_ROM_END = 0x07FFFF
  integer, parameter :: FIX_ROM_SIZE = 524288  ! Fix Layer ROM (512KB)
  integer, parameter :: SPR_ROM_START = 0x400000
  integer, parameter :: SPR_ROM_END = 0x4FFFFF
  integer, parameter :: SPR_ROM_SIZE = 1048576  ! Sprite ROM (up to 1MB)
  integer, parameter :: AUDIO_ROM_START = 0x800000
  integer, parameter :: AUDIO_ROM_END = 0x80FFFF
  integer, parameter :: AUDIO_ROM_SIZE = 65536  ! Audio ROM (up to 64KB)
  integer, parameter :: CART_ROM_START = 0xC00000
  integer, parameter :: CART_ROM_END = 0xC7FFFF
  integer, parameter :: CART_ROM_SIZE = 524288  ! Cartridge ROM (up to 512KB, expandable)
  integer, parameter :: IO_AREA_START = 0x300000
  integer, parameter :: IO_AREA_END = 0x3FFFFF
  integer, parameter :: IO_AREA_SIZE = 1048576  ! I/O Area (VDP, YM2610, Z80 port, etc.)
  integer, parameter :: Z80_RAM_START = 0x10000
  integer, parameter :: Z80_RAM_END = 0x107FF
  integer, parameter :: Z80_RAM_SIZE = 2048  ! Z80 Work RAM (2KB)

  ! 外设定义
  ! Z80 Audio Coprocessor @ 4MHz
  integer, parameter :: Z80_BASE = 0x300000
  integer, parameter :: Z80_Z80_A_ADDR = 0x00
  integer, parameter :: Z80_Z80_F_ADDR = 0x01
  integer, parameter :: Z80_Z80_B_ADDR = 0x02
  integer, parameter :: Z80_Z80_C_ADDR = 0x03
  integer, parameter :: Z80_Z80_D_ADDR = 0x04
  integer, parameter :: Z80_Z80_E_ADDR = 0x05
  integer, parameter :: Z80_Z80_H_ADDR = 0x06
  integer, parameter :: Z80_Z80_L_ADDR = 0x07
  integer, parameter :: Z80_Z80_AF_ADDR = 0x08
  integer, parameter :: Z80_Z80_BC_ADDR = 0x0A
  integer, parameter :: Z80_Z80_DE_ADDR = 0x0C
  integer, parameter :: Z80_Z80_HL_ADDR = 0x0E
  integer, parameter :: Z80_Z80_IX_ADDR = 0x10
  integer, parameter :: Z80_Z80_IY_ADDR = 0x12
  integer, parameter :: Z80_Z80_SP_ADDR = 0x14
  integer, parameter :: Z80_Z80_PC_ADDR = 0x16
  integer, parameter :: Z80_Z80_I_ADDR = 0x18
  integer, parameter :: Z80_Z80_R_ADDR = 0x19
  integer, parameter :: Z80_Z80_IM_ADDR = 0x1A
  integer, parameter :: Z80_Z80_BUSREQ_ADDR = 0x1E
  integer, parameter :: Z80_Z80_RESET_ADDR = 0x1F
  ! Yamaha YM2610 FM + ADPCM Audio Generator
  integer, parameter :: YM2610_BASE = 0x300000
  integer, parameter :: YM2610_YM_ADDR_A0_ADDR = 0x00
  integer, parameter :: YM2610_YM_DATA_A0_ADDR = 0x01
  integer, parameter :: YM2610_YM_ADDR_A1_ADDR = 0x02
  integer, parameter :: YM2610_YM_DATA_A1_ADDR = 0x03
  integer, parameter :: YM2610_YM_ADDR_B0_ADDR = 0x04
  integer, parameter :: YM2610_YM_DATA_B0_ADDR = 0x05
  integer, parameter :: YM2610_YM_TEST_ADDR = 0x08
  integer, parameter :: YM2610_YM_FM_CH0_FREQ_L_ADDR = 0xA0
  integer, parameter :: YM2610_YM_FM_CH0_FREQ_H_ADDR = 0xA4
  integer, parameter :: YM2610_YM_FM_CH1_FREQ_L_ADDR = 0xA1
  integer, parameter :: YM2610_YM_FM_CH1_FREQ_H_ADDR = 0xA5
  integer, parameter :: YM2610_YM_FM_CH2_FREQ_L_ADDR = 0xA2
  integer, parameter :: YM2610_YM_FM_CH2_FREQ_H_ADDR = 0xA6
  integer, parameter :: YM2610_YM_FM_CH3_FREQ_L_ADDR = 0xA3
  integer, parameter :: YM2610_YM_FM_CH3_FREQ_H_ADDR = 0xA7
  integer, parameter :: YM2610_YM_FM_KEY_ON_ADDR = 0x28
  integer, parameter :: YM2610_YM_FM_CH0_ALG_ADDR = 0xB0
  integer, parameter :: YM2610_YM_FM_CH1_ALG_ADDR = 0xB1
  integer, parameter :: YM2610_YM_FM_CH2_ALG_ADDR = 0xB2
  integer, parameter :: YM2610_YM_FM_CH3_ALG_ADDR = 0xB3
  integer, parameter :: YM2610_YM_FM_TIMER_H_ADDR = 0x24
  integer, parameter :: YM2610_YM_FM_TIMER_L_ADDR = 0x25
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_ADDR = 0x27
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_TIMER_A_START_BIT = 0  ! Timer A Start
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_TIMER_B_START_BIT = 1  ! Timer B Start
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_LOAD_A_BIT = 2  ! Load Timer A
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_LOAD_B_BIT = 3  ! Load Timer B
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A_BIT = 4  ! Timer A IRQ Enable
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B_BIT = 5  ! Timer B IRQ Enable
  integer, parameter :: YM2610_YM_FM_TIMER_CTRL_CSM_MODE_BIT = 7  ! CSM Mode (auto Key-On after timer A)
  integer, parameter :: YM2610_YM_FM_CH0_DETUNE_ADDR = 0x30
  integer, parameter :: YM2610_YM_FM_CH0_MUL_ADDR = 0x30
  integer, parameter :: YM2610_YM_FM_CH0_TL_ADDR = 0x40
  integer, parameter :: YM2610_YM_FM_CH0_KS_AR_ADDR = 0x50
  integer, parameter :: YM2610_YM_FM_CH0_AM_DR_ADDR = 0x60
  integer, parameter :: YM2610_YM_FM_CH0_SR_ADDR = 0x70
  integer, parameter :: YM2610_YM_FM_CH0_RR_SL_ADDR = 0x80
  integer, parameter :: YM2610_YM_FM_CH0_SSG_ADDR = 0x90
  integer, parameter :: YM2610_YM_SSG_CHA_FREQ_L_ADDR = 0x00
  integer, parameter :: YM2610_YM_SSG_CHA_FREQ_H_ADDR = 0x01
  integer, parameter :: YM2610_YM_SSG_CHB_FREQ_L_ADDR = 0x02
  integer, parameter :: YM2610_YM_SSG_CHB_FREQ_H_ADDR = 0x03
  integer, parameter :: YM2610_YM_SSG_CHC_FREQ_L_ADDR = 0x04
  integer, parameter :: YM2610_YM_SSG_CHC_FREQ_H_ADDR = 0x05
  integer, parameter :: YM2610_YM_SSG_CHA_VOL_ADDR = 0x08
  integer, parameter :: YM2610_YM_SSG_CHB_VOL_ADDR = 0x09
  integer, parameter :: YM2610_YM_SSG_CHC_VOL_ADDR = 0x0A
  integer, parameter :: YM2610_YM_SSG_MIXER_ADDR = 0x07
  integer, parameter :: YM2610_YM_SSG_ENV_FREQ_L_ADDR = 0x0B
  integer, parameter :: YM2610_YM_SSG_ENV_FREQ_H_ADDR = 0x0C
  integer, parameter :: YM2610_YM_SSG_ENV_SHAPE_ADDR = 0x0D
  integer, parameter :: YM2610_YM_SSG_IO_A_ADDR = 0x0E
  integer, parameter :: YM2610_YM_SSG_IO_B_ADDR = 0x0F
  integer, parameter :: YM2610_YM_ADPCM_STATUS_ADDR = 0x10
  integer, parameter :: YM2610_YM_ADPCM_START_ADDR = 0x11
  integer, parameter :: YM2610_YM_ADPCM_END_ADDR = 0x12
  integer, parameter :: YM2610_YM_ADPCM_VOL_L_ADDR = 0x13
  integer, parameter :: YM2610_YM_ADPCM_VOL_R_ADDR = 0x14
  integer, parameter :: YM2610_YM_DELTA_N_L_ADDR = 0x15
  integer, parameter :: YM2610_YM_DELTA_N_H_ADDR = 0x16
  integer, parameter :: YM2610_YM_ADPCM_B_START_ADDR = 0x18
  integer, parameter :: YM2610_YM_ADPCM_B_END_ADDR = 0x19
  integer, parameter :: YM2610_YM_ADPCM_B_VOL_ADDR = 0x1A
  integer, parameter :: YM2610_YM_ADPCM_B_CTRL_ADDR = 0x1B
  ! Neo Geo VDP (Video Display Processor)
  integer, parameter :: YGV628_BASE = 0x3C0000
  integer, parameter :: YGV628_VRAM_ADDR_L_ADDR = 0x00
  integer, parameter :: YGV628_VRAM_ADDR_H_ADDR = 0x01
  integer, parameter :: YGV628_VRAM_DATA_ADDR = 0x02
  integer, parameter :: YGV628_VRAM_READ_ADDR = 0x03
  integer, parameter :: YGV628_CRAM_ADDR_ADDR = 0x04
  integer, parameter :: YGV628_CRAM_DATA_ADDR = 0x05
  integer, parameter :: YGV628_VDP_STATUS_ADDR = 0x06
  integer, parameter :: YGV628_VDP_STATUS_VBLANK_BIT = 0  ! V-Blank Flag
  integer, parameter :: YGV628_VDP_STATUS_FIELD_BIT = 1  ! Field (0=even, 1=odd for interlace)
  integer, parameter :: YGV628_VDP_STATUS_ODD_FIELD_BIT = 1  ! Odd Field Flag
  integer, parameter :: YGV628_VDP_STATUS_DMA_BUSY_BIT = 2  ! DMA Busy
  integer, parameter :: YGV628_VDP_STATUS_SPRITE_OVERFLOW_BIT = 3  ! Sprite Overflow (more than 16 per line)
  integer, parameter :: YGV628_VDP_STATUS_SPRITE_COLLISION_BIT = 4  ! Sprite Collision
  integer, parameter :: YGV628_VDP_CTRL_ADDR = 0x07
  integer, parameter :: YGV628_VDP_CTRL_VRAM_INC_BIT = 0  ! VRAM Auto-Increment (0=+1, 1=+2)
  integer, parameter :: YGV628_VDP_CTRL_ROW_SCROLL_BIT = 1  ! Row Scroll Mode
  integer, parameter :: YGV628_VDP_CTRL_COL_SCROLL_BIT = 2  ! Column Scroll Mode
  integer, parameter :: YGV628_VDP_CTRL_FIX_DISP_BIT = 3  ! Fix Layer Display
  integer, parameter :: YGV628_VDP_CTRL_SPR_DISP_BIT = 4  ! Sprite Layer Display
  integer, parameter :: YGV628_VDP_CTRL_SCROLL2_DISP_BIT = 5  ! Scroll Layer 2 Display
  integer, parameter :: YGV628_VDP_CTRL_SCROLL1_DISP_BIT = 6  ! Scroll Layer 1 Display
  integer, parameter :: YGV628_VDP_CTRL_DMA_ENABLE_BIT = 7  ! DMA Enable
  integer, parameter :: YGV628_SCROLL1_BASE_ADDR = 0x08
  integer, parameter :: YGV628_SCROLL2_BASE_ADDR = 0x0A
  integer, parameter :: YGV628_SPR_BASE_ADDR = 0x0C
  integer, parameter :: YGV628_SPR_COUNT_ADDR = 0x0E
  integer, parameter :: YGV628_WINDOW_X_ADDR = 0x10
  integer, parameter :: YGV628_WINDOW_Y_ADDR = 0x11
  integer, parameter :: YGV628_WINDOW_W_ADDR = 0x12
  integer, parameter :: YGV628_WINDOW_H_ADDR = 0x13
  integer, parameter :: YGV628_LINE_SCROLL_L_ADDR = 0x14
  integer, parameter :: YGV628_LINE_SCROLL_H_ADDR = 0x15
  integer, parameter :: YGV628_RASTER_COMP_ADDR = 0x16
  integer, parameter :: YGV628_H_TIMING_ADDR = 0x18
  integer, parameter :: YGV628_V_TIMING_ADDR = 0x19
  integer, parameter :: YGV628_DMA_SRC_L_ADDR = 0x1A
  integer, parameter :: YGV628_DMA_SRC_H_ADDR = 0x1B
  integer, parameter :: YGV628_DMA_SRC_B_ADDR = 0x1C
  integer, parameter :: YGV628_DMA_DEST_L_ADDR = 0x1D
  integer, parameter :: YGV628_DMA_DEST_H_ADDR = 0x1E
  integer, parameter :: YGV628_DMA_COUNT_ADDR = 0x1F
  ! Neo Geo System Driver / Controller
  integer, parameter :: NEODRIVER_BASE = 0x310000
  integer, parameter :: NEODRIVER_PDI0_ADDR = 0x00
  integer, parameter :: NEODRIVER_PDI0_UP_BIT = 0  ! Up (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_DOWN_BIT = 1  ! Down (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_LEFT_BIT = 2  ! Left (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_RIGHT_BIT = 3  ! Right (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_A_BIT = 4  ! A Button (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_B_BIT = 5  ! B Button (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_C_BIT = 6  ! C Button (0=pressed)
  integer, parameter :: NEODRIVER_PDI0_D_BIT = 7  ! D Button (0=pressed)
  integer, parameter :: NEODRIVER_PDI1_ADDR = 0x01
  integer, parameter :: NEODRIVER_PDI2_ADDR = 0x02
  integer, parameter :: NEODRIVER_PDI3_ADDR = 0x03
  integer, parameter :: NEODRIVER_PDO0_ADDR = 0x04
  integer, parameter :: NEODRIVER_PDO1_ADDR = 0x05
  integer, parameter :: NEODRIVER_PDO2_ADDR = 0x06
  integer, parameter :: NEODRIVER_PDO3_ADDR = 0x07
  integer, parameter :: NEODRIVER_DIPSEL1_ADDR = 0x08
  integer, parameter :: NEODRIVER_DIPSEL1_COIN_SELECT_BIT = 0  ! Coin Select (0=common, 1=1 coin 1 credit)
  integer, parameter :: NEODRIVER_DIPSEL1_FREE_PLAY_BIT = 1  ! Free Play
  integer, parameter :: NEODRIVER_DIPSEL1_DEMO_SOUND_BIT = 2  ! Demo Sound
  integer, parameter :: NEODRIVER_DIPSEL1_CHIP_MODE_BIT = 3  ! Chip Mode (0=AES, 1=MVS)
  integer, parameter :: NEODRIVER_DIPSEL1_CONTROLLER_TYPE_BIT = 4  ! Controller Type (0=standard, 1=keyboard)
  integer, parameter :: NEODRIVER_DIPSEL2_ADDR = 0x09
  integer, parameter :: NEODRIVER_DIPSEL3_ADDR = 0x0A
  integer, parameter :: NEODRIVER_DIPSEL4_ADDR = 0x0B
  integer, parameter :: NEODRIVER_SYSCTRL_ADDR = 0x0C
  integer, parameter :: NEODRIVER_SYSCTRL_RTSEL_BIT = 0  ! Real Time Switch Select
  integer, parameter :: NEODRIVER_SYSCTRL_RESERVED0_BIT = 1  ! Reserved
  integer, parameter :: NEODRIVER_SYSCTRL_SCC_BIT = 2  ! System Clock Control
  integer, parameter :: NEODRIVER_SYSCTRL_PHEN_BIT = 3  ! PHEN (bus timing)
  integer, parameter :: NEODRIVER_SYSCTRL_PCK2_BIT = 4  ! PCK2 (bus timing)
  integer, parameter :: NEODRIVER_SYSCTRL_PCK1_BIT = 5  ! PCK1 (bus timing)
  integer, parameter :: NEODRIVER_SYSCTRL_CKDIV2_BIT = 6  ! Clock Divide by 2
  integer, parameter :: NEODRIVER_SYSCTRL_FEFIX_BIT = 7  ! FE Fix
  integer, parameter :: NEODRIVER_IRQMASK_ADDR = 0x0D
  integer, parameter :: NEODRIVER_IRQMASK_VBLANK_MASK_BIT = 0  ! V-Blank Interrupt Mask
  integer, parameter :: NEODRIVER_IRQMASK_HBLANK_MASK_BIT = 1  ! H-Blank Interrupt Mask
  integer, parameter :: NEODRIVER_IRQMASK_VECTOR_IN_MASK_BIT = 2  ! Vector In (from Z80) Mask
  integer, parameter :: NEODRIVER_IRQMASK_SYSTEM_IN_MASK_BIT = 3  ! System Input (JAMMA) Mask
  integer, parameter :: NEODRIVER_IRQFLAG_ADDR = 0x0E
  integer, parameter :: NEODRIVER_SECAM_MODE_ADDR = 0x0F
  ! Controller Port 1
  integer, parameter :: CONTROLLER1_BASE = 0x310000
  integer, parameter :: CONTROLLER1_PDI0_ADDR = 0x00
  ! Controller Port 2
  integer, parameter :: CONTROLLER2_BASE = 0x310001
  integer, parameter :: CONTROLLER2_PDI1_ADDR = 0x00
  ! Memory Card Interface
  integer, parameter :: MEMORY_CARD_BASE = 0x320000
  integer, parameter :: MEMORY_CARD_CARD_DATA_ADDR = 0x00
  integer, parameter :: MEMORY_CARD_CARD_STATUS_ADDR = 0x01
  integer, parameter :: MEMORY_CARD_CARD_STATUS_INSERTED_BIT = 0  ! Card Inserted (0=yes)
  integer, parameter :: MEMORY_CARD_CARD_STATUS_WRITE_PROTECT_BIT = 1  ! Write Protected (0=yes)
  integer, parameter :: MEMORY_CARD_CARD_STATUS_READY_BIT = 2  ! Ready for I/O
  integer, parameter :: MEMORY_CARD_CARD_CTRL_ADDR = 0x02
  ! Cartridge Bank Switching
  integer, parameter :: CART_BANK_BASE = 0x2FFFF0
  integer, parameter :: CART_BANK_BANK_REG_ADDR = 0x00

  ! 中断向量定义
  integer, parameter :: INT_RESET_SP = 1  ! Reset Initial Stack Pointer
  integer, parameter :: INT_RESET_PC = 2  ! Reset Initial PC
  integer, parameter :: INT_BUS_ERROR = 3  ! Bus Error
  integer, parameter :: INT_ADDRESS_ERROR = 4  ! Address Error
  integer, parameter :: INT_ILLEGAL_INSTR = 5  ! Illegal Instruction
  integer, parameter :: INT_ZERO_DIVIDE = 6  ! Zero Divide
  integer, parameter :: INT_CHK_EXCEPTION = 7  ! CHK Exception
  integer, parameter :: INT_TRAPV = 8  ! TRAPV Exception
  integer, parameter :: INT_PRIVILEGE = 9  ! Privilege Violation
  integer, parameter :: INT_TRACE = 10  ! Trace
  integer, parameter :: INT_LINE_A = 11  ! Line 1010 Emulator
  integer, parameter :: INT_LINE_F = 12  ! Line 1111 Emulator
  integer, parameter :: INT_IRQ1 = 24  ! H-Blank / VDP Interrupt (raster)
  integer, parameter :: INT_IRQ2 = 25  ! V-Blank / Frame End Interrupt
  integer, parameter :: INT_IRQ3 = 26  ! System Controller / Z80 Vector In
  integer, parameter :: INT_IRQ4 = 27  ! JAMMA / System Input
  integer, parameter :: INT_IRQ5 = 28  ! Z80 Interrupt Request
  integer, parameter :: INT_TRAP0 = 32  ! TRAP #0 (system call)
  integer, parameter :: INT_TRAP1 = 33  ! TRAP #1

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! Power Supply (5V)
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! System Clock (12MHz for 68K)
  integer, parameter :: PIN_RESET = 4  ! Reset (active low)
  integer, parameter :: PIN_HALT = 5  ! Halt (stops CPU)
  integer, parameter :: PIN_NMI = 6  ! Non-Maskable Interrupt
  integer, parameter :: PIN_IPL0 = 7  ! Interrupt Priority Level 0
  integer, parameter :: PIN_IPL1 = 8  ! Interrupt Priority Level 1
  integer, parameter :: PIN_IPL2 = 9  ! Interrupt Priority Level 2
  integer, parameter :: PIN_DTACK = 10  ! Data Acknowledge (active low)
  integer, parameter :: PIN_BERR = 11  ! Bus Error (active low)
  integer, parameter :: PIN_BR = 12  ! Bus Request (active low)
  integer, parameter :: PIN_BG = 13  ! Bus Grant (active low)
  integer, parameter :: PIN_A0 = 14  ! Address Bus Bit 0
  integer, parameter :: PIN_A1 = 15  ! Address Bus Bit 1
  integer, parameter :: PIN_A2 = 16  ! Address Bus Bit 2
  integer, parameter :: PIN_A3 = 17  ! Address Bus Bit 3
  integer, parameter :: PIN_A4 = 18  ! Address Bus Bit 4
  integer, parameter :: PIN_A5 = 19  ! Address Bus Bit 5
  integer, parameter :: PIN_A6 = 20  ! Address Bus Bit 6
  integer, parameter :: PIN_A7 = 21  ! Address Bus Bit 7
  integer, parameter :: PIN_A8 = 22  ! Address Bus Bit 8
  integer, parameter :: PIN_A9 = 23  ! Address Bus Bit 9
  integer, parameter :: PIN_A10 = 24  ! Address Bus Bit 10
  integer, parameter :: PIN_A11 = 25  ! Address Bus Bit 11
  integer, parameter :: PIN_A12 = 26  ! Address Bus Bit 12
  integer, parameter :: PIN_A13 = 27  ! Address Bus Bit 13
  integer, parameter :: PIN_A14 = 28  ! Address Bus Bit 14
  integer, parameter :: PIN_A15 = 29  ! Address Bus Bit 15
  integer, parameter :: PIN_A16 = 30  ! Address Bus Bit 16
  integer, parameter :: PIN_A17 = 31  ! Address Bus Bit 17
  integer, parameter :: PIN_A18 = 32  ! Address Bus Bit 18
  integer, parameter :: PIN_A19 = 33  ! Address Bus Bit 19
  integer, parameter :: PIN_A20 = 34  ! Address Bus Bit 20
  integer, parameter :: PIN_A21 = 35  ! Address Bus Bit 21
  integer, parameter :: PIN_A22 = 36  ! Address Bus Bit 22
  integer, parameter :: PIN_A23 = 37  ! Address Bus Bit 23
  integer, parameter :: PIN_D0 = 38  ! Data Bus Bit 0
  integer, parameter :: PIN_D1 = 39  ! Data Bus Bit 1
  integer, parameter :: PIN_D2 = 40  ! Data Bus Bit 2
  integer, parameter :: PIN_D3 = 41  ! Data Bus Bit 3
  integer, parameter :: PIN_D4 = 42  ! Data Bus Bit 4
  integer, parameter :: PIN_D5 = 43  ! Data Bus Bit 5
  integer, parameter :: PIN_D6 = 44  ! Data Bus Bit 6
  integer, parameter :: PIN_D7 = 45  ! Data Bus Bit 7
  integer, parameter :: PIN_D8 = 46  ! Data Bus Bit 8
  integer, parameter :: PIN_D9 = 47  ! Data Bus Bit 9
  integer, parameter :: PIN_D10 = 48  ! Data Bus Bit 10
  integer, parameter :: PIN_D11 = 49  ! Data Bus Bit 11
  integer, parameter :: PIN_D12 = 50  ! Data Bus Bit 12
  integer, parameter :: PIN_D13 = 51  ! Data Bus Bit 13
  integer, parameter :: PIN_D14 = 52  ! Data Bus Bit 14
  integer, parameter :: PIN_D15 = 53  ! Data Bus Bit 15
  integer, parameter :: PIN_AS = 54  ! Address Strobe (active low)
  integer, parameter :: PIN_UDS = 55  ! Upper Data Strobe (active low)
  integer, parameter :: PIN_LDS = 56  ! Lower Data Strobe (active low)
  integer, parameter :: PIN_R_W = 57  ! Read/Write (1=Read, 0=Write)
  integer, parameter :: PIN_FC0 = 58  ! Function Code 0
  integer, parameter :: PIN_FC1 = 59  ! Function Code 1
  integer, parameter :: PIN_FC2 = 60  ! Function Code 2
  integer, parameter :: PIN_E = 61  ! E Clock (Enable, for Z80 sync)
  integer, parameter :: PIN_VPA = 62  ! Valid Peripheral Address (for Z80 I/O)
  integer, parameter :: PIN_VM = 63  ! Valid Memory (for Z80 memory access)
  integer, parameter :: PIN_BKGR = 64  ! Background Audio Mix (analog output)
  integer, parameter :: PIN_AUDIO_OUT = 65  ! Main Audio Output (Left)
  integer, parameter :: PIN_AUDIO_R = 66  ! Audio Right Channel
  integer, parameter :: PIN_VIDEO_R = 67  ! Video Output Red
  integer, parameter :: PIN_VIDEO_G = 68  ! Video Output Green
  integer, parameter :: PIN_VIDEO_B = 69  ! Video Output Blue
  integer, parameter :: PIN_SYNC = 70  ! Video Sync

end module neogeo_68000_device
