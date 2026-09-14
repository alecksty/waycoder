// NEC-VR4300 设备定义 - Objective-C 头文件
// 生成自: NEC/MIPS-R4000/NEC-VR4300
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like
// CPU架构: MIPS-R4300i
// 位宽: 64位
// 时钟频率: 93750000 Hz

#ifndef NEC-VR4300_DEVICE_H
#define NEC-VR4300_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // Hard-wired Zero
#define R1_ADDR 0x08  // Assembler Temporary
#define R2_ADDR 0x10  // Value Return
#define R3_ADDR 0x18  // Expression Evaluation
#define R4_ADDR 0x20  // Expression Evaluation
#define R5_ADDR 0x28  // Expression Evaluation
#define R6_ADDR 0x30  // Expression Evaluation
#define R7_ADDR 0x38  // Expression Evaluation
#define R8_ADDR 0x40  // Expression Evaluation
#define R9_ADDR 0x48  // Expression Evaluation
#define R10_ADDR 0x50  // Expression Evaluation
#define R11_ADDR 0x58  // Expression Evaluation
#define R12_ADDR 0x60  // Expression Evaluation
#define R13_ADDR 0x68  // Expression Evaluation
#define R14_ADDR 0x70  // Expression Evaluation
#define R15_ADDR 0x78  // Expression Evaluation
#define R16_ADDR 0x80  // Saved Value
#define R17_ADDR 0x88  // Saved Value
#define R18_ADDR 0x90  // Saved Value
#define R19_ADDR 0x98  // Saved Value
#define R20_ADDR 0xA0  // Saved Value
#define R21_ADDR 0xA8  // Saved Value
#define R22_ADDR 0xB0  // Saved Value
#define R23_ADDR 0xB8  // Saved Value
#define R24_ADDR 0xC0  // Temporary
#define R25_ADDR 0xC8  // Temporary
#define R26_ADDR 0xD0  // Kernel Reserved
#define R27_ADDR 0xD8  // Kernel Reserved
#define R28_ADDR 0xE0  // Global Pointer
#define R29_ADDR 0xE8  // Stack Pointer
#define R30_ADDR 0xF0  // Frame Pointer
#define R31_ADDR 0xF8  // Return Address
#define HI_ADDR 0x100  // Multiply/Divide High (64-bit)
#define LO_ADDR 0x108  // Multiply/Divide Low (64-bit)
#define PC_ADDR 0x110  // Program Counter
#define LLB_ADDR 0x118  // LLAddr / LLBit (for LL/SC)
#define CP0_INDEX_ADDR 0x200  // TLB Index
#define CP0_RANDOM_ADDR 0x208  // TLB Random
#define CP0_ENTRYLO0_ADDR 0x210  // TLB EntryLo 0 (even page)
#define CP0_ENTRYLO1_ADDR 0x218  // TLB EntryLo 1 (odd page)
#define CP0_CONTEXT_ADDR 0x220  // Context Register (PTE base)
#define CP0_PAGEMASK_ADDR 0x228  // Page Mask (variable page size)
#define CP0_WIRED_ADDR 0x230  // TLB Wired
#define CP0_BADVADDR_ADDR 0x238  // Bad Virtual Address
#define CP0_COUNT_ADDR 0x240  // Count (incrementing timer)
#define CP0_ENTRYHI_ADDR 0x250  // TLB EntryHi (VPN2 + ASID)
#define CP0_COMPARE_ADDR 0x258  // Compare (timer interrupt)
#define CP0_STATUS_ADDR 0x260  // Status Register
#define CP0_STATUS_IE_BIT 0  // Interrupt Enable
#define CP0_STATUS_EXL_BIT 1  // Exception Level
#define CP0_STATUS_ERL_BIT 2  // Error Level
#define CP0_STATUS_KSU_BIT 0  // Kernel/User Mode
#define CP0_STATUS_UX_BIT 5  // User Mode 64-bit (1=64-bit user)
#define CP0_STATUS_SX_BIT 6  // Supervisor Mode 64-bit
#define CP0_STATUS_KX_BIT 7  // Kernel Mode 64-bit
#define CP0_STATUS_IM0_7_BIT 0  // Interrupt Mask
#define CP0_STATUS_CU0_BIT 28  // Coprocessor 0 Usable
#define CP0_STATUS_BEV_BIT 22  // Bootstrap Exception Vector
#define CP0_STATUS_TS_BIT 21  // TLB Shutdown
#define CP0_STATUS_FR_BIT 26  // Floating-Point Register Mode (32 double)
#define CP0_CAUSE_ADDR 0x268  // Cause Register
#define CP0_CAUSE_EXCCODE_BIT 0  // Exception Code
#define CP0_CAUSE_IP0_7_BIT 0  // Interrupt Pending
#define CP0_CAUSE_BD_BIT 31  // Branch Delay Slot
#define CP0_CAUSE_CE_BIT 0  // Coprocessor Error
#define CP0_EPC_ADDR 0x270  // Exception PC
#define CP0_CONFIG_ADDR 0x280  // Config Register
#define CP0_LLADDR_ADDR 0x288  // Load Linked Address
#define CP0_WATCHLO_ADDR 0x290  // WatchLo (data/instruction break)
#define CP0_WATCHHI_ADDR 0x298  // WatchHi
#define CP0_XCONTEXT_ADDR 0x2A0  // Extended Context
#define CP0_PID_ADDR 0x2B0  // Tag/Process ID
#define CP0_DEBUG_ADDR 0x2D8  // Debug Register
#define CP0_PERF_ADDR 0x2F0  // Performance Counter

// 内存段定义
#define RDRAM_START 0x00000000
#define RDRAM_END 0x003FFFFF
#define RDRAM_SIZE 4194304  // RDRAM (4MB base, up to 8MB)
#define RDRAM_REG_START 0x18000000
#define RDRAM_REG_END 0x18000FFF
#define RDRAM_REG_SIZE 4096  // RDRAM Registers
#define SP_MEM_START 0x1FC00000
#define SP_MEM_END 0x1FC007FF
#define SP_MEM_SIZE 2048  // RCP SP Memory / DMEM (2KB)
#define SP_IMEM_START 0x1FC00800
#define SP_IMEM_END 0x1FC00FFF
#define SP_IMEM_SIZE 2048  // RCP SP Instruction Memory / IMEM (2KB)
#define RCP_REGS_START 0x1FC00000
#define RCP_REGS_END 0x1FC3FFFF
#define RCP_REGS_SIZE 262144  // RCP Register Area
#define PI_REGS_START 0x1FC00000
#define PI_REGS_END 0x1FC007FF
#define PI_REGS_SIZE 2048  // PI (Peripheral Interface) Registers
#define VI_REGS_START 0x1FC002C0
#define VI_REGS_END 0x1FC002FF
#define VI_REGS_SIZE 64  // VI (Video Interface) Registers
#define AI_REGS_START 0x1FC00500
#define AI_REGS_END 0x1FC0053F
#define AI_REGS_SIZE 64  // AI (Audio Interface) Registers
#define SI_REGS_START 0x1FC004C0
#define SI_REGS_END 0x1FC004FF
#define SI_REGS_SIZE 64  // SI (Serial Interface) Registers
#define PI_DRAM_START 0xA0000000
#define PI_DRAM_END 0xA4000000
#define PI_DRAM_SIZE 67108864  // PI Bus DRAM (cartridge)
#define CART_ROM_START 0xB0000000
#define CART_ROM_END 0xBFFFFFFF
#define CART_ROM_SIZE 268435456  // Cartridge ROM (up to 256MB)
#define PIF_RAM_START 0x1FC007C0
#define PIF_RAM_END 0x1FC007FF
#define PIF_RAM_SIZE 64  // PIF-NUS ROM/RAM (CIC)

// 外设定义
// Reality Signal Processor (Audio/Video microcode engine)
#define RSP_BASE 0x04040000
#define RSP_SP_MEM_ADDR_ADDR 0x00
#define RSP_SP_DRAM_ADDR_ADDR 0x04
#define RSP_SP_RD_LEN_ADDR 0x08
#define RSP_SP_WR_LEN_ADDR 0x0C
#define RSP_SP_STATUS_ADDR 0x10
#define RSP_SP_STATUS_BROKE_BIT 0  // Command Queue Broke
#define RSP_SP_STATUS_SLEEP_BIT 2  // SP Sleep
#define RSP_SP_STATUS_GOODMATCH_BIT 3  // DMEM/IMEM Goodmatch
#define RSP_SP_STATUS_SSTEP_BIT 4  // Single Step
#define RSP_SP_STATUS_INTSIG_BIT 5  // Interrupt Signal
#define RSP_SP_STATUS_HALT_BIT 6  // Halt
#define RSP_SP_STATUS_CLEAR_BIT 7  // Clear SP Status
#define RSP_SP_STATUS_INTR_BRK_BIT 8  // IntrOnBreak
#define RSP_SP_STATUS_SIGNAL0_BIT 12  // Software Signal 0
#define RSP_SP_STATUS_SIGNAL1_BIT 13  // Software Signal 1
#define RSP_SP_STATUS_SIGNAL2_BIT 14  // Software Signal 2
#define RSP_SP_STATUS_SIGNAL3_BIT 15  // Software Signal 3
#define RSP_SP_STATUS_SIGNAL4_BIT 16  // Software Signal 4
#define RSP_SP_STATUS_SIGNAL5_BIT 17  // Software Signal 5
#define RSP_SP_STATUS_SIGNAL6_BIT 18  // Software Signal 6
#define RSP_SP_STATUS_SIGNAL7_BIT 19  // Software Signal 7
#define RSP_SP_DMA_FULL_ADDR 0x14
#define RSP_SP_DMA_BUSY_ADDR 0x18
#define RSP_SP_SEMAPHORE_ADDR 0x1C
#define RSP_SP_PC_ADDR 0x20
#define RSP_SP_IBIST_ADDR 0x24
// Reality Drawing Processor (Triangle/Quad rasterizer)
#define RDP_BASE 0x04100000
#define RDP_DP_START_ADDR 0x00
#define RDP_DP_END_ADDR 0x04
#define RDP_DP_CURRENT_ADDR 0x08
#define RDP_DP_STATUS_ADDR 0x0C
#define RDP_DP_STATUS_TERMINATE_BIT 0  // Terminator
#define RDP_DP_STATUS_PIPE_BUSY_BIT 1  // Pipeline Busy
#define RDP_DP_STATUS_TOMINO_BUSY_BIT 2  // ToMini Busy
#define RDP_DP_STATUS_PIPE_FLUSH_BIT 3  // Pipeline Flush
#define RDP_DP_STATUS_TOMINO_FLUSH_BIT 4  // ToMini Flush
#define RDP_DP_STATUS_FREEZE_BIT 5  // Freeze
#define RDP_DP_STATUS_START_GCLK_BIT 24  // Start GCLK
#define RDP_DP_CLOCK_ADDR 0x10
#define RDP_DP_BUFBUSY_ADDR 0x14
#define RDP_DP_PIPEBUSY_ADDR 0x18
#define RDP_DP_TMEM_ADDR 0x1C
// Video Interface (scanout engine)
#define VI_BASE 0x04400000
#define VI_VI_STATUS_ADDR 0x00
#define VI_VI_STATUS_TYPE_BIT 0  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
#define VI_VI_STATUS_DITHER_FILTER_BIT 6  // Dither Filter Enable
#define VI_VI_STATUS_GAMMA_BIT 7  // Gamma Correction Enable
#define VI_VI_STATUS_GAMMA_DITHER_BIT 8  // Gamma Dither Enable
#define VI_VI_STATUS_DIVOT_BIT 9  // Divot Control
#define VI_VI_STATUS_SERRATION_BIT 10  //  Serration Enable (for interlaced)
#define VI_VI_ORIGIN_ADDR 0x04
#define VI_VI_WIDTH_ADDR 0x08
#define VI_VI_V_INTR_ADDR 0x0C
#define VI_VI_V_CURRENT_ADDR 0x10
#define VI_VI_BURST_ADDR 0x14
#define VI_VI_H_SYNC_ADDR 0x18
#define VI_VI_H_SYNC_LEAP_ADDR 0x1C
#define VI_VI_H_VIDEO_ADDR 0x20
#define VI_VI_V_VIDEO_ADDR 0x24
#define VI_VI_V_BURST_ADDR 0x28
#define VI_VI_X_SCALE_ADDR 0x2C
#define VI_VI_Y_SCALE_ADDR 0x30
// Audio Interface (DAC)
#define AI_BASE 0x04500000
#define AI_AI_DRAM_ADDR_ADDR 0x00
#define AI_AI_LEN_ADDR 0x04
#define AI_AI_CONTROL_ADDR 0x08
#define AI_AI_CONTROL_DMA_ENABLE_BIT 0  // DMA Enable
#define AI_AI_CONTROL_DMA_FIFO_FULL_BIT 1  // DMA FIFO Full
#define AI_AI_STATUS_ADDR 0x0C
#define AI_AI_DACRATE_ADDR 0x10
#define AI_AI_BITRATE_ADDR 0x14
// Peripheral Interface (cartridge bus)
#define PI_BASE 0x04600000
#define PI_PI_DRAM_ADDR_ADDR 0x00
#define PI_PI_CART_ADDR_ADDR 0x04
#define PI_PI_RD_LEN_ADDR 0x08
#define PI_PI_WR_LEN_ADDR 0x0C
#define PI_PI_STATUS_ADDR 0x10
#define PI_PI_STATUS_DMA_BUSY_BIT 0  // DMA Busy
#define PI_PI_STATUS_IO_BUSY_BIT 1  // I/O Busy
#define PI_PI_STATUS_ERROR_BIT 2  // Bus Error
#define PI_PI_BSD_DOM1_LAT_ADDR 0x14
#define PI_PI_BSD_DOM1_PWD_ADDR 0x18
#define PI_PI_BSD_DOM1_PGS_ADDR 0x1C
#define PI_PI_BSD_DOM1_RLS_ADDR 0x20
#define PI_PI_BSD_DOM2_LAT_ADDR 0x24
#define PI_PI_BSD_DOM2_PWD_ADDR 0x28
#define PI_PI_BSD_DOM2_PGS_ADDR 0x2C
#define PI_PI_BSD_DOM2_RLS_ADDR 0x30
// Serial Interface (Controller Pak / 64DD)
#define SI_BASE 0x04800000
#define SI_SI_DRAM_ADDR_ADDR 0x00
#define SI_SI_PIF_ADDR_RD64B_ADDR 0x04
#define SI_SI_PIF_ADDR_WR64B_ADDR 0x08
#define SI_SI_STATUS_ADDR 0x10
#define SI_SI_STATUS_DMA_BUSY_BIT 0  // DMA Busy
#define SI_SI_STATUS_IO_BUSY_BIT 1  // I/O Busy
#define SI_SI_STATUS_INTERRUPT_BIT 12  // SI Interrupt
// PIF (CIC / NUSYC - anti-piracy/copy protection)
#define PIF_BASE 0x1FC007C0
#define PIF_PIF_CMD0_ADDR 0x00
#define PIF_PIF_CMD1_ADDR 0x01
#define PIF_PIF_CMD2_ADDR 0x02
#define PIF_PIF_CMD3_ADDR 0x03
#define PIF_PIF_CMD4_ADDR 0x04
#define PIF_PIF_CMD5_ADDR 0x05
#define PIF_PIF_CMD6_ADDR 0x06
#define PIF_PIF_CMD7_ADDR 0x07
#define PIF_PIF_STATUS_ADDR 0x3F
// Interrupt Control
#define INTERRUPT_BASE 0x1FC00200
#define INTERRUPT_MI_MODE_ADDR 0x00
#define INTERRUPT_MI_MODE_INIT_MODE_BIT 0  // Initialize Mode
#define INTERRUPT_MI_MODE_EBUS_TEST_BIT 1  // EBUS Test Mode
#define INTERRUPT_MI_VERSION_ADDR 0x04
#define INTERRUPT_MI_INTR_ADDR 0x08
#define INTERRUPT_MI_INTR_SP_BIT 0  // SP Interrupt Pending
#define INTERRUPT_MI_INTR_SI_BIT 1  // SI Interrupt Pending
#define INTERRUPT_MI_INTR_AI_BIT 2  // AI Interrupt Pending
#define INTERRUPT_MI_INTR_VI_BIT 3  // VI Interrupt Pending
#define INTERRUPT_MI_INTR_PI_BIT 4  // PI Interrupt Pending
#define INTERRUPT_MI_INTR_DP_BIT 5  // DP Interrupt Pending
#define INTERRUPT_MI_INTR_MASK_ADDR 0x0C
#define INTERRUPT_MI_INTR_MASK_SP_MASK_BIT 0  // SP Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_SI_MASK_BIT 1  // SI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_AI_MASK_BIT 2  // AI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_VI_MASK_BIT 3  // VI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_PI_MASK_BIT 4  // PI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_DP_MASK_BIT 5  // DP Interrupt Mask
// Controller Interface (SI channel 0-3)
#define CONTROLLER_BASE 0x1FC00600
#define CONTROLLER_SI_CH0_DATA_ADDR 0x00
#define CONTROLLER_SI_CH1_DATA_ADDR 0x08
#define CONTROLLER_SI_CH2_DATA_ADDR 0x10
#define CONTROLLER_SI_CH3_DATA_ADDR 0x18

// 中断向量定义
#define INT_RESET 0  // Soft Reset / NMI
#define INT_TLB_REFILL 1  // TLB Refill (I) / TLB Refill (D)
#define INT_CACHE_ERROR 2  // Cache Error
#define INT_GENERAL_EXCEPTION 3  // General Exception
#define INT_RSP 4  // RSP Interrupt (microcode signal)
#define INT_RDP 5  // RDP Interrupt (display list complete)
#define INT_VI 6  // VI Interrupt (V-Blank / scanline)
#define INT_AI 7  // AI Interrupt (audio DMA complete)
#define INT_PI 8  // PI Interrupt (cartridge DMA)
#define INT_SI 9  // SI Interrupt (serial interface)
#define INT_TIMER_COMPARE 10  // Timer Compare (CP0 Count == Compare)

// 引脚定义
#define PIN_VCC 1  // Power Supply (3.3V)
#define PIN_VSS 2  // Ground
#define PIN_CLK 3  // System Clock (93.75MHz from CIC/PLL)
#define PIN_RESET 4  // Reset (active low)
#define PIN_NMI 5  // Non-Maskable Interrupt
#define PIN_INT0 6  // Interrupt 0 (RCP)
#define PIN_INT1 7  // Interrupt 1 (cartridge)
#define PIN_INT2 8  // Interrupt 2 (SI)
#define PIN_INT3 9  // Interrupt 3 (PIF)
#define PIN_AB0 10  // Address Bus Bit 0
#define PIN_AB1 11  // Address Bus Bit 1
#define PIN_AB2 12  // Address Bus Bit 2
#define PIN_AB3 13  // Address Bus Bit 3
#define PIN_AB4 14  // Address Bus Bit 4
#define PIN_AB5 15  // Address Bus Bit 5
#define PIN_AB6 16  // Address Bus Bit 6
#define PIN_AB7 17  // Address Bus Bit 7
#define PIN_AB8 18  // Address Bus Bit 8
#define PIN_AB9 19  // Address Bus Bit 9
#define PIN_AB10 20  // Address Bus Bit 10
#define PIN_AB11 21  // Address Bus Bit 11
#define PIN_AB12 22  // Address Bus Bit 12
#define PIN_AB13 23  // Address Bus Bit 13
#define PIN_AB14 24  // Address Bus Bit 14
#define PIN_AB15 25  // Address Bus Bit 15
#define PIN_AB16 26  // Address Bus Bit 16
#define PIN_AB17 27  // Address Bus Bit 17
#define PIN_AB18 28  // Address Bus Bit 18
#define PIN_AB19 29  // Address Bus Bit 19
#define PIN_AB20 30  // Address Bus Bit 20
#define PIN_AB21 31  // Address Bus Bit 21
#define PIN_AB22 32  // Address Bus Bit 22
#define PIN_AB23 33  // Address Bus Bit 23
#define PIN_AB24 34  // Address Bus Bit 24
#define PIN_AB25 35  // Address Bus Bit 25
#define PIN_AB26 36  // Address Bus Bit 26
#define PIN_AB27 37  // Address Bus Bit 27
#define PIN_AB28 38  // Address Bus Bit 28
#define PIN_AB29 39  // Address Bus Bit 29
#define PIN_AB30 40  // Address Bus Bit 30
#define PIN_AB31 41  // Address Bus Bit 31
#define PIN_AB32 42  // Address Bus Bit 32
#define PIN_AB33 43  // Address Bus Bit 33
#define PIN_AB34 44  // Address Bus Bit 34
#define PIN_AB35 45  // Address Bus Bit 35
#define PIN_DB0 46  // Data Bus Bit 0
#define PIN_DB1 47  // Data Bus Bit 1
#define PIN_DB2 48  // Data Bus Bit 2
#define PIN_DB3 49  // Data Bus Bit 3
#define PIN_DB4 50  // Data Bus Bit 4
#define PIN_DB5 51  // Data Bus Bit 5
#define PIN_DB6 52  // Data Bus Bit 6
#define PIN_DB7 53  // Data Bus Bit 7
#define PIN_DB8 54  // Data Bus Bit 8
#define PIN_DB9 55  // Data Bus Bit 9
#define PIN_DB10 56  // Data Bus Bit 10
#define PIN_DB11 57  // Data Bus Bit 11
#define PIN_DB12 58  // Data Bus Bit 12
#define PIN_DB13 59  // Data Bus Bit 13
#define PIN_DB14 60  // Data Bus Bit 14
#define PIN_DB15 61  // Data Bus Bit 15
#define PIN_DB16 62  // Data Bus Bit 16
#define PIN_DB17 63  // Data Bus Bit 17
#define PIN_DB18 64  // Data Bus Bit 18
#define PIN_DB19 65  // Data Bus Bit 19
#define PIN_DB20 66  // Data Bus Bit 20
#define PIN_DB21 67  // Data Bus Bit 21
#define PIN_DB22 68  // Data Bus Bit 22
#define PIN_DB23 69  // Data Bus Bit 23
#define PIN_DB24 70  // Data Bus Bit 24
#define PIN_DB25 71  // Data Bus Bit 25
#define PIN_DB26 72  // Data Bus Bit 26
#define PIN_DB27 73  // Data Bus Bit 27
#define PIN_DB28 74  // Data Bus Bit 28
#define PIN_DB29 75  // Data Bus Bit 29
#define PIN_DB30 76  // Data Bus Bit 30
#define PIN_DB31 77  // Data Bus Bit 31
#define PIN_BE0 78  // Byte Enable 0
#define PIN_BE1 79  // Byte Enable 1
#define PIN_BE2 80  // Byte Enable 2
#define PIN_BE3 81  // Byte Enable 3
#define PIN_NCS0 82  // Chip Select 0 (RDRAM)
#define PIN_NCS1 83  // Chip Select 1 (RCP)
#define PIN_NCS2 84  // Chip Select 2 (PIF ROM)
#define PIN_NCS3 85  // Chip Select 3 (Cartridge)
#define PIN_NWR 86  // Write Enable
#define PIN_NRD 87  // Read Enable
#define PIN_EKN 88  // Audio DAC Data (I2S/EKN format)
#define PIN_AUDIO_L 89  // Audio Left Output
#define PIN_AUDIO_R 90  // Audio Right Output
#define PIN_VIDEO_R 91  // Video Output Red
#define PIN_VIDEO_G 92  // Video Output Green
#define PIN_VIDEO_B 93  // Video Output Blue
#define PIN_SYNC 94  // Video Sync

#endif /* NEC-VR4300_DEVICE_H */
