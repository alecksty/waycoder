package vml.device.sega.sega_master_system;

/**
 * Sega-Master-System 寄存器定义
 * 生成自: Sega/Master System/Sega-Master-System
 * 版本: 1.0
 */
public final class Sega_Master_System {
    private Sega_Master_System() {} // 工具类
    // CPU架构: Zilog Z80, 8位, 3579545 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0;

    // Flags
    public static final int F_ADDR = (int)0;

    // B
    public static final int B_ADDR = (int)0;

    // C
    public static final int C_ADDR = (int)0;

    // D
    public static final int D_ADDR = (int)0;

    // E
    public static final int E_ADDR = (int)0;

    // H
    public static final int H_ADDR = (int)0;

    // L
    public static final int L_ADDR = (int)0;

    // Index Register X
    public static final int IX_ADDR = (int)0;

    // Index Register Y
    public static final int IY_ADDR = (int)0;

    // Stack Pointer
    public static final int SP_ADDR = (int)0;

    // Program Counter
    public static final int PC_ADDR = (int)0;

    // Interrupt Vector
    public static final int I_ADDR = (int)0;

    // Memory Refresh
    public static final int R_ADDR = (int)0;

    // 外设定义
    // Video Display Processor (TMS9918A)
    public static final int VDP_BASE = (int);
    public static final int VDP_VDP_DATA = (int)0x000000BE;
    public static final int VDP_VDP_ADDR = (int)0x000000BF;
    public static final int VDP_VDP_STATUS = (int)0x000000BF;

    // Programmable Sound Generator (SN76489)
    public static final int PSG_BASE = (int);
    public static final int PSG_PSG_DATA = (int)0x0000007F;

    // I/O ports
    public static final int IO_BASE = (int);
    public static final int IO_IO_PORT_A = (int)0x000000DC;
    public static final int IO_IO_PORT_B = (int)0x000000DD;
    public static final int IO_IO_PORT_MISC = (int)0x000000DE;
    public static final int IO_IO_PORT_VDP = (int)0x000000DF;

    // Memory mapper
    public static final int MEMORYMAPPER_BASE = (int);
    public static final int MEMORYMAPPER_MAPPER_0 = (int)0x0000FFFC;
    public static final int MEMORYMAPPER_MAPPER_1 = (int)0x0000FFFD;
    public static final int MEMORYMAPPER_MAPPER_2 = (int)0x0000FFFE;
    public static final int MEMORYMAPPER_MAPPER_3 = (int)0x0000FFFF;

    // FM Sound Unit (optional)
    public static final int FMUNIT_BASE = (int);
    public static final int FMUNIT_FM_ADDR = (int)0x000000F0;
    public static final int FMUNIT_FM_DATA = (int)0x000000F1;
    public static final int FMUNIT_FM_DETECT = (int)0x000000F2;

    // 中断向量定义
    public static final int IRQ_RST_00 = 0;  // Restart 00h
    public static final int IRQ_IM1 = 56;  // Interrupt Mode 1
    public static final int IRQ_VBLANK = 56;  // Vertical blank interrupt
    public static final int IRQ_LINE = 100;  // Line interrupt

    public static native void sega_master_system_init();
}
