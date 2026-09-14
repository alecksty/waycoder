package vml.device.texasinstruments.tm4c123gh6pm;

/**
 * TM4C123GH6PM 寄存器定义
 * 生成自: Texas Instruments/Tiva C/TM4C123GH6PM
 * 版本: 1.0
 */
public final class TM4C123GH6PM {
    private TM4C123GH6PM() {} // 工具类
    // CPU架构: ARM-Cortex-M4F, 32位, 80000000 Hz

    // 外设定义
    // UART 0
    public static final int UART0_BASE = (int)0x4000C000;
    public static final int UART0_DR = (int)0x4000C000;
    public static final int UART0_FR = (int)0x4000C018;
    public static final int UART0_IBRD = (int)0x4000C024;
    public static final int UART0_FBRD = (int)0x4000C028;
    public static final int UART0_LCRH = (int)0x4000C02C;
    public static final int UART0_CTL = (int)0x4000C030;
    public static final int UART0_IM = (int)0x4000C038;
    public static final int UART0_RIS = (int)0x4000C03C;
    public static final int UART0_ICR = (int)0x4000C044;

    // UART 1
    public static final int UART1_BASE = (int)0x4000D000;
    public static final int UART1_DR = (int)0x4000D000;
    public static final int UART1_FR = (int)0x4000D018;
    public static final int UART1_IBRD = (int)0x4000D024;
    public static final int UART1_FBRD = (int)0x4000D028;
    public static final int UART1_LCRH = (int)0x4000D02C;
    public static final int UART1_CTL = (int)0x4000D030;

    // GPIO Port A
    public static final int GPIOA_BASE = (int)0x40004000;
    public static final int GPIOA_DATA = (int)0x400043FC;
    public static final int GPIOA_DIR = (int)0x40004400;
    public static final int GPIOA_IS = (int)0x40004404;
    public static final int GPIOA_IBE = (int)0x40004408;
    public static final int GPIOA_IEV = (int)0x4000440C;
    public static final int GPIOA_IM = (int)0x40004410;
    public static final int GPIOA_RIS = (int)0x40004414;
    public static final int GPIOA_MIS = (int)0x40004418;
    public static final int GPIOA_ICR = (int)0x4000441C;
    public static final int GPIOA_AFSEL = (int)0x40004420;
    public static final int GPIOA_DEN = (int)0x4000451C;

    // 16/32-bit Timer 0
    public static final int TIMER0_BASE = (int)0x40030000;
    public static final int TIMER0_CFG = (int)0x40030000;
    public static final int TIMER0_TAMR = (int)0x40030004;
    public static final int TIMER0_CTL = (int)0x4003000C;
    public static final int TIMER0_ILR = (int)0x40030028;
    public static final int TIMER0_V = (int)0x40030038;
    public static final int TIMER0_ICR = (int)0x40030024;

    // ADC 0
    public static final int ADC0_BASE = (int)0x40038000;
    public static final int ADC0_ACTSS = (int)0x40038000;
    public static final int ADC0_EMUX = (int)0x40038014;
    public static final int ADC0_SSMUX0 = (int)0x40038040;
    public static final int ADC0_SSFIFO0 = (int)0x40038048;
    public static final int ADC0_PROC = (int)0x40038030;

    public static native void tm4c123gh6pm_init();
}
