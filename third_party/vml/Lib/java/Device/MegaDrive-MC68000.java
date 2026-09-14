package vml.device.motorola.motorola_68000;

/**
 * Motorola-68000 寄存器定义
 * 生成自: Motorola/68000/Motorola-68000
 * 版本: 1.0
 */
public final class Motorola_68000 {
    private Motorola_68000() {} // 工具类
    // CPU架构: MC68000, 32位, 7670452 Hz

    // 寄存器定义
    // Data Register 0
    public static final int D0_ADDR = (int)0x00;

    // Data Register 1
    public static final int D1_ADDR = (int)0x04;

    // Data Register 2
    public static final int D2_ADDR = (int)0x08;

    // Data Register 3
    public static final int D3_ADDR = (int)0x0C;

    // Data Register 4
    public static final int D4_ADDR = (int)0x10;

    // Data Register 5
    public static final int D5_ADDR = (int)0x14;

    // Data Register 6
    public static final int D6_ADDR = (int)0x18;

    // Data Register 7
    public static final int D7_ADDR = (int)0x1C;

    // Address Register 0
    public static final int A0_ADDR = (int)0x20;

    // Address Register 1
    public static final int A1_ADDR = (int)0x24;

    // Address Register 2
    public static final int A2_ADDR = (int)0x28;

    // Address Register 3
    public static final int A3_ADDR = (int)0x2C;

    // Address Register 4
    public static final int A4_ADDR = (int)0x30;

    // Address Register 5
    public static final int A5_ADDR = (int)0x34;

    // Address Register 6
    public static final int A6_ADDR = (int)0x38;

    // Stack Pointer (USP)
    public static final int A7_ADDR = (int)0x3C;

    // Program Counter
    public static final int PC_ADDR = (int)0x40;

    // Status Register
    public static final int SR_ADDR = (int)0x44;
    public static final int SR_C = 0;  // Carry
    public static final int SR_V = 1;  // Overflow
    public static final int SR_Z = 2;  // Zero
    public static final int SR_N = 3;  // Negative
    public static final int SR_X = 4;  // Extend
    public static final int SR_I0 = 8;  // Interrupt Mask 0
    public static final int SR_I1 = 9;  // Interrupt Mask 1
    public static final int SR_I2 = 10;  // Interrupt Mask 2
    public static final int SR_M = 11;  // Master/Interrupt
    public static final int SR_S = 13;  // Supervisor/User
    public static final int SR_T0 = 14;  // Trace Mode 0
    public static final int SR_T1 = 15;  // Trace Mode 1

    // 内存段定义
    // System RAM (4MB)
    public static final int RAM_START = (int)0x000000;
    public static final int RAM_END = (int)0x3FFFFF;
    public static final int RAM_SIZE = 4194304;

    // Cartridge ROM
    public static final int ROM_START = (int)0x000000;
    public static final int ROM_END = (int)0x3FFFFF;
    public static final int ROM_SIZE = 4194304;

    // I/O Register Area
    public static final int IO_START = (int)0xA00000;
    public static final int IO_END = (int)0xA1FFFF;
    public static final int IO_SIZE = 131072;

    // VDP Registers
    public static final int VDP_START = (int)0xC00000;
    public static final int VDP_END = (int)0xC0001F;
    public static final int VDP_SIZE = 32;

    // Video RAM (256KB)
    public static final int VRAM_START = (int)0xE00000;
    public static final int VRAM_END = (int)0xE3FFFF;
    public static final int VRAM_SIZE = 262144;

    // 外设定义
    // Video Display Processor (TMS9918A variant)
    public static final int VDP_BASE = (int)0xC00000;
    public static final int VDP_DATA = (int)0x00C00000;
    public static final int VDP_CTRL = (int)0x00C00004;
    public static final int VDP_HVCOUNT = (int)0x00C00008;
    public static final int VDP_HVB_STATUS = (int)0x00C0000A;

    // Programmable Sound Generator (AY-3-8910)
    public static final int PSG_BASE = (int)0xC00011;
    public static final int PSG_CH_A_FREQ = (int)0x00C00011;
    public static final int PSG_CH_A_VOL = (int)0x00C00019;
    public static final int PSG_CH_B_FREQ = (int)0x00C00013;
    public static final int PSG_CH_B_VOL = (int)0x00C0001A;
    public static final int PSG_CH_C_FREQ = (int)0x00C00015;
    public static final int PSG_CH_C_VOL = (int)0x00C0001B;
    public static final int PSG_NOISE_FREQ = (int)0x00C00017;
    public static final int PSG_MIXER = (int)0x00C00018;
    public static final int PSG_ENV_FREQ = (int)0x00C0001E;
    public static final int PSG_ENV_SHAPE = (int)0x00C0001C;

    // Z80 Secondary CPU (Sound)
    public static final int Z80_BASE = (int)0xA00000;
    public static final int Z80_Z80_RESET = (int)0x00A00000;
    public static final int Z80_Z80_BUSREQ = (int)0x00A00004;
    public static final int Z80_Z80_STATUS = (int)0x00A00008;

    // Bank Register
    public static final int BANK_REG_BASE = (int)0xA12000;
    public static final int BANK_REG_ROM_BANK = (int)0x00A12000;
    public static final int BANK_REG_RAM_BANK = (int)0x00A12004;

    // Hardware Version
    public static final int HW_VERSION_BASE = (int)0xA10001;
    public static final int HW_VERSION_VERSION = (int)0x00A10001;

    // Controller Port 1
    public static final int CONTROLLER1_BASE = (int)0xA10003;
    public static final int CONTROLLER1_DATA = (int)0x00A10003;
    public static final int CONTROLLER1_CTRL = (int)0x00A10007;

    // Controller Port 2
    public static final int CONTROLLER2_BASE = (int)0xA10005;
    public static final int CONTROLLER2_DATA = (int)0x00A10005;
    public static final int CONTROLLER2_CTRL = (int)0x00A10009;

    // External Port
    public static final int EXT_PORT_BASE = (int)0xA10007;
    public static final int EXT_PORT_DATA = (int)0x00A10007;

    // DMA Controller
    public static final int DMA_BASE = (int)0xA10008;
    public static final int DMA_SOURCE = (int)0x00A10008;
    public static final int DMA_DEST = (int)0x00A1000C;
    public static final int DMA_COUNT = (int)0x00A10010;
    public static final int DMA_CTRL = (int)0x00A10012;

    // Hardware Timer
    public static final int TIMER_BASE = (int)0xA1000E;
    public static final int TIMER_H_COUNTER = (int)0x00A1000E;
    public static final int TIMER_V_COUNTER = (int)0x00A10012;

    // 中断向量定义
    public static final int IRQ_RESET_SP = 1;  // Reset Initial Stack Pointer
    public static final int IRQ_RESET_PC = 2;  // Reset Initial PC
    public static final int IRQ_BUS_ERROR = 3;  // Bus Error
    public static final int IRQ_ADDRESS_ERROR = 4;  // Address Error
    public static final int IRQ_ILLEGAL_INSTR = 5;  // Illegal Instruction
    public static final int IRQ_ZERO_DIVIDE = 6;  // Zero Divide
    public static final int IRQ_CHK_EXCEPTION = 7;  // CHK Exception
    public static final int IRQ_TRAPV = 8;  // TRAPV Exception
    public static final int IRQ_PRIVILEGE = 9;  // Privilege Violation
    public static final int IRQ_TRACE = 10;  // Trace
    public static final int IRQ_LINE_A = 11;  // Line 1010 Emulator
    public static final int IRQ_LINE_F = 12;  // Line 1111 Emulator
    public static final int IRQ_IRQ1 = 24;  // External Interrupt 1 (H-Blank)
    public static final int IRQ_IRQ2 = 25;  // External Interrupt 2 (V-Blank)
    public static final int IRQ_IRQ3 = 26;  // External Interrupt 3
    public static final int IRQ_IRQ4 = 27;  // External Interrupt 4 (D-Req)
    public static final int IRQ_IRQ5 = 28;  // External Interrupt 5
    public static final int IRQ_IRQ6 = 29;  // External Interrupt 6
    public static final int IRQ_IRQ7 = 30;  // External Interrupt 7
    public static final int IRQ_TRAP0 = 32;  // TRAP #0
    public static final int IRQ_TRAP1 = 33;  // TRAP #1
    public static final int IRQ_TRAP15 = 47;  // TRAP #15

    public static native void motorola_68000_init();
}
