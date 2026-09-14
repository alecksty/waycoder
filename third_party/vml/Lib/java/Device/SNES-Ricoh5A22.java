package vml.device.ricoh.ricoh_5a22;

/**
 * Ricoh-5A22 寄存器定义
 * 生成自: Ricoh/MOS-6502/Ricoh-5A22
 * 版本: 1.0
 */
public final class Ricoh_5A22 {
    private Ricoh_5A22() {} // 工具类
    // CPU架构: Ricoh-5A22, 16位, 3580000 Hz

    // 寄存器定义
    // Accumulator (8-bit, expandable to 16-bit)
    public static final int A_ADDR = (int)0x00;

    // Accumulator high byte when 16-bit
    public static final int B_ADDR = (int)0x01;

    // X Index Register (8/16-bit)
    public static final int X_ADDR = (int)0x02;

    // Y Index Register (8/16-bit)
    public static final int Y_ADDR = (int)0x03;

    // Stack Pointer (8-bit, banked)
    public static final int SP_ADDR = (int)0x04;

    // Program Counter (16-bit)
    public static final int PC_ADDR = (int)0x06;

    // Direct Page Register
    public static final int D_ADDR = (int)0x08;

    // Processor Status
    public static final int P_ADDR = (int)0x0A;
    public static final int P_N = 0;  // Negative
    public static final int P_V = 1;  // Overflow
    public static final int P_M = 2;  // Memory/Accumulator Select (0=16-bit, 1=8-bit)
    public static final int P_X = 3;  // Index Select (0=16-bit, 1=8-bit)
    public static final int P_B = 4;  // Break
    public static final int P_D = 5;  // Decimal Mode
    public static final int P_I = 6;  // Interrupt Disable
    public static final int P_Z = 7;  // Zero
    public static final int P_C = 8;  // Carry

    // 内存段定义
    // Work RAM (128KB internal)
    public static final int WRAM_START = (int)0x7E0000;
    public static final int WRAM_END = (int)0x7FFFFF;
    public static final int WRAM_SIZE = 131072;

    // Save RAM / Cartridge SRAM
    public static final int SRAM_START = (int)0x600000;
    public static final int SRAM_END = (int)0x6FFFFF;
    public static final int SRAM_SIZE = 1048576;

    // Cartridge ROM (LoROM/HiROM mapping)
    public static final int CART_ROM_START = (int)0x800000;
    public static final int CART_ROM_END = (int)0xFFFFFF;
    public static final int CART_ROM_SIZE = 8388608;

    // PPU1 Registers (background)
    public static final int PPU1_REGS_START = (int)0x2100;
    public static final int PPU1_REGS_END = (int)0x213F;
    public static final int PPU1_REGS_SIZE = 64;

    // PPU2 Registers (sprites)
    public static final int PPU2_REGS_START = (int)0x2140;
    public static final int PPU2_REGS_END = (int)0x217F;
    public static final int PPU2_REGS_SIZE = 64;

    // PPU3 Registers (extra)
    public static final int PPU3_REGS_START = (int)0x2180;
    public static final int PPU3_REGS_END = (int)0x21FF;
    public static final int PPU3_REGS_SIZE = 128;

    // APU I/O Registers
    public static final int APU_REGS_START = (int)0x2140;
    public static final int APU_REGS_END = (int)0x217F;
    public static final int APU_REGS_SIZE = 64;

    // CPU I/O Ports
    public static final int CPU_IO_START = (int)0x2000;
    public static final int CPU_IO_END = (int)0x20FF;
    public static final int CPU_IO_SIZE = 256;

    // DMA Channel Registers
    public static final int DMA_REGS_START = (int)0x4300;
    public static final int DMA_REGS_END = (int)0x437F;
    public static final int DMA_REGS_SIZE = 128;

    // HDMA Channel Registers
    public static final int HDMA_REGS_START = (int)0x4380;
    public static final int HDMA_REGS_END = (int)0x43FF;
    public static final int HDMA_REGS_SIZE = 128;

    // 外设定义
    // Picture Processing Unit 1 - Background Rendering
    public static final int PPU1_BASE = (int)0x2100;
    public static final int PPU1_INIDISP = (int)0x00004200;
    public static final int PPU1_OBSEL = (int)0x00004201;
    public static final int PPU1_OAMADDL = (int)0x00004202;
    public static final int PPU1_OAMADDH = (int)0x00004203;
    public static final int PPU1_OAMDATA = (int)0x00004204;
    public static final int PPU1_BGMODE = (int)0x00004205;
    public static final int PPU1_MOSAIC = (int)0x00004206;
    public static final int PPU1_BG1SC = (int)0x00004207;
    public static final int PPU1_BG2SC = (int)0x00004208;
    public static final int PPU1_BG3SC = (int)0x00004209;
    public static final int PPU1_BG4SC = (int)0x0000420A;
    public static final int PPU1_BG12NBA = (int)0x0000420B;
    public static final int PPU1_BG34NBA = (int)0x0000420C;
    public static final int PPU1_BG1HOFS = (int)0x0000420D;
    public static final int PPU1_BG1VOFS = (int)0x0000420E;
    public static final int PPU1_BG2HOFS = (int)0x0000420F;
    public static final int PPU1_BG2VOFS = (int)0x00004210;
    public static final int PPU1_BG3HOFS = (int)0x00004211;
    public static final int PPU1_BG3VOFS = (int)0x00004212;
    public static final int PPU1_BG4HOFS = (int)0x00004213;
    public static final int PPU1_BG4VOFS = (int)0x00004214;
    public static final int PPU1_VMAIN = (int)0x00004215;
    public static final int PPU1_VMADDL = (int)0x00004216;
    public static final int PPU1_VMADDH = (int)0x00004217;
    public static final int PPU1_VMDATAL = (int)0x00004218;
    public static final int PPU1_VMDATAH = (int)0x00004219;
    public static final int PPU1_M7SEL = (int)0x0000421A;
    public static final int PPU1_M7A = (int)0x0000421B;
    public static final int PPU1_M7B = (int)0x0000421C;
    public static final int PPU1_M7C = (int)0x0000421D;
    public static final int PPU1_M7D = (int)0x0000421E;
    public static final int PPU1_M7X = (int)0x0000421F;
    public static final int PPU1_M7Y = (int)0x00004220;
    public static final int PPU1_CGADD = (int)0x00004221;
    public static final int PPU1_CGDATA = (int)0x00004222;
    public static final int PPU1_W12SEL = (int)0x00004223;
    public static final int PPU1_W34SEL = (int)0x00004224;
    public static final int PPU1_WOBJSEL = (int)0x00004225;
    public static final int PPU1_WH0 = (int)0x00004226;
    public static final int PPU1_WH1 = (int)0x00004227;
    public static final int PPU1_WH2 = (int)0x00004228;
    public static final int PPU1_WH3 = (int)0x00004229;
    public static final int PPU1_WBGLOG = (int)0x0000422A;
    public static final int PPU1_WOBJLOG = (int)0x0000422B;
    public static final int PPU1_TM = (int)0x0000422C;
    public static final int PPU1_TS = (int)0x0000422D;
    public static final int PPU1_TMW = (int)0x0000422E;
    public static final int PPU1_TSW = (int)0x0000422F;
    public static final int PPU1_CGSWSEL = (int)0x00004230;
    public static final int PPU1_CGADSUB = (int)0x00004231;
    public static final int PPU1_SETINI = (int)0x00004233;

    // Picture Processing Unit 2 - Sprite Rendering
    public static final int PPU2_BASE = (int)0x2140;
    public static final int PPU2_OAMDATAREAD = (int)0x00004278;
    public static final int PPU2_VMDATAREAD = (int)0x00004279;
    public static final int PPU2_VMDATAHREAD = (int)0x0000427A;
    public static final int PPU2_CGDATAREAD = (int)0x0000427B;
    public static final int PPU2_OPHCT = (int)0x0000427C;
    public static final int PPU2_OPVCT = (int)0x0000427D;
    public static final int PPU2_STAT78 = (int)0x0000427F;

    // Sony SPC700 Audio CPU (8-bit)
    public static final int SPC700_BASE = (int)0x00;
    public static final int SPC700_PC = (int)0x00000000;
    public static final int SPC700_A = (int)0x00000002;
    public static final int SPC700_X = (int)0x00000003;
    public static final int SPC700_Y = (int)0x00000004;
    public static final int SPC700_SP = (int)0x00000005;
    public static final int SPC700_PSW = (int)0x00000006;
    public static final int SPC700_TEST = (int)0x0000000F;

    // S-DSP Audio DSP (8-channel ADPCM)
    public static final int DSP_BASE = (int)0x00;
    public static final int DSP_MVOL_L = (int)0x0000000C;
    public static final int DSP_MVOL_R = (int)0x0000001C;
    public static final int DSP_EVOL_L = (int)0x0000002C;
    public static final int DSP_EVOL_R = (int)0x0000003C;
    public static final int DSP_KON = (int)0x0000004C;
    public static final int DSP_KOFF = (int)0x0000005C;
    public static final int DSP_KONKOFF = (int)0x0000004D;
    public static final int DSP_FLG = (int)0x0000006C;
    public static final int DSP_ENDX = (int)0x0000007D;
    public static final int DSP_EBUST = (int)0x0000006D;
    public static final int DSP_EDL = (int)0x0000007D;
    public static final int DSP_ENV0 = (int)0x00000000;
    public static final int DSP_OUT0 = (int)0x0000001C;
    public static final int DSP_ENV1 = (int)0x00000001;
    public static final int DSP_OUT1 = (int)0x0000002C;
    public static final int DSP_ENV2 = (int)0x00000002;
    public static final int DSP_OUT2 = (int)0x0000003C;
    public static final int DSP_ENV3 = (int)0x00000003;
    public static final int DSP_OUT3 = (int)0x0000004C;
    public static final int DSP_ENV4 = (int)0x00000004;
    public static final int DSP_OUT4 = (int)0x0000005C;
    public static final int DSP_ENV5 = (int)0x00000005;
    public static final int DSP_OUT5 = (int)0x0000006C;
    public static final int DSP_ENV6 = (int)0x00000006;
    public static final int DSP_OUT6 = (int)0x0000007C;
    public static final int DSP_ENV7 = (int)0x00000007;
    public static final int DSP_OUT7 = (int)0x0000000D;
    public static final int DSP_V0SRC = (int)0x00000008;
    public static final int DSP_V1SRC = (int)0x00000009;
    public static final int DSP_V2SRC = (int)0x0000000A;
    public static final int DSP_V3SRC = (int)0x0000000B;
    public static final int DSP_V4SRC = (int)0x00000018;
    public static final int DSP_V5SRC = (int)0x00000019;
    public static final int DSP_V6SRC = (int)0x0000001A;
    public static final int DSP_V7SRC = (int)0x0000001B;
    public static final int DSP_V0PITCHL = (int)0x00000002;
    public static final int DSP_V0PITCHH = (int)0x00000003;
    public static final int DSP_V1PITCHL = (int)0x00000012;
    public static final int DSP_V1PITCHH = (int)0x00000013;
    public static final int DSP_V2PITCHL = (int)0x00000022;
    public static final int DSP_V2PITCHH = (int)0x00000023;
    public static final int DSP_V3PITCHL = (int)0x00000032;
    public static final int DSP_V3PITCHH = (int)0x00000033;
    public static final int DSP_V4PITCHL = (int)0x00000042;
    public static final int DSP_V4PITCHH = (int)0x00000043;
    public static final int DSP_V5PITCHL = (int)0x00000052;
    public static final int DSP_V5PITCHH = (int)0x00000053;
    public static final int DSP_V6PITCHL = (int)0x00000062;
    public static final int DSP_V6PITCHH = (int)0x00000063;
    public static final int DSP_V7PITCHL = (int)0x00000072;
    public static final int DSP_V7PITCHH = (int)0x00000073;
    public static final int DSP_V0ADSR0 = (int)0x00000004;
    public static final int DSP_V0ADSR1 = (int)0x00000005;
    public static final int DSP_V0ADSR2 = (int)0x00000006;
    public static final int DSP_V1ADSR0 = (int)0x00000014;
    public static final int DSP_V1ADSR1 = (int)0x00000015;
    public static final int DSP_V1ADSR2 = (int)0x00000016;
    public static final int DSP_V2ADSR0 = (int)0x00000024;
    public static final int DSP_V2ADSR1 = (int)0x00000025;
    public static final int DSP_V2ADSR2 = (int)0x00000026;
    public static final int DSP_V3ADSR0 = (int)0x00000034;
    public static final int DSP_V3ADSR1 = (int)0x00000035;
    public static final int DSP_V3ADSR2 = (int)0x00000036;
    public static final int DSP_V4ADSR0 = (int)0x00000044;
    public static final int DSP_V4ADSR1 = (int)0x00000045;
    public static final int DSP_V4ADSR2 = (int)0x00000046;
    public static final int DSP_V5ADSR0 = (int)0x00000054;
    public static final int DSP_V5ADSR1 = (int)0x00000055;
    public static final int DSP_V5ADSR2 = (int)0x00000056;
    public static final int DSP_V6ADSR0 = (int)0x00000064;
    public static final int DSP_V6ADSR1 = (int)0x00000065;
    public static final int DSP_V6ADSR2 = (int)0x00000066;
    public static final int DSP_V7ADSR0 = (int)0x00000074;
    public static final int DSP_V7ADSR1 = (int)0x00000075;
    public static final int DSP_V7ADSR2 = (int)0x00000076;
    public static final int DSP_V0GAIN = (int)0x00000007;
    public static final int DSP_V1GAIN = (int)0x00000017;
    public static final int DSP_V2GAIN = (int)0x00000027;
    public static final int DSP_V3GAIN = (int)0x00000037;
    public static final int DSP_V4GAIN = (int)0x00000047;
    public static final int DSP_V5GAIN = (int)0x00000057;
    public static final int DSP_V6GAIN = (int)0x00000067;
    public static final int DSP_V7GAIN = (int)0x00000077;
    public static final int DSP_V0WAVE = (int)0x0000000D;
    public static final int DSP_V1WAVE = (int)0x0000001D;
    public static final int DSP_V2WAVE = (int)0x0000002D;
    public static final int DSP_V3WAVE = (int)0x0000003D;
    public static final int DSP_V4WAVE = (int)0x0000004D;
    public static final int DSP_V5WAVE = (int)0x0000005D;
    public static final int DSP_V6WAVE = (int)0x0000006D;
    public static final int DSP_V7WAVE = (int)0x0000007D;

    // Direct Memory Access Controller
    public static final int DMA_BASE = (int)0x4300;
    public static final int DMA_DMAP0 = (int)0x00008600;
    public static final int DMA_BBAD0 = (int)0x00008601;
    public static final int DMA_A1T0L = (int)0x00008602;
    public static final int DMA_A1T0H = (int)0x00008603;
    public static final int DMA_A1B0 = (int)0x00008604;
    public static final int DMA_DAS0L = (int)0x00008605;
    public static final int DMA_DAS0H = (int)0x00008606;
    public static final int DMA_DASB0 = (int)0x00008607;
    public static final int DMA_A2A0 = (int)0x00008608;
    public static final int DMA_A2A1 = (int)0x00008609;
    public static final int DMA_A2B0 = (int)0x0000860A;
    public static final int DMA_NTT0 = (int)0x0000860B;
    public static final int DMA_DMAP1 = (int)0x00008610;
    public static final int DMA_BBAD1 = (int)0x00008611;
    public static final int DMA_A1T1L = (int)0x00008612;
    public static final int DMA_A1T1H = (int)0x00008613;
    public static final int DMA_A1B1 = (int)0x00008614;
    public static final int DMA_DAS1L = (int)0x00008615;
    public static final int DMA_DAS1H = (int)0x00008616;
    public static final int DMA_DASB1 = (int)0x00008617;
    public static final int DMA_DMAP2 = (int)0x00008620;
    public static final int DMA_BBAD2 = (int)0x00008621;
    public static final int DMA_A1T2L = (int)0x00008622;
    public static final int DMA_A1T2H = (int)0x00008623;
    public static final int DMA_A1B2 = (int)0x00008624;
    public static final int DMA_DAS2L = (int)0x00008625;
    public static final int DMA_DAS2H = (int)0x00008626;
    public static final int DMA_DASB2 = (int)0x00008627;
    public static final int DMA_DMAP3 = (int)0x00008630;
    public static final int DMA_BBAD3 = (int)0x00008631;
    public static final int DMA_A1T3L = (int)0x00008632;
    public static final int DMA_A1T3H = (int)0x00008633;
    public static final int DMA_A1B3 = (int)0x00008634;
    public static final int DMA_DAS3L = (int)0x00008635;
    public static final int DMA_DAS3H = (int)0x00008636;
    public static final int DMA_DASB3 = (int)0x00008637;
    public static final int DMA_MDMAEN = (int)0x00008650;

    // Horizontal DMA (scanline-based)
    public static final int HDMA_BASE = (int)0x4380;
    public static final int HDMA_HDMAP0 = (int)0x00008700;
    public static final int HDMA_HBAD0 = (int)0x00008701;
    public static final int HDMA_A1T0L = (int)0x00008702;
    public static final int HDMA_A1T0H = (int)0x00008703;
    public static final int HDMA_A1B0 = (int)0x00008704;
    public static final int HDMA_DAS0L = (int)0x00008705;
    public static final int HDMA_DAS0H = (int)0x00008706;
    public static final int HDMA_HDMAP1 = (int)0x00008708;
    public static final int HDMA_HBAD1 = (int)0x00008709;
    public static final int HDMA_A1T1L = (int)0x0000870A;
    public static final int HDMA_A1T1H = (int)0x0000870B;
    public static final int HDMA_A1B1 = (int)0x0000870C;
    public static final int HDMA_DAS1L = (int)0x0000870D;
    public static final int HDMA_DAS1H = (int)0x0000870E;
    public static final int HDMA_HDMAP2 = (int)0x00008710;
    public static final int HDMA_HBAD2 = (int)0x00008711;
    public static final int HDMA_A1T2L = (int)0x00008712;
    public static final int HDMA_A1T2H = (int)0x00008713;
    public static final int HDMA_A1B2 = (int)0x00008714;
    public static final int HDMA_DAS2L = (int)0x00008715;
    public static final int HDMA_DAS2H = (int)0x00008716;
    public static final int HDMA_HDMAP3 = (int)0x00008718;
    public static final int HDMA_HBAD3 = (int)0x00008719;
    public static final int HDMA_A1T3L = (int)0x0000871A;
    public static final int HDMA_A1T3H = (int)0x0000871B;
    public static final int HDMA_A1B3 = (int)0x0000871C;
    public static final int HDMA_DAS3L = (int)0x0000871D;
    public static final int HDMA_DAS3H = (int)0x0000871E;
    public static final int HDMA_HDMAEN = (int)0x00008770;

    // Controller Port 1
    public static final int CONTROLLER1_BASE = (int)0x4016;
    public static final int CONTROLLER1_JOYPAD1 = (int)0x0000802C;
    public static final int CONTROLLER1_JOYSTROBE = (int)0x0000802C;

    // Controller Port 2
    public static final int CONTROLLER2_BASE = (int)0x4017;
    public static final int CONTROLLER2_JOYPAD2 = (int)0x0000802E;
    public static final int CONTROLLER2_RDNMI = (int)0x00008227;
    public static final int CONTROLLER2_TIMEUP = (int)0x00008228;
    public static final int CONTROLLER2_HVBJOY = (int)0x00008229;

    // Timer / IRQ Control
    public static final int TIMER_BASE = (int)0x4200;
    public static final int TIMER_NMITIMEN = (int)0x00008400;
    public static final int TIMER_NMITIMEN_VBLANK_NMI = 7;  // V-Blank NMI Enable
    public static final int TIMER_NMITIMEN_HTIMER_EN = 4;  // H-Counter IRQ Enable
    public static final int TIMER_NMITIMEN_VTIMER_EN = 5;  // V-Counter IRQ Enable
    public static final int TIMER_WRI00 = (int)0x00008401;
    public static final int TIMER_HTIMEL = (int)0x00008402;
    public static final int TIMER_HTIMEH = (int)0x00008403;
    public static final int TIMER_VTIMEL = (int)0x00008404;
    public static final int TIMER_VTIMEH = (int)0x00008405;
    public static final int TIMER_MEMSEL = (int)0x0000840D;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt (V-Blank)
    public static final int IRQ_IRQ = 2;  // IRQ / BRK (Timer, HDMA, Controller)
    public static final int IRQ_TIMER_IRQ = 3;  // H/V Counter Timer IRQ

    // 引脚定义
    public static final int PIN_VCC = 1;  // Power Supply
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // System Clock Input (21.47727 MHz)
    public static final int PIN_RESET = 4;  // Reset Signal
    public static final int PIN_NMI = 5;  // Non-Maskable Interrupt
    public static final int PIN_IRQ = 6;  // Interrupt Request
    public static final int PIN_RDY = 7;  // Ready / Wait State
    public static final int PIN_AB0 = 8;  // Address Bus Bit 0
    public static final int PIN_AB1 = 9;  // Address Bus Bit 1
    public static final int PIN_AB2 = 10;  // Address Bus Bit 2
    public static final int PIN_AB3 = 11;  // Address Bus Bit 3
    public static final int PIN_AB4 = 12;  // Address Bus Bit 4
    public static final int PIN_AB5 = 13;  // Address Bus Bit 5
    public static final int PIN_AB6 = 14;  // Address Bus Bit 6
    public static final int PIN_AB7 = 15;  // Address Bus Bit 7
    public static final int PIN_AB8 = 16;  // Address Bus Bit 8
    public static final int PIN_AB9 = 17;  // Address Bus Bit 9
    public static final int PIN_AB10 = 18;  // Address Bus Bit 10
    public static final int PIN_AB11 = 19;  // Address Bus Bit 11
    public static final int PIN_AB12 = 20;  // Address Bus Bit 12
    public static final int PIN_AB13 = 21;  // Address Bus Bit 13
    public static final int PIN_AB14 = 22;  // Address Bus Bit 14
    public static final int PIN_AB15 = 23;  // Address Bus Bit 15
    public static final int PIN_AB16 = 24;  // Address Bus Bit 16
    public static final int PIN_AB17 = 25;  // Address Bus Bit 17
    public static final int PIN_AB18 = 26;  // Address Bus Bit 18
    public static final int PIN_AB19 = 27;  // Address Bus Bit 19
    public static final int PIN_AB20 = 28;  // Address Bus Bit 20
    public static final int PIN_AB21 = 29;  // Address Bus Bit 21
    public static final int PIN_AB22 = 30;  // Address Bus Bit 22
    public static final int PIN_AB23 = 31;  // Address Bus Bit 23
    public static final int PIN_DB0 = 32;  // Data Bus Bit 0
    public static final int PIN_DB1 = 33;  // Data Bus Bit 1
    public static final int PIN_DB2 = 34;  // Data Bus Bit 2
    public static final int PIN_DB3 = 35;  // Data Bus Bit 3
    public static final int PIN_DB4 = 36;  // Data Bus Bit 4
    public static final int PIN_DB5 = 37;  // Data Bus Bit 5
    public static final int PIN_DB6 = 38;  // Data Bus Bit 6
    public static final int PIN_DB7 = 39;  // Data Bus Bit 7
    public static final int PIN_PHI = 40;  // Phase Out Clock

    public static native void ricoh_5a22_init();
}
