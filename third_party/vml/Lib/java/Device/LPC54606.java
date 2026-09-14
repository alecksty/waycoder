package vml.device.nxp.lpc54606;

/**
 * LPC54606 寄存器定义
 * 生成自: NXP/LPC/LPC54606
 * 版本: 1.0
 */
public final class LPC54606 {
    private LPC54606() {} // 工具类
    // CPU架构: ARM-Cortex-M4, 32位, 180000000 Hz

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
    public static final int FLASH_START = (int)0x00000000;
    public static final int FLASH_END = (int)0x0003FFFF;
    public static final int FLASH_SIZE = 262144;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20021FFF;
    public static final int SRAM_SIZE = 139264;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x401FFFFF;
    public static final int PERIPHERAL_SIZE = 2097152;

    // 外设定义
    // System Control
    public static final int SYSCON_BASE = (int)0x40000000;
    public static final int SYSCON_SYSAHBCLKCTRL = (int)0x40000080;
    public static final int SYSCON_MAINCLKSEL = (int)0x40000004;
    public static final int SYSCON_MAINCLKUEN = (int)0x40000008;
    public static final int SYSCON_SYSPLLCTRL = (int)0x4000000C;

    // General Purpose I/O
    public static final int GPIO_BASE = (int)0x400F4000;
    public static final int GPIO_DIR0 = (int)0x400F4000;
    public static final int GPIO_PIN0 = (int)0x400F5000;
    public static final int GPIO_SET0 = (int)0x400F6000;
    public static final int GPIO_CLR0 = (int)0x400F7000;
    public static final int GPIO_NOT0 = (int)0x400F8000;
    public static final int GPIO_DIR1 = (int)0x400F4004;
    public static final int GPIO_PIN1 = (int)0x400F5004;
    public static final int GPIO_SET1 = (int)0x400F6004;
    public static final int GPIO_CLR1 = (int)0x400F7004;
    public static final int GPIO_NOT1 = (int)0x400F8004;

    // USART0
    public static final int USART0_BASE = (int)0x40086000;
    public static final int USART0_CFG = (int)0x40086000;
    public static final int USART0_CTRL = (int)0x40086004;
    public static final int USART0_STAT = (int)0x40086008;
    public static final int USART0_TXDAT = (int)0x40086010;
    public static final int USART0_RXDAT = (int)0x40086014;
    public static final int USART0_BRG = (int)0x40086020;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_USART0 = 24;  // USART0 Interrupt

    public static native void lpc54606_init();
}
