package vml.device.stmicroelectronics.stm32f072;

/**
 * STM32F072 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32F072
 * 版本: 1.0
 */
public final class STM32F072 {
    private STM32F072() {} // 工具类
    // CPU架构: ARM-Cortex-M0, 32位, 48000000 Hz

    // 寄存器定义
    public static final int R0_ADDR = (int)0x00;

    public static final int R1_ADDR = (int)0x04;

    public static final int R2_ADDR = (int)0x08;

    public static final int R3_ADDR = (int)0x0C;

    public static final int SP_ADDR = (int)0x34;

    public static final int LR_ADDR = (int)0x38;

    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x0801FFFF;
    public static final int FLASH_SIZE = 131072;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20003FFF;
    public static final int SRAM_SIZE = 16384;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x40027FFF;
    public static final int PERIPHERAL_SIZE = 163840;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40021000;
    public static final int RCC_CR = (int)0x40021000;
    public static final int RCC_CFGR = (int)0x40021004;
    public static final int RCC_AHBENR = (int)0x40021014;
    public static final int RCC_AHBENR_GPIOAEN = 17;  // GPIOA clock enable
    public static final int RCC_AHBENR_GPIOBEN = 18;  // GPIOB clock enable
    public static final int RCC_AHBENR_GPIOCEN = 19;  // GPIOC clock enable
    public static final int RCC_APB2ENR = (int)0x40021018;

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

    // General Purpose I/O Port C
    public static final int GPIOC_BASE = (int)0x48000800;
    public static final int GPIOC_MODER = (int)0x48000800;
    public static final int GPIOC_OTYPER = (int)0x48000804;
    public static final int GPIOC_IDR = (int)0x48000810;
    public static final int GPIOC_ODR = (int)0x48000814;
    public static final int GPIOC_BSRR = (int)0x48000818;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 

    public static native void stm32f072_init();
}
