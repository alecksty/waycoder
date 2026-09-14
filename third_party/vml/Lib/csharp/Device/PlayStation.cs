using System;

namespace VML.Device.Sony/MIPSTechnologies.MIPS_R3000A
{
    /// <summary>
    /// MIPS-R3000A 寄存器定义
    /// 生成自: Sony / MIPS Technologies/MIPS-I/MIPS-R3000A
    /// 版本: 1.0
    /// </summary>
    public static class MIPS_R3000A
    {
        // CPU架构: MIPS-R3000A, 32位, 33870000 Hz

        // 寄存器定义
        // Hard-wired Zero
        public const int R0_ADDR = 0x00;
        public static unsafe uint* R0 => (uint*)0x00;

        // Assembler Temporary
        public const int R1_ADDR = 0x04;
        public static unsafe uint* R1 => (uint*)0x04;

        // Value Returned by Subroutines
        public const int R2_ADDR = 0x08;
        public static unsafe uint* R2 => (uint*)0x08;

        // Expression Evaluation
        public const int R3_ADDR = 0x0C;
        public static unsafe uint* R3 => (uint*)0x0C;

        // Expression Evaluation
        public const int R4_ADDR = 0x10;
        public static unsafe uint* R4 => (uint*)0x10;

        // Expression Evaluation
        public const int R5_ADDR = 0x14;
        public static unsafe uint* R5 => (uint*)0x14;

        // Expression Evaluation
        public const int R6_ADDR = 0x18;
        public static unsafe uint* R6 => (uint*)0x18;

        // Expression Evaluation
        public const int R7_ADDR = 0x1C;
        public static unsafe uint* R7 => (uint*)0x1C;

        // Expression Evaluation
        public const int R8_ADDR = 0x20;
        public static unsafe uint* R8 => (uint*)0x20;

        // Expression Evaluation
        public const int R9_ADDR = 0x24;
        public static unsafe uint* R9 => (uint*)0x24;

        // Expression Evaluation
        public const int R10_ADDR = 0x28;
        public static unsafe uint* R10 => (uint*)0x28;

        // Expression Evaluation
        public const int R11_ADDR = 0x2C;
        public static unsafe uint* R11 => (uint*)0x2C;

        // Expression Evaluation
        public const int R12_ADDR = 0x30;
        public static unsafe uint* R12 => (uint*)0x30;

        // Expression Evaluation
        public const int R13_ADDR = 0x34;
        public static unsafe uint* R13 => (uint*)0x34;

        // Expression Evaluation
        public const int R14_ADDR = 0x38;
        public static unsafe uint* R14 => (uint*)0x38;

        // Expression Evaluation
        public const int R15_ADDR = 0x3C;
        public static unsafe uint* R15 => (uint*)0x3C;

        // Saved Value
        public const int R16_ADDR = 0x40;
        public static unsafe uint* R16 => (uint*)0x40;

        // Saved Value
        public const int R17_ADDR = 0x44;
        public static unsafe uint* R17 => (uint*)0x44;

        // Saved Value
        public const int R18_ADDR = 0x48;
        public static unsafe uint* R18 => (uint*)0x48;

        // Saved Value
        public const int R19_ADDR = 0x4C;
        public static unsafe uint* R19 => (uint*)0x4C;

        // Saved Value
        public const int R20_ADDR = 0x50;
        public static unsafe uint* R20 => (uint*)0x50;

        // Saved Value
        public const int R21_ADDR = 0x54;
        public static unsafe uint* R21 => (uint*)0x54;

        // Saved Value
        public const int R22_ADDR = 0x58;
        public static unsafe uint* R22 => (uint*)0x58;

        // Saved Value
        public const int R23_ADDR = 0x5C;
        public static unsafe uint* R23 => (uint*)0x5C;

        // Temporary
        public const int R24_ADDR = 0x60;
        public static unsafe uint* R24 => (uint*)0x60;

        // Temporary
        public const int R25_ADDR = 0x64;
        public static unsafe uint* R25 => (uint*)0x64;

        // Kernel Reserved
        public const int R26_ADDR = 0x68;
        public static unsafe uint* R26 => (uint*)0x68;

        // Kernel Reserved
        public const int R27_ADDR = 0x6C;
        public static unsafe uint* R27 => (uint*)0x6C;

        // Global Pointer
        public const int R28_ADDR = 0x70;
        public static unsafe uint* R28 => (uint*)0x70;

        // Stack Pointer
        public const int R29_ADDR = 0x74;
        public static unsafe uint* R29 => (uint*)0x74;

        // Frame Pointer
        public const int R30_ADDR = 0x78;
        public static unsafe uint* R30 => (uint*)0x78;

        // Return Address
        public const int R31_ADDR = 0x7C;
        public static unsafe uint* R31 => (uint*)0x7C;

        // Multiply/Divide High
        public const int HI_ADDR = 0x80;
        public static unsafe uint* HI => (uint*)0x80;

        // Multiply/Divide Low
        public const int LO_ADDR = 0x84;
        public static unsafe uint* LO => (uint*)0x84;

        // Program Counter
        public const int PC_ADDR = 0x88;
        public static unsafe uint* PC => (uint*)0x88;

        // Coprocessor 0 - Status Register
        public const int CP0_SR_ADDR = 0x90;
        public static unsafe uint* CP0_SR => (uint*)0x90;
        public const int CP0_SR_IE = 0;  // Interrupt Enable
        public const int CP0_SR_EXL = 1;  // Exception Level
        public const int CP0_SR_ERL = 2;  // Error Level
        public const int CP0_SR_KSU = 0;  // Kernel/User Mode
        public const int CP0_SR_IM0_7 = 0;  // Interrupt Mask bits
        public const int CP0_SR_CU0 = 28;  // Coprocessor 0 Usable
        public const int CP0_SR_BEV = 22;  // Bootstrap Exception Vector

        // Coprocessor 0 - Cause Register
        public const int CP0_CAUSE_ADDR = 0x94;
        public static unsafe uint* CP0_CAUSE => (uint*)0x94;
        public const int CP0_CAUSE_EXCCODE = 0;  // Exception Code
        public const int CP0_CAUSE_IP0_7 = 0;  // Interrupt Pending bits

        // Coprocessor 0 - Exception PC
        public const int CP0_EPC_ADDR = 0x98;
        public static unsafe uint* CP0_EPC => (uint*)0x98;

        // Coprocessor 0 - Bad Virtual Address
        public const int CP0_BADVADDR_ADDR = 0x9C;
        public static unsafe uint* CP0_BadVAddr => (uint*)0x9C;

        // Coprocessor 0 - Context Register
        public const int CP0_CONTEXT_ADDR = 0xA0;
        public static unsafe uint* CP0_CONTEXT => (uint*)0xA0;

        // Coprocessor 0 - Process ID
        public const int CP0_PID_ADDR = 0xA4;
        public static unsafe uint* CP0_PID => (uint*)0xA4;

        // 内存段定义
        // KSEG0 - Cached RAM (2MB System RAM)
        public const int KSEG0_START = 0x80000000;
        public const int KSEG0_END = 0x801FFFFF;
        public const int KSEG0_SIZE = 2097152;

        // KSEG1 - Uncached RAM (2MB System RAM)
        public const int KSEG1_START = 0xA0000000;
        public const int KSEG1_END = 0xA01FFFFF;
        public const int KSEG1_SIZE = 2097152;

        // VRAM (1MB, mirrored in KSEG1 at 0xB0000000)
        public const int VRAM_START = 0xA0000000;
        public const int VRAM_END = 0xA01FFFFF;
        public const int VRAM_SIZE = 1048576;

        // Expansion Region (maps to expansion RAM area)
        public const int EXPANSION_START = 0xA0000000;
        public const int EXPANSION_END = 0xA00FFFFF;
        public const int EXPANSION_SIZE = 1048576;

        // Data Scratchpad (1KB)
        public const int SCRATCHPAD_START = 0x1F800000;
        public const int SCRATCHPAD_END = 0x1F8003FF;
        public const int SCRATCHPAD_SIZE = 1024;

        // Kernel BIOS ROM (512KB)
        public const int EXP_ROM_START = 0x1FC00000;
        public const int EXP_ROM_END = 0x1FC7FFFF;
        public const int EXP_ROM_SIZE = 524288;

        // User ROM / Kernel Expansion
        public const int USER_ROM_START = 0x1F800000;
        public const int USER_ROM_END = 0x1FBFFFFF;
        public const int USER_ROM_SIZE = 4194304;

        // I/O Register Area (Expansion 1)
        public const int MMIO_START = 0x1F801000;
        public const int MMIO_END = 0x1F802FFF;
        public const int MMIO_SIZE = 8192;

        // GPU Registers
        public const int GPU_START = 0x1F801810;
        public const int GPU_END = 0x1F801817;
        public const int GPU_SIZE = 8;

        // CD-ROM Registers
        public const int CDROM_START = 0x1F801800;
        public const int CDROM_END = 0x1F80180F;
        public const int CDROM_SIZE = 16;

        // SPU Registers
        public const int SPU_START = 0x1F801C00;
        public const int SPU_END = 0x1F801DFF;
        public const int SPU_SIZE = 512;

        // Interrupt Control
        public const int IRQ_START = 0x1F801070;
        public const int IRQ_END = 0x1F801077;
        public const int IRQ_SIZE = 8;

        // DMA Registers (7 channels)
        public const int DMA_START = 0x1F801080;
        public const int DMA_END = 0x1F8010FF;
        public const int DMA_SIZE = 128;

        // Timer Registers
        public const int TIMER_START = 0x1F801100;
        public const int TIMER_END = 0x1F80112F;
        public const int TIMER_SIZE = 48;

        // JOY Interface Registers
        public const int JOY_START = 0x1F801040;
        public const int JOY_END = 0x1F80104F;
        public const int JOY_SIZE = 16;

        // MDEC (Motion Decoder) Registers
        public const int MDEC_START = 0x1F801820;
        public const int MDEC_END = 0x1F801827;
        public const int MDEC_SIZE = 8;

        // SIO Registers
        public const int SIO_START = 0x1F801050;
        public const int SIO_END = 0x1F80105F;
        public const int SIO_SIZE = 16;

        // GPU Status
        public const int GPU_STAT_START = 0x1F801814;
        public const int GPU_STAT_END = 0x1F801817;
        public const int GPU_STAT_SIZE = 4;

        // CD-ROM Status
        public const int CDROM_STAT_START = 0x1F801801;
        public const int CDROM_STAT_END = 0x1F801803;
        public const int CDROM_STAT_SIZE = 3;

        // SPU Work RAM (1KB)
        public const int SPU_RAM_START = 0x1F800000;
        public const int SPU_RAM_END = 0x1F800FFF;
        public const int SPU_RAM_SIZE = 1024;

        // 外设定义
        // Graphics Processing Unit
        public const int GPU_BASE = 0x1F801810;
        public static unsafe uint* GPU_GP0_CMD => (uint*)0x1F801810;
        public static unsafe uint* GPU_GP0_DATA => (uint*)0x1F801814;
        public static unsafe uint* GPU_GP1_CMD => (uint*)0x1F801818;
        public static unsafe uint* GPU_GP1_DATA => (uint*)0x1F80181C;
        public static unsafe byte* GPU_TPAGE => (byte*)0x1F801810;
        public static unsafe byte* GPU_DRAW_MODE => (byte*)0x1F801811;
        public static unsafe byte* GPU_TEXTURE_WIN => (byte*)0x1F801812;
        public static unsafe byte* GPU_DRAW_OFFSET_X => (byte*)0x1F801813;
        public static unsafe byte* GPU_DRAW_OFFSET_Y => (byte*)0x1F801814;
        public static unsafe byte* GPU_DRAW_AREA_X => (byte*)0x1F801815;
        public static unsafe byte* GPU_DRAW_AREA_Y => (byte*)0x1F801816;
        public static unsafe byte* GPU_DITHER => (byte*)0x1F801817;
        public static unsafe byte* GPU_DISPLAY_MODE => (byte*)0x1F801818;
        public static unsafe byte* GPU_DISPLAY_START_X => (byte*)0x1F801819;
        public static unsafe byte* GPU_DISPLAY_START_Y => (byte*)0x1F80181A;
        public static unsafe byte* GPU_DISPLAY_HORZ => (byte*)0x1F80181B;
        public static unsafe byte* GPU_DISPLAY_VERT => (byte*)0x1F80181C;
        public static unsafe byte* GPU_DMA_MODE => (byte*)0x1F80181D;
        public static unsafe byte* GPU_GPU_STAT => (byte*)0x1F80181C;
        public const int GPU_GPU_STAT_READY_CMD = 0;  // GPU Ready to Receive Command
        public const int GPU_GPU_STAT_READY_DMA = 1;  // GPU Ready for DMA
        public const int GPU_GPU_STAT_DRAWING = 2;  // Drawing Busy
        public const int GPU_GPU_STAT_DMA_REQ = 3;  // DMA Request
        public const int GPU_GPU_STAT_COMMAND_BUSY = 4;  // Command Busy
        public const int GPU_GPU_STAT_DISPLAY_DISABLE = 5;  // Display Disable
        public const int GPU_GPU_STAT_INTERRUPT = 24;  // V-Blank Interrupt Flag

        // Geometry Transformation Engine
        public const int GTE_BASE = 0x1F801880;
        public static unsafe uint* GTE_GTE_VXY0 => (uint*)0x1F801880;
        public static unsafe uint* GTE_GTE_VZ0 => (uint*)0x1F801884;
        public static unsafe uint* GTE_GTE_VXY1 => (uint*)0x1F801888;
        public static unsafe uint* GTE_GTE_VZ1 => (uint*)0x1F80188C;
        public static unsafe uint* GTE_GTE_VXY2 => (uint*)0x1F801890;
        public static unsafe uint* GTE_GTE_VZ2 => (uint*)0x1F801894;
        public static unsafe uint* GTE_GTE_RGB0 => (uint*)0x1F801898;
        public static unsafe uint* GTE_GTE_RGB1 => (uint*)0x1F80189C;
        public static unsafe uint* GTE_GTE_RGB2 => (uint*)0x1F8018A0;
        public static unsafe uint* GTE_GTE_RTP => (uint*)0x1F8018B0;
        public static unsafe uint* GTE_GTE_TRX => (uint*)0x1F8018B4;
        public static unsafe uint* GTE_GTE_TRY => (uint*)0x1F8018B8;
        public static unsafe uint* GTE_GTE_TRZ => (uint*)0x1F8018BC;
        public static unsafe uint* GTE_GTE_MAC0 => (uint*)0x1F8018C0;
        public static unsafe uint* GTE_GTE_MAC1 => (uint*)0x1F8018C4;
        public static unsafe uint* GTE_GTE_MAC2 => (uint*)0x1F8018C8;
        public static unsafe uint* GTE_GTE_MAC3 => (uint*)0x1F8018CC;
        public static unsafe uint* GTE_GTE_IR0 => (uint*)0x1F8018D0;
        public static unsafe uint* GTE_GTE_IR1 => (uint*)0x1F8018D4;
        public static unsafe uint* GTE_GTE_IR2 => (uint*)0x1F8018D8;
        public static unsafe uint* GTE_GTE_IR3 => (uint*)0x1F8018DC;
        public static unsafe uint* GTE_GTE_LZCS => (uint*)0x1F8018E0;
        public static unsafe uint* GTE_GTE_LZCR => (uint*)0x1F8018E4;
        public static unsafe uint* GTE_GTE_CTX => (uint*)0x1F8018E8;
        public static unsafe uint* GTE_GTE_CTY => (uint*)0x1F8018EC;
        public static unsafe uint* GTE_GTE_CTZ => (uint*)0x1F8018F0;
        public static unsafe uint* GTE_GTE_RTX => (uint*)0x1F8018F4;
        public static unsafe uint* GTE_GTE_RTY => (uint*)0x1F8018F8;
        public static unsafe uint* GTE_GTE_RTZ => (uint*)0x1F8018FC;
        public static unsafe uint* GTE_GTE_SR => (uint*)0x1F801900;
        public static unsafe uint* GTE_GTE_CMD => (uint*)0x1F801904;
        public static unsafe uint* GTE_GTE_H => (uint*)0x1F801908;
        public static unsafe uint* GTE_GTE_DQB => (uint*)0x1F80190C;
        public static unsafe uint* GTE_GTE_DQA => (uint*)0x1F801910;
        public static unsafe uint* GTE_GTE_ZSF3 => (uint*)0x1F801914;
        public static unsafe uint* GTE_GTE_ZSF4 => (uint*)0x1F801918;
        public static unsafe uint* GTE_GTE_OTZ => (uint*)0x1F80191C;

        // Sound Processing Unit (24-channel ADPCM)
        public const int SPU_BASE = 0x1F801C00;
        public static unsafe ushort* SPU_SPU_CTRL => (ushort*)0x1F801C00;
        public const int SPU_SPU_CTRL_REVERB_MASTER = 0;  // Reverb Master Enable
        public const int SPU_SPU_CTRL_IRQ9 = 9;  // Interrupt Request Enable
        public static unsafe ushort* SPU_SPU_STAT => (ushort*)0x1F801C04;
        public static unsafe ushort* SPU_SPU_CDVOL_L => (ushort*)0x1F801C08;
        public static unsafe ushort* SPU_SPU_CDVOL_R => (ushort*)0x1F801C0A;
        public static unsafe ushort* SPU_SPU_MAINVOL_L => (ushort*)0x1F801C0C;
        public static unsafe ushort* SPU_SPU_MAINVOL_R => (ushort*)0x1F801C0E;
        public static unsafe ushort* SPU_SPU_REVERB_L => (ushort*)0x1F801C10;
        public static unsafe ushort* SPU_SPU_REVERB_R => (ushort*)0x1F801C12;
        public static unsafe ushort* SPU_SPU_KEYON => (ushort*)0x1F801C80;
        public static unsafe ushort* SPU_SPU_KEYOFF => (ushort*)0x1F801C82;
        public static unsafe ushort* SPU_SPU_CHANNEL_MUTE => (ushort*)0x1F801C84;
        public static unsafe ushort* SPU_SPU_NOISE_CLK => (ushort*)0x1F801C88;
        public static unsafe ushort* SPU_SPU_REVERB_ADDR => (ushort*)0x1F801C8A;
        public static unsafe ushort* SPU_SPU_IRQ_ADDR => (ushort*)0x1F801C8C;
        public static unsafe ushort* SPU_SPU_REVERB_VOL_L => (ushort*)0x1F801C8E;
        public static unsafe ushort* SPU_SPU_REVERB_VOL_R => (ushort*)0x1F801C90;
        public static unsafe byte* SPU_SPU_VOICE_VOL_L => (byte*)0x1F801C00;
        public static unsafe byte* SPU_SPU_VOICE_VOL_R => (byte*)0x1F801C01;
        public static unsafe ushort* SPU_SPU_VOICE_FREQ => (ushort*)0x1F801C02;
        public static unsafe ushort* SPU_SPU_VOICE_START => (ushort*)0x1F801C04;
        public static unsafe ushort* SPU_SPU_VOICE_ADSR1 => (ushort*)0x1F801C06;
        public static unsafe ushort* SPU_SPU_VOICE_ADSR2 => (ushort*)0x1F801C08;
        public static unsafe ushort* SPU_SPU_VOICE_ENV => (ushort*)0x1F801C0A;
        public static unsafe ushort* SPU_SPU_VOICE_REPEAT => (ushort*)0x1F801C0C;
        public static unsafe byte* SPU_VOICE_BASE_SIZE => (byte*)0x1F801C10;

        // Motion Decoder (JPEG Decompression)
        public const int MDEC_BASE = 0x1F801820;
        public static unsafe uint* MDEC_MDEC_CTRL => (uint*)0x1F801820;
        public const int MDEC_MDEC_CTRL_DATA_IN_SIZE = 0;  // Data-in size in words
        public const int MDEC_MDEC_CTRL_RESET = 16;  // Reset MDEC
        public const int MDEC_MDEC_CTRL_BUSY = 17;  // MDEC Busy
        public static unsafe uint* MDEC_MDEC_DATA => (uint*)0x1F801824;
        public static unsafe uint* MDEC_MDEC_BKGD => (uint*)0x1F801828;

        // DMA Controller (7 channels)
        public const int DMA_BASE = 0x1F801080;
        public static unsafe byte* DMA_DMA_DPCR => (byte*)0x1F801080;
        public const int DMA_DMA_DPCR_CH0_EN = 0;  // Channel 0 Enable
        public const int DMA_DMA_DPCR_CH1_EN = 4;  // Channel 1 Enable
        public const int DMA_DMA_DPCR_CH2_EN = 8;  // Channel 2 Enable
        public const int DMA_DMA_DPCR_CH3_EN = 12;  // Channel 3 Enable
        public const int DMA_DMA_DPCR_CH4_EN = 16;  // Channel 4 Enable
        public const int DMA_DMA_DPCR_CH5_EN = 20;  // Channel 5 Enable
        public const int DMA_DMA_DPCR_CH6_EN = 24;  // Channel 6 Enable
        public static unsafe byte* DMA_DMA_INT => (byte*)0x1F801084;
        public static unsafe uint* DMA_DMA_CH0_BASE => (uint*)0x1F801090;
        public static unsafe ushort* DMA_DMA_CH0_COUNT => (ushort*)0x1F801094;
        public static unsafe byte* DMA_DMA_CH0_CTRL => (byte*)0x1F801098;
        public const int DMA_DMA_CH0_CTRL_DEST_DIR = 0;  // Destination Direction
        public const int DMA_DMA_CH0_CTRL_SRC_DIR = 0;  // Source Direction
        public const int DMA_DMA_CH0_CTRL_STEPS = 0;  // Step
        public const int DMA_DMA_CH0_CTRL_CHAIN = 0;  // Chain Mode (0=manual, 1=request, 2=chain, 3=illegal)
        public const int DMA_DMA_CH0_CTRL_SYNC = 0;  // Sync Mode (0=immediate, 1=request, 2=linked-list)
        public const int DMA_DMA_CH0_CTRL_TRIGGER = 10;  // Trigger
        public static unsafe uint* DMA_DMA_CH1_BASE => (uint*)0x1F8010A0;
        public static unsafe ushort* DMA_DMA_CH1_COUNT => (ushort*)0x1F8010A4;
        public static unsafe byte* DMA_DMA_CH1_CTRL => (byte*)0x1F8010A8;
        public static unsafe uint* DMA_DMA_CH2_BASE => (uint*)0x1F8010B0;
        public static unsafe ushort* DMA_DMA_CH2_COUNT => (ushort*)0x1F8010B4;
        public static unsafe byte* DMA_DMA_CH2_CTRL => (byte*)0x1F8010B8;
        public static unsafe uint* DMA_DMA_CH3_BASE => (uint*)0x1F8010C0;
        public static unsafe ushort* DMA_DMA_CH3_COUNT => (ushort*)0x1F8010C4;
        public static unsafe byte* DMA_DMA_CH3_CTRL => (byte*)0x1F8010C8;
        public static unsafe uint* DMA_DMA_CH4_BASE => (uint*)0x1F8010D0;
        public static unsafe ushort* DMA_DMA_CH4_COUNT => (ushort*)0x1F8010D4;
        public static unsafe byte* DMA_DMA_CH4_CTRL => (byte*)0x1F8010D8;
        public static unsafe uint* DMA_DMA_CH5_BASE => (uint*)0x1F8010E0;
        public static unsafe ushort* DMA_DMA_CH5_COUNT => (ushort*)0x1F8010E4;
        public static unsafe byte* DMA_DMA_CH5_CTRL => (byte*)0x1F8010E8;
        public static unsafe uint* DMA_DMA_CH6_BASE => (uint*)0x1F8010F0;
        public static unsafe ushort* DMA_DMA_CH6_COUNT => (ushort*)0x1F8010F4;
        public static unsafe byte* DMA_DMA_CH6_CTRL => (byte*)0x1F8010F8;

        // Timers (3 timers)
        public const int TIMER_BASE = 0x1F801100;
        public static unsafe ushort* TIMER_TM0_COUNT => (ushort*)0x1F801100;
        public static unsafe ushort* TIMER_TM0_MODE => (ushort*)0x1F801104;
        public const int TIMER_TM0_MODE_RELOAD = 0;  // Reload Enable
        public const int TIMER_TM0_MODE_CLOCK = 0;  // Clock Source (0=sysclk/1, 1=sysclk/8, 2=sysclk/64, 3=sysclk/256)
        public const int TIMER_TM0_MODE_IRQ_EN = 3;  // IRQ Enable
        public const int TIMER_TM0_MODE_IRQ_REPEAT = 4;  // IRQ Repeat
        public const int TIMER_TM0_MODE_IRQ_TOGGLE = 5;  // IRQ Toggle Mode
        public const int TIMER_TM0_MODE_REACH_MAX = 6;  // Reached Max Value
        public static unsafe ushort* TIMER_TM0_TARGET => (ushort*)0x1F801108;
        public static unsafe ushort* TIMER_TM1_COUNT => (ushort*)0x1F801110;
        public static unsafe ushort* TIMER_TM1_MODE => (ushort*)0x1F801114;
        public static unsafe ushort* TIMER_TM1_TARGET => (ushort*)0x1F801118;
        public static unsafe ushort* TIMER_TM2_COUNT => (ushort*)0x1F801120;
        public static unsafe ushort* TIMER_TM2_MODE => (ushort*)0x1F801124;
        public static unsafe ushort* TIMER_TM2_TARGET => (ushort*)0x1F801128;

        // CD-ROM Controller
        public const int CDROM_BASE = 0x1F801800;
        public static unsafe byte* CDROM_CD0_DATA => (byte*)0x1F801800;
        public static unsafe byte* CDROM_CD0_STATUS => (byte*)0x1F801801;
        public static unsafe byte* CDROM_CD0_RESPONSE => (byte*)0x1F801802;
        public static unsafe byte* CDROM_CD0_DATA1 => (byte*)0x1F801803;
        public static unsafe byte* CDROM_CD0_INT_FLAG => (byte*)0x1F801804;
        public const int CDROM_CD0_INT_FLAG_INT1 = 0;  // Data Ready
        public const int CDROM_CD0_INT_FLAG_INT2 = 1;  // Command Complete
        public const int CDROM_CD0_INT_FLAG_INT3 = 2;  // Acknowledge Received
        public const int CDROM_CD0_INT_FLAG_INT4 = 3;  // Error / N-Complete
        public static unsafe byte* CDROM_CD0_VOLUME_L => (byte*)0x1F801808;
        public static unsafe byte* CDROM_CD0_VOLUME_R => (byte*)0x1F801809;

        // JOY Interface
        public const int JOY_BASE = 0x1F801040;
        public static unsafe byte* JOY_JOY_CTRL => (byte*)0x1F801040;
        public const int JOY_JOY_CTRL_TX_EN = 0;  // Transmit Enable
        public const int JOY_JOY_CTRL_RX_EN = 1;  // Receive Enable
        public const int JOY_JOY_CTRL_CLOCK = 3;  // Internal/External Clock
        public const int JOY_JOY_CTRL_IRQ_EN = 4;  // IRQ Enable
        public static unsafe byte* JOY_JOY_MODE => (byte*)0x1F801041;
        public static unsafe byte* JOY_JOY_BAUD => (byte*)0x1F801042;
        public static unsafe byte* JOY_JOY_TX_DATA => (byte*)0x1F801044;
        public static unsafe byte* JOY_JOY_RX_DATA => (byte*)0x1F801045;
        public static unsafe byte* JOY_JOY_STAT => (byte*)0x1F801046;
        public const int JOY_JOY_STAT_TX_EMPTY = 0;  // Transmit Buffer Empty
        public const int JOY_JOY_STAT_RX_READY = 2;  // Receive Data Ready
        public const int JOY_JOY_STAT_TX_IRQ = 3;  // Transmit IRQ Pending
        public const int JOY_JOY_STAT_RX_IRQ = 4;  // Receive IRQ Pending

        // SIO (Serial I/O - Memory Card)
        public const int SIO_BASE = 0x1F801050;
        public static unsafe byte* SIO_SIO_DATA => (byte*)0x1F801050;
        public static unsafe byte* SIO_SIO_STATUS => (byte*)0x1F801051;
        public static unsafe byte* SIO_SIO_MODE => (byte*)0x1F801052;
        public static unsafe byte* SIO_SIO_CTRL => (byte*)0x1F801053;
        public static unsafe byte* SIO_SIO_BAUD => (byte*)0x1F801054;

        // Interrupt Controller
        public const int INTERRUPT_BASE = 0x1F801070;
        public static unsafe ushort* INTERRUPT_INT_STAT => (ushort*)0x1F801070;
        public static unsafe ushort* INTERRUPT_INT_MASK => (ushort*)0x1F801074;
        public const int INTERRUPT_INT_MASK_VBLANK = 0;  // V-Blank Interrupt
        public const int INTERRUPT_INT_MASK_GPU = 1;  // GPU Interrupt
        public const int INTERRUPT_INT_MASK_CDROM = 2;  // CD-ROM Interrupt
        public const int INTERRUPT_INT_MASK_DMA0 = 3;  // DMA Channel 0
        public const int INTERRUPT_INT_MASK_DMA1 = 4;  // DMA Channel 1
        public const int INTERRUPT_INT_MASK_DMA2 = 5;  // DMA Channel 2
        public const int INTERRUPT_INT_MASK_DMA3 = 6;  // DMA Channel 3
        public const int INTERRUPT_INT_MASK_DMA4 = 7;  // DMA Channel 4
        public const int INTERRUPT_INT_MASK_DMA5 = 8;  // DMA Channel 5
        public const int INTERRUPT_INT_MASK_DMA6 = 9;  // DMA Channel 6
        public const int INTERRUPT_INT_MASK_TIMER0 = 10;  // Timer 0
        public const int INTERRUPT_INT_MASK_TIMER1 = 11;  // Timer 1
        public const int INTERRUPT_INT_MASK_TIMER2 = 12;  // Timer 2
        public const int INTERRUPT_INT_MASK_SIO = 13;  // SIO / Memory Card
        public const int INTERRUPT_INT_MASK_SPU = 14;  // SPU Interrupt
        public const int INTERRUPT_INT_MASK_PIO = 15;  // PIO (Expansion)

        // 中断向量定义
        public const int IRQ_VBLANK = 0;  // V-Blank Interrupt (60Hz NTSC / 50Hz PAL)
        public const int IRQ_GPU = 1;  // GPU Interrupt (drawing complete / V-Blank)
        public const int IRQ_CDROM = 2;  // CD-ROM Interrupt
        public const int IRQ_DMA0 = 3;  // DMA Channel 0 Complete
        public const int IRQ_DMA1 = 4;  // DMA Channel 1 Complete
        public const int IRQ_DMA2 = 5;  // DMA Channel 2 Complete
        public const int IRQ_DMA3 = 6;  // DMA Channel 3 Complete
        public const int IRQ_DMA4 = 7;  // DMA Channel 4 Complete
        public const int IRQ_DMA5 = 8;  // DMA Channel 5 Complete
        public const int IRQ_DMA6 = 9;  // DMA Channel 6 Complete
        public const int IRQ_TIMER0 = 10;  // Timer 0 Interrupt
        public const int IRQ_TIMER1 = 11;  // Timer 1 Interrupt
        public const int IRQ_TIMER2 = 12;  // Timer 2 Interrupt
        public const int IRQ_SIO = 13;  // SIO / Memory Card Interrupt
        public const int IRQ_SPU = 14;  // SPU Interrupt
        public const int IRQ_PIO = 15;  // PIO / Expansion Interrupt

        // 引脚定义
        public const int PIN_VCC = 1;  // Power Supply (3.3V regulated)
        public const int PIN_VSS = 2;  // Ground
        public const int PIN_CLK = 3;  // System Clock Input (53.6932MHz / 2 = 26.8466MHz bus)
        public const int PIN_RESET = 4;  // Reset (active low)
        public const int PIN_NMI = 5;  // Non-Maskable Interrupt
        public const int PIN_IRQ = 6;  // Interrupt Request
        public const int PIN_AB0 = 7;  // Address Bus Bit 0
        public const int PIN_AB1 = 8;  // Address Bus Bit 1
        public const int PIN_AB2 = 9;  // Address Bus Bit 2
        public const int PIN_AB3 = 10;  // Address Bus Bit 3
        public const int PIN_AB4 = 11;  // Address Bus Bit 4
        public const int PIN_AB5 = 12;  // Address Bus Bit 5
        public const int PIN_AB6 = 13;  // Address Bus Bit 6
        public const int PIN_AB7 = 14;  // Address Bus Bit 7
        public const int PIN_AB8 = 15;  // Address Bus Bit 8
        public const int PIN_AB9 = 16;  // Address Bus Bit 9
        public const int PIN_AB10 = 17;  // Address Bus Bit 10
        public const int PIN_AB11 = 18;  // Address Bus Bit 11
        public const int PIN_AB12 = 19;  // Address Bus Bit 12
        public const int PIN_AB13 = 20;  // Address Bus Bit 13
        public const int PIN_AB14 = 21;  // Address Bus Bit 14
        public const int PIN_AB15 = 22;  // Address Bus Bit 15
        public const int PIN_AB16 = 23;  // Address Bus Bit 16
        public const int PIN_AB17 = 24;  // Address Bus Bit 17
        public const int PIN_AB18 = 25;  // Address Bus Bit 18
        public const int PIN_AB19 = 26;  // Address Bus Bit 19
        public const int PIN_AB20 = 27;  // Address Bus Bit 20
        public const int PIN_AB21 = 28;  // Address Bus Bit 21
        public const int PIN_AB22 = 29;  // Address Bus Bit 22
        public const int PIN_AB23 = 30;  // Address Bus Bit 23
        public const int PIN_AB24 = 31;  // Address Bus Bit 24
        public const int PIN_AB25 = 32;  // Address Bus Bit 25
        public const int PIN_AB26 = 33;  // Address Bus Bit 26
        public const int PIN_AB27 = 34;  // Address Bus Bit 27
        public const int PIN_AB28 = 35;  // Address Bus Bit 28
        public const int PIN_AB29 = 36;  // Address Bus Bit 29
        public const int PIN_AB30 = 37;  // Address Bus Bit 30
        public const int PIN_AB31 = 38;  // Address Bus Bit 31
        public const int PIN_DB0 = 39;  // Data Bus Bit 0
        public const int PIN_DB1 = 40;  // Data Bus Bit 1
        public const int PIN_DB2 = 41;  // Data Bus Bit 2
        public const int PIN_DB3 = 42;  // Data Bus Bit 3
        public const int PIN_DB4 = 43;  // Data Bus Bit 4
        public const int PIN_DB5 = 44;  // Data Bus Bit 5
        public const int PIN_DB6 = 45;  // Data Bus Bit 6
        public const int PIN_DB7 = 46;  // Data Bus Bit 7
        public const int PIN_DB8 = 47;  // Data Bus Bit 8
        public const int PIN_DB9 = 48;  // Data Bus Bit 9
        public const int PIN_DB10 = 49;  // Data Bus Bit 10
        public const int PIN_DB11 = 50;  // Data Bus Bit 11
        public const int PIN_DB12 = 51;  // Data Bus Bit 12
        public const int PIN_DB13 = 52;  // Data Bus Bit 13
        public const int PIN_DB14 = 53;  // Data Bus Bit 14
        public const int PIN_DB15 = 54;  // Data Bus Bit 15
        public const int PIN_DB16 = 55;  // Data Bus Bit 16
        public const int PIN_DB17 = 56;  // Data Bus Bit 17
        public const int PIN_DB18 = 57;  // Data Bus Bit 18
        public const int PIN_DB19 = 58;  // Data Bus Bit 19
        public const int PIN_DB20 = 59;  // Data Bus Bit 20
        public const int PIN_DB21 = 60;  // Data Bus Bit 21
        public const int PIN_DB22 = 61;  // Data Bus Bit 22
        public const int PIN_DB23 = 62;  // Data Bus Bit 23
        public const int PIN_DB24 = 63;  // Data Bus Bit 24
        public const int PIN_DB25 = 64;  // Data Bus Bit 25
        public const int PIN_DB26 = 65;  // Data Bus Bit 26
        public const int PIN_DB27 = 66;  // Data Bus Bit 27
        public const int PIN_DB28 = 67;  // Data Bus Bit 28
        public const int PIN_DB29 = 68;  // Data Bus Bit 29
        public const int PIN_DB30 = 69;  // Data Bus Bit 30
        public const int PIN_DB31 = 70;  // Data Bus Bit 31
        public const int PIN_NCS0 = 71;  // Chip Select 0 (ROM)
        public const int PIN_NCS1 = 72;  // Chip Select 1 (RAM)
        public const int PIN_NCS2 = 73;  // Chip Select 2 (I/O)
        public const int PIN_NWR = 74;  // Write Enable
        public const int PIN_NRD = 75;  // Read Enable
        public const int PIN_BE0 = 76;  // Byte Enable 0 (bits 0-7)
        public const int PIN_BE1 = 77;  // Byte Enable 1 (bits 8-15)
        public const int PIN_BE2 = 78;  // Byte Enable 2 (bits 16-23)
        public const int PIN_BE3 = 79;  // Byte Enable 3 (bits 24-31)
        public const int PIN_BUSREQ = 80;  // Bus Request (from external DMA)
        public const int PIN_BUSACK = 81;  // Bus Acknowledge
        public const int PIN_INT0 = 82;  // Interrupt 0 (V-Blank)
        public const int PIN_INT1 = 83;  // Interrupt 1 (GPU)
        public const int PIN_INT2 = 84;  // Interrupt 2 (CD-ROM)
        public const int PIN_INT3 = 85;  // Interrupt 3 (DMA)
        public const int PIN_INT4 = 86;  // Interrupt 4 (Timer)
        public const int PIN_INT5 = 87;  // Interrupt 5 (SIO)
        public const int PIN_AUDIO_L = 88;  // Audio Output Left
        public const int PIN_AUDIO_R = 89;  // Audio Output Right
        public const int PIN_VIDEO_R = 90;  // Video Output Red (analog RGB)
        public const int PIN_VIDEO_G = 91;  // Video Output Green
        public const int PIN_VIDEO_B = 92;  // Video Output Blue
        public const int PIN_SYNC = 93;  // Video Sync / Composite

        public static void mips_r3000a_init()
        {
            // 硬件初始化代码
        }
    }
}
