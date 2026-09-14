package vml.device.sinclairresearch.zx_spectrum;

/**
 * ZX-Spectrum 寄存器定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
 * 版本: 1.0
 */
public final class ZX_Spectrum {
    private ZX_Spectrum() {} // 工具类
    // CPU架构: Zilog Z80, 8位, 3500000 Hz

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

    // Alternate AF
    public static final int AF_ADDR = (int)0;

    // Alternate BC
    public static final int BC_ADDR = (int)0;

    // Alternate DE
    public static final int DE_ADDR = (int)0;

    // Alternate HL
    public static final int HL_ADDR = (int)0;

    // 外设定义
    // Uncommitted Logic Array (video and I/O)
    public static final int ULA_BASE = (int);
    public static final int ULA_ULA_PORT_FE = (int)0x000000FE;
    public static final int ULA_ULA_BORDER = (int)0x000000FE;
    public static final int ULA_ULA_BEEPER = (int)0x000000FE;
    public static final int ULA_ULA_MIC = (int)0x000000FE;

    // General Instruments AY-3-8912 sound chip
    public static final int AY_3_8912_BASE = (int);
    public static final int AY_3_8912_AY_REG_SEL = (int)0x0000FFFD;
    public static final int AY_3_8912_AY_DATA = (int)0x0000BFFD;
    public static final int AY_3_8912_AY_READ = (int)0x0000FFFD;

    // 40-key rubber keyboard
    public static final int KEYBOARD_BASE = (int);
    public static final int KEYBOARD_KEY_ROW0 = (int)0x0000FEFE;
    public static final int KEYBOARD_KEY_ROW1 = (int)0x0000FDFE;
    public static final int KEYBOARD_KEY_ROW2 = (int)0x0000FBFE;
    public static final int KEYBOARD_KEY_ROW3 = (int)0x0000F7FE;
    public static final int KEYBOARD_KEY_ROW4 = (int)0x0000EFFE;
    public static final int KEYBOARD_KEY_ROW5 = (int)0x0000DFFE;
    public static final int KEYBOARD_KEY_ROW6 = (int)0x0000BFFE;
    public static final int KEYBOARD_KEY_ROW7 = (int)0x00007FFE;

    // Kempston joystick interface
    public static final int KEMPSTON_BASE = (int);
    public static final int KEMPSTON_KEMPSTON_JOY = (int)0x0000001F;

    // ZX Interface 1 (RS-232 and Microdrive)
    public static final int INTERFACE1_BASE = (int);
    public static final int INTERFACE1_IF1_STATUS = (int)0x00001FFD;
    public static final int INTERFACE1_IF1_DATA = (int)0x00003FFD;

    // ZX Interface 2 (joystick and ROM cartridge)
    public static final int INTERFACE2_BASE = (int);
    public static final int INTERFACE2_IF2_JOY1 = (int)0x0000001F;
    public static final int INTERFACE2_IF2_JOY2 = (int)0x00000037;

    // 中断向量定义
    public static final int IRQ_IM1 = 56;  // Interrupt Mode 1
    public static final int IRQ_RST_00 = 0;  // Restart 00h
    public static final int IRQ_RST_08 = 8;  // Restart 08h
    public static final int IRQ_RST_10 = 16;  // Restart 10h
    public static final int IRQ_RST_18 = 24;  // Restart 18h
    public static final int IRQ_RST_20 = 32;  // Restart 20h
    public static final int IRQ_RST_28 = 40;  // Restart 28h
    public static final int IRQ_RST_30 = 48;  // Restart 30h
    public static final int IRQ_RST_38 = 56;  // Restart 38h

    public static native void zx_spectrum_init();
}
