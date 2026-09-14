--[[
  MIPS-R3000A设备定义 - Lua模块
  生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Sony PlayStation (PS1) main processor - MIPS R3000A @ 33.87MHz with R4000-like ISA
  CPU架构: MIPS-R3000A
  位宽: 32位
  时钟频率: 33870000 Hz
]]

local MIPS_R3000A = {}

-- 设备信息
MIPS_R3000A.DEVICE_NAME = "MIPS-R3000A"
MIPS_R3000A.MANUFACTURER = "Sony / MIPS Technologies"
MIPS_R3000A.FAMILY = "MIPS-I"
MIPS_R3000A.VERSION = "1.0"
MIPS_R3000A.ARCHITECTURE = "MIPS-R3000A"
MIPS_R3000A.BITS = 32
MIPS_R3000A.CLOCK_FREQUENCY = 33870000

-- 寄存器地址定义
MIPS_R3000A.R0_ADDR = 0x00  -- Hard-wired Zero
MIPS_R3000A.R1_ADDR = 0x04  -- Assembler Temporary
MIPS_R3000A.R2_ADDR = 0x08  -- Value Returned by Subroutines
MIPS_R3000A.R3_ADDR = 0x0C  -- Expression Evaluation
MIPS_R3000A.R4_ADDR = 0x10  -- Expression Evaluation
MIPS_R3000A.R5_ADDR = 0x14  -- Expression Evaluation
MIPS_R3000A.R6_ADDR = 0x18  -- Expression Evaluation
MIPS_R3000A.R7_ADDR = 0x1C  -- Expression Evaluation
MIPS_R3000A.R8_ADDR = 0x20  -- Expression Evaluation
MIPS_R3000A.R9_ADDR = 0x24  -- Expression Evaluation
MIPS_R3000A.R10_ADDR = 0x28  -- Expression Evaluation
MIPS_R3000A.R11_ADDR = 0x2C  -- Expression Evaluation
MIPS_R3000A.R12_ADDR = 0x30  -- Expression Evaluation
MIPS_R3000A.R13_ADDR = 0x34  -- Expression Evaluation
MIPS_R3000A.R14_ADDR = 0x38  -- Expression Evaluation
MIPS_R3000A.R15_ADDR = 0x3C  -- Expression Evaluation
MIPS_R3000A.R16_ADDR = 0x40  -- Saved Value
MIPS_R3000A.R17_ADDR = 0x44  -- Saved Value
MIPS_R3000A.R18_ADDR = 0x48  -- Saved Value
MIPS_R3000A.R19_ADDR = 0x4C  -- Saved Value
MIPS_R3000A.R20_ADDR = 0x50  -- Saved Value
MIPS_R3000A.R21_ADDR = 0x54  -- Saved Value
MIPS_R3000A.R22_ADDR = 0x58  -- Saved Value
MIPS_R3000A.R23_ADDR = 0x5C  -- Saved Value
MIPS_R3000A.R24_ADDR = 0x60  -- Temporary
MIPS_R3000A.R25_ADDR = 0x64  -- Temporary
MIPS_R3000A.R26_ADDR = 0x68  -- Kernel Reserved
MIPS_R3000A.R27_ADDR = 0x6C  -- Kernel Reserved
MIPS_R3000A.R28_ADDR = 0x70  -- Global Pointer
MIPS_R3000A.R29_ADDR = 0x74  -- Stack Pointer
MIPS_R3000A.R30_ADDR = 0x78  -- Frame Pointer
MIPS_R3000A.R31_ADDR = 0x7C  -- Return Address
MIPS_R3000A.HI_ADDR = 0x80  -- Multiply/Divide High
MIPS_R3000A.LO_ADDR = 0x84  -- Multiply/Divide Low
MIPS_R3000A.PC_ADDR = 0x88  -- Program Counter
MIPS_R3000A.CP0_SR_ADDR = 0x90  -- Coprocessor 0 - Status Register
MIPS_R3000A.CP0_SR_IE_BIT = 0  -- Interrupt Enable
MIPS_R3000A.CP0_SR_EXL_BIT = 1  -- Exception Level
MIPS_R3000A.CP0_SR_ERL_BIT = 2  -- Error Level
MIPS_R3000A.CP0_SR_KSU_BIT = 0  -- Kernel/User Mode
MIPS_R3000A.CP0_SR_IM0_7_BIT = 0  -- Interrupt Mask bits
MIPS_R3000A.CP0_SR_CU0_BIT = 28  -- Coprocessor 0 Usable
MIPS_R3000A.CP0_SR_BEV_BIT = 22  -- Bootstrap Exception Vector
MIPS_R3000A.CP0_CAUSE_ADDR = 0x94  -- Coprocessor 0 - Cause Register
MIPS_R3000A.CP0_CAUSE_EXCCODE_BIT = 0  -- Exception Code
MIPS_R3000A.CP0_CAUSE_IP0_7_BIT = 0  -- Interrupt Pending bits
MIPS_R3000A.CP0_EPC_ADDR = 0x98  -- Coprocessor 0 - Exception PC
MIPS_R3000A.CP0_BADVADDR_ADDR = 0x9C  -- Coprocessor 0 - Bad Virtual Address
MIPS_R3000A.CP0_CONTEXT_ADDR = 0xA0  -- Coprocessor 0 - Context Register
MIPS_R3000A.CP0_PID_ADDR = 0xA4  -- Coprocessor 0 - Process ID

-- 内存段定义
MIPS_R3000A.KSEG0_START = 0x80000000
MIPS_R3000A.KSEG0_END = 0x801FFFFF
MIPS_R3000A.KSEG0_SIZE = 2097152  -- KSEG0 - Cached RAM (2MB System RAM)
MIPS_R3000A.KSEG1_START = 0xA0000000
MIPS_R3000A.KSEG1_END = 0xA01FFFFF
MIPS_R3000A.KSEG1_SIZE = 2097152  -- KSEG1 - Uncached RAM (2MB System RAM)
MIPS_R3000A.VRAM_START = 0xA0000000
MIPS_R3000A.VRAM_END = 0xA01FFFFF
MIPS_R3000A.VRAM_SIZE = 1048576  -- VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
MIPS_R3000A.EXPANSION_START = 0xA0000000
MIPS_R3000A.EXPANSION_END = 0xA00FFFFF
MIPS_R3000A.EXPANSION_SIZE = 1048576  -- Expansion Region (maps to expansion RAM area)
MIPS_R3000A.SCRATCHPAD_START = 0x1F800000
MIPS_R3000A.SCRATCHPAD_END = 0x1F8003FF
MIPS_R3000A.SCRATCHPAD_SIZE = 1024  -- Data Scratchpad (1KB)
MIPS_R3000A.EXP_ROM_START = 0x1FC00000
MIPS_R3000A.EXP_ROM_END = 0x1FC7FFFF
MIPS_R3000A.EXP_ROM_SIZE = 524288  -- Kernel BIOS ROM (512KB)
MIPS_R3000A.USER_ROM_START = 0x1F800000
MIPS_R3000A.USER_ROM_END = 0x1FBFFFFF
MIPS_R3000A.USER_ROM_SIZE = 4194304  -- User ROM / Kernel Expansion
MIPS_R3000A.MMIO_START = 0x1F801000
MIPS_R3000A.MMIO_END = 0x1F802FFF
MIPS_R3000A.MMIO_SIZE = 8192  -- I/O Register Area (Expansion 1)
MIPS_R3000A.GPU_START = 0x1F801810
MIPS_R3000A.GPU_END = 0x1F801817
MIPS_R3000A.GPU_SIZE = 8  -- GPU Registers
MIPS_R3000A.CDROM_START = 0x1F801800
MIPS_R3000A.CDROM_END = 0x1F80180F
MIPS_R3000A.CDROM_SIZE = 16  -- CD-ROM Registers
MIPS_R3000A.SPU_START = 0x1F801C00
MIPS_R3000A.SPU_END = 0x1F801DFF
MIPS_R3000A.SPU_SIZE = 512  -- SPU Registers
MIPS_R3000A.IRQ_START = 0x1F801070
MIPS_R3000A.IRQ_END = 0x1F801077
MIPS_R3000A.IRQ_SIZE = 8  -- Interrupt Control
MIPS_R3000A.DMA_START = 0x1F801080
MIPS_R3000A.DMA_END = 0x1F8010FF
MIPS_R3000A.DMA_SIZE = 128  -- DMA Registers (7 channels)
MIPS_R3000A.TIMER_START = 0x1F801100
MIPS_R3000A.TIMER_END = 0x1F80112F
MIPS_R3000A.TIMER_SIZE = 48  -- Timer Registers
MIPS_R3000A.JOY_START = 0x1F801040
MIPS_R3000A.JOY_END = 0x1F80104F
MIPS_R3000A.JOY_SIZE = 16  -- JOY Interface Registers
MIPS_R3000A.MDEC_START = 0x1F801820
MIPS_R3000A.MDEC_END = 0x1F801827
MIPS_R3000A.MDEC_SIZE = 8  -- MDEC (Motion Decoder) Registers
MIPS_R3000A.SIO_START = 0x1F801050
MIPS_R3000A.SIO_END = 0x1F80105F
MIPS_R3000A.SIO_SIZE = 16  -- SIO Registers
MIPS_R3000A.GPU_STAT_START = 0x1F801814
MIPS_R3000A.GPU_STAT_END = 0x1F801817
MIPS_R3000A.GPU_STAT_SIZE = 4  -- GPU Status
MIPS_R3000A.CDROM_STAT_START = 0x1F801801
MIPS_R3000A.CDROM_STAT_END = 0x1F801803
MIPS_R3000A.CDROM_STAT_SIZE = 3  -- CD-ROM Status
MIPS_R3000A.SPU_RAM_START = 0x1F800000
MIPS_R3000A.SPU_RAM_END = 0x1F800FFF
MIPS_R3000A.SPU_RAM_SIZE = 1024  -- SPU Work RAM (1KB)

-- 外设定义
-- Graphics Processing Unit
MIPS_R3000A.GPU_BASE = 0x1F801810
MIPS_R3000A.GPU_GP0_CMD_ADDR = 0x00
MIPS_R3000A.GPU_GP0_DATA_ADDR = 0x04
MIPS_R3000A.GPU_GP1_CMD_ADDR = 0x08
MIPS_R3000A.GPU_GP1_DATA_ADDR = 0x0C
MIPS_R3000A.GPU_TPAGE_ADDR = 0x00
MIPS_R3000A.GPU_DRAW_MODE_ADDR = 0x01
MIPS_R3000A.GPU_TEXTURE_WIN_ADDR = 0x02
MIPS_R3000A.GPU_DRAW_OFFSET_X_ADDR = 0x03
MIPS_R3000A.GPU_DRAW_OFFSET_Y_ADDR = 0x04
MIPS_R3000A.GPU_DRAW_AREA_X_ADDR = 0x05
MIPS_R3000A.GPU_DRAW_AREA_Y_ADDR = 0x06
MIPS_R3000A.GPU_DITHER_ADDR = 0x07
MIPS_R3000A.GPU_DISPLAY_MODE_ADDR = 0x08
MIPS_R3000A.GPU_DISPLAY_START_X_ADDR = 0x09
MIPS_R3000A.GPU_DISPLAY_START_Y_ADDR = 0x0A
MIPS_R3000A.GPU_DISPLAY_HORZ_ADDR = 0x0B
MIPS_R3000A.GPU_DISPLAY_VERT_ADDR = 0x0C
MIPS_R3000A.GPU_DMA_MODE_ADDR = 0x0D
MIPS_R3000A.GPU_GPU_STAT_ADDR = 0x0C
MIPS_R3000A.GPU_GPU_STAT_READY_CMD_BIT = 0  -- GPU Ready to Receive Command
MIPS_R3000A.GPU_GPU_STAT_READY_DMA_BIT = 1  -- GPU Ready for DMA
MIPS_R3000A.GPU_GPU_STAT_DRAWING_BIT = 2  -- Drawing Busy
MIPS_R3000A.GPU_GPU_STAT_DMA_REQ_BIT = 3  -- DMA Request
MIPS_R3000A.GPU_GPU_STAT_COMMAND_BUSY_BIT = 4  -- Command Busy
MIPS_R3000A.GPU_GPU_STAT_DISPLAY_DISABLE_BIT = 5  -- Display Disable
MIPS_R3000A.GPU_GPU_STAT_INTERRUPT_BIT = 24  -- V-Blank Interrupt Flag
-- Geometry Transformation Engine
MIPS_R3000A.GTE_BASE = 0x1F801880
MIPS_R3000A.GTE_GTE_VXY0_ADDR = 0x00
MIPS_R3000A.GTE_GTE_VZ0_ADDR = 0x04
MIPS_R3000A.GTE_GTE_VXY1_ADDR = 0x08
MIPS_R3000A.GTE_GTE_VZ1_ADDR = 0x0C
MIPS_R3000A.GTE_GTE_VXY2_ADDR = 0x10
MIPS_R3000A.GTE_GTE_VZ2_ADDR = 0x14
MIPS_R3000A.GTE_GTE_RGB0_ADDR = 0x18
MIPS_R3000A.GTE_GTE_RGB1_ADDR = 0x1C
MIPS_R3000A.GTE_GTE_RGB2_ADDR = 0x20
MIPS_R3000A.GTE_GTE_RTP_ADDR = 0x30
MIPS_R3000A.GTE_GTE_TRX_ADDR = 0x34
MIPS_R3000A.GTE_GTE_TRY_ADDR = 0x38
MIPS_R3000A.GTE_GTE_TRZ_ADDR = 0x3C
MIPS_R3000A.GTE_GTE_MAC0_ADDR = 0x40
MIPS_R3000A.GTE_GTE_MAC1_ADDR = 0x44
MIPS_R3000A.GTE_GTE_MAC2_ADDR = 0x48
MIPS_R3000A.GTE_GTE_MAC3_ADDR = 0x4C
MIPS_R3000A.GTE_GTE_IR0_ADDR = 0x50
MIPS_R3000A.GTE_GTE_IR1_ADDR = 0x54
MIPS_R3000A.GTE_GTE_IR2_ADDR = 0x58
MIPS_R3000A.GTE_GTE_IR3_ADDR = 0x5C
MIPS_R3000A.GTE_GTE_LZCS_ADDR = 0x60
MIPS_R3000A.GTE_GTE_LZCR_ADDR = 0x64
MIPS_R3000A.GTE_GTE_CTX_ADDR = 0x68
MIPS_R3000A.GTE_GTE_CTY_ADDR = 0x6C
MIPS_R3000A.GTE_GTE_CTZ_ADDR = 0x70
MIPS_R3000A.GTE_GTE_RTX_ADDR = 0x74
MIPS_R3000A.GTE_GTE_RTY_ADDR = 0x78
MIPS_R3000A.GTE_GTE_RTZ_ADDR = 0x7C
MIPS_R3000A.GTE_GTE_SR_ADDR = 0x80
MIPS_R3000A.GTE_GTE_CMD_ADDR = 0x84
MIPS_R3000A.GTE_GTE_H_ADDR = 0x88
MIPS_R3000A.GTE_GTE_DQB_ADDR = 0x8C
MIPS_R3000A.GTE_GTE_DQA_ADDR = 0x90
MIPS_R3000A.GTE_GTE_ZSF3_ADDR = 0x94
MIPS_R3000A.GTE_GTE_ZSF4_ADDR = 0x98
MIPS_R3000A.GTE_GTE_OTZ_ADDR = 0x9C
-- Sound Processing Unit (24-channel ADPCM)
MIPS_R3000A.SPU_BASE = 0x1F801C00
MIPS_R3000A.SPU_SPU_CTRL_ADDR = 0x00
MIPS_R3000A.SPU_SPU_CTRL_REVERB_MASTER_BIT = 0  -- Reverb Master Enable
MIPS_R3000A.SPU_SPU_CTRL_IRQ9_BIT = 9  -- Interrupt Request Enable
MIPS_R3000A.SPU_SPU_STAT_ADDR = 0x04
MIPS_R3000A.SPU_SPU_CDVOL_L_ADDR = 0x08
MIPS_R3000A.SPU_SPU_CDVOL_R_ADDR = 0x0A
MIPS_R3000A.SPU_SPU_MAINVOL_L_ADDR = 0x0C
MIPS_R3000A.SPU_SPU_MAINVOL_R_ADDR = 0x0E
MIPS_R3000A.SPU_SPU_REVERB_L_ADDR = 0x10
MIPS_R3000A.SPU_SPU_REVERB_R_ADDR = 0x12
MIPS_R3000A.SPU_SPU_KEYON_ADDR = 0x80
MIPS_R3000A.SPU_SPU_KEYOFF_ADDR = 0x82
MIPS_R3000A.SPU_SPU_CHANNEL_MUTE_ADDR = 0x84
MIPS_R3000A.SPU_SPU_NOISE_CLK_ADDR = 0x88
MIPS_R3000A.SPU_SPU_REVERB_ADDR_ADDR = 0x8A
MIPS_R3000A.SPU_SPU_IRQ_ADDR_ADDR = 0x8C
MIPS_R3000A.SPU_SPU_REVERB_VOL_L_ADDR = 0x8E
MIPS_R3000A.SPU_SPU_REVERB_VOL_R_ADDR = 0x90
MIPS_R3000A.SPU_SPU_VOICE_VOL_L_ADDR = 0x00
MIPS_R3000A.SPU_SPU_VOICE_VOL_R_ADDR = 0x01
MIPS_R3000A.SPU_SPU_VOICE_FREQ_ADDR = 0x02
MIPS_R3000A.SPU_SPU_VOICE_START_ADDR = 0x04
MIPS_R3000A.SPU_SPU_VOICE_ADSR1_ADDR = 0x06
MIPS_R3000A.SPU_SPU_VOICE_ADSR2_ADDR = 0x08
MIPS_R3000A.SPU_SPU_VOICE_ENV_ADDR = 0x0A
MIPS_R3000A.SPU_SPU_VOICE_REPEAT_ADDR = 0x0C
MIPS_R3000A.SPU_VOICE_BASE_SIZE_ADDR = 0x10
-- Motion Decoder (JPEG Decompression)
MIPS_R3000A.MDEC_BASE = 0x1F801820
MIPS_R3000A.MDEC_MDEC_CTRL_ADDR = 0x00
MIPS_R3000A.MDEC_MDEC_CTRL_DATA_IN_SIZE_BIT = 0  -- Data-in size in words
MIPS_R3000A.MDEC_MDEC_CTRL_RESET_BIT = 16  -- Reset MDEC
MIPS_R3000A.MDEC_MDEC_CTRL_BUSY_BIT = 17  -- MDEC Busy
MIPS_R3000A.MDEC_MDEC_DATA_ADDR = 0x04
MIPS_R3000A.MDEC_MDEC_BKGD_ADDR = 0x08
-- DMA Controller (7 channels)
MIPS_R3000A.DMA_BASE = 0x1F801080
MIPS_R3000A.DMA_DMA_DPCR_ADDR = 0x00
MIPS_R3000A.DMA_DMA_DPCR_CH0_EN_BIT = 0  -- Channel 0 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH1_EN_BIT = 4  -- Channel 1 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH2_EN_BIT = 8  -- Channel 2 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH3_EN_BIT = 12  -- Channel 3 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH4_EN_BIT = 16  -- Channel 4 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH5_EN_BIT = 20  -- Channel 5 Enable
MIPS_R3000A.DMA_DMA_DPCR_CH6_EN_BIT = 24  -- Channel 6 Enable
MIPS_R3000A.DMA_DMA_INT_ADDR = 0x04
MIPS_R3000A.DMA_DMA_CH0_BASE_ADDR = 0x10
MIPS_R3000A.DMA_DMA_CH0_COUNT_ADDR = 0x14
MIPS_R3000A.DMA_DMA_CH0_CTRL_ADDR = 0x18
MIPS_R3000A.DMA_DMA_CH0_CTRL_DEST_DIR_BIT = 0  -- Destination Direction
MIPS_R3000A.DMA_DMA_CH0_CTRL_SRC_DIR_BIT = 0  -- Source Direction
MIPS_R3000A.DMA_DMA_CH0_CTRL_STEPS_BIT = 0  -- Step
MIPS_R3000A.DMA_DMA_CH0_CTRL_CHAIN_BIT = 0  -- Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
MIPS_R3000A.DMA_DMA_CH0_CTRL_SYNC_BIT = 0  -- Sync Mode (0=immediate, 1=request, 2=linked-list)
MIPS_R3000A.DMA_DMA_CH0_CTRL_TRIGGER_BIT = 10  -- Trigger
MIPS_R3000A.DMA_DMA_CH1_BASE_ADDR = 0x20
MIPS_R3000A.DMA_DMA_CH1_COUNT_ADDR = 0x24
MIPS_R3000A.DMA_DMA_CH1_CTRL_ADDR = 0x28
MIPS_R3000A.DMA_DMA_CH2_BASE_ADDR = 0x30
MIPS_R3000A.DMA_DMA_CH2_COUNT_ADDR = 0x34
MIPS_R3000A.DMA_DMA_CH2_CTRL_ADDR = 0x38
MIPS_R3000A.DMA_DMA_CH3_BASE_ADDR = 0x40
MIPS_R3000A.DMA_DMA_CH3_COUNT_ADDR = 0x44
MIPS_R3000A.DMA_DMA_CH3_CTRL_ADDR = 0x48
MIPS_R3000A.DMA_DMA_CH4_BASE_ADDR = 0x50
MIPS_R3000A.DMA_DMA_CH4_COUNT_ADDR = 0x54
MIPS_R3000A.DMA_DMA_CH4_CTRL_ADDR = 0x58
MIPS_R3000A.DMA_DMA_CH5_BASE_ADDR = 0x60
MIPS_R3000A.DMA_DMA_CH5_COUNT_ADDR = 0x64
MIPS_R3000A.DMA_DMA_CH5_CTRL_ADDR = 0x68
MIPS_R3000A.DMA_DMA_CH6_BASE_ADDR = 0x70
MIPS_R3000A.DMA_DMA_CH6_COUNT_ADDR = 0x74
MIPS_R3000A.DMA_DMA_CH6_CTRL_ADDR = 0x78
-- Timers (3 timers)
MIPS_R3000A.TIMER_BASE = 0x1F801100
MIPS_R3000A.TIMER_TM0_COUNT_ADDR = 0x00
MIPS_R3000A.TIMER_TM0_MODE_ADDR = 0x04
MIPS_R3000A.TIMER_TM0_MODE_RELOAD_BIT = 0  -- Reload Enable
MIPS_R3000A.TIMER_TM0_MODE_CLOCK_BIT = 0  -- Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
MIPS_R3000A.TIMER_TM0_MODE_IRQ_EN_BIT = 3  -- IRQ Enable
MIPS_R3000A.TIMER_TM0_MODE_IRQ_REPEAT_BIT = 4  -- IRQ Repeat
MIPS_R3000A.TIMER_TM0_MODE_IRQ_TOGGLE_BIT = 5  -- IRQ Toggle Mode
MIPS_R3000A.TIMER_TM0_MODE_REACH_MAX_BIT = 6  -- Reached Max Value
MIPS_R3000A.TIMER_TM0_TARGET_ADDR = 0x08
MIPS_R3000A.TIMER_TM1_COUNT_ADDR = 0x10
MIPS_R3000A.TIMER_TM1_MODE_ADDR = 0x14
MIPS_R3000A.TIMER_TM1_TARGET_ADDR = 0x18
MIPS_R3000A.TIMER_TM2_COUNT_ADDR = 0x20
MIPS_R3000A.TIMER_TM2_MODE_ADDR = 0x24
MIPS_R3000A.TIMER_TM2_TARGET_ADDR = 0x28
-- CD-ROM Controller
MIPS_R3000A.CDROM_BASE = 0x1F801800
MIPS_R3000A.CDROM_CD0_DATA_ADDR = 0x00
MIPS_R3000A.CDROM_CD0_STATUS_ADDR = 0x01
MIPS_R3000A.CDROM_CD0_RESPONSE_ADDR = 0x02
MIPS_R3000A.CDROM_CD0_DATA1_ADDR = 0x03
MIPS_R3000A.CDROM_CD0_INT_FLAG_ADDR = 0x04
MIPS_R3000A.CDROM_CD0_INT_FLAG_INT1_BIT = 0  -- Data Ready
MIPS_R3000A.CDROM_CD0_INT_FLAG_INT2_BIT = 1  -- Command Complete
MIPS_R3000A.CDROM_CD0_INT_FLAG_INT3_BIT = 2  -- Acknowledge Received
MIPS_R3000A.CDROM_CD0_INT_FLAG_INT4_BIT = 3  -- Error / N-Complete
MIPS_R3000A.CDROM_CD0_VOLUME_L_ADDR = 0x08
MIPS_R3000A.CDROM_CD0_VOLUME_R_ADDR = 0x09
-- JOY Interface
MIPS_R3000A.JOY_BASE = 0x1F801040
MIPS_R3000A.JOY_JOY_CTRL_ADDR = 0x00
MIPS_R3000A.JOY_JOY_CTRL_TX_EN_BIT = 0  -- Transmit Enable
MIPS_R3000A.JOY_JOY_CTRL_RX_EN_BIT = 1  -- Receive Enable
MIPS_R3000A.JOY_JOY_CTRL_CLOCK_BIT = 3  -- Internal/External Clock
MIPS_R3000A.JOY_JOY_CTRL_IRQ_EN_BIT = 4  -- IRQ Enable
MIPS_R3000A.JOY_JOY_MODE_ADDR = 0x01
MIPS_R3000A.JOY_JOY_BAUD_ADDR = 0x02
MIPS_R3000A.JOY_JOY_TX_DATA_ADDR = 0x04
MIPS_R3000A.JOY_JOY_RX_DATA_ADDR = 0x05
MIPS_R3000A.JOY_JOY_STAT_ADDR = 0x06
MIPS_R3000A.JOY_JOY_STAT_TX_EMPTY_BIT = 0  -- Transmit Buffer Empty
MIPS_R3000A.JOY_JOY_STAT_RX_READY_BIT = 2  -- Receive Data Ready
MIPS_R3000A.JOY_JOY_STAT_TX_IRQ_BIT = 3  -- Transmit IRQ Pending
MIPS_R3000A.JOY_JOY_STAT_RX_IRQ_BIT = 4  -- Receive IRQ Pending
-- SIO (Serial I/O - Memory Card)
MIPS_R3000A.SIO_BASE = 0x1F801050
MIPS_R3000A.SIO_SIO_DATA_ADDR = 0x00
MIPS_R3000A.SIO_SIO_STATUS_ADDR = 0x01
MIPS_R3000A.SIO_SIO_MODE_ADDR = 0x02
MIPS_R3000A.SIO_SIO_CTRL_ADDR = 0x03
MIPS_R3000A.SIO_SIO_BAUD_ADDR = 0x04
-- Interrupt Controller
MIPS_R3000A.INTERRUPT_BASE = 0x1F801070
MIPS_R3000A.INTERRUPT_INT_STAT_ADDR = 0x00
MIPS_R3000A.INTERRUPT_INT_MASK_ADDR = 0x04
MIPS_R3000A.INTERRUPT_INT_MASK_VBLANK_BIT = 0  -- V-Blank Interrupt
MIPS_R3000A.INTERRUPT_INT_MASK_GPU_BIT = 1  -- GPU Interrupt
MIPS_R3000A.INTERRUPT_INT_MASK_CDROM_BIT = 2  -- CD-ROM Interrupt
MIPS_R3000A.INTERRUPT_INT_MASK_DMA0_BIT = 3  -- DMA Channel 0
MIPS_R3000A.INTERRUPT_INT_MASK_DMA1_BIT = 4  -- DMA Channel 1
MIPS_R3000A.INTERRUPT_INT_MASK_DMA2_BIT = 5  -- DMA Channel 2
MIPS_R3000A.INTERRUPT_INT_MASK_DMA3_BIT = 6  -- DMA Channel 3
MIPS_R3000A.INTERRUPT_INT_MASK_DMA4_BIT = 7  -- DMA Channel 4
MIPS_R3000A.INTERRUPT_INT_MASK_DMA5_BIT = 8  -- DMA Channel 5
MIPS_R3000A.INTERRUPT_INT_MASK_DMA6_BIT = 9  -- DMA Channel 6
MIPS_R3000A.INTERRUPT_INT_MASK_TIMER0_BIT = 10  -- Timer 0
MIPS_R3000A.INTERRUPT_INT_MASK_TIMER1_BIT = 11  -- Timer 1
MIPS_R3000A.INTERRUPT_INT_MASK_TIMER2_BIT = 12  -- Timer 2
MIPS_R3000A.INTERRUPT_INT_MASK_SIO_BIT = 13  -- SIO / Memory Card
MIPS_R3000A.INTERRUPT_INT_MASK_SPU_BIT = 14  -- SPU Interrupt
MIPS_R3000A.INTERRUPT_INT_MASK_PIO_BIT = 15  -- PIO (Expansion)

-- 中断向量定义
MIPS_R3000A.INT_VBLANK = 0  -- V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
MIPS_R3000A.INT_GPU = 1  -- GPU Interrupt (drawing complete / V-Blank)
MIPS_R3000A.INT_CDROM = 2  -- CD-ROM Interrupt
MIPS_R3000A.INT_DMA0 = 3  -- DMA Channel 0 Complete
MIPS_R3000A.INT_DMA1 = 4  -- DMA Channel 1 Complete
MIPS_R3000A.INT_DMA2 = 5  -- DMA Channel 2 Complete
MIPS_R3000A.INT_DMA3 = 6  -- DMA Channel 3 Complete
MIPS_R3000A.INT_DMA4 = 7  -- DMA Channel 4 Complete
MIPS_R3000A.INT_DMA5 = 8  -- DMA Channel 5 Complete
MIPS_R3000A.INT_DMA6 = 9  -- DMA Channel 6 Complete
MIPS_R3000A.INT_TIMER0 = 10  -- Timer 0 Interrupt
MIPS_R3000A.INT_TIMER1 = 11  -- Timer 1 Interrupt
MIPS_R3000A.INT_TIMER2 = 12  -- Timer 2 Interrupt
MIPS_R3000A.INT_SIO = 13  -- SIO / Memory Card Interrupt
MIPS_R3000A.INT_SPU = 14  -- SPU Interrupt
MIPS_R3000A.INT_PIO = 15  -- PIO / Expansion Interrupt

-- 引脚定义
MIPS_R3000A.PIN_VCC = 1  -- Power Supply (3.3V regulated)
MIPS_R3000A.PIN_VSS = 2  -- Ground
MIPS_R3000A.PIN_CLK = 3  -- System Clock Input (53.6932MHz / 2 = 26.8466MHz bus)
MIPS_R3000A.PIN_RESET = 4  -- Reset (active low)
MIPS_R3000A.PIN_NMI = 5  -- Non-Maskable Interrupt
MIPS_R3000A.PIN_IRQ = 6  -- Interrupt Request
MIPS_R3000A.PIN_AB0 = 7  -- Address Bus Bit 0
MIPS_R3000A.PIN_AB1 = 8  -- Address Bus Bit 1
MIPS_R3000A.PIN_AB2 = 9  -- Address Bus Bit 2
MIPS_R3000A.PIN_AB3 = 10  -- Address Bus Bit 3
MIPS_R3000A.PIN_AB4 = 11  -- Address Bus Bit 4
MIPS_R3000A.PIN_AB5 = 12  -- Address Bus Bit 5
MIPS_R3000A.PIN_AB6 = 13  -- Address Bus Bit 6
MIPS_R3000A.PIN_AB7 = 14  -- Address Bus Bit 7
MIPS_R3000A.PIN_AB8 = 15  -- Address Bus Bit 8
MIPS_R3000A.PIN_AB9 = 16  -- Address Bus Bit 9
MIPS_R3000A.PIN_AB10 = 17  -- Address Bus Bit 10
MIPS_R3000A.PIN_AB11 = 18  -- Address Bus Bit 11
MIPS_R3000A.PIN_AB12 = 19  -- Address Bus Bit 12
MIPS_R3000A.PIN_AB13 = 20  -- Address Bus Bit 13
MIPS_R3000A.PIN_AB14 = 21  -- Address Bus Bit 14
MIPS_R3000A.PIN_AB15 = 22  -- Address Bus Bit 15
MIPS_R3000A.PIN_AB16 = 23  -- Address Bus Bit 16
MIPS_R3000A.PIN_AB17 = 24  -- Address Bus Bit 17
MIPS_R3000A.PIN_AB18 = 25  -- Address Bus Bit 18
MIPS_R3000A.PIN_AB19 = 26  -- Address Bus Bit 19
MIPS_R3000A.PIN_AB20 = 27  -- Address Bus Bit 20
MIPS_R3000A.PIN_AB21 = 28  -- Address Bus Bit 21
MIPS_R3000A.PIN_AB22 = 29  -- Address Bus Bit 22
MIPS_R3000A.PIN_AB23 = 30  -- Address Bus Bit 23
MIPS_R3000A.PIN_AB24 = 31  -- Address Bus Bit 24
MIPS_R3000A.PIN_AB25 = 32  -- Address Bus Bit 25
MIPS_R3000A.PIN_AB26 = 33  -- Address Bus Bit 26
MIPS_R3000A.PIN_AB27 = 34  -- Address Bus Bit 27
MIPS_R3000A.PIN_AB28 = 35  -- Address Bus Bit 28
MIPS_R3000A.PIN_AB29 = 36  -- Address Bus Bit 29
MIPS_R3000A.PIN_AB30 = 37  -- Address Bus Bit 30
MIPS_R3000A.PIN_AB31 = 38  -- Address Bus Bit 31
MIPS_R3000A.PIN_DB0 = 39  -- Data Bus Bit 0
MIPS_R3000A.PIN_DB1 = 40  -- Data Bus Bit 1
MIPS_R3000A.PIN_DB2 = 41  -- Data Bus Bit 2
MIPS_R3000A.PIN_DB3 = 42  -- Data Bus Bit 3
MIPS_R3000A.PIN_DB4 = 43  -- Data Bus Bit 4
MIPS_R3000A.PIN_DB5 = 44  -- Data Bus Bit 5
MIPS_R3000A.PIN_DB6 = 45  -- Data Bus Bit 6
MIPS_R3000A.PIN_DB7 = 46  -- Data Bus Bit 7
MIPS_R3000A.PIN_DB8 = 47  -- Data Bus Bit 8
MIPS_R3000A.PIN_DB9 = 48  -- Data Bus Bit 9
MIPS_R3000A.PIN_DB10 = 49  -- Data Bus Bit 10
MIPS_R3000A.PIN_DB11 = 50  -- Data Bus Bit 11
MIPS_R3000A.PIN_DB12 = 51  -- Data Bus Bit 12
MIPS_R3000A.PIN_DB13 = 52  -- Data Bus Bit 13
MIPS_R3000A.PIN_DB14 = 53  -- Data Bus Bit 14
MIPS_R3000A.PIN_DB15 = 54  -- Data Bus Bit 15
MIPS_R3000A.PIN_DB16 = 55  -- Data Bus Bit 16
MIPS_R3000A.PIN_DB17 = 56  -- Data Bus Bit 17
MIPS_R3000A.PIN_DB18 = 57  -- Data Bus Bit 18
MIPS_R3000A.PIN_DB19 = 58  -- Data Bus Bit 19
MIPS_R3000A.PIN_DB20 = 59  -- Data Bus Bit 20
MIPS_R3000A.PIN_DB21 = 60  -- Data Bus Bit 21
MIPS_R3000A.PIN_DB22 = 61  -- Data Bus Bit 22
MIPS_R3000A.PIN_DB23 = 62  -- Data Bus Bit 23
MIPS_R3000A.PIN_DB24 = 63  -- Data Bus Bit 24
MIPS_R3000A.PIN_DB25 = 64  -- Data Bus Bit 25
MIPS_R3000A.PIN_DB26 = 65  -- Data Bus Bit 26
MIPS_R3000A.PIN_DB27 = 66  -- Data Bus Bit 27
MIPS_R3000A.PIN_DB28 = 67  -- Data Bus Bit 28
MIPS_R3000A.PIN_DB29 = 68  -- Data Bus Bit 29
MIPS_R3000A.PIN_DB30 = 69  -- Data Bus Bit 30
MIPS_R3000A.PIN_DB31 = 70  -- Data Bus Bit 31
MIPS_R3000A.PIN_NCS0 = 71  -- Chip Select 0 (ROM)
MIPS_R3000A.PIN_NCS1 = 72  -- Chip Select 1 (RAM)
MIPS_R3000A.PIN_NCS2 = 73  -- Chip Select 2 (I/O)
MIPS_R3000A.PIN_NWR = 74  -- Write Enable
MIPS_R3000A.PIN_NRD = 75  -- Read Enable
MIPS_R3000A.PIN_BE0 = 76  -- Byte Enable 0 (bits 0-7)
MIPS_R3000A.PIN_BE1 = 77  -- Byte Enable 1 (bits 8-15)
MIPS_R3000A.PIN_BE2 = 78  -- Byte Enable 2 (bits 16-23)
MIPS_R3000A.PIN_BE3 = 79  -- Byte Enable 3 (bits 24-31)
MIPS_R3000A.PIN_BUSREQ = 80  -- Bus Request (from external DMA)
MIPS_R3000A.PIN_BUSACK = 81  -- Bus Acknowledge
MIPS_R3000A.PIN_INT0 = 82  -- Interrupt 0 (V-Blank)
MIPS_R3000A.PIN_INT1 = 83  -- Interrupt 1 (GPU)
MIPS_R3000A.PIN_INT2 = 84  -- Interrupt 2 (CD-ROM)
MIPS_R3000A.PIN_INT3 = 85  -- Interrupt 3 (DMA)
MIPS_R3000A.PIN_INT4 = 86  -- Interrupt 4 (Timer)
MIPS_R3000A.PIN_INT5 = 87  -- Interrupt 5 (SIO)
MIPS_R3000A.PIN_AUDIO_L = 88  -- Audio Output Left
MIPS_R3000A.PIN_AUDIO_R = 89  -- Audio Output Right
MIPS_R3000A.PIN_VIDEO_R = 90  -- Video Output Red (analog RGB)
MIPS_R3000A.PIN_VIDEO_G = 91  -- Video Output Green
MIPS_R3000A.PIN_VIDEO_B = 92  -- Video Output Blue
MIPS_R3000A.PIN_SYNC = 93  -- Video Sync / Composite

-- 设备类
function MIPS_R3000A.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 4,
            access = "r",
            description = "Hard-wired Zero",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Assembler Temporary",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Value Returned by Subroutines",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R13"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R14"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R15"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R16"] = {
            address = 0x40,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R17"] = {
            address = 0x44,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R18"] = {
            address = 0x48,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R19"] = {
            address = 0x4C,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R20"] = {
            address = 0x50,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R21"] = {
            address = 0x54,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R22"] = {
            address = 0x58,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R23"] = {
            address = 0x5C,
            size = 4,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R24"] = {
            address = 0x60,
            size = 4,
            access = "rw",
            description = "Temporary",
            value = 0
        }
        self.registers["R25"] = {
            address = 0x64,
            size = 4,
            access = "rw",
            description = "Temporary",
            value = 0
        }
        self.registers["R26"] = {
            address = 0x68,
            size = 4,
            access = "rw",
            description = "Kernel Reserved",
            value = 0
        }
        self.registers["R27"] = {
            address = 0x6C,
            size = 4,
            access = "rw",
            description = "Kernel Reserved",
            value = 0
        }
        self.registers["R28"] = {
            address = 0x70,
            size = 4,
            access = "rw",
            description = "Global Pointer",
            value = 0
        }
        self.registers["R29"] = {
            address = 0x74,
            size = 4,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["R30"] = {
            address = 0x78,
            size = 4,
            access = "rw",
            description = "Frame Pointer",
            value = 0
        }
        self.registers["R31"] = {
            address = 0x7C,
            size = 4,
            access = "rw",
            description = "Return Address",
            value = 0
        }
        self.registers["HI"] = {
            address = 0x80,
            size = 4,
            access = "rw",
            description = "Multiply/Divide High",
            value = 0
        }
        self.registers["LO"] = {
            address = 0x84,
            size = 4,
            access = "rw",
            description = "Multiply/Divide Low",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x88,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["CP0_SR"] = {
            address = 0x90,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Status Register",
            value = 0
        }
        self.registers["CP0_CAUSE"] = {
            address = 0x94,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Cause Register",
            value = 0
        }
        self.registers["CP0_EPC"] = {
            address = 0x98,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Exception PC",
            value = 0
        }
        self.registers["CP0_BadVAddr"] = {
            address = 0x9C,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Bad Virtual Address",
            value = 0
        }
        self.registers["CP0_CONTEXT"] = {
            address = 0xA0,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Context Register",
            value = 0
        }
        self.registers["CP0_PID"] = {
            address = 0xA4,
            size = 4,
            access = "rw",
            description = "Coprocessor 0 - Process ID",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPU"] = {
            base = 0x1F801810,
            type = "video",
            description = "Graphics Processing Unit",
            registers = {}
        }
        
        local p = self.peripherals["GPU"]
        p.registers["GP0_CMD"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["GP0_DATA"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["GP1_CMD"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["GP1_DATA"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["TPAGE"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["DRAW_MODE"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["TEXTURE_WIN"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["DRAW_OFFSET_X"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["DRAW_OFFSET_Y"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["DRAW_AREA_X"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["DRAW_AREA_Y"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["DITHER"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_MODE"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_START_X"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_START_Y"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_HORZ"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_VERT"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["DMA_MODE"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["GPU_STAT"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        self.peripherals["GTE"] = {
            base = 0x1F801880,
            type = "video",
            description = "Geometry Transformation Engine",
            registers = {}
        }
        
        local p = self.peripherals["GTE"]
        p.registers["GTE_VXY0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["GTE_VZ0"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["GTE_VXY1"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["GTE_VZ1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["GTE_VXY2"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["GTE_VZ2"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["GTE_RGB0"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["GTE_RGB1"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["GTE_RGB2"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["GTE_RTP"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        p.registers["GTE_TRX"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["GTE_TRY"] = {
            address = 0x38,
            size = 4,
            value = 0
        }
        p.registers["GTE_TRZ"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["GTE_MAC0"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        p.registers["GTE_MAC1"] = {
            address = 0x44,
            size = 4,
            value = 0
        }
        p.registers["GTE_MAC2"] = {
            address = 0x48,
            size = 4,
            value = 0
        }
        p.registers["GTE_MAC3"] = {
            address = 0x4C,
            size = 4,
            value = 0
        }
        p.registers["GTE_IR0"] = {
            address = 0x50,
            size = 4,
            value = 0
        }
        p.registers["GTE_IR1"] = {
            address = 0x54,
            size = 4,
            value = 0
        }
        p.registers["GTE_IR2"] = {
            address = 0x58,
            size = 4,
            value = 0
        }
        p.registers["GTE_IR3"] = {
            address = 0x5C,
            size = 4,
            value = 0
        }
        p.registers["GTE_LZCS"] = {
            address = 0x60,
            size = 4,
            value = 0
        }
        p.registers["GTE_LZCR"] = {
            address = 0x64,
            size = 4,
            value = 0
        }
        p.registers["GTE_CTX"] = {
            address = 0x68,
            size = 4,
            value = 0
        }
        p.registers["GTE_CTY"] = {
            address = 0x6C,
            size = 4,
            value = 0
        }
        p.registers["GTE_CTZ"] = {
            address = 0x70,
            size = 4,
            value = 0
        }
        p.registers["GTE_RTX"] = {
            address = 0x74,
            size = 4,
            value = 0
        }
        p.registers["GTE_RTY"] = {
            address = 0x78,
            size = 4,
            value = 0
        }
        p.registers["GTE_RTZ"] = {
            address = 0x7C,
            size = 4,
            value = 0
        }
        p.registers["GTE_SR"] = {
            address = 0x80,
            size = 4,
            value = 0
        }
        p.registers["GTE_CMD"] = {
            address = 0x84,
            size = 4,
            value = 0
        }
        p.registers["GTE_H"] = {
            address = 0x88,
            size = 4,
            value = 0
        }
        p.registers["GTE_DQB"] = {
            address = 0x8C,
            size = 4,
            value = 0
        }
        p.registers["GTE_DQA"] = {
            address = 0x90,
            size = 4,
            value = 0
        }
        p.registers["GTE_ZSF3"] = {
            address = 0x94,
            size = 4,
            value = 0
        }
        p.registers["GTE_ZSF4"] = {
            address = 0x98,
            size = 4,
            value = 0
        }
        p.registers["GTE_OTZ"] = {
            address = 0x9C,
            size = 4,
            value = 0
        }
        self.peripherals["SPU"] = {
            base = 0x1F801C00,
            type = "audio",
            description = "Sound Processing Unit (24-channel ADPCM)",
            registers = {}
        }
        
        local p = self.peripherals["SPU"]
        p.registers["SPU_CTRL"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["SPU_STAT"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["SPU_CDVOL_L"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["SPU_CDVOL_R"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["SPU_MAINVOL_L"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["SPU_MAINVOL_R"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        p.registers["SPU_REVERB_L"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        p.registers["SPU_REVERB_R"] = {
            address = 0x12,
            size = 2,
            value = 0
        }
        p.registers["SPU_KEYON"] = {
            address = 0x80,
            size = 2,
            value = 0
        }
        p.registers["SPU_KEYOFF"] = {
            address = 0x82,
            size = 2,
            value = 0
        }
        p.registers["SPU_CHANNEL_MUTE"] = {
            address = 0x84,
            size = 2,
            value = 0
        }
        p.registers["SPU_NOISE_CLK"] = {
            address = 0x88,
            size = 2,
            value = 0
        }
        p.registers["SPU_REVERB_ADDR"] = {
            address = 0x8A,
            size = 2,
            value = 0
        }
        p.registers["SPU_IRQ_ADDR"] = {
            address = 0x8C,
            size = 2,
            value = 0
        }
        p.registers["SPU_REVERB_VOL_L"] = {
            address = 0x8E,
            size = 2,
            value = 0
        }
        p.registers["SPU_REVERB_VOL_R"] = {
            address = 0x90,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_VOL_L"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["SPU_VOICE_VOL_R"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["SPU_VOICE_FREQ"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_START"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_ADSR1"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_ADSR2"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_ENV"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["SPU_VOICE_REPEAT"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["VOICE_BASE_SIZE"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        self.peripherals["MDEC"] = {
            base = 0x1F801820,
            type = "video",
            description = "Motion Decoder (JPEG Decompression)",
            registers = {}
        }
        
        local p = self.peripherals["MDEC"]
        p.registers["MDEC_CTRL"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["MDEC_DATA"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["MDEC_BKGD"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0x1F801080,
            type = "dma",
            description = "DMA Controller (7 channels)",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["DMA_DPCR"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["DMA_INT"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH0_BASE"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH0_COUNT"] = {
            address = 0x14,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH0_CTRL"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH1_BASE"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH1_COUNT"] = {
            address = 0x24,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH1_CTRL"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH2_BASE"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH2_COUNT"] = {
            address = 0x34,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH2_CTRL"] = {
            address = 0x38,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH3_BASE"] = {
            address = 0x40,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH3_COUNT"] = {
            address = 0x44,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH3_CTRL"] = {
            address = 0x48,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH4_BASE"] = {
            address = 0x50,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH4_COUNT"] = {
            address = 0x54,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH4_CTRL"] = {
            address = 0x58,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH5_BASE"] = {
            address = 0x60,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH5_COUNT"] = {
            address = 0x64,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH5_CTRL"] = {
            address = 0x68,
            size = 1,
            value = 0
        }
        p.registers["DMA_CH6_BASE"] = {
            address = 0x70,
            size = 4,
            value = 0
        }
        p.registers["DMA_CH6_COUNT"] = {
            address = 0x74,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH6_CTRL"] = {
            address = 0x78,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0x1F801100,
            type = "timer",
            description = "Timers (3 timers)",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["TM0_COUNT"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["TM0_MODE"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["TM0_TARGET"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["TM1_COUNT"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        p.registers["TM1_MODE"] = {
            address = 0x14,
            size = 2,
            value = 0
        }
        p.registers["TM1_TARGET"] = {
            address = 0x18,
            size = 2,
            value = 0
        }
        p.registers["TM2_COUNT"] = {
            address = 0x20,
            size = 2,
            value = 0
        }
        p.registers["TM2_MODE"] = {
            address = 0x24,
            size = 2,
            value = 0
        }
        p.registers["TM2_TARGET"] = {
            address = 0x28,
            size = 2,
            value = 0
        }
        self.peripherals["CDROM"] = {
            base = 0x1F801800,
            type = "io",
            description = "CD-ROM Controller",
            registers = {}
        }
        
        local p = self.peripherals["CDROM"]
        p.registers["CD0_DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CD0_STATUS"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CD0_RESPONSE"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["CD0_DATA1"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["CD0_INT_FLAG"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["CD0_VOLUME_L"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["CD0_VOLUME_R"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        self.peripherals["JOY"] = {
            base = 0x1F801040,
            type = "input",
            description = "JOY Interface",
            registers = {}
        }
        
        local p = self.peripherals["JOY"]
        p.registers["JOY_CTRL"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["JOY_MODE"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["JOY_BAUD"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["JOY_TX_DATA"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["JOY_RX_DATA"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["JOY_STAT"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        self.peripherals["SIO"] = {
            base = 0x1F801050,
            type = "uart",
            description = "SIO (Serial I/O - Memory Card)",
            registers = {}
        }
        
        local p = self.peripherals["SIO"]
        p.registers["SIO_DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["SIO_STATUS"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["SIO_MODE"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["SIO_CTRL"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["SIO_BAUD"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        self.peripherals["INTERRUPT"] = {
            base = 0x1F801070,
            type = "system",
            description = "Interrupt Controller",
            registers = {}
        }
        
        local p = self.peripherals["INTERRUPT"]
        p.registers["INT_STAT"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["INT_MASK"] = {
            address = 0x04,
            size = 2,
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
            name = MIPS_R3000A.DEVICE_NAME,
            manufacturer = MIPS_R3000A.MANUFACTURER,
            family = MIPS_R3000A.FAMILY,
            version = MIPS_R3000A.VERSION,
            architecture = MIPS_R3000A.ARCHITECTURE,
            bits = MIPS_R3000A.BITS,
            clock_frequency = MIPS_R3000A.CLOCK_FREQUENCY
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
        return string.format("MIPS_R3000A(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MIPS_R3000A.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MIPS_R3000A.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MIPS_R3000A.print_device_info(device)
    device = device or MIPS_R3000A.new()
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

function MIPS_R3000A.print_registers(device)
    device = device or MIPS_R3000A.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MIPS_R3000A.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MIPS_R3000A.example()
    print("=== MIPS-R3000A设备示例 ===")
    
    -- 创建设备实例
    local device = MIPS_R3000A.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MIPS_R3000A.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. MIPS_R3000A.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. MIPS_R3000A.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    MIPS_R3000A.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MIPS_R3000A.lua$") then
    MIPS_R3000A.example()
end

return MIPS_R3000A
