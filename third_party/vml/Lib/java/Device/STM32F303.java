package vml.device.stmicroelectronics.stm32f303cct6;

/**
 * STM32F303CCT6 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32F303CCT6
 * 版本: 1.0
 */
public final class STM32F303CCT6 {
    private STM32F303CCT6() {} // 工具类
    // CPU架构: ARM-Cortex-M4F, 32位, 72000000 Hz

    // 外设定义
    // USART 1
    public static final int USART1_BASE = (int)0x40013800;
    public static final int USART1_SR = (int)0x40013800;
    public static final int USART1_DR = (int)0x40013804;
    public static final int USART1_BRR = (int)0x40013808;
    public static final int USART1_CR1 = (int)0x4001380C;
    public static final int USART1_CR2 = (int)0x40013810;
    public static final int USART1_CR3 = (int)0x40013814;

    // USART 2
    public static final int USART2_BASE = (int)0x40004400;
    public static final int USART2_SR = (int)0x40004400;
    public static final int USART2_DR = (int)0x40004404;
    public static final int USART2_BRR = (int)0x40004408;
    public static final int USART2_CR1 = (int)0x4000440C;

    // USART 3
    public static final int USART3_BASE = (int)0x40004800;
    public static final int USART3_SR = (int)0x40004800;
    public static final int USART3_DR = (int)0x40004804;
    public static final int USART3_BRR = (int)0x40004808;
    public static final int USART3_CR1 = (int)0x4000480C;

    // GPIO Port A
    public static final int GPIOA_BASE = (int)0x48000000;
    public static final int GPIOA_MODER = (int)0x48000000;
    public static final int GPIOA_OTYPER = (int)0x48000004;
    public static final int GPIOA_OSPEEDR = (int)0x48000008;
    public static final int GPIOA_PUPDR = (int)0x4800000C;
    public static final int GPIOA_IDR = (int)0x48000010;
    public static final int GPIOA_ODR = (int)0x48000014;
    public static final int GPIOA_BSRR = (int)0x48000018;
    public static final int GPIOA_AFRL = (int)0x48000020;
    public static final int GPIOA_AFRH = (int)0x48000024;

    // 高级定时器 1
    public static final int TIM1_BASE = (int)0x40012C00;
    public static final int TIM1_CR1 = (int)0x40012C00;
    public static final int TIM1_CNT = (int)0x40012C24;
    public static final int TIM1_PSC = (int)0x40012C28;
    public static final int TIM1_ARR = (int)0x40012C2C;
    public static final int TIM1_CCR1 = (int)0x40012C34;

    // ADC 1
    public static final int ADC1_BASE = (int)0x50000000;
    public static final int ADC1_SR = (int)0x50000000;
    public static final int ADC1_CR = (int)0x50000008;
    public static final int ADC1_CFGR = (int)0x5000000C;
    public static final int ADC1_SMPR1 = (int)0x50000014;
    public static final int ADC1_DR = (int)0x50000040;

    public static native void stm32f303cct6_init();
}
