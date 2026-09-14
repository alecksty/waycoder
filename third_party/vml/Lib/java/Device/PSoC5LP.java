package vml.device.cypressinfineon.cy8c5888lti_lp097;

/**
 * CY8C5888LTI-LP097 寄存器定义
 * 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
 * 版本: 1.0
 */
public final class CY8C5888LTI_LP097 {
    private CY8C5888LTI_LP097() {} // 工具类
    // CPU架构: ARM-Cortex-M3, 32位, 80000000 Hz

    // 外设定义
    // SCB UART (可编程)
    public static final int UART_BASE = (int)0x40050000;
    public static final int UART_CTRL = (int)0x40050000;
    public static final int UART_STATUS = (int)0x40050004;
    public static final int UART_TX_DATA = (int)0x40050008;
    public static final int UART_RX_DATA = (int)0x4005000C;

    // SCB I2C
    public static final int I2C_BASE = (int)0x40051000;
    public static final int I2C_CTRL = (int)0x40051000;
    public static final int I2C_STATUS = (int)0x40051004;
    public static final int I2C_TX_DATA = (int)0x40051008;
    public static final int I2C_RX_DATA = (int)0x4005100C;

    // TCPWM 定时器
    public static final int TIMER_BASE = (int)0x40060000;
    public static final int TIMER_CTRL = (int)0x40060000;
    public static final int TIMER_STATUS = (int)0x40060004;
    public static final int TIMER_CNT = (int)0x40060008;
    public static final int TIMER_PERIOD = (int)0x4006000C;
    public static final int TIMER_CC = (int)0x40060010;

    // DelSig ADC 20-bit
    public static final int ADC_BASE = (int)0x40100000;
    public static final int ADC_CTRL = (int)0x40100000;
    public static final int ADC_STATUS = (int)0x40100004;
    public static final int ADC_DATA = (int)0x40100008;
    public static final int ADC_CLOCK = (int)0x40100010;

    // GPIO 端口
    public static final int GPIO_BASE = (int)0x40040000;
    public static final int GPIO_DR = (int)0x40040000;
    public static final int GPIO_PS = (int)0x40040004;
    public static final int GPIO_IE = (int)0x40040008;
    public static final int GPIO_DM = (int)0x4004000C;

    // USB 控制器
    public static final int USB_BASE = (int)0x40080000;
    public static final int USB_CR0 = (int)0x40080000;
    public static final int USB_CR1 = (int)0x40080004;
    public static final int USB_STAT = (int)0x40080008;

    public static native void cy8c5888lti_lp097_init();
}
