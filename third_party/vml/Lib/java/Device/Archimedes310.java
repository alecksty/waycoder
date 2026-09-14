package vml.device.acorncomputers.acorn_archimedes_a310;

/**
 * Acorn-Archimedes-A310 寄存器定义
 * 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
 * 版本: 1.0
 */
public final class Acorn_Archimedes_A310 {
    private Acorn_Archimedes_A310() {} // 工具类
    // CPU架构: ARM250, 32位, 26000000 Hz

    // 寄存器定义
    // General Purpose Register 0
    public static final int R0_ADDR = (int)0x00;

    // General Purpose Register 1
    public static final int R1_ADDR = (int)0x04;

    // General Purpose Register 2
    public static final int R2_ADDR = (int)0x08;

    // General Purpose Register 3
    public static final int R3_ADDR = (int)0x0C;

    // General Purpose Register 4
    public static final int R4_ADDR = (int)0x10;

    // General Purpose Register 5
    public static final int R5_ADDR = (int)0x14;

    // General Purpose Register 6
    public static final int R6_ADDR = (int)0x18;

    // General Purpose Register 7
    public static final int R7_ADDR = (int)0x1C;

    // General Purpose Register 8
    public static final int R8_ADDR = (int)0x20;

    // General Purpose Register 9
    public static final int R9_ADDR = (int)0x24;

    // General Purpose Register 10
    public static final int R10_ADDR = (int)0x28;

    // General Purpose Register 11 (fp)
    public static final int R11_ADDR = (int)0x2C;

    // General Purpose Register 12
    public static final int R12_ADDR = (int)0x30;

    // Stack Pointer (R13)
    public static final int SP_ADDR = (int)0x34;

    // Link Register (R14)
    public static final int LR_ADDR = (int)0x38;

    // Program Counter (R15)
    public static final int PC_ADDR = (int)0x3C;

    // Processor Status Register
    public static final int PSR_ADDR = (int)0x40;
    public static final int PSR_MODE = 0;  // Mode bits (0-4)
    public static final int PSR_T = 5;  // Thumb state
    public static final int PSR_F = 6;  // FIQ disable
    public static final int PSR_I = 7;  // IRQ disable
    public static final int PSR_V = 28;  // Overflow
    public static final int PSR_C = 29;  // Carry
    public static final int PSR_Z = 30;  // Zero
    public static final int PSR_N = 31;  // Negative

    // 内存段定义
    // RISC OS ROM (512KB)
    public static final int ROM_START = (int)0x00000000;
    public static final int ROM_END = (int)0x0007FFFF;
    public static final int ROM_SIZE = 524288;

    // Main RAM (up to 4MB)
    public static final int RAM_START = (int)0x00080000;
    public static final int RAM_END = (int)0x003FFFFF;
    public static final int RAM_SIZE = 3932160;

    // Video RAM (4MB, VIDC)
    public static final int VRAM_START = (int)0x00400000;
    public static final int VRAM_END = (int)0x007FFFFF;
    public static final int VRAM_SIZE = 4194304;

    // I/O controller (IOC)
    public static final int IO_START = (int)0x03000000;
    public static final int IO_END = (int)0x0301FFFF;
    public static final int IO_SIZE = 131072;

    // Memory Controller (MEMC)
    public static final int MEMC_START = (int)0x03200000;
    public static final int MEMC_END = (int)0x0320FFFF;
    public static final int MEMC_SIZE = 4096;

    // Video Controller (VIDC)
    public static final int VIDC_START = (int)0x03400000;
    public static final int VIDC_END = (int)0x0340FFFF;
    public static final int VIDC_SIZE = 4096;

    // I/O and Memory DMA
    public static final int IOMD_START = (int)0x03300000;
    public static final int IOMD_END = (int)0x0330FFFF;
    public static final int IOMD_SIZE = 4096;

    // 外设定义
    // I/O Controller (IOC) - Interrupt/Keyboard/RTC
    public static final int IOC_BASE = (int)0x03000000;
    public static final int IOC_IOC_TIMER1 = (int)0x06000000;
    public static final int IOC_IOC_TIMER2 = (int)0x06000004;
    public static final int IOC_IOC_IOSEL = (int)0x06000008;
    public static final int IOC_IOC_IRQST = (int)0x0600000C;
    public static final int IOC_IOC_IRQLATCH = (int)0x06000010;
    public static final int IOC_IOC_FIQST = (int)0x06000014;
    public static final int IOC_IOC_FIQEN = (int)0x06000018;
    public static final int IOC_IOC_IRQEN = (int)0x0600001C;
    public static final int IOC_IOC_KBDDATA = (int)0x06000020;
    public static final int IOC_IOC_KBDCR = (int)0x06000024;
    public static final int IOC_IOC_RTCDR = (int)0x06000028;
    public static final int IOC_IOC_RTCCR = (int)0x0600002C;
    public static final int IOC_IOC_PRST = (int)0x06000030;
    public static final int IOC_IOC_PORTA = (int)0x06000034;
    public static final int IOC_IOC_PORTB = (int)0x06000038;
    public static final int IOC_IOC_PORTC = (int)0x0600003C;

    // Memory Controller (MEMC1)
    public static final int MEMC_BASE = (int)0x03200000;
    public static final int MEMC_MEMC_PT = (int)0x06400000;
    public static final int MEMC_MEMC_CTRL = (int)0x06400004;
    public static final int MEMC_MEMC_DRAM = (int)0x06400008;
    public static final int MEMC_MEMC_ERR = (int)0x0640000C;

    // Video Controller - VIDC1
    public static final int VIDC_BASE = (int)0x03400000;
    public static final int VIDC_VIDC_PALETTE = (int)0x06800000;
    public static final int VIDC_VIDC_STARTL = (int)0x06800004;
    public static final int VIDC_VIDC_STARTH = (int)0x06800008;
    public static final int VIDC_VIDC_CONFIG = (int)0x0680000C;
    public static final int VIDC_VIDC_HDISP = (int)0x06800010;
    public static final int VIDC_VIDC_VDISP = (int)0x06800014;
    public static final int VIDC_VIDC_HSYNC = (int)0x06800018;
    public static final int VIDC_VIDC_VSYNC = (int)0x0680001C;
    public static final int VIDC_VIDC_BORDER = (int)0x06800020;
    public static final int VIDC_VIDC_CURSOR = (int)0x06800024;
    public static final int VIDC_VIDC_SOUND = (int)0x06800028;

    // Intel 82710 Floppy Disk Controller
    public static final int FDC_BASE = (int)0x03010000;
    public static final int FDC_FDC_STATUS = (int)0x06020000;
    public static final int FDC_FDC_COMMAND = (int)0x06020000;
    public static final int FDC_FDC_TRACK = (int)0x06020004;
    public static final int FDC_FDC_SECTOR = (int)0x06020008;
    public static final int FDC_FDC_DATA = (int)0x0602000C;

    // Serial Port (via IOC)
    public static final int SERIAL_BASE = (int)0x03010010;
    public static final int SERIAL_SERIAL_TX = (int)0x06020020;
    public static final int SERIAL_SERIAL_RX = (int)0x06020024;
    public static final int SERIAL_SERIAL_CTRL = (int)0x06020028;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Reset
    public static final int IRQ_UND = 1;  // Undefined instruction
    public static final int IRQ_SWI = 2;  // Software Interrupt (SWI/SVC)
    public static final int IRQ_PABORT = 3;  // Prefetch Abort
    public static final int IRQ_DABORT = 4;  // Data Abort
    public static final int IRQ_ADDRESS = 5;  // Address Exception
    public static final int IRQ_IRQ = 6;  // IRQ interrupt (IOC)
    public static final int IRQ_FIQ = 7;  // FIQ interrupt (VIDC)

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // ARM clock (26MHz)
    public static final int PIN_NRESET = 4;  // Reset (active low)
    public static final int PIN_NMREQ = 5;  // Memory Request (active low)
    public static final int PIN_NIORQ = 6;  // I/O Request (active low)
    public static final int PIN_NRW = 7;  // Read/Write (0=write, 1=read)
    public static final int PIN_MAS0 = 8;  // Master address bit 0
    public static final int PIN_MAS1 = 9;  // Master address bit 1
    public static final int PIN_MAS2 = 10;  // Master address bit 2
    public static final int PIN_LOCK = 11;  // Bus lock
    public static final int PIN_NMREQ = 12;  // Memory request (active low)
    public static final int PIN_NWAIT = 13;  // Wait state (active low)
    public static final int PIN_NIRQLINE = 14;  // IRQ line (active low)
    public static final int PIN_NFIRQLINE = 15;  // FIQ line (active low)
    public static final int PIN_A1_A25 = 16;  // Address Bus (26-bit)
    public static final int PIN_D0_D31 = 17;  // Data Bus (32-bit)

    public static native void acorn_archimedes_a310_init();
}
