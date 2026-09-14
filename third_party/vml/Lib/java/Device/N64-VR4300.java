package vml.device.nec.nec_vr4300;

/**
 * NEC-VR4300 寄存器定义
 * 生成自: NEC/MIPS-R4000/NEC-VR4300
 * 版本: 1.0
 */
public final class NEC_VR4300 {
    private NEC_VR4300() {} // 工具类
    // CPU架构: MIPS-R4300i, 64位, 93750000 Hz

    // 寄存器定义
    // Hard-wired Zero
    public static final int R0_ADDR = (int)0x00;

    // Assembler Temporary
    public static final int R1_ADDR = (int)0x08;

    // Value Return
    public static final int R2_ADDR = (int)0x10;

    // Expression Evaluation
    public static final int R3_ADDR = (int)0x18;

    // Expression Evaluation
    public static final int R4_ADDR = (int)0x20;

    // Expression Evaluation
    public static final int R5_ADDR = (int)0x28;

    // Expression Evaluation
    public static final int R6_ADDR = (int)0x30;

    // Expression Evaluation
    public static final int R7_ADDR = (int)0x38;

    // Expression Evaluation
    public static final int R8_ADDR = (int)0x40;

    // Expression Evaluation
    public static final int R9_ADDR = (int)0x48;

    // Expression Evaluation
    public static final int R10_ADDR = (int)0x50;

    // Expression Evaluation
    public static final int R11_ADDR = (int)0x58;

    // Expression Evaluation
    public static final int R12_ADDR = (int)0x60;

    // Expression Evaluation
    public static final int R13_ADDR = (int)0x68;

    // Expression Evaluation
    public static final int R14_ADDR = (int)0x70;

    // Expression Evaluation
    public static final int R15_ADDR = (int)0x78;

    // Saved Value
    public static final int R16_ADDR = (int)0x80;

    // Saved Value
    public static final int R17_ADDR = (int)0x88;

    // Saved Value
    public static final int R18_ADDR = (int)0x90;

    // Saved Value
    public static final int R19_ADDR = (int)0x98;

    // Saved Value
    public static final int R20_ADDR = (int)0xA0;

    // Saved Value
    public static final int R21_ADDR = (int)0xA8;

    // Saved Value
    public static final int R22_ADDR = (int)0xB0;

    // Saved Value
    public static final int R23_ADDR = (int)0xB8;

    // Temporary
    public static final int R24_ADDR = (int)0xC0;

    // Temporary
    public static final int R25_ADDR = (int)0xC8;

    // Kernel Reserved
    public static final int R26_ADDR = (int)0xD0;

    // Kernel Reserved
    public static final int R27_ADDR = (int)0xD8;

    // Global Pointer
    public static final int R28_ADDR = (int)0xE0;

    // Stack Pointer
    public static final int R29_ADDR = (int)0xE8;

    // Frame Pointer
    public static final int R30_ADDR = (int)0xF0;

    // Return Address
    public static final int R31_ADDR = (int)0xF8;

    // Multiply/Divide High (64-bit)
    public static final int HI_ADDR = (int)0x100;

    // Multiply/Divide Low (64-bit)
    public static final int LO_ADDR = (int)0x108;

    // Program Counter
    public static final int PC_ADDR = (int)0x110;

    // LLAddr / LLBit (for LL/SC)
    public static final int LLB_ADDR = (int)0x118;

    // TLB Index
    public static final int CP0_INDEX_ADDR = (int)0x200;

    // TLB Random
    public static final int CP0_RANDOM_ADDR = (int)0x208;

    // TLB EntryLo 0 (even page)
    public static final int CP0_ENTRYLO0_ADDR = (int)0x210;

    // TLB EntryLo 1 (odd page)
    public static final int CP0_ENTRYLO1_ADDR = (int)0x218;

    // Context Register (PTE base)
    public static final int CP0_CONTEXT_ADDR = (int)0x220;

    // Page Mask (variable page size)
    public static final int CP0_PAGEMASK_ADDR = (int)0x228;

    // TLB Wired
    public static final int CP0_WIRED_ADDR = (int)0x230;

    // Bad Virtual Address
    public static final int CP0_BADVADDR_ADDR = (int)0x238;

    // Count (incrementing timer)
    public static final int CP0_COUNT_ADDR = (int)0x240;

    // TLB EntryHi (VPN2 + ASID)
    public static final int CP0_ENTRYHI_ADDR = (int)0x250;

    // Compare (timer interrupt)
    public static final int CP0_COMPARE_ADDR = (int)0x258;

    // Status Register
    public static final int CP0_STATUS_ADDR = (int)0x260;
    public static final int CP0_STATUS_IE = 0;  // Interrupt Enable
    public static final int CP0_STATUS_EXL = 1;  // Exception Level
    public static final int CP0_STATUS_ERL = 2;  // Error Level
    public static final int CP0_STATUS_KSU = 0;  // Kernel/User Mode
    public static final int CP0_STATUS_UX = 5;  // User Mode 64-bit (1=64-bit user)
    public static final int CP0_STATUS_SX = 6;  // Supervisor Mode 64-bit
    public static final int CP0_STATUS_KX = 7;  // Kernel Mode 64-bit
    public static final int CP0_STATUS_IM0_7 = 0;  // Interrupt Mask
    public static final int CP0_STATUS_CU0 = 28;  // Coprocessor 0 Usable
    public static final int CP0_STATUS_BEV = 22;  // Bootstrap Exception Vector
    public static final int CP0_STATUS_TS = 21;  // TLB Shutdown
    public static final int CP0_STATUS_FR = 26;  // Floating-Point Register Mode (32 double)

    // Cause Register
    public static final int CP0_CAUSE_ADDR = (int)0x268;
    public static final int CP0_CAUSE_EXCCODE = 0;  // Exception Code
    public static final int CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending
    public static final int CP0_CAUSE_BD = 31;  // Branch Delay Slot
    public static final int CP0_CAUSE_CE = 0;  // Coprocessor Error

    // Exception PC
    public static final int CP0_EPC_ADDR = (int)0x270;

    // Config Register
    public static final int CP0_CONFIG_ADDR = (int)0x280;

    // Load Linked Address
    public static final int CP0_LLADDR_ADDR = (int)0x288;

    // WatchLo (data/instruction break)
    public static final int CP0_WATCHLO_ADDR = (int)0x290;

    // WatchHi
    public static final int CP0_WATCHHI_ADDR = (int)0x298;

    // Extended Context
    public static final int CP0_XCONTEXT_ADDR = (int)0x2A0;

    // Tag/Process ID
    public static final int CP0_PID_ADDR = (int)0x2B0;

    // Debug Register
    public static final int CP0_DEBUG_ADDR = (int)0x2D8;

    // Performance Counter
    public static final int CP0_PERF_ADDR = (int)0x2F0;

    // 内存段定义
    // RDRAM (4MB base, up to 8MB)
    public static final int RDRAM_START = (int)0x00000000;
    public static final int RDRAM_END = (int)0x003FFFFF;
    public static final int RDRAM_SIZE = 4194304;

    // RDRAM Registers
    public static final int RDRAM_REG_START = (int)0x18000000;
    public static final int RDRAM_REG_END = (int)0x18000FFF;
    public static final int RDRAM_REG_SIZE = 4096;

    // RCP SP Memory / DMEM (2KB)
    public static final int SP_MEM_START = (int)0x1FC00000;
    public static final int SP_MEM_END = (int)0x1FC007FF;
    public static final int SP_MEM_SIZE = 2048;

    // RCP SP Instruction Memory / IMEM (2KB)
    public static final int SP_IMEM_START = (int)0x1FC00800;
    public static final int SP_IMEM_END = (int)0x1FC00FFF;
    public static final int SP_IMEM_SIZE = 2048;

    // RCP Register Area
    public static final int RCP_REGS_START = (int)0x1FC00000;
    public static final int RCP_REGS_END = (int)0x1FC3FFFF;
    public static final int RCP_REGS_SIZE = 262144;

    // PI (Peripheral Interface) Registers
    public static final int PI_REGS_START = (int)0x1FC00000;
    public static final int PI_REGS_END = (int)0x1FC007FF;
    public static final int PI_REGS_SIZE = 2048;

    // VI (Video Interface) Registers
    public static final int VI_REGS_START = (int)0x1FC002C0;
    public static final int VI_REGS_END = (int)0x1FC002FF;
    public static final int VI_REGS_SIZE = 64;

    // AI (Audio Interface) Registers
    public static final int AI_REGS_START = (int)0x1FC00500;
    public static final int AI_REGS_END = (int)0x1FC0053F;
    public static final int AI_REGS_SIZE = 64;

    // SI (Serial Interface) Registers
    public static final int SI_REGS_START = (int)0x1FC004C0;
    public static final int SI_REGS_END = (int)0x1FC004FF;
    public static final int SI_REGS_SIZE = 64;

    // PI Bus DRAM (cartridge)
    public static final int PI_DRAM_START = (int)0xA0000000;
    public static final int PI_DRAM_END = (int)0xA4000000;
    public static final int PI_DRAM_SIZE = 67108864;

    // Cartridge ROM (up to 256MB)
    public static final int CART_ROM_START = (int)0xB0000000;
    public static final int CART_ROM_END = (int)0xBFFFFFFF;
    public static final int CART_ROM_SIZE = 268435456;

    // PIF-NUS ROM/RAM (CIC)
    public static final int PIF_RAM_START = (int)0x1FC007C0;
    public static final int PIF_RAM_END = (int)0x1FC007FF;
    public static final int PIF_RAM_SIZE = 64;

    // 外设定义
    // Reality Signal Processor (Audio/Video microcode engine)
    public static final int RSP_BASE = (int)0x04040000;
    public static final int RSP_SP_MEM_ADDR = (int)0x04040000;
    public static final int RSP_SP_DRAM_ADDR = (int)0x04040004;
    public static final int RSP_SP_RD_LEN = (int)0x04040008;
    public static final int RSP_SP_WR_LEN = (int)0x0404000C;
    public static final int RSP_SP_STATUS = (int)0x04040010;
    public static final int RSP_SP_STATUS_BROKE = 0;  // Command Queue Broke
    public static final int RSP_SP_STATUS_SLEEP = 2;  // SP Sleep
    public static final int RSP_SP_STATUS_GOODMATCH = 3;  // DMEM/IMEM Goodmatch
    public static final int RSP_SP_STATUS_SSTEP = 4;  // Single Step
    public static final int RSP_SP_STATUS_INTSIG = 5;  // Interrupt Signal
    public static final int RSP_SP_STATUS_HALT = 6;  // Halt
    public static final int RSP_SP_STATUS_CLEAR = 7;  // Clear SP Status
    public static final int RSP_SP_STATUS_INTR_BRK = 8;  // IntrOnBreak
    public static final int RSP_SP_STATUS_SIGNAL0 = 12;  // Software Signal 0
    public static final int RSP_SP_STATUS_SIGNAL1 = 13;  // Software Signal 1
    public static final int RSP_SP_STATUS_SIGNAL2 = 14;  // Software Signal 2
    public static final int RSP_SP_STATUS_SIGNAL3 = 15;  // Software Signal 3
    public static final int RSP_SP_STATUS_SIGNAL4 = 16;  // Software Signal 4
    public static final int RSP_SP_STATUS_SIGNAL5 = 17;  // Software Signal 5
    public static final int RSP_SP_STATUS_SIGNAL6 = 18;  // Software Signal 6
    public static final int RSP_SP_STATUS_SIGNAL7 = 19;  // Software Signal 7
    public static final int RSP_SP_DMA_FULL = (int)0x04040014;
    public static final int RSP_SP_DMA_BUSY = (int)0x04040018;
    public static final int RSP_SP_SEMAPHORE = (int)0x0404001C;
    public static final int RSP_SP_PC = (int)0x04040020;
    public static final int RSP_SP_IBIST = (int)0x04040024;

    // Reality Drawing Processor (Triangle/Quad rasterizer)
    public static final int RDP_BASE = (int)0x04100000;
    public static final int RDP_DP_START = (int)0x04100000;
    public static final int RDP_DP_END = (int)0x04100004;
    public static final int RDP_DP_CURRENT = (int)0x04100008;
    public static final int RDP_DP_STATUS = (int)0x0410000C;
    public static final int RDP_DP_STATUS_TERMINATE = 0;  // Terminator
    public static final int RDP_DP_STATUS_PIPE_BUSY = 1;  // Pipeline Busy
    public static final int RDP_DP_STATUS_TOMINO_BUSY = 2;  // ToMini Busy
    public static final int RDP_DP_STATUS_PIPE_FLUSH = 3;  // Pipeline Flush
    public static final int RDP_DP_STATUS_TOMINO_FLUSH = 4;  // ToMini Flush
    public static final int RDP_DP_STATUS_FREEZE = 5;  // Freeze
    public static final int RDP_DP_STATUS_START_GCLK = 24;  // Start GCLK
    public static final int RDP_DP_CLOCK = (int)0x04100010;
    public static final int RDP_DP_BUFBUSY = (int)0x04100014;
    public static final int RDP_DP_PIPEBUSY = (int)0x04100018;
    public static final int RDP_DP_TMEM = (int)0x0410001C;

    // Video Interface (scanout engine)
    public static final int VI_BASE = (int)0x04400000;
    public static final int VI_VI_STATUS = (int)0x04400000;
    public static final int VI_VI_STATUS_TYPE = 0;  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
    public static final int VI_VI_STATUS_DITHER_FILTER = 6;  // Dither Filter Enable
    public static final int VI_VI_STATUS_GAMMA = 7;  // Gamma Correction Enable
    public static final int VI_VI_STATUS_GAMMA_DITHER = 8;  // Gamma Dither Enable
    public static final int VI_VI_STATUS_DIVOT = 9;  // Divot Control
    public static final int VI_VI_STATUS_SERRATION = 10;  //  Serration Enable (for interlaced)
    public static final int VI_VI_ORIGIN = (int)0x04400004;
    public static final int VI_VI_WIDTH = (int)0x04400008;
    public static final int VI_VI_V_INTR = (int)0x0440000C;
    public static final int VI_VI_V_CURRENT = (int)0x04400010;
    public static final int VI_VI_BURST = (int)0x04400014;
    public static final int VI_VI_H_SYNC = (int)0x04400018;
    public static final int VI_VI_H_SYNC_LEAP = (int)0x0440001C;
    public static final int VI_VI_H_VIDEO = (int)0x04400020;
    public static final int VI_VI_V_VIDEO = (int)0x04400024;
    public static final int VI_VI_V_BURST = (int)0x04400028;
    public static final int VI_VI_X_SCALE = (int)0x0440002C;
    public static final int VI_VI_Y_SCALE = (int)0x04400030;

    // Audio Interface (DAC)
    public static final int AI_BASE = (int)0x04500000;
    public static final int AI_AI_DRAM_ADDR = (int)0x04500000;
    public static final int AI_AI_LEN = (int)0x04500004;
    public static final int AI_AI_CONTROL = (int)0x04500008;
    public static final int AI_AI_CONTROL_DMA_ENABLE = 0;  // DMA Enable
    public static final int AI_AI_CONTROL_DMA_FIFO_FULL = 1;  // DMA FIFO Full
    public static final int AI_AI_STATUS = (int)0x0450000C;
    public static final int AI_AI_DACRATE = (int)0x04500010;
    public static final int AI_AI_BITRATE = (int)0x04500014;

    // Peripheral Interface (cartridge bus)
    public static final int PI_BASE = (int)0x04600000;
    public static final int PI_PI_DRAM_ADDR = (int)0x04600000;
    public static final int PI_PI_CART_ADDR = (int)0x04600004;
    public static final int PI_PI_RD_LEN = (int)0x04600008;
    public static final int PI_PI_WR_LEN = (int)0x0460000C;
    public static final int PI_PI_STATUS = (int)0x04600010;
    public static final int PI_PI_STATUS_DMA_BUSY = 0;  // DMA Busy
    public static final int PI_PI_STATUS_IO_BUSY = 1;  // I/O Busy
    public static final int PI_PI_STATUS_ERROR = 2;  // Bus Error
    public static final int PI_PI_BSD_DOM1_LAT = (int)0x04600014;
    public static final int PI_PI_BSD_DOM1_PWD = (int)0x04600018;
    public static final int PI_PI_BSD_DOM1_PGS = (int)0x0460001C;
    public static final int PI_PI_BSD_DOM1_RLS = (int)0x04600020;
    public static final int PI_PI_BSD_DOM2_LAT = (int)0x04600024;
    public static final int PI_PI_BSD_DOM2_PWD = (int)0x04600028;
    public static final int PI_PI_BSD_DOM2_PGS = (int)0x0460002C;
    public static final int PI_PI_BSD_DOM2_RLS = (int)0x04600030;

    // Serial Interface (Controller Pak / 64DD)
    public static final int SI_BASE = (int)0x04800000;
    public static final int SI_SI_DRAM_ADDR = (int)0x04800000;
    public static final int SI_SI_PIF_ADDR_RD64B = (int)0x04800004;
    public static final int SI_SI_PIF_ADDR_WR64B = (int)0x04800008;
    public static final int SI_SI_STATUS = (int)0x04800010;
    public static final int SI_SI_STATUS_DMA_BUSY = 0;  // DMA Busy
    public static final int SI_SI_STATUS_IO_BUSY = 1;  // I/O Busy
    public static final int SI_SI_STATUS_INTERRUPT = 12;  // SI Interrupt

    // PIF (CIC / NUSYC - anti-piracy/copy protection)
    public static final int PIF_BASE = (int)0x1FC007C0;
    public static final int PIF_PIF_CMD0 = (int)0x1FC007C0;
    public static final int PIF_PIF_CMD1 = (int)0x1FC007C1;
    public static final int PIF_PIF_CMD2 = (int)0x1FC007C2;
    public static final int PIF_PIF_CMD3 = (int)0x1FC007C3;
    public static final int PIF_PIF_CMD4 = (int)0x1FC007C4;
    public static final int PIF_PIF_CMD5 = (int)0x1FC007C5;
    public static final int PIF_PIF_CMD6 = (int)0x1FC007C6;
    public static final int PIF_PIF_CMD7 = (int)0x1FC007C7;
    public static final int PIF_PIF_STATUS = (int)0x1FC007FF;

    // Interrupt Control
    public static final int INTERRUPT_BASE = (int)0x1FC00200;
    public static final int INTERRUPT_MI_MODE = (int)0x1FC00200;
    public static final int INTERRUPT_MI_MODE_INIT_MODE = 0;  // Initialize Mode
    public static final int INTERRUPT_MI_MODE_EBUS_TEST = 1;  // EBUS Test Mode
    public static final int INTERRUPT_MI_VERSION = (int)0x1FC00204;
    public static final int INTERRUPT_MI_INTR = (int)0x1FC00208;
    public static final int INTERRUPT_MI_INTR_SP = 0;  // SP Interrupt Pending
    public static final int INTERRUPT_MI_INTR_SI = 1;  // SI Interrupt Pending
    public static final int INTERRUPT_MI_INTR_AI = 2;  // AI Interrupt Pending
    public static final int INTERRUPT_MI_INTR_VI = 3;  // VI Interrupt Pending
    public static final int INTERRUPT_MI_INTR_PI = 4;  // PI Interrupt Pending
    public static final int INTERRUPT_MI_INTR_DP = 5;  // DP Interrupt Pending
    public static final int INTERRUPT_MI_INTR_MASK = (int)0x1FC0020C;
    public static final int INTERRUPT_MI_INTR_MASK_SP_MASK = 0;  // SP Interrupt Mask
    public static final int INTERRUPT_MI_INTR_MASK_SI_MASK = 1;  // SI Interrupt Mask
    public static final int INTERRUPT_MI_INTR_MASK_AI_MASK = 2;  // AI Interrupt Mask
    public static final int INTERRUPT_MI_INTR_MASK_VI_MASK = 3;  // VI Interrupt Mask
    public static final int INTERRUPT_MI_INTR_MASK_PI_MASK = 4;  // PI Interrupt Mask
    public static final int INTERRUPT_MI_INTR_MASK_DP_MASK = 5;  // DP Interrupt Mask

    // Controller Interface (SI channel 0-3)
    public static final int CONTROLLER_BASE = (int)0x1FC00600;
    public static final int CONTROLLER_SI_CH0_DATA = (int)0x1FC00600;
    public static final int CONTROLLER_SI_CH1_DATA = (int)0x1FC00608;
    public static final int CONTROLLER_SI_CH2_DATA = (int)0x1FC00610;
    public static final int CONTROLLER_SI_CH3_DATA = (int)0x1FC00618;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Soft Reset / NMI
    public static final int IRQ_TLB_REFILL = 1;  // TLB Refill (I) / TLB Refill (D)
    public static final int IRQ_CACHE_ERROR = 2;  // Cache Error
    public static final int IRQ_GENERAL_EXCEPTION = 3;  // General Exception
    public static final int IRQ_RSP = 4;  // RSP Interrupt (microcode signal)
    public static final int IRQ_RDP = 5;  // RDP Interrupt (display list complete)
    public static final int IRQ_VI = 6;  // VI Interrupt (V-Blank / scanline)
    public static final int IRQ_AI = 7;  // AI Interrupt (audio DMA complete)
    public static final int IRQ_PI = 8;  // PI Interrupt (cartridge DMA)
    public static final int IRQ_SI = 9;  // SI Interrupt (serial interface)
    public static final int IRQ_TIMER_COMPARE = 10;  // Timer Compare (CP0 Count == Compare)

    // 引脚定义
    public static final int PIN_VCC = 1;  // Power Supply (3.3V)
    public static final int PIN_VSS = 2;  // Ground
    public static final int PIN_CLK = 3;  // System Clock (93.75MHz from CIC/PLL)
    public static final int PIN_RESET = 4;  // Reset (active low)
    public static final int PIN_NMI = 5;  // Non-Maskable Interrupt
    public static final int PIN_INT0 = 6;  // Interrupt 0 (RCP)
    public static final int PIN_INT1 = 7;  // Interrupt 1 (cartridge)
    public static final int PIN_INT2 = 8;  // Interrupt 2 (SI)
    public static final int PIN_INT3 = 9;  // Interrupt 3 (PIF)
    public static final int PIN_AB0 = 10;  // Address Bus Bit 0
    public static final int PIN_AB1 = 11;  // Address Bus Bit 1
    public static final int PIN_AB2 = 12;  // Address Bus Bit 2
    public static final int PIN_AB3 = 13;  // Address Bus Bit 3
    public static final int PIN_AB4 = 14;  // Address Bus Bit 4
    public static final int PIN_AB5 = 15;  // Address Bus Bit 5
    public static final int PIN_AB6 = 16;  // Address Bus Bit 6
    public static final int PIN_AB7 = 17;  // Address Bus Bit 7
    public static final int PIN_AB8 = 18;  // Address Bus Bit 8
    public static final int PIN_AB9 = 19;  // Address Bus Bit 9
    public static final int PIN_AB10 = 20;  // Address Bus Bit 10
    public static final int PIN_AB11 = 21;  // Address Bus Bit 11
    public static final int PIN_AB12 = 22;  // Address Bus Bit 12
    public static final int PIN_AB13 = 23;  // Address Bus Bit 13
    public static final int PIN_AB14 = 24;  // Address Bus Bit 14
    public static final int PIN_AB15 = 25;  // Address Bus Bit 15
    public static final int PIN_AB16 = 26;  // Address Bus Bit 16
    public static final int PIN_AB17 = 27;  // Address Bus Bit 17
    public static final int PIN_AB18 = 28;  // Address Bus Bit 18
    public static final int PIN_AB19 = 29;  // Address Bus Bit 19
    public static final int PIN_AB20 = 30;  // Address Bus Bit 20
    public static final int PIN_AB21 = 31;  // Address Bus Bit 21
    public static final int PIN_AB22 = 32;  // Address Bus Bit 22
    public static final int PIN_AB23 = 33;  // Address Bus Bit 23
    public static final int PIN_AB24 = 34;  // Address Bus Bit 24
    public static final int PIN_AB25 = 35;  // Address Bus Bit 25
    public static final int PIN_AB26 = 36;  // Address Bus Bit 26
    public static final int PIN_AB27 = 37;  // Address Bus Bit 27
    public static final int PIN_AB28 = 38;  // Address Bus Bit 28
    public static final int PIN_AB29 = 39;  // Address Bus Bit 29
    public static final int PIN_AB30 = 40;  // Address Bus Bit 30
    public static final int PIN_AB31 = 41;  // Address Bus Bit 31
    public static final int PIN_AB32 = 42;  // Address Bus Bit 32
    public static final int PIN_AB33 = 43;  // Address Bus Bit 33
    public static final int PIN_AB34 = 44;  // Address Bus Bit 34
    public static final int PIN_AB35 = 45;  // Address Bus Bit 35
    public static final int PIN_DB0 = 46;  // Data Bus Bit 0
    public static final int PIN_DB1 = 47;  // Data Bus Bit 1
    public static final int PIN_DB2 = 48;  // Data Bus Bit 2
    public static final int PIN_DB3 = 49;  // Data Bus Bit 3
    public static final int PIN_DB4 = 50;  // Data Bus Bit 4
    public static final int PIN_DB5 = 51;  // Data Bus Bit 5
    public static final int PIN_DB6 = 52;  // Data Bus Bit 6
    public static final int PIN_DB7 = 53;  // Data Bus Bit 7
    public static final int PIN_DB8 = 54;  // Data Bus Bit 8
    public static final int PIN_DB9 = 55;  // Data Bus Bit 9
    public static final int PIN_DB10 = 56;  // Data Bus Bit 10
    public static final int PIN_DB11 = 57;  // Data Bus Bit 11
    public static final int PIN_DB12 = 58;  // Data Bus Bit 12
    public static final int PIN_DB13 = 59;  // Data Bus Bit 13
    public static final int PIN_DB14 = 60;  // Data Bus Bit 14
    public static final int PIN_DB15 = 61;  // Data Bus Bit 15
    public static final int PIN_DB16 = 62;  // Data Bus Bit 16
    public static final int PIN_DB17 = 63;  // Data Bus Bit 17
    public static final int PIN_DB18 = 64;  // Data Bus Bit 18
    public static final int PIN_DB19 = 65;  // Data Bus Bit 19
    public static final int PIN_DB20 = 66;  // Data Bus Bit 20
    public static final int PIN_DB21 = 67;  // Data Bus Bit 21
    public static final int PIN_DB22 = 68;  // Data Bus Bit 22
    public static final int PIN_DB23 = 69;  // Data Bus Bit 23
    public static final int PIN_DB24 = 70;  // Data Bus Bit 24
    public static final int PIN_DB25 = 71;  // Data Bus Bit 25
    public static final int PIN_DB26 = 72;  // Data Bus Bit 26
    public static final int PIN_DB27 = 73;  // Data Bus Bit 27
    public static final int PIN_DB28 = 74;  // Data Bus Bit 28
    public static final int PIN_DB29 = 75;  // Data Bus Bit 29
    public static final int PIN_DB30 = 76;  // Data Bus Bit 30
    public static final int PIN_DB31 = 77;  // Data Bus Bit 31
    public static final int PIN_BE0 = 78;  // Byte Enable 0
    public static final int PIN_BE1 = 79;  // Byte Enable 1
    public static final int PIN_BE2 = 80;  // Byte Enable 2
    public static final int PIN_BE3 = 81;  // Byte Enable 3
    public static final int PIN_NCS0 = 82;  // Chip Select 0 (RDRAM)
    public static final int PIN_NCS1 = 83;  // Chip Select 1 (RCP)
    public static final int PIN_NCS2 = 84;  // Chip Select 2 (PIF ROM)
    public static final int PIN_NCS3 = 85;  // Chip Select 3 (Cartridge)
    public static final int PIN_NWR = 86;  // Write Enable
    public static final int PIN_NRD = 87;  // Read Enable
    public static final int PIN_EKN = 88;  // Audio DAC Data (I2S/EKN format)
    public static final int PIN_AUDIO_L = 89;  // Audio Left Output
    public static final int PIN_AUDIO_R = 90;  // Audio Right Output
    public static final int PIN_VIDEO_R = 91;  // Video Output Red
    public static final int PIN_VIDEO_G = 92;  // Video Output Green
    public static final int PIN_VIDEO_B = 93;  // Video Output Blue
    public static final int PIN_SYNC = 94;  // Video Sync

    public static native void nec_vr4300_init();
}
