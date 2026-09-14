//
// PIC16F877A Register Definitions
// Generated from: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
// Version: 1.0
// Date: 2026-04-16
//

import Foundation

// MARK: - GPIO_PORTB (Port B)
let GPIO_PORTB_PORTB: UInt8 = 0x0x06
let GPIO_PORTB_TRISB: UInt8 = 0x0x86

// MARK: - GPIO_PORTC (Port C)
let GPIO_PORTC_PORTC: UInt8 = 0x0x07
let GPIO_PORTC_TRISC: UInt8 = 0x0x87

// MARK: - GPIO_PORTD (Port D)
let GPIO_PORTD_PORTD: UInt8 = 0x0x08
let GPIO_PORTD_TRISD: UInt8 = 0x0x88

// MARK: - TIMER0 (Timer 0)
let TIMER0_TMR0: UInt8 = 0x0x01
let TIMER0_OPTION_REG: UInt8 = 0x0x81

// MARK: - TIMER1 (Timer 1)
let TIMER1_T1CON: UInt8 = 0x0x10
let TIMER1_TMR1L: UInt8 = 0x0x0E
let TIMER1_TMR1H: UInt8 = 0x0x0F

// MARK: - TIMER2 (Timer 2)
let TIMER2_T2CON: UInt8 = 0x0x12
let TIMER2_TMR2: UInt8 = 0x0x11
let TIMER2_PR2: UInt8 = 0x0x92

// MARK: - ADC (A/D Converter)
let ADC_ADRESH: UInt8 = 0x0x1E
let ADC_ADRESL: UInt8 = 0x0x9F
let ADC_ADCON0: UInt8 = 0x0x1F
let ADC_ADCON1: UInt8 = 0x0x9F

// MARK: - MSSP (Master Synchronous Serial Port)
let MSSP_SSPSTAT: UInt8 = 0x0x94
let MSSP_SSPCON: UInt8 = 0x0x14
let MSSP_SSPBUF: UInt8 = 0x0x13

// MARK: - USART (USART)
let USART_TXREG: UInt8 = 0x0x19
let USART_RCREG: UInt8 = 0x0x1A
let USART_SPBRG: UInt8 = 0x0x99
let USART_TXSTA: UInt8 = 0x0x98
let USART_RCSTA: UInt8 = 0x0x18

// MARK: - CCP1 (Capture/Compare/PWM 1)
let CCP1_CCP1CON: UInt8 = 0x0x17
let CCP1_CCPR1L: UInt8 = 0x0x15
let CCP1_CCPR1H: UInt8 = 0x0x16

// MARK: - CCP2 (Capture/Compare/PWM 2)
let CCP2_CCP2CON: UInt8 = 0x0x1D
let CCP2_CCPR2L: UInt8 = 0x0x1B
let CCP2_CCPR2H: UInt8 = 0x0x1C

// MARK: - Interrupt Vectors
let IRQ_INT: Int = 1
let IRQ_TMR0: Int = 2
let IRQ_RB: Int = 3
let IRQ_CCP1: Int = 4
let IRQ_CCP2: Int = 5
let IRQ_TMR1: Int = 6
let IRQ_TMR2: Int = 8
let IRQ_SPI: Int = 9
let IRQ_SCI: Int = 10
let IRQ_SCI: Int = 11
let IRQ_ADC: Int = 12
let IRQ_EEPROM: Int = 13

// MARK: - Memory Segments
let MEM_program: (start: UInt32, size: UInt32) = (0x0x0000, 8192)
let MEM_data: (start: UInt32, size: UInt32) = (0x0x20, 96)
let MEM_sram: (start: UInt32, size: UInt32) = (0x0xA0, 96)
let MEM_eeprom: (start: UInt32, size: UInt32) = (0x0x2100, 256)

// MARK: - Device Functions
func pic16f877a_init() {
}

func read_register(_ addr: UInt32) -> UInt32 {
    return 0
}

func write_register(_ addr: UInt32, _ value: UInt32) {
}
