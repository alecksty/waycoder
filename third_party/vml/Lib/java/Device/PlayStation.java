package vml.device.sonymipstechnologies.mips_r3000a;

/**
 * MIPS-R3000A 寄存器定义
 * 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
 * 版本: 1.0
 */
public final class MIPS_R3000A {
    private MIPS_R3000A() {} // 工具类
    // CPU架构: MIPS-R3000A, 32位, 33870000 Hz

    // 寄存器定义
    // Hard-wired Zero
    public static final int R0_ADDR = (int)0x00;

    // Assembler Temporary
    public static final int R1_ADDR = (int)0x04;

    // Value Returned by Subroutines
    public static final int R2_ADDR = (int)0x08;

    // Expression Evaluation
    public static final int R3_ADDR = (int)0x0C;

    // Expression Evaluation
    public static final int R4_ADDR = (int)0x10;

    // Expression Evaluation
    public static final int R5_ADDR = (int)0x14;

    // Expression Evaluation
    public static final int R6_ADDR = (int)0x18;

    // Expression Evaluation
    public static final int R7_ADDR = (int)0x1C;

    // Expression Evaluation
    public static final int R8_ADDR = (int)0x20;

    // Expression Evaluation
    public static final int R9_ADDR = (int)0x24;

    // Expression Evaluation
    public static final int R10_ADDR = (int)0x28;

    // Expression Evaluation
    public static final int R11_ADDR = (int)0x2C;

    // Expression Evaluation
    public static final int R12_ADDR = (int)0x30;

    // Expression Evaluation
    public static final int R13_ADDR = (int)0x34;

    // Expression Evaluation
    public static final int R14_ADDR = (int)0x38;

    // Expression Evaluation
    public static final int R15_ADDR = (int)0x3C;

    // Saved Value
    public static final int R16_ADDR = (int)0x40;

    // Saved Value
    public static final int R17_ADDR = (int)0x44;

    // Saved Value
    public static final int R18_ADDR = (int)0x48;

    // Saved Value
    public static final int R19_ADDR = (int)0x4C;

    // Saved Value
    public static final int R20_ADDR = (int)0x50;

    // Saved Value
    public static final int R21_ADDR = (int)0x54;

    // Saved Value
    public static final int R22_ADDR = (int)0x58;

    // Saved Value
    public static final int R23_ADDR = (int)0x5C;

    // Temporary
    public static final int R24_ADDR = (int)0x60;

    // Temporary
    public static final int R25_ADDR = (int)0x64;

    // Kernel Reserved
    public static final int R26_ADDR = (int)0x68;

    // Kernel Reserved
    public static final int R27_ADDR = (int)0x6C;

    // Global Pointer
    public static final int R28_ADDR = (int)0x70;

    // Stack Pointer
    public static final int R29_ADDR = (int)0x74;

    // Frame Pointer
    public static final int R30_ADDR = (int)0x78;

    // Return Address
    public static final int R31_ADDR = (int)0x7C;

    // Multiply/Divide High
    public static final int HI_ADDR = (int)0x80;

    // Multiply/Divide Low
    public static final int LO_ADDR = (int)0x84;

    // Program Counter
    public static final int PC_ADDR = (int)0x88;

    // Coprocessor 0 - Status Register
    public static final int CP0_SR_ADDR = (int)0x90;
    public static final int CP0_SR_IE = 0;  // Interrupt Enable
    public static final int CP0_SR_EXL = 1;  // Exception Level
    public static final int CP0_SR_ERL = 2;  // Error Level
    public static final int CP0_SR_KSU = 0;  // Kernel/User Mode
    public static final int CP0_SR_IM0_7 = 0;  // Interrupt Mask bits
    public static final int CP0_SR_CU0 = 28;  // Coprocessor 0 Usable
    public static final int CP0_SR_BEV = 22;  // Bootstrap Exception Vector

    // Coprocessor 0 - Cause Register
    public static final int CP0_CAUSE_ADDR = (int)0x94;
    public static final int CP0_CAUSE_EXCCODE = 0;  // Exception Code
    public static final int CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending bits

    // Coprocessor 0 - Exception PC
    public static final int CP0_EPC_ADDR = (int)0x98;

    // Coprocessor 0 - Bad Virtual Address
    public static final int CP0_BADVADDR_ADDR = (int)0x9C;

    // Coprocessor 0 - Context Register
    public static final int CP0_CONTEXT_ADDR = (int)0xA0;

    // Coprocessor 0 - Process ID
    public static final int CP0_PID_ADDR = (int)0xA4;

    // 内存段定义
    // KSEG0 - Cached RAM (2MB System RAM)
    public static final int KSEG0_START = (int)0x80000000;
    public static final int KSEG0_END = (int)0x801FFFFF;
    public static final int KSEG0_SIZE = 2097152;

    // KSEG1 - Uncached RAM (2MB System RAM)
    public static final int KSEG1_START = (int)0xA0000000;
    public static final int KSEG1_END = (int)0xA01FFFFF;
    public static final int KSEG1_SIZE = 2097152;

    // VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
    public static final int VRAM_START = (int)0xA0000000;
    public static final int VRAM_END = (int)0xA01FFFFF;
    public static final int VRAM_SIZE = 1048576;

    // Expansion Region (maps to expansion RAM area)
    public static final int EXPANSION_START = (int)0xA0000000;
    public static final int EXPANSION_END = (int)0xA00FFFFF;
    public static final int EXPANSION_SIZE = 1048576;

    // Data Scratchpad (1KB)
    public static final int SCRATCHPAD_START = (int)0x1F800000;
    public static final int SCRATCHPAD_END = (int)0x1F8003FF;
    public static final int SCRATCHPAD_SIZE = 1024;

    // Kernel BIOS ROM (512KB)
    public static final int EXP_ROM_START = (int)0x1FC00000;
    public static final int EXP_ROM_END = (int)0x1FC7FFFF;
    public static final int EXP_ROM_SIZE = 524288;

    // User ROM / Kernel Expansion
    public static final int USER_ROM_START = (int)0x1F800000;
    public static final int USER_ROM_END = (int)0x1FBFFFFF;
    public static final int USER_ROM_SIZE = 4194304;

    // I/O Register Area (Expansion 1)
    public static final int MMIO_START = (int)0x1F801000;
    public static final int MMIO_END = (int)0x1F802FFF;
    public static final int MMIO_SIZE = 8192;

    // GPU Registers
    public static final int GPU_START = (int)0x1F801810;
    public static final int GPU_END = (int)0x1F801817;
    public static final int GPU_SIZE = 8;

    // CD-ROM Registers
    public static final int CDROM_START = (int)0x1F801800;
    public static final int CDROM_END = (int)0x1F80180F;
    public static final int CDROM_SIZE = 16;

    // SPU Registers
    public static final int SPU_START = (int)0x1F801C00;
    public static final int SPU_END = (int)0x1F801DFF;
    public static final int SPU_SIZE = 512;

    // Interrupt Control
    public static final int IRQ_START = (int)0x1F801070;
    public static final int IRQ_END = (int)0x1F801077;
    public static final int IRQ_SIZE = 8;

    // DMA Registers (7 channels)
    public static final int DMA_START = (int)0x1F801080;
    public static final int DMA_END = (int)0x1F8010FF;
    public static final int DMA_SIZE = 128;

    // Timer Registers
    public static final int TIMER_START = (int)0x1F801100;
    public static final int TIMER_END = (int)0x1F80112F;
    public static final int TIMER_SIZE = 48;

    // JOY Interface Registers
    public static final int JOY_START = (int)0x1F801040;
    public static final int JOY_END = (int)0x1F80104F;
    public static final int JOY_SIZE = 16;

    // MDEC (Motion Decoder) Registers
    public static final int MDEC_START = (int)0x1F801820;
    public static final int MDEC_END = (int)0x1F801827;
    public static final int MDEC_SIZE = 8;

    // SIO Registers
    public static final int SIO_START = (int)0x1F801050;
    public static final int SIO_END = (int)0x1F80105F;
    public static final int SIO_SIZE = 16;

    // GPU Status
    public static final int GPU_STAT_START = (int)0x1F801814;
    public static final int GPU_STAT_END = (int)0x1F801817;
    public static final int GPU_STAT_SIZE = 4;

    // CD-ROM Status
    public static final int CDROM_STAT_START = (int)0x1F801801;
    public static final int CDROM_STAT_END = (int)0x1F801803;
    public static final int CDROM_STAT_SIZE = 3;

    // SPU Work RAM (1KB)
    public static final int SPU_RAM_START = (int)0x1F800000;
    public static final int SPU_RAM_END = (int)0x1F800FFF;
    public static final int SPU_RAM_SIZE = 1024;

    // 外设定义
    // Graphics Processing Unit
    public static final int GPU_BASE = (int)0x1F801810;
    public static final int GPU_GP0_CMD = (int)0x1F801810;
    public static final int GPU_GP0_DATA = (int)0x1F801814;
    public static final int GPU_GP1_CMD = (int)0x1F801818;
    public static final int GPU_GP1_DATA = (int)0x1F80181C;
    public static final int GPU_TPAGE = (int)0x1F801810;
    public static final int GPU_DRAW_MODE = (int)0x1F801811;
    public static final int GPU_TEXTURE_WIN = (int)0x1F801812;
    public static final int GPU_DRAW_OFFSET_X = (int)0x1F801813;
    public static final int GPU_DRAW_OFFSET_Y = (int)0x1F801814;
    public static final int GPU_DRAW_AREA_X = (int)0x1F801815;
    public static final int GPU_DRAW_AREA_Y = (int)0x1F801816;
    public static final int GPU_DITHER = (int)0x1F801817;
    public static final int GPU_DISPLAY_MODE = (int)0x1F801818;
    public static final int GPU_DISPLAY_START_X = (int)0x1F801819;
    public static final int GPU_DISPLAY_START_Y = (int)0x1F80181A;
    public static final int GPU_DISPLAY_HORZ = (int)0x1F80181B;
    public static final int GPU_DISPLAY_VERT = (int)0x1F80181C;
    public static final int GPU_DMA_MODE = (int)0x1F80181D;
    public static final int GPU_GPU_STAT = (int)0x1F80181C;
    public static final int GPU_GPU_STAT_READY_CMD = 0;  // GPU Ready to Receive Command
    public static final int GPU_GPU_STAT_READY_DMA = 1;  // GPU Ready for DMA
    public static final int GPU_GPU_STAT_DRAWING = 2;  // Drawing Busy
    public static final int GPU_GPU_STAT_DMA_REQ = 3;  // DMA Request
    public static final int GPU_GPU_STAT_COMMAND_BUSY = 4;  // Command Busy
    public static final int GPU_GPU_STAT_DISPLAY_DISABLE = 5;  // Display Disable
    public static final int GPU_GPU_STAT_INTERRUPT = 24;  // V-Blank Interrupt Flag

    // Geometry Transformation Engine
    public static final int GTE_BASE = (int)0x1F801880;
    public static final int GTE_GTE_VXY0 = (int)0x1F801880;
    public static final int GTE_GTE_VZ0 = (int)0x1F801884;
    public static final int GTE_GTE_VXY1 = (int)0x1F801888;
    public static final int GTE_GTE_VZ1 = (int)0x1F80188C;
    public static final int GTE_GTE_VXY2 = (int)0x1F801890;
    public static final int GTE_GTE_VZ2 = (int)0x1F801894;
    public static final int GTE_GTE_RGB0 = (int)0x1F801898;
    public static final int GTE_GTE_RGB1 = (int)0x1F80189C;
    public static final int GTE_GTE_RGB2 = (int)0x1F8018A0;
    public static final int GTE_GTE_RTP = (int)0x1F8018B0;
    public static final int GTE_GTE_TRX = (int)0x1F8018B4;
    public static final int GTE_GTE_TRY = (int)0x1F8018B8;
    public static final int GTE_GTE_TRZ = (int)0x1F8018BC;
    public static final int GTE_GTE_MAC0 = (int)0x1F8018C0;
    public static final int GTE_GTE_MAC1 = (int)0x1F8018C4;
    public static final int GTE_GTE_MAC2 = (int)0x1F8018C8;
    public static final int GTE_GTE_MAC3 = (int)0x1F8018CC;
    public static final int GTE_GTE_IR0 = (int)0x1F8018D0;
    public static final int GTE_GTE_IR1 = (int)0x1F8018D4;
    public static final int GTE_GTE_IR2 = (int)0x1F8018D8;
    public static final int GTE_GTE_IR3 = (int)0x1F8018DC;
    public static final int GTE_GTE_LZCS = (int)0x1F8018E0;
    public static final int GTE_GTE_LZCR = (int)0x1F8018E4;
    public static final int GTE_GTE_CTX = (int)0x1F8018E8;
    public static final int GTE_GTE_CTY = (int)0x1F8018EC;
    public static final int GTE_GTE_CTZ = (int)0x1F8018F0;
    public static final int GTE_GTE_RTX = (int)0x1F8018F4;
    public static final int GTE_GTE_RTY = (int)0x1F8018F8;
    public static final int GTE_GTE_RTZ = (int)0x1F8018FC;
    public static final int GTE_GTE_SR = (int)0x1F801900;
    public static final int GTE_GTE_CMD = (int)0x1F801904;
    public static final int GTE_GTE_H = (int)0x1F801908;
    public static final int GTE_GTE_DQB = (int)0x1F80190C;
    public static final int GTE_GTE_DQA = (int)0x1F801910;
    public static final int GTE_GTE_ZSF3 = (int)0x1F801914;
    public static final int GTE_GTE_ZSF4 = (int)0x1F801918;
    public static final int GTE_GTE_OTZ = (int)0x1F80191C;

    // Sound Processing Unit (24-channel ADPCM)
    public static final int SPU_BASE = (int)0x1F801C00;
    public static final int SPU_SPU_CTRL = (int)0x1F801C00;
    public static final int SPU_SPU_CTRL_REVERB_MASTER = 0;  // Reverb Master Enable
    public static final int SPU_SPU_CTRL_IRQ9 = 9;  // Interrupt Request Enable
    public static final int SPU_SPU_STAT = (int)0x1F801C04;
    public static final int SPU_SPU_CDVOL_L = (int)0x1F801C08;
    public static final int SPU_SPU_CDVOL_R = (int)0x1F801C0A;
    public static final int SPU_SPU_MAINVOL_L = (int)0x1F801C0C;
    public static final int SPU_SPU_MAINVOL_R = (int)0x1F801C0E;
    public static final int SPU_SPU_REVERB_L = (int)0x1F801C10;
    public static final int SPU_SPU_REVERB_R = (int)0x1F801C12;
    public static final int SPU_SPU_KEYON = (int)0x1F801C80;
    public static final int SPU_SPU_KEYOFF = (int)0x1F801C82;
    public static final int SPU_SPU_CHANNEL_MUTE = (int)0x1F801C84;
    public static final int SPU_SPU_NOISE_CLK = (int)0x1F801C88;
    public static final int SPU_SPU_REVERB_ADDR = (int)0x1F801C8A;
    public static final int SPU_SPU_IRQ_ADDR = (int)0x1F801C8C;
    public static final int SPU_SPU_REVERB_VOL_L = (int)0x1F801C8E;
    public static final int SPU_SPU_REVERB_VOL_R = (int)0x1F801C90;
    public static final int SPU_SPU_VOICE_VOL_L = (int)0x1F801C00;
    public static final int SPU_SPU_VOICE_VOL_R = (int)0x1F801C01;
    public static final int SPU_SPU_VOICE_FREQ = (int)0x1F801C02;
    public static final int SPU_SPU_VOICE_START = (int)0x1F801C04;
    public static final int SPU_SPU_VOICE_ADSR1 = (int)0x1F801C06;
    public static final int SPU_SPU_VOICE_ADSR2 = (int)0x1F801C08;
    public static final int SPU_SPU_VOICE_ENV = (int)0x1F801C0A;
    public static final int SPU_SPU_VOICE_REPEAT = (int)0x1F801C0C;
    public static final int SPU_VOICE_BASE_SIZE = (int)0x1F801C10;

    // Motion Decoder (JPEG Decompression)
    public static final int MDEC_BASE = (int)0x1F801820;
    public static final int MDEC_MDEC_CTRL = (int)0x1F801820;
    public static final int MDEC_MDEC_CTRL_DATA_IN_SIZE = 0;  // Data-in size in words
    public static final int MDEC_MDEC_CTRL_RESET = 16;  // Reset MDEC
    public static final int MDEC_MDEC_CTRL_BUSY = 17;  // MDEC Busy
    public static final int MDEC_MDEC_DATA = (int)0x1F801824;
    public static final int MDEC_MDEC_BKGD = (int)0x1F801828;

    // DMA Controller (7 channels)
    public static final int DMA_BASE = (int)0x1F801080;
    public static final int DMA_DMA_DPCR = (int)0x1F801080;
    public static final int DMA_DMA_DPCR_CH0_EN = 0;  // Channel 0 Enable
    public static final int DMA_DMA_DPCR_CH1_EN = 4;  // Channel 1 Enable
    public static final int DMA_DMA_DPCR_CH2_EN = 8;  // Channel 2 Enable
    public static final int DMA_DMA_DPCR_CH3_EN = 12;  // Channel 3 Enable
    public static final int DMA_DMA_DPCR_CH4_EN = 16;  // Channel 4 Enable
    public static final int DMA_DMA_DPCR_CH5_EN = 20;  // Channel 5 Enable
    public static final int DMA_DMA_DPCR_CH6_EN = 24;  // Channel 6 Enable
    public static final int DMA_DMA_INT = (int)0x1F801084;
    public static final int DMA_DMA_CH0_BASE = (int)0x1F801090;
    public static final int DMA_DMA_CH0_COUNT = (int)0x1F801094;
    public static final int DMA_DMA_CH0_CTRL = (int)0x1F801098;
    public static final int DMA_DMA_CH0_CTRL_DEST_DIR = 0;  // Destination Direction
    public static final int DMA_DMA_CH0_CTRL_SRC_DIR = 0;  // Source Direction
    public static final int DMA_DMA_CH0_CTRL_STEPS = 0;  // Step
    public static final int DMA_DMA_CH0_CTRL_CHAIN = 0;  // Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
    public static final int DMA_DMA_CH0_CTRL_SYNC = 0;  // Sync Mode (0=immediate, 1=request, 2=linked-list)
    public static final int DMA_DMA_CH0_CTRL_TRIGGER = 10;  // Trigger
    public static final int DMA_DMA_CH1_BASE = (int)0x1F8010A0;
    public static final int DMA_DMA_CH1_COUNT = (int)0x1F8010A4;
    public static final int DMA_DMA_CH1_CTRL = (int)0x1F8010A8;
    public static final int DMA_DMA_CH2_BASE = (int)0x1F8010B0;
    public static final int DMA_DMA_CH2_COUNT = (int)0x1F8010B4;
    public static final int DMA_DMA_CH2_CTRL = (int)0x1F8010B8;
    public static final int DMA_DMA_CH3_BASE = (int)0x1F8010C0;
    public static final int DMA_DMA_CH3_COUNT = (int)0x1F8010C4;
    public static final int DMA_DMA_CH3_CTRL = (int)0x1F8010C8;
    public static final int DMA_DMA_CH4_BASE = (int)0x1F8010D0;
    public static final int DMA_DMA_CH4_COUNT = (int)0x1F8010D4;
    public static final int DMA_DMA_CH4_CTRL = (int)0x1F8010D8;
    public static final int DMA_DMA_CH5_BASE = (int)0x1F8010E0;
    public static final int DMA_DMA_CH5_COUNT = (int)0x1F8010E4;
    public static final int DMA_DMA_CH5_CTRL = (int)0x1F8010E8;
    public static final int DMA_DMA_CH6_BASE = (int)0x1F8010F0;
    public static final int DMA_DMA_CH6_COUNT = (int)0x1F8010F4;
    public static final int DMA_DMA_CH6_CTRL = (int)0x1F8010F8;

    // Timers (3 timers)
    public static final int TIMER_BASE = (int)0x1F801100;
    public static final int TIMER_TM0_COUNT = (int)0x1F801100;
    public static final int TIMER_TM0_MODE = (int)0x1F801104;
    public static final int TIMER_TM0_MODE_RELOAD = 0;  // Reload Enable
    public static final int TIMER_TM0_MODE_CLOCK = 0;  // Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
    public static final int TIMER_TM0_MODE_IRQ_EN = 3;  // IRQ Enable
    public static final int TIMER_TM0_MODE_IRQ_REPEAT = 4;  // IRQ Repeat
    public static final int TIMER_TM0_MODE_IRQ_TOGGLE = 5;  // IRQ Toggle Mode
    public static final int TIMER_TM0_MODE_REACH_MAX = 6;  // Reached Max Value
    public static final int TIMER_TM0_TARGET = (int)0x1F801108;
    public static final int TIMER_TM1_COUNT = (int)0x1F801110;
    public static final int TIMER_TM1_MODE = (int)0x1F801114;
    public static final int TIMER_TM1_TARGET = (int)0x1F801118;
    public static final int TIMER_TM2_COUNT = (int)0x1F801120;
    public static final int TIMER_TM2_MODE = (int)0x1F801124;
    public static final int TIMER_TM2_TARGET = (int)0x1F801128;

    // CD-ROM Controller
    public static final int CDROM_BASE = (int)0x1F801800;
    public static final int CDROM_CD0_DATA = (int)0x1F801800;
    public static final int CDROM_CD0_STATUS = (int)0x1F801801;
    public static final int CDROM_CD0_RESPONSE = (int)0x1F801802;
    public static final int CDROM_CD0_DATA1 = (int)0x1F801803;
    public static final int CDROM_CD0_INT_FLAG = (int)0x1F801804;
    public static final int CDROM_CD0_INT_FLAG_INT1 = 0;  // Data Ready
    public static final int CDROM_CD0_INT_FLAG_INT2 = 1;  // Command Complete
    public static final int CDROM_CD0_INT_FLAG_INT3 = 2;  // Acknowledge Received
    public static final int CDROM_CD0_INT_FLAG_INT4 = 3;  // Error / N-Complete
    public static final int CDROM_CD0_VOLUME_L = (int)0x1F801808;
    public static final int CDROM_CD0_VOLUME_R = (int)0x1F801809;

    // JOY Interface
    public static final int JOY_BASE = (int)0x1F801040;
    public static final int JOY_JOY_CTRL = (int)0x1F801040;
    public static final int JOY_JOY_CTRL_TX_EN = 0;  // Transmit Enable
    public static final int JOY_JOY_CTRL_RX_EN = 1;  // Receive Enable
    public static final int JOY_JOY_CTRL_CLOCK = 3;  // Internal/External Clock
    public static final int JOY_JOY_CTRL_IRQ_EN = 4;  // IRQ Enable
    public static final int JOY_JOY_MODE = (int)0x1F801041;
    public static final int JOY_JOY_BAUD = (int)0x1F801042;
    public static final int JOY_JOY_TX_DATA = (int)0x1F801044;
    public static final int JOY_JOY_RX_DATA = (int)0x1F801045;
    public static final int JOY_JOY_STAT = (int)0x1F801046;
    public static final int JOY_JOY_STAT_TX_EMPTY = 0;  // Transmit Buffer Empty
    public static final int JOY_JOY_STAT_RX_READY = 2;  // Receive Data Ready
    public static final int JOY_JOY_STAT_TX_IRQ = 3;  // Transmit IRQ Pending
    public static final int JOY_JOY_STAT_RX_IRQ = 4;  // Receive IRQ Pending

    // SIO (Serial I/O - Memory Card)
    public static final int SIO_BASE = (int)0x1F801050;
    public static final int SIO_SIO_DATA = (int)0x1F801050;
    public static final int SIO_SIO_STATUS = (int)0x1F801051;
    public static final int SIO_SIO_MODE = (int)0x1F801052;
    public static final int SIO_SIO_CTRL = (int)0x1F801053;
    public static final int SIO_SIO_BAUD = (int)0x1F801054;

    // Interrupt Controller
    public static final int INTERRUPT_BASE = (int)0x1F801070;
    public static final int INTERRUPT_INT_STAT = (int)0x1F801070;
    public static final int INTERRUPT_INT_MASK = (int)0x1F801074;
    public static final int INTERRUPT_INT_MASK_VBLANK = 0;  // V-Blank Interrupt
    public static final int INTERRUPT_INT_MASK_GPU = 1;  // GPU Interrupt
    public static final int INTERRUPT_INT_MASK_CDROM = 2;  // CD-ROM Interrupt
    public static final int INTERRUPT_INT_MASK_DMA0 = 3;  // DMA Channel 0
    public static final int INTERRUPT_INT_MASK_DMA1 = 4;  // DMA Channel 1
    public static final int INTERRUPT_INT_MASK_DMA2 = 5;  // DMA Channel 2
    public static final int INTERRUPT_INT_MASK_DMA3 = 6;  // DMA Channel 3
    public static final int INTERRUPT_INT_MASK_DMA4 = 7;  // DMA Channel 4
    public static final int INTERRUPT_INT_MASK_DMA5 = 8;  // DMA Channel 5
    public static final int INTERRUPT_INT_MASK_DMA6 = 9;  // DMA Channel 6
    public static final int INTERRUPT_INT_MASK_TIMER0 = 10;  // Timer 0
    public static final int INTERRUPT_INT_MASK_TIMER1 = 11;  // Timer 1
    public static final int INTERRUPT_INT_MASK_TIMER2 = 12;  // Timer 2
    public static final int INTERRUPT_INT_MASK_SIO = 13;  // SIO / Memory Card
    public static final int INTERRUPT_INT_MASK_SPU = 14;  // SPU Interrupt
    public static final int INTERRUPT_INT_MASK_PIO = 15;  // PIO (Expansion)

    // 中断向量定义
    public static final int IRQ_VBLANK = 0;  // V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
    public static final int IRQ_GPU = 1;  // GPU Interrupt (drawing complete / V-Blank)
    public static final int IRQ_CDROM = 2;  // CD-ROM Interrupt
    public static final int IRQ_DMA0 = 3;  // DMA Channel 0 Complete
    public static final int IRQ_DMA1 = 4;  // DMA Channel 1 Complete
    public static final int IRQ_DMA2 = 5;  // DMA Channel 2 Complete
    public static final int IRQ_DMA3 = 6;  // DMA Channel 3 Complete
    public static final int IRQ_DMA4 = 7;  // DMA Channel 4 Complete
    public static final int IRQ_DMA5 = 8;  // DMA Channel 5 Complete
    public static final int IRQ_DMA6 = 9;  // DMA Channel 6 Complete
    public static final int IRQ_TIMER0 = 10;  // Timer 0 Interrupt
    public static final int IRQ_TIMER1 = 11;  // Timer 1 Interrupt
    public static final int IRQ_TIMER2 = 12;  // Timer 2 Interrupt
    public static final int IRQ_SIO = 13;  // SIO / Memory Card Interrupt
    public static final int IRQ_SPU = 14;  // SPU Interrupt
    public static final int IRQ_PIO = 15;  // PIO / Expansion Interrupt

    // 引脚定义
    public static final int PIN_VCC = 1;  // Power Supply (3.3V regulated)
    public static final int PIN_VSS = 2;  // Ground
    public static final int PIN_CLK = 3;  // System Clock Input (53.6932MHz / 2 = 26.8466MHz bus)
    public static final int PIN_RESET = 4;  // Reset (active low)
    public static final int PIN_NMI = 5;  // Non-Maskable Interrupt
    public static final int PIN_IRQ = 6;  // Interrupt Request
    public static final int PIN_AB0 = 7;  // Address Bus Bit 0
    public static final int PIN_AB1 = 8;  // Address Bus Bit 1
    public static final int PIN_AB2 = 9;  // Address Bus Bit 2
    public static final int PIN_AB3 = 10;  // Address Bus Bit 3
    public static final int PIN_AB4 = 11;  // Address Bus Bit 4
    public static final int PIN_AB5 = 12;  // Address Bus Bit 5
    public static final int PIN_AB6 = 13;  // Address Bus Bit 6
    public static final int PIN_AB7 = 14;  // Address Bus Bit 7
    public static final int PIN_AB8 = 15;  // Address Bus Bit 8
    public static final int PIN_AB9 = 16;  // Address Bus Bit 9
    public static final int PIN_AB10 = 17;  // Address Bus Bit 10
    public static final int PIN_AB11 = 18;  // Address Bus Bit 11
    public static final int PIN_AB12 = 19;  // Address Bus Bit 12
    public static final int PIN_AB13 = 20;  // Address Bus Bit 13
    public static final int PIN_AB14 = 21;  // Address Bus Bit 14
    public static final int PIN_AB15 = 22;  // Address Bus Bit 15
    public static final int PIN_AB16 = 23;  // Address Bus Bit 16
    public static final int PIN_AB17 = 24;  // Address Bus Bit 17
    public static final int PIN_AB18 = 25;  // Address Bus Bit 18
    public static final int PIN_AB19 = 26;  // Address Bus Bit 19
    public static final int PIN_AB20 = 27;  // Address Bus Bit 20
    public static final int PIN_AB21 = 28;  // Address Bus Bit 21
    public static final int PIN_AB22 = 29;  // Address Bus Bit 22
    public static final int PIN_AB23 = 30;  // Address Bus Bit 23
    public static final int PIN_AB24 = 31;  // Address Bus Bit 24
    public static final int PIN_AB25 = 32;  // Address Bus Bit 25
    public static final int PIN_AB26 = 33;  // Address Bus Bit 26
    public static final int PIN_AB27 = 34;  // Address Bus Bit 27
    public static final int PIN_AB28 = 35;  // Address Bus Bit 28
    public static final int PIN_AB29 = 36;  // Address Bus Bit 29
    public static final int PIN_AB30 = 37;  // Address Bus Bit 30
    public static final int PIN_AB31 = 38;  // Address Bus Bit 31
    public static final int PIN_DB0 = 39;  // Data Bus Bit 0
    public static final int PIN_DB1 = 40;  // Data Bus Bit 1
    public static final int PIN_DB2 = 41;  // Data Bus Bit 2
    public static final int PIN_DB3 = 42;  // Data Bus Bit 3
    public static final int PIN_DB4 = 43;  // Data Bus Bit 4
    public static final int PIN_DB5 = 44;  // Data Bus Bit 5
    public static final int PIN_DB6 = 45;  // Data Bus Bit 6
    public static final int PIN_DB7 = 46;  // Data Bus Bit 7
    public static final int PIN_DB8 = 47;  // Data Bus Bit 8
    public static final int PIN_DB9 = 48;  // Data Bus Bit 9
    public static final int PIN_DB10 = 49;  // Data Bus Bit 10
    public static final int PIN_DB11 = 50;  // Data Bus Bit 11
    public static final int PIN_DB12 = 51;  // Data Bus Bit 12
    public static final int PIN_DB13 = 52;  // Data Bus Bit 13
    public static final int PIN_DB14 = 53;  // Data Bus Bit 14
    public static final int PIN_DB15 = 54;  // Data Bus Bit 15
    public static final int PIN_DB16 = 55;  // Data Bus Bit 16
    public static final int PIN_DB17 = 56;  // Data Bus Bit 17
    public static final int PIN_DB18 = 57;  // Data Bus Bit 18
    public static final int PIN_DB19 = 58;  // Data Bus Bit 19
    public static final int PIN_DB20 = 59;  // Data Bus Bit 20
    public static final int PIN_DB21 = 60;  // Data Bus Bit 21
    public static final int PIN_DB22 = 61;  // Data Bus Bit 22
    public static final int PIN_DB23 = 62;  // Data Bus Bit 23
    public static final int PIN_DB24 = 63;  // Data Bus Bit 24
    public static final int PIN_DB25 = 64;  // Data Bus Bit 25
    public static final int PIN_DB26 = 65;  // Data Bus Bit 26
    public static final int PIN_DB27 = 66;  // Data Bus Bit 27
    public static final int PIN_DB28 = 67;  // Data Bus Bit 28
    public static final int PIN_DB29 = 68;  // Data Bus Bit 29
    public static final int PIN_DB30 = 69;  // Data Bus Bit 30
    public static final int PIN_DB31 = 70;  // Data Bus Bit 31
    public static final int PIN_NCS0 = 71;  // Chip Select 0 (ROM)
    public static final int PIN_NCS1 = 72;  // Chip Select 1 (RAM)
    public static final int PIN_NCS2 = 73;  // Chip Select 2 (I/O)
    public static final int PIN_NWR = 74;  // Write Enable
    public static final int PIN_NRD = 75;  // Read Enable
    public static final int PIN_BE0 = 76;  // Byte Enable 0 (bits 0-7)
    public static final int PIN_BE1 = 77;  // Byte Enable 1 (bits 8-15)
    public static final int PIN_BE2 = 78;  // Byte Enable 2 (bits 16-23)
    public static final int PIN_BE3 = 79;  // Byte Enable 3 (bits 24-31)
    public static final int PIN_BUSREQ = 80;  // Bus Request (from external DMA)
    public static final int PIN_BUSACK = 81;  // Bus Acknowledge
    public static final int PIN_INT0 = 82;  // Interrupt 0 (V-Blank)
    public static final int PIN_INT1 = 83;  // Interrupt 1 (GPU)
    public static final int PIN_INT2 = 84;  // Interrupt 2 (CD-ROM)
    public static final int PIN_INT3 = 85;  // Interrupt 3 (DMA)
    public static final int PIN_INT4 = 86;  // Interrupt 4 (Timer)
    public static final int PIN_INT5 = 87;  // Interrupt 5 (SIO)
    public static final int PIN_AUDIO_L = 88;  // Audio Output Left
    public static final int PIN_AUDIO_R = 89;  // Audio Output Right
    public static final int PIN_VIDEO_R = 90;  // Video Output Red (analog RGB)
    public static final int PIN_VIDEO_G = 91;  // Video Output Green
    public static final int PIN_VIDEO_B = 92;  // Video Output Blue
    public static final int PIN_SYNC = 93;  // Video Sync / Composite

    public static native void mips_r3000a_init();
}
