package vml.device.commodoreinternational.commodore_64;

/**
 * Commodore-64 寄存器定义
 * 生成自: Commodore International/Commodore 64/Commodore-64
 * 版本: 1.0
 */
public final class Commodore_64 {
    private Commodore_64() {} // 工具类
    // CPU架构: MOS 6510, 8位, 985248 Hz

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

    // I/O Port (6510 specific)
    public static final int PORT_ADDR = (int)1;

    // 外设定义
    // Video Interface Chip II
    public static final int VIC_II_BASE = (int);
    public static final int VIC_II_VIC_CTRL1 = (int)0x0000D011;
    public static final int VIC_II_VIC_CTRL2 = (int)0x0000D016;
    public static final int VIC_II_VIC_RASTER = (int)0x0000D012;
    public static final int VIC_II_VIC_MEMPTR = (int)0x0000D018;
    public static final int VIC_II_VIC_IRQ = (int)0x0000D019;
    public static final int VIC_II_VIC_IRQMASK = (int)0x0000D01A;
    public static final int VIC_II_VIC_BORDER = (int)0x0000D020;
    public static final int VIC_II_VIC_BG0 = (int)0x0000D021;
    public static final int VIC_II_VIC_BG1 = (int)0x0000D022;
    public static final int VIC_II_VIC_BG2 = (int)0x0000D023;
    public static final int VIC_II_VIC_BG3 = (int)0x0000D024;
    public static final int VIC_II_VIC_SPRITE0_X = (int)0x0000D000;
    public static final int VIC_II_VIC_SPRITE0_Y = (int)0x0000D001;
    public static final int VIC_II_VIC_SPRITE1_X = (int)0x0000D002;
    public static final int VIC_II_VIC_SPRITE1_Y = (int)0x0000D003;

    // Sound Interface Device (6581)
    public static final int SID_BASE = (int);
    public static final int SID_SID_VOICE1_FREQ_LO = (int)0x0000D400;
    public static final int SID_SID_VOICE1_FREQ_HI = (int)0x0000D401;
    public static final int SID_SID_VOICE1_PW_LO = (int)0x0000D402;
    public static final int SID_SID_VOICE1_PW_HI = (int)0x0000D403;
    public static final int SID_SID_VOICE1_CTRL = (int)0x0000D404;
    public static final int SID_SID_VOICE1_AD = (int)0x0000D405;
    public static final int SID_SID_VOICE1_SR = (int)0x0000D406;
    public static final int SID_SID_VOICE2_FREQ_LO = (int)0x0000D407;
    public static final int SID_SID_VOICE2_FREQ_HI = (int)0x0000D408;
    public static final int SID_SID_VOICE2_PW_LO = (int)0x0000D409;
    public static final int SID_SID_VOICE2_PW_HI = (int)0x0000D40A;
    public static final int SID_SID_VOICE2_CTRL = (int)0x0000D40B;
    public static final int SID_SID_VOICE2_AD = (int)0x0000D40C;
    public static final int SID_SID_VOICE2_SR = (int)0x0000D40D;
    public static final int SID_SID_VOICE3_FREQ_LO = (int)0x0000D40E;
    public static final int SID_SID_VOICE3_FREQ_HI = (int)0x0000D40F;
    public static final int SID_SID_VOICE3_PW_LO = (int)0x0000D410;
    public static final int SID_SID_VOICE3_PW_HI = (int)0x0000D411;
    public static final int SID_SID_VOICE3_CTRL = (int)0x0000D412;
    public static final int SID_SID_VOICE3_AD = (int)0x0000D413;
    public static final int SID_SID_VOICE3_SR = (int)0x0000D414;
    public static final int SID_SID_FILTER_CUTOFF_LO = (int)0x0000D415;
    public static final int SID_SID_FILTER_CUTOFF_HI = (int)0x0000D416;
    public static final int SID_SID_FILTER_CTRL = (int)0x0000D417;
    public static final int SID_SID_VOLUME = (int)0x0000D418;
    public static final int SID_SID_POTX = (int)0x0000D419;
    public static final int SID_SID_POTY = (int)0x0000D41A;
    public static final int SID_SID_OSC3 = (int)0x0000D41B;
    public static final int SID_SID_ENV3 = (int)0x0000D41C;

    // Complex Interface Adapter 1 (6526)
    public static final int CIA1_BASE = (int);
    public static final int CIA1_CIA1_PRA = (int)0x0000DC00;
    public static final int CIA1_CIA1_PRB = (int)0x0000DC01;
    public static final int CIA1_CIA1_DDRA = (int)0x0000DC02;
    public static final int CIA1_CIA1_DDRB = (int)0x0000DC03;
    public static final int CIA1_CIA1_TALO = (int)0x0000DC04;
    public static final int CIA1_CIA1_TAHI = (int)0x0000DC05;
    public static final int CIA1_CIA1_TBLO = (int)0x0000DC06;
    public static final int CIA1_CIA1_TBHI = (int)0x0000DC07;
    public static final int CIA1_CIA1_TODTEN = (int)0x0000DC08;
    public static final int CIA1_CIA1_TODSEC = (int)0x0000DC09;
    public static final int CIA1_CIA1_TODMIN = (int)0x0000DC0A;
    public static final int CIA1_CIA1_TODHR = (int)0x0000DC0B;
    public static final int CIA1_CIA1_SDR = (int)0x0000DC0C;
    public static final int CIA1_CIA1_ICR = (int)0x0000DC0D;
    public static final int CIA1_CIA1_CRA = (int)0x0000DC0E;
    public static final int CIA1_CIA1_CRB = (int)0x0000DC0F;

    // Complex Interface Adapter 2 (6526)
    public static final int CIA2_BASE = (int);
    public static final int CIA2_CIA2_PRA = (int)0x0000DD00;
    public static final int CIA2_CIA2_PRB = (int)0x0000DD01;
    public static final int CIA2_CIA2_DDRA = (int)0x0000DD02;
    public static final int CIA2_CIA2_DDRB = (int)0x0000DD03;
    public static final int CIA2_CIA2_TALO = (int)0x0000DD04;
    public static final int CIA2_CIA2_TAHI = (int)0x0000DD05;
    public static final int CIA2_CIA2_TBLO = (int)0x0000DD06;
    public static final int CIA2_CIA2_TBHI = (int)0x0000DD07;
    public static final int CIA2_CIA2_TODTEN = (int)0x0000DD08;
    public static final int CIA2_CIA2_TODSEC = (int)0x0000DD09;
    public static final int CIA2_CIA2_TODMIN = (int)0x0000DD0A;
    public static final int CIA2_CIA2_TODHR = (int)0x0000DD0B;
    public static final int CIA2_CIA2_SDR = (int)0x0000DD0C;
    public static final int CIA2_CIA2_ICR = (int)0x0000DD0D;
    public static final int CIA2_CIA2_CRA = (int)0x0000DD0E;
    public static final int CIA2_CIA2_CRB = (int)0x0000DD0F;

    // 中断向量定义
    public static final int IRQ_IRQ = 65532;  // Maskable Interrupt
    public static final int IRQ_NMI = 65534;  // Non-Maskable Interrupt
    public static final int IRQ_RESET = 65526;  // Reset Vector

    public static native void commodore_64_init();
}
