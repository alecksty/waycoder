// 8051 设备定义 - Dart 库
// 生成自: Intel/MCS-51/8051
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
// CPU架构: MCS-51
// 位宽: 8位
// 时钟频率: 11059200 Hz

class 8051Device {
  static const String deviceName = "8051";
  static const String manufacturer = "Intel";
  static const String family = "MCS-51";
  static const String version = "1.0";
  static const String architecture = "MCS-51";
  static const int bits = 8;
  static const int clockFrequency = 11059200;

  // 寄存器地址定义
  static const int ACC_ADDR = 0xE0;  // Accumulator
  static const int B_ADDR = 0xF0;  // B Register
  static const int PSW_ADDR = 0xD0;  // Program Status Word
  static const int PSW_P_BIT = 0;  // Parity Flag
  static const int PSW_OV_BIT = 2;  // Overflow Flag
  static const int PSW_RS0_BIT = 3;  // Register Bank Select 0
  static const int PSW_RS1_BIT = 4;  // Register Bank Select 1
  static const int PSW_F0_BIT = 5;  // Flag 0
  static const int PSW_AC_BIT = 6;  // Auxiliary Carry Flag
  static const int PSW_CY_BIT = 7;  // Carry Flag
  static const int SP_ADDR = 0x81;  // Stack Pointer
  static const int DPTR_ADDR = 0x82;  // Data Pointer (DPL/DPH)

  // 内存段定义
  static const int CODE_START = 0x0000;
  static const int CODE_END = 0x0FFF;
  static const int CODE_SIZE = 4096;  // Program Memory
  static const int IDATA_START = 0x00;
  static const int IDATA_END = 0x7F;
  static const int IDATA_SIZE = 128;  // Internal Data Memory
  static const int SFR_START = 0x80;
  static const int SFR_END = 0xFF;
  static const int SFR_SIZE = 128;  // Special Function Registers
  static const int XDATA_START = 0x0000;
  static const int XDATA_END = 0xFFFF;
  static const int XDATA_SIZE = 65536;  // External Data Memory

  // 外设定义
  // Port 0
  static const int PORT0_BASE = 0x80;
  static const int PORT0_P0_ADDR = 0x80;
  // Port 1
  static const int PORT1_BASE = 0x90;
  static const int PORT1_P1_ADDR = 0x90;
  // Port 2
  static const int PORT2_BASE = 0xA0;
  static const int PORT2_P2_ADDR = 0xA0;
  // Port 3
  static const int PORT3_BASE = 0xB0;
  static const int PORT3_P3_ADDR = 0xB0;
  // Timer/Counter 0
  static const int TIMER0_BASE = 0x8A;
  static const int TIMER0_TH0_ADDR = 0x8C;
  static const int TIMER0_TL0_ADDR = 0x8A;
  static const int TIMER0_TMOD_ADDR = 0x89;
  static const int TIMER0_TMOD_M0_0_BIT = 0;  // Timer 0 Mode bit 0
  static const int TIMER0_TMOD_M1_0_BIT = 1;  // Timer 0 Mode bit 1
  static const int TIMER0_TMOD_C_T0_BIT = 2;  // Timer 0 Counter/Timer Select
  static const int TIMER0_TMOD_GATE0_BIT = 3;  // Timer 0 Gate Control
  static const int TIMER0_TCON_ADDR = 0x88;
  static const int TIMER0_TCON_TR0_BIT = 4;  // Timer 0 Run Control
  static const int TIMER0_TCON_TF0_BIT = 5;  // Timer 0 Overflow Flag
  // Serial Port
  static const int UART_BASE = 0x98;
  static const int UART_SBUF_ADDR = 0x99;
  static const int UART_SCON_ADDR = 0x98;
  static const int UART_SCON_RI_BIT = 0;  // Receive Interrupt Flag
  static const int UART_SCON_TI_BIT = 1;  // Transmit Interrupt Flag
  static const int UART_SCON_REN_BIT = 4;  // Receive Enable
  static const int UART_SCON_SM0_BIT = 6;  // Serial Mode bit 0
  static const int UART_SCON_SM1_BIT = 7;  // Serial Mode bit 1

  // 中断向量定义
  static const int INT_RESET = 0;  // Reset Vector
  static const int INT_INT0 = 1;  // External Interrupt 0
  static const int INT_TIMER0 = 2;  // Timer 0 Interrupt
  static const int INT_INT1 = 3;  // External Interrupt 1
  static const int INT_TIMER1 = 4;  // Timer 1 Interrupt
  static const int INT_UART = 5;  // Serial Port Interrupt

  // 引脚定义
  static const int PIN_P1_0 = 1;  // Port 1, bit 0
  static const int PIN_P1_1 = 2;  // Port 1, bit 1
  static const int PIN_P1_2 = 3;  // Port 1, bit 2
  static const int PIN_P1_3 = 4;  // Port 1, bit 3
  static const int PIN_P1_4 = 5;  // Port 1, bit 4
  static const int PIN_P1_5 = 6;  // Port 1, bit 5
  static const int PIN_P1_6 = 7;  // Port 1, bit 6
  static const int PIN_P1_7 = 8;  // Port 1, bit 7
  static const int PIN_RST = 9;  // Reset Pin
  static const int PIN_RX = 10;  // Serial Receive (P3.0)
  static const int PIN_TX = 11;  // Serial Transmit (P3.1)
  static const int PIN_INT0 = 12;  // External Interrupt 0 (P3.2)
  static const int PIN_INT1 = 13;  // External Interrupt 1 (P3.3)
  static const int PIN_T0 = 14;  // Timer 0 Input (P3.4)
  static const int PIN_T1 = 15;  // Timer 1 Input (P3.5)
  static const int PIN_WR = 16;  // External Memory Write Strobe (P3.6)
  static const int PIN_RD = 17;  // External Memory Read Strobe (P3.7)
  static const int PIN_XTAL1 = 18;  // Crystal Oscillator Input
  static const int PIN_XTAL2 = 19;  // Crystal Oscillator Output
  static const int PIN_VCC = 20;  // Power Supply (+5V)
  static const int PIN_GND = 21;  // Ground

}
