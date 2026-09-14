package vml.device.renesas.ra4m2;

/**
 * RA4M2 寄存器定义
 * 生成自: Renesas/RA/RA4M2
 * 版本: 1.0
 */
public final class RA4M2 {
    private RA4M2() {} // 工具类
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
    public static final int FLASH_START = (int)0x00000000;
    public static final int FLASH_END = (int)0x0003FFFF;
    public static final int FLASH_SIZE = 262144;

    // SRAM0
    public static final int SRAM_START = (int)0x1FFE0000;
    public static final int SRAM_END = (int)0x1FFE7FFF;
    public static final int SRAM_SIZE = 32768;

    // SRAM1
    public static final int SRAM1_START = (int)0x20000000;
    public static final int SRAM1_END = (int)0x20017FFF;
    public static final int SRAM1_SIZE = 98304;

    public static final int PERIPHERAL_START = (int)0x40000000;
    public static final int PERIPHERAL_END = (int)0x400FFFFF;
    public static final int PERIPHERAL_SIZE = 1048576;

    // 外设定义
    // Module Stop Control
    public static final int MSTP_BASE = (int)0x40020000;
    public static final int MSTP_MSTPCR_A = (int)0x40020020;
    public static final int MSTP_MSTPCR_A_MSTP41 = 9;  // GPIO A stop
    public static final int MSTP_MSTPCR_A_MSTP42 = 10;  // GPIO B stop
    public static final int MSTP_MSTPCR_B = (int)0x40020024;
    public static final int MSTP_MSTPCR_C = (int)0x40020028;
    public static final int MSTP_MSTPCR_D = (int)0x4002002C;

    // Interrupt Controller Unit
    public static final int ICU_BASE = (int)0x40030000;
    public static final int ICU_IRQCR0 = (int)0x40030600;
    public static final int ICU_IRQCR1 = (int)0x40030602;

    // General Purpose I/O Port A
    public static final int GPIOA_BASE = (int)0x40040000;
    public static final int GPIOA_PDR = (int)0x40040000;
    public static final int GPIOA_PODR = (int)0x40040004;
    public static final int GPIOA_PIDR = (int)0x40040008;
    public static final int GPIOA_PMR = (int)0x40040010;
    public static final int GPIOA_PCR = (int)0x40040018;

    // General Purpose I/O Port B
    public static final int GPIOB_BASE = (int)0x40040020;
    public static final int GPIOB_PDR = (int)0x40040020;
    public static final int GPIOB_PODR = (int)0x40040024;
    public static final int GPIOB_PIDR = (int)0x40040028;
    public static final int GPIOB_PMR = (int)0x40040030;

    // SCI UART 0
    public static final int SCIUART0_BASE = (int)0x40070000;
    public static final int SCIUART0_SCR = (int)0x40070000;
    public static final int SCIUART0_BRR = (int)0x40070004;
    public static final int SCIUART0_TDR = (int)0x40070008;
    public static final int SCIUART0_RDR = (int)0x4007000C;
    public static final int SCIUART0_SSR = (int)0x40070010;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // 
    public static final int IRQ_SVCALL = 11;  // 
    public static final int IRQ_SCIUART0_RXI = 24;  // SCI UART0 Receive Interrupt
    public static final int IRQ_SCIUART0_TXI = 25;  // SCI UART0 Transmit Interrupt

    public static native void ra4m2_init();
}
