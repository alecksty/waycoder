--[[
  NEC-VR4300设备定义 - Lua模块
  生成自: NEC/MIPS-R4000/NEC-VR4300
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like
  CPU架构: MIPS-R4300i
  位宽: 64位
  时钟频率: 93750000 Hz
]]

local NEC_VR4300 = {}

-- 设备信息
NEC_VR4300.DEVICE_NAME = "NEC-VR4300"
NEC_VR4300.MANUFACTURER = "NEC"
NEC_VR4300.FAMILY = "MIPS-R4000"
NEC_VR4300.VERSION = "1.0"
NEC_VR4300.ARCHITECTURE = "MIPS-R4300i"
NEC_VR4300.BITS = 64
NEC_VR4300.CLOCK_FREQUENCY = 93750000

-- 寄存器地址定义
NEC_VR4300.R0_ADDR = 0x00  -- Hard-wired Zero
NEC_VR4300.R1_ADDR = 0x08  -- Assembler Temporary
NEC_VR4300.R2_ADDR = 0x10  -- Value Return
NEC_VR4300.R3_ADDR = 0x18  -- Expression Evaluation
NEC_VR4300.R4_ADDR = 0x20  -- Expression Evaluation
NEC_VR4300.R5_ADDR = 0x28  -- Expression Evaluation
NEC_VR4300.R6_ADDR = 0x30  -- Expression Evaluation
NEC_VR4300.R7_ADDR = 0x38  -- Expression Evaluation
NEC_VR4300.R8_ADDR = 0x40  -- Expression Evaluation
NEC_VR4300.R9_ADDR = 0x48  -- Expression Evaluation
NEC_VR4300.R10_ADDR = 0x50  -- Expression Evaluation
NEC_VR4300.R11_ADDR = 0x58  -- Expression Evaluation
NEC_VR4300.R12_ADDR = 0x60  -- Expression Evaluation
NEC_VR4300.R13_ADDR = 0x68  -- Expression Evaluation
NEC_VR4300.R14_ADDR = 0x70  -- Expression Evaluation
NEC_VR4300.R15_ADDR = 0x78  -- Expression Evaluation
NEC_VR4300.R16_ADDR = 0x80  -- Saved Value
NEC_VR4300.R17_ADDR = 0x88  -- Saved Value
NEC_VR4300.R18_ADDR = 0x90  -- Saved Value
NEC_VR4300.R19_ADDR = 0x98  -- Saved Value
NEC_VR4300.R20_ADDR = 0xA0  -- Saved Value
NEC_VR4300.R21_ADDR = 0xA8  -- Saved Value
NEC_VR4300.R22_ADDR = 0xB0  -- Saved Value
NEC_VR4300.R23_ADDR = 0xB8  -- Saved Value
NEC_VR4300.R24_ADDR = 0xC0  -- Temporary
NEC_VR4300.R25_ADDR = 0xC8  -- Temporary
NEC_VR4300.R26_ADDR = 0xD0  -- Kernel Reserved
NEC_VR4300.R27_ADDR = 0xD8  -- Kernel Reserved
NEC_VR4300.R28_ADDR = 0xE0  -- Global Pointer
NEC_VR4300.R29_ADDR = 0xE8  -- Stack Pointer
NEC_VR4300.R30_ADDR = 0xF0  -- Frame Pointer
NEC_VR4300.R31_ADDR = 0xF8  -- Return Address
NEC_VR4300.HI_ADDR = 0x100  -- Multiply/Divide High (64-bit)
NEC_VR4300.LO_ADDR = 0x108  -- Multiply/Divide Low (64-bit)
NEC_VR4300.PC_ADDR = 0x110  -- Program Counter
NEC_VR4300.LLB_ADDR = 0x118  -- LLAddr / LLBit (for LL/SC)
NEC_VR4300.CP0_INDEX_ADDR = 0x200  -- TLB Index
NEC_VR4300.CP0_RANDOM_ADDR = 0x208  -- TLB Random
NEC_VR4300.CP0_ENTRYLO0_ADDR = 0x210  -- TLB EntryLo 0 (even page)
NEC_VR4300.CP0_ENTRYLO1_ADDR = 0x218  -- TLB EntryLo 1 (odd page)
NEC_VR4300.CP0_CONTEXT_ADDR = 0x220  -- Context Register (PTE base)
NEC_VR4300.CP0_PAGEMASK_ADDR = 0x228  -- Page Mask (variable page size)
NEC_VR4300.CP0_WIRED_ADDR = 0x230  -- TLB Wired
NEC_VR4300.CP0_BADVADDR_ADDR = 0x238  -- Bad Virtual Address
NEC_VR4300.CP0_COUNT_ADDR = 0x240  -- Count (incrementing timer)
NEC_VR4300.CP0_ENTRYHI_ADDR = 0x250  -- TLB EntryHi (VPN2 + ASID)
NEC_VR4300.CP0_COMPARE_ADDR = 0x258  -- Compare (timer interrupt)
NEC_VR4300.CP0_STATUS_ADDR = 0x260  -- Status Register
NEC_VR4300.CP0_STATUS_IE_BIT = 0  -- Interrupt Enable
NEC_VR4300.CP0_STATUS_EXL_BIT = 1  -- Exception Level
NEC_VR4300.CP0_STATUS_ERL_BIT = 2  -- Error Level
NEC_VR4300.CP0_STATUS_KSU_BIT = 0  -- Kernel/User Mode
NEC_VR4300.CP0_STATUS_UX_BIT = 5  -- User Mode 64-bit (1=64-bit user)
NEC_VR4300.CP0_STATUS_SX_BIT = 6  -- Supervisor Mode 64-bit
NEC_VR4300.CP0_STATUS_KX_BIT = 7  -- Kernel Mode 64-bit
NEC_VR4300.CP0_STATUS_IM0_7_BIT = 0  -- Interrupt Mask
NEC_VR4300.CP0_STATUS_CU0_BIT = 28  -- Coprocessor 0 Usable
NEC_VR4300.CP0_STATUS_BEV_BIT = 22  -- Bootstrap Exception Vector
NEC_VR4300.CP0_STATUS_TS_BIT = 21  -- TLB Shutdown
NEC_VR4300.CP0_STATUS_FR_BIT = 26  -- Floating-Point Register Mode (32 double)
NEC_VR4300.CP0_CAUSE_ADDR = 0x268  -- Cause Register
NEC_VR4300.CP0_CAUSE_EXCCODE_BIT = 0  -- Exception Code
NEC_VR4300.CP0_CAUSE_IP0_7_BIT = 0  -- Interrupt Pending
NEC_VR4300.CP0_CAUSE_BD_BIT = 31  -- Branch Delay Slot
NEC_VR4300.CP0_CAUSE_CE_BIT = 0  -- Coprocessor Error
NEC_VR4300.CP0_EPC_ADDR = 0x270  -- Exception PC
NEC_VR4300.CP0_CONFIG_ADDR = 0x280  -- Config Register
NEC_VR4300.CP0_LLADDR_ADDR = 0x288  -- Load Linked Address
NEC_VR4300.CP0_WATCHLO_ADDR = 0x290  -- WatchLo (data/instruction break)
NEC_VR4300.CP0_WATCHHI_ADDR = 0x298  -- WatchHi
NEC_VR4300.CP0_XCONTEXT_ADDR = 0x2A0  -- Extended Context
NEC_VR4300.CP0_PID_ADDR = 0x2B0  -- Tag/Process ID
NEC_VR4300.CP0_DEBUG_ADDR = 0x2D8  -- Debug Register
NEC_VR4300.CP0_PERF_ADDR = 0x2F0  -- Performance Counter

-- 内存段定义
NEC_VR4300.RDRAM_START = 0x00000000
NEC_VR4300.RDRAM_END = 0x003FFFFF
NEC_VR4300.RDRAM_SIZE = 4194304  -- RDRAM (4MB base, up to 8MB)
NEC_VR4300.RDRAM_REG_START = 0x18000000
NEC_VR4300.RDRAM_REG_END = 0x18000FFF
NEC_VR4300.RDRAM_REG_SIZE = 4096  -- RDRAM Registers
NEC_VR4300.SP_MEM_START = 0x1FC00000
NEC_VR4300.SP_MEM_END = 0x1FC007FF
NEC_VR4300.SP_MEM_SIZE = 2048  -- RCP SP Memory / DMEM (2KB)
NEC_VR4300.SP_IMEM_START = 0x1FC00800
NEC_VR4300.SP_IMEM_END = 0x1FC00FFF
NEC_VR4300.SP_IMEM_SIZE = 2048  -- RCP SP Instruction Memory / IMEM (2KB)
NEC_VR4300.RCP_REGS_START = 0x1FC00000
NEC_VR4300.RCP_REGS_END = 0x1FC3FFFF
NEC_VR4300.RCP_REGS_SIZE = 262144  -- RCP Register Area
NEC_VR4300.PI_REGS_START = 0x1FC00000
NEC_VR4300.PI_REGS_END = 0x1FC007FF
NEC_VR4300.PI_REGS_SIZE = 2048  -- PI (Peripheral Interface) Registers
NEC_VR4300.VI_REGS_START = 0x1FC002C0
NEC_VR4300.VI_REGS_END = 0x1FC002FF
NEC_VR4300.VI_REGS_SIZE = 64  -- VI (Video Interface) Registers
NEC_VR4300.AI_REGS_START = 0x1FC00500
NEC_VR4300.AI_REGS_END = 0x1FC0053F
NEC_VR4300.AI_REGS_SIZE = 64  -- AI (Audio Interface) Registers
NEC_VR4300.SI_REGS_START = 0x1FC004C0
NEC_VR4300.SI_REGS_END = 0x1FC004FF
NEC_VR4300.SI_REGS_SIZE = 64  -- SI (Serial Interface) Registers
NEC_VR4300.PI_DRAM_START = 0xA0000000
NEC_VR4300.PI_DRAM_END = 0xA4000000
NEC_VR4300.PI_DRAM_SIZE = 67108864  -- PI Bus DRAM (cartridge)
NEC_VR4300.CART_ROM_START = 0xB0000000
NEC_VR4300.CART_ROM_END = 0xBFFFFFFF
NEC_VR4300.CART_ROM_SIZE = 268435456  -- Cartridge ROM (up to 256MB)
NEC_VR4300.PIF_RAM_START = 0x1FC007C0
NEC_VR4300.PIF_RAM_END = 0x1FC007FF
NEC_VR4300.PIF_RAM_SIZE = 64  -- PIF-NUS ROM/RAM (CIC)

-- 外设定义
-- Reality Signal Processor (Audio/Video microcode engine)
NEC_VR4300.RSP_BASE = 0x04040000
NEC_VR4300.RSP_SP_MEM_ADDR_ADDR = 0x00
NEC_VR4300.RSP_SP_DRAM_ADDR_ADDR = 0x04
NEC_VR4300.RSP_SP_RD_LEN_ADDR = 0x08
NEC_VR4300.RSP_SP_WR_LEN_ADDR = 0x0C
NEC_VR4300.RSP_SP_STATUS_ADDR = 0x10
NEC_VR4300.RSP_SP_STATUS_BROKE_BIT = 0  -- Command Queue Broke
NEC_VR4300.RSP_SP_STATUS_SLEEP_BIT = 2  -- SP Sleep
NEC_VR4300.RSP_SP_STATUS_GOODMATCH_BIT = 3  -- DMEM/IMEM Goodmatch
NEC_VR4300.RSP_SP_STATUS_SSTEP_BIT = 4  -- Single Step
NEC_VR4300.RSP_SP_STATUS_INTSIG_BIT = 5  -- Interrupt Signal
NEC_VR4300.RSP_SP_STATUS_HALT_BIT = 6  -- Halt
NEC_VR4300.RSP_SP_STATUS_CLEAR_BIT = 7  -- Clear SP Status
NEC_VR4300.RSP_SP_STATUS_INTR_BRK_BIT = 8  -- IntrOnBreak
NEC_VR4300.RSP_SP_STATUS_SIGNAL0_BIT = 12  -- Software Signal 0
NEC_VR4300.RSP_SP_STATUS_SIGNAL1_BIT = 13  -- Software Signal 1
NEC_VR4300.RSP_SP_STATUS_SIGNAL2_BIT = 14  -- Software Signal 2
NEC_VR4300.RSP_SP_STATUS_SIGNAL3_BIT = 15  -- Software Signal 3
NEC_VR4300.RSP_SP_STATUS_SIGNAL4_BIT = 16  -- Software Signal 4
NEC_VR4300.RSP_SP_STATUS_SIGNAL5_BIT = 17  -- Software Signal 5
NEC_VR4300.RSP_SP_STATUS_SIGNAL6_BIT = 18  -- Software Signal 6
NEC_VR4300.RSP_SP_STATUS_SIGNAL7_BIT = 19  -- Software Signal 7
NEC_VR4300.RSP_SP_DMA_FULL_ADDR = 0x14
NEC_VR4300.RSP_SP_DMA_BUSY_ADDR = 0x18
NEC_VR4300.RSP_SP_SEMAPHORE_ADDR = 0x1C
NEC_VR4300.RSP_SP_PC_ADDR = 0x20
NEC_VR4300.RSP_SP_IBIST_ADDR = 0x24
-- Reality Drawing Processor (Triangle/Quad rasterizer)
NEC_VR4300.RDP_BASE = 0x04100000
NEC_VR4300.RDP_DP_START_ADDR = 0x00
NEC_VR4300.RDP_DP_END_ADDR = 0x04
NEC_VR4300.RDP_DP_CURRENT_ADDR = 0x08
NEC_VR4300.RDP_DP_STATUS_ADDR = 0x0C
NEC_VR4300.RDP_DP_STATUS_TERMINATE_BIT = 0  -- Terminator
NEC_VR4300.RDP_DP_STATUS_PIPE_BUSY_BIT = 1  -- Pipeline Busy
NEC_VR4300.RDP_DP_STATUS_TOMINO_BUSY_BIT = 2  -- ToMini Busy
NEC_VR4300.RDP_DP_STATUS_PIPE_FLUSH_BIT = 3  -- Pipeline Flush
NEC_VR4300.RDP_DP_STATUS_TOMINO_FLUSH_BIT = 4  -- ToMini Flush
NEC_VR4300.RDP_DP_STATUS_FREEZE_BIT = 5  -- Freeze
NEC_VR4300.RDP_DP_STATUS_START_GCLK_BIT = 24  -- Start GCLK
NEC_VR4300.RDP_DP_CLOCK_ADDR = 0x10
NEC_VR4300.RDP_DP_BUFBUSY_ADDR = 0x14
NEC_VR4300.RDP_DP_PIPEBUSY_ADDR = 0x18
NEC_VR4300.RDP_DP_TMEM_ADDR = 0x1C
-- Video Interface (scanout engine)
NEC_VR4300.VI_BASE = 0x04400000
NEC_VR4300.VI_VI_STATUS_ADDR = 0x00
NEC_VR4300.VI_VI_STATUS_TYPE_BIT = 0  -- Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
NEC_VR4300.VI_VI_STATUS_DITHER_FILTER_BIT = 6  -- Dither Filter Enable
NEC_VR4300.VI_VI_STATUS_GAMMA_BIT = 7  -- Gamma Correction Enable
NEC_VR4300.VI_VI_STATUS_GAMMA_DITHER_BIT = 8  -- Gamma Dither Enable
NEC_VR4300.VI_VI_STATUS_DIVOT_BIT = 9  -- Divot Control
NEC_VR4300.VI_VI_STATUS_SERRATION_BIT = 10  --  Serration Enable (for interlaced)
NEC_VR4300.VI_VI_ORIGIN_ADDR = 0x04
NEC_VR4300.VI_VI_WIDTH_ADDR = 0x08
NEC_VR4300.VI_VI_V_INTR_ADDR = 0x0C
NEC_VR4300.VI_VI_V_CURRENT_ADDR = 0x10
NEC_VR4300.VI_VI_BURST_ADDR = 0x14
NEC_VR4300.VI_VI_H_SYNC_ADDR = 0x18
NEC_VR4300.VI_VI_H_SYNC_LEAP_ADDR = 0x1C
NEC_VR4300.VI_VI_H_VIDEO_ADDR = 0x20
NEC_VR4300.VI_VI_V_VIDEO_ADDR = 0x24
NEC_VR4300.VI_VI_V_BURST_ADDR = 0x28
NEC_VR4300.VI_VI_X_SCALE_ADDR = 0x2C
NEC_VR4300.VI_VI_Y_SCALE_ADDR = 0x30
-- Audio Interface (DAC)
NEC_VR4300.AI_BASE = 0x04500000
NEC_VR4300.AI_AI_DRAM_ADDR_ADDR = 0x00
NEC_VR4300.AI_AI_LEN_ADDR = 0x04
NEC_VR4300.AI_AI_CONTROL_ADDR = 0x08
NEC_VR4300.AI_AI_CONTROL_DMA_ENABLE_BIT = 0  -- DMA Enable
NEC_VR4300.AI_AI_CONTROL_DMA_FIFO_FULL_BIT = 1  -- DMA FIFO Full
NEC_VR4300.AI_AI_STATUS_ADDR = 0x0C
NEC_VR4300.AI_AI_DACRATE_ADDR = 0x10
NEC_VR4300.AI_AI_BITRATE_ADDR = 0x14
-- Peripheral Interface (cartridge bus)
NEC_VR4300.PI_BASE = 0x04600000
NEC_VR4300.PI_PI_DRAM_ADDR_ADDR = 0x00
NEC_VR4300.PI_PI_CART_ADDR_ADDR = 0x04
NEC_VR4300.PI_PI_RD_LEN_ADDR = 0x08
NEC_VR4300.PI_PI_WR_LEN_ADDR = 0x0C
NEC_VR4300.PI_PI_STATUS_ADDR = 0x10
NEC_VR4300.PI_PI_STATUS_DMA_BUSY_BIT = 0  -- DMA Busy
NEC_VR4300.PI_PI_STATUS_IO_BUSY_BIT = 1  -- I/O Busy
NEC_VR4300.PI_PI_STATUS_ERROR_BIT = 2  -- Bus Error
NEC_VR4300.PI_PI_BSD_DOM1_LAT_ADDR = 0x14
NEC_VR4300.PI_PI_BSD_DOM1_PWD_ADDR = 0x18
NEC_VR4300.PI_PI_BSD_DOM1_PGS_ADDR = 0x1C
NEC_VR4300.PI_PI_BSD_DOM1_RLS_ADDR = 0x20
NEC_VR4300.PI_PI_BSD_DOM2_LAT_ADDR = 0x24
NEC_VR4300.PI_PI_BSD_DOM2_PWD_ADDR = 0x28
NEC_VR4300.PI_PI_BSD_DOM2_PGS_ADDR = 0x2C
NEC_VR4300.PI_PI_BSD_DOM2_RLS_ADDR = 0x30
-- Serial Interface (Controller Pak / 64DD)
NEC_VR4300.SI_BASE = 0x04800000
NEC_VR4300.SI_SI_DRAM_ADDR_ADDR = 0x00
NEC_VR4300.SI_SI_PIF_ADDR_RD64B_ADDR = 0x04
NEC_VR4300.SI_SI_PIF_ADDR_WR64B_ADDR = 0x08
NEC_VR4300.SI_SI_STATUS_ADDR = 0x10
NEC_VR4300.SI_SI_STATUS_DMA_BUSY_BIT = 0  -- DMA Busy
NEC_VR4300.SI_SI_STATUS_IO_BUSY_BIT = 1  -- I/O Busy
NEC_VR4300.SI_SI_STATUS_INTERRUPT_BIT = 12  -- SI Interrupt
-- PIF (CIC / NUSYC - anti-piracy/copy protection)
NEC_VR4300.PIF_BASE = 0x1FC007C0
NEC_VR4300.PIF_PIF_CMD0_ADDR = 0x00
NEC_VR4300.PIF_PIF_CMD1_ADDR = 0x01
NEC_VR4300.PIF_PIF_CMD2_ADDR = 0x02
NEC_VR4300.PIF_PIF_CMD3_ADDR = 0x03
NEC_VR4300.PIF_PIF_CMD4_ADDR = 0x04
NEC_VR4300.PIF_PIF_CMD5_ADDR = 0x05
NEC_VR4300.PIF_PIF_CMD6_ADDR = 0x06
NEC_VR4300.PIF_PIF_CMD7_ADDR = 0x07
NEC_VR4300.PIF_PIF_STATUS_ADDR = 0x3F
-- Interrupt Control
NEC_VR4300.INTERRUPT_BASE = 0x1FC00200
NEC_VR4300.INTERRUPT_MI_MODE_ADDR = 0x00
NEC_VR4300.INTERRUPT_MI_MODE_INIT_MODE_BIT = 0  -- Initialize Mode
NEC_VR4300.INTERRUPT_MI_MODE_EBUS_TEST_BIT = 1  -- EBUS Test Mode
NEC_VR4300.INTERRUPT_MI_VERSION_ADDR = 0x04
NEC_VR4300.INTERRUPT_MI_INTR_ADDR = 0x08
NEC_VR4300.INTERRUPT_MI_INTR_SP_BIT = 0  -- SP Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_SI_BIT = 1  -- SI Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_AI_BIT = 2  -- AI Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_VI_BIT = 3  -- VI Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_PI_BIT = 4  -- PI Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_DP_BIT = 5  -- DP Interrupt Pending
NEC_VR4300.INTERRUPT_MI_INTR_MASK_ADDR = 0x0C
NEC_VR4300.INTERRUPT_MI_INTR_MASK_SP_MASK_BIT = 0  -- SP Interrupt Mask
NEC_VR4300.INTERRUPT_MI_INTR_MASK_SI_MASK_BIT = 1  -- SI Interrupt Mask
NEC_VR4300.INTERRUPT_MI_INTR_MASK_AI_MASK_BIT = 2  -- AI Interrupt Mask
NEC_VR4300.INTERRUPT_MI_INTR_MASK_VI_MASK_BIT = 3  -- VI Interrupt Mask
NEC_VR4300.INTERRUPT_MI_INTR_MASK_PI_MASK_BIT = 4  -- PI Interrupt Mask
NEC_VR4300.INTERRUPT_MI_INTR_MASK_DP_MASK_BIT = 5  -- DP Interrupt Mask
-- Controller Interface (SI channel 0-3)
NEC_VR4300.CONTROLLER_BASE = 0x1FC00600
NEC_VR4300.CONTROLLER_SI_CH0_DATA_ADDR = 0x00
NEC_VR4300.CONTROLLER_SI_CH1_DATA_ADDR = 0x08
NEC_VR4300.CONTROLLER_SI_CH2_DATA_ADDR = 0x10
NEC_VR4300.CONTROLLER_SI_CH3_DATA_ADDR = 0x18

-- 中断向量定义
NEC_VR4300.INT_RESET = 0  -- Soft Reset / NMI
NEC_VR4300.INT_TLB_REFILL = 1  -- TLB Refill (I) / TLB Refill (D)
NEC_VR4300.INT_CACHE_ERROR = 2  -- Cache Error
NEC_VR4300.INT_GENERAL_EXCEPTION = 3  -- General Exception
NEC_VR4300.INT_RSP = 4  -- RSP Interrupt (microcode signal)
NEC_VR4300.INT_RDP = 5  -- RDP Interrupt (display list complete)
NEC_VR4300.INT_VI = 6  -- VI Interrupt (V-Blank / scanline)
NEC_VR4300.INT_AI = 7  -- AI Interrupt (audio DMA complete)
NEC_VR4300.INT_PI = 8  -- PI Interrupt (cartridge DMA)
NEC_VR4300.INT_SI = 9  -- SI Interrupt (serial interface)
NEC_VR4300.INT_TIMER_COMPARE = 10  -- Timer Compare (CP0 Count == Compare)

-- 引脚定义
NEC_VR4300.PIN_VCC = 1  -- Power Supply (3.3V)
NEC_VR4300.PIN_VSS = 2  -- Ground
NEC_VR4300.PIN_CLK = 3  -- System Clock (93.75MHz from CIC/PLL)
NEC_VR4300.PIN_RESET = 4  -- Reset (active low)
NEC_VR4300.PIN_NMI = 5  -- Non-Maskable Interrupt
NEC_VR4300.PIN_INT0 = 6  -- Interrupt 0 (RCP)
NEC_VR4300.PIN_INT1 = 7  -- Interrupt 1 (cartridge)
NEC_VR4300.PIN_INT2 = 8  -- Interrupt 2 (SI)
NEC_VR4300.PIN_INT3 = 9  -- Interrupt 3 (PIF)
NEC_VR4300.PIN_AB0 = 10  -- Address Bus Bit 0
NEC_VR4300.PIN_AB1 = 11  -- Address Bus Bit 1
NEC_VR4300.PIN_AB2 = 12  -- Address Bus Bit 2
NEC_VR4300.PIN_AB3 = 13  -- Address Bus Bit 3
NEC_VR4300.PIN_AB4 = 14  -- Address Bus Bit 4
NEC_VR4300.PIN_AB5 = 15  -- Address Bus Bit 5
NEC_VR4300.PIN_AB6 = 16  -- Address Bus Bit 6
NEC_VR4300.PIN_AB7 = 17  -- Address Bus Bit 7
NEC_VR4300.PIN_AB8 = 18  -- Address Bus Bit 8
NEC_VR4300.PIN_AB9 = 19  -- Address Bus Bit 9
NEC_VR4300.PIN_AB10 = 20  -- Address Bus Bit 10
NEC_VR4300.PIN_AB11 = 21  -- Address Bus Bit 11
NEC_VR4300.PIN_AB12 = 22  -- Address Bus Bit 12
NEC_VR4300.PIN_AB13 = 23  -- Address Bus Bit 13
NEC_VR4300.PIN_AB14 = 24  -- Address Bus Bit 14
NEC_VR4300.PIN_AB15 = 25  -- Address Bus Bit 15
NEC_VR4300.PIN_AB16 = 26  -- Address Bus Bit 16
NEC_VR4300.PIN_AB17 = 27  -- Address Bus Bit 17
NEC_VR4300.PIN_AB18 = 28  -- Address Bus Bit 18
NEC_VR4300.PIN_AB19 = 29  -- Address Bus Bit 19
NEC_VR4300.PIN_AB20 = 30  -- Address Bus Bit 20
NEC_VR4300.PIN_AB21 = 31  -- Address Bus Bit 21
NEC_VR4300.PIN_AB22 = 32  -- Address Bus Bit 22
NEC_VR4300.PIN_AB23 = 33  -- Address Bus Bit 23
NEC_VR4300.PIN_AB24 = 34  -- Address Bus Bit 24
NEC_VR4300.PIN_AB25 = 35  -- Address Bus Bit 25
NEC_VR4300.PIN_AB26 = 36  -- Address Bus Bit 26
NEC_VR4300.PIN_AB27 = 37  -- Address Bus Bit 27
NEC_VR4300.PIN_AB28 = 38  -- Address Bus Bit 28
NEC_VR4300.PIN_AB29 = 39  -- Address Bus Bit 29
NEC_VR4300.PIN_AB30 = 40  -- Address Bus Bit 30
NEC_VR4300.PIN_AB31 = 41  -- Address Bus Bit 31
NEC_VR4300.PIN_AB32 = 42  -- Address Bus Bit 32
NEC_VR4300.PIN_AB33 = 43  -- Address Bus Bit 33
NEC_VR4300.PIN_AB34 = 44  -- Address Bus Bit 34
NEC_VR4300.PIN_AB35 = 45  -- Address Bus Bit 35
NEC_VR4300.PIN_DB0 = 46  -- Data Bus Bit 0
NEC_VR4300.PIN_DB1 = 47  -- Data Bus Bit 1
NEC_VR4300.PIN_DB2 = 48  -- Data Bus Bit 2
NEC_VR4300.PIN_DB3 = 49  -- Data Bus Bit 3
NEC_VR4300.PIN_DB4 = 50  -- Data Bus Bit 4
NEC_VR4300.PIN_DB5 = 51  -- Data Bus Bit 5
NEC_VR4300.PIN_DB6 = 52  -- Data Bus Bit 6
NEC_VR4300.PIN_DB7 = 53  -- Data Bus Bit 7
NEC_VR4300.PIN_DB8 = 54  -- Data Bus Bit 8
NEC_VR4300.PIN_DB9 = 55  -- Data Bus Bit 9
NEC_VR4300.PIN_DB10 = 56  -- Data Bus Bit 10
NEC_VR4300.PIN_DB11 = 57  -- Data Bus Bit 11
NEC_VR4300.PIN_DB12 = 58  -- Data Bus Bit 12
NEC_VR4300.PIN_DB13 = 59  -- Data Bus Bit 13
NEC_VR4300.PIN_DB14 = 60  -- Data Bus Bit 14
NEC_VR4300.PIN_DB15 = 61  -- Data Bus Bit 15
NEC_VR4300.PIN_DB16 = 62  -- Data Bus Bit 16
NEC_VR4300.PIN_DB17 = 63  -- Data Bus Bit 17
NEC_VR4300.PIN_DB18 = 64  -- Data Bus Bit 18
NEC_VR4300.PIN_DB19 = 65  -- Data Bus Bit 19
NEC_VR4300.PIN_DB20 = 66  -- Data Bus Bit 20
NEC_VR4300.PIN_DB21 = 67  -- Data Bus Bit 21
NEC_VR4300.PIN_DB22 = 68  -- Data Bus Bit 22
NEC_VR4300.PIN_DB23 = 69  -- Data Bus Bit 23
NEC_VR4300.PIN_DB24 = 70  -- Data Bus Bit 24
NEC_VR4300.PIN_DB25 = 71  -- Data Bus Bit 25
NEC_VR4300.PIN_DB26 = 72  -- Data Bus Bit 26
NEC_VR4300.PIN_DB27 = 73  -- Data Bus Bit 27
NEC_VR4300.PIN_DB28 = 74  -- Data Bus Bit 28
NEC_VR4300.PIN_DB29 = 75  -- Data Bus Bit 29
NEC_VR4300.PIN_DB30 = 76  -- Data Bus Bit 30
NEC_VR4300.PIN_DB31 = 77  -- Data Bus Bit 31
NEC_VR4300.PIN_BE0 = 78  -- Byte Enable 0
NEC_VR4300.PIN_BE1 = 79  -- Byte Enable 1
NEC_VR4300.PIN_BE2 = 80  -- Byte Enable 2
NEC_VR4300.PIN_BE3 = 81  -- Byte Enable 3
NEC_VR4300.PIN_NCS0 = 82  -- Chip Select 0 (RDRAM)
NEC_VR4300.PIN_NCS1 = 83  -- Chip Select 1 (RCP)
NEC_VR4300.PIN_NCS2 = 84  -- Chip Select 2 (PIF ROM)
NEC_VR4300.PIN_NCS3 = 85  -- Chip Select 3 (Cartridge)
NEC_VR4300.PIN_NWR = 86  -- Write Enable
NEC_VR4300.PIN_NRD = 87  -- Read Enable
NEC_VR4300.PIN_EKN = 88  -- Audio DAC Data (I2S/EKN format)
NEC_VR4300.PIN_AUDIO_L = 89  -- Audio Left Output
NEC_VR4300.PIN_AUDIO_R = 90  -- Audio Right Output
NEC_VR4300.PIN_VIDEO_R = 91  -- Video Output Red
NEC_VR4300.PIN_VIDEO_G = 92  -- Video Output Green
NEC_VR4300.PIN_VIDEO_B = 93  -- Video Output Blue
NEC_VR4300.PIN_SYNC = 94  -- Video Sync

-- 设备类
function NEC_VR4300.new(memory_base)
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
            size = 8,
            access = "r",
            description = "Hard-wired Zero",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x08,
            size = 8,
            access = "rw",
            description = "Assembler Temporary",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x10,
            size = 8,
            access = "rw",
            description = "Value Return",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x18,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x20,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x28,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x30,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x38,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x40,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x48,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x50,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x58,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x60,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R13"] = {
            address = 0x68,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R14"] = {
            address = 0x70,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R15"] = {
            address = 0x78,
            size = 8,
            access = "rw",
            description = "Expression Evaluation",
            value = 0
        }
        self.registers["R16"] = {
            address = 0x80,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R17"] = {
            address = 0x88,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R18"] = {
            address = 0x90,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R19"] = {
            address = 0x98,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R20"] = {
            address = 0xA0,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R21"] = {
            address = 0xA8,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R22"] = {
            address = 0xB0,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R23"] = {
            address = 0xB8,
            size = 8,
            access = "rw",
            description = "Saved Value",
            value = 0
        }
        self.registers["R24"] = {
            address = 0xC0,
            size = 8,
            access = "rw",
            description = "Temporary",
            value = 0
        }
        self.registers["R25"] = {
            address = 0xC8,
            size = 8,
            access = "rw",
            description = "Temporary",
            value = 0
        }
        self.registers["R26"] = {
            address = 0xD0,
            size = 8,
            access = "rw",
            description = "Kernel Reserved",
            value = 0
        }
        self.registers["R27"] = {
            address = 0xD8,
            size = 8,
            access = "rw",
            description = "Kernel Reserved",
            value = 0
        }
        self.registers["R28"] = {
            address = 0xE0,
            size = 8,
            access = "rw",
            description = "Global Pointer",
            value = 0
        }
        self.registers["R29"] = {
            address = 0xE8,
            size = 8,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["R30"] = {
            address = 0xF0,
            size = 8,
            access = "rw",
            description = "Frame Pointer",
            value = 0
        }
        self.registers["R31"] = {
            address = 0xF8,
            size = 8,
            access = "rw",
            description = "Return Address",
            value = 0
        }
        self.registers["HI"] = {
            address = 0x100,
            size = 8,
            access = "rw",
            description = "Multiply/Divide High (64-bit)",
            value = 0
        }
        self.registers["LO"] = {
            address = 0x108,
            size = 8,
            access = "rw",
            description = "Multiply/Divide Low (64-bit)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x110,
            size = 8,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["LLB"] = {
            address = 0x118,
            size = 8,
            access = "rw",
            description = "LLAddr / LLBit (for LL/SC)",
            value = 0
        }
        self.registers["CP0_INDEX"] = {
            address = 0x200,
            size = 8,
            access = "rw",
            description = "TLB Index",
            value = 0
        }
        self.registers["CP0_RANDOM"] = {
            address = 0x208,
            size = 8,
            access = "r",
            description = "TLB Random",
            value = 0
        }
        self.registers["CP0_ENTRYLO0"] = {
            address = 0x210,
            size = 8,
            access = "rw",
            description = "TLB EntryLo 0 (even page)",
            value = 0
        }
        self.registers["CP0_ENTRYLO1"] = {
            address = 0x218,
            size = 8,
            access = "rw",
            description = "TLB EntryLo 1 (odd page)",
            value = 0
        }
        self.registers["CP0_CONTEXT"] = {
            address = 0x220,
            size = 8,
            access = "rw",
            description = "Context Register (PTE base)",
            value = 0
        }
        self.registers["CP0_PAGEMASK"] = {
            address = 0x228,
            size = 8,
            access = "rw",
            description = "Page Mask (variable page size)",
            value = 0
        }
        self.registers["CP0_WIRED"] = {
            address = 0x230,
            size = 8,
            access = "rw",
            description = "TLB Wired",
            value = 0
        }
        self.registers["CP0_BADVADDR"] = {
            address = 0x238,
            size = 8,
            access = "r",
            description = "Bad Virtual Address",
            value = 0
        }
        self.registers["CP0_COUNT"] = {
            address = 0x240,
            size = 8,
            access = "rw",
            description = "Count (incrementing timer)",
            value = 0
        }
        self.registers["CP0_ENTRYHI"] = {
            address = 0x250,
            size = 8,
            access = "rw",
            description = "TLB EntryHi (VPN2 + ASID)",
            value = 0
        }
        self.registers["CP0_COMPARE"] = {
            address = 0x258,
            size = 8,
            access = "rw",
            description = "Compare (timer interrupt)",
            value = 0
        }
        self.registers["CP0_STATUS"] = {
            address = 0x260,
            size = 8,
            access = "rw",
            description = "Status Register",
            value = 0
        }
        self.registers["CP0_CAUSE"] = {
            address = 0x268,
            size = 8,
            access = "rw",
            description = "Cause Register",
            value = 0
        }
        self.registers["CP0_EPC"] = {
            address = 0x270,
            size = 8,
            access = "rw",
            description = "Exception PC",
            value = 0
        }
        self.registers["CP0_CONFIG"] = {
            address = 0x280,
            size = 8,
            access = "rw",
            description = "Config Register",
            value = 0
        }
        self.registers["CP0_LLADDR"] = {
            address = 0x288,
            size = 8,
            access = "r",
            description = "Load Linked Address",
            value = 0
        }
        self.registers["CP0_WATCHLO"] = {
            address = 0x290,
            size = 8,
            access = "rw",
            description = "WatchLo (data/instruction break)",
            value = 0
        }
        self.registers["CP0_WATCHHI"] = {
            address = 0x298,
            size = 8,
            access = "rw",
            description = "WatchHi",
            value = 0
        }
        self.registers["CP0_XCONTEXT"] = {
            address = 0x2A0,
            size = 8,
            access = "rw",
            description = "Extended Context",
            value = 0
        }
        self.registers["CP0_PID"] = {
            address = 0x2B0,
            size = 8,
            access = "rw",
            description = "Tag/Process ID",
            value = 0
        }
        self.registers["CP0_DEBUG"] = {
            address = 0x2D8,
            size = 8,
            access = "rw",
            description = "Debug Register",
            value = 0
        }
        self.registers["CP0_PERF"] = {
            address = 0x2F0,
            size = 8,
            access = "rw",
            description = "Performance Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["RSP"] = {
            base = 0x04040000,
            type = "video",
            description = "Reality Signal Processor (Audio/Video microcode engine)",
            registers = {}
        }
        
        local p = self.peripherals["RSP"]
        p.registers["SP_MEM_ADDR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["SP_DRAM_ADDR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SP_RD_LEN"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SP_WR_LEN"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["SP_STATUS"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["SP_DMA_FULL"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["SP_DMA_BUSY"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["SP_SEMAPHORE"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["SP_PC"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["SP_IBIST"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["RDP"] = {
            base = 0x04100000,
            type = "video",
            description = "Reality Drawing Processor (Triangle/Quad rasterizer)",
            registers = {}
        }
        
        local p = self.peripherals["RDP"]
        p.registers["DP_START"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DP_END"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["DP_CURRENT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["DP_STATUS"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["DP_CLOCK"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["DP_BUFBUSY"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["DP_PIPEBUSY"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["DP_TMEM"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        self.peripherals["VI"] = {
            base = 0x04400000,
            type = "video",
            description = "Video Interface (scanout engine)",
            registers = {}
        }
        
        local p = self.peripherals["VI"]
        p.registers["VI_STATUS"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["VI_ORIGIN"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["VI_WIDTH"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["VI_V_INTR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["VI_V_CURRENT"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["VI_BURST"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["VI_H_SYNC"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["VI_H_SYNC_LEAP"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["VI_H_VIDEO"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["VI_V_VIDEO"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["VI_V_BURST"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["VI_X_SCALE"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["VI_Y_SCALE"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        self.peripherals["AI"] = {
            base = 0x04500000,
            type = "audio",
            description = "Audio Interface (DAC)",
            registers = {}
        }
        
        local p = self.peripherals["AI"]
        p.registers["AI_DRAM_ADDR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["AI_LEN"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["AI_CONTROL"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["AI_STATUS"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["AI_DACRATE"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["AI_BITRATE"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["PI"] = {
            base = 0x04600000,
            type = "io",
            description = "Peripheral Interface (cartridge bus)",
            registers = {}
        }
        
        local p = self.peripherals["PI"]
        p.registers["PI_DRAM_ADDR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PI_CART_ADDR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["PI_RD_LEN"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["PI_WR_LEN"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["PI_STATUS"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM1_LAT"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM1_PWD"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM1_PGS"] = {
            address = 0x1C,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM1_RLS"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM2_LAT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM2_PWD"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM2_PGS"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["PI_BSD_DOM2_RLS"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        self.peripherals["SI"] = {
            base = 0x04800000,
            type = "io",
            description = "Serial Interface (Controller Pak / 64DD)",
            registers = {}
        }
        
        local p = self.peripherals["SI"]
        p.registers["SI_DRAM_ADDR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["SI_PIF_ADDR_RD64B"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["SI_PIF_ADDR_WR64B"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["SI_STATUS"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        self.peripherals["PIF"] = {
            base = 0x1FC007C0,
            type = "io",
            description = "PIF (CIC / NUSYC - anti-piracy/copy protection)",
            registers = {}
        }
        
        local p = self.peripherals["PIF"]
        p.registers["PIF_CMD0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD1"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD2"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD3"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD4"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD5"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD6"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["PIF_CMD7"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["PIF_STATUS"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        self.peripherals["INTERRUPT"] = {
            base = 0x1FC00200,
            type = "system",
            description = "Interrupt Control",
            registers = {}
        }
        
        local p = self.peripherals["INTERRUPT"]
        p.registers["MI_MODE"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["MI_VERSION"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["MI_INTR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["MI_INTR_MASK"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["CONTROLLER"] = {
            base = 0x1FC00600,
            type = "input",
            description = "Controller Interface (SI channel 0-3)",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER"]
        p.registers["SI_CH0_DATA"] = {
            address = 0x00,
            size = 8,
            value = 0
        }
        p.registers["SI_CH1_DATA"] = {
            address = 0x08,
            size = 8,
            value = 0
        }
        p.registers["SI_CH2_DATA"] = {
            address = 0x10,
            size = 8,
            value = 0
        }
        p.registers["SI_CH3_DATA"] = {
            address = 0x18,
            size = 8,
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
            name = NEC_VR4300.DEVICE_NAME,
            manufacturer = NEC_VR4300.MANUFACTURER,
            family = NEC_VR4300.FAMILY,
            version = NEC_VR4300.VERSION,
            architecture = NEC_VR4300.ARCHITECTURE,
            bits = NEC_VR4300.BITS,
            clock_frequency = NEC_VR4300.CLOCK_FREQUENCY
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
        return string.format("NEC_VR4300(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function NEC_VR4300.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function NEC_VR4300.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function NEC_VR4300.print_device_info(device)
    device = device or NEC_VR4300.new()
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

function NEC_VR4300.print_registers(device)
    device = device or NEC_VR4300.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            NEC_VR4300.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function NEC_VR4300.example()
    print("=== NEC-VR4300设备示例 ===")
    
    -- 创建设备实例
    local device = NEC_VR4300.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    NEC_VR4300.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. NEC_VR4300.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. NEC_VR4300.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    NEC_VR4300.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("NEC_VR4300.lua$") then
    NEC_VR4300.example()
end

return NEC_VR4300
