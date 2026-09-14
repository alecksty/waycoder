/**
 * ATmega328P 寄存器定义
 * 生成自: Atmel/AVR/ATmega328P
 * 版本: 1.0
 */
export const atmega328p = {
  // CPU: AVR, 8位, 16000000 Hz

  // 寄存器定义
  // General Purpose Register 0
  R0: 0x00,
  // General Purpose Register 1
  R1: 0x01,
  // General Purpose Register 2
  R2: 0x02,
  // General Purpose Register 3
  R3: 0x03,
  // General Purpose Register 4
  R4: 0x04,
  // General Purpose Register 5
  R5: 0x05,
  // General Purpose Register 6
  R6: 0x06,
  // General Purpose Register 7
  R7: 0x07,
  // General Purpose Register 8
  R8: 0x08,
  // General Purpose Register 9
  R9: 0x09,
  // General Purpose Register 10
  R10: 0x0A,
  // General Purpose Register 11
  R11: 0x0B,
  // General Purpose Register 12
  R12: 0x0C,
  // General Purpose Register 13
  R13: 0x0D,
  // General Purpose Register 14
  R14: 0x0E,
  // General Purpose Register 15
  R15: 0x0F,
  // General Purpose Register 16
  R16: 0x10,
  // General Purpose Register 17
  R17: 0x11,
  // General Purpose Register 18
  R18: 0x12,
  // General Purpose Register 19
  R19: 0x13,
  // General Purpose Register 20
  R20: 0x14,
  // General Purpose Register 21
  R21: 0x15,
  // General Purpose Register 22
  R22: 0x16,
  // General Purpose Register 23
  R23: 0x17,
  // General Purpose Register 24
  R24: 0x18,
  // General Purpose Register 25
  R25: 0x19,
  // General Purpose Register 26 (XL)
  R26: 0x1A,
  // General Purpose Register 27 (XH)
  R27: 0x1B,
  // General Purpose Register 28 (YL)
  R28: 0x1C,
  // General Purpose Register 29 (YH)
  R29: 0x1D,
  // General Purpose Register 30 (ZL)
  R30: 0x1E,
  // General Purpose Register 31 (ZH)
  R31: 0x1F,
  // Stack Pointer Low
  SPL: 0x5D,
  // Stack Pointer High
  SPH: 0x5E,
  // Status Register
  SREG: 0x5F,
  SREG_C: 0,  // Carry Flag
  SREG_Z: 1,  // Zero Flag
  SREG_N: 2,  // Negative Flag
  SREG_V: 3,  // Two's Complement Overflow Flag
  SREG_S: 4,  // Sign Flag (N ⊕ V)
  SREG_H: 5,  // Half Carry Flag
  SREG_T: 6,  // Transfer Bit
  SREG_I: 7,  // Global Interrupt Enable

  // 内存段
  // Program Flash Memory
  FLASH_START: 0x0000,
  FLASH_END: 0x7FFF,
  FLASH_SIZE: 32768,
  // Static RAM
  SRAM_START: 0x0100,
  SRAM_END: 0x08FF,
  SRAM_SIZE: 2048,
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
  // Port B Data Register
  PORTB_BASE: 0x23,
  PORTB_PORTB: 0x00000048,
  PORTB_DDRB: 0x00000047,
  PORTB_PINB: 0x00000046,
  PORTB_PB0: 0,  // Port B, bit 0
  PORTB_PB1: 1,  // Port B, bit 1
  PORTB_PB2: 2,  // Port B, bit 2
  PORTB_PB3: 3,  // Port B, bit 3
  PORTB_PB4: 4,  // Port B, bit 4
  PORTB_PB5: 5,  // Port B, bit 5
  PORTB_PB6: 6,  // Port B, bit 6
  PORTB_PB7: 7,  // Port B, bit 7
  // Port C Data Register
  PORTC_BASE: 0x26,
  PORTC_PORTC: 0x0000004E,
  PORTC_DDRC: 0x0000004D,
  PORTC_PINC: 0x0000004C,
  PORTC_PC0: 0,  // Port C, bit 0
  PORTC_PC1: 1,  // Port C, bit 1
  PORTC_PC2: 2,  // Port C, bit 2
  PORTC_PC3: 3,  // Port C, bit 3
  PORTC_PC4: 4,  // Port C, bit 4
  PORTC_PC5: 5,  // Port C, bit 5
  PORTC_PC6: 6,  // Port C, bit 6
  // Port D Data Register
  PORTD_BASE: 0x29,
  PORTD_PORTD: 0x00000054,
  PORTD_DDRD: 0x00000053,
  PORTD_PIND: 0x00000052,
  PORTD_PD0: 0,  // Port D, bit 0
  PORTD_PD1: 1,  // Port D, bit 1
  PORTD_PD2: 2,  // Port D, bit 2
  PORTD_PD3: 3,  // Port D, bit 3
  PORTD_PD4: 4,  // Port D, bit 4
  PORTD_PD5: 5,  // Port D, bit 5
  PORTD_PD6: 6,  // Port D, bit 6
  PORTD_PD7: 7,  // Port D, bit 7
  // 8-bit Timer/Counter0
  TIMER0_BASE: 0x44,
  TIMER0_TCCR0A: 0x00000088,
  TIMER0_TCCR0A_WGM00: 0,  // Waveform Generation Mode
  TIMER0_TCCR0A_WGM01: 1,  // Waveform Generation Mode
  TIMER0_TCCR0A_COM0B0: 4,  // Compare Output Mode for Channel B
  TIMER0_TCCR0A_COM0B1: 5,  // Compare Output Mode for Channel B
  TIMER0_TCCR0A_COM0A0: 6,  // Compare Output Mode for Channel A
  TIMER0_TCCR0A_COM0A1: 7,  // Compare Output Mode for Channel A
  TIMER0_TCCR0B: 0x00000089,
  TIMER0_TCCR0B_CS00: 0,  // Clock Select
  TIMER0_TCCR0B_CS01: 1,  // Clock Select
  TIMER0_TCCR0B_CS02: 2,  // Clock Select
  TIMER0_TCCR0B_WGM02: 3,  // Waveform Generation Mode
  TIMER0_TCCR0B_FOC0B: 6,  // Force Output Compare B
  TIMER0_TCCR0B_FOC0A: 7,  // Force Output Compare A
  TIMER0_TCNT0: 0x0000008A,
  TIMER0_OCR0A: 0x0000008B,
  TIMER0_OCR0B: 0x0000008C,
  TIMER0_TIMSK0: 0x000000B2,
  TIMER0_TIMSK0_TOIE0: 0,  // Timer/Counter0 Overflow Interrupt Enable
  TIMER0_TIMSK0_OCIE0A: 1,  // Timer/Counter0 Output Compare A Match Interrupt Enable
  TIMER0_TIMSK0_OCIE0B: 2,  // Timer/Counter0 Output Compare B Match Interrupt Enable
  TIMER0_TIFR0: 0x00000079,
  TIMER0_TIFR0_TOV0: 0,  // Timer/Counter0 Overflow Flag
  TIMER0_TIFR0_OCF0A: 1,  // Output Compare Flag 0A
  TIMER0_TIFR0_OCF0B: 2,  // Output Compare Flag 0B
  // Universal Synchronous/Asynchronous Receiver/Transmitter
  USART0_BASE: 0xC0,
  USART0_UDR0: 0x00000186,
  USART0_UCSR0A: 0x00000180,
  USART0_UCSR0A_MPCM0: 0,  // Multi-processor Communication Mode
  USART0_UCSR0A_U2X0: 1,  // Double the USART Transmission Speed
  USART0_UCSR0A_UPE0: 2,  // Parity Error
  USART0_UCSR0A_DOR0: 3,  // Data OverRun
  USART0_UCSR0A_FE0: 4,  // Frame Error
  USART0_UCSR0A_UDRE0: 5,  // USART Data Register Empty
  USART0_UCSR0A_TXC0: 6,  // USART Transmit Complete
  USART0_UCSR0A_RXC0: 7,  // USART Receive Complete
  USART0_UCSR0B: 0x00000181,
  USART0_UCSR0B_TXB80: 0,  // Transmit Data Bit 8
  USART0_UCSR0B_RXB80: 1,  // Receive Data Bit 8
  USART0_UCSR0B_UCSZ02: 2,  // Character Size
  USART0_UCSR0B_TXEN0: 3,  // Transmitter Enable
  USART0_UCSR0B_RXEN0: 4,  // Receiver Enable
  USART0_UCSR0B_UDRIE0: 5,  // USART Data Register Empty Interrupt Enable
  USART0_UCSR0B_TXCIE0: 6,  // TX Complete Interrupt Enable
  USART0_UCSR0B_RXCIE0: 7,  // RX Complete Interrupt Enable
  USART0_UCSR0C: 0x00000182,
  USART0_UCSR0C_UCPOL0: 0,  // Clock Polarity
  USART0_UCSR0C_UCSZ00: 1,  // Character Size
  USART0_UCSR0C_UCSZ01: 2,  // Character Size
  USART0_UCSR0C_USBS0: 3,  // Stop Bit Select
  USART0_UCSR0C_UPM00: 4,  // Parity Mode
  USART0_UCSR0C_UPM01: 5,  // Parity Mode
  USART0_UCSR0C_UMSEL00: 6,  // USART Mode Select
  USART0_UCSR0C_UMSEL01: 7,  // USART Mode Select
  USART0_UBRR0: 0x00000184,
  // Analog-to-Digital Converter
  ADC_BASE: 0x78,
  ADC_ADMUX: 0x000000F4,
  ADC_ADMUX_MUX0: 0,  // Analog Channel Selection
  ADC_ADMUX_MUX1: 1,  // Analog Channel Selection
  ADC_ADMUX_MUX2: 2,  // Analog Channel Selection
  ADC_ADMUX_MUX3: 3,  // Analog Channel Selection
  ADC_ADMUX_ADLAR: 5,  // ADC Left Adjust Result
  ADC_ADMUX_REFS0: 6,  // Reference Selection
  ADC_ADMUX_REFS1: 7,  // Reference Selection
  ADC_ADCSRA: 0x000000F2,
  ADC_ADCSRA_ADPS0: 0,  // ADC Prescaler Select
  ADC_ADCSRA_ADPS1: 1,  // ADC Prescaler Select
  ADC_ADCSRA_ADPS2: 2,  // ADC Prescaler Select
  ADC_ADCSRA_ADIE: 3,  // ADC Interrupt Enable
  ADC_ADCSRA_ADIF: 4,  // ADC Interrupt Flag
  ADC_ADCSRA_ADATE: 5,  // ADC Auto Trigger Enable
  ADC_ADCSRA_ADSC: 6,  // ADC Start Conversion
  ADC_ADCSRA_ADEN: 7,  // ADC Enable
  ADC_ADCH: 0x000000F1,
  ADC_ADCL: 0x000000F0,

  // 中断向量
  IRQ_INT0: 1,  // External Interrupt Request 0
  IRQ_INT1: 2,  // External Interrupt Request 1
  IRQ_PCINT0: 3,  // Pin Change Interrupt Request 0
  IRQ_PCINT1: 4,  // Pin Change Interrupt Request 1
  IRQ_PCINT2: 5,  // Pin Change Interrupt Request 2
  IRQ_WDT: 6,  // Watchdog Time-out Interrupt
  IRQ_TIMER2_COMPA: 7,  // Timer/Counter2 Compare Match A
  IRQ_TIMER2_COMPB: 8,  // Timer/Counter2 Compare Match B
  IRQ_TIMER2_OVF: 9,  // Timer/Counter2 Overflow
  IRQ_TIMER1_CAPT: 10,  // Timer/Counter1 Capture Event
  IRQ_TIMER1_COMPA: 11,  // Timer/Counter1 Compare Match A
  IRQ_TIMER1_COMPB: 12,  // Timer/Counter1 Compare Match B
  IRQ_TIMER1_OVF: 13,  // Timer/Counter1 Overflow
  IRQ_TIMER0_COMPA: 14,  // Timer/Counter0 Compare Match A
  IRQ_TIMER0_COMPB: 15,  // Timer/Counter0 Compare Match B
  IRQ_TIMER0_OVF: 16,  // Timer/Counter0 Overflow
  IRQ_SPI_STC: 17,  // SPI Serial Transfer Complete
  IRQ_USART_RX: 18,  // USART Rx Complete
  IRQ_USART_UDRE: 19,  // USART Data Register Empty
  IRQ_USART_TX: 20,  // USART Tx Complete
  IRQ_ADC: 21,  // ADC Conversion Complete
  IRQ_EE_READY: 22,  // EEPROM Ready
  IRQ_ANALOG_COMP: 23,  // Analog Comparator
  IRQ_TWI: 24,  // Two-wire Serial Interface
  IRQ_SPM_READY: 25,  // Store Program Memory Ready

  // 引脚定义
  PIN_PC6: 1,  // Reset Pin
  PIN_PD0: 2,  // Digital I/O, RX (USART)
  PIN_PD1: 3,  // Digital I/O, TX (USART)
  PIN_PD2: 4,  // Digital I/O, INT0
  PIN_PD3: 5,  // Digital I/O, INT1, OC2B
  PIN_PD4: 6,  // Digital I/O, T0, XCK
  PIN_VCC: 7,  // Supply Voltage
  PIN_GND: 8,  // Ground
  PIN_PB6: 9,  // Digital I/O, XTAL1
  PIN_PB7: 10,  // Digital I/O, XTAL2
  PIN_PD5: 11,  // Digital I/O, T1, OC0B
  PIN_PD6: 12,  // Digital I/O, AIN0, OC0A
  PIN_PD7: 13,  // Digital I/O, AIN1
  PIN_PB0: 14,  // Digital I/O, ICP1, CLKO
  PIN_PB1: 15,  // Digital I/O, OC1A
  PIN_PB2: 16,  // Digital I/O, SS, OC1B
  PIN_PB3: 17,  // Digital I/O, MOSI, OC2A
  PIN_PB4: 18,  // Digital I/O, MISO
  PIN_PB5: 19,  // Digital I/O, SCK
  PIN_AVCC: 20,  // Supply Voltage for ADC
  PIN_AREF: 21,  // Analog Reference
  PIN_GND: 22,  // Ground
  PIN_PC0: 23,  // Digital I/O, ADC0
  PIN_PC1: 24,  // Digital I/O, ADC1
  PIN_PC2: 25,  // Digital I/O, ADC2
  PIN_PC3: 26,  // Digital I/O, ADC3
  PIN_PC4: 27,  // Digital I/O, ADC4, SDA
  PIN_PC5: 28,  // Digital I/O, ADC5, SCL

  init: function() {
    // 硬件初始化
  }
};
