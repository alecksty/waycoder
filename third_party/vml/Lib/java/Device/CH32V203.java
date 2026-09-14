package vml.device.wch.ch32v203;

/**
 * CH32V203 寄存器定义
 * 生成自: WCH/CH32V2/CH32V203
 * 版本: 1.0
 */
public final class CH32V203 {
    private CH32V203() {} // 工具类
    // CPU架构: RISC-V, 32位, 144000000 Hz

    // 寄存器定义
    // Return Address
    public static final int X1_ADDR = (int)0x04;

    // Stack Pointer (SP)
    public static final int X2_ADDR = (int)0x08;

    // Global Pointer (GP)
    public static final int X3_ADDR = (int)0x0C;

    // Frame Pointer (FP)
    public static final int X8_ADDR = (int)0x20;

    // Function Argument (A0)
    public static final int X10_ADDR = (int)0x28;

    // Function Argument (A1)
    public static final int X11_ADDR = (int)0x2C;

    // Program Counter
    public static final int PC_ADDR = (int)0x3C;

    // 内存段定义
    public static final int FLASH_START = (int)0x08000000;
    public static final int FLASH_END = (int)0x0800FFFF;
    public static final int FLASH_SIZE = 65536;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20004FFF;
    public static final int SRAM_SIZE = 20480;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4003FFFF;
    public static final int PERIPHERAL_SIZE = 262144;

    // 外设定义
    // Reset and Clock Control
    public static final int RCC_BASE = (int)0x40021000;
    public static final int RCC_RCC_CTLR = (int)0x40021000;
    public static final int RCC_RCC_CFGR0 = (int)0x40021004;
    public static final int RCC_RCC_APB2PCENR = (int)0x40021018;
    public static final int RCC_RCC_APB2PCENR_IOPAEN = 2;  // GPIOA clock enable
    public static final int RCC_RCC_APB2PCENR_IOPBEN = 3;  // GPIOB clock enable
    public static final int RCC_RCC_APB2PCENR_IOPCEN = 4;  // GPIOC clock enable

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x40010800;
    public static final int GPIOA_CFGLR = (int)0x40010800;
    public static final int GPIOA_CFGHR = (int)0x40010804;
    public static final int GPIOA_INDR = (int)0x40010808;
    public static final int GPIOA_OUTDR = (int)0x4001080C;
    public static final int GPIOA_BSHR = (int)0x40010810;
    public static final int GPIOA_BCR = (int)0x40010814;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x40010C00;
    public static final int GPIOB_CFGLR = (int)0x40010C00;
    public static final int GPIOB_CFGHR = (int)0x40010C04;
    public static final int GPIOB_INDR = (int)0x40010C08;
    public static final int GPIOB_OUTDR = (int)0x40010C0C;
    public static final int GPIOB_BSHR = (int)0x40010C10;
    public static final int GPIOB_BCR = (int)0x40010C14;

    // General Purpose I/O Port C
    public static final int GPIOC_BASE = (int)0x40011000;
    public static final int GPIOC_CFGLR = (int)0x40011000;
    public static final int GPIOC_CFGHR = (int)0x40011004;
    public static final int GPIOC_INDR = (int)0x40011008;
    public static final int GPIOC_OUTDR = (int)0x4001100C;
    public static final int GPIOC_BSHR = (int)0x40011010;
    public static final int GPIOC_BCR = (int)0x40011014;

    // USART1
    public static final int USART1_BASE = (int)0x40013800;
    public static final int USART1_USART_STATR = (int)0x40013800;
    public static final int USART1_USART_DATAR = (int)0x40013804;
    public static final int USART1_USART_BRR = (int)0x40013808;
    public static final int USART1_USART_CTLR1 = (int)0x4001380C;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_MACHINESOFTWARE = 3;  // 
    public static final int IRQ_MACHINETIMER = 7;  // 
    public static final int IRQ_MACHINEEXTERNAL = 11;  // 
    public static final int IRQ_USART1 = 25;  // USART1 Global Interrupt

    public static native void ch32v203_init();
}
