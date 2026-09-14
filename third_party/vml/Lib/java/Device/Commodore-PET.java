package vml.device.commodoreinternational.commodore_pet;

/**
 * Commodore-PET 寄存器定义
 * 生成自: Commodore International/PET/Commodore-PET
 * 版本: 1.0
 */
public final class Commodore_PET {
    private Commodore_PET() {} // 工具类
    // CPU架构: MOS 6502, 8位, 1000000 Hz

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
    // Peripheral Interface Adapter 1 (6520)
    public static final int PIA1_BASE = (int);
    public static final int PIA1_PIA1_DDRA = (int)0x0000E810;
    public static final int PIA1_PIA1_ORA = (int)0x0000E811;
    public static final int PIA1_PIA1_DDRB = (int)0x0000E812;
    public static final int PIA1_PIA1_ORB = (int)0x0000E813;
    public static final int PIA1_PIA1_CRA = (int)0x0000E814;
    public static final int PIA1_PIA1_CRB = (int)0x0000E815;

    // Peripheral Interface Adapter 2 (6520)
    public static final int PIA2_BASE = (int);
    public static final int PIA2_PIA2_DDRA = (int)0x0000E820;
    public static final int PIA2_PIA2_ORA = (int)0x0000E821;
    public static final int PIA2_PIA2_DDRB = (int)0x0000E822;
    public static final int PIA2_PIA2_ORB = (int)0x0000E823;
    public static final int PIA2_PIA2_CRA = (int)0x0000E824;
    public static final int PIA2_PIA2_CRB = (int)0x0000E825;

    // Versatile Interface Adapter (6522)
    public static final int VIA_BASE = (int);
    public static final int VIA_VIA_ORB = (int)0x0000E840;
    public static final int VIA_VIA_ORA = (int)0x0000E841;
    public static final int VIA_VIA_DDRB = (int)0x0000E842;
    public static final int VIA_VIA_DDRA = (int)0x0000E843;
    public static final int VIA_VIA_T1CL = (int)0x0000E844;
    public static final int VIA_VIA_T1CH = (int)0x0000E845;
    public static final int VIA_VIA_T1LL = (int)0x0000E846;
    public static final int VIA_VIA_T1LH = (int)0x0000E847;
    public static final int VIA_VIA_T2CL = (int)0x0000E848;
    public static final int VIA_VIA_T2CH = (int)0x0000E849;
    public static final int VIA_VIA_SR = (int)0x0000E84A;
    public static final int VIA_VIA_ACR = (int)0x0000E84B;
    public static final int VIA_VIA_PCR = (int)0x0000E84C;
    public static final int VIA_VIA_IFR = (int)0x0000E84D;
    public static final int VIA_VIA_IER = (int)0x0000E84E;

    // CRT Controller (6545)
    public static final int CRTC_BASE = (int);
    public static final int CRTC_CRTC_ADDR = (int)0x0000E880;
    public static final int CRTC_CRTC_DATA = (int)0x0000E881;

    // Cassette tape interface
    public static final int CASSETTE_BASE = (int);
    public static final int CASSETTE_CASS_MOTOR = (int)0x0000E840;
    public static final int CASSETTE_CASS_WRITE = (int)0x0000E842;
    public static final int CASSETTE_CASS_READ = (int)0x0000E812;

    // IEEE-488 bus interface
    public static final int IEEE488_BASE = (int);
    public static final int IEEE488_IEEE_DATA = (int)0x0000E801;
    public static final int IEEE488_IEEE_STATUS = (int)0x0000E802;
    public static final int IEEE488_IEEE_CONTROL = (int)0x0000E803;

    // 中断向量定义
    public static final int IRQ_NMI = 65526;  // Non-maskable interrupt
    public static final int IRQ_RESET = 65528;  // Reset vector
    public static final int IRQ_IRQ = 65530;  // Interrupt request
    public static final int IRQ_BRK = 65532;  // Break instruction

    public static native void commodore_pet_init();
}
