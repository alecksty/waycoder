using System;

namespace VML.Device.Ricoh.Ricoh_2A03
{
    /// <summary>
    /// Ricoh-2A03 寄存器定义
    /// 生成自: Ricoh/MOS-6502/Ricoh-2A03
    /// 版本: 1.0
    /// </summary>
    public static class Ricoh_2A03
    {
        // CPU架构: MOS-6502, 8位, 10765930 Hz

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

        // Stack Pointer
        public const int SP_ADDR = 0x03;
        public static unsafe byte* SP => (byte*)0x03;

        // Program Counter (16-bit)
        public const int PC_ADDR = 0x04;
        public static unsafe ushort* PC => (ushort*)0x04;

        // Processor Status
        public const int P_ADDR = 0x06;
        public static unsafe byte* P => (byte*)0x06;
        public const int P_C = 0;  // Carry
        public const int P_Z = 1;  // Zero
        public const int P_I = 2;  // Interrupt Disable
        public const int P_D = 3;  // Decimal Mode
        public const int P_B = 4;  // Break
        public const int P_U = 5;  // Unused
        public const int P_V = 6;  // Overflow
        public const int P_N = 7;  // Negative

        // 内存段定义
        // CPU 2KB RAM (mirrored)
        public const int CPU_RAM_START = 0x0000;
        public const int CPU_RAM_END = 0x07FF;
        public const int CPU_RAM_SIZE = 2048;

        // PPU Registers (mirrored every 8 bytes)
        public const int PPU_REGISTERS_START = 0x2000;
        public const int PPU_REGISTERS_END = 0x3FFF;
        public const int PPU_REGISTERS_SIZE = 8192;

        // APU and I/O Registers
        public const int APU_REGISTERS_START = 0x4000;
        public const int APU_REGISTERS_END = 0x401F;
        public const int APU_REGISTERS_SIZE = 32;

        // Expansion ROM
        public const int EXPANSION_START = 0x4020;
        public const int EXPANSION_END = 0x5FFF;
        public const int EXPANSION_SIZE = 8160;

        // Save RAM
        public const int SRAM_START = 0x6000;
        public const int SRAM_END = 0x7FFF;
        public const int SRAM_SIZE = 8192;

        // PRG ROM Lower Bank (16KB)
        public const int PRG_ROM_LOW_START = 0x8000;
        public const int PRG_ROM_LOW_END = 0xBFFF;
        public const int PRG_ROM_LOW_SIZE = 16384;

        // PRG ROM Higher Bank (16KB)
        public const int PRG_ROM_HIGH_START = 0xC000;
        public const int PRG_ROM_HIGH_END = 0xFFFF;
        public const int PRG_ROM_HIGH_SIZE = 16384;

        // 外设定义
        // Picture Processing Unit
        public const int PPU_BASE = 0x2000;
        public static unsafe byte* PPU_PPUCTRL => (byte*)0x00004000;
        public static unsafe byte* PPU_PPUMASK => (byte*)0x00004001;
        public static unsafe byte* PPU_PPUSTATUS => (byte*)0x00004002;
        public static unsafe byte* PPU_OAMADDR => (byte*)0x00004003;
        public static unsafe byte* PPU_OAMDATA => (byte*)0x00004004;
        public static unsafe byte* PPU_PPUSCROLL => (byte*)0x00004005;
        public static unsafe byte* PPU_PPUADDR => (byte*)0x00004006;
        public static unsafe byte* PPU_PPUDATA => (byte*)0x00004007;

        // Audio Processing Unit
        public const int APU_BASE = 0x4000;
        public static unsafe byte* APU_PULSE1_VOL => (byte*)0x00008000;
        public static unsafe byte* APU_PULSE1_SWEEP => (byte*)0x00008001;
        public static unsafe byte* APU_PULSE1_LO => (byte*)0x00008002;
        public static unsafe byte* APU_PULSE1_HI => (byte*)0x00008003;
        public static unsafe byte* APU_PULSE2_VOL => (byte*)0x00008004;
        public static unsafe byte* APU_PULSE2_SWEEP => (byte*)0x00008005;
        public static unsafe byte* APU_PULSE2_LO => (byte*)0x00008006;
        public static unsafe byte* APU_PULSE2_HI => (byte*)0x00008007;
        public static unsafe byte* APU_TRIANGLE => (byte*)0x00008008;
        public static unsafe byte* APU_TRIANGLE_HI => (byte*)0x0000800B;
        public static unsafe byte* APU_NOISE_VOL => (byte*)0x0000800C;
        public static unsafe byte* APU_NOISE_HI => (byte*)0x0000800E;
        public static unsafe byte* APU_NOISE_LENGTH => (byte*)0x0000800F;
        public static unsafe byte* APU_DMC_RATE => (byte*)0x00008010;
        public static unsafe byte* APU_DMC_RAW => (byte*)0x00008011;
        public static unsafe byte* APU_DMC_START => (byte*)0x00008012;
        public static unsafe byte* APU_DMC_LENGTH => (byte*)0x00008013;
        public static unsafe byte* APU_OAMDMA => (byte*)0x00008014;
        public static unsafe byte* APU_SNDCHN => (byte*)0x00008015;
        public static unsafe byte* APU_JOY1 => (byte*)0x00008016;
        public static unsafe byte* APU_JOY2 => (byte*)0x00008017;

        // Controller Port 1
        public const int INPUT1_BASE = 0x4016;
        public static unsafe byte* INPUT1_JOYPAD1 => (byte*)0x0000802C;

        // Controller Port 2
        public const int INPUT2_BASE = 0x4017;
        public static unsafe byte* INPUT2_JOYPAD2 => (byte*)0x0000802E;

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Reset
        public const int IRQ_NMI = 1;  // Non-Maskable Interrupt (VBlank)
        public const int IRQ_IRQ = 2;  // IRQ / BRK

        public static void ricoh_2a03_init()
        {
            // 硬件初始化代码
        }
    }
}
