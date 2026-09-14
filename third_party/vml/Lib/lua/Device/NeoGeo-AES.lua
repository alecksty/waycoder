--[[
  NeoGeo-68000设备定义 - Lua模块
  生成自: SNK/M68K/NeoGeo-68000
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
  CPU架构: MC68000
  位宽: 32位
  时钟频率: 12000000 Hz
]]

local NeoGeo_68000 = {}

-- 设备信息
NeoGeo_68000.DEVICE_NAME = "NeoGeo-68000"
NeoGeo_68000.MANUFACTURER = "SNK"
NeoGeo_68000.FAMILY = "M68K"
NeoGeo_68000.VERSION = "1.0"
NeoGeo_68000.ARCHITECTURE = "MC68000"
NeoGeo_68000.BITS = 32
NeoGeo_68000.CLOCK_FREQUENCY = 12000000

-- 寄存器地址定义
NeoGeo_68000.D0_ADDR = 0x00  -- Data Register 0
NeoGeo_68000.D1_ADDR = 0x04  -- Data Register 1
NeoGeo_68000.D2_ADDR = 0x08  -- Data Register 2
NeoGeo_68000.D3_ADDR = 0x0C  -- Data Register 3
NeoGeo_68000.D4_ADDR = 0x10  -- Data Register 4
NeoGeo_68000.D5_ADDR = 0x14  -- Data Register 5
NeoGeo_68000.D6_ADDR = 0x18  -- Data Register 6
NeoGeo_68000.D7_ADDR = 0x1C  -- Data Register 7
NeoGeo_68000.A0_ADDR = 0x20  -- Address Register 0
NeoGeo_68000.A1_ADDR = 0x24  -- Address Register 1
NeoGeo_68000.A2_ADDR = 0x28  -- Address Register 2
NeoGeo_68000.A3_ADDR = 0x2C  -- Address Register 3
NeoGeo_68000.A4_ADDR = 0x30  -- Address Register 4
NeoGeo_68000.A5_ADDR = 0x34  -- Address Register 5
NeoGeo_68000.A6_ADDR = 0x38  -- Address Register 6
NeoGeo_68000.A7_ADDR = 0x3C  -- User Stack Pointer (USP)
NeoGeo_68000.SP_ADDR = 0x3C  -- Supervisor Stack Pointer (SSP)
NeoGeo_68000.PC_ADDR = 0x40  -- Program Counter
NeoGeo_68000.SR_ADDR = 0x44  -- Status Register
NeoGeo_68000.SR_C_BIT = 0  -- Carry
NeoGeo_68000.SR_V_BIT = 1  -- Overflow
NeoGeo_68000.SR_Z_BIT = 2  -- Zero
NeoGeo_68000.SR_N_BIT = 3  -- Negative
NeoGeo_68000.SR_X_BIT = 4  -- Extend
NeoGeo_68000.SR_I0_BIT = 8  -- Interrupt Mask 0
NeoGeo_68000.SR_I1_BIT = 9  -- Interrupt Mask 1
NeoGeo_68000.SR_I2_BIT = 10  -- Interrupt Mask 2
NeoGeo_68000.SR_M_BIT = 11  -- Master/Interrupt
NeoGeo_68000.SR_S_BIT = 13  -- Supervisor/User
NeoGeo_68000.SR_T0_BIT = 14  -- Trace Mode 0
NeoGeo_68000.SR_T1_BIT = 15  -- Trace Mode 1

-- 内存段定义
NeoGeo_68000.WORK_RAM_START = 0x100000
NeoGeo_68000.WORK_RAM_END = 0x10FFFF
NeoGeo_68000.WORK_RAM_SIZE = 65536  -- Work RAM (64KB)
NeoGeo_68000.BACKUP_RAM_START = 0x200000
NeoGeo_68000.BACKUP_RAM_END = 0x20FFFF
NeoGeo_68000.BACKUP_RAM_SIZE = 65536  -- Backup SRAM (battery-backed, 64KB)
NeoGeo_68000.FIX_ROM_START = 0x000000
NeoGeo_68000.FIX_ROM_END = 0x07FFFF
NeoGeo_68000.FIX_ROM_SIZE = 524288  -- Fix Layer ROM (512KB)
NeoGeo_68000.SPR_ROM_START = 0x400000
NeoGeo_68000.SPR_ROM_END = 0x4FFFFF
NeoGeo_68000.SPR_ROM_SIZE = 1048576  -- Sprite ROM (up to 1MB)
NeoGeo_68000.AUDIO_ROM_START = 0x800000
NeoGeo_68000.AUDIO_ROM_END = 0x80FFFF
NeoGeo_68000.AUDIO_ROM_SIZE = 65536  -- Audio ROM (up to 64KB)
NeoGeo_68000.CART_ROM_START = 0xC00000
NeoGeo_68000.CART_ROM_END = 0xC7FFFF
NeoGeo_68000.CART_ROM_SIZE = 524288  -- Cartridge ROM (up to 512KB, expandable)
NeoGeo_68000.IO_AREA_START = 0x300000
NeoGeo_68000.IO_AREA_END = 0x3FFFFF
NeoGeo_68000.IO_AREA_SIZE = 1048576  -- I/O Area (VDP, YM2610, Z80 port, etc.)
NeoGeo_68000.Z80_RAM_START = 0x10000
NeoGeo_68000.Z80_RAM_END = 0x107FF
NeoGeo_68000.Z80_RAM_SIZE = 2048  -- Z80 Work RAM (2KB)

-- 外设定义
-- Z80 Audio Coprocessor @ 4MHz
NeoGeo_68000.Z80_BASE = 0x300000
NeoGeo_68000.Z80_Z80_A_ADDR = 0x00
NeoGeo_68000.Z80_Z80_F_ADDR = 0x01
NeoGeo_68000.Z80_Z80_B_ADDR = 0x02
NeoGeo_68000.Z80_Z80_C_ADDR = 0x03
NeoGeo_68000.Z80_Z80_D_ADDR = 0x04
NeoGeo_68000.Z80_Z80_E_ADDR = 0x05
NeoGeo_68000.Z80_Z80_H_ADDR = 0x06
NeoGeo_68000.Z80_Z80_L_ADDR = 0x07
NeoGeo_68000.Z80_Z80_AF_ADDR = 0x08
NeoGeo_68000.Z80_Z80_BC_ADDR = 0x0A
NeoGeo_68000.Z80_Z80_DE_ADDR = 0x0C
NeoGeo_68000.Z80_Z80_HL_ADDR = 0x0E
NeoGeo_68000.Z80_Z80_IX_ADDR = 0x10
NeoGeo_68000.Z80_Z80_IY_ADDR = 0x12
NeoGeo_68000.Z80_Z80_SP_ADDR = 0x14
NeoGeo_68000.Z80_Z80_PC_ADDR = 0x16
NeoGeo_68000.Z80_Z80_I_ADDR = 0x18
NeoGeo_68000.Z80_Z80_R_ADDR = 0x19
NeoGeo_68000.Z80_Z80_IM_ADDR = 0x1A
NeoGeo_68000.Z80_Z80_BUSREQ_ADDR = 0x1E
NeoGeo_68000.Z80_Z80_RESET_ADDR = 0x1F
-- Yamaha YM2610 FM + ADPCM Audio Generator
NeoGeo_68000.YM2610_BASE = 0x300000
NeoGeo_68000.YM2610_YM_ADDR_A0_ADDR = 0x00
NeoGeo_68000.YM2610_YM_DATA_A0_ADDR = 0x01
NeoGeo_68000.YM2610_YM_ADDR_A1_ADDR = 0x02
NeoGeo_68000.YM2610_YM_DATA_A1_ADDR = 0x03
NeoGeo_68000.YM2610_YM_ADDR_B0_ADDR = 0x04
NeoGeo_68000.YM2610_YM_DATA_B0_ADDR = 0x05
NeoGeo_68000.YM2610_YM_TEST_ADDR = 0x08
NeoGeo_68000.YM2610_YM_FM_CH0_FREQ_L_ADDR = 0xA0
NeoGeo_68000.YM2610_YM_FM_CH0_FREQ_H_ADDR = 0xA4
NeoGeo_68000.YM2610_YM_FM_CH1_FREQ_L_ADDR = 0xA1
NeoGeo_68000.YM2610_YM_FM_CH1_FREQ_H_ADDR = 0xA5
NeoGeo_68000.YM2610_YM_FM_CH2_FREQ_L_ADDR = 0xA2
NeoGeo_68000.YM2610_YM_FM_CH2_FREQ_H_ADDR = 0xA6
NeoGeo_68000.YM2610_YM_FM_CH3_FREQ_L_ADDR = 0xA3
NeoGeo_68000.YM2610_YM_FM_CH3_FREQ_H_ADDR = 0xA7
NeoGeo_68000.YM2610_YM_FM_KEY_ON_ADDR = 0x28
NeoGeo_68000.YM2610_YM_FM_CH0_ALG_ADDR = 0xB0
NeoGeo_68000.YM2610_YM_FM_CH1_ALG_ADDR = 0xB1
NeoGeo_68000.YM2610_YM_FM_CH2_ALG_ADDR = 0xB2
NeoGeo_68000.YM2610_YM_FM_CH3_ALG_ADDR = 0xB3
NeoGeo_68000.YM2610_YM_FM_TIMER_H_ADDR = 0x24
NeoGeo_68000.YM2610_YM_FM_TIMER_L_ADDR = 0x25
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_ADDR = 0x27
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_TIMER_A_START_BIT = 0  -- Timer A Start
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_TIMER_B_START_BIT = 1  -- Timer B Start
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_LOAD_A_BIT = 2  -- Load Timer A
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_LOAD_B_BIT = 3  -- Load Timer B
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A_BIT = 4  -- Timer A IRQ Enable
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B_BIT = 5  -- Timer B IRQ Enable
NeoGeo_68000.YM2610_YM_FM_TIMER_CTRL_CSM_MODE_BIT = 7  -- CSM Mode (auto Key-On after timer A)
NeoGeo_68000.YM2610_YM_FM_CH0_DETUNE_ADDR = 0x30
NeoGeo_68000.YM2610_YM_FM_CH0_MUL_ADDR = 0x30
NeoGeo_68000.YM2610_YM_FM_CH0_TL_ADDR = 0x40
NeoGeo_68000.YM2610_YM_FM_CH0_KS_AR_ADDR = 0x50
NeoGeo_68000.YM2610_YM_FM_CH0_AM_DR_ADDR = 0x60
NeoGeo_68000.YM2610_YM_FM_CH0_SR_ADDR = 0x70
NeoGeo_68000.YM2610_YM_FM_CH0_RR_SL_ADDR = 0x80
NeoGeo_68000.YM2610_YM_FM_CH0_SSG_ADDR = 0x90
NeoGeo_68000.YM2610_YM_SSG_CHA_FREQ_L_ADDR = 0x00
NeoGeo_68000.YM2610_YM_SSG_CHA_FREQ_H_ADDR = 0x01
NeoGeo_68000.YM2610_YM_SSG_CHB_FREQ_L_ADDR = 0x02
NeoGeo_68000.YM2610_YM_SSG_CHB_FREQ_H_ADDR = 0x03
NeoGeo_68000.YM2610_YM_SSG_CHC_FREQ_L_ADDR = 0x04
NeoGeo_68000.YM2610_YM_SSG_CHC_FREQ_H_ADDR = 0x05
NeoGeo_68000.YM2610_YM_SSG_CHA_VOL_ADDR = 0x08
NeoGeo_68000.YM2610_YM_SSG_CHB_VOL_ADDR = 0x09
NeoGeo_68000.YM2610_YM_SSG_CHC_VOL_ADDR = 0x0A
NeoGeo_68000.YM2610_YM_SSG_MIXER_ADDR = 0x07
NeoGeo_68000.YM2610_YM_SSG_ENV_FREQ_L_ADDR = 0x0B
NeoGeo_68000.YM2610_YM_SSG_ENV_FREQ_H_ADDR = 0x0C
NeoGeo_68000.YM2610_YM_SSG_ENV_SHAPE_ADDR = 0x0D
NeoGeo_68000.YM2610_YM_SSG_IO_A_ADDR = 0x0E
NeoGeo_68000.YM2610_YM_SSG_IO_B_ADDR = 0x0F
NeoGeo_68000.YM2610_YM_ADPCM_STATUS_ADDR = 0x10
NeoGeo_68000.YM2610_YM_ADPCM_START_ADDR = 0x11
NeoGeo_68000.YM2610_YM_ADPCM_END_ADDR = 0x12
NeoGeo_68000.YM2610_YM_ADPCM_VOL_L_ADDR = 0x13
NeoGeo_68000.YM2610_YM_ADPCM_VOL_R_ADDR = 0x14
NeoGeo_68000.YM2610_YM_DELTA_N_L_ADDR = 0x15
NeoGeo_68000.YM2610_YM_DELTA_N_H_ADDR = 0x16
NeoGeo_68000.YM2610_YM_ADPCM_B_START_ADDR = 0x18
NeoGeo_68000.YM2610_YM_ADPCM_B_END_ADDR = 0x19
NeoGeo_68000.YM2610_YM_ADPCM_B_VOL_ADDR = 0x1A
NeoGeo_68000.YM2610_YM_ADPCM_B_CTRL_ADDR = 0x1B
-- Neo Geo VDP (Video Display Processor)
NeoGeo_68000.YGV628_BASE = 0x3C0000
NeoGeo_68000.YGV628_VRAM_ADDR_L_ADDR = 0x00
NeoGeo_68000.YGV628_VRAM_ADDR_H_ADDR = 0x01
NeoGeo_68000.YGV628_VRAM_DATA_ADDR = 0x02
NeoGeo_68000.YGV628_VRAM_READ_ADDR = 0x03
NeoGeo_68000.YGV628_CRAM_ADDR_ADDR = 0x04
NeoGeo_68000.YGV628_CRAM_DATA_ADDR = 0x05
NeoGeo_68000.YGV628_VDP_STATUS_ADDR = 0x06
NeoGeo_68000.YGV628_VDP_STATUS_VBLANK_BIT = 0  -- V-Blank Flag
NeoGeo_68000.YGV628_VDP_STATUS_FIELD_BIT = 1  -- Field (0=even, 1=odd for interlace)
NeoGeo_68000.YGV628_VDP_STATUS_ODD_FIELD_BIT = 1  -- Odd Field Flag
NeoGeo_68000.YGV628_VDP_STATUS_DMA_BUSY_BIT = 2  -- DMA Busy
NeoGeo_68000.YGV628_VDP_STATUS_SPRITE_OVERFLOW_BIT = 3  -- Sprite Overflow (more than 16 per line)
NeoGeo_68000.YGV628_VDP_STATUS_SPRITE_COLLISION_BIT = 4  -- Sprite Collision
NeoGeo_68000.YGV628_VDP_CTRL_ADDR = 0x07
NeoGeo_68000.YGV628_VDP_CTRL_VRAM_INC_BIT = 0  -- VRAM Auto-Increment (0=+1, 1=+2)
NeoGeo_68000.YGV628_VDP_CTRL_ROW_SCROLL_BIT = 1  -- Row Scroll Mode
NeoGeo_68000.YGV628_VDP_CTRL_COL_SCROLL_BIT = 2  -- Column Scroll Mode
NeoGeo_68000.YGV628_VDP_CTRL_FIX_DISP_BIT = 3  -- Fix Layer Display
NeoGeo_68000.YGV628_VDP_CTRL_SPR_DISP_BIT = 4  -- Sprite Layer Display
NeoGeo_68000.YGV628_VDP_CTRL_SCROLL2_DISP_BIT = 5  -- Scroll Layer 2 Display
NeoGeo_68000.YGV628_VDP_CTRL_SCROLL1_DISP_BIT = 6  -- Scroll Layer 1 Display
NeoGeo_68000.YGV628_VDP_CTRL_DMA_ENABLE_BIT = 7  -- DMA Enable
NeoGeo_68000.YGV628_SCROLL1_BASE_ADDR = 0x08
NeoGeo_68000.YGV628_SCROLL2_BASE_ADDR = 0x0A
NeoGeo_68000.YGV628_SPR_BASE_ADDR = 0x0C
NeoGeo_68000.YGV628_SPR_COUNT_ADDR = 0x0E
NeoGeo_68000.YGV628_WINDOW_X_ADDR = 0x10
NeoGeo_68000.YGV628_WINDOW_Y_ADDR = 0x11
NeoGeo_68000.YGV628_WINDOW_W_ADDR = 0x12
NeoGeo_68000.YGV628_WINDOW_H_ADDR = 0x13
NeoGeo_68000.YGV628_LINE_SCROLL_L_ADDR = 0x14
NeoGeo_68000.YGV628_LINE_SCROLL_H_ADDR = 0x15
NeoGeo_68000.YGV628_RASTER_COMP_ADDR = 0x16
NeoGeo_68000.YGV628_H_TIMING_ADDR = 0x18
NeoGeo_68000.YGV628_V_TIMING_ADDR = 0x19
NeoGeo_68000.YGV628_DMA_SRC_L_ADDR = 0x1A
NeoGeo_68000.YGV628_DMA_SRC_H_ADDR = 0x1B
NeoGeo_68000.YGV628_DMA_SRC_B_ADDR = 0x1C
NeoGeo_68000.YGV628_DMA_DEST_L_ADDR = 0x1D
NeoGeo_68000.YGV628_DMA_DEST_H_ADDR = 0x1E
NeoGeo_68000.YGV628_DMA_COUNT_ADDR = 0x1F
-- Neo Geo System Driver / Controller
NeoGeo_68000.NEODRIVER_BASE = 0x310000
NeoGeo_68000.NEODRIVER_PDI0_ADDR = 0x00
NeoGeo_68000.NEODRIVER_PDI0_UP_BIT = 0  -- Up (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_DOWN_BIT = 1  -- Down (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_LEFT_BIT = 2  -- Left (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_RIGHT_BIT = 3  -- Right (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_A_BIT = 4  -- A Button (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_B_BIT = 5  -- B Button (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_C_BIT = 6  -- C Button (0=pressed)
NeoGeo_68000.NEODRIVER_PDI0_D_BIT = 7  -- D Button (0=pressed)
NeoGeo_68000.NEODRIVER_PDI1_ADDR = 0x01
NeoGeo_68000.NEODRIVER_PDI2_ADDR = 0x02
NeoGeo_68000.NEODRIVER_PDI3_ADDR = 0x03
NeoGeo_68000.NEODRIVER_PDO0_ADDR = 0x04
NeoGeo_68000.NEODRIVER_PDO1_ADDR = 0x05
NeoGeo_68000.NEODRIVER_PDO2_ADDR = 0x06
NeoGeo_68000.NEODRIVER_PDO3_ADDR = 0x07
NeoGeo_68000.NEODRIVER_DIPSEL1_ADDR = 0x08
NeoGeo_68000.NEODRIVER_DIPSEL1_COIN_SELECT_BIT = 0  -- Coin Select (0=common, 1=1 coin 1 credit)
NeoGeo_68000.NEODRIVER_DIPSEL1_FREE_PLAY_BIT = 1  -- Free Play
NeoGeo_68000.NEODRIVER_DIPSEL1_DEMO_SOUND_BIT = 2  -- Demo Sound
NeoGeo_68000.NEODRIVER_DIPSEL1_CHIP_MODE_BIT = 3  -- Chip Mode (0=AES, 1=MVS)
NeoGeo_68000.NEODRIVER_DIPSEL1_CONTROLLER_TYPE_BIT = 4  -- Controller Type (0=standard, 1=keyboard)
NeoGeo_68000.NEODRIVER_DIPSEL2_ADDR = 0x09
NeoGeo_68000.NEODRIVER_DIPSEL3_ADDR = 0x0A
NeoGeo_68000.NEODRIVER_DIPSEL4_ADDR = 0x0B
NeoGeo_68000.NEODRIVER_SYSCTRL_ADDR = 0x0C
NeoGeo_68000.NEODRIVER_SYSCTRL_RTSEL_BIT = 0  -- Real Time Switch Select
NeoGeo_68000.NEODRIVER_SYSCTRL_RESERVED0_BIT = 1  -- Reserved
NeoGeo_68000.NEODRIVER_SYSCTRL_SCC_BIT = 2  -- System Clock Control
NeoGeo_68000.NEODRIVER_SYSCTRL_PHEN_BIT = 3  -- PHEN (bus timing)
NeoGeo_68000.NEODRIVER_SYSCTRL_PCK2_BIT = 4  -- PCK2 (bus timing)
NeoGeo_68000.NEODRIVER_SYSCTRL_PCK1_BIT = 5  -- PCK1 (bus timing)
NeoGeo_68000.NEODRIVER_SYSCTRL_CKDIV2_BIT = 6  -- Clock Divide by 2
NeoGeo_68000.NEODRIVER_SYSCTRL_FEFIX_BIT = 7  -- FE Fix
NeoGeo_68000.NEODRIVER_IRQMASK_ADDR = 0x0D
NeoGeo_68000.NEODRIVER_IRQMASK_VBLANK_MASK_BIT = 0  -- V-Blank Interrupt Mask
NeoGeo_68000.NEODRIVER_IRQMASK_HBLANK_MASK_BIT = 1  -- H-Blank Interrupt Mask
NeoGeo_68000.NEODRIVER_IRQMASK_VECTOR_IN_MASK_BIT = 2  -- Vector In (from Z80) Mask
NeoGeo_68000.NEODRIVER_IRQMASK_SYSTEM_IN_MASK_BIT = 3  -- System Input (JAMMA) Mask
NeoGeo_68000.NEODRIVER_IRQFLAG_ADDR = 0x0E
NeoGeo_68000.NEODRIVER_SECAM_MODE_ADDR = 0x0F
-- Controller Port 1
NeoGeo_68000.CONTROLLER1_BASE = 0x310000
NeoGeo_68000.CONTROLLER1_PDI0_ADDR = 0x00
-- Controller Port 2
NeoGeo_68000.CONTROLLER2_BASE = 0x310001
NeoGeo_68000.CONTROLLER2_PDI1_ADDR = 0x00
-- Memory Card Interface
NeoGeo_68000.MEMORY_CARD_BASE = 0x320000
NeoGeo_68000.MEMORY_CARD_CARD_DATA_ADDR = 0x00
NeoGeo_68000.MEMORY_CARD_CARD_STATUS_ADDR = 0x01
NeoGeo_68000.MEMORY_CARD_CARD_STATUS_INSERTED_BIT = 0  -- Card Inserted (0=yes)
NeoGeo_68000.MEMORY_CARD_CARD_STATUS_WRITE_PROTECT_BIT = 1  -- Write Protected (0=yes)
NeoGeo_68000.MEMORY_CARD_CARD_STATUS_READY_BIT = 2  -- Ready for I/O
NeoGeo_68000.MEMORY_CARD_CARD_CTRL_ADDR = 0x02
-- Cartridge Bank Switching
NeoGeo_68000.CART_BANK_BASE = 0x2FFFF0
NeoGeo_68000.CART_BANK_BANK_REG_ADDR = 0x00

-- 中断向量定义
NeoGeo_68000.INT_RESET_SP = 1  -- Reset Initial Stack Pointer
NeoGeo_68000.INT_RESET_PC = 2  -- Reset Initial PC
NeoGeo_68000.INT_BUS_ERROR = 3  -- Bus Error
NeoGeo_68000.INT_ADDRESS_ERROR = 4  -- Address Error
NeoGeo_68000.INT_ILLEGAL_INSTR = 5  -- Illegal Instruction
NeoGeo_68000.INT_ZERO_DIVIDE = 6  -- Zero Divide
NeoGeo_68000.INT_CHK_EXCEPTION = 7  -- CHK Exception
NeoGeo_68000.INT_TRAPV = 8  -- TRAPV Exception
NeoGeo_68000.INT_PRIVILEGE = 9  -- Privilege Violation
NeoGeo_68000.INT_TRACE = 10  -- Trace
NeoGeo_68000.INT_LINE_A = 11  -- Line 1010 Emulator
NeoGeo_68000.INT_LINE_F = 12  -- Line 1111 Emulator
NeoGeo_68000.INT_IRQ1 = 24  -- H-Blank / VDP Interrupt (raster)
NeoGeo_68000.INT_IRQ2 = 25  -- V-Blank / Frame End Interrupt
NeoGeo_68000.INT_IRQ3 = 26  -- System Controller / Z80 Vector In
NeoGeo_68000.INT_IRQ4 = 27  -- JAMMA / System Input
NeoGeo_68000.INT_IRQ5 = 28  -- Z80 Interrupt Request
NeoGeo_68000.INT_TRAP0 = 32  -- TRAP #0 (system call)
NeoGeo_68000.INT_TRAP1 = 33  -- TRAP #1

-- 引脚定义
NeoGeo_68000.PIN_VCC = 1  -- Power Supply (5V)
NeoGeo_68000.PIN_GND = 2  -- Ground
NeoGeo_68000.PIN_CLK = 3  -- System Clock (12MHz for 68K)
NeoGeo_68000.PIN_RESET = 4  -- Reset (active low)
NeoGeo_68000.PIN_HALT = 5  -- Halt (stops CPU)
NeoGeo_68000.PIN_NMI = 6  -- Non-Maskable Interrupt
NeoGeo_68000.PIN_IPL0 = 7  -- Interrupt Priority Level 0
NeoGeo_68000.PIN_IPL1 = 8  -- Interrupt Priority Level 1
NeoGeo_68000.PIN_IPL2 = 9  -- Interrupt Priority Level 2
NeoGeo_68000.PIN_DTACK = 10  -- Data Acknowledge (active low)
NeoGeo_68000.PIN_BERR = 11  -- Bus Error (active low)
NeoGeo_68000.PIN_BR = 12  -- Bus Request (active low)
NeoGeo_68000.PIN_BG = 13  -- Bus Grant (active low)
NeoGeo_68000.PIN_A0 = 14  -- Address Bus Bit 0
NeoGeo_68000.PIN_A1 = 15  -- Address Bus Bit 1
NeoGeo_68000.PIN_A2 = 16  -- Address Bus Bit 2
NeoGeo_68000.PIN_A3 = 17  -- Address Bus Bit 3
NeoGeo_68000.PIN_A4 = 18  -- Address Bus Bit 4
NeoGeo_68000.PIN_A5 = 19  -- Address Bus Bit 5
NeoGeo_68000.PIN_A6 = 20  -- Address Bus Bit 6
NeoGeo_68000.PIN_A7 = 21  -- Address Bus Bit 7
NeoGeo_68000.PIN_A8 = 22  -- Address Bus Bit 8
NeoGeo_68000.PIN_A9 = 23  -- Address Bus Bit 9
NeoGeo_68000.PIN_A10 = 24  -- Address Bus Bit 10
NeoGeo_68000.PIN_A11 = 25  -- Address Bus Bit 11
NeoGeo_68000.PIN_A12 = 26  -- Address Bus Bit 12
NeoGeo_68000.PIN_A13 = 27  -- Address Bus Bit 13
NeoGeo_68000.PIN_A14 = 28  -- Address Bus Bit 14
NeoGeo_68000.PIN_A15 = 29  -- Address Bus Bit 15
NeoGeo_68000.PIN_A16 = 30  -- Address Bus Bit 16
NeoGeo_68000.PIN_A17 = 31  -- Address Bus Bit 17
NeoGeo_68000.PIN_A18 = 32  -- Address Bus Bit 18
NeoGeo_68000.PIN_A19 = 33  -- Address Bus Bit 19
NeoGeo_68000.PIN_A20 = 34  -- Address Bus Bit 20
NeoGeo_68000.PIN_A21 = 35  -- Address Bus Bit 21
NeoGeo_68000.PIN_A22 = 36  -- Address Bus Bit 22
NeoGeo_68000.PIN_A23 = 37  -- Address Bus Bit 23
NeoGeo_68000.PIN_D0 = 38  -- Data Bus Bit 0
NeoGeo_68000.PIN_D1 = 39  -- Data Bus Bit 1
NeoGeo_68000.PIN_D2 = 40  -- Data Bus Bit 2
NeoGeo_68000.PIN_D3 = 41  -- Data Bus Bit 3
NeoGeo_68000.PIN_D4 = 42  -- Data Bus Bit 4
NeoGeo_68000.PIN_D5 = 43  -- Data Bus Bit 5
NeoGeo_68000.PIN_D6 = 44  -- Data Bus Bit 6
NeoGeo_68000.PIN_D7 = 45  -- Data Bus Bit 7
NeoGeo_68000.PIN_D8 = 46  -- Data Bus Bit 8
NeoGeo_68000.PIN_D9 = 47  -- Data Bus Bit 9
NeoGeo_68000.PIN_D10 = 48  -- Data Bus Bit 10
NeoGeo_68000.PIN_D11 = 49  -- Data Bus Bit 11
NeoGeo_68000.PIN_D12 = 50  -- Data Bus Bit 12
NeoGeo_68000.PIN_D13 = 51  -- Data Bus Bit 13
NeoGeo_68000.PIN_D14 = 52  -- Data Bus Bit 14
NeoGeo_68000.PIN_D15 = 53  -- Data Bus Bit 15
NeoGeo_68000.PIN_AS = 54  -- Address Strobe (active low)
NeoGeo_68000.PIN_UDS = 55  -- Upper Data Strobe (active low)
NeoGeo_68000.PIN_LDS = 56  -- Lower Data Strobe (active low)
NeoGeo_68000.PIN_R_W = 57  -- Read/Write (1=Read, 0=Write)
NeoGeo_68000.PIN_FC0 = 58  -- Function Code 0
NeoGeo_68000.PIN_FC1 = 59  -- Function Code 1
NeoGeo_68000.PIN_FC2 = 60  -- Function Code 2
NeoGeo_68000.PIN_E = 61  -- E Clock (Enable, for Z80 sync)
NeoGeo_68000.PIN_VPA = 62  -- Valid Peripheral Address (for Z80 I/O)
NeoGeo_68000.PIN_VM = 63  -- Valid Memory (for Z80 memory access)
NeoGeo_68000.PIN_BKGR = 64  -- Background Audio Mix (analog output)
NeoGeo_68000.PIN_AUDIO_OUT = 65  -- Main Audio Output (Left)
NeoGeo_68000.PIN_AUDIO_R = 66  -- Audio Right Channel
NeoGeo_68000.PIN_VIDEO_R = 67  -- Video Output Red
NeoGeo_68000.PIN_VIDEO_G = 68  -- Video Output Green
NeoGeo_68000.PIN_VIDEO_B = 69  -- Video Output Blue
NeoGeo_68000.PIN_SYNC = 70  -- Video Sync

-- 设备类
function NeoGeo_68000.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["D0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "Data Register 0",
            value = 0
        }
        self.registers["D1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Data Register 1",
            value = 0
        }
        self.registers["D2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Data Register 2",
            value = 0
        }
        self.registers["D3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Data Register 3",
            value = 0
        }
        self.registers["D4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "Data Register 4",
            value = 0
        }
        self.registers["D5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "Data Register 5",
            value = 0
        }
        self.registers["D6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "Data Register 6",
            value = 0
        }
        self.registers["D7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "Data Register 7",
            value = 0
        }
        self.registers["A0"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "Address Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "Address Register 1",
            value = 0
        }
        self.registers["A2"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "Address Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Address Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "Address Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Address Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Address Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "User Stack Pointer (USP)",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Supervisor Stack Pointer (SSP)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x40,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["SR"] = {
            address = 0x44,
            size = 2,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["Z80"] = {
            base = 0x300000,
            type = "audio_cpu",
            description = "Z80 Audio Coprocessor @ 4MHz",
            registers = {}
        }
        
        local p = self.peripherals["Z80"]
        p.registers["Z80_A"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["Z80_F"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["Z80_B"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["Z80_C"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["Z80_D"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["Z80_E"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["Z80_H"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["Z80_L"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["Z80_AF_"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["Z80_BC_"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["Z80_DE_"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["Z80_HL_"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        p.registers["Z80_IX"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        p.registers["Z80_IY"] = {
            address = 0x12,
            size = 2,
            value = 0
        }
        p.registers["Z80_SP"] = {
            address = 0x14,
            size = 2,
            value = 0
        }
        p.registers["Z80_PC"] = {
            address = 0x16,
            size = 2,
            value = 0
        }
        p.registers["Z80_I"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["Z80_R"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["Z80_IM"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["Z80_BUSREQ"] = {
            address = 0x1E,
            size = 1,
            value = 0
        }
        p.registers["Z80_RESET"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        self.peripherals["YM2610"] = {
            base = 0x300000,
            type = "audio",
            description = "Yamaha YM2610 FM + ADPCM Audio Generator",
            registers = {}
        }
        
        local p = self.peripherals["YM2610"]
        p.registers["YM_ADDR_A0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["YM_DATA_A0"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["YM_ADDR_A1"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["YM_DATA_A1"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["YM_ADDR_B0"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["YM_DATA_B0"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["YM_TEST"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_FREQ_L"] = {
            address = 0xA0,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_FREQ_H"] = {
            address = 0xA4,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH1_FREQ_L"] = {
            address = 0xA1,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH1_FREQ_H"] = {
            address = 0xA5,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH2_FREQ_L"] = {
            address = 0xA2,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH2_FREQ_H"] = {
            address = 0xA6,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH3_FREQ_L"] = {
            address = 0xA3,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH3_FREQ_H"] = {
            address = 0xA7,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_KEY_ON"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_ALG"] = {
            address = 0xB0,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH1_ALG"] = {
            address = 0xB1,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH2_ALG"] = {
            address = 0xB2,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH3_ALG"] = {
            address = 0xB3,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_TIMER_H"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_TIMER_L"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_TIMER_CTRL"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_DETune"] = {
            address = 0x30,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_MUL"] = {
            address = 0x30,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_TL"] = {
            address = 0x40,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_KS_AR"] = {
            address = 0x50,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_AM_DR"] = {
            address = 0x60,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_SR"] = {
            address = 0x70,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_RR_SL"] = {
            address = 0x80,
            size = 1,
            value = 0
        }
        p.registers["YM_FM_CH0_SSG"] = {
            address = 0x90,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHA_FREQ_L"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHA_FREQ_H"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHB_FREQ_L"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHB_FREQ_H"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHC_FREQ_L"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHC_FREQ_H"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHA_VOL"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHB_VOL"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_CHC_VOL"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_MIXER"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_ENV_FREQ_L"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_ENV_FREQ_H"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_ENV_SHAPE"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_IO_A"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["YM_SSG_IO_B"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_STATUS"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_START"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_END"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_VOL_L"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_VOL_R"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["YM_DELTA_N_L"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["YM_DELTA_N_H"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_B_START"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_B_END"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_B_VOL"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["YM_ADPCM_B_CTRL"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        self.peripherals["YGV628"] = {
            base = 0x3C0000,
            type = "video",
            description = "Neo Geo VDP (Video Display Processor)",
            registers = {}
        }
        
        local p = self.peripherals["YGV628"]
        p.registers["VRAM_ADDR_L"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["VRAM_ADDR_H"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["VRAM_DATA"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["VRAM_READ"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["CRAM_ADDR"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["CRAM_DATA"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["VDP_STATUS"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["VDP_CTRL"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["SCROLL1_BASE"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["SCROLL2_BASE"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["SPR_BASE"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["SPR_COUNT"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["WINDOW_X"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        p.registers["WINDOW_Y"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["WINDOW_W"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["WINDOW_H"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["LINE_SCROLL_L"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["LINE_SCROLL_H"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["RASTER_COMP"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        p.registers["H_TIMING"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["V_TIMING"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["DMA_SRC_L"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["DMA_SRC_H"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        p.registers["DMA_SRC_B"] = {
            address = 0x1C,
            size = 1,
            value = 0
        }
        p.registers["DMA_DEST_L"] = {
            address = 0x1D,
            size = 1,
            value = 0
        }
        p.registers["DMA_DEST_H"] = {
            address = 0x1E,
            size = 1,
            value = 0
        }
        p.registers["DMA_COUNT"] = {
            address = 0x1F,
            size = 2,
            value = 0
        }
        self.peripherals["NEODRIVER"] = {
            base = 0x310000,
            type = "system",
            description = "Neo Geo System Driver / Controller",
            registers = {}
        }
        
        local p = self.peripherals["NEODRIVER"]
        p.registers["PDI0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["PDI1"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["PDI2"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["PDI3"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["PDO0"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["PDO1"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["PDO2"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["PDO3"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["DIPSEL1"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["DIPSEL2"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["DIPSEL3"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["DIPSEL4"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["SYSCTRL"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["IRQMASK"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["IRQFLAG"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["SECAM_MODE"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER1"] = {
            base = 0x310000,
            type = "input",
            description = "Controller Port 1",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER1"]
        p.registers["PDI0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER2"] = {
            base = 0x310001,
            type = "input",
            description = "Controller Port 2",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER2"]
        p.registers["PDI1"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        self.peripherals["MEMORY_CARD"] = {
            base = 0x320000,
            type = "storage",
            description = "Memory Card Interface",
            registers = {}
        }
        
        local p = self.peripherals["MEMORY_CARD"]
        p.registers["CARD_DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CARD_STATUS"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CARD_CTRL"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        self.peripherals["CART_BANK"] = {
            base = 0x2FFFF0,
            type = "memory",
            description = "Cartridge Bank Switching",
            registers = {}
        }
        
        local p = self.peripherals["CART_BANK"]
        p.registers["BANK_REG"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = NeoGeo_68000.DEVICE_NAME,
            manufacturer = NeoGeo_68000.MANUFACTURER,
            family = NeoGeo_68000.FAMILY,
            version = NeoGeo_68000.VERSION,
            architecture = NeoGeo_68000.ARCHITECTURE,
            bits = NeoGeo_68000.BITS,
            clock_frequency = NeoGeo_68000.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("NeoGeo_68000(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function NeoGeo_68000.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function NeoGeo_68000.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function NeoGeo_68000.print_device_info(device)
    device = device or NeoGeo_68000.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function NeoGeo_68000.print_registers(device)
    device = device or NeoGeo_68000.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            NeoGeo_68000.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function NeoGeo_68000.example()
    print("=== NeoGeo-68000设备示例 ===")
    
    -- 创建设备实例
    local device = NeoGeo_68000.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    NeoGeo_68000.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["D0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("D0", 0x55)
        print("写入 D0: " .. NeoGeo_68000.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("D0")
        print("读取 D0: " .. NeoGeo_68000.hex(value))
        
        -- 位操作
        device:set_bit("D0", 0, true)
        local bit0 = device:get_bit("D0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    NeoGeo_68000.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("NeoGeo_68000.lua$") then
    NeoGeo_68000.example()
end

return NeoGeo_68000
