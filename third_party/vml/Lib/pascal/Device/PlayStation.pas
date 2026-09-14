unit mips_r3000a;

interface

// MIPS-R3000A寄存器定义
// 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Sony PlayStation (PS1) main processor - MIPS R3000A @ 33.87MHz with R4000-like ISA

// CPU架构: MIPS-R3000A
// 位宽: 32位
// 时钟频率: 33870000 Hz

const

  // 寄存器定义
  // Hard-wired Zero
  R0 = 0x00;

  // Assembler Temporary
  R1 = 0x04;

  // Value Returned by Subroutines
  R2 = 0x08;

  // Expression Evaluation
  R3 = 0x0C;

  // Expression Evaluation
  R4 = 0x10;

  // Expression Evaluation
  R5 = 0x14;

  // Expression Evaluation
  R6 = 0x18;

  // Expression Evaluation
  R7 = 0x1C;

  // Expression Evaluation
  R8 = 0x20;

  // Expression Evaluation
  R9 = 0x24;

  // Expression Evaluation
  R10 = 0x28;

  // Expression Evaluation
  R11 = 0x2C;

  // Expression Evaluation
  R12 = 0x30;

  // Expression Evaluation
  R13 = 0x34;

  // Expression Evaluation
  R14 = 0x38;

  // Expression Evaluation
  R15 = 0x3C;

  // Saved Value
  R16 = 0x40;

  // Saved Value
  R17 = 0x44;

  // Saved Value
  R18 = 0x48;

  // Saved Value
  R19 = 0x4C;

  // Saved Value
  R20 = 0x50;

  // Saved Value
  R21 = 0x54;

  // Saved Value
  R22 = 0x58;

  // Saved Value
  R23 = 0x5C;

  // Temporary
  R24 = 0x60;

  // Temporary
  R25 = 0x64;

  // Kernel Reserved
  R26 = 0x68;

  // Kernel Reserved
  R27 = 0x6C;

  // Global Pointer
  R28 = 0x70;

  // Stack Pointer
  R29 = 0x74;

  // Frame Pointer
  R30 = 0x78;

  // Return Address
  R31 = 0x7C;

  // Multiply/Divide High
  HI = 0x80;

  // Multiply/Divide Low
  LO = 0x84;

  // Program Counter
  PC = 0x88;

  // Coprocessor 0 - Status Register
  CP0_SR = 0x90;
  CP0_SR_IE = 0;  // Interrupt Enable
  CP0_SR_EXL = 1;  // Exception Level
  CP0_SR_ERL = 2;  // Error Level
  CP0_SR_KSU = 0;  // Kernel/User Mode
  CP0_SR_IM0_7 = 0;  // Interrupt Mask bits
  CP0_SR_CU0 = 28;  // Coprocessor 0 Usable
  CP0_SR_BEV = 22;  // Bootstrap Exception Vector

  // Coprocessor 0 - Cause Register
  CP0_CAUSE = 0x94;
  CP0_CAUSE_EXCCODE = 0;  // Exception Code
  CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending bits

  // Coprocessor 0 - Exception PC
  CP0_EPC = 0x98;

  // Coprocessor 0 - Bad Virtual Address
  CP0_BADVADDR = 0x9C;

  // Coprocessor 0 - Context Register
  CP0_CONTEXT = 0xA0;

  // Coprocessor 0 - Process ID
  CP0_PID = 0xA4;

  // 内存段定义
  // KSEG0 - Cached RAM (2MB System RAM)
  KSEG0_START = 0x80000000;
  KSEG0_END = 0x801FFFFF;
  KSEG0_SIZE = 2097152;

  // KSEG1 - Uncached RAM (2MB System RAM)
  KSEG1_START = 0xA0000000;
  KSEG1_END = 0xA01FFFFF;
  KSEG1_SIZE = 2097152;

  // VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
  VRAM_START = 0xA0000000;
  VRAM_END = 0xA01FFFFF;
  VRAM_SIZE = 1048576;

  // Expansion Region (maps to expansion RAM area)
  EXPANSION_START = 0xA0000000;
  EXPANSION_END = 0xA00FFFFF;
  EXPANSION_SIZE = 1048576;

  // Data Scratchpad (1KB)
  SCRATCHPAD_START = 0x1F800000;
  SCRATCHPAD_END = 0x1F8003FF;
  SCRATCHPAD_SIZE = 1024;

  // Kernel BIOS ROM (512KB)
  EXP_ROM_START = 0x1FC00000;
  EXP_ROM_END = 0x1FC7FFFF;
  EXP_ROM_SIZE = 524288;

  // User ROM / Kernel Expansion
  USER_ROM_START = 0x1F800000;
  USER_ROM_END = 0x1FBFFFFF;
  USER_ROM_SIZE = 4194304;

  // I/O Register Area (Expansion 1)
  MMIO_START = 0x1F801000;
  MMIO_END = 0x1F802FFF;
  MMIO_SIZE = 8192;

  // GPU Registers
  GPU_START = 0x1F801810;
  GPU_END = 0x1F801817;
  GPU_SIZE = 8;

  // CD-ROM Registers
  CDROM_START = 0x1F801800;
  CDROM_END = 0x1F80180F;
  CDROM_SIZE = 16;

  // SPU Registers
  SPU_START = 0x1F801C00;
  SPU_END = 0x1F801DFF;
  SPU_SIZE = 512;

  // Interrupt Control
  IRQ_START = 0x1F801070;
  IRQ_END = 0x1F801077;
  IRQ_SIZE = 8;

  // DMA Registers (7 channels)
  DMA_START = 0x1F801080;
  DMA_END = 0x1F8010FF;
  DMA_SIZE = 128;

  // Timer Registers
  TIMER_START = 0x1F801100;
  TIMER_END = 0x1F80112F;
  TIMER_SIZE = 48;

  // JOY Interface Registers
  JOY_START = 0x1F801040;
  JOY_END = 0x1F80104F;
  JOY_SIZE = 16;

  // MDEC (Motion Decoder) Registers
  MDEC_START = 0x1F801820;
  MDEC_END = 0x1F801827;
  MDEC_SIZE = 8;

  // SIO Registers
  SIO_START = 0x1F801050;
  SIO_END = 0x1F80105F;
  SIO_SIZE = 16;

  // GPU Status
  GPU_STAT_START = 0x1F801814;
  GPU_STAT_END = 0x1F801817;
  GPU_STAT_SIZE = 4;

  // CD-ROM Status
  CDROM_STAT_START = 0x1F801801;
  CDROM_STAT_END = 0x1F801803;
  CDROM_STAT_SIZE = 3;

  // SPU Work RAM (1KB)
  SPU_RAM_START = 0x1F800000;
  SPU_RAM_END = 0x1F800FFF;
  SPU_RAM_SIZE = 1024;

  // 外设定义
  // Graphics Processing Unit
  GPU_BASE = 0x1F801810;
  GPU_GP0_CMD = 0x00;
  GPU_GP0_DATA = 0x04;
  GPU_GP1_CMD = 0x08;
  GPU_GP1_DATA = 0x0C;
  GPU_TPAGE = 0x00;
  GPU_DRAW_MODE = 0x01;
  GPU_TEXTURE_WIN = 0x02;
  GPU_DRAW_OFFSET_X = 0x03;
  GPU_DRAW_OFFSET_Y = 0x04;
  GPU_DRAW_AREA_X = 0x05;
  GPU_DRAW_AREA_Y = 0x06;
  GPU_DITHER = 0x07;
  GPU_DISPLAY_MODE = 0x08;
  GPU_DISPLAY_START_X = 0x09;
  GPU_DISPLAY_START_Y = 0x0A;
  GPU_DISPLAY_HORZ = 0x0B;
  GPU_DISPLAY_VERT = 0x0C;
  GPU_DMA_MODE = 0x0D;
  GPU_GPU_STAT = 0x0C;
  GPU_GPU_STAT_READY_CMD = 0;  // GPU Ready to Receive Command
  GPU_GPU_STAT_READY_DMA = 1;  // GPU Ready for DMA
  GPU_GPU_STAT_DRAWING = 2;  // Drawing Busy
  GPU_GPU_STAT_DMA_REQ = 3;  // DMA Request
  GPU_GPU_STAT_COMMAND_BUSY = 4;  // Command Busy
  GPU_GPU_STAT_DISPLAY_DISABLE = 5;  // Display Disable
  GPU_GPU_STAT_INTERRUPT = 24;  // V-Blank Interrupt Flag

  // Geometry Transformation Engine
  GTE_BASE = 0x1F801880;
  GTE_GTE_VXY0 = 0x00;
  GTE_GTE_VZ0 = 0x04;
  GTE_GTE_VXY1 = 0x08;
  GTE_GTE_VZ1 = 0x0C;
  GTE_GTE_VXY2 = 0x10;
  GTE_GTE_VZ2 = 0x14;
  GTE_GTE_RGB0 = 0x18;
  GTE_GTE_RGB1 = 0x1C;
  GTE_GTE_RGB2 = 0x20;
  GTE_GTE_RTP = 0x30;
  GTE_GTE_TRX = 0x34;
  GTE_GTE_TRY = 0x38;
  GTE_GTE_TRZ = 0x3C;
  GTE_GTE_MAC0 = 0x40;
  GTE_GTE_MAC1 = 0x44;
  GTE_GTE_MAC2 = 0x48;
  GTE_GTE_MAC3 = 0x4C;
  GTE_GTE_IR0 = 0x50;
  GTE_GTE_IR1 = 0x54;
  GTE_GTE_IR2 = 0x58;
  GTE_GTE_IR3 = 0x5C;
  GTE_GTE_LZCS = 0x60;
  GTE_GTE_LZCR = 0x64;
  GTE_GTE_CTX = 0x68;
  GTE_GTE_CTY = 0x6C;
  GTE_GTE_CTZ = 0x70;
  GTE_GTE_RTX = 0x74;
  GTE_GTE_RTY = 0x78;
  GTE_GTE_RTZ = 0x7C;
  GTE_GTE_SR = 0x80;
  GTE_GTE_CMD = 0x84;
  GTE_GTE_H = 0x88;
  GTE_GTE_DQB = 0x8C;
  GTE_GTE_DQA = 0x90;
  GTE_GTE_ZSF3 = 0x94;
  GTE_GTE_ZSF4 = 0x98;
  GTE_GTE_OTZ = 0x9C;

  // Sound Processing Unit (24-channel ADPCM)
  SPU_BASE = 0x1F801C00;
  SPU_SPU_CTRL = 0x00;
  SPU_SPU_CTRL_REVERB_MASTER = 0;  // Reverb Master Enable
  SPU_SPU_CTRL_IRQ9 = 9;  // Interrupt Request Enable
  SPU_SPU_STAT = 0x04;
  SPU_SPU_CDVOL_L = 0x08;
  SPU_SPU_CDVOL_R = 0x0A;
  SPU_SPU_MAINVOL_L = 0x0C;
  SPU_SPU_MAINVOL_R = 0x0E;
  SPU_SPU_REVERB_L = 0x10;
  SPU_SPU_REVERB_R = 0x12;
  SPU_SPU_KEYON = 0x80;
  SPU_SPU_KEYOFF = 0x82;
  SPU_SPU_CHANNEL_MUTE = 0x84;
  SPU_SPU_NOISE_CLK = 0x88;
  SPU_SPU_REVERB_ADDR = 0x8A;
  SPU_SPU_IRQ_ADDR = 0x8C;
  SPU_SPU_REVERB_VOL_L = 0x8E;
  SPU_SPU_REVERB_VOL_R = 0x90;
  SPU_SPU_VOICE_VOL_L = 0x00;
  SPU_SPU_VOICE_VOL_R = 0x01;
  SPU_SPU_VOICE_FREQ = 0x02;
  SPU_SPU_VOICE_START = 0x04;
  SPU_SPU_VOICE_ADSR1 = 0x06;
  SPU_SPU_VOICE_ADSR2 = 0x08;
  SPU_SPU_VOICE_ENV = 0x0A;
  SPU_SPU_VOICE_REPEAT = 0x0C;
  SPU_VOICE_BASE_SIZE = 0x10;

  // Motion Decoder (JPEG Decompression)
  MDEC_BASE = 0x1F801820;
  MDEC_MDEC_CTRL = 0x00;
  MDEC_MDEC_CTRL_DATA_IN_SIZE = 0;  // Data-in size in words
  MDEC_MDEC_CTRL_RESET = 16;  // Reset MDEC
  MDEC_MDEC_CTRL_BUSY = 17;  // MDEC Busy
  MDEC_MDEC_DATA = 0x04;
  MDEC_MDEC_BKGD = 0x08;

  // DMA Controller (7 channels)
  DMA_BASE = 0x1F801080;
  DMA_DMA_DPCR = 0x00;
  DMA_DMA_DPCR_CH0_EN = 0;  // Channel 0 Enable
  DMA_DMA_DPCR_CH1_EN = 4;  // Channel 1 Enable
  DMA_DMA_DPCR_CH2_EN = 8;  // Channel 2 Enable
  DMA_DMA_DPCR_CH3_EN = 12;  // Channel 3 Enable
  DMA_DMA_DPCR_CH4_EN = 16;  // Channel 4 Enable
  DMA_DMA_DPCR_CH5_EN = 20;  // Channel 5 Enable
  DMA_DMA_DPCR_CH6_EN = 24;  // Channel 6 Enable
  DMA_DMA_INT = 0x04;
  DMA_DMA_CH0_BASE = 0x10;
  DMA_DMA_CH0_COUNT = 0x14;
  DMA_DMA_CH0_CTRL = 0x18;
  DMA_DMA_CH0_CTRL_DEST_DIR = 0;  // Destination Direction
  DMA_DMA_CH0_CTRL_SRC_DIR = 0;  // Source Direction
  DMA_DMA_CH0_CTRL_STEPS = 0;  // Step
  DMA_DMA_CH0_CTRL_CHAIN = 0;  // Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
  DMA_DMA_CH0_CTRL_SYNC = 0;  // Sync Mode (0=immediate, 1=request, 2=linked-list)
  DMA_DMA_CH0_CTRL_TRIGGER = 10;  // Trigger
  DMA_DMA_CH1_BASE = 0x20;
  DMA_DMA_CH1_COUNT = 0x24;
  DMA_DMA_CH1_CTRL = 0x28;
  DMA_DMA_CH2_BASE = 0x30;
  DMA_DMA_CH2_COUNT = 0x34;
  DMA_DMA_CH2_CTRL = 0x38;
  DMA_DMA_CH3_BASE = 0x40;
  DMA_DMA_CH3_COUNT = 0x44;
  DMA_DMA_CH3_CTRL = 0x48;
  DMA_DMA_CH4_BASE = 0x50;
  DMA_DMA_CH4_COUNT = 0x54;
  DMA_DMA_CH4_CTRL = 0x58;
  DMA_DMA_CH5_BASE = 0x60;
  DMA_DMA_CH5_COUNT = 0x64;
  DMA_DMA_CH5_CTRL = 0x68;
  DMA_DMA_CH6_BASE = 0x70;
  DMA_DMA_CH6_COUNT = 0x74;
  DMA_DMA_CH6_CTRL = 0x78;

  // Timers (3 timers)
  TIMER_BASE = 0x1F801100;
  TIMER_TM0_COUNT = 0x00;
  TIMER_TM0_MODE = 0x04;
  TIMER_TM0_MODE_RELOAD = 0;  // Reload Enable
  TIMER_TM0_MODE_CLOCK = 0;  // Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
  TIMER_TM0_MODE_IRQ_EN = 3;  // IRQ Enable
  TIMER_TM0_MODE_IRQ_REPEAT = 4;  // IRQ Repeat
  TIMER_TM0_MODE_IRQ_TOGGLE = 5;  // IRQ Toggle Mode
  TIMER_TM0_MODE_REACH_MAX = 6;  // Reached Max Value
  TIMER_TM0_TARGET = 0x08;
  TIMER_TM1_COUNT = 0x10;
  TIMER_TM1_MODE = 0x14;
  TIMER_TM1_TARGET = 0x18;
  TIMER_TM2_COUNT = 0x20;
  TIMER_TM2_MODE = 0x24;
  TIMER_TM2_TARGET = 0x28;

  // CD-ROM Controller
  CDROM_BASE = 0x1F801800;
  CDROM_CD0_DATA = 0x00;
  CDROM_CD0_STATUS = 0x01;
  CDROM_CD0_RESPONSE = 0x02;
  CDROM_CD0_DATA1 = 0x03;
  CDROM_CD0_INT_FLAG = 0x04;
  CDROM_CD0_INT_FLAG_INT1 = 0;  // Data Ready
  CDROM_CD0_INT_FLAG_INT2 = 1;  // Command Complete
  CDROM_CD0_INT_FLAG_INT3 = 2;  // Acknowledge Received
  CDROM_CD0_INT_FLAG_INT4 = 3;  // Error / N-Complete
  CDROM_CD0_VOLUME_L = 0x08;
  CDROM_CD0_VOLUME_R = 0x09;

  // JOY Interface
  JOY_BASE = 0x1F801040;
  JOY_JOY_CTRL = 0x00;
  JOY_JOY_CTRL_TX_EN = 0;  // Transmit Enable
  JOY_JOY_CTRL_RX_EN = 1;  // Receive Enable
  JOY_JOY_CTRL_CLOCK = 3;  // Internal/External Clock
  JOY_JOY_CTRL_IRQ_EN = 4;  // IRQ Enable
  JOY_JOY_MODE = 0x01;
  JOY_JOY_BAUD = 0x02;
  JOY_JOY_TX_DATA = 0x04;
  JOY_JOY_RX_DATA = 0x05;
  JOY_JOY_STAT = 0x06;
  JOY_JOY_STAT_TX_EMPTY = 0;  // Transmit Buffer Empty
  JOY_JOY_STAT_RX_READY = 2;  // Receive Data Ready
  JOY_JOY_STAT_TX_IRQ = 3;  // Transmit IRQ Pending
  JOY_JOY_STAT_RX_IRQ = 4;  // Receive IRQ Pending

  // SIO (Serial I/O - Memory Card)
  SIO_BASE = 0x1F801050;
  SIO_SIO_DATA = 0x00;
  SIO_SIO_STATUS = 0x01;
  SIO_SIO_MODE = 0x02;
  SIO_SIO_CTRL = 0x03;
  SIO_SIO_BAUD = 0x04;

  // Interrupt Controller
  INTERRUPT_BASE = 0x1F801070;
  INTERRUPT_INT_STAT = 0x00;
  INTERRUPT_INT_MASK = 0x04;
  INTERRUPT_INT_MASK_VBLANK = 0;  // V-Blank Interrupt
  INTERRUPT_INT_MASK_GPU = 1;  // GPU Interrupt
  INTERRUPT_INT_MASK_CDROM = 2;  // CD-ROM Interrupt
  INTERRUPT_INT_MASK_DMA0 = 3;  // DMA Channel 0
  INTERRUPT_INT_MASK_DMA1 = 4;  // DMA Channel 1
  INTERRUPT_INT_MASK_DMA2 = 5;  // DMA Channel 2
  INTERRUPT_INT_MASK_DMA3 = 6;  // DMA Channel 3
  INTERRUPT_INT_MASK_DMA4 = 7;  // DMA Channel 4
  INTERRUPT_INT_MASK_DMA5 = 8;  // DMA Channel 5
  INTERRUPT_INT_MASK_DMA6 = 9;  // DMA Channel 6
  INTERRUPT_INT_MASK_TIMER0 = 10;  // Timer 0
  INTERRUPT_INT_MASK_TIMER1 = 11;  // Timer 1
  INTERRUPT_INT_MASK_TIMER2 = 12;  // Timer 2
  INTERRUPT_INT_MASK_SIO = 13;  // SIO / Memory Card
  INTERRUPT_INT_MASK_SPU = 14;  // SPU Interrupt
  INTERRUPT_INT_MASK_PIO = 15;  // PIO (Expansion)

  // 中断向量定义
  VBLANK_VECTOR = 0;  // V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
  GPU_VECTOR = 1;  // GPU Interrupt (drawing complete / V-Blank)
  CDROM_VECTOR = 2;  // CD-ROM Interrupt
  DMA0_VECTOR = 3;  // DMA Channel 0 Complete
  DMA1_VECTOR = 4;  // DMA Channel 1 Complete
  DMA2_VECTOR = 5;  // DMA Channel 2 Complete
  DMA3_VECTOR = 6;  // DMA Channel 3 Complete
  DMA4_VECTOR = 7;  // DMA Channel 4 Complete
  DMA5_VECTOR = 8;  // DMA Channel 5 Complete
  DMA6_VECTOR = 9;  // DMA Channel 6 Complete
  TIMER0_VECTOR = 10;  // Timer 0 Interrupt
  TIMER1_VECTOR = 11;  // Timer 1 Interrupt
  TIMER2_VECTOR = 12;  // Timer 2 Interrupt
  SIO_VECTOR = 13;  // SIO / Memory Card Interrupt
  SPU_VECTOR = 14;  // SPU Interrupt
  PIO_VECTOR = 15;  // PIO / Expansion Interrupt

  // 引脚定义
  PIN_VCC = 1;  // Power Supply (3.3V regulated)
  PIN_VSS = 2;  // Ground
  PIN_CLK = 3;  // System Clock Input (53.6932MHz / 2 = 26.8466MHz bus)
  PIN_RESET = 4;  // Reset (active low)
  PIN_NMI = 5;  // Non-Maskable Interrupt
  PIN_IRQ = 6;  // Interrupt Request
  PIN_AB0 = 7;  // Address Bus Bit 0
  PIN_AB1 = 8;  // Address Bus Bit 1
  PIN_AB2 = 9;  // Address Bus Bit 2
  PIN_AB3 = 10;  // Address Bus Bit 3
  PIN_AB4 = 11;  // Address Bus Bit 4
  PIN_AB5 = 12;  // Address Bus Bit 5
  PIN_AB6 = 13;  // Address Bus Bit 6
  PIN_AB7 = 14;  // Address Bus Bit 7
  PIN_AB8 = 15;  // Address Bus Bit 8
  PIN_AB9 = 16;  // Address Bus Bit 9
  PIN_AB10 = 17;  // Address Bus Bit 10
  PIN_AB11 = 18;  // Address Bus Bit 11
  PIN_AB12 = 19;  // Address Bus Bit 12
  PIN_AB13 = 20;  // Address Bus Bit 13
  PIN_AB14 = 21;  // Address Bus Bit 14
  PIN_AB15 = 22;  // Address Bus Bit 15
  PIN_AB16 = 23;  // Address Bus Bit 16
  PIN_AB17 = 24;  // Address Bus Bit 17
  PIN_AB18 = 25;  // Address Bus Bit 18
  PIN_AB19 = 26;  // Address Bus Bit 19
  PIN_AB20 = 27;  // Address Bus Bit 20
  PIN_AB21 = 28;  // Address Bus Bit 21
  PIN_AB22 = 29;  // Address Bus Bit 22
  PIN_AB23 = 30;  // Address Bus Bit 23
  PIN_AB24 = 31;  // Address Bus Bit 24
  PIN_AB25 = 32;  // Address Bus Bit 25
  PIN_AB26 = 33;  // Address Bus Bit 26
  PIN_AB27 = 34;  // Address Bus Bit 27
  PIN_AB28 = 35;  // Address Bus Bit 28
  PIN_AB29 = 36;  // Address Bus Bit 29
  PIN_AB30 = 37;  // Address Bus Bit 30
  PIN_AB31 = 38;  // Address Bus Bit 31
  PIN_DB0 = 39;  // Data Bus Bit 0
  PIN_DB1 = 40;  // Data Bus Bit 1
  PIN_DB2 = 41;  // Data Bus Bit 2
  PIN_DB3 = 42;  // Data Bus Bit 3
  PIN_DB4 = 43;  // Data Bus Bit 4
  PIN_DB5 = 44;  // Data Bus Bit 5
  PIN_DB6 = 45;  // Data Bus Bit 6
  PIN_DB7 = 46;  // Data Bus Bit 7
  PIN_DB8 = 47;  // Data Bus Bit 8
  PIN_DB9 = 48;  // Data Bus Bit 9
  PIN_DB10 = 49;  // Data Bus Bit 10
  PIN_DB11 = 50;  // Data Bus Bit 11
  PIN_DB12 = 51;  // Data Bus Bit 12
  PIN_DB13 = 52;  // Data Bus Bit 13
  PIN_DB14 = 53;  // Data Bus Bit 14
  PIN_DB15 = 54;  // Data Bus Bit 15
  PIN_DB16 = 55;  // Data Bus Bit 16
  PIN_DB17 = 56;  // Data Bus Bit 17
  PIN_DB18 = 57;  // Data Bus Bit 18
  PIN_DB19 = 58;  // Data Bus Bit 19
  PIN_DB20 = 59;  // Data Bus Bit 20
  PIN_DB21 = 60;  // Data Bus Bit 21
  PIN_DB22 = 61;  // Data Bus Bit 22
  PIN_DB23 = 62;  // Data Bus Bit 23
  PIN_DB24 = 63;  // Data Bus Bit 24
  PIN_DB25 = 64;  // Data Bus Bit 25
  PIN_DB26 = 65;  // Data Bus Bit 26
  PIN_DB27 = 66;  // Data Bus Bit 27
  PIN_DB28 = 67;  // Data Bus Bit 28
  PIN_DB29 = 68;  // Data Bus Bit 29
  PIN_DB30 = 69;  // Data Bus Bit 30
  PIN_DB31 = 70;  // Data Bus Bit 31
  PIN_NCS0 = 71;  // Chip Select 0 (ROM)
  PIN_NCS1 = 72;  // Chip Select 1 (RAM)
  PIN_NCS2 = 73;  // Chip Select 2 (I/O)
  PIN_NWR = 74;  // Write Enable
  PIN_NRD = 75;  // Read Enable
  PIN_BE0 = 76;  // Byte Enable 0 (bits 0-7)
  PIN_BE1 = 77;  // Byte Enable 1 (bits 8-15)
  PIN_BE2 = 78;  // Byte Enable 2 (bits 16-23)
  PIN_BE3 = 79;  // Byte Enable 3 (bits 24-31)
  PIN_BUSREQ = 80;  // Bus Request (from external DMA)
  PIN_BUSACK = 81;  // Bus Acknowledge
  PIN_INT0 = 82;  // Interrupt 0 (V-Blank)
  PIN_INT1 = 83;  // Interrupt 1 (GPU)
  PIN_INT2 = 84;  // Interrupt 2 (CD-ROM)
  PIN_INT3 = 85;  // Interrupt 3 (DMA)
  PIN_INT4 = 86;  // Interrupt 4 (Timer)
  PIN_INT5 = 87;  // Interrupt 5 (SIO)
  PIN_AUDIO_L = 88;  // Audio Output Left
  PIN_AUDIO_R = 89;  // Audio Output Right
  PIN_VIDEO_R = 90;  // Video Output Red (analog RGB)
  PIN_VIDEO_G = 91;  // Video Output Green
  PIN_VIDEO_B = 92;  // Video Output Blue
  PIN_SYNC = 93;  // Video Sync / Composite

type
  TMIPS-R3000A = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure mips_r3000a_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure mips_r3000a_init;
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
