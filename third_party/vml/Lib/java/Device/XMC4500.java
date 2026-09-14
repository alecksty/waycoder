package vml.device.infineon.xmc4500;

/**
 * XMC4500 寄存器定义
 * 生成自: Infineon/XMC4000/XMC4500
 * 版本: 1.0
 */
public final class XMC4500 {
    private XMC4500() {} // 工具类
    // CPU架构: ARM-Cortex-M4, 32位, 120000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x04;

    public static final int R2_ADDR = (int)0x08;

    public static final int R3_ADDR = (int)0x0C;

    public static final int R4_ADDR = (int)0x10;

    public static final int R5_ADDR = (int)0x14;

    public static final int SP_ADDR = (int)0x34;

    public static final int LR_ADDR = (int)0x38;

    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x080FFFFF;
    public static final int FLASH_SIZE = 1048576;

    public static final int SRAM_START = (int)0x1FF00000;
    public static final int SRAM_END = (int)0x1FF0FFFF;
    public static final int SRAM_SIZE = 65536;

    // Communication Memory
    public static final int SRAM_COM_START = (int)0x20000000;
    public static final int SRAM_COM_END = (int)0x20007FFF;
    public static final int SRAM_COM_SIZE = 32768;

    // CPU SRAM
    public static final int SRAM_CPU_START = (int)0x20010000;
    public static final int SRAM_CPU_END = (int)0x2001FFFF;
    public static final int SRAM_CPU_SIZE = 65536;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4FFFFFFF;
    public static final int PERIPHERAL_SIZE = 268435456;

    // 外设定义
    // System Control Unit
    public static final int SCU_BASE = (int)0x40020000;
    public static final int SCU_CLKCR = (int)0x40020000;
    public static final int SCU_CLKCR_PCLK_SEL = 0;  // CPU clock selection
    public static final int SCU_CLKCR_FBKDIV = 16;  // Feedback divider
    public static final int SCU_PLLCONFIG = (int)0x40020004;
    public static final int SCU_OSCHPCTRL = (int)0x40020008;
    public static final int SCU_CGATSET0 = (int)0x40020020;
    public static final int SCU_CGATSET0_CG_GATE_GPIO = 4;  // GPIO gate enable
    public static final int SCU_CGATCLR0 = (int)0x40020024;

    // Port 0
    public static final int PORT0_BASE = (int)0x48000000;
    public static final int PORT0_OUT = (int)0x48000000;
    public static final int PORT0_OMR = (int)0x48000004;
    public static final int PORT0_IOCR0 = (int)0x48000010;
    public static final int PORT0_IOCR4 = (int)0x48000014;
    public static final int PORT0_IOCR8 = (int)0x48000018;
    public static final int PORT0_IOCR12 = (int)0x4800001C;
    public static final int PORT0_IN = (int)0x48000024;

    // Port 1
    public static final int PORT1_BASE = (int)0x48010000;
    public static final int PORT1_OUT = (int)0x48010000;
    public static final int PORT1_OMR = (int)0x48010004;
    public static final int PORT1_IOCR0 = (int)0x48010010;
    public static final int PORT1_IOCR4 = (int)0x48010014;
    public static final int PORT1_IOCR8 = (int)0x48010018;
    public static final int PORT1_IOCR12 = (int)0x4801001C;
    public static final int PORT1_IN = (int)0x48010024;

    // Port 2
    public static final int PORT2_BASE = (int)0x48020000;
    public static final int PORT2_OUT = (int)0x48020000;
    public static final int PORT2_OMR = (int)0x48020004;
    public static final int PORT2_IOCR0 = (int)0x48020010;
    public static final int PORT2_IOCR4 = (int)0x48020014;
    public static final int PORT2_IN = (int)0x48020024;

    // Universal Serial Interface 0 (UART)
    public static final int USIC0_BASE = (int)0x48030000;
    public static final int USIC0_CCR = (int)0x48030000;
    public static final int USIC0_PCR = (int)0x48030004;
    public static final int USIC0_RBUF = (int)0x48030008;
    public static final int USIC0_TBUF = (int)0x4803000C;
    public static final int USIC0_BRG = (int)0x48030010;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_USIC0_SR0 = 12;  // USIC0 Service Request 0

    public static native void xmc4500_init();
}
