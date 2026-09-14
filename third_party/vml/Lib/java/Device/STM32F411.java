package vml.device.stmicroelectronics.stm32f411;

/**
 * STM32F411 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32F411
 * 版本: 1.0
 */
public final class STM32F411 {
    private STM32F411() {} // 工具类
    // CPU架构: ARM-Cortex-M4, 32位, 100000000 Hz

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
    public static final int FLASH_END = (int)0x0807FFFF;
    public static final int FLASH_SIZE = 524288;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x2001FFFF;
    public static final int SRAM_SIZE = 131072;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x400FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40023800;
    public static final int RCC_CR = (int)0x40023800;
    public static final int RCC_PLLCFGR = (int)0x40023804;
    public static final int RCC_CFGR = (int)0x40023808;
    public static final int RCC_AHB1ENR = (int)0x40023830;
    public static final int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
    public static final int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
    public static final int RCC_AHB1ENR_GPIOCEN = 2;  // GPIOC clock enable
    public static final int RCC_APB1ENR = (int)0x40023840;
    public static final int RCC_APB2ENR = (int)0x40023844;

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x40020000;
    public static final int GPIOA_MODER = (int)0x40020000;
    public static final int GPIOA_OTYPER = (int)0x40020004;
    public static final int GPIOA_OSPEEDR = (int)0x40020008;
    public static final int GPIOA_PUPDR = (int)0x4002000C;
    public static final int GPIOA_IDR = (int)0x40020010;
    public static final int GPIOA_ODR = (int)0x40020014;
    public static final int GPIOA_BSRR = (int)0x40020018;
    public static final int GPIOA_BRR = (int)0x40020028;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x40020400;
    public static final int GPIOB_MODER = (int)0x40020400;
    public static final int GPIOB_OTYPER = (int)0x40020404;
    public static final int GPIOB_OSPEEDR = (int)0x40020408;
    public static final int GPIOB_PUPDR = (int)0x4002040C;
    public static final int GPIOB_IDR = (int)0x40020410;
    public static final int GPIOB_ODR = (int)0x40020414;
    public static final int GPIOB_BSRR = (int)0x40020418;
    public static final int GPIOB_BRR = (int)0x40020428;

    // General Purpose I/O Port C
    public static final int GPIOC_BASE = (int)0x40020800;
    public static final int GPIOC_MODER = (int)0x40020800;
    public static final int GPIOC_OTYPER = (int)0x40020804;
    public static final int GPIOC_IDR = (int)0x40020810;
    public static final int GPIOC_ODR = (int)0x40020814;
    public static final int GPIOC_BSRR = (int)0x40020818;

    // Universal Synchronous/Asynchronous Receiver/Transmitter 1
    public static final int USART1_BASE = (int)0x40011000;
    public static final int USART1_SR = (int)0x40011000;
    public static final int USART1_DR = (int)0x40011004;
    public static final int USART1_BRR = (int)0x40011008;
    public static final int USART1_CR1 = (int)0x4001100C;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_USART1 = 37;  // USART1 Global Interrupt

    public static native void stm32f411_init();
}
