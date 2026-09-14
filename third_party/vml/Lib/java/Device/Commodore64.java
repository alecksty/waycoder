package vml.device.commodore.commodore_64;

/**
 * Commodore-64 寄存器定义
 * 生成自: Commodore/C64/Commodore-64
 * 版本: 1.0
 */
public final class Commodore_64 {
    private Commodore_64() {} // 工具类
    // CPU架构: MOS-6510, 8位, 1022727 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // X Index Register
    public static final int X_ADDR = (int)0x01;

    // Y Index Register
    public static final int Y_ADDR = (int)0x02;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x03;

    // Program Counter
    public static final int PC_ADDR = (int)0x04;

    // Processor Status
    public static final int P_ADDR = (int)0x06;
    public static final int P_C = 0;  // Carry Flag
    public static final int P_Z = 1;  // Zero Flag
    public static final int P_I = 2;  // Interrupt Disable
    public static final int P_D = 3;  // Decimal Mode
    public static final int P_B = 4;  // Break Flag
    public static final int P_U = 5;  // Unused
    public static final int P_V = 6;  // Overflow Flag
    public static final int P_N = 7;  // Negative Flag

    // I/O Port (6510 only: DDR + data)
    public static final int PORT_ADDR = (int)0x00;

    // 内存段定义
    // 64KB main RAM
    public static final int RAM_START = (int)0x0000;
    public static final int RAM_END = (int)0xFFFF;
    public static final int RAM_SIZE = 65536;

    // BASIC interpreter ROM
    public static final int BASIC_ROM_START = (int)0xA000;
    public static final int BASIC_ROM_END = (int)0xBFFF;
    public static final int BASIC_ROM_SIZE = 8192;

    // KERNAL operating system ROM
    public static final int KERNAL_ROM_START = (int)0xE000;
    public static final int KERNAL_ROM_END = (int)0xFFFF;
    public static final int KERNAL_ROM_SIZE = 8192;

    // Character generator ROM
    public static final int CHAR_ROM_START = (int)0xD000;
    public static final int CHAR_ROM_END = (int)0xDFFF;
    public static final int CHAR_ROM_SIZE = 4096;

    // I/O + RAM window (switchable)
    public static final int IO_RAM_START = (int)0xD000;
    public static final int IO_RAM_END = (int)0xDFFF;
    public static final int IO_RAM_SIZE = 4096;

    // 外设定义
    // Video Interface Chip II - 6567/6569
    public static final int VICII_BASE = (int)0xD000;
    public static final int VICII_SP0X = (int)0x0001A000;
    public static final int VICII_SP0Y = (int)0x0001A001;
    public static final int VICII_SP1X = (int)0x0001A002;
    public static final int VICII_SP1Y = (int)0x0001A003;
    public static final int VICII_SP2X = (int)0x0001A004;
    public static final int VICII_SP2Y = (int)0x0001A005;
    public static final int VICII_SP3X = (int)0x0001A006;
    public static final int VICII_SP3Y = (int)0x0001A007;
    public static final int VICII_SP4X = (int)0x0001A008;
    public static final int VICII_SP4Y = (int)0x0001A009;
    public static final int VICII_SP5X = (int)0x0001A00A;
    public static final int VICII_SP5Y = (int)0x0001A00B;
    public static final int VICII_SP6X = (int)0x0001A00C;
    public static final int VICII_SP6Y = (int)0x0001A00D;
    public static final int VICII_SP7X = (int)0x0001A00E;
    public static final int VICII_SP7Y = (int)0x0001A00F;
    public static final int VICII_MSIGX = (int)0x0001A010;
    public static final int VICII_SCROLY = (int)0x0001A011;
    public static final int VICII_SCROLX = (int)0x0001A016;
    public static final int VICII_YPSTOP = (int)0x0001A012;
    public static final int VICII_LPX = (int)0x0001A013;
    public static final int VICII_LPY = (int)0x0001A014;
    public static final int VICII_SPENA = (int)0x0001A015;
    public static final int VICII_CSPMC = (int)0x0001A017;
    public static final int VICII_MM0 = (int)0x0001A018;
    public static final int VICII_VM01 = (int)0x0001A016;
    public static final int VICII_VICBAS = (int)0x0001A018;
    public static final int VICII_IRQMASK = (int)0x0001A019;
    public static final int VICII_IRQST = (int)0x0001A01A;
    public static final int VICII_SPBGPR = (int)0x0001A01B;
    public static final int VICII_SPMC = (int)0x0001A01C;
    public static final int VICII_SP1C = (int)0x0001A025;
    public static final int VICII_SP2C = (int)0x0001A026;
    public static final int VICII_SPBC = (int)0x0001A027;
    public static final int VICII_SP1C0 = (int)0x0001A028;
    public static final int VICII_SP2C0 = (int)0x0001A029;
    public static final int VICII_SP3C0 = (int)0x0001A02A;
    public static final int VICII_SP4C0 = (int)0x0001A02B;
    public static final int VICII_SP5C0 = (int)0x0001A02C;
    public static final int VICII_SP6C0 = (int)0x0001A02D;
    public static final int VICII_SP7C0 = (int)0x0001A02E;
    public static final int VICII_REG_FD = (int)0x0001A01D;
    public static final int VICII_BGCOL0 = (int)0x0001A021;
    public static final int VICII_BGCOL1 = (int)0x0001A022;
    public static final int VICII_BGCOL2 = (int)0x0001A023;
    public static final int VICII_BGCOL3 = (int)0x0001A024;

    // Sound Interface Device 6581/8580
    public static final int SID_BASE = (int)0xD400;
    public static final int SID_FREQ1LO = (int)0x0001A800;
    public static final int SID_FREQ1HI = (int)0x0001A801;
    public static final int SID_PW1LO = (int)0x0001A802;
    public static final int SID_PW1HI = (int)0x0001A803;
    public static final int SID_CR1 = (int)0x0001A804;
    public static final int SID_AD1 = (int)0x0001A805;
    public static final int SID_SR1 = (int)0x0001A806;
    public static final int SID_FREQ2LO = (int)0x0001A807;
    public static final int SID_FREQ2HI = (int)0x0001A808;
    public static final int SID_PW2LO = (int)0x0001A809;
    public static final int SID_PW2HI = (int)0x0001A80A;
    public static final int SID_CR2 = (int)0x0001A80B;
    public static final int SID_AD2 = (int)0x0001A80C;
    public static final int SID_SR2 = (int)0x0001A80D;
    public static final int SID_FREQ3LO = (int)0x0001A80E;
    public static final int SID_FREQ3HI = (int)0x0001A80F;
    public static final int SID_PW3LO = (int)0x0001A810;
    public static final int SID_PW3HI = (int)0x0001A811;
    public static final int SID_CR3 = (int)0x0001A812;
    public static final int SID_AD3 = (int)0x0001A813;
    public static final int SID_SR3 = (int)0x0001A814;
    public static final int SID_FCH = (int)0x0001A815;
    public static final int SID_FCL = (int)0x0001A816;
    public static final int SID_RES_FLT = (int)0x0001A817;
    public static final int SID_VOLUME = (int)0x0001A818;
    public static final int SID_POTX = (int)0x0001A819;
    public static final int SID_POTY = (int)0x0001A81A;
    public static final int SID_OSC3 = (int)0x0001A81B;
    public static final int SID_ENV3 = (int)0x0001A81C;

    // Complex Interface Adapter 1 - Keyboard/Serial
    public static final int CIA1_BASE = (int)0xDC00;
    public static final int CIA1_PRA = (int)0x0001B800;
    public static final int CIA1_PRB = (int)0x0001B801;
    public static final int CIA1_DDRA = (int)0x0001B802;
    public static final int CIA1_DDRB = (int)0x0001B803;
    public static final int CIA1_TA_LO = (int)0x0001B804;
    public static final int CIA1_TA_HI = (int)0x0001B805;
    public static final int CIA1_TB_LO = (int)0x0001B806;
    public static final int CIA1_TB_HI = (int)0x0001B807;
    public static final int CIA1_TOD_TENTH = (int)0x0001B808;
    public static final int CIA1_TOD_SEC = (int)0x0001B809;
    public static final int CIA1_TOD_MIN = (int)0x0001B80A;
    public static final int CIA1_TOD_HR = (int)0x0001B80B;
    public static final int CIA1_SDR = (int)0x0001B80C;
    public static final int CIA1_ICR = (int)0x0001B80D;
    public static final int CIA1_CRA = (int)0x0001B80E;
    public static final int CIA1_CRB = (int)0x0001B80F;

    // Complex Interface Adapter 2 - Serial/Bus
    public static final int CIA2_BASE = (int)0xDD00;
    public static final int CIA2_PRA = (int)0x0001BA00;
    public static final int CIA2_PRB = (int)0x0001BA01;
    public static final int CIA2_DDRA = (int)0x0001BA02;
    public static final int CIA2_DDRB = (int)0x0001BA03;
    public static final int CIA2_TA_LO = (int)0x0001BA04;
    public static final int CIA2_TA_HI = (int)0x0001BA05;
    public static final int CIA2_TB_LO = (int)0x0001BA06;
    public static final int CIA2_TB_HI = (int)0x0001BA07;
    public static final int CIA2_TOD_TENTH = (int)0x0001BA08;
    public static final int CIA2_TOD_SEC = (int)0x0001BA09;
    public static final int CIA2_TOD_MIN = (int)0x0001BA0A;
    public static final int CIA2_TOD_HR = (int)0x0001BA0B;
    public static final int CIA2_SDR = (int)0x0001BA0C;
    public static final int CIA2_ICR = (int)0x0001BA0D;
    public static final int CIA2_CRA = (int)0x0001BA0E;
    public static final int CIA2_CRB = (int)0x0001BA0F;

    // Color RAM (4-bit per char cell)
    public static final int COLORRAM_BASE = (int)0xD800;
    public static final int COLORRAM_COLOR = (int)0x0001B000;

    // IEC Serial Bus (via CIA1)
    public static final int IEC_BASE = (int)0xDC00;
    public static final int IEC_IEC_DATA = (int)0x0001B800;
    public static final int IEC_IEC_CLOCK = (int)0x0001B801;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-on / Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt
    public static final int IRQ_IRQ = 2;  // IRQ (VIC raster / CIA timer)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_RESET = 3;  // System Reset
    public static final int PIN_CLK = 4;  // System Clock (~1MHz)
    public static final int PIN_DOTCLK = 5;  // VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
    public static final int PIN_AEC = 6;  // Address Enable Control (VIC steals cycles)
    public static final int PIN_BA = 7;  // Bus Available (from VIC)
    public static final int PIN_IRQ = 8;  // Interrupt Request
    public static final int PIN_NMI = 9;  // Non-Maskable Interrupt
    public static final int PIN_RWB = 10;  // Read/Write
    public static final int PIN_A0_A15 = 11;  // Address Bus
    public static final int PIN_D0_D7 = 12;  // Data Bus

    public static native void commodore_64_init();
}
