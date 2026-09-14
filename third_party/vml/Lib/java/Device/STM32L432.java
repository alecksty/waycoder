package vml.device.stmicroelectronics.stm32l432;

/**
 * STM32L432 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32L432
 * 版本: 1.0
 */
public final class STM32L432 {
    private STM32L432() {} // 工具类
    // CPU架构: ARM-Cortex-M4, 32位, 80000000 Hz

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
    public static final int FLASH_END = (int)0x0803FFFF;
    public static final int FLASH_SIZE = 262144;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x2000FFFF;
    public static final int SRAM_SIZE = 65536;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4007FFFF;
    public static final int PERIPHERAL_SIZE = 524288;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40021000;
    public static final int RCC_CR = (int)0x40021000;
    public static final int RCC_CFGR = (int)0x40021008;
    public static final int RCC_PLLCFGR = (int)0x4002100C;
    public static final int RCC_AHB1ENR = (int)0x40021038;
    public static final int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
    public static final int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
    public static final int RCC_APB1ENR1 = (int)0x40021058;
    public static final int RCC_APB2ENR = (int)0x40021060;

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x48000000;
    public static final int GPIOA_MODER = (int)0x48000000;
    public static final int GPIOA_OTYPER = (int)0x48000004;
    public static final int GPIOA_OSPEEDR = (int)0x48000008;
    public static final int GPIOA_PUPDR = (int)0x4800000C;
    public static final int GPIOA_IDR = (int)0x48000010;
    public static final int GPIOA_ODR = (int)0x48000014;
    public static final int GPIOA_BSRR = (int)0x48000018;
    public static final int GPIOA_BRR = (int)0x48000028;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x48000400;
    public static final int GPIOB_MODER = (int)0x48000400;
    public static final int GPIOB_OTYPER = (int)0x48000404;
    public static final int GPIOB_OSPEEDR = (int)0x48000408;
    public static final int GPIOB_PUPDR = (int)0x4800040C;
    public static final int GPIOB_IDR = (int)0x48000410;
    public static final int GPIOB_ODR = (int)0x48000414;
    public static final int GPIOB_BSRR = (int)0x48000418;
    public static final int GPIOB_BRR = (int)0x48000428;

    // Low-power UART 1
    public static final int LPUART1_BASE = (int)0x40008000;
    public static final int LPUART1_CR1 = (int)0x40008000;
    public static final int LPUART1_BRR = (int)0x4000800C;
    public static final int LPUART1_RDR = (int)0x40008024;
    public static final int LPUART1_TDR = (int)0x40008028;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_LPUART1 = 53;  // LPUART1 Global Interrupt

    public static native void stm32l432_init();
}
