/**
 * ATmega32U4 寄存器定义
 * 生成自: Atmel/AVR/ATmega32U4
 * 版本: 1.0
 */
export const atmega32u4 = {
  // CPU: AVR, 8位, 16000000 Hz

  // 寄存器定义
  R0: 0x00,
  R1: 0x01,
  R2: 0x02,
  R3: 0x03,
  R4: 0x04,
  R5: 0x05,
  R6: 0x06,
  R7: 0x07,
  R8: 0x08,
  R9: 0x09,
  R10: 0x0A,
  R11: 0x0B,
  R12: 0x0C,
  R13: 0x0D,
  R14: 0x0E,
  R15: 0x0F,
  R16: 0x10,
  R17: 0x11,
  R18: 0x12,
  R19: 0x13,
  R20: 0x14,
  R21: 0x15,
  R22: 0x16,
  R23: 0x17,
  R24: 0x18,
  R25: 0x19,
  R26: 0x1A,
  R27: 0x1B,
  R28: 0x1C,
  R29: 0x1D,
  R30: 0x1E,
  R31: 0x1F,
  SPL: 0x5D,
  SPH: 0x5E,
  SREG: 0x5F,

  // 内存段
  // Program Flash Memory
  FLASH_START: 0x0000,
  FLASH_END: 0x7FFF,
  FLASH_SIZE: 32768,
  // Static RAM
  SRAM_START: 0x0100,
  SRAM_END: 0x0AFF,
  SRAM_SIZE: 2560,
  // EEPROM
  EEPROM_START: 0x0000,
  EEPROM_END: 0x03FF,
  EEPROM_SIZE: 1024,
  // I/O Registers
  IO_START: 0x00,
  IO_END: 0x3F,
  IO_SIZE: 64,
  // Extended I/O Registers
  EXTIO_START: 0x40,
  EXTIO_END: 0xFF,
  EXTIO_SIZE: 192,

  // 外设定义
  // Port B
  PORTB_BASE: 0x23,
  PORTB_PORTB: 0x00000048,
  PORTB_DDRB: 0x00000047,
  PORTB_PINB: 0x00000046,
  // Port C
  PORTC_BASE: 0x26,
  PORTC_PORTC: 0x0000004E,
  PORTC_DDRC: 0x0000004D,
  PORTC_PINC: 0x0000004C,
  // Port D
  PORTD_BASE: 0x29,
  PORTD_PORTD: 0x00000054,
  PORTD_DDRD: 0x00000053,
  PORTD_PIND: 0x00000052,
  // Port E
  PORTE_BASE: 0x2C,
  PORTE_PORTE: 0x0000005A,
  PORTE_DDRE: 0x00000059,
  PORTE_PINE: 0x00000058,
  // USART1
  UART1_BASE: 0xC8,
  UART1_UDR1: 0x00000196,
  UART1_UCSR1A: 0x00000190,
  UART1_UCSR1B: 0x00000191,
  UART1_UCSR1C: 0x00000192,
  UART1_UBRR1: 0x00000194,
  // USB Controller
  USB_BASE: 0xD0,
  USB_UDCON: 0x000001A0,
  USB_UDIEN: 0x000001A1,
  USB_UDINT: 0x000001A2,

  // 中断向量
  IRQ_INT0: 1,  // External Interrupt 0
  IRQ_INT1: 2,  // External Interrupt 1
  IRQ_INT2: 3,  // External Interrupt 2
  IRQ_INT3: 4,  // External Interrupt 3
  IRQ_INT4: 5,  // External Interrupt 4
  IRQ_INT5: 6,  // External Interrupt 5
  IRQ_INT6: 7,  // External Interrupt 6
  IRQ_PCINT0: 8,  // Pin Change Interrupt 0
  IRQ_USB_General: 9,  // USB General
  IRQ_USB_Endpoint: 10,  // USB Endpoint
  IRQ_WDT: 11,  // Watchdog Timeout
  IRQ_TIMER1_CAPT: 12,  // Timer1 Capture
  IRQ_TIMER1_COMPA: 13,  // Timer1 Compare A
  IRQ_TIMER1_COMPB: 14,  // Timer1 Compare B
  IRQ_TIMER1_OVF: 15,  // Timer1 Overflow
  IRQ_TIMER0_COMPA: 16,  // Timer0 Compare A
  IRQ_TIMER0_COMPB: 17,  // Timer0 Compare B
  IRQ_TIMER0_OVF: 18,  // Timer0 Overflow
  IRQ_SPI_STC: 19,  // SPI Transfer Complete
  IRQ_UART1_RX: 20,  // UART1 Receive
  IRQ_UART1_UDRE: 21,  // UART1 Data Register Empty
  IRQ_UART1_TX: 22,  // UART1 Transmit
  IRQ_ADC: 23,  // ADC Conversion Complete

  init: function() {
    // 硬件初始化
  }
};
