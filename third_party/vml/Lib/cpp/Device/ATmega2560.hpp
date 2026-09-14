#ifndef ATMEGA2560_HPP
#define ATMEGA2560_HPP

// ATmega2560寄存器定义
// 生成自: Atmel/AVR/ATmega2560
// 版本: 1.0
// 日期: 2026-04-28


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 16000000 Hz

// 寄存器定义
#define R0 (*(volatile uint8_t*)0x00)

#define R1 (*(volatile uint8_t*)0x01)

#define R2 (*(volatile uint8_t*)0x02)

#define SPL (*(volatile uint8_t*)0x5D)

#define SPH (*(volatile uint8_t*)0x5E)

#define SREG (*(volatile uint8_t*)0x5F)

// 内存段定义
#define FLASH_START 0x0000
#define FLASH_END 0x3FFFF
#define FLASH_SIZE 262144

#define SRAM_START 0x0200
#define SRAM_END 0x21FF
#define SRAM_SIZE 8192

#define EEPROM_START 0x0000
#define EEPROM_END 0x0FFF
#define EEPROM_SIZE 4096

#define IO_START 0x00
#define IO_END 0x3F
#define IO_SIZE 64

#define EXTIO_START 0x40
#define EXTIO_END 0xFF
#define EXTIO_SIZE 192

// 外设定义
// Port A
#define PORTA_BASE 0x22
#define PORTA_DDRA (*(volatile uint8_t*)0x00000043)
#define PORTA_PORTA (*(volatile uint8_t*)0x00000044)
#define PORTA_PINA (*(volatile uint8_t*)0x00000042)

// Port B
#define PORTB_BASE 0x25
#define PORTB_DDRB (*(volatile uint8_t*)0x00000049)
#define PORTB_PORTB (*(volatile uint8_t*)0x0000004A)
#define PORTB_PINB (*(volatile uint8_t*)0x00000048)

// Port C
#define PORTC_BASE 0x28
#define PORTC_DDRC (*(volatile uint8_t*)0x0000004F)
#define PORTC_PORTC (*(volatile uint8_t*)0x00000050)
#define PORTC_PINC (*(volatile uint8_t*)0x0000004E)

// Port D
#define PORTD_BASE 0x2B
#define PORTD_DDRD (*(volatile uint8_t*)0x00000055)
#define PORTD_PORTD (*(volatile uint8_t*)0x00000056)
#define PORTD_PIND (*(volatile uint8_t*)0x00000054)

// Port E
#define PORTE_BASE 0x2E
#define PORTE_DDRE (*(volatile uint8_t*)0x0000005B)
#define PORTE_PORTE (*(volatile uint8_t*)0x0000005C)
#define PORTE_PINE (*(volatile uint8_t*)0x0000005A)

// Port F
#define PORTF_BASE 0x31
#define PORTF_DDRF (*(volatile uint8_t*)0x00000061)
#define PORTF_PORTF (*(volatile uint8_t*)0x00000062)
#define PORTF_PINF (*(volatile uint8_t*)0x00000060)

// Port G
#define PORTG_BASE 0x34
#define PORTG_DDRG (*(volatile uint8_t*)0x00000067)
#define PORTG_PORTG (*(volatile uint8_t*)0x00000068)
#define PORTG_PING (*(volatile uint8_t*)0x00000066)

// USART 0
#define USART0_BASE 0xC0
#define USART0_UDR0 (*(volatile uint8_t*)0x00000186)
#define USART0_UCSR0A (*(volatile uint8_t*)0x00000180)
#define USART0_UCSR0B (*(volatile uint8_t*)0x00000181)
#define USART0_UCSR0C (*(volatile uint8_t*)0x00000182)
#define USART0_UBRR0L (*(volatile uint8_t*)0x00000184)
#define USART0_UBRR0H (*(volatile uint8_t*)0x00000185)

// 中断向量定义
#define RESET_VECTOR 1  // 
#define INT0_VECTOR 2  // 
#define INT1_VECTOR 3  // 
#define INT2_VECTOR 4  // 
#define INT3_VECTOR 5  // 
#define INT4_VECTOR 6  // 
#define INT5_VECTOR 7  // 
#define INT6_VECTOR 8  // 
#define INT7_VECTOR 9  // 
#define PCINT0_VECTOR 10  // 
#define PCINT1_VECTOR 11  // 
#define PCINT2_VECTOR 12  // 
#define WDT_VECTOR 13  // 
#define TIM2_COMPA_VECTOR 14  // 
#define TIM2_COMPB_VECTOR 15  // 
#define TIM2_OVF_VECTOR 16  // 
#define TIM1_CAPT_VECTOR 17  // 
#define TIM1_COMPA_VECTOR 18  // 
#define TIM1_COMPB_VECTOR 19  // 
#define TIM1_OVF_VECTOR 20  // 
#define TIM0_COMPA_VECTOR 21  // 
#define TIM0_COMPB_VECTOR 22  // 
#define TIM0_OVF_VECTOR 23  // 
#define SPI_STC_VECTOR 24  // 
#define USART0_RX_VECTOR 25  // 
#define USART0_UDRE_VECTOR 26  // 
#define USART0_TX_VECTOR 27  // 

void atmega2560_init(void);

#ifdef __cplusplus
}
#endif

#endif // ATMEGA2560_HPP
