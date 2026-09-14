using System;

namespace VML.Device.AppleComputer.Apple_IIe
{
    /// <summary>
    /// Apple-IIe 寄存器定义
    /// 生成自: Apple Computer/Apple II/Apple-IIe
    /// 版本: 1.0
    /// </summary>
    public static class Apple_IIe
    {
        // CPU架构: MOS-6502, 8位, 1021800 Hz

        // 寄存器定义
        // Accumulator
        public const int A_ADDR = 0x00;
        public static unsafe byte* A => (byte*)0x00;

        // X Index Register
        public const int X_ADDR = 0x01;
        public static unsafe byte* X => (byte*)0x01;

        // Y Index Register
        public const int Y_ADDR = 0x02;
        public static unsafe byte* Y => (byte*)0x02;

        // Stack Pointer
        public const int SP_ADDR = 0x03;
        public static unsafe byte* SP => (byte*)0x03;

        // Program Counter
        public const int PC_ADDR = 0x04;
        public static unsafe ushort* PC => (ushort*)0x04;

        // Processor Status
        public const int P_ADDR = 0x06;
        public static unsafe byte* P => (byte*)0x06;
        public const int P_C = 0;  // Carry Flag
        public const int P_Z = 1;  // Zero Flag
        public const int P_I = 2;  // Interrupt Disable
        public const int P_D = 3;  // Decimal Mode
        public const int P_B = 4;  // Break Command
        public const int P_U = 5;  // Unused
        public const int P_V = 6;  // Overflow Flag
        public const int P_N = 7;  // Negative Flag

        // 内存段定义
        // Main RAM (48KB base, up to 64KB with slot RAM)
        public const int MAIN_RAM_START = 0x0000;
        public const int MAIN_RAM_END = 0xBFFF;
        public const int MAIN_RAM_SIZE = 49152;

        // Text screen buffer (40x24)
        public const int TEXT_RAM_START = 0x0400;
        public const int TEXT_RAM_END = 0x07FF;
        public const int TEXT_RAM_SIZE = 1024;

        // High-resolution graphics buffer
        public const int HIRES_RAM_START = 0x2000;
        public const int HIRES_RAM_END = 0x5FFF;
        public const int HIRES_RAM_SIZE = 16384;

        // 80-column text auxiliary RAM
        public const int AUX_RAM_START = 0x0400;
        public const int AUX_RAM_END = 0x09FF;
        public const int AUX_RAM_SIZE = 1536;

        // Monitor ROM (applesoft/Integer)
        public const int MONITOR_ROM_START = 0xC100;
        public const int MONITOR_ROM_END = 0xCFFF;
        public const int MONITOR_ROM_SIZE = 3840;

        // Applesoft BASIC ROM
        public const int BASIC_ROM_START = 0xD000;
        public const int BASIC_ROM_END = 0xFFFF;
        public const int BASIC_ROM_SIZE = 12288;

        // Expansion Slot ROM
        public const int SLOT_ROM_START = 0xC100;
        public const int SLOT_ROM_END = 0xC7FF;
        public const int SLOT_ROM_SIZE = 768;

        // I/O Select (slot space)
        public const int MMIO_START = 0xC080;
        public const int MMIO_END = 0xC0FF;
        public const int MMIO_SIZE = 128;

        // 外设定义
        // Versatile Interface Adapter (6522)
        public const int VIA_BASE = 0xC000;
        public static unsafe byte* VIA_ORB => (byte*)0x00018000;
        public static unsafe byte* VIA_ORA => (byte*)0x00018001;
        public static unsafe byte* VIA_DDRB => (byte*)0x00018002;
        public static unsafe byte* VIA_DDRA => (byte*)0x00018003;
        public static unsafe ushort* VIA_T1C => (ushort*)0x00018004;
        public static unsafe ushort* VIA_T1L => (ushort*)0x00018006;
        public static unsafe ushort* VIA_T2C => (ushort*)0x00018008;
        public static unsafe byte* VIA_SR => (byte*)0x0001800A;
        public static unsafe byte* VIA_ACR => (byte*)0x0001800B;
        public static unsafe byte* VIA_PCR => (byte*)0x0001800C;
        public static unsafe byte* VIA_IFG => (byte*)0x0001800D;
        public static unsafe byte* VIA_IER => (byte*)0x0001800E;
        public static unsafe byte* VIA_ORA_NH => (byte*)0x0001800F;

        // Peripheral Interface Adapter (6520)
        public const int PIA_BASE = 0xC010;
        public static unsafe byte* PIA_PA => (byte*)0x00018020;
        public static unsafe byte* PIA_PB => (byte*)0x00018021;
        public static unsafe byte* PIA_DDRA => (byte*)0x00018022;
        public static unsafe byte* PIA_DDRB => (byte*)0x00018023;
        public static unsafe byte* PIA_CA1 => (byte*)0x00018024;
        public static unsafe byte* PIA_CA2 => (byte*)0x00018025;
        public static unsafe byte* PIA_CB1 => (byte*)0x00018026;
        public static unsafe byte* PIA_CB2 => (byte*)0x00018027;

        // Keyboard (via PIA)
        public const int KBD_BASE = 0xC000;
        public static unsafe byte* KBD_KEYDATA => (byte*)0x00018000;
        public static unsafe byte* KBD_KEYSTROBE => (byte*)0x00018010;
        public static unsafe byte* KBD_KBDCTRL => (byte*)0x00018025;
        public static unsafe byte* KBD_KBDERR => (byte*)0x00018026;

        // Speaker
        public const int SPEAKER_BASE = 0xC030;
        public static unsafe byte* SPEAKER_SPKR => (byte*)0x00018060;

        // Game I/O Port
        public const int GAME_PORT_BASE = 0xC050;
        public static unsafe byte* GAME_PORT_GAME_SW0 => (byte*)0x000180B1;
        public static unsafe byte* GAME_PORT_GAME_SW1 => (byte*)0x000180B2;
        public static unsafe byte* GAME_PORT_GAME_AN0 => (byte*)0x000180B4;
        public static unsafe byte* GAME_PORT_GAME_AN1 => (byte*)0x000180B5;
        public static unsafe byte* GAME_PORT_GAME_AN2 => (byte*)0x000180B6;
        public static unsafe byte* GAME_PORT_GAME_AN3 => (byte*)0x000180B7;
        public static unsafe byte* GAME_PORT_GAME_TRIG => (byte*)0x000180C0;

        // Disk II Controller
        public const int DISKII_BASE = 0xC0E0;
        public static unsafe byte* DISKII_PHASE0 => (byte*)0x000181C0;
        public static unsafe byte* DISKII_PHASE1 => (byte*)0x000181C1;
        public static unsafe byte* DISKII_PHASE2 => (byte*)0x000181C2;
        public static unsafe byte* DISKII_PHASE3 => (byte*)0x000181C3;
        public static unsafe byte* DISKII_Q6L => (byte*)0x000181CC;
        public static unsafe byte* DISKII_Q7L => (byte*)0x000181CD;
        public static unsafe byte* DISKII_Q6R => (byte*)0x000181CE;
        public static unsafe byte* DISKII_Q7R => (byte*)0x000181CF;

        // Video Display Generator
        public const int VIDEO_BASE = 0xC050;
        public static unsafe byte* VIDEO_TXTCLR => (byte*)0x000180A0;
        public static unsafe byte* VIDEO_MIXCLR => (byte*)0x000180A1;
        public static unsafe byte* VIDEO_TXTPAGE2 => (byte*)0x000180A4;
        public static unsafe byte* VIDEO_TXTPAGE1 => (byte*)0x000180A5;
        public static unsafe byte* VIDEO_LORES => (byte*)0x000180A6;
        public static unsafe byte* VIDEO_HIRES => (byte*)0x000180A7;
        public static unsafe byte* VIDEO_DHIRESON => (byte*)0x000180AE;
        public static unsafe byte* VIDEO_AN0 => (byte*)0x000180A8;
        public static unsafe byte* VIDEO_AN1 => (byte*)0x000180A9;
        public static unsafe byte* VIDEO_AN2 => (byte*)0x000180AA;
        public static unsafe byte* VIDEO_AN3 => (byte*)0x000180AB;
        public static unsafe byte* VIDEO__80STORE => (byte*)0x00018050;

        // RAM Read/Write Control
        public const int RAMRD_BASE = 0xC080;
        public static unsafe byte* RAMRD_INTCXROM => (byte*)0x0001907F;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Power-on Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt (from VIA)
        public const int IRQ_IRQ = 2;  // IRQ from VIA/timer/slot
        public const int IRQ_BRK = 3;  // BRK Instruction

        // 引脚定义
        public const int PIN_VCC = 1;  // +5V Power
        public const int PIN_GND = 2;  // Ground
        public const int PIN_RESET = 3;  // System Reset
        public const int PIN_CLK = 4;  // System Clock (1.023MHz NTSC)
        public const int PIN_RDY = 5;  // CPU Ready
        public const int PIN_NMI = 6;  // Non-Maskable Interrupt
        public const int PIN_IRQ = 7;  // Interrupt Request
        public const int PIN_SO = 8;  // Set Overflow
        public const int PIN_RWB = 9;  // Read/Write Bar
        public const int PIN_SYNC = 10;  // Instruction Sync
        public const int PIN_A0_A15 = 11;  // Address Bus (16-bit)
        public const int PIN_D0_D7 = 12;  // Data Bus (8-bit)
        public const int PIN_PHASE0 = 13;  // Phase 0 (4MHz system)
        public const int PIN_PHASE1 = 14;  // Phase 1
        public const int PIN_PHASE2 = 15;  // Phase 2
        public const int PIN_PHASE3 = 16;  // Phase 3

        public static void apple_iie_init()
        {
            // 硬件初始化代码
        }
    }
}
