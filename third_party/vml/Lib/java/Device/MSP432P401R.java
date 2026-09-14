package vml.device.texasinstruments.msp432p401r;

/**
 * MSP432P401R 寄存器定义
 * 生成自: Texas Instruments/MSP432/MSP432P401R
 * 版本: 1.0
 */
public final class MSP432P401R {
    private MSP432P401R() {} // 工具类
    // CPU架构: ARM-Cortex-M4F, 32位, 48000000 Hz

    // 外设定义
    // eUSCI_A0 UART
    public static final int UART0_BASE = (int)0x40001000;
    public static final int UART0_CTLW0 = (int)0x40001000;
    public static final int UART0_BRW = (int)0x40001006;
    public static final int UART0_UCA0TXBUF = (int)0x40001008;
    public static final int UART0_UCA0RXBUF = (int)0x4000100A;
    public static final int UART0_IFG = (int)0x4000100C;
    public static final int UART0_IE = (int)0x4000100E;

    // eUSCI_A1 UART
    public static final int UART1_BASE = (int)0x40002000;
    public static final int UART1_CTLW0 = (int)0x40002000;
    public static final int UART1_BRW = (int)0x40002006;
    public static final int UART1_TXBUF = (int)0x40002008;
    public static final int UART1_RXBUF = (int)0x4000200A;
    public static final int UART1_IFG = (int)0x4000200C;
    public static final int UART1_IE = (int)0x4000200E;

    // Timer_A0 16bit
    public static final int TIMER0_BASE = (int)0x40003000;
    public static final int TIMER0_CTL = (int)0x40003000;
    public static final int TIMER0_R = (int)0x40003010;
    public static final int TIMER0_CCR0 = (int)0x40003012;
    public static final int TIMER0_CCR1 = (int)0x40003014;
    public static final int TIMER0_CCR2 = (int)0x40003016;
    public static final int TIMER0_EX0 = (int)0x40003020;

    // ADC14 14-bit
    public static final int ADC14_BASE = (int)0x40006000;
    public static final int ADC14_CTL0 = (int)0x40006000;
    public static final int ADC14_CTL1 = (int)0x40006002;
    public static final int ADC14_LO = (int)0x40006004;
    public static final int ADC14_HI = (int)0x40006006;
    public static final int ADC14_MCTL0 = (int)0x40006008;
    public static final int ADC14_MEM0 = (int)0x40006020;

    public static native void msp432p401r_init();
}
