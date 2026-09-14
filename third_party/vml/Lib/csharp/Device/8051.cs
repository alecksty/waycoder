using System;

namespace VML.Device.Intel.8051
{
    /// <summary>
    /// 8051 寄存器定义
    /// 生成自: Intel/MCS-51/8051
    /// 版本: 1.0
    /// </summary>
    public static class 8051
    {
        // CPU架构: MCS-51, 8位, 11059200 Hz

        // 寄存器定义
        // Accumulator
        public const int ACC_ADDR = 0xE0;
        public static unsafe byte* ACC => (byte*)0xE0;

        // B Register
        public const int B_ADDR = 0xF0;
        public static unsafe byte* B => (byte*)0xF0;

        // Program Status Word
        public const int PSW_ADDR = 0xD0;
        public static unsafe byte* PSW => (byte*)0xD0;
        public const int PSW_P = 0;  // Parity Flag
        public const int PSW_OV = 2;  // Overflow Flag
        public const int PSW_RS0 = 3;  // Register Bank Select 0
        public const int PSW_RS1 = 4;  // Register Bank Select 1
        public const int PSW_F0 = 5;  // Flag 0
        public const int PSW_AC = 6;  // Auxiliary Carry Flag
        public const int PSW_CY = 7;  // Carry Flag

        // Stack Pointer
        public const int SP_ADDR = 0x81;
        public static unsafe byte* SP => (byte*)0x81;

        // Data Pointer (DPL/DPH)
        public const int DPTR_ADDR = 0x82;
        public static unsafe ushort* DPTR => (ushort*)0x82;

        // 内存段定义
        // Program Memory
        public const int CODE_START = 0x0000;
        public const int CODE_END = 0x0FFF;
        public const int CODE_SIZE = 4096;

        // Internal Data Memory
        public const int IDATA_START = 0x00;
        public const int IDATA_END = 0x7F;
        public const int IDATA_SIZE = 128;

        // Special Function Registers
        public const int SFR_START = 0x80;
        public const int SFR_END = 0xFF;
        public const int SFR_SIZE = 128;

        // External Data Memory
        public const int XDATA_START = 0x0000;
        public const int XDATA_END = 0xFFFF;
        public const int XDATA_SIZE = 65536;

        // 外设定义
        // Port 0
        public const int PORT0_BASE = 0x80;
        public static unsafe byte* PORT0_P0 => (byte*)0x00000100;

        // Port 1
        public const int PORT1_BASE = 0x90;
        public static unsafe byte* PORT1_P1 => (byte*)0x00000120;

        // Port 2
        public const int PORT2_BASE = 0xA0;
        public static unsafe byte* PORT2_P2 => (byte*)0x00000140;

        // Port 3
        public const int PORT3_BASE = 0xB0;
        public static unsafe byte* PORT3_P3 => (byte*)0x00000160;

        // Timer/Counter 0
        public const int TIMER0_BASE = 0x8A;
        public static unsafe byte* TIMER0_TH0 => (byte*)0x00000116;
        public static unsafe byte* TIMER0_TL0 => (byte*)0x00000114;
        public static unsafe byte* TIMER0_TMOD => (byte*)0x00000113;
        public const int TIMER0_TMOD_M0_0 = 0;  // Timer 0 Mode bit 0
        public const int TIMER0_TMOD_M1_0 = 1;  // Timer 0 Mode bit 1
        public const int TIMER0_TMOD_C_T0 = 2;  // Timer 0 Counter/Timer Select
        public const int TIMER0_TMOD_GATE0 = 3;  // Timer 0 Gate Control
        public static unsafe byte* TIMER0_TCON => (byte*)0x00000112;
        public const int TIMER0_TCON_TR0 = 4;  // Timer 0 Run Control
        public const int TIMER0_TCON_TF0 = 5;  // Timer 0 Overflow Flag

        // Serial Port
        public const int UART_BASE = 0x98;
        public static unsafe byte* UART_SBUF => (byte*)0x00000131;
        public static unsafe byte* UART_SCON => (byte*)0x00000130;
        public const int UART_SCON_RI = 0;  // Receive Interrupt Flag
        public const int UART_SCON_TI = 1;  // Transmit Interrupt Flag
        public const int UART_SCON_REN = 4;  // Receive Enable
        public const int UART_SCON_SM0 = 6;  // Serial Mode bit 0
        public const int UART_SCON_SM1 = 7;  // Serial Mode bit 1

        // 中断向量定义
        public const int IRQ_RESET = 0;  // Reset Vector
        public const int IRQ_INT0 = 1;  // External Interrupt 0
        public const int IRQ_TIMER0 = 2;  // Timer 0 Interrupt
        public const int IRQ_INT1 = 3;  // External Interrupt 1
        public const int IRQ_TIMER1 = 4;  // Timer 1 Interrupt
        public const int IRQ_UART = 5;  // Serial Port Interrupt

        // 引脚定义
        public const int PIN_P1_0 = 1;  // Port 1, bit 0
        public const int PIN_P1_1 = 2;  // Port 1, bit 1
        public const int PIN_P1_2 = 3;  // Port 1, bit 2
        public const int PIN_P1_3 = 4;  // Port 1, bit 3
        public const int PIN_P1_4 = 5;  // Port 1, bit 4
        public const int PIN_P1_5 = 6;  // Port 1, bit 5
        public const int PIN_P1_6 = 7;  // Port 1, bit 6
        public const int PIN_P1_7 = 8;  // Port 1, bit 7
        public const int PIN_RST = 9;  // Reset Pin
        public const int PIN_RX = 10;  // Serial Receive (P3.0)
        public const int PIN_TX = 11;  // Serial Transmit (P3.1)
        public const int PIN_INT0 = 12;  // External Interrupt 0 (P3.2)
        public const int PIN_INT1 = 13;  // External Interrupt 1 (P3.3)
        public const int PIN_T0 = 14;  // Timer 0 Input (P3.4)
        public const int PIN_T1 = 15;  // Timer 1 Input (P3.5)
        public const int PIN_WR = 16;  // External Memory Write Strobe (P3.6)
        public const int PIN_RD = 17;  // External Memory Read Strobe (P3.7)
        public const int PIN_XTAL1 = 18;  // Crystal Oscillator Input
        public const int PIN_XTAL2 = 19;  // Crystal Oscillator Output
        public const int PIN_VCC = 20;  // Power Supply (+5V)
        public const int PIN_GND = 21;  // Ground

        public static void _8051_init()
        {
            // 硬件初始化代码
        }
    }
}
