/**
 * ATtiny13 寄存器定义
 * 生成自: Atmel/AVR/ATtiny13
 * 版本: 1.0
 */
export const attiny13 = {
  // CPU: AVR, 8位, 20000000 Hz

  // 寄存器定义
  R0: 0x00,
  R1: 0x01,
  R2: 0x02,
  R16: 0x10,
  R17: 0x11,
  // XL
  R26: 0x1A,
  // XH
  R27: 0x1B,
  // YL
  R28: 0x1C,
  // YH
  R29: 0x1D,
  // ZL
  R30: 0x1E,
  // ZH
  R31: 0x1F,
  // Stack Pointer Low
  SPL: 0x5D,
  // Stack Pointer High
  SPH: 0x5E,
  // Status Register
  SREG: 0x5F,

  // 内存段
  FLASH_START: 0x0000,
  FLASH_END: 0x03FF,
  FLASH_SIZE: 1024,
  SRAM_START: 0x0060,
  SRAM_END: 0x009F,
  SRAM_SIZE: 64,
  EEPROM_START: 0x0000,
  EEPROM_END: 0x003F,
  EEPROM_SIZE: 64,
  IO_START: 0x00,
  IO_END: 0x1F,
  IO_SIZE: 32,
  EXTIO_START: 0x20,
  EXTIO_END: 0x5F,
  EXTIO_SIZE: 64,

  // 外设定义
  // Port B (only port)
  PORTB_BASE: 0x18,
  PORTB_DDRB: 0x0000002F,
  PORTB_PORTB: 0x00000030,
  PORTB_PINB: 0x00000031,
  PORTB_PB0: 0,  // Port B bit 0
  PORTB_PB1: 1,  // Port B bit 1
  PORTB_PB2: 2,  // Port B bit 2
  PORTB_PB3: 3,  // Port B bit 3
  PORTB_PB4: 4,  // Port B bit 4
  PORTB_PB5: 5,  // Port B bit 5
  // 8-bit Timer/Counter0
  TIMER0_BASE: 0x33,
  TIMER0_TCCR0A: 0x00000066,
  TIMER0_TCCR0B: 0x00000066,
  TIMER0_TCNT0: 0x00000065,
  TIMER0_OCR0A: 0x00000069,
  TIMER0_OCR0B: 0x00000068,
  TIMER0_TIMSK0: 0x0000006C,
  TIMER0_TIFR0: 0x0000006B,
  // Analog-to-Digital
  ADC_BASE: 0x04,
  ADC_ADMUX: 0x0000000B,
  ADC_ADCSRA: 0x0000000A,
  ADC_ADCL: 0x00000008,
  ADC_ADCH: 0x00000009,

  // 中断向量
  IRQ_Reset: 1,  // 
  IRQ_INT0: 2,  // External Interrupt 0
  IRQ_PCINT0: 3,  // Pin Change Interrupt
  IRQ_TIM0_OVF: 4,  // Timer0 Overflow
  IRQ_TIM0_COMPA: 5,  // Timer0 Compare A
  IRQ_WDT: 6,  // Watchdog Timeout
  IRQ_ADC: 7,  // ADC Conversion Complete

  init: function() {
    // 硬件初始化
  }
};
