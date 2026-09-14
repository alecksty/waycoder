#ifndef ATTINY85_HPP
#define ATTINY85_HPP

// ATtiny85寄存器定义
// 生成自: Microchip/AVR/ATtiny85
// 版本: 1.0
// 日期: 2026-04-16


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 1000000 Hz

// 寄存器定义
// General Purpose Register 0
#define R0 (*(volatile uint8_t*)0x00)

// General Purpose Register 1
#define R1 (*(volatile uint8_t*)0x01)

// General Purpose Register 2
#define R2 (*(volatile uint8_t*)0x02)

// General Purpose Register 3
#define R3 (*(volatile uint8_t*)0x03)

// General Purpose Register 4
#define R4 (*(volatile uint8_t*)0x04)

// General Purpose Register 5
#define R5 (*(volatile uint8_t*)0x05)

// General Purpose Register 6
#define R6 (*(volatile uint8_t*)0x06)

// General Purpose Register 7
#define R7 (*(volatile uint8_t*)0x07)

// General Purpose Register 8
#define R8 (*(volatile uint8_t*)0x08)

// General Purpose Register 9
#define R9 (*(volatile uint8_t*)0x09)

// General Purpose Register 10
#define R10 (*(volatile uint8_t*)0x0A)

// General Purpose Register 11
#define R11 (*(volatile uint8_t*)0x0B)

// General Purpose Register 12
#define R12 (*(volatile uint8_t*)0x0C)

// General Purpose Register 13
#define R13 (*(volatile uint8_t*)0x0D)

// General Purpose Register 14
#define R14 (*(volatile uint8_t*)0x0E)

// General Purpose Register 15
#define R15 (*(volatile uint8_t*)0x0F)

// General Purpose Register 16
#define R16 (*(volatile uint8_t*)0x10)

// General Purpose Register 17
#define R17 (*(volatile uint8_t*)0x11)

// General Purpose Register 18
#define R18 (*(volatile uint8_t*)0x12)

// General Purpose Register 19
#define R19 (*(volatile uint8_t*)0x13)

// General Purpose Register 20
#define R20 (*(volatile uint8_t*)0x14)

// General Purpose Register 21
#define R21 (*(volatile uint8_t*)0x15)

// General Purpose Register 22
#define R22 (*(volatile uint8_t*)0x16)

// General Purpose Register 23
#define R23 (*(volatile uint8_t*)0x17)

// General Purpose Register 24
#define R24 (*(volatile uint8_t*)0x18)

// General Purpose Register 25
#define R25 (*(volatile uint8_t*)0x19)

// Register pair X (R27:R26)
#define X (*(volatile uint16_t*)0x1A)

// Register pair Y (R29:R28)
#define Y (*(volatile uint16_t*)0x1C)

// Register pair Z (R31:R30)
#define Z (*(volatile uint16_t*)0x1E)

// Stack Pointer
#define SP (*(volatile uint16_t*)0x3D)

// Status Register
#define SREG (*(volatile uint8_t*)0x3F)
#define SREG_C 0  // Carry Flag
#define SREG_Z 1  // Zero Flag
#define SREG_N 2  // Negative Flag
#define SREG_V 3  // Two's Complement Overflow Flag
#define SREG_S 4  // Sign Flag (N xor V)
#define SREG_H 5  // Half Carry Flag
#define SREG_T 6  // Transfer Bit
#define SREG_I 7  // Global Interrupt Enable

// 内存段定义
// Program Flash (8KB)
#define FLASH_START 0x0000
#define FLASH_END 0x1FFF
#define FLASH_SIZE 8192

// Internal SRAM (512B)
#define SRAM_START 0x0060
#define SRAM_END 0x025F
#define SRAM_SIZE 512

// EEPROM (512B)
#define EEPROM_START 0x0000
#define EEPROM_END 0x01FF
#define EEPROM_SIZE 512

// I/O Registers
#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64

// 外设定义
// Port A
#define PORTA_BASE 0x20
#define PORTA_PINA (*(volatile uint8_t*)0x00000040)
#define PORTA_DDRA (*(volatile uint8_t*)0x00000041)
#define PORTA_PORTA (*(volatile uint8_t*)0x00000042)

// Port B
#define PORTB_BASE 0x18
#define PORTB_PINB (*(volatile uint8_t*)0x0000002E)
#define PORTB_DDRB (*(volatile uint8_t*)0x0000002F)
#define PORTB_PORTB (*(volatile uint8_t*)0x00000030)

// Timer/Counter0
#define TIPO_BASE 0x20
#define TIPO_TCCR0A (*(volatile uint8_t*)0x00000040)
#define TIPO_TCCR0A_WGM00 0  // Waveform Generation Mode
#define TIPO_TCCR0A_WGM01 1  // Waveform Generation Mode
#define TIPO_TCCR0A_COM0B0 4  // Compare Output Mode B
#define TIPO_TCCR0A_COM0B1 5  // Compare Output Mode B
#define TIPO_TCCR0A_COM0A0 6  // Compare Output Mode A
#define TIPO_TCCR0A_COM0A1 7  // Compare Output Mode A
#define TIPO_TCCR0B (*(volatile uint8_t*)0x00000041)
#define TIPO_TCCR0B_CS00 0  // Clock Select
#define TIPO_TCCR0B_CS01 1  // Clock Select
#define TIPO_TCCR0B_CS02 2  // Clock Select
#define TIPO_TCCR0B_WGM02 3  // Waveform Generation Mode
#define TIPO_TCCR0B_FOC0B 6  // Force Output Compare B
#define TIPO_TCCR0B_FOC0A 7  // Force Output Compare A
#define TIPO_TCNT0 (*(volatile uint8_t*)0x00000042)
#define TIPO_OCR0A (*(volatile uint8_t*)0x00000043)
#define TIPO_OCR0B (*(volatile uint8_t*)0x00000044)
#define TIPO_TIMSK (*(volatile uint8_t*)0x00000059)
#define TIPO_TIMSK_TOIE0 0  // Timer/Counter0 Overflow Interrupt Enable
#define TIPO_TIMSK_OCIE0A 1  // Output Compare A Match Interrupt Enable
#define TIPO_TIMSK_OCIE0B 2  // Output Compare B Match Interrupt Enable
#define TIPO_TIFR (*(volatile uint8_t*)0x00000058)
#define TIPO_TIFR_TOV0 0  // Timer/Counter0 Overflow Flag
#define TIPO_TIFR_OCF0A 1  // Output Compare A Flag
#define TIPO_TIFR_OCF0B 2  // Output Compare B Flag

// Timer/Counter1
#define TMR1_BASE 0x28
#define TMR1_TCCR1A (*(volatile uint8_t*)0x00000050)
#define TMR1_TCCR1A_PCM1 0  // PWM Mode
#define TMR1_TCCR1A_COM1A 0  // Compare Output Mode A
#define TMR1_TCCR1A_COM1B 0  // Compare Output Mode B
#define TMR1_TCCR1A_WG13 1  // Waveform Generation Mode
#define TMR1_TCCR1A_WG10 0  // Waveform Generation Mode
#define TMR1_TCCR1B (*(volatile uint8_t*)0x00000051)
#define TMR1_TCCR1B_CTC1 7  // Clear Timer on Compare
#define TMR1_TCCR1B_WGM13 4  // Waveform Generation Mode
#define TMR1_TCCR1B_WGM12 3  // Waveform Generation Mode
#define TMR1_TCCR1B_CS1 0  // Clock Select
#define TMR1_TCNT1 (*(volatile uint16_t*)0x00000052)
#define TMR1_OCR1A (*(volatile uint16_t*)0x00000054)
#define TMR1_OCR1B (*(volatile uint16_t*)0x00000056)
#define TMR1_OCR1C (*(volatile uint16_t*)0x00000058)
#define TMR1_TIMSK1 (*(volatile uint8_t*)0x0000005B)
#define TMR1_TIFR1 (*(volatile uint8_t*)0x0000005A)

// ADC Multiplexer
#define ADMUX_BASE 0x12
#define ADMUX_ADMUX (*(volatile uint8_t*)0x00000024)
#define ADMUX_ADMUX_MUX 0  // Analog Channel Selection
#define ADMUX_ADMUX_ADLAR 5  // ADC Left Adjust Result
#define ADMUX_ADMUX_REFS 0  // Reference Selection
#define ADMUX_ADCSRA (*(volatile uint8_t*)0x00000025)
#define ADMUX_ADCSRA_ADPS 0  // ADC Prescaler Select
#define ADMUX_ADCSRA_ADIE 3  // ADC Interrupt Enable
#define ADMUX_ADCSRA_ADIF 4  // ADC Interrupt Flag
#define ADMUX_ADCSRA_ADATE 5  // ADC Auto Trigger Enable
#define ADMUX_ADCSRA_ADSC 6  // ADC Start Conversion
#define ADMUX_ADCSRA_ADEN 7  // ADC Enable
#define ADMUX_ADCH (*(volatile uint8_t*)0x00000026)
#define ADMUX_ADCL (*(volatile uint8_t*)0x00000027)

// Universal Serial Interface
#define USI_BASE 0x18
#define USI_USIDR (*(volatile uint8_t*)0x00000030)
#define USI_USISR (*(volatile uint8_t*)0x00000031)
#define USI_USISR_USICNT 0  // Counter
#define USI_USISR_USIDC 4  // Data Register
#define USI_USISR_USIPF 5  // Stop Cond Flag
#define USI_USISR_USIOV 6  // Overflow Flag
#define USI_USISR_USISIF 7  // Start Cond Interrupt Flag
#define USI_USICR (*(volatile uint8_t*)0x00000032)
#define USI_USICR_USICS 0  // Clock Source Select
#define USI_USICR_USISCL 2  // SCL strobe
#define USI_USICR_USIOW 3  // SDA output override
#define USI_USICR_USIOE 4  // Output Enable
#define USI_USICR_USISRE 5  // Start Recognition Enable
#define USI_USICR_USIORE 6  // Stop Recognition Enable
#define USI_USICR_USIGIE 7  // Global Interrupt Enable
#define USI_USIPORT (*(volatile uint8_t*)0x00000033)

// MCU Control
#define MCUCR_BASE 0x35
#define MCUCR_MCUCR (*(volatile uint8_t*)0x0000006A)
#define MCUCR_MCUCR_ISC 0  // Interrupt Sense Control
#define MCUCR_MCUCR_SE 4  // Sleep Enable
#define MCUCR_MCUCR_SM 0  // Sleep Mode
#define MCUCR_MCUCSR (*(volatile uint8_t*)0x0000006B)
#define MCUCR_MCUCSR_PORF 0  // Power-on Reset Flag
#define MCUCR_MCUCSR_EXTRF 1  // External Reset Flag
#define MCUCR_MCUCSR_WDRF 2  // Watchdog Reset Flag
#define MCUCR_MCUCSR_BORF 4  // Brown-out Reset Flag

// Watchdog Timer
#define WDTCR_BASE 0x21
#define WDTCR_WDTCR (*(volatile uint8_t*)0x00000042)
#define WDTCR_WDTCR_WDP 0  // Watchdog Prescaler
#define WDTCR_WDTCR_WDE 3  // Watchdog Enable
#define WDTCR_WDTCR_WDIE 4  // Watchdog Interrupt Enable

// EEPROM
#define EEPR_BASE 0x1C
#define EEPR_EEAR (*(volatile uint8_t*)0x0000003A)
#define EEPR_EEDR (*(volatile uint8_t*)0x00000039)
#define EEPR_EECR (*(volatile uint8_t*)0x0000003B)
#define EEPR_EECR_EEPM 0  // EEPROM Programming Mode
#define EEPR_EECR_EERIE 3  // EEPROM Ready Interrupt Enable
#define EEPR_EECR_EEWE 2  // EEPROM Write Enable
#define EEPR_EECR_EEMWE 1  // EEPROM Master Write Enable
#define EEPR_EECR_EERE 0  // EEPROM Read Enable

// External Interrupt
#define GIMSK_BASE 0x3B
#define GIMSK_GIMSK (*(volatile uint8_t*)0x00000076)
#define GIMSK_GIMSK_INT0 0  // External Interrupt Request 0 Enable
#define GIMSK_GIMSK_PCIE 1  // Pin Change Interrupt Enable
#define GIMSK_GIFR (*(volatile uint8_t*)0x00000077)
#define GIMSK_GIFR_INTF0 0  // External Interrupt Flag 0
#define GIMSK_GIFR_PCIF 1  // Pin Change Interrupt Flag

// Pin Change Mask
#define PCMSK_BASE 0x15
#define PCMSK_PCMSK (*(volatile uint8_t*)0x0000002A)

// Store Program Memory
#define SPMCSR_BASE 0x37
#define SPMCSR_SPMCSR (*(volatile uint8_t*)0x0000006E)
#define SPMCSR_SPMCSR_SPMCR 0  // SPM Mode
#define SPMCSR_SPMCSR_PGERS 1  // Page Erase
#define SPMCSR_SPMCSR_PGWRT 2  // Page Write
#define SPMCSR_SPMCSR_BLBSET 3  // Boot Lock Bits Set
#define SPMCSR_SPMCSR_RWWSRE 4  // Read-While-Read Strobe Enable
#define SPMCSR_SPMCSR_SIGRD 5  // Signature Row Read
#define SPMCSR_SPMCSR_SPMEN 7  // SPM Enable

// 中断向量定义
#define RESET_VECTOR 0  // External Reset, Power-on Reset, Brown-out Reset
#define INT0_VECTOR 1  // External Interrupt Request 0
#define PCINT0_VECTOR 2  // Pin Change
#define WDT_VECTOR 3  // Watchdog Timeout
#define TIM1_COMPA_VECTOR 4  // Timer/Counter1 Compare Match A
#define TIM1_OVF_VECTOR 5  // Timer/Counter1 Overflow
#define TIM0_COMPA_VECTOR 6  // Timer/Counter0 Compare Match A
#define TIM0_OVF_VECTOR 7  // Timer/Counter0 Overflow
#define SPI_STC_VECTOR 8  // SPI Serial Transfer Complete
#define ADC_VECTOR 9  // ADC Conversion Complete
#define USI_START_VECTOR 10  // USI Start Condition
#define USI_OVF_VECTOR 11  // USI Overflow
#define EE_READY_VECTOR 12  // EEPROM Ready

// 引脚定义
#define PIN_PB5 1  // RESET - ADC0 - dW
#define PIN_PB3 2  // XTAL1 - CLKI - ADC3
#define PIN_PB4 3  // XTAL2 - ADC2
#define PIN_PB0 4  // MOSI - AI - ADC0 - T0 - INT0
#define PIN_PB1 5  // MISO - AI - ADC1 - OC1A - INT1
#define PIN_PB2 6  // SCK - AI - ADC3 - OC1B
#define PIN_VCC 7  // Supply Voltage
#define PIN_GND 8  // Ground

void attiny85_init(void);

#ifdef __cplusplus
}
#endif

#endif // ATTINY85_HPP
