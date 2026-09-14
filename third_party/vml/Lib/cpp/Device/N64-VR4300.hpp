#ifndef NEC_VR4300_HPP
#define NEC_VR4300_HPP

// NEC-VR4300寄存器定义
// 生成自: NEC/MIPS-R4000/NEC-VR4300
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: MIPS-R4300i
// 位宽: 64位
// 时钟频率: 93750000 Hz

// 寄存器定义
// Hard-wired Zero
#define R0 (*(volatile uint64_t*)0x00)

// Assembler Temporary
#define R1 (*(volatile uint64_t*)0x08)

// Value Return
#define R2 (*(volatile uint64_t*)0x10)

// Expression Evaluation
#define R3 (*(volatile uint64_t*)0x18)

// Expression Evaluation
#define R4 (*(volatile uint64_t*)0x20)

// Expression Evaluation
#define R5 (*(volatile uint64_t*)0x28)

// Expression Evaluation
#define R6 (*(volatile uint64_t*)0x30)

// Expression Evaluation
#define R7 (*(volatile uint64_t*)0x38)

// Expression Evaluation
#define R8 (*(volatile uint64_t*)0x40)

// Expression Evaluation
#define R9 (*(volatile uint64_t*)0x48)

// Expression Evaluation
#define R10 (*(volatile uint64_t*)0x50)

// Expression Evaluation
#define R11 (*(volatile uint64_t*)0x58)

// Expression Evaluation
#define R12 (*(volatile uint64_t*)0x60)

// Expression Evaluation
#define R13 (*(volatile uint64_t*)0x68)

// Expression Evaluation
#define R14 (*(volatile uint64_t*)0x70)

// Expression Evaluation
#define R15 (*(volatile uint64_t*)0x78)

// Saved Value
#define R16 (*(volatile uint64_t*)0x80)

// Saved Value
#define R17 (*(volatile uint64_t*)0x88)

// Saved Value
#define R18 (*(volatile uint64_t*)0x90)

// Saved Value
#define R19 (*(volatile uint64_t*)0x98)

// Saved Value
#define R20 (*(volatile uint64_t*)0xA0)

// Saved Value
#define R21 (*(volatile uint64_t*)0xA8)

// Saved Value
#define R22 (*(volatile uint64_t*)0xB0)

// Saved Value
#define R23 (*(volatile uint64_t*)0xB8)

// Temporary
#define R24 (*(volatile uint64_t*)0xC0)

// Temporary
#define R25 (*(volatile uint64_t*)0xC8)

// Kernel Reserved
#define R26 (*(volatile uint64_t*)0xD0)

// Kernel Reserved
#define R27 (*(volatile uint64_t*)0xD8)

// Global Pointer
#define R28 (*(volatile uint64_t*)0xE0)

// Stack Pointer
#define R29 (*(volatile uint64_t*)0xE8)

// Frame Pointer
#define R30 (*(volatile uint64_t*)0xF0)

// Return Address
#define R31 (*(volatile uint64_t*)0xF8)

// Multiply/Divide High (64-bit)
#define HI (*(volatile uint64_t*)0x100)

// Multiply/Divide Low (64-bit)
#define LO (*(volatile uint64_t*)0x108)

// Program Counter
#define PC (*(volatile uint64_t*)0x110)

// LLAddr / LLBit (for LL/SC)
#define LLB (*(volatile uint64_t*)0x118)

// TLB Index
#define CP0_INDEX (*(volatile uint64_t*)0x200)

// TLB Random
#define CP0_RANDOM (*(volatile uint64_t*)0x208)

// TLB EntryLo 0 (even page)
#define CP0_ENTRYLO0 (*(volatile uint64_t*)0x210)

// TLB EntryLo 1 (odd page)
#define CP0_ENTRYLO1 (*(volatile uint64_t*)0x218)

// Context Register (PTE base)
#define CP0_CONTEXT (*(volatile uint64_t*)0x220)

// Page Mask (variable page size)
#define CP0_PAGEMASK (*(volatile uint64_t*)0x228)

// TLB Wired
#define CP0_WIRED (*(volatile uint64_t*)0x230)

// Bad Virtual Address
#define CP0_BADVADDR (*(volatile uint64_t*)0x238)

// Count (incrementing timer)
#define CP0_COUNT (*(volatile uint64_t*)0x240)

// TLB EntryHi (VPN2 + ASID)
#define CP0_ENTRYHI (*(volatile uint64_t*)0x250)

// Compare (timer interrupt)
#define CP0_COMPARE (*(volatile uint64_t*)0x258)

// Status Register
#define CP0_STATUS (*(volatile uint64_t*)0x260)
#define CP0_STATUS_IE 0  // Interrupt Enable
#define CP0_STATUS_EXL 1  // Exception Level
#define CP0_STATUS_ERL 2  // Error Level
#define CP0_STATUS_KSU 0  // Kernel/User Mode
#define CP0_STATUS_UX 5  // User Mode 64-bit (1=64-bit user)
#define CP0_STATUS_SX 6  // Supervisor Mode 64-bit
#define CP0_STATUS_KX 7  // Kernel Mode 64-bit
#define CP0_STATUS_IM0_7 0  // Interrupt Mask
#define CP0_STATUS_CU0 28  // Coprocessor 0 Usable
#define CP0_STATUS_BEV 22  // Bootstrap Exception Vector
#define CP0_STATUS_TS 21  // TLB Shutdown
#define CP0_STATUS_FR 26  // Floating-Point Register Mode (32 double)

// Cause Register
#define CP0_CAUSE (*(volatile uint64_t*)0x268)
#define CP0_CAUSE_EXCCODE 0  // Exception Code
#define CP0_CAUSE_IP0_7 0  // Interrupt Pending
#define CP0_CAUSE_BD 31  // Branch Delay Slot
#define CP0_CAUSE_CE 0  // Coprocessor Error

// Exception PC
#define CP0_EPC (*(volatile uint64_t*)0x270)

// Config Register
#define CP0_CONFIG (*(volatile uint64_t*)0x280)

// Load Linked Address
#define CP0_LLADDR (*(volatile uint64_t*)0x288)

// WatchLo (data/instruction break)
#define CP0_WATCHLO (*(volatile uint64_t*)0x290)

// WatchHi
#define CP0_WATCHHI (*(volatile uint64_t*)0x298)

// Extended Context
#define CP0_XCONTEXT (*(volatile uint64_t*)0x2A0)

// Tag/Process ID
#define CP0_PID (*(volatile uint64_t*)0x2B0)

// Debug Register
#define CP0_DEBUG (*(volatile uint64_t*)0x2D8)

// Performance Counter
#define CP0_PERF (*(volatile uint64_t*)0x2F0)

// 内存段定义
// RDRAM (4MB base, up to 8MB)
#define RDRAM_START 0x00000000
#define RDRAM_END 0x003FFFFF
#define RDRAM_SIZE 4194304

// RDRAM Registers
#define RDRAM_REG_START 0x18000000
#define RDRAM_REG_END 0x18000FFF
#define RDRAM_REG_SIZE 4096

// RCP SP Memory / DMEM (2KB)
#define SP_MEM_START 0x1FC00000
#define SP_MEM_END 0x1FC007FF
#define SP_MEM_SIZE 2048

// RCP SP Instruction Memory / IMEM (2KB)
#define SP_IMEM_START 0x1FC00800
#define SP_IMEM_END 0x1FC00FFF
#define SP_IMEM_SIZE 2048

// RCP Register Area
#define RCP_REGS_START 0x1FC00000
#define RCP_REGS_END 0x1FC3FFFF
#define RCP_REGS_SIZE 262144

// PI (Peripheral Interface) Registers
#define PI_REGS_START 0x1FC00000
#define PI_REGS_END 0x1FC007FF
#define PI_REGS_SIZE 2048

// VI (Video Interface) Registers
#define VI_REGS_START 0x1FC002C0
#define VI_REGS_END 0x1FC002FF
#define VI_REGS_SIZE 64

// AI (Audio Interface) Registers
#define AI_REGS_START 0x1FC00500
#define AI_REGS_END 0x1FC0053F
#define AI_REGS_SIZE 64

// SI (Serial Interface) Registers
#define SI_REGS_START 0x1FC004C0
#define SI_REGS_END 0x1FC004FF
#define SI_REGS_SIZE 64

// PI Bus DRAM (cartridge)
#define PI_DRAM_START 0xA0000000
#define PI_DRAM_END 0xA4000000
#define PI_DRAM_SIZE 67108864

// Cartridge ROM (up to 256MB)
#define CART_ROM_START 0xB0000000
#define CART_ROM_END 0xBFFFFFFF
#define CART_ROM_SIZE 268435456

// PIF-NUS ROM/RAM (CIC)
#define PIF_RAM_START 0x1FC007C0
#define PIF_RAM_END 0x1FC007FF
#define PIF_RAM_SIZE 64

// 外设定义
// Reality Signal Processor (Audio/Video microcode engine)
#define RSP_BASE 0x04040000
#define RSP_SP_MEM_ADDR (*(volatile uint32_t*)0x04040000)
#define RSP_SP_DRAM_ADDR (*(volatile uint32_t*)0x04040004)
#define RSP_SP_RD_LEN (*(volatile uint32_t*)0x04040008)
#define RSP_SP_WR_LEN (*(volatile uint32_t*)0x0404000C)
#define RSP_SP_STATUS (*(volatile uint32_t*)0x04040010)
#define RSP_SP_STATUS_BROKE 0  // Command Queue Broke
#define RSP_SP_STATUS_SLEEP 2  // SP Sleep
#define RSP_SP_STATUS_GOODMATCH 3  // DMEM/IMEM Goodmatch
#define RSP_SP_STATUS_SSTEP 4  // Single Step
#define RSP_SP_STATUS_INTSIG 5  // Interrupt Signal
#define RSP_SP_STATUS_HALT 6  // Halt
#define RSP_SP_STATUS_CLEAR 7  // Clear SP Status
#define RSP_SP_STATUS_INTR_BRK 8  // IntrOnBreak
#define RSP_SP_STATUS_SIGNAL0 12  // Software Signal 0
#define RSP_SP_STATUS_SIGNAL1 13  // Software Signal 1
#define RSP_SP_STATUS_SIGNAL2 14  // Software Signal 2
#define RSP_SP_STATUS_SIGNAL3 15  // Software Signal 3
#define RSP_SP_STATUS_SIGNAL4 16  // Software Signal 4
#define RSP_SP_STATUS_SIGNAL5 17  // Software Signal 5
#define RSP_SP_STATUS_SIGNAL6 18  // Software Signal 6
#define RSP_SP_STATUS_SIGNAL7 19  // Software Signal 7
#define RSP_SP_DMA_FULL (*(volatile uint32_t*)0x04040014)
#define RSP_SP_DMA_BUSY (*(volatile uint32_t*)0x04040018)
#define RSP_SP_SEMAPHORE (*(volatile uint32_t*)0x0404001C)
#define RSP_SP_PC (*(volatile uint32_t*)0x04040020)
#define RSP_SP_IBIST (*(volatile uint32_t*)0x04040024)

// Reality Drawing Processor (Triangle/Quad rasterizer)
#define RDP_BASE 0x04100000
#define RDP_DP_START (*(volatile uint32_t*)0x04100000)
#define RDP_DP_END (*(volatile uint32_t*)0x04100004)
#define RDP_DP_CURRENT (*(volatile uint32_t*)0x04100008)
#define RDP_DP_STATUS (*(volatile uint32_t*)0x0410000C)
#define RDP_DP_STATUS_TERMINATE 0  // Terminator
#define RDP_DP_STATUS_PIPE_BUSY 1  // Pipeline Busy
#define RDP_DP_STATUS_TOMINO_BUSY 2  // ToMini Busy
#define RDP_DP_STATUS_PIPE_FLUSH 3  // Pipeline Flush
#define RDP_DP_STATUS_TOMINO_FLUSH 4  // ToMini Flush
#define RDP_DP_STATUS_FREEZE 5  // Freeze
#define RDP_DP_STATUS_START_GCLK 24  // Start GCLK
#define RDP_DP_CLOCK (*(volatile uint32_t*)0x04100010)
#define RDP_DP_BUFBUSY (*(volatile uint32_t*)0x04100014)
#define RDP_DP_PIPEBUSY (*(volatile uint32_t*)0x04100018)
#define RDP_DP_TMEM (*(volatile uint32_t*)0x0410001C)

// Video Interface (scanout engine)
#define VI_BASE 0x04400000
#define VI_VI_STATUS (*(volatile uint32_t*)0x04400000)
#define VI_VI_STATUS_TYPE 0  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
#define VI_VI_STATUS_DITHER_FILTER 6  // Dither Filter Enable
#define VI_VI_STATUS_GAMMA 7  // Gamma Correction Enable
#define VI_VI_STATUS_GAMMA_DITHER 8  // Gamma Dither Enable
#define VI_VI_STATUS_DIVOT 9  // Divot Control
#define VI_VI_STATUS_SERRATION 10  //  Serration Enable (for interlaced)
#define VI_VI_ORIGIN (*(volatile uint32_t*)0x04400004)
#define VI_VI_WIDTH (*(volatile uint32_t*)0x04400008)
#define VI_VI_V_INTR (*(volatile uint32_t*)0x0440000C)
#define VI_VI_V_CURRENT (*(volatile uint32_t*)0x04400010)
#define VI_VI_BURST (*(volatile uint32_t*)0x04400014)
#define VI_VI_H_SYNC (*(volatile uint32_t*)0x04400018)
#define VI_VI_H_SYNC_LEAP (*(volatile uint32_t*)0x0440001C)
#define VI_VI_H_VIDEO (*(volatile uint32_t*)0x04400020)
#define VI_VI_V_VIDEO (*(volatile uint32_t*)0x04400024)
#define VI_VI_V_BURST (*(volatile uint32_t*)0x04400028)
#define VI_VI_X_SCALE (*(volatile uint32_t*)0x0440002C)
#define VI_VI_Y_SCALE (*(volatile uint32_t*)0x04400030)

// Audio Interface (DAC)
#define AI_BASE 0x04500000
#define AI_AI_DRAM_ADDR (*(volatile uint32_t*)0x04500000)
#define AI_AI_LEN (*(volatile uint32_t*)0x04500004)
#define AI_AI_CONTROL (*(volatile uint32_t*)0x04500008)
#define AI_AI_CONTROL_DMA_ENABLE 0  // DMA Enable
#define AI_AI_CONTROL_DMA_FIFO_FULL 1  // DMA FIFO Full
#define AI_AI_STATUS (*(volatile uint32_t*)0x0450000C)
#define AI_AI_DACRATE (*(volatile uint32_t*)0x04500010)
#define AI_AI_BITRATE (*(volatile uint32_t*)0x04500014)

// Peripheral Interface (cartridge bus)
#define PI_BASE 0x04600000
#define PI_PI_DRAM_ADDR (*(volatile uint32_t*)0x04600000)
#define PI_PI_CART_ADDR (*(volatile uint32_t*)0x04600004)
#define PI_PI_RD_LEN (*(volatile uint32_t*)0x04600008)
#define PI_PI_WR_LEN (*(volatile uint32_t*)0x0460000C)
#define PI_PI_STATUS (*(volatile uint32_t*)0x04600010)
#define PI_PI_STATUS_DMA_BUSY 0  // DMA Busy
#define PI_PI_STATUS_IO_BUSY 1  // I/O Busy
#define PI_PI_STATUS_ERROR 2  // Bus Error
#define PI_PI_BSD_DOM1_LAT (*(volatile uint32_t*)0x04600014)
#define PI_PI_BSD_DOM1_PWD (*(volatile uint32_t*)0x04600018)
#define PI_PI_BSD_DOM1_PGS (*(volatile uint32_t*)0x0460001C)
#define PI_PI_BSD_DOM1_RLS (*(volatile uint32_t*)0x04600020)
#define PI_PI_BSD_DOM2_LAT (*(volatile uint32_t*)0x04600024)
#define PI_PI_BSD_DOM2_PWD (*(volatile uint32_t*)0x04600028)
#define PI_PI_BSD_DOM2_PGS (*(volatile uint32_t*)0x0460002C)
#define PI_PI_BSD_DOM2_RLS (*(volatile uint32_t*)0x04600030)

// Serial Interface (Controller Pak / 64DD)
#define SI_BASE 0x04800000
#define SI_SI_DRAM_ADDR (*(volatile uint32_t*)0x04800000)
#define SI_SI_PIF_ADDR_RD64B (*(volatile uint32_t*)0x04800004)
#define SI_SI_PIF_ADDR_WR64B (*(volatile uint32_t*)0x04800008)
#define SI_SI_STATUS (*(volatile uint32_t*)0x04800010)
#define SI_SI_STATUS_DMA_BUSY 0  // DMA Busy
#define SI_SI_STATUS_IO_BUSY 1  // I/O Busy
#define SI_SI_STATUS_INTERRUPT 12  // SI Interrupt

// PIF (CIC / NUSYC - anti-piracy/copy protection)
#define PIF_BASE 0x1FC007C0
#define PIF_PIF_CMD0 (*(volatile uint8_t*)0x1FC007C0)
#define PIF_PIF_CMD1 (*(volatile uint8_t*)0x1FC007C1)
#define PIF_PIF_CMD2 (*(volatile uint8_t*)0x1FC007C2)
#define PIF_PIF_CMD3 (*(volatile uint8_t*)0x1FC007C3)
#define PIF_PIF_CMD4 (*(volatile uint8_t*)0x1FC007C4)
#define PIF_PIF_CMD5 (*(volatile uint8_t*)0x1FC007C5)
#define PIF_PIF_CMD6 (*(volatile uint8_t*)0x1FC007C6)
#define PIF_PIF_CMD7 (*(volatile uint8_t*)0x1FC007C7)
#define PIF_PIF_STATUS (*(volatile uint8_t*)0x1FC007FF)

// Interrupt Control
#define INTERRUPT_BASE 0x1FC00200
#define INTERRUPT_MI_MODE (*(volatile uint32_t*)0x1FC00200)
#define INTERRUPT_MI_MODE_INIT_MODE 0  // Initialize Mode
#define INTERRUPT_MI_MODE_EBUS_TEST 1  // EBUS Test Mode
#define INTERRUPT_MI_VERSION (*(volatile uint32_t*)0x1FC00204)
#define INTERRUPT_MI_INTR (*(volatile uint32_t*)0x1FC00208)
#define INTERRUPT_MI_INTR_SP 0  // SP Interrupt Pending
#define INTERRUPT_MI_INTR_SI 1  // SI Interrupt Pending
#define INTERRUPT_MI_INTR_AI 2  // AI Interrupt Pending
#define INTERRUPT_MI_INTR_VI 3  // VI Interrupt Pending
#define INTERRUPT_MI_INTR_PI 4  // PI Interrupt Pending
#define INTERRUPT_MI_INTR_DP 5  // DP Interrupt Pending
#define INTERRUPT_MI_INTR_MASK (*(volatile uint32_t*)0x1FC0020C)
#define INTERRUPT_MI_INTR_MASK_SP_MASK 0  // SP Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_SI_MASK 1  // SI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_AI_MASK 2  // AI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_VI_MASK 3  // VI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_PI_MASK 4  // PI Interrupt Mask
#define INTERRUPT_MI_INTR_MASK_DP_MASK 5  // DP Interrupt Mask

// Controller Interface (SI channel 0-3)
#define CONTROLLER_BASE 0x1FC00600
#define CONTROLLER_SI_CH0_DATA (*(volatile uint64_t*)0x1FC00600)
#define CONTROLLER_SI_CH1_DATA (*(volatile uint64_t*)0x1FC00608)
#define CONTROLLER_SI_CH2_DATA (*(volatile uint64_t*)0x1FC00610)
#define CONTROLLER_SI_CH3_DATA (*(volatile uint64_t*)0x1FC00618)

// 中断向量定义
#define RESET_VECTOR 0  // Soft Reset / NMI
#define TLB_REFILL_VECTOR 1  // TLB Refill (I) / TLB Refill (D)
#define CACHE_ERROR_VECTOR 2  // Cache Error
#define GENERAL_EXCEPTION_VECTOR 3  // General Exception
#define RSP_VECTOR 4  // RSP Interrupt (microcode signal)
#define RDP_VECTOR 5  // RDP Interrupt (display list complete)
#define VI_VECTOR 6  // VI Interrupt (V-Blank / scanline)
#define AI_VECTOR 7  // AI Interrupt (audio DMA complete)
#define PI_VECTOR 8  // PI Interrupt (cartridge DMA)
#define SI_VECTOR 9  // SI Interrupt (serial interface)
#define TIMER_COMPARE_VECTOR 10  // Timer Compare (CP0 Count == Compare)

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

void nec_vr4300_init(void);

#ifdef __cplusplus
}
#endif

#endif // NEC_VR4300_HPP
