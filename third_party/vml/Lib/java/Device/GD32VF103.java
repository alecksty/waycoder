package vml.device.gigadevice.gd32vf103;

/**
 * GD32VF103 寄存器定义
 * 生成自: GigaDevice/GD32/GD32VF103
 * 版本: 1.0
 */
public final class GD32VF103 {
    private GD32VF103() {} // 工具类
    // CPU架构: RISC-V, 32位, 108000000 Hz

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
    public static final int FLASH_END = (int)0x0801FFFF;
    public static final int FLASH_SIZE = 131072;

    public static final int SRAM_START = (int)0x20000000;
    public static final int SRAM_END = (int)0x20007FFF;
    public static final int SRAM_SIZE = 32768;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x4003FFFF;
    public static final int PERIPHERAL_SIZE = 262144;

    // 外设定义
    // Reset and Clock Control
    public static final int RCU_BASE = (int)0x40021000;
    public static final int RCU_CTL = (int)0x40021000;
    public static final int RCU_CFG0 = (int)0x40021004;
    public static final int RCU_CFG1 = (int)0x40021008;
    public static final int RCU_APB2EN = (int)0x40021018;
    public static final int RCU_APB2EN_PAEN = 2;  // GPIOA enable
    public static final int RCU_APB2EN_PBEN = 3;  // GPIOB enable
    public static final int RCU_APB2EN_PCEN = 4;  // GPIOC enable
    public static final int RCU_APB2EN_USART0EN = 14;  // USART0 enable
    public static final int RCU_APB1EN = (int)0x4002101C;

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

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_MACHINESOFTWARE = 3;  // 
    public static final int IRQ_MACHINETIMER = 7;  // 
    public static final int IRQ_MACHINEEXTERNAL = 11;  // 
    public static final int IRQ_USART0 = 25;  // USART0 Global Interrupt

    public static native void gd32vf103_init();
}
