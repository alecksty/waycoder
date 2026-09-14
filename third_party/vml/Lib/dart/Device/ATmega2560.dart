// ATmega2560 设备定义 - Dart 库
// 生成自: Atmel/AVR/ATmega2560
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

class ATmega2560Device {
  static const String deviceName = "ATmega2560";
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
  static const int SPL_ADDR = 0x5D;  // 
  static const int SPH_ADDR = 0x5E;  // 
  static const int SREG_ADDR = 0x5F;  // 

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x3FFFF;
  static const int FLASH_SIZE = 262144;  // 
  static const int SRAM_START = 0x0200;
  static const int SRAM_END = 0x21FF;
  static const int SRAM_SIZE = 8192;  // 
  static const int EEPROM_START = 0x0000;
  static const int EEPROM_END = 0x0FFF;
  static const int EEPROM_SIZE = 4096;  // 
  static const int IO_START = 0x00;
  static const int IO_END = 0x3F;
  static const int IO_SIZE = 64;  // 
  static const int EXTIO_START = 0x40;
  static const int EXTIO_END = 0xFF;
  static const int EXTIO_SIZE = 192;  // 

  // 外设定义
  // Port A
  static const int PORTA_BASE = 0x22;
  static const int PORTA_DDRA_ADDR = 0x21;
  static const int PORTA_PORTA_ADDR = 0x22;
  static const int PORTA_PINA_ADDR = 0x20;
  // Port B
  static const int PORTB_BASE = 0x25;
  static const int PORTB_DDRB_ADDR = 0x24;
  static const int PORTB_PORTB_ADDR = 0x25;
  static const int PORTB_PINB_ADDR = 0x23;
  // Port C
  static const int PORTC_BASE = 0x28;
  static const int PORTC_DDRC_ADDR = 0x27;
  static const int PORTC_PORTC_ADDR = 0x28;
  static const int PORTC_PINC_ADDR = 0x26;
  // Port D
  static const int PORTD_BASE = 0x2B;
  static const int PORTD_DDRD_ADDR = 0x2A;
  static const int PORTD_PORTD_ADDR = 0x2B;
  static const int PORTD_PIND_ADDR = 0x29;
  // Port E
  static const int PORTE_BASE = 0x2E;
  static const int PORTE_DDRE_ADDR = 0x2D;
  static const int PORTE_PORTE_ADDR = 0x2E;
  static const int PORTE_PINE_ADDR = 0x2C;
  // Port F
  static const int PORTF_BASE = 0x31;
  static const int PORTF_DDRF_ADDR = 0x30;
  static const int PORTF_PORTF_ADDR = 0x31;
  static const int PORTF_PINF_ADDR = 0x2F;
  // Port G
  static const int PORTG_BASE = 0x34;
  static const int PORTG_DDRG_ADDR = 0x33;
  static const int PORTG_PORTG_ADDR = 0x34;
  static const int PORTG_PING_ADDR = 0x32;
  // USART 0
  static const int USART0_BASE = 0xC0;
  static const int USART0_UDR0_ADDR = 0xC6;
  static const int USART0_UCSR0A_ADDR = 0xC0;
  static const int USART0_UCSR0B_ADDR = 0xC1;
  static const int USART0_UCSR0C_ADDR = 0xC2;
  static const int USART0_UBRR0L_ADDR = 0xC4;
  static const int USART0_UBRR0H_ADDR = 0xC5;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_INT0 = 2;  // 
  static const int INT_INT1 = 3;  // 
  static const int INT_INT2 = 4;  // 
  static const int INT_INT3 = 5;  // 
  static const int INT_INT4 = 6;  // 
  static const int INT_INT5 = 7;  // 
  static const int INT_INT6 = 8;  // 
  static const int INT_INT7 = 9;  // 
  static const int INT_PCINT0 = 10;  // 
  static const int INT_PCINT1 = 11;  // 
  static const int INT_PCINT2 = 12;  // 
  static const int INT_WDT = 13;  // 
  static const int INT_TIM2_COMPA = 14;  // 
  static const int INT_TIM2_COMPB = 15;  // 
  static const int INT_TIM2_OVF = 16;  // 
  static const int INT_TIM1_CAPT = 17;  // 
  static const int INT_TIM1_COMPA = 18;  // 
  static const int INT_TIM1_COMPB = 19;  // 
  static const int INT_TIM1_OVF = 20;  // 
  static const int INT_TIM0_COMPA = 21;  // 
  static const int INT_TIM0_COMPB = 22;  // 
  static const int INT_TIM0_OVF = 23;  // 
  static const int INT_SPI_STC = 24;  // 
  static const int INT_USART0_RX = 25;  // 
  static const int INT_USART0_UDRE = 26;  // 
  static const int INT_USART0_TX = 27;  // 

}
