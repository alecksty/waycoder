package vml.device.mostechnology.mos_6507;

/**
 * MOS-6507 寄存器定义
 * 生成自: MOS Technology/MOS-6502/MOS-6507
 * 版本: 1.0
 */
public final class MOS_6507 {
    private MOS_6507() {} // 工具类
    // CPU架构: MOS-6507, 8位, 1190000 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // X Index
    public static final int X_ADDR = (int)0x01;

    // Y Index
    public static final int Y_ADDR = (int)0x02;

    // Stack Pointer (6-bit, 128-byte stack)
    public static final int SP_ADDR = (int)0x03;

    // Program Counter (16-bit)
    public static final int PC_ADDR = (int)0x04;

    // Processor Status
    public static final int P_ADDR = (int)0x06;
    public static final int P_N = 7;  // Negative
    public static final int P_V = 6;  // Overflow
    public static final int P_B = 4;  // Break
    public static final int P_D = 3;  // Decimal Mode (N/A on 6507)
    public static final int P_I = 2;  // Interrupt Disable
    public static final int P_Z = 1;  // Zero
    public static final int P_C = 0;  // Carry

    // 内存段定义
    // TIA Registers
    public static final int TIA_REGS_START = (int)0x0000;
    public static final int TIA_REGS_END = (int)0x007F;
    public static final int TIA_REGS_SIZE = 128;

    // RIOT 128byte RAM mirrored
    public static final int RIOT_RAM_START = (int)0x0080;
    public static final int RIOT_RAM_END = (int)0x00FF;
    public static final int RIOT_RAM_SIZE = 128;

    // RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
    public static final int RIOT_IO_START = (int)0x0280;
    public static final int RIOT_IO_END = (int)0x029F;
    public static final int RIOT_IO_SIZE = 32;

    // Cartridge ROM (4KB, bank-switched)
    public static final int CART_ROM_START = (int)0x1000;
    public static final int CART_ROM_END = (int)0x1FFF;
    public static final int CART_ROM_SIZE = 4096;

    // 外设定义
    // Television Interface Adaptor (Video + Audio + I/O)
    public static final int TIA_BASE = (int)0x0000;
    public static final int TIA_VSYNC = (int)0x00000000;
    public static final int TIA_VBLANK = (int)0x00000001;
    public static final int TIA_VBLANK_D7 = 7;  // Inhibit D7 (1=disable D7 output to PB7)
    public static final int TIA_VBLANK_D6 = 6;  // Inhibit D6 (1=disable D6 output to PB6)
    public static final int TIA_VBLANK_D5 = 5;  // Inhibit D5 (1=disable D5 output to PB5)
    public static final int TIA_VBLANK_D4 = 4;  // Inhibit D4 (1=disable D4 output to PB4)
    public static final int TIA_VBLANK_D3 = 3;  // Inhibit D3 (1=disable D3 output to PB3)
    public static final int TIA_VBLANK_D2 = 2;  // Inhibit D2 (1=disable D2 output to PB2)
    public static final int TIA_VBLANK_D1 = 1;  // Inhibit D1 (1=disable D1 output to PB1)
    public static final int TIA_VBLANK_D0 = 0;  // Inhibit D0 (1=disable D0 output to PB0)
    public static final int TIA_VBLANK_VBW = 5;  // Vertical Blank Enable (1=set VBLANK)
    public static final int TIA_VBLANK_VBL = 1;  // Vertical Blank Set (1=V-Blank active)
    public static final int TIA_VBLANK_RESBL = 0;  // Reset Blank (1=allow VSYNC/VBLANK reset on clock)
    public static final int TIA_WSYNC = (int)0x00000002;
    public static final int TIA_RSYNC = (int)0x00000003;
    public static final int TIA_NUSIZ0 = (int)0x00000004;
    public static final int TIA_NUSIZ0_NUSIZ = 0;  // Number/Size Code (0-7)
    public static final int TIA_NUSIZ0_MISSILE_SIZE = 0;  // Missile Size
    public static final int TIA_NUSIZ0_RESM0 = 6;  // Reset M0
    public static final int TIA_NUSIZ0_RESM1 = 7;  // Reset M1
    public static final int TIA_NUSIZ1 = (int)0x00000005;
    public static final int TIA_COLUP0 = (int)0x00000006;
    public static final int TIA_COLUP1 = (int)0x00000007;
    public static final int TIA_COLUPF = (int)0x00000008;
    public static final int TIA_COLUBK = (int)0x00000009;
    public static final int TIA_CTRLPF = (int)0x0000000A;
    public static final int TIA_CTRLPF_DELL = 0;  // Delay Playfield L (Reflected/Left score)
    public static final int TIA_CTRLPF_BALL_SIZE = 0;  // Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
    public static final int TIA_CTRLPF_REF = 5;  // Reflect (1=mirror playfield)
    public static final int TIA_CTRLPF_SCORE = 6;  // Score Mode (1=use player colors for L/R halves)
    public static final int TIA_CTRLPF_DELBL = 7;  // Delay Ball (1=delay ball 1 clock)
    public static final int TIA_REFPL = (int)0x0000000B;
    public static final int TIA_PF0 = (int)0x0000000D;
    public static final int TIA_PF1 = (int)0x0000000E;
    public static final int TIA_PF2 = (int)0x0000000F;
    public static final int TIA_RESP0 = (int)0x00000010;
    public static final int TIA_RESP1 = (int)0x00000011;
    public static final int TIA_RESM0 = (int)0x00000012;
    public static final int TIA_RESM1 = (int)0x00000013;
    public static final int TIA_RESBL = (int)0x00000014;
    public static final int TIA_AUDC0 = (int)0x00000015;
    public static final int TIA_AUDC0_VOL = 0;  // Volume (0-15)
    public static final int TIA_AUDC0_TONE = 0;  // Tone Divisor (5-bit counter)
    public static final int TIA_AUDC1 = (int)0x00000016;
    public static final int TIA_AUDF0 = (int)0x00000017;
    public static final int TIA_AUDF1 = (int)0x00000018;
    public static final int TIA_AUDV0 = (int)0x00000019;
    public static final int TIA_AUDV1 = (int)0x0000001A;
    public static final int TIA_GRP0 = (int)0x0000001B;
    public static final int TIA_GRP1 = (int)0x0000001C;
    public static final int TIA_DGRP0 = (int)0x0000001D;
    public static final int TIA_DGRP1 = (int)0x0000001E;
    public static final int TIA_ENAM0 = (int)0x0000001F;
    public static final int TIA_ENAM1 = (int)0x00000020;
    public static final int TIA_ENABL = (int)0x00000021;
    public static final int TIA_HMP0 = (int)0x00000022;
    public static final int TIA_HMP1 = (int)0x00000023;
    public static final int TIA_HMM0 = (int)0x00000024;
    public static final int TIA_HMM1 = (int)0x00000025;
    public static final int TIA_HMBL = (int)0x00000026;
    public static final int TIA_VDEL0 = (int)0x00000027;
    public static final int TIA_VDEL1 = (int)0x00000028;
    public static final int TIA_VDELBL = (int)0x00000029;
    public static final int TIA_RESBB = (int)0x0000002A;
    public static final int TIA_HMOVE = (int)0x0000002A;
    public static final int TIA_HMCLR = (int)0x0000002B;
    public static final int TIA_CXM0P = (int)0x00000030;
    public static final int TIA_CXM1P = (int)0x00000031;
    public static final int TIA_CXP0FB = (int)0x00000032;
    public static final int TIA_CXP1FB = (int)0x00000033;
    public static final int TIA_CXM0FB = (int)0x00000034;
    public static final int TIA_CXM1FB = (int)0x00000035;
    public static final int TIA_CXBLPF = (int)0x00000036;
    public static final int TIA_CXPPMM = (int)0x00000037;
    public static final int TIA_INPT0 = (int)0x00000038;
    public static final int TIA_INPT1 = (int)0x00000039;
    public static final int TIA_INPT2 = (int)0x0000003A;
    public static final int TIA_INPT3 = (int)0x0000003B;
    public static final int TIA_INPT4 = (int)0x0000003C;
    public static final int TIA_INPT5 = (int)0x0000003D;

    // RAM, I/O, Timer (6532 RIOT)
    public static final int RIOT_BASE = (int)0x0080;
    public static final int RIOT_SWCHA = (int)0x00000300;
    public static final int RIOT_SWACNT = (int)0x00000301;
    public static final int RIOT_SWCHB = (int)0x00000302;
    public static final int RIOT_SWCHB_RESET = 1;  // Game Reset Switch (0=pressed)
    public static final int RIOT_SWCHB_SELECT = 2;  // Game Select Switch (0=pressed)
    public static final int RIOT_SWCHB_DIFFB = 3;  // Difficulty B (0=hard, 1=easy)
    public static final int RIOT_SWCHB_DIFFA = 4;  // Difficulty A (0=hard, 1=easy)
    public static final int RIOT_SWBCNT = (int)0x00000303;
    public static final int RIOT_INTIM = (int)0x00000304;
    public static final int RIOT_TIMINT = (int)0x00000305;
    public static final int RIOT_TIM1T = (int)0x00000314;
    public static final int RIOT_TIM8T = (int)0x00000315;
    public static final int RIOT_TIM64T = (int)0x00000316;
    public static final int RIOT_TIM1024T = (int)0x00000317;

    // Controller Port 1 (Joystick)
    public static final int CONTROLLER1_BASE = (int)0x280;
    public static final int CONTROLLER1_SWCHA = (int)0x00000500;

    // Controller Port 2 (Joystick)
    public static final int CONTROLLER2_BASE = (int)0x281;
    public static final int CONTROLLER2_SWCHA = (int)0x00000501;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-On Reset

    // 引脚定义
    public static final int PIN_VSS = 1;  // Ground
    public static final int PIN_VCC = 2;  // Power Supply
    public static final int PIN_PHI0 = 3;  // Clock Input (1.19MHz NTSC / 1.18MHz PAL)
    public static final int PIN_RESET = 4;  // Reset (active low)
    public static final int PIN_A0 = 5;  // Address Bus Bit 0
    public static final int PIN_A1 = 6;  // Address Bus Bit 1
    public static final int PIN_A2 = 7;  // Address Bus Bit 2
    public static final int PIN_A3 = 8;  // Address Bus Bit 3
    public static final int PIN_A4 = 9;  // Address Bus Bit 4
    public static final int PIN_A5 = 10;  // Address Bus Bit 5
    public static final int PIN_A6 = 11;  // Address Bus Bit 6
    public static final int PIN_A7 = 12;  // Address Bus Bit 7
    public static final int PIN_A8 = 13;  // Address Bus Bit 8
    public static final int PIN_A9 = 14;  // Address Bus Bit 9
    public static final int PIN_A10 = 15;  // Address Bus Bit 10
    public static final int PIN_A11 = 16;  // Address Bus Bit 11
    public static final int PIN_A12 = 17;  // Address Bus Bit 12
    public static final int PIN_D0 = 18;  // Data Bus Bit 0
    public static final int PIN_D1 = 19;  // Data Bus Bit 1
    public static final int PIN_D2 = 20;  // Data Bus Bit 2
    public static final int PIN_D3 = 21;  // Data Bus Bit 3
    public static final int PIN_D4 = 22;  // Data Bus Bit 4
    public static final int PIN_D5 = 23;  // Data Bus Bit 5
    public static final int PIN_D6 = 24;  // Data Bus Bit 6
    public static final int PIN_D7 = 25;  // Data Bus Bit 7
    public static final int PIN_RDY = 26;  // Ready (stops CPU on read)
    public static final int PIN_R_W = 27;  // Read/Write (1=Read, 0=Write)
    public static final int PIN_NC = 28;  // Not Connected

    public static native void mos_6507_init();
}
