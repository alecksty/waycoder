package vml.device.gigadevice.gd32f103;

/**
 * GD32F103 寄存器定义
 * 生成自: GigaDevice/GD32/GD32F103
 * 版本: 1.0
 */
public final class GD32F103 {
    private GD32F103() {} // 工具类
    // CPU架构: ARM-Cortex-M3, 32位, 108000000 Hz

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
    public static final int FLASH_END = (int)0x0801FFFF;
    public static final int FLASH_SIZE = 131072;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20004FFF;
    public static final int SRAM_SIZE = 20480;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4003FFFF;
    public static final int PERIPHERAL_SIZE = 262144;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40021000;
    public static final int RCC_CTLR = (int)0x40021000;
    public static final int RCC_CFGR0 = (int)0x40021004;
    public static final int RCC_APB2PCENR = (int)0x40021018;
    public static final int RCC_APB2PCENR_IOPAEN = 2;  // GPIOA clock enable
    public static final int RCC_APB2PCENR_IOPBEN = 3;  // GPIOB clock enable
    public static final int RCC_APB2PCENR_IOPCEN = 4;  // GPIOC clock enable
    public static final int RCC_APB2PCENR_USART0EN = 14;  // USART0 clock enable
    public static final int RCC_APB1PCENR = (int)0x4002101C;
    public static final int RCC_APB1PCENR_USART1EN = 17;  // USART1 clock enable

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x40010800;
    public static final int GPIOA_CTL0 = (int)0x40010800;
    public static final int GPIOA_CTL1 = (int)0x40010804;
    public static final int GPIOA_ISTAT = (int)0x40010808;
    public static final int GPIOA_OCTL = (int)0x4001080C;
    public static final int GPIOA_BOP = (int)0x40010810;
    public static final int GPIOA_BC = (int)0x40010814;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x40010C00;
    public static final int GPIOB_CTL0 = (int)0x40010C00;
    public static final int GPIOB_CTL1 = (int)0x40010C04;
    public static final int GPIOB_ISTAT = (int)0x40010C08;
    public static final int GPIOB_OCTL = (int)0x40010C0C;
    public static final int GPIOB_BOP = (int)0x40010C10;
    public static final int GPIOB_BC = (int)0x40010C14;

    // General Purpose I/O Port C
    public static final int GPIOC_BASE = (int)0x40011000;
    public static final int GPIOC_CTL0 = (int)0x40011000;
    public static final int GPIOC_CTL1 = (int)0x40011004;
    public static final int GPIOC_ISTAT = (int)0x40011008;
    public static final int GPIOC_OCTL = (int)0x4001100C;
    public static final int GPIOC_BOP = (int)0x40011010;
    public static final int GPIOC_BC = (int)0x40011014;

    // USART0
    public static final int USART0_BASE = (int)0x40013800;
    public static final int USART0_STATR = (int)0x40013800;
    public static final int USART0_DATAR = (int)0x40013804;
    public static final int USART0_BRR = (int)0x40013808;
    public static final int USART0_CTLR1 = (int)0x4001380C;

    // USART1
    public static final int USART1_BASE = (int)0x40004400;
    public static final int USART1_STATR = (int)0x40004400;
    public static final int USART1_DATAR = (int)0x40004404;
    public static final int USART1_BRR = (int)0x40004408;
    public static final int USART1_CTLR1 = (int)0x4000440C;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_USART0 = 25;  // USART0 Global Interrupt
    public static final int IRQ_USART1 = 37;  // USART1 Global Interrupt

    public static native void gd32f103_init();
}
