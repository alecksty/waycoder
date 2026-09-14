package vml.device.intel._8051;

/**
 * 8051 寄存器定义
 * 生成自: Intel/MCS-51/8051
 * 版本: 1.0
 */
public final class 8051 {
    private 8051() {} // 工具类
    // CPU架构: MCS-51, 8位, 11059200 Hz

    // 寄存器定义
    // Accumulator
    public static final int ACC_ADDR = (int)0xE0;

    // B Register
    public static final int B_ADDR = (int)0xF0;

    // Program Status Word
    public static final int PSW_ADDR = (int)0xD0;
    public static final int PSW_P = 0;  // Parity Flag
    public static final int PSW_OV = 2;  // Overflow Flag
    public static final int PSW_RS0 = 3;  // Register Bank Select 0
    public static final int PSW_RS1 = 4;  // Register Bank Select 1
    public static final int PSW_F0 = 5;  // Flag 0
    public static final int PSW_AC = 6;  // Auxiliary Carry Flag
    public static final int PSW_CY = 7;  // Carry Flag

    // Stack Pointer
    public static final int SP_ADDR = (int)0x81;

    // Data Pointer (DPL/DPH)
    public static final int DPTR_ADDR = (int)0x82;

    // 内存段定义
    // Program Memory
    public static final int CODE_START = (int)0x0000;
    public static final int CODE_END = (int)0x0FFF;
    public static final int CODE_SIZE = 4096;

    // Internal Data Memory
    public static final int IDATA_START = (int)0x00;
    public static final int IDATA_END = (int)0x7F;
    public static final int IDATA_SIZE = 128;

    // Special Function Registers
    public static final int SFR_START = (int)0x80;
    public static final int SFR_END = (int)0xFF;
    public static final int SFR_SIZE = 128;

    // External Data Memory
    public static final int XDATA_START = (int)0x0000;
    public static final int XDATA_END = (int)0xFFFF;
    public static final int XDATA_SIZE = 65536;

    // 外设定义
    // Port 0
    public static final int PORT0_BASE = (int)0x80;
    public static final int PORT0_P0 = (int)0x00000100;

    // Port 1
    public static final int PORT1_BASE = (int)0x90;
    public static final int PORT1_P1 = (int)0x00000120;

    // Port 2
    public static final int PORT2_BASE = (int)0xA0;
    public static final int PORT2_P2 = (int)0x00000140;

    // Port 3
    public static final int PORT3_BASE = (int)0xB0;
    public static final int PORT3_P3 = (int)0x00000160;

    // Timer/Counter 0
    public static final int TIMER0_BASE = (int)0x8A;
    public static final int TIMER0_TH0 = (int)0x00000116;
    public static final int TIMER0_TL0 = (int)0x00000114;
    public static final int TIMER0_TMOD = (int)0x00000113;
    public static final int TIMER0_TMOD_M0_0 = 0;  // Timer 0 Mode bit 0
    public static final int TIMER0_TMOD_M1_0 = 1;  // Timer 0 Mode bit 1
    public static final int TIMER0_TMOD_C_T0 = 2;  // Timer 0 Counter/Timer Select
    public static final int TIMER0_TMOD_GATE0 = 3;  // Timer 0 Gate Control
    public static final int TIMER0_TCON = (int)0x00000112;
    public static final int TIMER0_TCON_TR0 = 4;  // Timer 0 Run Control
    public static final int TIMER0_TCON_TF0 = 5;  // Timer 0 Overflow Flag

    // Serial Port
    public static final int UART_BASE = (int)0x98;
    public static final int UART_SBUF = (int)0x00000131;
    public static final int UART_SCON = (int)0x00000130;
    public static final int UART_SCON_RI = 0;  // Receive Interrupt Flag
    public static final int UART_SCON_TI = 1;  // Transmit Interrupt Flag
    public static final int UART_SCON_REN = 4;  // Receive Enable
    public static final int UART_SCON_SM0 = 6;  // Serial Mode bit 0
    public static final int UART_SCON_SM1 = 7;  // Serial Mode bit 1

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Reset Vector
    public static final int IRQ_INT0 = 1;  // External Interrupt 0
    public static final int IRQ_TIMER0 = 2;  // Timer 0 Interrupt
    public static final int IRQ_INT1 = 3;  // External Interrupt 1
    public static final int IRQ_TIMER1 = 4;  // Timer 1 Interrupt
    public static final int IRQ_UART = 5;  // Serial Port Interrupt

    // 引脚定义
    public static final int PIN_P1_0 = 1;  // Port 1, bit 0
    public static final int PIN_P1_1 = 2;  // Port 1, bit 1
    public static final int PIN_P1_2 = 3;  // Port 1, bit 2
    public static final int PIN_P1_3 = 4;  // Port 1, bit 3
    public static final int PIN_P1_4 = 5;  // Port 1, bit 4
    public static final int PIN_P1_5 = 6;  // Port 1, bit 5
    public static final int PIN_P1_6 = 7;  // Port 1, bit 6
    public static final int PIN_P1_7 = 8;  // Port 1, bit 7
    public static final int PIN_RST = 9;  // Reset Pin
    public static final int PIN_RX = 10;  // Serial Receive (P3.0)
    public static final int PIN_TX = 11;  // Serial Transmit (P3.1)
    public static final int PIN_INT0 = 12;  // External Interrupt 0 (P3.2)
    public static final int PIN_INT1 = 13;  // External Interrupt 1 (P3.3)
    public static final int PIN_T0 = 14;  // Timer 0 Input (P3.4)
    public static final int PIN_T1 = 15;  // Timer 1 Input (P3.5)
    public static final int PIN_WR = 16;  // External Memory Write Strobe (P3.6)
    public static final int PIN_RD = 17;  // External Memory Read Strobe (P3.7)
    public static final int PIN_XTAL1 = 18;  // Crystal Oscillator Input
    public static final int PIN_XTAL2 = 19;  // Crystal Oscillator Output
    public static final int PIN_VCC = 20;  // Power Supply (+5V)
    public static final int PIN_GND = 21;  // Ground

    public static native void _8051_init();
}
