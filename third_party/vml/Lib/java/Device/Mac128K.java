package vml.device.applecomputer.macintosh_128k;

/**
 * Macintosh-128K 寄存器定义
 * 生成自: Apple Computer/Macintosh/Macintosh-128K
 * 版本: 1.0
 */
public final class Macintosh_128K {
    private Macintosh_128K() {} // 工具类
    // CPU架构: MC68000, 32位, 7833600 Hz

    // 寄存器定义
    // Data Register 0
    public static final int D0_ADDR = (int)0x00;

    // Data Register 1
    public static final int D1_ADDR = (int)0x04;

    // Data Register 2
    public static final int D2_ADDR = (int)0x08;

    // Data Register 3
    public static final int D3_ADDR = (int)0x0C;

    // Data Register 4
    public static final int D4_ADDR = (int)0x10;

    // Data Register 5
    public static final int D5_ADDR = (int)0x14;

    // Data Register 6
    public static final int D6_ADDR = (int)0x18;

    // Data Register 7
    public static final int D7_ADDR = (int)0x1C;

    // Address Register 0
    public static final int A0_ADDR = (int)0x20;

    // Address Register 1
    public static final int A1_ADDR = (int)0x24;

    // Address Register 2
    public static final int A2_ADDR = (int)0x28;

    // Address Register 3
    public static final int A3_ADDR = (int)0x2C;

    // Address Register 4
    public static final int A4_ADDR = (int)0x30;

    // Address Register 5
    public static final int A5_ADDR = (int)0x34;

    // Address Register 6
    public static final int A6_ADDR = (int)0x38;

    // Stack Pointer (USP)
    public static final int A7_ADDR = (int)0x3C;

    // Program Counter
    public static final int PC_ADDR = (int)0x40;

    // Status Register
    public static final int SR_ADDR = (int)0x44;
    public static final int SR_C = 0;  // Carry
    public static final int SR_V = 1;  // Overflow
    public static final int SR_Z = 2;  // Zero
    public static final int SR_N = 3;  // Negative
    public static final int SR_X = 4;  // Extend
    public static final int SR_I0 = 8;  // Interrupt Mask 0
    public static final int SR_I1 = 9;  // Interrupt Mask 1
    public static final int SR_I2 = 10;  // Interrupt Mask 2
    public static final int SR_S = 13;  // Supervisor/User
    public static final int SR_T0 = 14;  // Trace Mode 0
    public static final int SR_T1 = 15;  // Trace Mode 1

    // 内存段定义
    // Main RAM (128KB unified)
    public static final int RAM_START = (int)0x000000;
    public static final int RAM_END = (int)0x01FFFF;
    public static final int RAM_SIZE = 131072;

    // Mac ROM (128KB)
    public static final int ROM_START = (int)0x40000000;
    public static final int ROM_END = (int)0x4001FFFF;
    public static final int ROM_SIZE = 131072;

    // Screen bitmap (512x342x1 = 21792 bytes)
    public static final int FRAMEBUFFER_START = (int)0x00400000;
    public static final int FRAMEBUFFER_END = (int)0x00400555;
    public static final int FRAMEBUFFER_SIZE = 1366;

    // Shadow screen (double-buffering)
    public static final int FRAMEBUFFER2_START = (int)0x00410000;
    public static final int FRAMEBUFFER2_END = (int)0x00410555;
    public static final int FRAMEBUFFER2_SIZE = 1366;

    // VIA 6522 (I/O)
    public static final int VIA_START = (int)0x00E00000;
    public static final int VIA_END = (int)0x00E0FFFF;
    public static final int VIA_SIZE = 4096;

    // SCC 8530 (serial)
    public static final int SCC_START = (int)0x00F00000;
    public static final int SCC_END = (int)0x00F0FFFF;
    public static final int SCC_SIZE = 4096;

    // ADB bus
    public static final int ADB_START = (int)0x01600000;
    public static final int ADB_END = (int)0x0160FFFF;
    public static final int ADB_SIZE = 4096;

    // IWM floppy controller
    public static final int IWM_START = (int)0x01E00000;
    public static final int IWM_END = (int)0x01E0FFFF;
    public static final int IWM_SIZE = 4096;

    // 外设定义
    // Versatile Interface Adapter 6522
    public static final int VIA_BASE = (int)0xE00000;
    public static final int VIA_ORB = (int)0x01C00000;
    public static final int VIA_ORA = (int)0x01C00002;
    public static final int VIA_DDRB = (int)0x01C00004;
    public static final int VIA_DDRA = (int)0x01C00006;
    public static final int VIA_T1C_L = (int)0x01C00008;
    public static final int VIA_T1C_H = (int)0x01C0000A;
    public static final int VIA_T1L_L = (int)0x01C0000C;
    public static final int VIA_T1L_H = (int)0x01C0000E;
    public static final int VIA_T2C_L = (int)0x01C00010;
    public static final int VIA_T2C_H = (int)0x01C00012;
    public static final int VIA_SR = (int)0x01C00014;
    public static final int VIA_ACR = (int)0x01C00016;
    public static final int VIA_PCR = (int)0x01C00018;
    public static final int VIA_IFR = (int)0x01C0001E;
    public static final int VIA_IER = (int)0x01C0001E;

    // SCC 8530 Serial Communications Controller
    public static final int SCC_BASE = (int)0xF00000;
    public static final int SCC_SCC_CHA_B = (int)0x01E00000;
    public static final int SCC_SCC_CHA_C = (int)0x01E00002;
    public static final int SCC_SCC_CHB_D = (int)0x01E00004;
    public static final int SCC_SCC_CHB_CT = (int)0x01E00006;

    // Integrated Woz Machine - Floppy Disk Controller
    public static final int IWM_BASE = (int)0x1E00000;
    public static final int IWM_IWM_DATA = (int)0x03C00000;
    public static final int IWM_IWM_MODE = (int)0x03C00008;
    public static final int IWM_IWM_Q6L = (int)0x03C00020;
    public static final int IWM_IWM_Q7L = (int)0x03C00022;
    public static final int IWM_IWM_Q6R = (int)0x03C00024;
    public static final int IWM_IWM_Q7R = (int)0x03C00026;

    // Video Graphics Controller (custom Apple chip)
    public static final int VGC_BASE = (int)0x00F20000;
    public static final int VGC_VGC_MODE = (int)0x01E40000;
    public static final int VGC_VGC_START_HI = (int)0x01E40002;
    public static final int VGC_VGC_START_LO = (int)0x01E40004;

    // Apple Desktop Bus
    public static final int ADB_BASE = (int)0x01600000;
    public static final int ADB_ADB_DATA = (int)0x02C00000;
    public static final int ADB_ADB_STATUS = (int)0x02C00004;
    public static final int ADB_ADB_CMD = (int)0x02C00008;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // Reset Initial SP
    public static final int IRQ_RESET_PC = 2;  // Reset Initial PC
    public static final int IRQ_IRQ1 = 24;  // VIA interrupt (level 1)
    public static final int IRQ_IRQ2 = 25;  // SCC interrupt (level 2)
    public static final int IRQ_IRQ3 = 26;  // ADB / VIA (level 3)
    public static final int IRQ_IRQ4 = 27;  // ADB / VIA (level 4)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // 16MHz master clock / 7.83MHz CPU clock
    public static final int PIN_FC0 = 4;  // Function Code 0
    public static final int PIN_FC1 = 5;  // Function Code 1
    public static final int PIN_FC2 = 6;  // Function Code 2
    public static final int PIN_AS = 7;  // Address Strobe
    public static final int PIN_UDS = 8;  // Upper Data Strobe
    public static final int PIN_LDS = 9;  // Lower Data Strobe
    public static final int PIN_RWB = 10;  // Read/Write
    public static final int PIN_DTACK = 11;  // Data Acknowledge
    public static final int PIN_BERR = 12;  // Bus Error
    public static final int PIN_BR = 13;  // Bus Request
    public static final int PIN_BG = 14;  // Bus Grant
    public static final int PIN_BGACK = 15;  // Bus Grant Acknowledge
    public static final int PIN_IPL0 = 16;  // Interrupt Priority 0
    public static final int PIN_IPL1 = 17;  // Interrupt Priority 1
    public static final int PIN_IPL2 = 18;  // Interrupt Priority 2
    public static final int PIN_RESET = 19;  // Reset
    public static final int PIN_HALT = 20;  // Halt
    public static final int PIN_A1_A23 = 21;  // Address Bus (24-bit)
    public static final int PIN_D0_D15 = 22;  // Data Bus (16-bit)

    public static native void macintosh_128k_init();
}
