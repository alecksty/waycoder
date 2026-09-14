package vml.device.stmicroelectronics.stm32h743;

/**
 * STM32H743 寄存器定义
 * 生成自: STMicroelectronics/STM32/STM32H743
 * 版本: 1.0
 */
public final class STM32H743 {
    private STM32H743() {} // 工具类
    // CPU架构: ARM-Cortex-M7, 32位, 400000000 Hz

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
    public static final int FLASH_END = (int)0x081FFFFF;
    public static final int FLASH_SIZE = 2097152;

    // DTCM RAM
    public static final int DTCM_START = (int)0x20000000;
    public static final int DTCM_END = (int)0x2001FFFF;
    public static final int DTCM_SIZE = 131072;

    // ITCM RAM
    public static final int ITCM_START = (int)0x00000000;
    public static final int ITCM_END = (int)0x0000FFFF;
    public static final int ITCM_SIZE = 65536;

    // AXI SRAM
    public static final int SRAM_AXI_START = (int)0x24000000;
    public static final int SRAM_AXI_END = (int)0x2407FFFF;
    public static final int SRAM_AXI_SIZE = 524288;

    // SRAM1-3
    public static final int SRAM_SRAM_START = (int)0x30000000;
    public static final int SRAM_SRAM_END = (int)0x3003FFFF;
    public static final int SRAM_SRAM_SIZE = 262144;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4FFFFFFF;
    public static final int PERIPHERAL_SIZE = 268435456;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x58024400;
    public static final int RCC_CR = (int)0x58024400;
    public static final int RCC_CFGR = (int)0x58024404;
    public static final int RCC_PLL1CFGR = (int)0x5802440C;
    public static final int RCC_AHB1ENR = (int)0x58024430;
    public static final int RCC_AHB1ENR_GPIOAEN = 0;  // GPIOA clock enable
    public static final int RCC_AHB1ENR_GPIOBEN = 1;  // GPIOB clock enable
    public static final int RCC_AHB1ENR_GPIOCEN = 2;  // GPIOC clock enable
    public static final int RCC_AHB1ENR_GPIODEN = 3;  // GPIOD clock enable
    public static final int RCC_AHB1ENR_GPIOEEN = 4;  // GPIOE clock enable
    public static final int RCC_AHB1ENR_DMA1EN = 21;  // DMA1 clock enable
    public static final int RCC_AHB1ENR_DMA2EN = 22;  // DMA2 clock enable
    public static final int RCC_AHB2ENR = (int)0x58024434;
    public static final int RCC_AHB4ENR = (int)0x5802443C;
    public static final int RCC_APB1LENR = (int)0x58024450;
    public static final int RCC_APB2ENR = (int)0x58024458;

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x58020000;
    public static final int GPIOA_MODER = (int)0x58020000;
    public static final int GPIOA_OTYPER = (int)0x58020004;
    public static final int GPIOA_OSPEEDR = (int)0x58020008;
    public static final int GPIOA_PUPDR = (int)0x5802000C;
    public static final int GPIOA_IDR = (int)0x58020010;
    public static final int GPIOA_ODR = (int)0x58020014;
    public static final int GPIOA_BSRR = (int)0x58020018;
    public static final int GPIOA_BRR = (int)0x58020028;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x58020400;
    public static final int GPIOB_MODER = (int)0x58020400;
    public static final int GPIOB_OTYPER = (int)0x58020404;
    public static final int GPIOB_OSPEEDR = (int)0x58020408;
    public static final int GPIOB_PUPDR = (int)0x5802040C;
    public static final int GPIOB_IDR = (int)0x58020410;
    public static final int GPIOB_ODR = (int)0x58020414;
    public static final int GPIOB_BSRR = (int)0x58020418;
    public static final int GPIOB_BRR = (int)0x58020428;

    // General Purpose I/O Port C
    public static final int GPIOC_BASE = (int)0x58020800;
    public static final int GPIOC_MODER = (int)0x58020800;
    public static final int GPIOC_OTYPER = (int)0x58020804;
    public static final int GPIOC_IDR = (int)0x58020810;
    public static final int GPIOC_ODR = (int)0x58020814;
    public static final int GPIOC_BSRR = (int)0x58020818;

    // General Purpose I/O Port D
    public static final int GPIOD_BASE = (int)0x58020C00;
    public static final int GPIOD_MODER = (int)0x58020C00;
    public static final int GPIOD_OTYPER = (int)0x58020C04;
    public static final int GPIOD_IDR = (int)0x58020C10;
    public static final int GPIOD_ODR = (int)0x58020C14;
    public static final int GPIOD_BSRR = (int)0x58020C18;

    // General Purpose I/O Port E
    public static final int GPIOE_BASE = (int)0x58021000;
    public static final int GPIOE_MODER = (int)0x58021000;
    public static final int GPIOE_OTYPER = (int)0x58021004;
    public static final int GPIOE_IDR = (int)0x58021010;
    public static final int GPIOE_ODR = (int)0x58021014;
    public static final int GPIOE_BSRR = (int)0x58021018;

    // USART1
    public static final int USART1_BASE = (int)0x40011000;
    public static final int USART1_CR1 = (int)0x40011000;
    public static final int USART1_BRR = (int)0x4001100C;
    public static final int USART1_RDR = (int)0x40011024;
    public static final int USART1_TDR = (int)0x40011028;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_SYSTICK = 15;  // 
    public static final int IRQ_USART1 = 56;  // USART1 Global Interrupt

    public static native void stm32h743_init();
}
