using System;

namespace VML.Device.MOSTechnology.MOS_6507
{
    /// <summary>
    /// MOS-6507 寄存器定义
    /// 生成自: MOS Technology/MOS-6502/MOS-6507
    /// 版本: 1.0
    /// </summary>
    public static class MOS_6507
    {
        // CPU架构: MOS-6507, 8位, 1190000 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // X Index
        public const int X_ADDR = 0x01;
        public static unsafe byte* X => (byte*)0x01;

        // Y Index
        public const int Y_ADDR = 0x02;
        public static unsafe byte* Y => (byte*)0x02;

        // Stack Pointer (6-bit, 128-byte stack)
        public const int SP_ADDR = 0x03;
        public static unsafe byte* SP => (byte*)0x03;

        // Program Counter (16-bit)
        public const int PC_ADDR = 0x04;
        public static unsafe ushort* PC => (ushort*)0x04;

        // Processor Status
        public const int P_ADDR = 0x06;
        public static unsafe byte* P => (byte*)0x06;
        public const int P_N = 7;  // Negative
        public const int P_V = 6;  // Overflow
        public const int P_B = 4;  // Break
        public const int P_D = 3;  // Decimal Mode (N/A on 6507)
        public const int P_I = 2;  // Interrupt Disable
        public const int P_Z = 1;  // Zero
        public const int P_C = 0;  // Carry

        // 内存段定义
        // TIA Registers
        public const int TIA_REGS_START = 0x0000;
        public const int TIA_REGS_END = 0x007F;
        public const int TIA_REGS_SIZE = 128;

        // RIOT 128byte RAM mirrored
        public const int RIOT_RAM_START = 0x0080;
        public const int RIOT_RAM_END = 0x00FF;
        public const int RIOT_RAM_SIZE = 128;

        // RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
        public const int RIOT_IO_START = 0x0280;
        public const int RIOT_IO_END = 0x029F;
        public const int RIOT_IO_SIZE = 32;

        // Cartridge ROM (4KB, bank-switched)
        public const int CART_ROM_START = 0x1000;
        public const int CART_ROM_END = 0x1FFF;
        public const int CART_ROM_SIZE = 4096;

        // 外设定义
        // Television Interface Adaptor (Video + Audio + I/O)
        public const int TIA_BASE = 0x0000;
        public static unsafe byte* TIA_VSYNC => (byte*)0x00000000;
        public static unsafe byte* TIA_VBLANK => (byte*)0x00000001;
        public const int TIA_VBLANK_D7 = 7;  // Inhibit D7 (1=disable D7 output to PB7)
        public const int TIA_VBLANK_D6 = 6;  // Inhibit D6 (1=disable D6 output to PB6)
        public const int TIA_VBLANK_D5 = 5;  // Inhibit D5 (1=disable D5 output to PB5)
        public const int TIA_VBLANK_D4 = 4;  // Inhibit D4 (1=disable D4 output to PB4)
        public const int TIA_VBLANK_D3 = 3;  // Inhibit D3 (1=disable D3 output to PB3)
        public const int TIA_VBLANK_D2 = 2;  // Inhibit D2 (1=disable D2 output to PB2)
        public const int TIA_VBLANK_D1 = 1;  // Inhibit D1 (1=disable D1 output to PB1)
        public const int TIA_VBLANK_D0 = 0;  // Inhibit D0 (1=disable D0 output to PB0)
        public const int TIA_VBLANK_VBW = 5;  // Vertical Blank Enable (1=set VBLANK)
        public const int TIA_VBLANK_VBL = 1;  // Vertical Blank Set (1=V-Blank active)
        public const int TIA_VBLANK_RESBL = 0;  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
        public static unsafe byte* TIA_WSYNC => (byte*)0x00000002;
        public static unsafe byte* TIA_RSYNC => (byte*)0x00000003;
        public static unsafe byte* TIA_NUSIZ0 => (byte*)0x00000004;
        public const int TIA_NUSIZ0_NUSIZ = 0;  // Number/Size Code (0-7)
        public const int TIA_NUSIZ0_MISSILE_SIZE = 0;  // Missile Size
        public const int TIA_NUSIZ0_RESM0 = 6;  // Reset M0
        public const int TIA_NUSIZ0_RESM1 = 7;  // Reset M1
        public static unsafe byte* TIA_NUSIZ1 => (byte*)0x00000005;
        public static unsafe byte* TIA_COLUP0 => (byte*)0x00000006;
        public static unsafe byte* TIA_COLUP1 => (byte*)0x00000007;
        public static unsafe byte* TIA_COLUPF => (byte*)0x00000008;
        public static unsafe byte* TIA_COLUBK => (byte*)0x00000009;
        public static unsafe byte* TIA_CTRLPF => (byte*)0x0000000A;
        public const int TIA_CTRLPF_DELL = 0;  // Delay Playfield L (Reflected/Left score)
        public const int TIA_CTRLPF_BALL_SIZE = 0;  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
        public const int TIA_CTRLPF_REF = 5;  // Reflect (1=mirror playfield)
        public const int TIA_CTRLPF_SCORE = 6;  // Score Mode (1=use player colors for L/R halves)
        public const int TIA_CTRLPF_DELBL = 7;  // Delay Ball (1=delay ball 1 clock)
        public static unsafe byte* TIA_REFPL => (byte*)0x0000000B;
        public static unsafe byte* TIA_PF0 => (byte*)0x0000000D;
        public static unsafe byte* TIA_PF1 => (byte*)0x0000000E;
        public static unsafe byte* TIA_PF2 => (byte*)0x0000000F;
        public static unsafe byte* TIA_RESP0 => (byte*)0x00000010;
        public static unsafe byte* TIA_RESP1 => (byte*)0x00000011;
        public static unsafe byte* TIA_RESM0 => (byte*)0x00000012;
        public static unsafe byte* TIA_RESM1 => (byte*)0x00000013;
        public static unsafe byte* TIA_RESBL => (byte*)0x00000014;
        public static unsafe byte* TIA_AUDC0 => (byte*)0x00000015;
        public const int TIA_AUDC0_VOL = 0;  // Volume (0-15)
        public const int TIA_AUDC0_TONE = 0;  // Tone Divisor (5-bit counter)
        public static unsafe byte* TIA_AUDC1 => (byte*)0x00000016;
        public static unsafe byte* TIA_AUDF0 => (byte*)0x00000017;
        public static unsafe byte* TIA_AUDF1 => (byte*)0x00000018;
        public static unsafe byte* TIA_AUDV0 => (byte*)0x00000019;
        public static unsafe byte* TIA_AUDV1 => (byte*)0x0000001A;
        public static unsafe byte* TIA_GRP0 => (byte*)0x0000001B;
        public static unsafe byte* TIA_GRP1 => (byte*)0x0000001C;
        public static unsafe byte* TIA_DGRP0 => (byte*)0x0000001D;
        public static unsafe byte* TIA_DGRP1 => (byte*)0x0000001E;
        public static unsafe byte* TIA_ENAM0 => (byte*)0x0000001F;
        public static unsafe byte* TIA_ENAM1 => (byte*)0x00000020;
        public static unsafe byte* TIA_ENABL => (byte*)0x00000021;
        public static unsafe byte* TIA_HMP0 => (byte*)0x00000022;
        public static unsafe byte* TIA_HMP1 => (byte*)0x00000023;
        public static unsafe byte* TIA_HMM0 => (byte*)0x00000024;
        public static unsafe byte* TIA_HMM1 => (byte*)0x00000025;
        public static unsafe byte* TIA_HMBL => (byte*)0x00000026;
        public static unsafe byte* TIA_VDEL0 => (byte*)0x00000027;
        public static unsafe byte* TIA_VDEL1 => (byte*)0x00000028;
        public static unsafe byte* TIA_VDELBL => (byte*)0x00000029;
        public static unsafe byte* TIA_RESBB => (byte*)0x0000002A;
        public static unsafe byte* TIA_HMOVE => (byte*)0x0000002A;
        public static unsafe byte* TIA_HMCLR => (byte*)0x0000002B;
        public static unsafe byte* TIA_CXM0P => (byte*)0x00000030;
        public static unsafe byte* TIA_CXM1P => (byte*)0x00000031;
        public static unsafe byte* TIA_CXP0FB => (byte*)0x00000032;
        public static unsafe byte* TIA_CXP1FB => (byte*)0x00000033;
        public static unsafe byte* TIA_CXM0FB => (byte*)0x00000034;
        public static unsafe byte* TIA_CXM1FB => (byte*)0x00000035;
        public static unsafe byte* TIA_CXBLPF => (byte*)0x00000036;
        public static unsafe byte* TIA_CXPPMM => (byte*)0x00000037;
        public static unsafe byte* TIA_INPT0 => (byte*)0x00000038;
        public static unsafe byte* TIA_INPT1 => (byte*)0x00000039;
        public static unsafe byte* TIA_INPT2 => (byte*)0x0000003A;
        public static unsafe byte* TIA_INPT3 => (byte*)0x0000003B;
        public static unsafe byte* TIA_INPT4 => (byte*)0x0000003C;
        public static unsafe byte* TIA_INPT5 => (byte*)0x0000003D;

        // RAM, I/O, Timer (6532 RIOT)
        public const int RIOT_BASE = 0x0080;
        public static unsafe byte* RIOT_SWCHA => (byte*)0x00000300;
        public static unsafe byte* RIOT_SWACNT => (byte*)0x00000301;
        public static unsafe byte* RIOT_SWCHB => (byte*)0x00000302;
        public const int RIOT_SWCHB_RESET = 1;  // Game Reset Switch (0=pressed)
        public const int RIOT_SWCHB_SELECT = 2;  // Game Select Switch (0=pressed)
        public const int RIOT_SWCHB_DIFFB = 3;  // Difficulty B (0=hard, 1=easy)
        public const int RIOT_SWCHB_DIFFA = 4;  // Difficulty A (0=hard, 1=easy)
        public static unsafe byte* RIOT_SWBCNT => (byte*)0x00000303;
        public static unsafe byte* RIOT_INTIM => (byte*)0x00000304;
        public static unsafe byte* RIOT_TIMINT => (byte*)0x00000305;
        public static unsafe byte* RIOT_TIM1T => (byte*)0x00000314;
        public static unsafe byte* RIOT_TIM8T => (byte*)0x00000315;
        public static unsafe byte* RIOT_TIM64T => (byte*)0x00000316;
        public static unsafe byte* RIOT_TIM1024T => (byte*)0x00000317;

        // Controller Port 1 (Joystick)
        public const int CONTROLLER1_BASE = 0x280;
        public static unsafe byte* CONTROLLER1_SWCHA => (byte*)0x00000500;

        // Controller Port 2 (Joystick)
        public const int CONTROLLER2_BASE = 0x281;
        public static unsafe byte* CONTROLLER2_SWCHA => (byte*)0x00000501;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-On Reset

        // 引脚定义
        public const int PIN_VSS = 1;  // Ground
        public const int PIN_VCC = 2;  // Power Supply
        public const int PIN_PHI0 = 3;  // Clock Input (1.19MHz NTSC / 1.18MHz PAL)
        public const int PIN_RESET = 4;  // Reset (active low)
        public const int PIN_A0 = 5;  // Address Bus Bit 0
        public const int PIN_A1 = 6;  // Address Bus Bit 1
        public const int PIN_A2 = 7;  // Address Bus Bit 2
        public const int PIN_A3 = 8;  // Address Bus Bit 3
        public const int PIN_A4 = 9;  // Address Bus Bit 4
        public const int PIN_A5 = 10;  // Address Bus Bit 5
        public const int PIN_A6 = 11;  // Address Bus Bit 6
        public const int PIN_A7 = 12;  // Address Bus Bit 7
        public const int PIN_A8 = 13;  // Address Bus Bit 8
        public const int PIN_A9 = 14;  // Address Bus Bit 9
        public const int PIN_A10 = 15;  // Address Bus Bit 10
        public const int PIN_A11 = 16;  // Address Bus Bit 11
        public const int PIN_A12 = 17;  // Address Bus Bit 12
        public const int PIN_D0 = 18;  // Data Bus Bit 0
        public const int PIN_D1 = 19;  // Data Bus Bit 1
        public const int PIN_D2 = 20;  // Data Bus Bit 2
        public const int PIN_D3 = 21;  // Data Bus Bit 3
        public const int PIN_D4 = 22;  // Data Bus Bit 4
        public const int PIN_D5 = 23;  // Data Bus Bit 5
        public const int PIN_D6 = 24;  // Data Bus Bit 6
        public const int PIN_D7 = 25;  // Data Bus Bit 7
        public const int PIN_RDY = 26;  // Ready (stops CPU on read)
        public const int PIN_R_W = 27;  // Read/Write (1=Read, 0=Write)
        public const int PIN_NC = 28;  // Not Connected

        public static void mos_6507_init()
        {
            // 硬件初始化代码
        }
    }
}
