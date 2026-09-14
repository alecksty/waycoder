// ATtiny13 设备定义 - Dart 库
// 生成自: Atmel/AVR/ATtiny13
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 20000000 Hz

class ATtiny13Device {
  static const String deviceName = "ATtiny13";
  static const String manufacturer = "Atmel";
  static const String family = "AVR";
  static const String version = "1.0";
  static const String architecture = "AVR";
  static const int bits = 8;
  static const int clockFrequency = 20000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // 
  static const int R1_ADDR = 0x01;  // 
  static const int R2_ADDR = 0x02;  // 
  static const int R16_ADDR = 0x10;  // 
  static const int R17_ADDR = 0x11;  // 
  static const int R26_ADDR = 0x1A;  // XL
  static const int R27_ADDR = 0x1B;  // XH
  static const int R28_ADDR = 0x1C;  // YL
  static const int R29_ADDR = 0x1D;  // YH
  static const int R30_ADDR = 0x1E;  // ZL
  static const int R31_ADDR = 0x1F;  // ZH
  static const int SPL_ADDR = 0x5D;  // Stack Pointer Low
  static const int SPH_ADDR = 0x5E;  // Stack Pointer High
  static const int SREG_ADDR = 0x5F;  // Status Register

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x03FF;
  static const int FLASH_SIZE = 1024;  // 
  static const int SRAM_START = 0x0060;
  static const int SRAM_END = 0x009F;
  static const int SRAM_SIZE = 64;  // 
  static const int EEPROM_START = 0x0000;
  static const int EEPROM_END = 0x003F;
  static const int EEPROM_SIZE = 64;  // 
  static const int IO_START = 0x00;
  static const int IO_END = 0x1F;
  static const int IO_SIZE = 32;  // 
  static const int EXTIO_START = 0x20;
  static const int EXTIO_END = 0x5F;
  static const int EXTIO_SIZE = 64;  // 

  // 外设定义
  // Port B (only port)
  static const int PORTB_BASE = 0x18;
  static const int PORTB_DDRB_ADDR = 0x17;
  static const int PORTB_PORTB_ADDR = 0x18;
  static const int PORTB_PINB_ADDR = 0x19;
  // 8-bit Timer/Counter0
  static const int TIMER0_BASE = 0x33;
  static const int TIMER0_TCCR0A_ADDR = 0x33;
  static const int TIMER0_TCCR0B_ADDR = 0x33;
  static const int TIMER0_TCNT0_ADDR = 0x32;
  static const int TIMER0_OCR0A_ADDR = 0x36;
  static const int TIMER0_OCR0B_ADDR = 0x35;
  static const int TIMER0_TIMSK0_ADDR = 0x39;
  static const int TIMER0_TIFR0_ADDR = 0x38;
  // Analog-to-Digital
  static const int ADC_BASE = 0x04;
  static const int ADC_ADMUX_ADDR = 0x07;
  static const int ADC_ADCSRA_ADDR = 0x06;
  static const int ADC_ADCL_ADDR = 0x04;
  static const int ADC_ADCH_ADDR = 0x05;

  // 中断向量定义
  static const int INT_RESET = 1;  // 
  static const int INT_INT0 = 2;  // External Interrupt 0
  static const int INT_PCINT0 = 3;  // Pin Change Interrupt
  static const int INT_TIM0_OVF = 4;  // Timer0 Overflow
  static const int INT_TIM0_COMPA = 5;  // Timer0 Compare A
  static const int INT_WDT = 6;  // Watchdog Timeout
  static const int INT_ADC = 7;  // ADC Conversion Complete

}
