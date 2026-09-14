unit attiny13;

interface

// ATtiny13寄存器定义
// 生成自: Atmel/AVR/ATtiny13
// 版本: 1.0
// 日期: 2026-04-28
// 作者: VML Team
// 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 20000000 Hz

const

  // 寄存器定义
  R0 = 0x00;

  R1 = 0x01;

  R2 = 0x02;

  R16 = 0x10;

  R17 = 0x11;

  // XL
  R26 = 0x1A;

  // XH
  R27 = 0x1B;

  // YL
  R28 = 0x1C;

  // YH
  R29 = 0x1D;

  // ZL
  R30 = 0x1E;

  // ZH
  R31 = 0x1F;

  // Stack Pointer Low
  SPL = 0x5D;

  // Stack Pointer High
  SPH = 0x5E;

  // Status Register
  SREG = 0x5F;

  // 内存段定义
  FLASH_START = 0x0000;
  FLASH_END = 0x03FF;
  FLASH_SIZE = 1024;

  SRAM_START = 0x0060;
  SRAM_END = 0x009F;
  SRAM_SIZE = 64;

  EEPROM_START = 0x0000;
  EEPROM_END = 0x003F;
  EEPROM_SIZE = 64;

  IO_START = 0x00;
  IO_END = 0x1F;
  IO_SIZE = 32;

  EXTIO_START = 0x20;
  EXTIO_END = 0x5F;
  EXTIO_SIZE = 64;

  // 外设定义
  // Port B (only port)
  PORTB_BASE = 0x18;
  PORTB_DDRB = 0x17;
  PORTB_PORTB = 0x18;
  PORTB_PINB = 0x19;
  PORTB_PB0 = 0;  // Port B bit 0
  PORTB_PB1 = 1;  // Port B bit 1
  PORTB_PB2 = 2;  // Port B bit 2
  PORTB_PB3 = 3;  // Port B bit 3
  PORTB_PB4 = 4;  // Port B bit 4
  PORTB_PB5 = 5;  // Port B bit 5

  // 8-bit Timer/Counter0
  TIMER0_BASE = 0x33;
  TIMER0_TCCR0A = 0x33;
  TIMER0_TCCR0B = 0x33;
  TIMER0_TCNT0 = 0x32;
  TIMER0_OCR0A = 0x36;
  TIMER0_OCR0B = 0x35;
  TIMER0_TIMSK0 = 0x39;
  TIMER0_TIFR0 = 0x38;

  // Analog-to-Digital
  ADC_BASE = 0x04;
  ADC_ADMUX = 0x07;
  ADC_ADCSRA = 0x06;
  ADC_ADCL = 0x04;
  ADC_ADCH = 0x05;

  // 中断向量定义
  RESET_VECTOR = 1;  // 
  INT0_VECTOR = 2;  // External Interrupt 0
  PCINT0_VECTOR = 3;  // Pin Change Interrupt
  TIM0_OVF_VECTOR = 4;  // Timer0 Overflow
  TIM0_COMPA_VECTOR = 5;  // Timer0 Compare A
  WDT_VECTOR = 6;  // Watchdog Timeout
  ADC_VECTOR = 7;  // ADC Conversion Complete

type
  TATtiny13 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure attiny13_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure attiny13_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
