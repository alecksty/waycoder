using System;

namespace VML.Device.NEC.NEC_VR4300
{
    /// <summary>
    /// NEC-VR4300 寄存器定义
    /// 生成自: NEC/MIPS-R4000/NEC-VR4300
    /// 版本: 1.0
    /// </summary>
    public static class NEC_VR4300
    {
        // CPU架构: MIPS-R4300i, 64位, 93750000 Hz

        // 寄存器定义
        // Hard-wired Zero
        public const int R0_ADDR = 0x00;
        public static unsafe ulong* R0 => (ulong*)0x00;

        // Assembler Temporary
        public const int R1_ADDR = 0x08;
        public static unsafe ulong* R1 => (ulong*)0x08;

        // Value Return
        public const int R2_ADDR = 0x10;
        public static unsafe ulong* R2 => (ulong*)0x10;

        // Expression Evaluation
        public const int R3_ADDR = 0x18;
        public static unsafe ulong* R3 => (ulong*)0x18;

        // Expression Evaluation
        public const int R4_ADDR = 0x20;
        public static unsafe ulong* R4 => (ulong*)0x20;

        // Expression Evaluation
        public const int R5_ADDR = 0x28;
        public static unsafe ulong* R5 => (ulong*)0x28;

        // Expression Evaluation
        public const int R6_ADDR = 0x30;
        public static unsafe ulong* R6 => (ulong*)0x30;

        // Expression Evaluation
        public const int R7_ADDR = 0x38;
        public static unsafe ulong* R7 => (ulong*)0x38;

        // Expression Evaluation
        public const int R8_ADDR = 0x40;
        public static unsafe ulong* R8 => (ulong*)0x40;

        // Expression Evaluation
        public const int R9_ADDR = 0x48;
        public static unsafe ulong* R9 => (ulong*)0x48;

        // Expression Evaluation
        public const int R10_ADDR = 0x50;
        public static unsafe ulong* R10 => (ulong*)0x50;

        // Expression Evaluation
        public const int R11_ADDR = 0x58;
        public static unsafe ulong* R11 => (ulong*)0x58;

        // Expression Evaluation
        public const int R12_ADDR = 0x60;
        public static unsafe ulong* R12 => (ulong*)0x60;

        // Expression Evaluation
        public const int R13_ADDR = 0x68;
        public static unsafe ulong* R13 => (ulong*)0x68;

        // Expression Evaluation
        public const int R14_ADDR = 0x70;
        public static unsafe ulong* R14 => (ulong*)0x70;

        // Expression Evaluation
        public const int R15_ADDR = 0x78;
        public static unsafe ulong* R15 => (ulong*)0x78;

        // Saved Value
        public const int R16_ADDR = 0x80;
        public static unsafe ulong* R16 => (ulong*)0x80;

        // Saved Value
        public const int R17_ADDR = 0x88;
        public static unsafe ulong* R17 => (ulong*)0x88;

        // Saved Value
        public const int R18_ADDR = 0x90;
        public static unsafe ulong* R18 => (ulong*)0x90;

        // Saved Value
        public const int R19_ADDR = 0x98;
        public static unsafe ulong* R19 => (ulong*)0x98;

        // Saved Value
        public const int R20_ADDR = 0xA0;
        public static unsafe ulong* R20 => (ulong*)0xA0;

        // Saved Value
        public const int R21_ADDR = 0xA8;
        public static unsafe ulong* R21 => (ulong*)0xA8;

        // Saved Value
        public const int R22_ADDR = 0xB0;
        public static unsafe ulong* R22 => (ulong*)0xB0;

        // Saved Value
        public const int R23_ADDR = 0xB8;
        public static unsafe ulong* R23 => (ulong*)0xB8;

        // Temporary
        public const int R24_ADDR = 0xC0;
        public static unsafe ulong* R24 => (ulong*)0xC0;

        // Temporary
        public const int R25_ADDR = 0xC8;
        public static unsafe ulong* R25 => (ulong*)0xC8;

        // Kernel Reserved
        public const int R26_ADDR = 0xD0;
        public static unsafe ulong* R26 => (ulong*)0xD0;

        // Kernel Reserved
        public const int R27_ADDR = 0xD8;
        public static unsafe ulong* R27 => (ulong*)0xD8;

        // Global Pointer
        public const int R28_ADDR = 0xE0;
        public static unsafe ulong* R28 => (ulong*)0xE0;

        // Stack Pointer
        public const int R29_ADDR = 0xE8;
        public static unsafe ulong* R29 => (ulong*)0xE8;

        // Frame Pointer
        public const int R30_ADDR = 0xF0;
        public static unsafe ulong* R30 => (ulong*)0xF0;

        // Return Address
        public const int R31_ADDR = 0xF8;
        public static unsafe ulong* R31 => (ulong*)0xF8;

        // Multiply/Divide High (64-bit)
        public const int HI_ADDR = 0x100;
        public static unsafe ulong* HI => (ulong*)0x100;

        // Multiply/Divide Low (64-bit)
        public const int LO_ADDR = 0x108;
        public static unsafe ulong* LO => (ulong*)0x108;

        // Program Counter
        public const int PC_ADDR = 0x110;
        public static unsafe ulong* PC => (ulong*)0x110;

        // LLAddr / LLBit (for LL/SC)
        public const int LLB_ADDR = 0x118;
        public static unsafe ulong* LLB => (ulong*)0x118;

        // TLB Index
        public const int CP0_INDEX_ADDR = 0x200;
        public static unsafe ulong* CP0_INDEX => (ulong*)0x200;

        // TLB Random
        public const int CP0_RANDOM_ADDR = 0x208;
        public static unsafe ulong* CP0_RANDOM => (ulong*)0x208;

        // TLB EntryLo 0 (even page)
        public const int CP0_ENTRYLO0_ADDR = 0x210;
        public static unsafe ulong* CP0_ENTRYLO0 => (ulong*)0x210;

        // TLB EntryLo 1 (odd page)
        public const int CP0_ENTRYLO1_ADDR = 0x218;
        public static unsafe ulong* CP0_ENTRYLO1 => (ulong*)0x218;

        // Context Register (PTE base)
        public const int CP0_CONTEXT_ADDR = 0x220;
        public static unsafe ulong* CP0_CONTEXT => (ulong*)0x220;

        // Page Mask (variable page size)
        public const int CP0_PAGEMASK_ADDR = 0x228;
        public static unsafe ulong* CP0_PAGEMASK => (ulong*)0x228;

        // TLB Wired
        public const int CP0_WIRED_ADDR = 0x230;
        public static unsafe ulong* CP0_WIRED => (ulong*)0x230;

        // Bad Virtual Address
        public const int CP0_BADVADDR_ADDR = 0x238;
        public static unsafe ulong* CP0_BADVADDR => (ulong*)0x238;

        // Count (incrementing timer)
        public const int CP0_COUNT_ADDR = 0x240;
        public static unsafe ulong* CP0_COUNT => (ulong*)0x240;

        // TLB EntryHi (VPN2 + ASID)
        public const int CP0_ENTRYHI_ADDR = 0x250;
        public static unsafe ulong* CP0_ENTRYHI => (ulong*)0x250;

        // Compare (timer interrupt)
        public const int CP0_COMPARE_ADDR = 0x258;
        public static unsafe ulong* CP0_COMPARE => (ulong*)0x258;

        // Status Register
        public const int CP0_STATUS_ADDR = 0x260;
        public static unsafe ulong* CP0_STATUS => (ulong*)0x260;
        public const int CP0_STATUS_IE = 0;  // Interrupt Enable
        public const int CP0_STATUS_EXL = 1;  // Exception Level
        public const int CP0_STATUS_ERL = 2;  // Error Level
        public const int CP0_STATUS_KSU = 0;  // Kernel/User Mode
        public const int CP0_STATUS_UX = 5;  // User Mode 64-bit (1=64-bit user)
        public const int CP0_STATUS_SX = 6;  // Supervisor Mode 64-bit
        public const int CP0_STATUS_KX = 7;  // Kernel Mode 64-bit
        public const int CP0_STATUS_IM0_7 = 0;  // Interrupt Mask
        public const int CP0_STATUS_CU0 = 28;  // Coprocessor 0 Usable
        public const int CP0_STATUS_BEV = 22;  // Bootstrap Exception Vector
        public const int CP0_STATUS_TS = 21;  // TLB Shutdown
        public const int CP0_STATUS_FR = 26;  // Floating-Point Register Mode (32 double)

        // Cause Register
        public const int CP0_CAUSE_ADDR = 0x268;
        public static unsafe ulong* CP0_CAUSE => (ulong*)0x268;
        public const int CP0_CAUSE_EXCCODE = 0;  // Exception Code
        public const int CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending
        public const int CP0_CAUSE_BD = 31;  // Branch Delay Slot
        public const int CP0_CAUSE_CE = 0;  // Coprocessor Error

        // Exception PC
        public const int CP0_EPC_ADDR = 0x270;
        public static unsafe ulong* CP0_EPC => (ulong*)0x270;

        // Config Register
        public const int CP0_CONFIG_ADDR = 0x280;
        public static unsafe ulong* CP0_CONFIG => (ulong*)0x280;

        // Load Linked Address
        public const int CP0_LLADDR_ADDR = 0x288;
        public static unsafe ulong* CP0_LLADDR => (ulong*)0x288;

        // WatchLo (data/instruction break)
        public const int CP0_WATCHLO_ADDR = 0x290;
        public static unsafe ulong* CP0_WATCHLO => (ulong*)0x290;

        // WatchHi
        public const int CP0_WATCHHI_ADDR = 0x298;
        public static unsafe ulong* CP0_WATCHHI => (ulong*)0x298;

        // Extended Context
        public const int CP0_XCONTEXT_ADDR = 0x2A0;
        public static unsafe ulong* CP0_XCONTEXT => (ulong*)0x2A0;

        // Tag/Process ID
        public const int CP0_PID_ADDR = 0x2B0;
        public static unsafe ulong* CP0_PID => (ulong*)0x2B0;

        // Debug Register
        public const int CP0_DEBUG_ADDR = 0x2D8;
        public static unsafe ulong* CP0_DEBUG => (ulong*)0x2D8;

        // Performance Counter
        public const int CP0_PERF_ADDR = 0x2F0;
        public static unsafe ulong* CP0_PERF => (ulong*)0x2F0;

        // 内存段定义
        // RDRAM (4MB base, up to 8MB)
        public const int RDRAM_START = 0x00000000;
        public const int RDRAM_END = 0x003FFFFF;
        public const int RDRAM_SIZE = 4194304;

        // RDRAM Registers
        public const int RDRAM_REG_START = 0x18000000;
        public const int RDRAM_REG_END = 0x18000FFF;
        public const int RDRAM_REG_SIZE = 4096;

        // RCP SP Memory / DMEM (2KB)
        public const int SP_MEM_START = 0x1FC00000;
        public const int SP_MEM_END = 0x1FC007FF;
        public const int SP_MEM_SIZE = 2048;

        // RCP SP Instruction Memory / IMEM (2KB)
        public const int SP_IMEM_START = 0x1FC00800;
        public const int SP_IMEM_END = 0x1FC00FFF;
        public const int SP_IMEM_SIZE = 2048;

        // RCP Register Area
        public const int RCP_REGS_START = 0x1FC00000;
        public const int RCP_REGS_END = 0x1FC3FFFF;
        public const int RCP_REGS_SIZE = 262144;

        // PI (Peripheral Interface) Registers
        public const int PI_REGS_START = 0x1FC00000;
        public const int PI_REGS_END = 0x1FC007FF;
        public const int PI_REGS_SIZE = 2048;

        // VI (Video Interface) Registers
        public const int VI_REGS_START = 0x1FC002C0;
        public const int VI_REGS_END = 0x1FC002FF;
        public const int VI_REGS_SIZE = 64;

        // AI (Audio Interface) Registers
        public const int AI_REGS_START = 0x1FC00500;
        public const int AI_REGS_END = 0x1FC0053F;
        public const int AI_REGS_SIZE = 64;

        // SI (Serial Interface) Registers
        public const int SI_REGS_START = 0x1FC004C0;
        public const int SI_REGS_END = 0x1FC004FF;
        public const int SI_REGS_SIZE = 64;

        // PI Bus DRAM (cartridge)
        public const int PI_DRAM_START = 0xA0000000;
        public const int PI_DRAM_END = 0xA4000000;
        public const int PI_DRAM_SIZE = 67108864;

        // Cartridge ROM (up to 256MB)
        public const int CART_ROM_START = 0xB0000000;
        public const int CART_ROM_END = 0xBFFFFFFF;
        public const int CART_ROM_SIZE = 268435456;

        // PIF-NUS ROM/RAM (CIC)
        public const int PIF_RAM_START = 0x1FC007C0;
        public const int PIF_RAM_END = 0x1FC007FF;
        public const int PIF_RAM_SIZE = 64;

        // 外设定义
        // Reality Signal Processor (Audio/Video microcode engine)
        public const int RSP_BASE = 0x04040000;
        public static unsafe uint* RSP_SP_MEM_ADDR => (uint*)0x04040000;
        public static unsafe uint* RSP_SP_DRAM_ADDR => (uint*)0x04040004;
        public static unsafe uint* RSP_SP_RD_LEN => (uint*)0x04040008;
        public static unsafe uint* RSP_SP_WR_LEN => (uint*)0x0404000C;
        public static unsafe uint* RSP_SP_STATUS => (uint*)0x04040010;
        public const int RSP_SP_STATUS_BROKE = 0;  // Command Queue Broke
        public const int RSP_SP_STATUS_SLEEP = 2;  // SP Sleep
        public const int RSP_SP_STATUS_GOODMATCH = 3;  // DMEM/IMEM Goodmatch
        public const int RSP_SP_STATUS_SSTEP = 4;  // Single Step
        public const int RSP_SP_STATUS_INTSIG = 5;  // Interrupt Signal
        public const int RSP_SP_STATUS_HALT = 6;  // Halt
        public const int RSP_SP_STATUS_CLEAR = 7;  // Clear SP Status
        public const int RSP_SP_STATUS_INTR_BRK = 8;  // IntrOnBreak
        public const int RSP_SP_STATUS_SIGNAL0 = 12;  // Software Signal 0
        public const int RSP_SP_STATUS_SIGNAL1 = 13;  // Software Signal 1
        public const int RSP_SP_STATUS_SIGNAL2 = 14;  // Software Signal 2
        public const int RSP_SP_STATUS_SIGNAL3 = 15;  // Software Signal 3
        public const int RSP_SP_STATUS_SIGNAL4 = 16;  // Software Signal 4
        public const int RSP_SP_STATUS_SIGNAL5 = 17;  // Software Signal 5
        public const int RSP_SP_STATUS_SIGNAL6 = 18;  // Software Signal 6
        public const int RSP_SP_STATUS_SIGNAL7 = 19;  // Software Signal 7
        public static unsafe uint* RSP_SP_DMA_FULL => (uint*)0x04040014;
        public static unsafe uint* RSP_SP_DMA_BUSY => (uint*)0x04040018;
        public static unsafe uint* RSP_SP_SEMAPHORE => (uint*)0x0404001C;
        public static unsafe uint* RSP_SP_PC => (uint*)0x04040020;
        public static unsafe uint* RSP_SP_IBIST => (uint*)0x04040024;

        // Reality Drawing Processor (Triangle/Quad rasterizer)
        public const int RDP_BASE = 0x04100000;
        public static unsafe uint* RDP_DP_START => (uint*)0x04100000;
        public static unsafe uint* RDP_DP_END => (uint*)0x04100004;
        public static unsafe uint* RDP_DP_CURRENT => (uint*)0x04100008;
        public static unsafe uint* RDP_DP_STATUS => (uint*)0x0410000C;
        public const int RDP_DP_STATUS_TERMINATE = 0;  // Terminator
        public const int RDP_DP_STATUS_PIPE_BUSY = 1;  // Pipeline Busy
        public const int RDP_DP_STATUS_TOMINO_BUSY = 2;  // ToMini Busy
        public const int RDP_DP_STATUS_PIPE_FLUSH = 3;  // Pipeline Flush
        public const int RDP_DP_STATUS_TOMINO_FLUSH = 4;  // ToMini Flush
        public const int RDP_DP_STATUS_FREEZE = 5;  // Freeze
        public const int RDP_DP_STATUS_START_GCLK = 24;  // Start GCLK
        public static unsafe uint* RDP_DP_CLOCK => (uint*)0x04100010;
        public static unsafe uint* RDP_DP_BUFBUSY => (uint*)0x04100014;
        public static unsafe uint* RDP_DP_PIPEBUSY => (uint*)0x04100018;
        public static unsafe uint* RDP_DP_TMEM => (uint*)0x0410001C;

        // Video Interface (scanout engine)
        public const int VI_BASE = 0x04400000;
        public static unsafe uint* VI_VI_STATUS => (uint*)0x04400000;
        public const int VI_VI_STATUS_TYPE = 0;  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
        public const int VI_VI_STATUS_DITHER_FILTER = 6;  // Dither Filter Enable
        public const int VI_VI_STATUS_GAMMA = 7;  // Gamma Correction Enable
        public const int VI_VI_STATUS_GAMMA_DITHER = 8;  // Gamma Dither Enable
        public const int VI_VI_STATUS_DIVOT = 9;  // Divot Control
        public const int VI_VI_STATUS_SERRATION = 10;  //  Serration Enable (for interlaced)
        public static unsafe uint* VI_VI_ORIGIN => (uint*)0x04400004;
        public static unsafe uint* VI_VI_WIDTH => (uint*)0x04400008;
        public static unsafe uint* VI_VI_V_INTR => (uint*)0x0440000C;
        public static unsafe uint* VI_VI_V_CURRENT => (uint*)0x04400010;
        public static unsafe uint* VI_VI_BURST => (uint*)0x04400014;
        public static unsafe uint* VI_VI_H_SYNC => (uint*)0x04400018;
        public static unsafe uint* VI_VI_H_SYNC_LEAP => (uint*)0x0440001C;
        public static unsafe uint* VI_VI_H_VIDEO => (uint*)0x04400020;
        public static unsafe uint* VI_VI_V_VIDEO => (uint*)0x04400024;
        public static unsafe uint* VI_VI_V_BURST => (uint*)0x04400028;
        public static unsafe uint* VI_VI_X_SCALE => (uint*)0x0440002C;
        public static unsafe uint* VI_VI_Y_SCALE => (uint*)0x04400030;

        // Audio Interface (DAC)
        public const int AI_BASE = 0x04500000;
        public static unsafe uint* AI_AI_DRAM_ADDR => (uint*)0x04500000;
        public static unsafe uint* AI_AI_LEN => (uint*)0x04500004;
        public static unsafe uint* AI_AI_CONTROL => (uint*)0x04500008;
        public const int AI_AI_CONTROL_DMA_ENABLE = 0;  // DMA Enable
        public const int AI_AI_CONTROL_DMA_FIFO_FULL = 1;  // DMA FIFO Full
        public static unsafe uint* AI_AI_STATUS => (uint*)0x0450000C;
        public static unsafe uint* AI_AI_DACRATE => (uint*)0x04500010;
        public static unsafe uint* AI_AI_BITRATE => (uint*)0x04500014;

        // Peripheral Interface (cartridge bus)
        public const int PI_BASE = 0x04600000;
        public static unsafe uint* PI_PI_DRAM_ADDR => (uint*)0x04600000;
        public static unsafe uint* PI_PI_CART_ADDR => (uint*)0x04600004;
        public static unsafe uint* PI_PI_RD_LEN => (uint*)0x04600008;
        public static unsafe uint* PI_PI_WR_LEN => (uint*)0x0460000C;
        public static unsafe uint* PI_PI_STATUS => (uint*)0x04600010;
        public const int PI_PI_STATUS_DMA_BUSY = 0;  // DMA Busy
        public const int PI_PI_STATUS_IO_BUSY = 1;  // I/O Busy
        public const int PI_PI_STATUS_ERROR = 2;  // Bus Error
        public static unsafe uint* PI_PI_BSD_DOM1_LAT => (uint*)0x04600014;
        public static unsafe uint* PI_PI_BSD_DOM1_PWD => (uint*)0x04600018;
        public static unsafe uint* PI_PI_BSD_DOM1_PGS => (uint*)0x0460001C;
        public static unsafe uint* PI_PI_BSD_DOM1_RLS => (uint*)0x04600020;
        public static unsafe uint* PI_PI_BSD_DOM2_LAT => (uint*)0x04600024;
        public static unsafe uint* PI_PI_BSD_DOM2_PWD => (uint*)0x04600028;
        public static unsafe uint* PI_PI_BSD_DOM2_PGS => (uint*)0x0460002C;
        public static unsafe uint* PI_PI_BSD_DOM2_RLS => (uint*)0x04600030;

        // Serial Interface (Controller Pak / 64DD)
        public const int SI_BASE = 0x04800000;
        public static unsafe uint* SI_SI_DRAM_ADDR => (uint*)0x04800000;
        public static unsafe uint* SI_SI_PIF_ADDR_RD64B => (uint*)0x04800004;
        public static unsafe uint* SI_SI_PIF_ADDR_WR64B => (uint*)0x04800008;
        public static unsafe uint* SI_SI_STATUS => (uint*)0x04800010;
        public const int SI_SI_STATUS_DMA_BUSY = 0;  // DMA Busy
        public const int SI_SI_STATUS_IO_BUSY = 1;  // I/O Busy
        public const int SI_SI_STATUS_INTERRUPT = 12;  // SI Interrupt

        // PIF (CIC / NUSYC - anti-piracy/copy protection)
        public const int PIF_BASE = 0x1FC007C0;
        public static unsafe byte* PIF_PIF_CMD0 => (byte*)0x1FC007C0;
        public static unsafe byte* PIF_PIF_CMD1 => (byte*)0x1FC007C1;
        public static unsafe byte* PIF_PIF_CMD2 => (byte*)0x1FC007C2;
        public static unsafe byte* PIF_PIF_CMD3 => (byte*)0x1FC007C3;
        public static unsafe byte* PIF_PIF_CMD4 => (byte*)0x1FC007C4;
        public static unsafe byte* PIF_PIF_CMD5 => (byte*)0x1FC007C5;
        public static unsafe byte* PIF_PIF_CMD6 => (byte*)0x1FC007C6;
        public static unsafe byte* PIF_PIF_CMD7 => (byte*)0x1FC007C7;
        public static unsafe byte* PIF_PIF_STATUS => (byte*)0x1FC007FF;

        // Interrupt Control
        public const int INTERRUPT_BASE = 0x1FC00200;
        public static unsafe uint* INTERRUPT_MI_MODE => (uint*)0x1FC00200;
        public const int INTERRUPT_MI_MODE_INIT_MODE = 0;  // Initialize Mode
        public const int INTERRUPT_MI_MODE_EBUS_TEST = 1;  // EBUS Test Mode
        public static unsafe uint* INTERRUPT_MI_VERSION => (uint*)0x1FC00204;
        public static unsafe uint* INTERRUPT_MI_INTR => (uint*)0x1FC00208;
        public const int INTERRUPT_MI_INTR_SP = 0;  // SP Interrupt Pending
        public const int INTERRUPT_MI_INTR_SI = 1;  // SI Interrupt Pending
        public const int INTERRUPT_MI_INTR_AI = 2;  // AI Interrupt Pending
        public const int INTERRUPT_MI_INTR_VI = 3;  // VI Interrupt Pending
        public const int INTERRUPT_MI_INTR_PI = 4;  // PI Interrupt Pending
        public const int INTERRUPT_MI_INTR_DP = 5;  // DP Interrupt Pending
        public static unsafe uint* INTERRUPT_MI_INTR_MASK => (uint*)0x1FC0020C;
        public const int INTERRUPT_MI_INTR_MASK_SP_MASK = 0;  // SP Interrupt Mask
        public const int INTERRUPT_MI_INTR_MASK_SI_MASK = 1;  // SI Interrupt Mask
        public const int INTERRUPT_MI_INTR_MASK_AI_MASK = 2;  // AI Interrupt Mask
        public const int INTERRUPT_MI_INTR_MASK_VI_MASK = 3;  // VI Interrupt Mask
        public const int INTERRUPT_MI_INTR_MASK_PI_MASK = 4;  // PI Interrupt Mask
        public const int INTERRUPT_MI_INTR_MASK_DP_MASK = 5;  // DP Interrupt Mask

        // Controller Interface (SI channel 0-3)
        public const int CONTROLLER_BASE = 0x1FC00600;
        public static unsafe ulong* CONTROLLER_SI_CH0_DATA => (ulong*)0x1FC00600;
        public static unsafe ulong* CONTROLLER_SI_CH1_DATA => (ulong*)0x1FC00608;
        public static unsafe ulong* CONTROLLER_SI_CH2_DATA => (ulong*)0x1FC00610;
        public static unsafe ulong* CONTROLLER_SI_CH3_DATA => (ulong*)0x1FC00618;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Soft Reset / NMI
        public const int IRQ_TLB_REFILL = 1;  // TLB Refill (I) / TLB Refill (D)
        public const int IRQ_CACHE_ERROR = 2;  // Cache Error
        public const int IRQ_GENERAL_EXCEPTION = 3;  // General Exception
        public const int IRQ_RSP = 4;  // RSP Interrupt (microcode signal)
        public const int IRQ_RDP = 5;  // RDP Interrupt (display list complete)
        public const int IRQ_VI = 6;  // VI Interrupt (V-Blank / scanline)
        public const int IRQ_AI = 7;  // AI Interrupt (audio DMA complete)
        public const int IRQ_PI = 8;  // PI Interrupt (cartridge DMA)
        public const int IRQ_SI = 9;  // SI Interrupt (serial interface)
        public const int IRQ_TIMER_COMPARE = 10;  // Timer Compare (CP0 Count == Compare)

        // 引脚定义
        public const int PIN_VCC = 1;  // Power Supply (3.3V)
        public const int PIN_VSS = 2;  // Ground
        public const int PIN_CLK = 3;  // System Clock (93.75MHz from CIC/PLL)
        public const int PIN_RESET = 4;  // Reset (active low)
        public const int PIN_NMI = 5;  // Non-Maskable Interrupt
        public const int PIN_INT0 = 6;  // Interrupt 0 (RCP)
        public const int PIN_INT1 = 7;  // Interrupt 1 (cartridge)
        public const int PIN_INT2 = 8;  // Interrupt 2 (SI)
        public const int PIN_INT3 = 9;  // Interrupt 3 (PIF)
        public const int PIN_AB0 = 10;  // Address Bus Bit 0
        public const int PIN_AB1 = 11;  // Address Bus Bit 1
        public const int PIN_AB2 = 12;  // Address Bus Bit 2
        public const int PIN_AB3 = 13;  // Address Bus Bit 3
        public const int PIN_AB4 = 14;  // Address Bus Bit 4
        public const int PIN_AB5 = 15;  // Address Bus Bit 5
        public const int PIN_AB6 = 16;  // Address Bus Bit 6
        public const int PIN_AB7 = 17;  // Address Bus Bit 7
        public const int PIN_AB8 = 18;  // Address Bus Bit 8
        public const int PIN_AB9 = 19;  // Address Bus Bit 9
        public const int PIN_AB10 = 20;  // Address Bus Bit 10
        public const int PIN_AB11 = 21;  // Address Bus Bit 11
        public const int PIN_AB12 = 22;  // Address Bus Bit 12
        public const int PIN_AB13 = 23;  // Address Bus Bit 13
        public const int PIN_AB14 = 24;  // Address Bus Bit 14
        public const int PIN_AB15 = 25;  // Address Bus Bit 15
        public const int PIN_AB16 = 26;  // Address Bus Bit 16
        public const int PIN_AB17 = 27;  // Address Bus Bit 17
        public const int PIN_AB18 = 28;  // Address Bus Bit 18
        public const int PIN_AB19 = 29;  // Address Bus Bit 19
        public const int PIN_AB20 = 30;  // Address Bus Bit 20
        public const int PIN_AB21 = 31;  // Address Bus Bit 21
        public const int PIN_AB22 = 32;  // Address Bus Bit 22
        public const int PIN_AB23 = 33;  // Address Bus Bit 23
        public const int PIN_AB24 = 34;  // Address Bus Bit 24
        public const int PIN_AB25 = 35;  // Address Bus Bit 25
        public const int PIN_AB26 = 36;  // Address Bus Bit 26
        public const int PIN_AB27 = 37;  // Address Bus Bit 27
        public const int PIN_AB28 = 38;  // Address Bus Bit 28
        public const int PIN_AB29 = 39;  // Address Bus Bit 29
        public const int PIN_AB30 = 40;  // Address Bus Bit 30
        public const int PIN_AB31 = 41;  // Address Bus Bit 31
        public const int PIN_AB32 = 42;  // Address Bus Bit 32
        public const int PIN_AB33 = 43;  // Address Bus Bit 33
        public const int PIN_AB34 = 44;  // Address Bus Bit 34
        public const int PIN_AB35 = 45;  // Address Bus Bit 35
        public const int PIN_DB0 = 46;  // Data Bus Bit 0
        public const int PIN_DB1 = 47;  // Data Bus Bit 1
        public const int PIN_DB2 = 48;  // Data Bus Bit 2
        public const int PIN_DB3 = 49;  // Data Bus Bit 3
        public const int PIN_DB4 = 50;  // Data Bus Bit 4
        public const int PIN_DB5 = 51;  // Data Bus Bit 5
        public const int PIN_DB6 = 52;  // Data Bus Bit 6
        public const int PIN_DB7 = 53;  // Data Bus Bit 7
        public const int PIN_DB8 = 54;  // Data Bus Bit 8
        public const int PIN_DB9 = 55;  // Data Bus Bit 9
        public const int PIN_DB10 = 56;  // Data Bus Bit 10
        public const int PIN_DB11 = 57;  // Data Bus Bit 11
        public const int PIN_DB12 = 58;  // Data Bus Bit 12
        public const int PIN_DB13 = 59;  // Data Bus Bit 13
        public const int PIN_DB14 = 60;  // Data Bus Bit 14
        public const int PIN_DB15 = 61;  // Data Bus Bit 15
        public const int PIN_DB16 = 62;  // Data Bus Bit 16
        public const int PIN_DB17 = 63;  // Data Bus Bit 17
        public const int PIN_DB18 = 64;  // Data Bus Bit 18
        public const int PIN_DB19 = 65;  // Data Bus Bit 19
        public const int PIN_DB20 = 66;  // Data Bus Bit 20
        public const int PIN_DB21 = 67;  // Data Bus Bit 21
        public const int PIN_DB22 = 68;  // Data Bus Bit 22
        public const int PIN_DB23 = 69;  // Data Bus Bit 23
        public const int PIN_DB24 = 70;  // Data Bus Bit 24
        public const int PIN_DB25 = 71;  // Data Bus Bit 25
        public const int PIN_DB26 = 72;  // Data Bus Bit 26
        public const int PIN_DB27 = 73;  // Data Bus Bit 27
        public const int PIN_DB28 = 74;  // Data Bus Bit 28
        public const int PIN_DB29 = 75;  // Data Bus Bit 29
        public const int PIN_DB30 = 76;  // Data Bus Bit 30
        public const int PIN_DB31 = 77;  // Data Bus Bit 31
        public const int PIN_BE0 = 78;  // Byte Enable 0
        public const int PIN_BE1 = 79;  // Byte Enable 1
        public const int PIN_BE2 = 80;  // Byte Enable 2
        public const int PIN_BE3 = 81;  // Byte Enable 3
        public const int PIN_NCS0 = 82;  // Chip Select 0 (RDRAM)
        public const int PIN_NCS1 = 83;  // Chip Select 1 (RCP)
        public const int PIN_NCS2 = 84;  // Chip Select 2 (PIF ROM)
        public const int PIN_NCS3 = 85;  // Chip Select 3 (Cartridge)
        public const int PIN_NWR = 86;  // Write Enable
        public const int PIN_NRD = 87;  // Read Enable
        public const int PIN_EKN = 88;  // Audio DAC Data (I2S/EKN format)
        public const int PIN_AUDIO_L = 89;  // Audio Left Output
        public const int PIN_AUDIO_R = 90;  // Audio Right Output
        public const int PIN_VIDEO_R = 91;  // Video Output Red
        public const int PIN_VIDEO_G = 92;  // Video Output Green
        public const int PIN_VIDEO_B = 93;  // Video Output Blue
        public const int PIN_SYNC = 94;  // Video Sync

        public static void nec_vr4300_init()
        {
            // 硬件初始化代码
        }
    }
}
