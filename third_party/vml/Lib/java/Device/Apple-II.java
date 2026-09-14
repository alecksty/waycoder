package vml.device.applecomputer.apple_ii;

/**
 * Apple-II 寄存器定义
 * 生成自: Apple Computer/Apple II/Apple-II
 * 版本: 1.0
 */
public final class Apple_II {
    private Apple_II() {} // 工具类
    // CPU架构: MOS 6502, 8位, 1023000 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0;

    // Index Register X
    public static final int X_ADDR = (int)0;

    // Index Register Y
    public static final int Y_ADDR = (int)0;

    // Stack Pointer
    public static final int SP_ADDR = (int)0;

    // Program Counter
    public static final int PC_ADDR = (int)0;

    // Status Register
    public static final int P_ADDR = (int)0;

    // 外设定义
    // Apple II keyboard
    public static final int KEYBOARD_BASE = (int);
    public static final int KEYBOARD_KBD = (int)0x0000C000;
    public static final int KEYBOARD_KBDSTRB = (int)0x0000C010;

    // Built-in speaker
    public static final int SPEAKER_BASE = (int);
    public static final int SPEAKER_SPKR = (int)0x0000C030;

    // Cassette tape interface
    public static final int CASSETTE_BASE = (int);
    public static final int CASSETTE_TAPEIN = (int)0x0000C060;
    public static final int CASSETTE_TAPEOUT = (int)0x0000C020;

    // Game controller port
    public static final int GAMEPORT_BASE = (int);
    public static final int GAMEPORT_PADDLE0 = (int)0x0000C064;
    public static final int GAMEPORT_PADDLE1 = (int)0x0000C065;
    public static final int GAMEPORT_PADDLE2 = (int)0x0000C066;
    public static final int GAMEPORT_PADDLE3 = (int)0x0000C067;
    public static final int GAMEPORT_BUTTON0 = (int)0x0000C061;
    public static final int GAMEPORT_BUTTON1 = (int)0x0000C062;

    // Disk II controller
    public static final int DISKCONTROLLER_BASE = (int);
    public static final int DISKCONTROLLER_DISKUNIT = (int)0x0000C0E0;
    public static final int DISKCONTROLLER_DISKCMD = (int)0x0000C0E8;
    public static final int DISKCONTROLLER_DISKSTAT = (int)0x0000C0E9;
    public static final int DISKCONTROLLER_DISKDATA = (int)0x0000C0EA;

    // 中断向量定义
    public static final int IRQ_NMI = 65526;  // Non-maskable interrupt
    public static final int IRQ_RESET = 65528;  // Reset vector
    public static final int IRQ_IRQ = 65530;  // Interrupt request
    public static final int IRQ_BRK = 65532;  // Break instruction

    public static native void apple_ii_init();
}
