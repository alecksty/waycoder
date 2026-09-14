package vml.device.applecomputer.macintosh_128k;

/**
 * Macintosh-128K 寄存器定义
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 */
public final class Macintosh_128K {
    private Macintosh_128K() {} // 工具类
    // CPU架构: Motorola 68000, 32位, 7998000 Hz

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
    // Versatile Interface Adapter (6522)
    public static final int VIA_BASE = (int);
    public static final int VIA_VIA_ORB = (int)0x00E80000;
    public static final int VIA_VIA_ORA = (int)0x00E80001;
    public static final int VIA_VIA_DDRB = (int)0x00E80002;
    public static final int VIA_VIA_DDRA = (int)0x00E80003;
    public static final int VIA_VIA_T1CL = (int)0x00E80004;
    public static final int VIA_VIA_T1CH = (int)0x00E80005;
    public static final int VIA_VIA_T1LL = (int)0x00E80006;
    public static final int VIA_VIA_T1LH = (int)0x00E80007;
    public static final int VIA_VIA_T2CL = (int)0x00E80008;
    public static final int VIA_VIA_T2CH = (int)0x00E80009;
    public static final int VIA_VIA_SR = (int)0x00E8000A;
    public static final int VIA_VIA_ACR = (int)0x00E8000B;
    public static final int VIA_VIA_PCR = (int)0x00E8000C;
    public static final int VIA_VIA_IFR = (int)0x00E8000D;
    public static final int VIA_VIA_IER = (int)0x00E8000E;
    public static final int VIA_VIA_ORA2 = (int)0x00E8000F;

    // Integrated Woz Machine (floppy controller)
    public static final int IWM_BASE = (int);
    public static final int IWM_IWM_Q6 = (int)0x00D00000;
    public static final int IWM_IWM_Q7 = (int)0x00D00002;
    public static final int IWM_IWM_PH0 = (int)0x00D00004;
    public static final int IWM_IWM_PH1 = (int)0x00D00006;
    public static final int IWM_IWM_PH2 = (int)0x00D00008;
    public static final int IWM_IWM_PH3 = (int)0x00D0000A;

    // Zilog 8530 Serial Communications Controller
    public static final int SCC_BASE = (int);
    public static final int SCC_SCC_CA = (int)0x00500000;
    public static final int SCC_SCC_DA = (int)0x00500002;
    public static final int SCC_SCC_CB = (int)0x00500004;
    public static final int SCC_SCC_DB = (int)0x00500006;

    // Built-in speaker
    public static final int SOUND_BASE = (int);
    public static final int SOUND_SOUND_VOL = (int)0x00E80100;
    public static final int SOUND_SOUND_FREQ = (int)0x00E80102;

    // 中断向量定义
    public static final int IRQ_RESET_SP = 0;  // Reset (Initial SP)
    public static final int IRQ_RESET_PC = 4;  // Reset (Initial PC)
    public static final int IRQ_AUTOVECTOR1 = 24;  // Auto vector 1
    public static final int IRQ_AUTOVECTOR2 = 25;  // Auto vector 2
    public static final int IRQ_AUTOVECTOR3 = 26;  // Auto vector 3
    public static final int IRQ_AUTOVECTOR4 = 27;  // Auto vector 4
    public static final int IRQ_AUTOVECTOR5 = 28;  // Auto vector 5
    public static final int IRQ_AUTOVECTOR6 = 29;  // Auto vector 6
    public static final int IRQ_AUTOVECTOR7 = 30;  // Auto vector 7
    public static final int IRQ_SPURIOUS = 31;  // Spurious interrupt

    public static native void macintosh_128k_init();
}
