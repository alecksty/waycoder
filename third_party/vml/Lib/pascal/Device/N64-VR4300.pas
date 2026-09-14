unit nec_vr4300;

interface

// NEC-VR4300寄存器定义
// 生成自: NEC/MIPS-R4000/NEC-VR4300
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like

// CPU架构: MIPS-R4300i
// 位宽: 64位
// 时钟频率: 93750000 Hz

const

  // 寄存器定义
  // Hard-wired Zero
  R0 = 0x00;

  // Assembler Temporary
  R1 = 0x08;

  // Value Return
  R2 = 0x10;

  // Expression Evaluation
  R3 = 0x18;

  // Expression Evaluation
  R4 = 0x20;

  // Expression Evaluation
  R5 = 0x28;

  // Expression Evaluation
  R6 = 0x30;

  // Expression Evaluation
  R7 = 0x38;

  // Expression Evaluation
  R8 = 0x40;

  // Expression Evaluation
  R9 = 0x48;

  // Expression Evaluation
  R10 = 0x50;

  // Expression Evaluation
  R11 = 0x58;

  // Expression Evaluation
  R12 = 0x60;

  // Expression Evaluation
  R13 = 0x68;

  // Expression Evaluation
  R14 = 0x70;

  // Expression Evaluation
  R15 = 0x78;

  // Saved Value
  R16 = 0x80;

  // Saved Value
  R17 = 0x88;

  // Saved Value
  R18 = 0x90;

  // Saved Value
  R19 = 0x98;

  // Saved Value
  R20 = 0xA0;

  // Saved Value
  R21 = 0xA8;

  // Saved Value
  R22 = 0xB0;

  // Saved Value
  R23 = 0xB8;

  // Temporary
  R24 = 0xC0;

  // Temporary
  R25 = 0xC8;

  // Kernel Reserved
  R26 = 0xD0;

  // Kernel Reserved
  R27 = 0xD8;

  // Global Pointer
  R28 = 0xE0;

  // Stack Pointer
  R29 = 0xE8;

  // Frame Pointer
  R30 = 0xF0;

  // Return Address
  R31 = 0xF8;

  // Multiply/Divide High (64-bit)
  HI = 0x100;

  // Multiply/Divide Low (64-bit)
  LO = 0x108;

  // Program Counter
  PC = 0x110;

  // LLAddr / LLBit (for LL/SC)
  LLB = 0x118;

  // TLB Index
  CP0_INDEX = 0x200;

  // TLB Random
  CP0_RANDOM = 0x208;

  // TLB EntryLo 0 (even page)
  CP0_ENTRYLO0 = 0x210;

  // TLB EntryLo 1 (odd page)
  CP0_ENTRYLO1 = 0x218;

  // Context Register (PTE base)
  CP0_CONTEXT = 0x220;

  // Page Mask (variable page size)
  CP0_PAGEMASK = 0x228;

  // TLB Wired
  CP0_WIRED = 0x230;

  // Bad Virtual Address
  CP0_BADVADDR = 0x238;

  // Count (incrementing timer)
  CP0_COUNT = 0x240;

  // TLB EntryHi (VPN2 + ASID)
  CP0_ENTRYHI = 0x250;

  // Compare (timer interrupt)
  CP0_COMPARE = 0x258;

  // Status Register
  CP0_STATUS = 0x260;
  CP0_STATUS_IE = 0;  // Interrupt Enable
  CP0_STATUS_EXL = 1;  // Exception Level
  CP0_STATUS_ERL = 2;  // Error Level
  CP0_STATUS_KSU = 0;  // Kernel/User Mode
  CP0_STATUS_UX = 5;  // User Mode 64-bit (1=64-bit user)
  CP0_STATUS_SX = 6;  // Supervisor Mode 64-bit
  CP0_STATUS_KX = 7;  // Kernel Mode 64-bit
  CP0_STATUS_IM0_7 = 0;  // Interrupt Mask
  CP0_STATUS_CU0 = 28;  // Coprocessor 0 Usable
  CP0_STATUS_BEV = 22;  // Bootstrap Exception Vector
  CP0_STATUS_TS = 21;  // TLB Shutdown
  CP0_STATUS_FR = 26;  // Floating-Point Register Mode (32 double)

  // Cause Register
  CP0_CAUSE = 0x268;
  CP0_CAUSE_EXCCODE = 0;  // Exception Code
  CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending
  CP0_CAUSE_BD = 31;  // Branch Delay Slot
  CP0_CAUSE_CE = 0;  // Coprocessor Error

  // Exception PC
  CP0_EPC = 0x270;

  // Config Register
  CP0_CONFIG = 0x280;

  // Load Linked Address
  CP0_LLADDR = 0x288;

  // WatchLo (data/instruction break)
  CP0_WATCHLO = 0x290;

  // WatchHi
  CP0_WATCHHI = 0x298;

  // Extended Context
  CP0_XCONTEXT = 0x2A0;

  // Tag/Process ID
  CP0_PID = 0x2B0;

  // Debug Register
  CP0_DEBUG = 0x2D8;

  // Performance Counter
  CP0_PERF = 0x2F0;

  // 内存段定义
  // RDRAM (4MB base, up to 8MB)
  RDRAM_START = 0x00000000;
  RDRAM_END = 0x003FFFFF;
  RDRAM_SIZE = 4194304;

  // RDRAM Registers
  RDRAM_REG_START = 0x18000000;
  RDRAM_REG_END = 0x18000FFF;
  RDRAM_REG_SIZE = 4096;

  // RCP SP Memory / DMEM (2KB)
  SP_MEM_START = 0x1FC00000;
  SP_MEM_END = 0x1FC007FF;
  SP_MEM_SIZE = 2048;

  // RCP SP Instruction Memory / IMEM (2KB)
  SP_IMEM_START = 0x1FC00800;
  SP_IMEM_END = 0x1FC00FFF;
  SP_IMEM_SIZE = 2048;

  // RCP Register Area
  RCP_REGS_START = 0x1FC00000;
  RCP_REGS_END = 0x1FC3FFFF;
  RCP_REGS_SIZE = 262144;

  // PI (Peripheral Interface) Registers
  PI_REGS_START = 0x1FC00000;
  PI_REGS_END = 0x1FC007FF;
  PI_REGS_SIZE = 2048;

  // VI (Video Interface) Registers
  VI_REGS_START = 0x1FC002C0;
  VI_REGS_END = 0x1FC002FF;
  VI_REGS_SIZE = 64;

  // AI (Audio Interface) Registers
  AI_REGS_START = 0x1FC00500;
  AI_REGS_END = 0x1FC0053F;
  AI_REGS_SIZE = 64;

  // SI (Serial Interface) Registers
  SI_REGS_START = 0x1FC004C0;
  SI_REGS_END = 0x1FC004FF;
  SI_REGS_SIZE = 64;

  // PI Bus DRAM (cartridge)
  PI_DRAM_START = 0xA0000000;
  PI_DRAM_END = 0xA4000000;
  PI_DRAM_SIZE = 67108864;

  // Cartridge ROM (up to 256MB)
  CART_ROM_START = 0xB0000000;
  CART_ROM_END = 0xBFFFFFFF;
  CART_ROM_SIZE = 268435456;

  // PIF-NUS ROM/RAM (CIC)
  PIF_RAM_START = 0x1FC007C0;
  PIF_RAM_END = 0x1FC007FF;
  PIF_RAM_SIZE = 64;

  // 外设定义
  // Reality Signal Processor (Audio/Video microcode engine)
  RSP_BASE = 0x04040000;
  RSP_SP_MEM_ADDR = 0x00;
  RSP_SP_DRAM_ADDR = 0x04;
  RSP_SP_RD_LEN = 0x08;
  RSP_SP_WR_LEN = 0x0C;
  RSP_SP_STATUS = 0x10;
  RSP_SP_STATUS_BROKE = 0;  // Command Queue Broke
  RSP_SP_STATUS_SLEEP = 2;  // SP Sleep
  RSP_SP_STATUS_GOODMATCH = 3;  // DMEM/IMEM Goodmatch
  RSP_SP_STATUS_SSTEP = 4;  // Single Step
  RSP_SP_STATUS_INTSIG = 5;  // Interrupt Signal
  RSP_SP_STATUS_HALT = 6;  // Halt
  RSP_SP_STATUS_CLEAR = 7;  // Clear SP Status
  RSP_SP_STATUS_INTR_BRK = 8;  // IntrOnBreak
  RSP_SP_STATUS_SIGNAL0 = 12;  // Software Signal 0
  RSP_SP_STATUS_SIGNAL1 = 13;  // Software Signal 1
  RSP_SP_STATUS_SIGNAL2 = 14;  // Software Signal 2
  RSP_SP_STATUS_SIGNAL3 = 15;  // Software Signal 3
  RSP_SP_STATUS_SIGNAL4 = 16;  // Software Signal 4
  RSP_SP_STATUS_SIGNAL5 = 17;  // Software Signal 5
  RSP_SP_STATUS_SIGNAL6 = 18;  // Software Signal 6
  RSP_SP_STATUS_SIGNAL7 = 19;  // Software Signal 7
  RSP_SP_DMA_FULL = 0x14;
  RSP_SP_DMA_BUSY = 0x18;
  RSP_SP_SEMAPHORE = 0x1C;
  RSP_SP_PC = 0x20;
  RSP_SP_IBIST = 0x24;

  // Reality Drawing Processor (Triangle/Quad rasterizer)
  RDP_BASE = 0x04100000;
  RDP_DP_START = 0x00;
  RDP_DP_END = 0x04;
  RDP_DP_CURRENT = 0x08;
  RDP_DP_STATUS = 0x0C;
  RDP_DP_STATUS_TERMINATE = 0;  // Terminator
  RDP_DP_STATUS_PIPE_BUSY = 1;  // Pipeline Busy
  RDP_DP_STATUS_TOMINO_BUSY = 2;  // ToMini Busy
  RDP_DP_STATUS_PIPE_FLUSH = 3;  // Pipeline Flush
  RDP_DP_STATUS_TOMINO_FLUSH = 4;  // ToMini Flush
  RDP_DP_STATUS_FREEZE = 5;  // Freeze
  RDP_DP_STATUS_START_GCLK = 24;  // Start GCLK
  RDP_DP_CLOCK = 0x10;
  RDP_DP_BUFBUSY = 0x14;
  RDP_DP_PIPEBUSY = 0x18;
  RDP_DP_TMEM = 0x1C;

  // Video Interface (scanout engine)
  VI_BASE = 0x04400000;
  VI_VI_STATUS = 0x00;
  VI_VI_STATUS_TYPE = 0;  // Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
  VI_VI_STATUS_DITHER_FILTER = 6;  // Dither Filter Enable
  VI_VI_STATUS_GAMMA = 7;  // Gamma Correction Enable
  VI_VI_STATUS_GAMMA_DITHER = 8;  // Gamma Dither Enable
  VI_VI_STATUS_DIVOT = 9;  // Divot Control
  VI_VI_STATUS_SERRATION = 10;  //  Serration Enable (for interlaced)
  VI_VI_ORIGIN = 0x04;
  VI_VI_WIDTH = 0x08;
  VI_VI_V_INTR = 0x0C;
  VI_VI_V_CURRENT = 0x10;
  VI_VI_BURST = 0x14;
  VI_VI_H_SYNC = 0x18;
  VI_VI_H_SYNC_LEAP = 0x1C;
  VI_VI_H_VIDEO = 0x20;
  VI_VI_V_VIDEO = 0x24;
  VI_VI_V_BURST = 0x28;
  VI_VI_X_SCALE = 0x2C;
  VI_VI_Y_SCALE = 0x30;

  // Audio Interface (DAC)
  AI_BASE = 0x04500000;
  AI_AI_DRAM_ADDR = 0x00;
  AI_AI_LEN = 0x04;
  AI_AI_CONTROL = 0x08;
  AI_AI_CONTROL_DMA_ENABLE = 0;  // DMA Enable
  AI_AI_CONTROL_DMA_FIFO_FULL = 1;  // DMA FIFO Full
  AI_AI_STATUS = 0x0C;
  AI_AI_DACRATE = 0x10;
  AI_AI_BITRATE = 0x14;

  // Peripheral Interface (cartridge bus)
  PI_BASE = 0x04600000;
  PI_PI_DRAM_ADDR = 0x00;
  PI_PI_CART_ADDR = 0x04;
  PI_PI_RD_LEN = 0x08;
  PI_PI_WR_LEN = 0x0C;
  PI_PI_STATUS = 0x10;
  PI_PI_STATUS_DMA_BUSY = 0;  // DMA Busy
  PI_PI_STATUS_IO_BUSY = 1;  // I/O Busy
  PI_PI_STATUS_ERROR = 2;  // Bus Error
  PI_PI_BSD_DOM1_LAT = 0x14;
  PI_PI_BSD_DOM1_PWD = 0x18;
  PI_PI_BSD_DOM1_PGS = 0x1C;
  PI_PI_BSD_DOM1_RLS = 0x20;
  PI_PI_BSD_DOM2_LAT = 0x24;
  PI_PI_BSD_DOM2_PWD = 0x28;
  PI_PI_BSD_DOM2_PGS = 0x2C;
  PI_PI_BSD_DOM2_RLS = 0x30;

  // Serial Interface (Controller Pak / 64DD)
  SI_BASE = 0x04800000;
  SI_SI_DRAM_ADDR = 0x00;
  SI_SI_PIF_ADDR_RD64B = 0x04;
  SI_SI_PIF_ADDR_WR64B = 0x08;
  SI_SI_STATUS = 0x10;
  SI_SI_STATUS_DMA_BUSY = 0;  // DMA Busy
  SI_SI_STATUS_IO_BUSY = 1;  // I/O Busy
  SI_SI_STATUS_INTERRUPT = 12;  // SI Interrupt

  // PIF (CIC / NUSYC - anti-piracy/copy protection)
  PIF_BASE = 0x1FC007C0;
  PIF_PIF_CMD0 = 0x00;
  PIF_PIF_CMD1 = 0x01;
  PIF_PIF_CMD2 = 0x02;
  PIF_PIF_CMD3 = 0x03;
  PIF_PIF_CMD4 = 0x04;
  PIF_PIF_CMD5 = 0x05;
  PIF_PIF_CMD6 = 0x06;
  PIF_PIF_CMD7 = 0x07;
  PIF_PIF_STATUS = 0x3F;

  // Interrupt Control
  INTERRUPT_BASE = 0x1FC00200;
  INTERRUPT_MI_MODE = 0x00;
  INTERRUPT_MI_MODE_INIT_MODE = 0;  // Initialize Mode
  INTERRUPT_MI_MODE_EBUS_TEST = 1;  // EBUS Test Mode
  INTERRUPT_MI_VERSION = 0x04;
  INTERRUPT_MI_INTR = 0x08;
  INTERRUPT_MI_INTR_SP = 0;  // SP Interrupt Pending
  INTERRUPT_MI_INTR_SI = 1;  // SI Interrupt Pending
  INTERRUPT_MI_INTR_AI = 2;  // AI Interrupt Pending
  INTERRUPT_MI_INTR_VI = 3;  // VI Interrupt Pending
  INTERRUPT_MI_INTR_PI = 4;  // PI Interrupt Pending
  INTERRUPT_MI_INTR_DP = 5;  // DP Interrupt Pending
  INTERRUPT_MI_INTR_MASK = 0x0C;
  INTERRUPT_MI_INTR_MASK_SP_MASK = 0;  // SP Interrupt Mask
  INTERRUPT_MI_INTR_MASK_SI_MASK = 1;  // SI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_AI_MASK = 2;  // AI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_VI_MASK = 3;  // VI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_PI_MASK = 4;  // PI Interrupt Mask
  INTERRUPT_MI_INTR_MASK_DP_MASK = 5;  // DP Interrupt Mask

  // Controller Interface (SI channel 0-3)
  CONTROLLER_BASE = 0x1FC00600;
  CONTROLLER_SI_CH0_DATA = 0x00;
  CONTROLLER_SI_CH1_DATA = 0x08;
  CONTROLLER_SI_CH2_DATA = 0x10;
  CONTROLLER_SI_CH3_DATA = 0x18;

  // 中断向量定义
  RESET_VECTOR = 0;  // Soft Reset / NMI
  TLB_REFILL_VECTOR = 1;  // TLB Refill (I) / TLB Refill (D)
  CACHE_ERROR_VECTOR = 2;  // Cache Error
  GENERAL_EXCEPTION_VECTOR = 3;  // General Exception
  RSP_VECTOR = 4;  // RSP Interrupt (microcode signal)
  RDP_VECTOR = 5;  // RDP Interrupt (display list complete)
  VI_VECTOR = 6;  // VI Interrupt (V-Blank / scanline)
  AI_VECTOR = 7;  // AI Interrupt (audio DMA complete)
  PI_VECTOR = 8;  // PI Interrupt (cartridge DMA)
  SI_VECTOR = 9;  // SI Interrupt (serial interface)
  TIMER_COMPARE_VECTOR = 10;  // Timer Compare (CP0 Count == Compare)

  // 引脚定义
  PIN_VCC = 1;  // Power Supply (3.3V)
  PIN_VSS = 2;  // Ground
  PIN_CLK = 3;  // System Clock (93.75MHz from CIC/PLL)
  PIN_RESET = 4;  // Reset (active low)
  PIN_NMI = 5;  // Non-Maskable Interrupt
  PIN_INT0 = 6;  // Interrupt 0 (RCP)
  PIN_INT1 = 7;  // Interrupt 1 (cartridge)
  PIN_INT2 = 8;  // Interrupt 2 (SI)
  PIN_INT3 = 9;  // Interrupt 3 (PIF)
  PIN_AB0 = 10;  // Address Bus Bit 0
  PIN_AB1 = 11;  // Address Bus Bit 1
  PIN_AB2 = 12;  // Address Bus Bit 2
  PIN_AB3 = 13;  // Address Bus Bit 3
  PIN_AB4 = 14;  // Address Bus Bit 4
  PIN_AB5 = 15;  // Address Bus Bit 5
  PIN_AB6 = 16;  // Address Bus Bit 6
  PIN_AB7 = 17;  // Address Bus Bit 7
  PIN_AB8 = 18;  // Address Bus Bit 8
  PIN_AB9 = 19;  // Address Bus Bit 9
  PIN_AB10 = 20;  // Address Bus Bit 10
  PIN_AB11 = 21;  // Address Bus Bit 11
  PIN_AB12 = 22;  // Address Bus Bit 12
  PIN_AB13 = 23;  // Address Bus Bit 13
  PIN_AB14 = 24;  // Address Bus Bit 14
  PIN_AB15 = 25;  // Address Bus Bit 15
  PIN_AB16 = 26;  // Address Bus Bit 16
  PIN_AB17 = 27;  // Address Bus Bit 17
  PIN_AB18 = 28;  // Address Bus Bit 18
  PIN_AB19 = 29;  // Address Bus Bit 19
  PIN_AB20 = 30;  // Address Bus Bit 20
  PIN_AB21 = 31;  // Address Bus Bit 21
  PIN_AB22 = 32;  // Address Bus Bit 22
  PIN_AB23 = 33;  // Address Bus Bit 23
  PIN_AB24 = 34;  // Address Bus Bit 24
  PIN_AB25 = 35;  // Address Bus Bit 25
  PIN_AB26 = 36;  // Address Bus Bit 26
  PIN_AB27 = 37;  // Address Bus Bit 27
  PIN_AB28 = 38;  // Address Bus Bit 28
  PIN_AB29 = 39;  // Address Bus Bit 29
  PIN_AB30 = 40;  // Address Bus Bit 30
  PIN_AB31 = 41;  // Address Bus Bit 31
  PIN_AB32 = 42;  // Address Bus Bit 32
  PIN_AB33 = 43;  // Address Bus Bit 33
  PIN_AB34 = 44;  // Address Bus Bit 34
  PIN_AB35 = 45;  // Address Bus Bit 35
  PIN_DB0 = 46;  // Data Bus Bit 0
  PIN_DB1 = 47;  // Data Bus Bit 1
  PIN_DB2 = 48;  // Data Bus Bit 2
  PIN_DB3 = 49;  // Data Bus Bit 3
  PIN_DB4 = 50;  // Data Bus Bit 4
  PIN_DB5 = 51;  // Data Bus Bit 5
  PIN_DB6 = 52;  // Data Bus Bit 6
  PIN_DB7 = 53;  // Data Bus Bit 7
  PIN_DB8 = 54;  // Data Bus Bit 8
  PIN_DB9 = 55;  // Data Bus Bit 9
  PIN_DB10 = 56;  // Data Bus Bit 10
  PIN_DB11 = 57;  // Data Bus Bit 11
  PIN_DB12 = 58;  // Data Bus Bit 12
  PIN_DB13 = 59;  // Data Bus Bit 13
  PIN_DB14 = 60;  // Data Bus Bit 14
  PIN_DB15 = 61;  // Data Bus Bit 15
  PIN_DB16 = 62;  // Data Bus Bit 16
  PIN_DB17 = 63;  // Data Bus Bit 17
  PIN_DB18 = 64;  // Data Bus Bit 18
  PIN_DB19 = 65;  // Data Bus Bit 19
  PIN_DB20 = 66;  // Data Bus Bit 20
  PIN_DB21 = 67;  // Data Bus Bit 21
  PIN_DB22 = 68;  // Data Bus Bit 22
  PIN_DB23 = 69;  // Data Bus Bit 23
  PIN_DB24 = 70;  // Data Bus Bit 24
  PIN_DB25 = 71;  // Data Bus Bit 25
  PIN_DB26 = 72;  // Data Bus Bit 26
  PIN_DB27 = 73;  // Data Bus Bit 27
  PIN_DB28 = 74;  // Data Bus Bit 28
  PIN_DB29 = 75;  // Data Bus Bit 29
  PIN_DB30 = 76;  // Data Bus Bit 30
  PIN_DB31 = 77;  // Data Bus Bit 31
  PIN_BE0 = 78;  // Byte Enable 0
  PIN_BE1 = 79;  // Byte Enable 1
  PIN_BE2 = 80;  // Byte Enable 2
  PIN_BE3 = 81;  // Byte Enable 3
  PIN_NCS0 = 82;  // Chip Select 0 (RDRAM)
  PIN_NCS1 = 83;  // Chip Select 1 (RCP)
  PIN_NCS2 = 84;  // Chip Select 2 (PIF ROM)
  PIN_NCS3 = 85;  // Chip Select 3 (Cartridge)
  PIN_NWR = 86;  // Write Enable
  PIN_NRD = 87;  // Read Enable
  PIN_EKN = 88;  // Audio DAC Data (I2S/EKN format)
  PIN_AUDIO_L = 89;  // Audio Left Output
  PIN_AUDIO_R = 90;  // Audio Right Output
  PIN_VIDEO_R = 91;  // Video Output Red
  PIN_VIDEO_G = 92;  // Video Output Green
  PIN_VIDEO_B = 93;  // Video Output Blue
  PIN_SYNC = 94;  // Video Sync

type
  TNEC-VR4300 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure nec_vr4300_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure nec_vr4300_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
