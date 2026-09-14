# NEC-VR4300 设备定义 - R 脚本
# 生成自: NEC/MIPS-R4000/NEC-VR4300
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like
# CPU架构: MIPS-R4300i
# 位宽: 64位
# 时钟频率: 93750000 Hz

# 寄存器地址定义
R0_ADDR <- 0x00  # Hard-wired Zero
R1_ADDR <- 0x08  # Assembler Temporary
R2_ADDR <- 0x10  # Value Return
R3_ADDR <- 0x18  # Expression Evaluation
R4_ADDR <- 0x20  # Expression Evaluation
R5_ADDR <- 0x28  # Expression Evaluation
R6_ADDR <- 0x30  # Expression Evaluation
R7_ADDR <- 0x38  # Expression Evaluation
R8_ADDR <- 0x40  # Expression Evaluation
R9_ADDR <- 0x48  # Expression Evaluation
R10_ADDR <- 0x50  # Expression Evaluation
R11_ADDR <- 0x58  # Expression Evaluation
R12_ADDR <- 0x60  # Expression Evaluation
R13_ADDR <- 0x68  # Expression Evaluation
R14_ADDR <- 0x70  # Expression Evaluation
R15_ADDR <- 0x78  # Expression Evaluation
R16_ADDR <- 0x80  # Saved Value
R17_ADDR <- 0x88  # Saved Value
R18_ADDR <- 0x90  # Saved Value
R19_ADDR <- 0x98  # Saved Value
R20_ADDR <- 0xA0  # Saved Value
R21_ADDR <- 0xA8  # Saved Value
R22_ADDR <- 0xB0  # Saved Value
R23_ADDR <- 0xB8  # Saved Value
R24_ADDR <- 0xC0  # Temporary
R25_ADDR <- 0xC8  # Temporary
R26_ADDR <- 0xD0  # Kernel Reserved
R27_ADDR <- 0xD8  # Kernel Reserved
R28_ADDR <- 0xE0  # Global Pointer
R29_ADDR <- 0xE8  # Stack Pointer
R30_ADDR <- 0xF0  # Frame Pointer
R31_ADDR <- 0xF8  # Return Address
HI_ADDR <- 0x100  # Multiply/Divide High (64-bit)
LO_ADDR <- 0x108  # Multiply/Divide Low (64-bit)
PC_ADDR <- 0x110  # Program Counter
LLB_ADDR <- 0x118  # LLAddr / LLBit (for LL/SC)
CP0_INDEX_ADDR <- 0x200  # TLB Index
CP0_RANDOM_ADDR <- 0x208  # TLB Random
CP0_ENTRYLO0_ADDR <- 0x210  # TLB EntryLo 0 (even page)
CP0_ENTRYLO1_ADDR <- 0x218  # TLB EntryLo 1 (odd page)
CP0_CONTEXT_ADDR <- 0x220  # Context Register (PTE base)
CP0_PAGEMASK_ADDR <- 0x228  # Page Mask (variable page size)
CP0_WIRED_ADDR <- 0x230  # TLB Wired
CP0_BADVADDR_ADDR <- 0x238  # Bad Virtual Address
CP0_COUNT_ADDR <- 0x240  # Count (incrementing timer)
CP0_ENTRYHI_ADDR <- 0x250  # TLB EntryHi (VPN2 + ASID)
CP0_COMPARE_ADDR <- 0x258  # Compare (timer interrupt)
CP0_STATUS_ADDR <- 0x260  # Status Register
CP0_STATUS_IE_BIT <- 0  # Interrupt Enable
CP0_STATUS_EXL_BIT <- 1  # Exception Level
CP0_STATUS_ERL_BIT <- 2  # Error Level
CP0_STATUS_KSU_BIT <- 0  # Kernel/User Mode
CP0_STATUS_UX_BIT <- 5  # User Mode 64-bit (1=64-bit user)
CP0_STATUS_SX_BIT <- 6  # Supervisor Mode 64-bit
CP0_STATUS_KX_BIT <- 7  # Kernel Mode 64-bit
CP0_STATUS_IM0_7_BIT <- 0  # Interrupt Mask
CP0_STATUS_CU0_BIT <- 28  # Coprocessor 0 Usable
CP0_STATUS_BEV_BIT <- 22  # Bootstrap Exception Vector
CP0_STATUS_TS_BIT <- 21  # TLB Shutdown
CP0_STATUS_FR_BIT <- 26  # Floating-Point Register Mode (32 double)
CP0_CAUSE_ADDR <- 0x268  # Cause Register
CP0_CAUSE_EXCCODE_BIT <- 0  # Exception Code
CP0_CAUSE_IP0_7_BIT <- 0  # Interrupt Pending
CP0_CAUSE_BD_BIT <- 31  # Branch Delay Slot
CP0_CAUSE_CE_BIT <- 0  # Coprocessor Error
CP0_EPC_ADDR <- 0x270  # Exception PC
CP0_CONFIG_ADDR <- 0x280  # Config Register
CP0_LLADDR_ADDR <- 0x288  # Load Linked Address
CP0_WATCHLO_ADDR <- 0x290  # WatchLo (data/instruction break)
CP0_WATCHHI_ADDR <- 0x298  # WatchHi
CP0_XCONTEXT_ADDR <- 0x2A0  # Extended Context
CP0_PID_ADDR <- 0x2B0  # Tag/Process ID
CP0_DEBUG_ADDR <- 0x2D8  # Debug Register
CP0_PERF_ADDR <- 0x2F0  # Performance Counter

# 内存段定义
RDRAM_START <- 0x00000000
RDRAM_END <- 0x003FFFFF
RDRAM_SIZE <- 4194304  # RDRAM (4MB base, up to 8MB)
RDRAM_REG_START <- 0x18000000
RDRAM_REG_END <- 0x18000FFF
RDRAM_REG_SIZE <- 4096  # RDRAM Registers
SP_MEM_START <- 0x1FC00000
SP_MEM_END <- 0x1FC007FF
SP_MEM_SIZE <- 2048  # RCP SP Memory / DMEM (2KB)
SP_IMEM_START <- 0x1FC00800
SP_IMEM_END <- 0x1FC00FFF
SP_IMEM_SIZE <- 2048  # RCP SP Instruction Memory / IMEM (2KB)
RCP_REGS_START <- 0x1FC00000
RCP_REGS_END <- 0x1FC3FFFF
RCP_REGS_SIZE <- 262144  # RCP Register Area
PI_REGS_START <- 0x1FC00000
PI_REGS_END <- 0x1FC007FF
PI_REGS_SIZE <- 2048  # PI (Peripheral Interface) Registers
VI_REGS_START <- 0x1FC002C0
VI_REGS_END <- 0x1FC002FF
VI_REGS_SIZE <- 64  # VI (Video Interface) Registers
AI_REGS_START <- 0x1FC00500
AI_REGS_END <- 0x1FC0053F
AI_REGS_SIZE <- 64  # AI (Audio Interface) Registers
SI_REGS_START <- 0x1FC004C0
SI_REGS_END <- 0x1FC004FF
SI_REGS_SIZE <- 64  # SI (Serial Interface) Registers
PI_DRAM_START <- 0xA0000000
PI_DRAM_END <- 0xA4000000
PI_DRAM_SIZE <- 67108864  # PI Bus DRAM (cartridge)
CART_ROM_START <- 0xB0000000
CART_ROM_END <- 0xBFFFFFFF
CART_ROM_SIZE <- 268435456  # Cartridge ROM (up to 256MB)
PIF_RAM_START <- 0x1FC007C0
PIF_RAM_END <- 0x1FC007FF
PIF_RAM_SIZE <- 64  # PIF-NUS ROM/RAM (CIC)

# 外设定义
# Reality Signal Processor (Audio/Video microcode engine)
RSP_BASE <- 0x04040000
RSP_SP_MEM_ADDR_ADDR <- 0x00
RSP_SP_DRAM_ADDR_ADDR <- 0x04
RSP_SP_RD_LEN_ADDR <- 0x08
RSP_SP_WR_LEN_ADDR <- 0x0C
RSP_SP_STATUS_ADDR <- 0x10
RSP_SP_STATUS_BROKE_BIT <- 0  # Command Queue Broke
RSP_SP_STATUS_SLEEP_BIT <- 2  # SP Sleep
RSP_SP_STATUS_GOODMATCH_BIT <- 3  # DMEM/IMEM Goodmatch
RSP_SP_STATUS_SSTEP_BIT <- 4  # Single Step
RSP_SP_STATUS_INTSIG_BIT <- 5  # Interrupt Signal
RSP_SP_STATUS_HALT_BIT <- 6  # Halt
RSP_SP_STATUS_CLEAR_BIT <- 7  # Clear SP Status
RSP_SP_STATUS_INTR_BRK_BIT <- 8  # IntrOnBreak
RSP_SP_STATUS_SIGNAL0_BIT <- 12  # Software Signal 0
RSP_SP_STATUS_SIGNAL1_BIT <- 13  # Software Signal 1
RSP_SP_STATUS_SIGNAL2_BIT <- 14  # Software Signal 2
RSP_SP_STATUS_SIGNAL3_BIT <- 15  # Software Signal 3
RSP_SP_STATUS_SIGNAL4_BIT <- 16  # Software Signal 4
RSP_SP_STATUS_SIGNAL5_BIT <- 17  # Software Signal 5
RSP_SP_STATUS_SIGNAL6_BIT <- 18  # Software Signal 6
RSP_SP_STATUS_SIGNAL7_BIT <- 19  # Software Signal 7
RSP_SP_DMA_FULL_ADDR <- 0x14
RSP_SP_DMA_BUSY_ADDR <- 0x18
RSP_SP_SEMAPHORE_ADDR <- 0x1C
RSP_SP_PC_ADDR <- 0x20
RSP_SP_IBIST_ADDR <- 0x24
# Reality Drawing Processor (Triangle/Quad rasterizer)
RDP_BASE <- 0x04100000
RDP_DP_START_ADDR <- 0x00
RDP_DP_END_ADDR <- 0x04
RDP_DP_CURRENT_ADDR <- 0x08
RDP_DP_STATUS_ADDR <- 0x0C
RDP_DP_STATUS_TERMINATE_BIT <- 0  # Terminator
RDP_DP_STATUS_PIPE_BUSY_BIT <- 1  # Pipeline Busy
RDP_DP_STATUS_TOMINO_BUSY_BIT <- 2  # ToMini Busy
RDP_DP_STATUS_PIPE_FLUSH_BIT <- 3  # Pipeline Flush
RDP_DP_STATUS_TOMINO_FLUSH_BIT <- 4  # ToMini Flush
RDP_DP_STATUS_FREEZE_BIT <- 5  # Freeze
RDP_DP_STATUS_START_GCLK_BIT <- 24  # Start GCLK
RDP_DP_CLOCK_ADDR <- 0x10
RDP_DP_BUFBUSY_ADDR <- 0x14
RDP_DP_PIPEBUSY_ADDR <- 0x18
RDP_DP_TMEM_ADDR <- 0x1C
# Video Interface (scanout engine)
VI_BASE <- 0x04400000
VI_VI_STATUS_ADDR <- 0x00
VI_VI_STATUS_TYPE_BIT <- 0  # Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
VI_VI_STATUS_DITHER_FILTER_BIT <- 6  # Dither Filter Enable
VI_VI_STATUS_GAMMA_BIT <- 7  # Gamma Correction Enable
VI_VI_STATUS_GAMMA_DITHER_BIT <- 8  # Gamma Dither Enable
VI_VI_STATUS_DIVOT_BIT <- 9  # Divot Control
VI_VI_STATUS_SERRATION_BIT <- 10  #  Serration Enable (for interlaced)
VI_VI_ORIGIN_ADDR <- 0x04
VI_VI_WIDTH_ADDR <- 0x08
VI_VI_V_INTR_ADDR <- 0x0C
VI_VI_V_CURRENT_ADDR <- 0x10
VI_VI_BURST_ADDR <- 0x14
VI_VI_H_SYNC_ADDR <- 0x18
VI_VI_H_SYNC_LEAP_ADDR <- 0x1C
VI_VI_H_VIDEO_ADDR <- 0x20
VI_VI_V_VIDEO_ADDR <- 0x24
VI_VI_V_BURST_ADDR <- 0x28
VI_VI_X_SCALE_ADDR <- 0x2C
VI_VI_Y_SCALE_ADDR <- 0x30
# Audio Interface (DAC)
AI_BASE <- 0x04500000
AI_AI_DRAM_ADDR_ADDR <- 0x00
AI_AI_LEN_ADDR <- 0x04
AI_AI_CONTROL_ADDR <- 0x08
AI_AI_CONTROL_DMA_ENABLE_BIT <- 0  # DMA Enable
AI_AI_CONTROL_DMA_FIFO_FULL_BIT <- 1  # DMA FIFO Full
AI_AI_STATUS_ADDR <- 0x0C
AI_AI_DACRATE_ADDR <- 0x10
AI_AI_BITRATE_ADDR <- 0x14
# Peripheral Interface (cartridge bus)
PI_BASE <- 0x04600000
PI_PI_DRAM_ADDR_ADDR <- 0x00
PI_PI_CART_ADDR_ADDR <- 0x04
PI_PI_RD_LEN_ADDR <- 0x08
PI_PI_WR_LEN_ADDR <- 0x0C
PI_PI_STATUS_ADDR <- 0x10
PI_PI_STATUS_DMA_BUSY_BIT <- 0  # DMA Busy
PI_PI_STATUS_IO_BUSY_BIT <- 1  # I/O Busy
PI_PI_STATUS_ERROR_BIT <- 2  # Bus Error
PI_PI_BSD_DOM1_LAT_ADDR <- 0x14
PI_PI_BSD_DOM1_PWD_ADDR <- 0x18
PI_PI_BSD_DOM1_PGS_ADDR <- 0x1C
PI_PI_BSD_DOM1_RLS_ADDR <- 0x20
PI_PI_BSD_DOM2_LAT_ADDR <- 0x24
PI_PI_BSD_DOM2_PWD_ADDR <- 0x28
PI_PI_BSD_DOM2_PGS_ADDR <- 0x2C
PI_PI_BSD_DOM2_RLS_ADDR <- 0x30
# Serial Interface (Controller Pak / 64DD)
SI_BASE <- 0x04800000
SI_SI_DRAM_ADDR_ADDR <- 0x00
SI_SI_PIF_ADDR_RD64B_ADDR <- 0x04
SI_SI_PIF_ADDR_WR64B_ADDR <- 0x08
SI_SI_STATUS_ADDR <- 0x10
SI_SI_STATUS_DMA_BUSY_BIT <- 0  # DMA Busy
SI_SI_STATUS_IO_BUSY_BIT <- 1  # I/O Busy
SI_SI_STATUS_INTERRUPT_BIT <- 12  # SI Interrupt
# PIF (CIC / NUSYC - anti-piracy/copy protection)
PIF_BASE <- 0x1FC007C0
PIF_PIF_CMD0_ADDR <- 0x00
PIF_PIF_CMD1_ADDR <- 0x01
PIF_PIF_CMD2_ADDR <- 0x02
PIF_PIF_CMD3_ADDR <- 0x03
PIF_PIF_CMD4_ADDR <- 0x04
PIF_PIF_CMD5_ADDR <- 0x05
PIF_PIF_CMD6_ADDR <- 0x06
PIF_PIF_CMD7_ADDR <- 0x07
PIF_PIF_STATUS_ADDR <- 0x3F
# Interrupt Control
INTERRUPT_BASE <- 0x1FC00200
INTERRUPT_MI_MODE_ADDR <- 0x00
INTERRUPT_MI_MODE_INIT_MODE_BIT <- 0  # Initialize Mode
INTERRUPT_MI_MODE_EBUS_TEST_BIT <- 1  # EBUS Test Mode
INTERRUPT_MI_VERSION_ADDR <- 0x04
INTERRUPT_MI_INTR_ADDR <- 0x08
INTERRUPT_MI_INTR_SP_BIT <- 0  # SP Interrupt Pending
INTERRUPT_MI_INTR_SI_BIT <- 1  # SI Interrupt Pending
INTERRUPT_MI_INTR_AI_BIT <- 2  # AI Interrupt Pending
INTERRUPT_MI_INTR_VI_BIT <- 3  # VI Interrupt Pending
INTERRUPT_MI_INTR_PI_BIT <- 4  # PI Interrupt Pending
INTERRUPT_MI_INTR_DP_BIT <- 5  # DP Interrupt Pending
INTERRUPT_MI_INTR_MASK_ADDR <- 0x0C
INTERRUPT_MI_INTR_MASK_SP_MASK_BIT <- 0  # SP Interrupt Mask
INTERRUPT_MI_INTR_MASK_SI_MASK_BIT <- 1  # SI Interrupt Mask
INTERRUPT_MI_INTR_MASK_AI_MASK_BIT <- 2  # AI Interrupt Mask
INTERRUPT_MI_INTR_MASK_VI_MASK_BIT <- 3  # VI Interrupt Mask
INTERRUPT_MI_INTR_MASK_PI_MASK_BIT <- 4  # PI Interrupt Mask
INTERRUPT_MI_INTR_MASK_DP_MASK_BIT <- 5  # DP Interrupt Mask
# Controller Interface (SI channel 0-3)
CONTROLLER_BASE <- 0x1FC00600
CONTROLLER_SI_CH0_DATA_ADDR <- 0x00
CONTROLLER_SI_CH1_DATA_ADDR <- 0x08
CONTROLLER_SI_CH2_DATA_ADDR <- 0x10
CONTROLLER_SI_CH3_DATA_ADDR <- 0x18

# 中断向量定义
INT_RESET <- 0  # Soft Reset / NMI
INT_TLB_REFILL <- 1  # TLB Refill (I) / TLB Refill (D)
INT_CACHE_ERROR <- 2  # Cache Error
INT_GENERAL_EXCEPTION <- 3  # General Exception
INT_RSP <- 4  # RSP Interrupt (microcode signal)
INT_RDP <- 5  # RDP Interrupt (display list complete)
INT_VI <- 6  # VI Interrupt (V-Blank / scanline)
INT_AI <- 7  # AI Interrupt (audio DMA complete)
INT_PI <- 8  # PI Interrupt (cartridge DMA)
INT_SI <- 9  # SI Interrupt (serial interface)
INT_TIMER_COMPARE <- 10  # Timer Compare (CP0 Count == Compare)

# 引脚定义
PIN_VCC <- 1  # Power Supply (3.3V)
PIN_VSS <- 2  # Ground
PIN_CLK <- 3  # System Clock (93.75MHz from CIC/PLL)
PIN_RESET <- 4  # Reset (active low)
PIN_NMI <- 5  # Non-Maskable Interrupt
PIN_INT0 <- 6  # Interrupt 0 (RCP)
PIN_INT1 <- 7  # Interrupt 1 (cartridge)
PIN_INT2 <- 8  # Interrupt 2 (SI)
PIN_INT3 <- 9  # Interrupt 3 (PIF)
PIN_AB0 <- 10  # Address Bus Bit 0
PIN_AB1 <- 11  # Address Bus Bit 1
PIN_AB2 <- 12  # Address Bus Bit 2
PIN_AB3 <- 13  # Address Bus Bit 3
PIN_AB4 <- 14  # Address Bus Bit 4
PIN_AB5 <- 15  # Address Bus Bit 5
PIN_AB6 <- 16  # Address Bus Bit 6
PIN_AB7 <- 17  # Address Bus Bit 7
PIN_AB8 <- 18  # Address Bus Bit 8
PIN_AB9 <- 19  # Address Bus Bit 9
PIN_AB10 <- 20  # Address Bus Bit 10
PIN_AB11 <- 21  # Address Bus Bit 11
PIN_AB12 <- 22  # Address Bus Bit 12
PIN_AB13 <- 23  # Address Bus Bit 13
PIN_AB14 <- 24  # Address Bus Bit 14
PIN_AB15 <- 25  # Address Bus Bit 15
PIN_AB16 <- 26  # Address Bus Bit 16
PIN_AB17 <- 27  # Address Bus Bit 17
PIN_AB18 <- 28  # Address Bus Bit 18
PIN_AB19 <- 29  # Address Bus Bit 19
PIN_AB20 <- 30  # Address Bus Bit 20
PIN_AB21 <- 31  # Address Bus Bit 21
PIN_AB22 <- 32  # Address Bus Bit 22
PIN_AB23 <- 33  # Address Bus Bit 23
PIN_AB24 <- 34  # Address Bus Bit 24
PIN_AB25 <- 35  # Address Bus Bit 25
PIN_AB26 <- 36  # Address Bus Bit 26
PIN_AB27 <- 37  # Address Bus Bit 27
PIN_AB28 <- 38  # Address Bus Bit 28
PIN_AB29 <- 39  # Address Bus Bit 29
PIN_AB30 <- 40  # Address Bus Bit 30
PIN_AB31 <- 41  # Address Bus Bit 31
PIN_AB32 <- 42  # Address Bus Bit 32
PIN_AB33 <- 43  # Address Bus Bit 33
PIN_AB34 <- 44  # Address Bus Bit 34
PIN_AB35 <- 45  # Address Bus Bit 35
PIN_DB0 <- 46  # Data Bus Bit 0
PIN_DB1 <- 47  # Data Bus Bit 1
PIN_DB2 <- 48  # Data Bus Bit 2
PIN_DB3 <- 49  # Data Bus Bit 3
PIN_DB4 <- 50  # Data Bus Bit 4
PIN_DB5 <- 51  # Data Bus Bit 5
PIN_DB6 <- 52  # Data Bus Bit 6
PIN_DB7 <- 53  # Data Bus Bit 7
PIN_DB8 <- 54  # Data Bus Bit 8
PIN_DB9 <- 55  # Data Bus Bit 9
PIN_DB10 <- 56  # Data Bus Bit 10
PIN_DB11 <- 57  # Data Bus Bit 11
PIN_DB12 <- 58  # Data Bus Bit 12
PIN_DB13 <- 59  # Data Bus Bit 13
PIN_DB14 <- 60  # Data Bus Bit 14
PIN_DB15 <- 61  # Data Bus Bit 15
PIN_DB16 <- 62  # Data Bus Bit 16
PIN_DB17 <- 63  # Data Bus Bit 17
PIN_DB18 <- 64  # Data Bus Bit 18
PIN_DB19 <- 65  # Data Bus Bit 19
PIN_DB20 <- 66  # Data Bus Bit 20
PIN_DB21 <- 67  # Data Bus Bit 21
PIN_DB22 <- 68  # Data Bus Bit 22
PIN_DB23 <- 69  # Data Bus Bit 23
PIN_DB24 <- 70  # Data Bus Bit 24
PIN_DB25 <- 71  # Data Bus Bit 25
PIN_DB26 <- 72  # Data Bus Bit 26
PIN_DB27 <- 73  # Data Bus Bit 27
PIN_DB28 <- 74  # Data Bus Bit 28
PIN_DB29 <- 75  # Data Bus Bit 29
PIN_DB30 <- 76  # Data Bus Bit 30
PIN_DB31 <- 77  # Data Bus Bit 31
PIN_BE0 <- 78  # Byte Enable 0
PIN_BE1 <- 79  # Byte Enable 1
PIN_BE2 <- 80  # Byte Enable 2
PIN_BE3 <- 81  # Byte Enable 3
PIN_NCS0 <- 82  # Chip Select 0 (RDRAM)
PIN_NCS1 <- 83  # Chip Select 1 (RCP)
PIN_NCS2 <- 84  # Chip Select 2 (PIF ROM)
PIN_NCS3 <- 85  # Chip Select 3 (Cartridge)
PIN_NWR <- 86  # Write Enable
PIN_NRD <- 87  # Read Enable
PIN_EKN <- 88  # Audio DAC Data (I2S/EKN format)
PIN_AUDIO_L <- 89  # Audio Left Output
PIN_AUDIO_R <- 90  # Audio Right Output
PIN_VIDEO_R <- 91  # Video Output Red
PIN_VIDEO_G <- 92  # Video Output Green
PIN_VIDEO_B <- 93  # Video Output Blue
PIN_SYNC <- 94  # Video Sync

