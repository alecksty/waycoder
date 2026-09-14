package vml.device.sega.sega_genesis;

/**
 * Sega-Genesis 寄存器定义
 * 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
 * 版本: 1.0
 */
public final class Sega_Genesis {
    private Sega_Genesis() {} // 工具类
    // CPU架构: Motorola 68000, 32位, 7670000 Hz

    // 寄存器定义
    // Data Register 0
    public static final int D0_ADDR = (int)0;

    // Data Register 1
    public static final int D1_ADDR = (int)0;

    // Data Register 2
    public static final int D2_ADDR = (int)0;

    // Data Register 3
    public static final int D3_ADDR = (int)0;

    // Data Register 4
    public static final int D4_ADDR = (int)0;

    // Data Register 5
    public static final int D5_ADDR = (int)0;

    // Data Register 6
    public static final int D6_ADDR = (int)0;

    // Data Register 7
    public static final int D7_ADDR = (int)0;

    // Address Register 0
    public static final int A0_ADDR = (int)0;

    // Address Register 1
    public static final int A1_ADDR = (int)0;

    // Address Register 2
    public static final int A2_ADDR = (int)0;

    // Address Register 3
    public static final int A3_ADDR = (int)0;

    // Address Register 4
    public static final int A4_ADDR = (int)0;

    // Address Register 5
    public static final int A5_ADDR = (int)0;

    // Address Register 6
    public static final int A6_ADDR = (int)0;

    // Address Register 7 (SP)
    public static final int A7_ADDR = (int)0;

    // Program Counter
    public static final int PC_ADDR = (int)0;

    // Status Register
    public static final int SR_ADDR = (int)0;

    // 外设定义
    // Video Display Processor (315-5313)
    public static final int VDP_BASE = (int);
    public static final int VDP_VDP_DATA = (int)0x00C00000;
    public static final int VDP_VDP_CONTROL = (int)0x00C00004;
    public static final int VDP_VDP_HVCOUNTER = (int)0x00C00008;
    public static final int VDP_VDP_PSG = (int)0x00C00011;

    // FM synthesis sound chip
    public static final int YM2612_BASE = (int);
    public static final int YM2612_YM2612_ADDR0 = (int)0x00A04000;
    public static final int YM2612_YM2612_DATA0 = (int)0x00A04001;
    public static final int YM2612_YM2612_ADDR1 = (int)0x00A04002;
    public static final int YM2612_YM2612_DATA1 = (int)0x00A04003;

    // I/O ports
    public static final int IOPORTS_BASE = (int);
    public static final int IOPORTS_IO_DATA1 = (int)0x00A10002;
    public static final int IOPORTS_IO_DATA2 = (int)0x00A10004;
    public static final int IOPORTS_IO_DATA3 = (int)0x00A10006;
    public static final int IOPORTS_IO_CTRL1 = (int)0x00A10008;
    public static final int IOPORTS_IO_CTRL2 = (int)0x00A1000A;
    public static final int IOPORTS_IO_CTRL3 = (int)0x00A1000C;

    // TradeMark Security System
    public static final int TMSS_BASE = (int);
    public static final int TMSS_TMSS = (int)0x00A14000;

    // Z80 bus control
    public static final int Z80BUS_BASE = (int);
    public static final int Z80BUS_Z80_BUSREQ = (int)0x00A11100;
    public static final int Z80BUS_Z80_RESET = (int)0x00A11200;
    public static final int Z80BUS_Z80_YM2612 = (int)0x00A04000;

    // 中断向量定义
    public static final int IRQ_RESET_SP = 0;  // Reset (Initial SP)
    public static final int IRQ_RESET_PC = 4;  // Reset (Initial PC)
    public static final int IRQ_HBLANK = 24;  // Horizontal blank interrupt
    public static final int IRQ_VBLANK = 28;  // Vertical blank interrupt
    public static final int IRQ_EXTINT1 = 32;  // External interrupt 1
    public static final int IRQ_EXTINT2 = 36;  // External interrupt 2
    public static final int IRQ_EXTINT3 = 40;  // External interrupt 3
    public static final int IRQ_EXTINT4 = 44;  // External interrupt 4
    public static final int IRQ_EXTINT5 = 48;  // External interrupt 5
    public static final int IRQ_EXTINT6 = 52;  // External interrupt 6
    public static final int IRQ_EXTINT7 = 56;  // External interrupt 7

    public static native void sega_genesis_init();
}
