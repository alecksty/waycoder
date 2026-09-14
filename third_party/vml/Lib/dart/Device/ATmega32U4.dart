// ATmega32U4 设备定义 - Dart 库
// 生成自: Atmel/AVR/ATmega32U4
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

class ATmega32U4Device {
  static const String deviceName = "ATmega32U4";
  static const String manufacturer = "Atmel";
  static const String family = "AVR";
  static const String version = "1.0";
  static const String architecture = "AVR";
  static const int bits = 8;
  static const int clockFrequency = 16000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x01;  // 
  static const int R2_ADDR = 0x02;  // 
  static const int R3_ADDR = 0x03;  // 
  static const int R4_ADDR = 0x04;  // 
  static const int R5_ADDR = 0x05;  // 
  static const int R6_ADDR = 0x06;  // 
  static const int R7_ADDR = 0x07;  // 
  static const int R8_ADDR = 0x08;  // 
  static const int R9_ADDR = 0x09;  // 
  static const int R10_ADDR = 0x0A;  // 
  static const int R11_ADDR = 0x0B;  // 
  static const int R12_ADDR = 0x0C;  // 
  static const int R13_ADDR = 0x0D;  // 
  static const int R14_ADDR = 0x0E;  // 
  static const int R15_ADDR = 0x0F;  // 
  static const int R16_ADDR = 0x10;  // 
  static const int R17_ADDR = 0x11;  // 
  static const int R18_ADDR = 0x12;  // 
  static const int R19_ADDR = 0x13;  // 
  static const int R20_ADDR = 0x14;  // 
  static const int R21_ADDR = 0x15;  // 
  static const int R22_ADDR = 0x16;  // 
  static const int R23_ADDR = 0x17;  // 
  static const int R24_ADDR = 0x18;  // 
  static const int R25_ADDR = 0x19;  // 
  static const int R26_ADDR = 0x1A;  // 
  static const int R27_ADDR = 0x1B;  // 
  static const int R28_ADDR = 0x1C;  // 
  static const int R29_ADDR = 0x1D;  // 
  static const int R30_ADDR = 0x1E;  // 
  static const int R31_ADDR = 0x1F;  // 
  static const int SPL_ADDR = 0x5D;  // 
  static const int SPH_ADDR = 0x5E;  // 
  static const int SREG_ADDR = 0x5F;  // 

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x7FFF;
  static const int FLASH_SIZE = 32768;  // Program Flash Memory
  static const int SRAM_START = 0x0100;
  static const int SRAM_END = 0x0AFF;
  static const int SRAM_SIZE = 2560;  // Static RAM
  static const int EEPROM_START = 0x0000;
  static const int EEPROM_END = 0x03FF;
  static const int EEPROM_SIZE = 1024;  // EEPROM
  static const int IO_START = 0x00;
  static const int IO_END = 0x3F;
  static const int IO_SIZE = 64;  // I/O Registers
  static const int EXTIO_START = 0x40;
  static const int EXTIO_END = 0xFF;
  static const int EXTIO_SIZE = 192;  // Extended I/O Registers

  // 外设定义
  // Port B
  static const int PORTB_BASE = 0x23;
  static const int PORTB_PORTB_ADDR = 0x25;
  static const int PORTB_DDRB_ADDR = 0x24;
  static const int PORTB_PINB_ADDR = 0x23;
  // Port C
  static const int PORTC_BASE = 0x26;
  static const int PORTC_PORTC_ADDR = 0x28;
  static const int PORTC_DDRC_ADDR = 0x27;
  static const int PORTC_PINC_ADDR = 0x26;
  // Port D
  static const int PORTD_BASE = 0x29;
  static const int PORTD_PORTD_ADDR = 0x2B;
  static const int PORTD_DDRD_ADDR = 0x2A;
  static const int PORTD_PIND_ADDR = 0x29;
  // Port E
  static const int PORTE_BASE = 0x2C;
  static const int PORTE_PORTE_ADDR = 0x2E;
  static const int PORTE_DDRE_ADDR = 0x2D;
  static const int PORTE_PINE_ADDR = 0x2C;
  // USART1
  static const int UART1_BASE = 0xC8;
  static const int UART1_UDR1_ADDR = 0xCE;
  static const int UART1_UCSR1A_ADDR = 0xC8;
  static const int UART1_UCSR1B_ADDR = 0xC9;
  static const int UART1_UCSR1C_ADDR = 0xCA;
  static const int UART1_UBRR1_ADDR = 0xCC;
  // USB Controller
  static const int USB_BASE = 0xD0;
  static const int USB_UDCON_ADDR = 0xD0;
  static const int USB_UDIEN_ADDR = 0xD1;
  static const int USB_UDINT_ADDR = 0xD2;

  // 中断向量定义
  static const int INT_INT0 = 1;  // External Interrupt 0
  static const int INT_INT1 = 2;  // External Interrupt 1
  static const int INT_INT2 = 3;  // External Interrupt 2
  static const int INT_INT3 = 4;  // External Interrupt 3
  static const int INT_INT4 = 5;  // External Interrupt 4
  static const int INT_INT5 = 6;  // External Interrupt 5
  static const int INT_INT6 = 7;  // External Interrupt 6
  static const int INT_PCINT0 = 8;  // Pin Change Interrupt 0
  static const int INT_USB_GENERAL = 9;  // USB General
  static const int INT_USB_ENDPOINT = 10;  // USB Endpoint
  static const int INT_WDT = 11;  // Watchdog Timeout
  static const int INT_TIMER1_CAPT = 12;  // Timer1 Capture
  static const int INT_TIMER1_COMPA = 13;  // Timer1 Compare A
  static const int INT_TIMER1_COMPB = 14;  // Timer1 Compare B
  static const int INT_TIMER1_OVF = 15;  // Timer1 Overflow
  static const int INT_TIMER0_COMPA = 16;  // Timer0 Compare A
  static const int INT_TIMER0_COMPB = 17;  // Timer0 Compare B
  static const int INT_TIMER0_OVF = 18;  // Timer0 Overflow
  static const int INT_SPI_STC = 19;  // SPI Transfer Complete
  static const int INT_UART1_RX = 20;  // UART1 Receive
  static const int INT_UART1_UDRE = 21;  // UART1 Data Register Empty
  static const int INT_UART1_TX = 22;  // UART1 Transmit
  static const int INT_ADC = 23;  // ADC Conversion Complete

}
