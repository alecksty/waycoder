package vml.device.ricoh.ricoh_2a03;

/**
 * Ricoh-2A03 寄存器定义
 * 生成自: Ricoh/MOS-6502/Ricoh-2A03
 * 版本: 1.0
 */
public final class Ricoh_2A03 {
    private Ricoh_2A03() {} // 工具类
    // CPU架构: MOS-6502, 8位, 10765930 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // X Index
    public static final int X_ADDR = (int)0x01;

    // Y Index
    public static final int Y_ADDR = (int)0x02;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x03;

    // Program Counter (16-bit)
    public static final int PC_ADDR = (int)0x04;

    // Processor Status
    public static final int P_ADDR = (int)0x06;
    public static final int P_C = 0;  // Carry
    public static final int P_Z = 1;  // Zero
    public static final int P_I = 2;  // Interrupt Disable
    public static final int P_D = 3;  // Decimal Mode
    public static final int P_B = 4;  // Break
    public static final int P_U = 5;  // Unused
    public static final int P_V = 6;  // Overflow
    public static final int P_N = 7;  // Negative

    // 内存段定义
    // CPU 2KB RAM (mirrored)
    public static final int CPU_RAM_START = (int)0x0000;
    public static final int CPU_RAM_END = (int)0x07FF;
    public static final int CPU_RAM_SIZE = 2048;

    // PPU Registers (mirrored every 8 bytes)
    public static final int PPU_REGISTERS_START = (int)0x2000;
    public static final int PPU_REGISTERS_END = (int)0x3FFF;
    public static final int PPU_REGISTERS_SIZE = 8192;

    // APU and I/O Registers
    public static final int APU_REGISTERS_START = (int)0x4000;
    public static final int APU_REGISTERS_END = (int)0x401F;
    public static final int APU_REGISTERS_SIZE = 32;

    // Expansion ROM
    public static final int EXPANSION_START = (int)0x4020;
    public static final int EXPANSION_END = (int)0x5FFF;
    public static final int EXPANSION_SIZE = 8160;

    // Save RAM
    public static final int SRAM_START = (int)0x6000;
    public static final int SRAM_END = (int)0x7FFF;
    public static final int SRAM_SIZE = 8192;

    // PRG ROM Lower Bank (16KB)
    public static final int PRG_ROM_LOW_START = (int)0x8000;
    public static final int PRG_ROM_LOW_END = (int)0xBFFF;
    public static final int PRG_ROM_LOW_SIZE = 16384;

    // PRG ROM Higher Bank (16KB)
    public static final int PRG_ROM_HIGH_START = (int)0xC000;
    public static final int PRG_ROM_HIGH_END = (int)0xFFFF;
    public static final int PRG_ROM_HIGH_SIZE = 16384;

    // 外设定义
    // Picture Processing Unit
    public static final int PPU_BASE = (int)0x2000;
    public static final int PPU_PPUCTRL = (int)0x00004000;
    public static final int PPU_PPUMASK = (int)0x00004001;
    public static final int PPU_PPUSTATUS = (int)0x00004002;
    public static final int PPU_OAMADDR = (int)0x00004003;
    public static final int PPU_OAMDATA = (int)0x00004004;
    public static final int PPU_PPUSCROLL = (int)0x00004005;
    public static final int PPU_PPUADDR = (int)0x00004006;
    public static final int PPU_PPUDATA = (int)0x00004007;

    // Audio Processing Unit
    public static final int APU_BASE = (int)0x4000;
    public static final int APU_PULSE1_VOL = (int)0x00008000;
    public static final int APU_PULSE1_SWEEP = (int)0x00008001;
    public static final int APU_PULSE1_LO = (int)0x00008002;
    public static final int APU_PULSE1_HI = (int)0x00008003;
    public static final int APU_PULSE2_VOL = (int)0x00008004;
    public static final int APU_PULSE2_SWEEP = (int)0x00008005;
    public static final int APU_PULSE2_LO = (int)0x00008006;
    public static final int APU_PULSE2_HI = (int)0x00008007;
    public static final int APU_TRIANGLE = (int)0x00008008;
    public static final int APU_TRIANGLE_HI = (int)0x0000800B;
    public static final int APU_NOISE_VOL = (int)0x0000800C;
    public static final int APU_NOISE_HI = (int)0x0000800E;
    public static final int APU_NOISE_LENGTH = (int)0x0000800F;
    public static final int APU_DMC_RATE = (int)0x00008010;
    public static final int APU_DMC_RAW = (int)0x00008011;
    public static final int APU_DMC_START = (int)0x00008012;
    public static final int APU_DMC_LENGTH = (int)0x00008013;
    public static final int APU_OAMDMA = (int)0x00008014;
    public static final int APU_SNDCHN = (int)0x00008015;
    public static final int APU_JOY1 = (int)0x00008016;
    public static final int APU_JOY2 = (int)0x00008017;

    // Controller Port 1
    public static final int INPUT1_BASE = (int)0x4016;
    public static final int INPUT1_JOYPAD1 = (int)0x0000802C;

    // Controller Port 2
    public static final int INPUT2_BASE = (int)0x4017;
    public static final int INPUT2_JOYPAD2 = (int)0x0000802E;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt (VBlank)
    public static final int IRQ_IRQ = 2;  // IRQ / BRK

    public static native void ricoh_2a03_init();
}
