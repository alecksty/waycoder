using System;

namespace VML.Device.Ricoh.Ricoh_5A22
{
    /// <summary>
    /// Ricoh-5A22 寄存器定义
    /// 生成自: Ricoh/MOS-6502/Ricoh-5A22
    /// 版本: 1.0
    /// </summary>
    public static class Ricoh_5A22
    {
        // CPU架构: Ricoh-5A22, 16位, 3580000 Hz

        // 寄存器定义
        // Accumulator (8-bit, expandable to 16-bit)
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // Accumulator high byte when 16-bit
        public const int B_ADDR = 0x01;
        public static unsafe byte* B => (byte*)0x01;

        // X Index Register (8/16-bit)
        public const int X_ADDR = 0x02;
        public static unsafe byte* X => (byte*)0x02;

        // Y Index Register (8/16-bit)
        public const int Y_ADDR = 0x03;
        public static unsafe byte* Y => (byte*)0x03;

        // Stack Pointer (8-bit, banked)
        public const int SP_ADDR = 0x04;
        public static unsafe byte* SP => (byte*)0x04;

        // Program Counter (16-bit)
        public const int PC_ADDR = 0x06;
        public static unsafe ushort* PC => (ushort*)0x06;

        // Direct Page Register
        public const int D_ADDR = 0x08;
        public static unsafe byte* D => (byte*)0x08;

        // Processor Status
        public const int P_ADDR = 0x0A;
        public static unsafe byte* P => (byte*)0x0A;
        public const int P_N = 0;  // Negative
        public const int P_V = 1;  // Overflow
        public const int P_M = 2;  // Memory/Accumulator Select (0=16-bit, 1=8-bit)
        public const int P_X = 3;  // Index Select (0=16-bit, 1=8-bit)
        public const int P_B = 4;  // Break
        public const int P_D = 5;  // Decimal Mode
        public const int P_I = 6;  // Interrupt Disable
        public const int P_Z = 7;  // Zero
        public const int P_C = 8;  // Carry

        // 内存段定义
        // Work RAM (128KB internal)
        public const int WRAM_START = 0x7E0000;
        public const int WRAM_END = 0x7FFFFF;
        public const int WRAM_SIZE = 131072;

        // Save RAM / Cartridge SRAM
        public const int SRAM_START = 0x600000;
        public const int SRAM_END = 0x6FFFFF;
        public const int SRAM_SIZE = 1048576;

        // Cartridge ROM (LoROM/HiROM mapping)
        public const int CART_ROM_START = 0x800000;
        public const int CART_ROM_END = 0xFFFFFF;
        public const int CART_ROM_SIZE = 8388608;

        // PPU1 Registers (background)
        public const int PPU1_REGS_START = 0x2100;
        public const int PPU1_REGS_END = 0x213F;
        public const int PPU1_REGS_SIZE = 64;

        // PPU2 Registers (sprites)
        public const int PPU2_REGS_START = 0x2140;
        public const int PPU2_REGS_END = 0x217F;
        public const int PPU2_REGS_SIZE = 64;

        // PPU3 Registers (extra)
        public const int PPU3_REGS_START = 0x2180;
        public const int PPU3_REGS_END = 0x21FF;
        public const int PPU3_REGS_SIZE = 128;

        // APU I/O Registers
        public const int APU_REGS_START = 0x2140;
        public const int APU_REGS_END = 0x217F;
        public const int APU_REGS_SIZE = 64;

        // CPU I/O Ports
        public const int CPU_IO_START = 0x2000;
        public const int CPU_IO_END = 0x20FF;
        public const int CPU_IO_SIZE = 256;

        // DMA Channel Registers
        public const int DMA_REGS_START = 0x4300;
        public const int DMA_REGS_END = 0x437F;
        public const int DMA_REGS_SIZE = 128;

        // HDMA Channel Registers
        public const int HDMA_REGS_START = 0x4380;
        public const int HDMA_REGS_END = 0x43FF;
        public const int HDMA_REGS_SIZE = 128;

        // 外设定义
        // Picture Processing Unit 1 - Background Rendering
        public const int PPU1_BASE = 0x2100;
        public static unsafe byte* PPU1_INIDISP => (byte*)0x00004200;
        public static unsafe byte* PPU1_OBSEL => (byte*)0x00004201;
        public static unsafe byte* PPU1_OAMADDL => (byte*)0x00004202;
        public static unsafe byte* PPU1_OAMADDH => (byte*)0x00004203;
        public static unsafe byte* PPU1_OAMDATA => (byte*)0x00004204;
        public static unsafe byte* PPU1_BGMODE => (byte*)0x00004205;
        public static unsafe byte* PPU1_MOSAIC => (byte*)0x00004206;
        public static unsafe byte* PPU1_BG1SC => (byte*)0x00004207;
        public static unsafe byte* PPU1_BG2SC => (byte*)0x00004208;
        public static unsafe byte* PPU1_BG3SC => (byte*)0x00004209;
        public static unsafe byte* PPU1_BG4SC => (byte*)0x0000420A;
        public static unsafe byte* PPU1_BG12NBA => (byte*)0x0000420B;
        public static unsafe byte* PPU1_BG34NBA => (byte*)0x0000420C;
        public static unsafe ushort* PPU1_BG1HOFS => (ushort*)0x0000420D;
        public static unsafe ushort* PPU1_BG1VOFS => (ushort*)0x0000420E;
        public static unsafe ushort* PPU1_BG2HOFS => (ushort*)0x0000420F;
        public static unsafe ushort* PPU1_BG2VOFS => (ushort*)0x00004210;
        public static unsafe ushort* PPU1_BG3HOFS => (ushort*)0x00004211;
        public static unsafe ushort* PPU1_BG3VOFS => (ushort*)0x00004212;
        public static unsafe ushort* PPU1_BG4HOFS => (ushort*)0x00004213;
        public static unsafe ushort* PPU1_BG4VOFS => (ushort*)0x00004214;
        public static unsafe byte* PPU1_VMAIN => (byte*)0x00004215;
        public static unsafe byte* PPU1_VMADDL => (byte*)0x00004216;
        public static unsafe byte* PPU1_VMADDH => (byte*)0x00004217;
        public static unsafe byte* PPU1_VMDATAL => (byte*)0x00004218;
        public static unsafe byte* PPU1_VMDATAH => (byte*)0x00004219;
        public static unsafe byte* PPU1_M7SEL => (byte*)0x0000421A;
        public static unsafe ushort* PPU1_M7A => (ushort*)0x0000421B;
        public static unsafe ushort* PPU1_M7B => (ushort*)0x0000421C;
        public static unsafe ushort* PPU1_M7C => (ushort*)0x0000421D;
        public static unsafe ushort* PPU1_M7D => (ushort*)0x0000421E;
        public static unsafe ushort* PPU1_M7X => (ushort*)0x0000421F;
        public static unsafe ushort* PPU1_M7Y => (ushort*)0x00004220;
        public static unsafe byte* PPU1_CGADD => (byte*)0x00004221;
        public static unsafe byte* PPU1_CGDATA => (byte*)0x00004222;
        public static unsafe byte* PPU1_W12SEL => (byte*)0x00004223;
        public static unsafe byte* PPU1_W34SEL => (byte*)0x00004224;
        public static unsafe byte* PPU1_WOBJSEL => (byte*)0x00004225;
        public static unsafe byte* PPU1_WH0 => (byte*)0x00004226;
        public static unsafe byte* PPU1_WH1 => (byte*)0x00004227;
        public static unsafe byte* PPU1_WH2 => (byte*)0x00004228;
        public static unsafe byte* PPU1_WH3 => (byte*)0x00004229;
        public static unsafe byte* PPU1_WBGLOG => (byte*)0x0000422A;
        public static unsafe byte* PPU1_WOBJLOG => (byte*)0x0000422B;
        public static unsafe byte* PPU1_TM => (byte*)0x0000422C;
        public static unsafe byte* PPU1_TS => (byte*)0x0000422D;
        public static unsafe byte* PPU1_TMW => (byte*)0x0000422E;
        public static unsafe byte* PPU1_TSW => (byte*)0x0000422F;
        public static unsafe byte* PPU1_CGSWSEL => (byte*)0x00004230;
        public static unsafe byte* PPU1_CGADSUB => (byte*)0x00004231;
        public static unsafe byte* PPU1_SETINI => (byte*)0x00004233;

        // Picture Processing Unit 2 - Sprite Rendering
        public const int PPU2_BASE = 0x2140;
        public static unsafe byte* PPU2_OAMDATAREAD => (byte*)0x00004278;
        public static unsafe byte* PPU2_VMDATAREAD => (byte*)0x00004279;
        public static unsafe byte* PPU2_VMDATAHREAD => (byte*)0x0000427A;
        public static unsafe byte* PPU2_CGDATAREAD => (byte*)0x0000427B;
        public static unsafe byte* PPU2_OPHCT => (byte*)0x0000427C;
        public static unsafe byte* PPU2_OPVCT => (byte*)0x0000427D;
        public static unsafe byte* PPU2_STAT78 => (byte*)0x0000427F;

        // Sony SPC700 Audio CPU (8-bit)
        public const int SPC700_BASE = 0x00;
        public static unsafe ushort* SPC700_PC => (ushort*)0x00000000;
        public static unsafe byte* SPC700_A => (byte*)0x00000002;
        public static unsafe byte* SPC700_X => (byte*)0x00000003;
        public static unsafe byte* SPC700_Y => (byte*)0x00000004;
        public static unsafe byte* SPC700_SP => (byte*)0x00000005;
        public static unsafe byte* SPC700_PSW => (byte*)0x00000006;
        public static unsafe byte* SPC700_TEST => (byte*)0x0000000F;

        // S-DSP Audio DSP (8-channel ADPCM)
        public const int DSP_BASE = 0x00;
        public static unsafe byte* DSP_MVOL_L => (byte*)0x0000000C;
        public static unsafe byte* DSP_MVOL_R => (byte*)0x0000001C;
        public static unsafe byte* DSP_EVOL_L => (byte*)0x0000002C;
        public static unsafe byte* DSP_EVOL_R => (byte*)0x0000003C;
        public static unsafe byte* DSP_KON => (byte*)0x0000004C;
        public static unsafe byte* DSP_KOFF => (byte*)0x0000005C;
        public static unsafe byte* DSP_KONKOFF => (byte*)0x0000004D;
        public static unsafe byte* DSP_FLG => (byte*)0x0000006C;
        public static unsafe byte* DSP_ENDX => (byte*)0x0000007D;
        public static unsafe byte* DSP_EBUST => (byte*)0x0000006D;
        public static unsafe byte* DSP_EDL => (byte*)0x0000007D;
        public static unsafe byte* DSP_ENV0 => (byte*)0x00000000;
        public static unsafe byte* DSP_OUT0 => (byte*)0x0000001C;
        public static unsafe byte* DSP_ENV1 => (byte*)0x00000001;
        public static unsafe byte* DSP_OUT1 => (byte*)0x0000002C;
        public static unsafe byte* DSP_ENV2 => (byte*)0x00000002;
        public static unsafe byte* DSP_OUT2 => (byte*)0x0000003C;
        public static unsafe byte* DSP_ENV3 => (byte*)0x00000003;
        public static unsafe byte* DSP_OUT3 => (byte*)0x0000004C;
        public static unsafe byte* DSP_ENV4 => (byte*)0x00000004;
        public static unsafe byte* DSP_OUT4 => (byte*)0x0000005C;
        public static unsafe byte* DSP_ENV5 => (byte*)0x00000005;
        public static unsafe byte* DSP_OUT5 => (byte*)0x0000006C;
        public static unsafe byte* DSP_ENV6 => (byte*)0x00000006;
        public static unsafe byte* DSP_OUT6 => (byte*)0x0000007C;
        public static unsafe byte* DSP_ENV7 => (byte*)0x00000007;
        public static unsafe byte* DSP_OUT7 => (byte*)0x0000000D;
        public static unsafe byte* DSP_V0SRC => (byte*)0x00000008;
        public static unsafe byte* DSP_V1SRC => (byte*)0x00000009;
        public static unsafe byte* DSP_V2SRC => (byte*)0x0000000A;
        public static unsafe byte* DSP_V3SRC => (byte*)0x0000000B;
        public static unsafe byte* DSP_V4SRC => (byte*)0x00000018;
        public static unsafe byte* DSP_V5SRC => (byte*)0x00000019;
        public static unsafe byte* DSP_V6SRC => (byte*)0x0000001A;
        public static unsafe byte* DSP_V7SRC => (byte*)0x0000001B;
        public static unsafe byte* DSP_V0PITCHL => (byte*)0x00000002;
        public static unsafe byte* DSP_V0PITCHH => (byte*)0x00000003;
        public static unsafe byte* DSP_V1PITCHL => (byte*)0x00000012;
        public static unsafe byte* DSP_V1PITCHH => (byte*)0x00000013;
        public static unsafe byte* DSP_V2PITCHL => (byte*)0x00000022;
        public static unsafe byte* DSP_V2PITCHH => (byte*)0x00000023;
        public static unsafe byte* DSP_V3PITCHL => (byte*)0x00000032;
        public static unsafe byte* DSP_V3PITCHH => (byte*)0x00000033;
        public static unsafe byte* DSP_V4PITCHL => (byte*)0x00000042;
        public static unsafe byte* DSP_V4PITCHH => (byte*)0x00000043;
        public static unsafe byte* DSP_V5PITCHL => (byte*)0x00000052;
        public static unsafe byte* DSP_V5PITCHH => (byte*)0x00000053;
        public static unsafe byte* DSP_V6PITCHL => (byte*)0x00000062;
        public static unsafe byte* DSP_V6PITCHH => (byte*)0x00000063;
        public static unsafe byte* DSP_V7PITCHL => (byte*)0x00000072;
        public static unsafe byte* DSP_V7PITCHH => (byte*)0x00000073;
        public static unsafe byte* DSP_V0ADSR0 => (byte*)0x00000004;
        public static unsafe byte* DSP_V0ADSR1 => (byte*)0x00000005;
        public static unsafe byte* DSP_V0ADSR2 => (byte*)0x00000006;
        public static unsafe byte* DSP_V1ADSR0 => (byte*)0x00000014;
        public static unsafe byte* DSP_V1ADSR1 => (byte*)0x00000015;
        public static unsafe byte* DSP_V1ADSR2 => (byte*)0x00000016;
        public static unsafe byte* DSP_V2ADSR0 => (byte*)0x00000024;
        public static unsafe byte* DSP_V2ADSR1 => (byte*)0x00000025;
        public static unsafe byte* DSP_V2ADSR2 => (byte*)0x00000026;
        public static unsafe byte* DSP_V3ADSR0 => (byte*)0x00000034;
        public static unsafe byte* DSP_V3ADSR1 => (byte*)0x00000035;
        public static unsafe byte* DSP_V3ADSR2 => (byte*)0x00000036;
        public static unsafe byte* DSP_V4ADSR0 => (byte*)0x00000044;
        public static unsafe byte* DSP_V4ADSR1 => (byte*)0x00000045;
        public static unsafe byte* DSP_V4ADSR2 => (byte*)0x00000046;
        public static unsafe byte* DSP_V5ADSR0 => (byte*)0x00000054;
        public static unsafe byte* DSP_V5ADSR1 => (byte*)0x00000055;
        public static unsafe byte* DSP_V5ADSR2 => (byte*)0x00000056;
        public static unsafe byte* DSP_V6ADSR0 => (byte*)0x00000064;
        public static unsafe byte* DSP_V6ADSR1 => (byte*)0x00000065;
        public static unsafe byte* DSP_V6ADSR2 => (byte*)0x00000066;
        public static unsafe byte* DSP_V7ADSR0 => (byte*)0x00000074;
        public static unsafe byte* DSP_V7ADSR1 => (byte*)0x00000075;
        public static unsafe byte* DSP_V7ADSR2 => (byte*)0x00000076;
        public static unsafe byte* DSP_V0GAIN => (byte*)0x00000007;
        public static unsafe byte* DSP_V1GAIN => (byte*)0x00000017;
        public static unsafe byte* DSP_V2GAIN => (byte*)0x00000027;
        public static unsafe byte* DSP_V3GAIN => (byte*)0x00000037;
        public static unsafe byte* DSP_V4GAIN => (byte*)0x00000047;
        public static unsafe byte* DSP_V5GAIN => (byte*)0x00000057;
        public static unsafe byte* DSP_V6GAIN => (byte*)0x00000067;
        public static unsafe byte* DSP_V7GAIN => (byte*)0x00000077;
        public static unsafe byte* DSP_V0WAVE => (byte*)0x0000000D;
        public static unsafe byte* DSP_V1WAVE => (byte*)0x0000001D;
        public static unsafe byte* DSP_V2WAVE => (byte*)0x0000002D;
        public static unsafe byte* DSP_V3WAVE => (byte*)0x0000003D;
        public static unsafe byte* DSP_V4WAVE => (byte*)0x0000004D;
        public static unsafe byte* DSP_V5WAVE => (byte*)0x0000005D;
        public static unsafe byte* DSP_V6WAVE => (byte*)0x0000006D;
        public static unsafe byte* DSP_V7WAVE => (byte*)0x0000007D;

        // Direct Memory Access Controller
        public const int DMA_BASE = 0x4300;
        public static unsafe byte* DMA_DMAP0 => (byte*)0x00008600;
        public static unsafe byte* DMA_BBAD0 => (byte*)0x00008601;
        public static unsafe byte* DMA_A1T0L => (byte*)0x00008602;
        public static unsafe byte* DMA_A1T0H => (byte*)0x00008603;
        public static unsafe byte* DMA_A1B0 => (byte*)0x00008604;
        public static unsafe byte* DMA_DAS0L => (byte*)0x00008605;
        public static unsafe byte* DMA_DAS0H => (byte*)0x00008606;
        public static unsafe byte* DMA_DASB0 => (byte*)0x00008607;
        public static unsafe byte* DMA_A2A0 => (byte*)0x00008608;
        public static unsafe byte* DMA_A2A1 => (byte*)0x00008609;
        public static unsafe byte* DMA_A2B0 => (byte*)0x0000860A;
        public static unsafe byte* DMA_NTT0 => (byte*)0x0000860B;
        public static unsafe byte* DMA_DMAP1 => (byte*)0x00008610;
        public static unsafe byte* DMA_BBAD1 => (byte*)0x00008611;
        public static unsafe byte* DMA_A1T1L => (byte*)0x00008612;
        public static unsafe byte* DMA_A1T1H => (byte*)0x00008613;
        public static unsafe byte* DMA_A1B1 => (byte*)0x00008614;
        public static unsafe byte* DMA_DAS1L => (byte*)0x00008615;
        public static unsafe byte* DMA_DAS1H => (byte*)0x00008616;
        public static unsafe byte* DMA_DASB1 => (byte*)0x00008617;
        public static unsafe byte* DMA_DMAP2 => (byte*)0x00008620;
        public static unsafe byte* DMA_BBAD2 => (byte*)0x00008621;
        public static unsafe byte* DMA_A1T2L => (byte*)0x00008622;
        public static unsafe byte* DMA_A1T2H => (byte*)0x00008623;
        public static unsafe byte* DMA_A1B2 => (byte*)0x00008624;
        public static unsafe byte* DMA_DAS2L => (byte*)0x00008625;
        public static unsafe byte* DMA_DAS2H => (byte*)0x00008626;
        public static unsafe byte* DMA_DASB2 => (byte*)0x00008627;
        public static unsafe byte* DMA_DMAP3 => (byte*)0x00008630;
        public static unsafe byte* DMA_BBAD3 => (byte*)0x00008631;
        public static unsafe byte* DMA_A1T3L => (byte*)0x00008632;
        public static unsafe byte* DMA_A1T3H => (byte*)0x00008633;
        public static unsafe byte* DMA_A1B3 => (byte*)0x00008634;
        public static unsafe byte* DMA_DAS3L => (byte*)0x00008635;
        public static unsafe byte* DMA_DAS3H => (byte*)0x00008636;
        public static unsafe byte* DMA_DASB3 => (byte*)0x00008637;
        public static unsafe byte* DMA_MDMAEN => (byte*)0x00008650;

        // Horizontal DMA (scanline-based)
        public const int HDMA_BASE = 0x4380;
        public static unsafe byte* HDMA_HDMAP0 => (byte*)0x00008700;
        public static unsafe byte* HDMA_HBAD0 => (byte*)0x00008701;
        public static unsafe byte* HDMA_A1T0L => (byte*)0x00008702;
        public static unsafe byte* HDMA_A1T0H => (byte*)0x00008703;
        public static unsafe byte* HDMA_A1B0 => (byte*)0x00008704;
        public static unsafe byte* HDMA_DAS0L => (byte*)0x00008705;
        public static unsafe byte* HDMA_DAS0H => (byte*)0x00008706;
        public static unsafe byte* HDMA_HDMAP1 => (byte*)0x00008708;
        public static unsafe byte* HDMA_HBAD1 => (byte*)0x00008709;
        public static unsafe byte* HDMA_A1T1L => (byte*)0x0000870A;
        public static unsafe byte* HDMA_A1T1H => (byte*)0x0000870B;
        public static unsafe byte* HDMA_A1B1 => (byte*)0x0000870C;
        public static unsafe byte* HDMA_DAS1L => (byte*)0x0000870D;
        public static unsafe byte* HDMA_DAS1H => (byte*)0x0000870E;
        public static unsafe byte* HDMA_HDMAP2 => (byte*)0x00008710;
        public static unsafe byte* HDMA_HBAD2 => (byte*)0x00008711;
        public static unsafe byte* HDMA_A1T2L => (byte*)0x00008712;
        public static unsafe byte* HDMA_A1T2H => (byte*)0x00008713;
        public static unsafe byte* HDMA_A1B2 => (byte*)0x00008714;
        public static unsafe byte* HDMA_DAS2L => (byte*)0x00008715;
        public static unsafe byte* HDMA_DAS2H => (byte*)0x00008716;
        public static unsafe byte* HDMA_HDMAP3 => (byte*)0x00008718;
        public static unsafe byte* HDMA_HBAD3 => (byte*)0x00008719;
        public static unsafe byte* HDMA_A1T3L => (byte*)0x0000871A;
        public static unsafe byte* HDMA_A1T3H => (byte*)0x0000871B;
        public static unsafe byte* HDMA_A1B3 => (byte*)0x0000871C;
        public static unsafe byte* HDMA_DAS3L => (byte*)0x0000871D;
        public static unsafe byte* HDMA_DAS3H => (byte*)0x0000871E;
        public static unsafe byte* HDMA_HDMAEN => (byte*)0x00008770;

        // Controller Port 1
        public const int CONTROLLER1_BASE = 0x4016;
        public static unsafe byte* CONTROLLER1_JOYPAD1 => (byte*)0x0000802C;
        public static unsafe byte* CONTROLLER1_JOYSTROBE => (byte*)0x0000802C;

        // Controller Port 2
        public const int CONTROLLER2_BASE = 0x4017;
        public static unsafe byte* CONTROLLER2_JOYPAD2 => (byte*)0x0000802E;
        public static unsafe byte* CONTROLLER2_RDNMI => (byte*)0x00008227;
        public static unsafe byte* CONTROLLER2_TIMEUP => (byte*)0x00008228;
        public static unsafe byte* CONTROLLER2_HVBJOY => (byte*)0x00008229;

        // Timer / IRQ Control
        public const int TIMER_BASE = 0x4200;
        public static unsafe byte* TIMER_NMITIMEN => (byte*)0x00008400;
        public const int TIMER_NMITIMEN_VBLANK_NMI = 7;  // V-Blank NMI Enable
        public const int TIMER_NMITIMEN_HTIMER_EN = 4;  // H-Counter IRQ Enable
        public const int TIMER_NMITIMEN_VTIMER_EN = 5;  // V-Counter IRQ Enable
        public static unsafe byte* TIMER_WRI00 => (byte*)0x00008401;
        public static unsafe byte* TIMER_HTIMEL => (byte*)0x00008402;
        public static unsafe byte* TIMER_HTIMEH => (byte*)0x00008403;
        public static unsafe byte* TIMER_VTIMEL => (byte*)0x00008404;
        public static unsafe byte* TIMER_VTIMEH => (byte*)0x00008405;
        public static unsafe byte* TIMER_MEMSEL => (byte*)0x0000840D;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt (V-Blank)
        public const int IRQ_IRQ = 2;  // IRQ / BRK (Timer, HDMA, Controller)
        public const int IRQ_TIMER_IRQ = 3;  // H/V Counter Timer IRQ

        // 引脚定义
        public const int PIN_VCC = 1;  // Power Supply
        public const int PIN_GND = 2;  // Ground
        public const int PIN_CLK = 3;  // System Clock Input (21.47727 MHz)
        public const int PIN_RESET = 4;  // Reset Signal
        public const int PIN_NMI = 5;  // Non-Maskable Interrupt
        public const int PIN_IRQ = 6;  // Interrupt Request
        public const int PIN_RDY = 7;  // Ready / Wait State
        public const int PIN_AB0 = 8;  // Address Bus Bit 0
        public const int PIN_AB1 = 9;  // Address Bus Bit 1
        public const int PIN_AB2 = 10;  // Address Bus Bit 2
        public const int PIN_AB3 = 11;  // Address Bus Bit 3
        public const int PIN_AB4 = 12;  // Address Bus Bit 4
        public const int PIN_AB5 = 13;  // Address Bus Bit 5
        public const int PIN_AB6 = 14;  // Address Bus Bit 6
        public const int PIN_AB7 = 15;  // Address Bus Bit 7
        public const int PIN_AB8 = 16;  // Address Bus Bit 8
        public const int PIN_AB9 = 17;  // Address Bus Bit 9
        public const int PIN_AB10 = 18;  // Address Bus Bit 10
        public const int PIN_AB11 = 19;  // Address Bus Bit 11
        public const int PIN_AB12 = 20;  // Address Bus Bit 12
        public const int PIN_AB13 = 21;  // Address Bus Bit 13
        public const int PIN_AB14 = 22;  // Address Bus Bit 14
        public const int PIN_AB15 = 23;  // Address Bus Bit 15
        public const int PIN_AB16 = 24;  // Address Bus Bit 16
        public const int PIN_AB17 = 25;  // Address Bus Bit 17
        public const int PIN_AB18 = 26;  // Address Bus Bit 18
        public const int PIN_AB19 = 27;  // Address Bus Bit 19
        public const int PIN_AB20 = 28;  // Address Bus Bit 20
        public const int PIN_AB21 = 29;  // Address Bus Bit 21
        public const int PIN_AB22 = 30;  // Address Bus Bit 22
        public const int PIN_AB23 = 31;  // Address Bus Bit 23
        public const int PIN_DB0 = 32;  // Data Bus Bit 0
        public const int PIN_DB1 = 33;  // Data Bus Bit 1
        public const int PIN_DB2 = 34;  // Data Bus Bit 2
        public const int PIN_DB3 = 35;  // Data Bus Bit 3
        public const int PIN_DB4 = 36;  // Data Bus Bit 4
        public const int PIN_DB5 = 37;  // Data Bus Bit 5
        public const int PIN_DB6 = 38;  // Data Bus Bit 6
        public const int PIN_DB7 = 39;  // Data Bus Bit 7
        public const int PIN_PHI = 40;  // Phase Out Clock

        public static void ricoh_5a22_init()
        {
            // 硬件初始化代码
        }
    }
}
