/**
 * ATtiny85 寄存器定义
 * 生成自: Microchip/AVR/ATtiny85
 * 版本: 1.0
 */
export const attiny85 = {
  // CPU: AVR, 8位, 1000000 Hz

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
  // Register pair X (R27:R26)
  X: 0x1A,
  // Register pair Y (R29:R28)
  Y: 0x1C,
  // Register pair Z (R31:R30)
  Z: 0x1E,
  // Stack Pointer
  SP: 0x3D,
  // Status Register
  SREG: 0x3F,
  SREG_C: 0,  // Carry Flag
  SREG_Z: 1,  // Zero Flag
  SREG_N: 2,  // Negative Flag
  SREG_V: 3,  // Two's Complement Overflow Flag
  SREG_S: 4,  // Sign Flag (N xor V)
  SREG_H: 5,  // Half Carry Flag
  SREG_T: 6,  // Transfer Bit
  SREG_I: 7,  // Global Interrupt Enable

  // 内存段
  // Program Flash (8KB)
  flash_START: 0x0000,
  flash_END: 0x1FFF,
  flash_SIZE: 8192,
  // Internal SRAM (512B)
  sram_START: 0x0060,
  sram_END: 0x025F,
  sram_SIZE: 512,
  // EEPROM (512B)
  eeprom_START: 0x0000,
  eeprom_END: 0x01FF,
  eeprom_SIZE: 512,
  // I/O Registers
  io_START: 0x00,
  io_END: 0x3F,
  io_SIZE: 64,

  // 外设定义
  // Port A
  PORTA_BASE: 0x20,
  PORTA_PINA: 0x00000040,
  PORTA_DDRA: 0x00000041,
  PORTA_PORTA: 0x00000042,
  // Port B
  PORTB_BASE: 0x18,
  PORTB_PINB: 0x0000002E,
  PORTB_DDRB: 0x0000002F,
  PORTB_PORTB: 0x00000030,
  // Timer/Counter0
  TIPO_BASE: 0x20,
  TIPO_TCCR0A: 0x00000040,
  TIPO_TCCR0A_WGM00: 0,  // Waveform Generation Mode
  TIPO_TCCR0A_WGM01: 1,  // Waveform Generation Mode
  TIPO_TCCR0A_COM0B0: 4,  // Compare Output Mode B
  TIPO_TCCR0A_COM0B1: 5,  // Compare Output Mode B
  TIPO_TCCR0A_COM0A0: 6,  // Compare Output Mode A
  TIPO_TCCR0A_COM0A1: 7,  // Compare Output Mode A
  TIPO_TCCR0B: 0x00000041,
  TIPO_TCCR0B_CS00: 0,  // Clock Select
  TIPO_TCCR0B_CS01: 1,  // Clock Select
  TIPO_TCCR0B_CS02: 2,  // Clock Select
  TIPO_TCCR0B_WGM02: 3,  // Waveform Generation Mode
  TIPO_TCCR0B_FOC0B: 6,  // Force Output Compare B
  TIPO_TCCR0B_FOC0A: 7,  // Force Output Compare A
  TIPO_TCNT0: 0x00000042,
  TIPO_OCR0A: 0x00000043,
  TIPO_OCR0B: 0x00000044,
  TIPO_TIMSK: 0x00000059,
  TIPO_TIMSK_TOIE0: 0,  // Timer/Counter0 Overflow Interrupt Enable
  TIPO_TIMSK_OCIE0A: 1,  // Output Compare A Match Interrupt Enable
  TIPO_TIMSK_OCIE0B: 2,  // Output Compare B Match Interrupt Enable
  TIPO_TIFR: 0x00000058,
  TIPO_TIFR_TOV0: 0,  // Timer/Counter0 Overflow Flag
  TIPO_TIFR_OCF0A: 1,  // Output Compare A Flag
  TIPO_TIFR_OCF0B: 2,  // Output Compare B Flag
  // Timer/Counter1
  TMR1_BASE: 0x28,
  TMR1_TCCR1A: 0x00000050,
  TMR1_TCCR1A_PCM1: 0,  // PWM Mode
  TMR1_TCCR1A_COM1A: 0,  // Compare Output Mode A
  TMR1_TCCR1A_COM1B: 0,  // Compare Output Mode B
  TMR1_TCCR1A_WG13: 1,  // Waveform Generation Mode
  TMR1_TCCR1A_WG10: 0,  // Waveform Generation Mode
  TMR1_TCCR1B: 0x00000051,
  TMR1_TCCR1B_CTC1: 7,  // Clear Timer on Compare
  TMR1_TCCR1B_WGM13: 4,  // Waveform Generation Mode
  TMR1_TCCR1B_WGM12: 3,  // Waveform Generation Mode
  TMR1_TCCR1B_CS1: 0,  // Clock Select
  TMR1_TCNT1: 0x00000052,
  TMR1_OCR1A: 0x00000054,
  TMR1_OCR1B: 0x00000056,
  TMR1_OCR1C: 0x00000058,
  TMR1_TIMSK1: 0x0000005B,
  TMR1_TIFR1: 0x0000005A,
  // ADC Multiplexer
  ADMUX_BASE: 0x12,
  ADMUX_ADMUX: 0x00000024,
  ADMUX_ADMUX_MUX: 0,  // Analog Channel Selection
  ADMUX_ADMUX_ADLAR: 5,  // ADC Left Adjust Result
  ADMUX_ADMUX_REFS: 0,  // Reference Selection
  ADMUX_ADCSRA: 0x00000025,
  ADMUX_ADCSRA_ADPS: 0,  // ADC Prescaler Select
  ADMUX_ADCSRA_ADIE: 3,  // ADC Interrupt Enable
  ADMUX_ADCSRA_ADIF: 4,  // ADC Interrupt Flag
  ADMUX_ADCSRA_ADATE: 5,  // ADC Auto Trigger Enable
  ADMUX_ADCSRA_ADSC: 6,  // ADC Start Conversion
  ADMUX_ADCSRA_ADEN: 7,  // ADC Enable
  ADMUX_ADCH: 0x00000026,
  ADMUX_ADCL: 0x00000027,
  // Universal Serial Interface
  USI_BASE: 0x18,
  USI_USIDR: 0x00000030,
  USI_USISR: 0x00000031,
  USI_USISR_USICNT: 0,  // Counter
  USI_USISR_USIDC: 4,  // Data Register
  USI_USISR_USIPF: 5,  // Stop Cond Flag
  USI_USISR_USIOV: 6,  // Overflow Flag
  USI_USISR_USISIF: 7,  // Start Cond Interrupt Flag
  USI_USICR: 0x00000032,
  USI_USICR_USICS: 0,  // Clock Source Select
  USI_USICR_USISCL: 2,  // SCL strobe
  USI_USICR_USIOW: 3,  // SDA output override
  USI_USICR_USIOE: 4,  // Output Enable
  USI_USICR_USISRE: 5,  // Start Recognition Enable
  USI_USICR_USIORE: 6,  // Stop Recognition Enable
  USI_USICR_USIGIE: 7,  // Global Interrupt Enable
  USI_USIPORT: 0x00000033,
  // MCU Control
  MCUCR_BASE: 0x35,
  MCUCR_MCUCR: 0x0000006A,
  MCUCR_MCUCR_ISC: 0,  // Interrupt Sense Control
  MCUCR_MCUCR_SE: 4,  // Sleep Enable
  MCUCR_MCUCR_SM: 0,  // Sleep Mode
  MCUCR_MCUCSR: 0x0000006B,
  MCUCR_MCUCSR_PORF: 0,  // Power-on Reset Flag
  MCUCR_MCUCSR_EXTRF: 1,  // External Reset Flag
  MCUCR_MCUCSR_WDRF: 2,  // Watchdog Reset Flag
  MCUCR_MCUCSR_BORF: 4,  // Brown-out Reset Flag
  // Watchdog Timer
  WDTCR_BASE: 0x21,
  WDTCR_WDTCR: 0x00000042,
  WDTCR_WDTCR_WDP: 0,  // Watchdog Prescaler
  WDTCR_WDTCR_WDE: 3,  // Watchdog Enable
  WDTCR_WDTCR_WDIE: 4,  // Watchdog Interrupt Enable
  // EEPROM
  EEPR_BASE: 0x1C,
  EEPR_EEAR: 0x0000003A,
  EEPR_EEDR: 0x00000039,
  EEPR_EECR: 0x0000003B,
  EEPR_EECR_EEPM: 0,  // EEPROM Programming Mode
  EEPR_EECR_EERIE: 3,  // EEPROM Ready Interrupt Enable
  EEPR_EECR_EEWE: 2,  // EEPROM Write Enable
  EEPR_EECR_EEMWE: 1,  // EEPROM Master Write Enable
  EEPR_EECR_EERE: 0,  // EEPROM Read Enable
  // External Interrupt
  GIMSK_BASE: 0x3B,
  GIMSK_GIMSK: 0x00000076,
  GIMSK_GIMSK_INT0: 0,  // External Interrupt Request 0 Enable
  GIMSK_GIMSK_PCIE: 1,  // Pin Change Interrupt Enable
  GIMSK_GIFR: 0x00000077,
  GIMSK_GIFR_INTF0: 0,  // External Interrupt Flag 0
  GIMSK_GIFR_PCIF: 1,  // Pin Change Interrupt Flag
  // Pin Change Mask
  PCMSK_BASE: 0x15,
  PCMSK_PCMSK: 0x0000002A,
  // Store Program Memory
  SPMCSR_BASE: 0x37,
  SPMCSR_SPMCSR: 0x0000006E,
  SPMCSR_SPMCSR_SPMCR: 0,  // SPM Mode
  SPMCSR_SPMCSR_PGERS: 1,  // Page Erase
  SPMCSR_SPMCSR_PGWRT: 2,  // Page Write
  SPMCSR_SPMCSR_BLBSET: 3,  // Boot Lock Bits Set
  SPMCSR_SPMCSR_RWWSRE: 4,  // Read-While-Read Strobe Enable
  SPMCSR_SPMCSR_SIGRD: 5,  // Signature Row Read
  SPMCSR_SPMCSR_SPMEN: 7,  // SPM Enable

  // 中断向量
  IRQ_RESET: 0,  // External Reset, Power-on Reset, Brown-out Reset
  IRQ_INT0: 1,  // External Interrupt Request 0
  IRQ_PCINT0: 2,  // Pin Change
  IRQ_WDT: 3,  // Watchdog Timeout
  IRQ_TIM1_COMPA: 4,  // Timer/Counter1 Compare Match A
  IRQ_TIM1_OVF: 5,  // Timer/Counter1 Overflow
  IRQ_TIM0_COMPA: 6,  // Timer/Counter0 Compare Match A
  IRQ_TIM0_OVF: 7,  // Timer/Counter0 Overflow
  IRQ_SPI_STC: 8,  // SPI Serial Transfer Complete
  IRQ_ADC: 9,  // ADC Conversion Complete
  IRQ_USI_START: 10,  // USI Start Condition
  IRQ_USI_OVF: 11,  // USI Overflow
  IRQ_EE_READY: 12,  // EEPROM Ready

  // 引脚定义
  PIN_PB5: 1,  // RESET - ADC0 - dW
  PIN_PB3: 2,  // XTAL1 - CLKI - ADC3
  PIN_PB4: 3,  // XTAL2 - ADC2
  PIN_PB0: 4,  // MOSI - AI - ADC0 - T0 - INT0
  PIN_PB1: 5,  // MISO - AI - ADC1 - OC1A - INT1
  PIN_PB2: 6,  // SCK - AI - ADC3 - OC1B
  PIN_VCC: 7,  // Supply Voltage
  PIN_GND: 8,  // Ground

  init: function() {
    // 硬件初始化
  }
};
