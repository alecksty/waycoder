package vml.device.variousasciiawanagamsxassociation.msx1;

/**
 * MSX1 寄存器定义
 * 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
 * 版本: 1.0
 */
public final class MSX1 {
    private MSX1() {} // 工具类
    // CPU架构: Z80A, 8位, 3579545 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // Flags
    public static final int F_ADDR = (int)0x01;
    public static final int F_C = 0;  // Carry
    public static final int F_N = 1;  // Subtract
    public static final int F_PV = 2;  // Parity/Overflow
    public static final int F_H = 4;  // Half Carry
    public static final int F_Z = 6;  // Zero
    public static final int F_S = 7;  // Sign

    // B Register
    public static final int B_ADDR = (int)0x02;

    // C Register
    public static final int C_ADDR = (int)0x03;

    // D Register
    public static final int D_ADDR = (int)0x04;

    // E Register
    public static final int E_ADDR = (int)0x05;

    // H Register
    public static final int H_ADDR = (int)0x06;

    // L Register
    public static final int L_ADDR = (int)0x07;

    // Alternate AF
    public static final int AF_ADDR = (int)0x08;

    // Alternate BC
    public static final int BC_ADDR = (int)0x0A;

    // Alternate DE
    public static final int DE_ADDR = (int)0x0C;

    // Alternate HL
    public static final int HL_ADDR = (int)0x0E;

    // Interrupt Vector
    public static final int I_ADDR = (int)0x10;

    // Refresh
    public static final int R_ADDR = (int)0x11;

    // Index X
    public static final int IX_ADDR = (int)0x12;

    // Index Y (usually = 0xF38F)
    public static final int IY_ADDR = (int)0x14;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x16;

    // Program Counter
    public static final int PC_ADDR = (int)0x18;

    // 内存段定义
    // Cartridge/SUB-ROM / Main-ROM
    public static final int SLOT0_ROM_START = (int)0x0000;
    public static final int SLOT0_ROM_END = (int)0x7FFF;
    public static final int SLOT0_ROM_SIZE = 32768;

    // MSX-BIOS ROM
    public static final int SYSROM_START = (int)0x0000;
    public static final int SYSROM_END = (int)0x3FFF;
    public static final int SYSROM_SIZE = 16384;

    // Extension ROM (cartridge)
    public static final int EXTROM_START = (int)0x4000;
    public static final int EXTROM_END = (int)0x7FFF;
    public static final int EXTROM_SIZE = 16384;

    // Main RAM (32KB working area)
    public static final int MAIN_RAM_START = (int)0x4000;
    public static final int MAIN_RAM_END = (int)0xC000;
    public static final int MAIN_RAM_SIZE = 32768;

    // Work RAM (16KB)
    public static final int WORK_RAM_START = (int)0xC000;
    public static final int WORK_RAM_END = (int)0xFFFF;
    public static final int WORK_RAM_SIZE = 16384;

    // System variables area
    public static final int SYSVAR_START = (int)0xF000;
    public static final int SYSVAR_END = (int)0xFCA0;
    public static final int SYSVAR_SIZE = 3232;

    // Slot-mapped memory
    public static final int SLOTS_START = (int)0x8000;
    public static final int SLOTS_END = (int)0xFFFF;
    public static final int SLOTS_SIZE = 32768;

    // 外设定义
    // TMS9918A Video Display Processor
    public static final int VDP_BASE = (int)0x98;
    public static final int VDP_VDP_REG0 = (int)0x00000131;
    public static final int VDP_VDP_REG1 = (int)0x00000131;
    public static final int VDP_VDP_REG2 = (int)0x00000131;
    public static final int VDP_VDP_REG3 = (int)0x00000131;
    public static final int VDP_VDP_REG4 = (int)0x00000131;
    public static final int VDP_VDP_REG5 = (int)0x00000131;
    public static final int VDP_VDP_REG6 = (int)0x00000131;
    public static final int VDP_VDP_REG7 = (int)0x00000131;
    public static final int VDP_VDP_STATUS = (int)0x00000131;
    public static final int VDP_VDP_DATA = (int)0x00000130;
    public static final int VDP_VDP_POT = (int)0x00000130;

    // AY-3-8910 Programmable Sound Generator
    public static final int PSG_BASE = (int)0xA0;
    public static final int PSG_PSG_REG = (int)0x00000141;
    public static final int PSG_PSG_DATA = (int)0x00000143;
    public static final int PSG_FREQ_A_LO = (int)0x00000140;
    public static final int PSG_FREQ_A_HI = (int)0x00000141;
    public static final int PSG_FREQ_B_LO = (int)0x00000142;
    public static final int PSG_FREQ_B_HI = (int)0x00000143;
    public static final int PSG_FREQ_C_LO = (int)0x00000144;
    public static final int PSG_FREQ_C_HI = (int)0x00000145;
    public static final int PSG_NOISE_FREQ = (int)0x00000146;
    public static final int PSG_ENABLE = (int)0x00000147;
    public static final int PSG_VOL_A = (int)0x00000148;
    public static final int PSG_VOL_B = (int)0x00000149;
    public static final int PSG_VOL_C = (int)0x0000014A;
    public static final int PSG_ENV_FREQ_LO = (int)0x0000014B;
    public static final int PSG_ENV_FREQ_HI = (int)0x0000014C;
    public static final int PSG_ENV_SHAPE = (int)0x0000014D;
    public static final int PSG_PORT_A = (int)0x0000014E;
    public static final int PSG_PORT_B = (int)0x0000014F;

    // PPI 8255 Programmable Peripheral Interface
    public static final int PPI_BASE = (int)0xA8;
    public static final int PPI_PPI_PA = (int)0x00000150;
    public static final int PPI_PPI_PB = (int)0x00000151;
    public static final int PPI_PPI_PC = (int)0x00000152;
    public static final int PPI_PPI_CTRL = (int)0x00000153;

    // MSX Slot Expansion System
    public static final int SLOTEXP_BASE = (int)0x0000;
    public static final int SLOTEXP_SLOT0 = (int)0x0000FCC0;
    public static final int SLOTEXP_SLOT1 = (int)0x0000FCC1;
    public static final int SLOTEXP_SLOT2 = (int)0x0000FCC2;
    public static final int SLOTEXP_SLOT3 = (int)0x0000FCC3;
    public static final int SLOTEXP_EXPTBL0 = (int)0x0000FCC4;
    public static final int SLOTEXP_EXPTBL1 = (int)0x0000FCC5;
    public static final int SLOTEXP_EXPTBL2 = (int)0x0000FCC6;
    public static final int SLOTEXP_EXPTBL3 = (int)0x0000FCC7;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-on / Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt
    public static final int IRQ_INT = 2;  // VDP Vertical Interrupt (frame)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // Z80 Clock (3.58MHz)
    public static final int PIN_A0_A15 = 4;  // Address Bus
    public static final int PIN_D0_D7 = 5;  // Data Bus
    public static final int PIN_MREQ = 6;  // Memory Request
    public static final int PIN_IORQ = 7;  // I/O Request
    public static final int PIN_RD = 8;  // Read
    public static final int PIN_WR = 9;  // Write
    public static final int PIN_INT = 10;  // Interrupt Request
    public static final int PIN_NMI = 11;  // Non-Maskable Interrupt
    public static final int PIN_RESET = 12;  // Reset
    public static final int PIN_SLTSL = 13;  // Slot select (for memory mapping)
    public static final int PIN_WAIT = 14;  // Wait (for slow I/O)

    public static native void msx1_init();
}
