// ATtiny85 设备定义 - Objective-C 头文件
// 生成自: Microchip/AVR/ATtiny85
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 1000000 Hz

#ifndef ATTINY85_DEVICE_H
#define ATTINY85_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define R0_ADDR 0x00  // General Purpose Register 0
#define R1_ADDR 0x01  // General Purpose Register 1
#define R2_ADDR 0x02  // General Purpose Register 2
#define R3_ADDR 0x03  // General Purpose Register 3
#define R4_ADDR 0x04  // General Purpose Register 4
#define R5_ADDR 0x05  // General Purpose Register 5
#define R6_ADDR 0x06  // General Purpose Register 6
#define R7_ADDR 0x07  // General Purpose Register 7
#define R8_ADDR 0x08  // General Purpose Register 8
#define R9_ADDR 0x09  // General Purpose Register 9
#define R10_ADDR 0x0A  // General Purpose Register 10
#define R11_ADDR 0x0B  // General Purpose Register 11
#define R12_ADDR 0x0C  // General Purpose Register 12
#define R13_ADDR 0x0D  // General Purpose Register 13
#define R14_ADDR 0x0E  // General Purpose Register 14
#define R15_ADDR 0x0F  // General Purpose Register 15
#define R16_ADDR 0x10  // General Purpose Register 16
#define R17_ADDR 0x11  // General Purpose Register 17
#define R18_ADDR 0x12  // General Purpose Register 18
#define R19_ADDR 0x13  // General Purpose Register 19
#define R20_ADDR 0x14  // General Purpose Register 20
#define R21_ADDR 0x15  // General Purpose Register 21
#define R22_ADDR 0x16  // General Purpose Register 22
#define R23_ADDR 0x17  // General Purpose Register 23
#define R24_ADDR 0x18  // General Purpose Register 24
#define R25_ADDR 0x19  // General Purpose Register 25
#define X_ADDR 0x1A  // Register pair X (R27:R26)
#define Y_ADDR 0x1C  // Register pair Y (R29:R28)
#define Z_ADDR 0x1E  // Register pair Z (R31:R30)
#define SP_ADDR 0x3D  // Stack Pointer
#define SREG_ADDR 0x3F  // Status Register
#define SREG_C_BIT 0  // Carry Flag
#define SREG_Z_BIT 1  // Zero Flag
#define SREG_N_BIT 2  // Negative Flag
#define SREG_V_BIT 3  // Two's Complement Overflow Flag
#define SREG_S_BIT 4  // Sign Flag (N xor V)
#define SREG_H_BIT 5  // Half Carry Flag
#define SREG_T_BIT 6  // Transfer Bit
#define SREG_I_BIT 7  // Global Interrupt Enable

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x1FFF
#define FLASH_SIZE 8192  // Program Flash (8KB)
#define SRAM_START 0x0060
#define SRAM_END 0x025F
#define SRAM_SIZE 512  // Internal SRAM (512B)
#define EEPROM_START 0x0000
#define EEPROM_END 0x01FF
#define EEPROM_SIZE 512  // EEPROM (512B)
#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64  // I/O Registers

// 外设定义
// Port A
#define PORTA_BASE 0x20
#define PORTA_PINA_ADDR 0x20
#define PORTA_DDRA_ADDR 0x21
#define PORTA_PORTA_ADDR 0x22
// Port B
#define PORTB_BASE 0x18
#define PORTB_PINB_ADDR 0x16
#define PORTB_DDRB_ADDR 0x17
#define PORTB_PORTB_ADDR 0x18
// Timer/Counter0
#define TIPO_BASE 0x20
#define TIPO_TCCR0A_ADDR 0x20
#define TIPO_TCCR0A_WGM00_BIT 0  // Waveform Generation Mode
#define TIPO_TCCR0A_WGM01_BIT 1  // Waveform Generation Mode
#define TIPO_TCCR0A_COM0B0_BIT 4  // Compare Output Mode B
#define TIPO_TCCR0A_COM0B1_BIT 5  // Compare Output Mode B
#define TIPO_TCCR0A_COM0A0_BIT 6  // Compare Output Mode A
#define TIPO_TCCR0A_COM0A1_BIT 7  // Compare Output Mode A
#define TIPO_TCCR0B_ADDR 0x21
#define TIPO_TCCR0B_CS00_BIT 0  // Clock Select
#define TIPO_TCCR0B_CS01_BIT 1  // Clock Select
#define TIPO_TCCR0B_CS02_BIT 2  // Clock Select
#define TIPO_TCCR0B_WGM02_BIT 3  // Waveform Generation Mode
#define TIPO_TCCR0B_FOC0B_BIT 6  // Force Output Compare B
#define TIPO_TCCR0B_FOC0A_BIT 7  // Force Output Compare A
#define TIPO_TCNT0_ADDR 0x22
#define TIPO_OCR0A_ADDR 0x23
#define TIPO_OCR0B_ADDR 0x24
#define TIPO_TIMSK_ADDR 0x39
#define TIPO_TIMSK_TOIE0_BIT 0  // Timer/Counter0 Overflow Interrupt Enable
#define TIPO_TIMSK_OCIE0A_BIT 1  // Output Compare A Match Interrupt Enable
#define TIPO_TIMSK_OCIE0B_BIT 2  // Output Compare B Match Interrupt Enable
#define TIPO_TIFR_ADDR 0x38
#define TIPO_TIFR_TOV0_BIT 0  // Timer/Counter0 Overflow Flag
#define TIPO_TIFR_OCF0A_BIT 1  // Output Compare A Flag
#define TIPO_TIFR_OCF0B_BIT 2  // Output Compare B Flag
// Timer/Counter1
#define TMR1_BASE 0x28
#define TMR1_TCCR1A_ADDR 0x28
#define TMR1_TCCR1A_PCM1_BIT 0  // PWM Mode
#define TMR1_TCCR1A_COM1A_BIT 0  // Compare Output Mode A
#define TMR1_TCCR1A_COM1B_BIT 0  // Compare Output Mode B
#define TMR1_TCCR1A_WG13_BIT 1  // Waveform Generation Mode
#define TMR1_TCCR1A_WG10_BIT 0  // Waveform Generation Mode
#define TMR1_TCCR1B_ADDR 0x29
#define TMR1_TCCR1B_CTC1_BIT 7  // Clear Timer on Compare
#define TMR1_TCCR1B_WGM13_BIT 4  // Waveform Generation Mode
#define TMR1_TCCR1B_WGM12_BIT 3  // Waveform Generation Mode
#define TMR1_TCCR1B_CS1_BIT 0  // Clock Select
#define TMR1_TCNT1_ADDR 0x2A
#define TMR1_OCR1A_ADDR 0x2C
#define TMR1_OCR1B_ADDR 0x2E
#define TMR1_OCR1C_ADDR 0x30
#define TMR1_TIMSK1_ADDR 0x33
#define TMR1_TIFR1_ADDR 0x32
// ADC Multiplexer
#define ADMUX_BASE 0x12
#define ADMUX_ADMUX_ADDR 0x12
#define ADMUX_ADMUX_MUX_BIT 0  // Analog Channel Selection
#define ADMUX_ADMUX_ADLAR_BIT 5  // ADC Left Adjust Result
#define ADMUX_ADMUX_REFS_BIT 0  // Reference Selection
#define ADMUX_ADCSRA_ADDR 0x13
#define ADMUX_ADCSRA_ADPS_BIT 0  // ADC Prescaler Select
#define ADMUX_ADCSRA_ADIE_BIT 3  // ADC Interrupt Enable
#define ADMUX_ADCSRA_ADIF_BIT 4  // ADC Interrupt Flag
#define ADMUX_ADCSRA_ADATE_BIT 5  // ADC Auto Trigger Enable
#define ADMUX_ADCSRA_ADSC_BIT 6  // ADC Start Conversion
#define ADMUX_ADCSRA_ADEN_BIT 7  // ADC Enable
#define ADMUX_ADCH_ADDR 0x14
#define ADMUX_ADCL_ADDR 0x15
// Universal Serial Interface
#define USI_BASE 0x18
#define USI_USIDR_ADDR 0x18
#define USI_USISR_ADDR 0x19
#define USI_USISR_USICNT_BIT 0  // Counter
#define USI_USISR_USIDC_BIT 4  // Data Register
#define USI_USISR_USIPF_BIT 5  // Stop Cond Flag
#define USI_USISR_USIOV_BIT 6  // Overflow Flag
#define USI_USISR_USISIF_BIT 7  // Start Cond Interrupt Flag
#define USI_USICR_ADDR 0x1A
#define USI_USICR_USICS_BIT 0  // Clock Source Select
#define USI_USICR_USISCL_BIT 2  // SCL strobe
#define USI_USICR_USIOW_BIT 3  // SDA output override
#define USI_USICR_USIOE_BIT 4  // Output Enable
#define USI_USICR_USISRE_BIT 5  // Start Recognition Enable
#define USI_USICR_USIORE_BIT 6  // Stop Recognition Enable
#define USI_USICR_USIGIE_BIT 7  // Global Interrupt Enable
#define USI_USIPORT_ADDR 0x1B
// MCU Control
#define MCUCR_BASE 0x35
#define MCUCR_MCUCR_ADDR 0x35
#define MCUCR_MCUCR_ISC_BIT 0  // Interrupt Sense Control
#define MCUCR_MCUCR_SE_BIT 4  // Sleep Enable
#define MCUCR_MCUCR_SM_BIT 0  // Sleep Mode
#define MCUCR_MCUCSR_ADDR 0x36
#define MCUCR_MCUCSR_PORF_BIT 0  // Power-on Reset Flag
#define MCUCR_MCUCSR_EXTRF_BIT 1  // External Reset Flag
#define MCUCR_MCUCSR_WDRF_BIT 2  // Watchdog Reset Flag
#define MCUCR_MCUCSR_BORF_BIT 4  // Brown-out Reset Flag
// Watchdog Timer
#define WDTCR_BASE 0x21
#define WDTCR_WDTCR_ADDR 0x21
#define WDTCR_WDTCR_WDP_BIT 0  // Watchdog Prescaler
#define WDTCR_WDTCR_WDE_BIT 3  // Watchdog Enable
#define WDTCR_WDTCR_WDIE_BIT 4  // Watchdog Interrupt Enable
// EEPROM
#define EEPR_BASE 0x1C
#define EEPR_EEAR_ADDR 0x1E
#define EEPR_EEDR_ADDR 0x1D
#define EEPR_EECR_ADDR 0x1F
#define EEPR_EECR_EEPM_BIT 0  // EEPROM Programming Mode
#define EEPR_EECR_EERIE_BIT 3  // EEPROM Ready Interrupt Enable
#define EEPR_EECR_EEWE_BIT 2  // EEPROM Write Enable
#define EEPR_EECR_EEMWE_BIT 1  // EEPROM Master Write Enable
#define EEPR_EECR_EERE_BIT 0  // EEPROM Read Enable
// External Interrupt
#define GIMSK_BASE 0x3B
#define GIMSK_GIMSK_ADDR 0x3B
#define GIMSK_GIMSK_INT0_BIT 0  // External Interrupt Request 0 Enable
#define GIMSK_GIMSK_PCIE_BIT 1  // Pin Change Interrupt Enable
#define GIMSK_GIFR_ADDR 0x3C
#define GIMSK_GIFR_INTF0_BIT 0  // External Interrupt Flag 0
#define GIMSK_GIFR_PCIF_BIT 1  // Pin Change Interrupt Flag
// Pin Change Mask
#define PCMSK_BASE 0x15
#define PCMSK_PCMSK_ADDR 0x15
// Store Program Memory
#define SPMCSR_BASE 0x37
#define SPMCSR_SPMCSR_ADDR 0x37
#define SPMCSR_SPMCSR_SPMCR_BIT 0  // SPM Mode
#define SPMCSR_SPMCSR_PGERS_BIT 1  // Page Erase
#define SPMCSR_SPMCSR_PGWRT_BIT 2  // Page Write
#define SPMCSR_SPMCSR_BLBSET_BIT 3  // Boot Lock Bits Set
#define SPMCSR_SPMCSR_RWWSRE_BIT 4  // Read-While-Read Strobe Enable
#define SPMCSR_SPMCSR_SIGRD_BIT 5  // Signature Row Read
#define SPMCSR_SPMCSR_SPMEN_BIT 7  // SPM Enable

// 中断向量定义
#define INT_RESET 0  // External Reset, Power-on Reset, Brown-out Reset
#define INT_INT0 1  // External Interrupt Request 0
#define INT_PCINT0 2  // Pin Change
#define INT_WDT 3  // Watchdog Timeout
#define INT_TIM1_COMPA 4  // Timer/Counter1 Compare Match A
#define INT_TIM1_OVF 5  // Timer/Counter1 Overflow
#define INT_TIM0_COMPA 6  // Timer/Counter0 Compare Match A
#define INT_TIM0_OVF 7  // Timer/Counter0 Overflow
#define INT_SPI_STC 8  // SPI Serial Transfer Complete
#define INT_ADC 9  // ADC Conversion Complete
#define INT_USI_START 10  // USI Start Condition
#define INT_USI_OVF 11  // USI Overflow
#define INT_EE_READY 12  // EEPROM Ready

// 引脚定义
#define PIN_PB5 1  // RESET - ADC0 - dW
#define PIN_PB3 2  // XTAL1 - CLKI - ADC3
#define PIN_PB4 3  // XTAL2 - ADC2
#define PIN_PB0 4  // MOSI - AI - ADC0 - T0 - INT0
#define PIN_PB1 5  // MISO - AI - ADC1 - OC1A - INT1
#define PIN_PB2 6  // SCK - AI - ADC3 - OC1B
#define PIN_VCC 7  // Supply Voltage
#define PIN_GND 8  // Ground

#endif /* ATTINY85_DEVICE_H */
