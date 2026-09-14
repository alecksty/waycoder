package vml.device.nxp.i_mx_rt1062;

/**
 * i.MX RT1062 寄存器定义
 * 生成自: NXP/i.MX RT/i.MX RT1062
 * 版本: 1.0
 */
public final class i.MX RT1062 {
    private i.MX RT1062() {} // 工具类
    // CPU架构: ARM-Cortex-M7, 32位, 528000000 Hz

    // 外设定义
    // LPUART 1
    public static final int UART1_BASE = (int)0x40184000;
    public static final int UART1_VERID = (int)0x40184000;
    public static final int UART1_CTRL = (int)0x40184010;
    public static final int UART1_STAT = (int)0x40184014;
    public static final int UART1_DATA = (int)0x4018401C;
    public static final int UART1_BAUD = (int)0x40184024;

    // LPUART 2
    public static final int UART2_BASE = (int)0x40188000;
    public static final int UART2_CTRL = (int)0x40188010;
    public static final int UART2_STAT = (int)0x40188014;
    public static final int UART2_DATA = (int)0x4018801C;
    public static final int UART2_BAUD = (int)0x40188024;

    // GPIO 1
    public static final int GPIO1_BASE = (int)0x401B8000;
    public static final int GPIO1_DR = (int)0x401B8000;
    public static final int GPIO1_GDIR = (int)0x401B8004;
    public static final int GPIO1_PSR = (int)0x401B8008;
    public static final int GPIO1_ICR1 = (int)0x401B800C;
    public static final int GPIO1_ICR2 = (int)0x401B8010;
    public static final int GPIO1_IMR = (int)0x401B8014;
    public static final int GPIO1_ISR = (int)0x401B8018;
    public static final int GPIO1_EDGE_SEL = (int)0x401B801C;

    // GPT 定时器 1
    public static final int GPT1_BASE = (int)0x401EC000;
    public static final int GPT1_CR = (int)0x401EC000;
    public static final int GPT1_PR = (int)0x401EC004;
    public static final int GPT1_SR = (int)0x401EC008;
    public static final int GPT1_IR = (int)0x401EC00C;
    public static final int GPT1_OCR1 = (int)0x401EC010;
    public static final int GPT1_CNT = (int)0x401EC024;

    // USB OTG 1
    public static final int USB1_BASE = (int)0x402E0000;
    public static final int USB1_ID = (int)0x402E0000;
    public static final int USB1_OTGSC = (int)0x402E000C;
    public static final int USB1_USBCMD = (int)0x402E0100;
    public static final int USB1_PORTSC1 = (int)0x402E0184;

    public static native void i_mx_rt1062_init();
}
