package vml.device.sinclairresearch.zx_spectrum_48k;

/**
 * ZX-Spectrum-48K 寄存器定义
 * 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
 * 版本: 1.0
 */
public final class ZX_Spectrum_48K {
    private ZX_Spectrum_48K() {} // 工具类
    // CPU架构: Z80A, 8位, 3500000 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // Flags Register
    public static final int F_ADDR = (int)0x01;
    public static final int F_C = 0;  // Carry
    public static final int F_N = 1;  // Add/Subtract
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

    // Interrupt Vector Register
    public static final int I_ADDR = (int)0x10;

    // Refresh Counter
    public static final int R_ADDR = (int)0x11;

    // Index X
    public static final int IX_ADDR = (int)0x12;

    // Index Y
    public static final int IY_ADDR = (int)0x14;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x16;

    // Program Counter
    public static final int PC_ADDR = (int)0x18;

    // 内存段定义
    // 48KB ZX Spectrum ROM (BASIC + monitor)
    public static final int ROM_START = (int)0x0000;
    public static final int ROM_END = (int)0x3FFF;
    public static final int ROM_SIZE = 16384;

    // Display file (256x192 bitmap)
    public static final int VIDEO_RAM_START = (int)0x4000;
    public static final int VIDEO_RAM_END = (int)0x57FF;
    public static final int VIDEO_RAM_SIZE = 6144;

    // Attribute file (32x24 color cells)
    public static final int ATTR_RAM_START = (int)0x5800;
    public static final int ATTR_RAM_END = (int)0x5AFF;
    public static final int ATTR_RAM_SIZE = 768;

    // User RAM (40KB)
    public static final int USER_RAM_START = (int)0x5B00;
    public static final int USER_RAM_END = (int)0xFFFF;
    public static final int USER_RAM_SIZE = 40960;

    // 外设定义
    // Uncommitted Logic Array - Sinclair custom IC
    public static final int ULA_BASE = (int)0xFE;
    public static final int ULA_BORDER = (int)0x000001FC;
    public static final int ULA_KBD_ROW0 = (int)0x000001FC;
    public static final int ULA_KBD_ROW1 = (int)0x000001FC;
    public static final int ULA_KBD_ROW2 = (int)0x000001FC;
    public static final int ULA_KBD_ROW3 = (int)0x000001FC;
    public static final int ULA_KBD_ROW4 = (int)0x000001FC;
    public static final int ULA_KBD_ROW5 = (int)0x000001FC;
    public static final int ULA_KBD_ROW6 = (int)0x000001FC;
    public static final int ULA_KBD_ROW7 = (int)0x000001FC;
    public static final int ULA_KBD_ROW8 = (int)0x000001FC;

    // Keyboard Matrix (40 keys, 8 rows x 5 cols)
    public static final int KEYBOARD_BASE = (int)0xFE;
    public static final int KEYBOARD_KBD_IN = (int)0x000001FC;

    // Internal Beeper
    public static final int BEEPER_BASE = (int)0xFE;
    public static final int BEEPER_BEEP = (int)0x000001FC;

    // Tape Interface
    public static final int TAPE_BASE = (int)0xFE;
    public static final int TAPE_EAR_IN = (int)0x000001FC;
    public static final int TAPE_MIC_OUT = (int)0x000001FC;

    // Kempston Joystick Interface
    public static final int JOYSTICK_BASE = (int)0xF7FE;
    public static final int JOYSTICK_KEMPSTON = (int)0x0001EFFC;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-on / Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt (BREAK key)
    public static final int IRQ_INT = 2;  // Maskable Interrupt (ULA vertical blank, 50Hz)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // Z80 Clock (3.5MHz)
    public static final int PIN_M1 = 4;  // Machine Cycle 1
    public static final int PIN_MREQ = 5;  // Memory Request
    public static final int PIN_IORQ = 6;  // I/O Request
    public static final int PIN_RD = 7;  // Read
    public static final int PIN_WR = 8;  // Write
    public static final int PIN_HALT = 9;  // Halt State
    public static final int PIN_BUSAK = 10;  // Bus Acknowledge
    public static final int PIN_WAIT = 11;  // Wait State (ULA inserts)
    public static final int PIN_INT = 12;  // Interrupt Request
    public static final int PIN_NMI = 13;  // Non-Maskable Interrupt
    public static final int PIN_RESET = 14;  // Reset
    public static final int PIN_A0_A15 = 15;  // Address Bus (16-bit)
    public static final int PIN_D0_D7 = 16;  // Data Bus (8-bit)

    public static native void zx_spectrum_48k_init();
}
