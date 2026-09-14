package vml.device.texasinstruments.tms320f280049;

/**
 * TMS320F280049 寄存器定义
 * 生成自: Texas Instruments/C2000/TMS320F280049
 * 版本: 1.0
 */
public final class TMS320F280049 {
    private TMS320F280049() {} // 工具类
    // CPU架构: C28x-DSP, 32位, 100000000 Hz

    // 寄存器定义
    // Accumulator Low
    public static final int AL_ADDR = (int)0x00;

    // Accumulator High
    public static final int AH_ADDR = (int)0x02;

    // Product High
    public static final int PH_ADDR = (int)0x04;

    // Product Low
    public static final int PL_ADDR = (int)0x06;

    // Temporary Register
    public static final int TREG_ADDR = (int)0x08;

    public static final int AR0_ADDR = (int)0x0A;

    public static final int AR1_ADDR = (int)0x0C;

    // Status 0
    public static final int ST0_ADDR = (int)0x20;

    // Status 1
    public static final int ST1_ADDR = (int)0x22;

    // Program Counter
    public static final int PC_ADDR = (int)0x24;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x26;

    // 内存段定义
    public static final int FLASH_START = (int)0x080000;
    public static final int FLASH_END = (int)0x0BFFFF;
    public static final int FLASH_SIZE = 262144;

    // Local Shared RAM
    public static final int SRAM_LS_START = (int)0x008000;
    public static final int SRAM_LS_END = (int)0x00BFFF;
    public static final int SRAM_LS_SIZE = 16384;

    // Global Shared RAM
    public static final int SRAM_GS_START = (int)0x00C000;
    public static final int SRAM_GS_END = (int)0x01FFFF;
    public static final int SRAM_GS_SIZE = 81920;

    public static final int PERIPHERAL_START = (int)0x400000;
    public static final int PERIPHERAL_END = (int)0x40FFFF;
    public static final int PERIPHERAL_SIZE = 65536;

    // 外设定义
    // PLL Clock Control
    public static final int PLL_BASE = (int)0x5C10;
    public static final int PLL_SYSPLLCTL1 = (int)0x00005C10;
    public static final int PLL_SYSPLLCTL2 = (int)0x00005C12;
    public static final int PLL_CLKSRCCTL1 = (int)0x00005C14;
    public static final int PLL_CLKSRCCTL2 = (int)0x00005C16;

    // GPIO Control Registers
    public static final int GPIO_CTRL_BASE = (int)0x7C00;
    public static final int GPIO_CTRL_GPACTRL = (int)0x00007C00;
    public static final int GPIO_CTRL_GPAQSEL1 = (int)0x00007C02;
    public static final int GPIO_CTRL_GPAQSEL2 = (int)0x00007C04;
    public static final int GPIO_CTRL_GPAMUX1 = (int)0x00007C06;
    public static final int GPIO_CTRL_GPAMUX2 = (int)0x00007C08;
    public static final int GPIO_CTRL_GPADIR = (int)0x00007C0A;
    public static final int GPIO_CTRL_GPAPUD = (int)0x00007C0C;

    // GPIO Data Registers
    public static final int GPIO_DATA_BASE = (int)0x7F00;
    public static final int GPIO_DATA_GPADAT = (int)0x00007F00;
    public static final int GPIO_DATA_GPASET = (int)0x00007F02;
    public static final int GPIO_DATA_GPACLEAR = (int)0x00007F04;
    public static final int GPIO_DATA_GPATOGGLE = (int)0x00007F06;
    public static final int GPIO_DATA_GPBDAT = (int)0x00007F08;
    public static final int GPIO_DATA_GPBSET = (int)0x00007F0A;
    public static final int GPIO_DATA_GPBCLEAR = (int)0x00007F0C;
    public static final int GPIO_DATA_GPBTOGGLE = (int)0x00007F0E;

    // GPIO B Control
    public static final int GPIO_B_CTRL_BASE = (int)0x7C20;
    public static final int GPIO_B_CTRL_GPBMUX1 = (int)0x00007C20;
    public static final int GPIO_B_CTRL_GPBMUX2 = (int)0x00007C22;
    public static final int GPIO_B_CTRL_GPBDIR = (int)0x00007C24;
    public static final int GPIO_B_CTRL_GPBPUD = (int)0x00007C26;

    // SCI-A UART
    public static final int SCI_A_BASE = (int)0x7320;
    public static final int SCI_A_SCICCR = (int)0x00007320;
    public static final int SCI_A_SCICTL1 = (int)0x00007322;
    public static final int SCI_A_SCIBAUD = (int)0x00007324;
    public static final int SCI_A_SCIRXBUF = (int)0x0000732A;
    public static final int SCI_A_SCITXBUF = (int)0x0000732C;

    // 中断向量定义
    public static final int IRQ_RESET = 1;  // 
    public static final int IRQ_SCIA_RX = 8;  // SCI-A Receive Interrupt
    public static final int IRQ_SCIA_TX = 9;  // SCI-A Transmit Interrupt

    public static native void tms320f280049_init();
}
