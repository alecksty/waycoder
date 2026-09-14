(*
 * ATmega2560梯形图定义
 * 生成自: Atmel/AVR/ATmega2560
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
 * CPU架构: AVR
 * 位宽: 8位
 * 时钟频率: 16000000 Hz
 *)

PROGRAM ATmega2560
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : BYTE; (*  *)
    R1 AT %M0x01 : BYTE; (*  *)
    R2 AT %M0x02 : BYTE; (*  *)
    SPL AT %M0x5D : BYTE; (*  *)
    SPH AT %M0x5E : BYTE; (*  *)
    SREG AT %M0x5F : BYTE; (*  *)

    (* 外设变量 *)
    (* Port A *)
    PORTA_BASE AT %MW0x22 : WORD;
    PORTA_DDRA AT %MW0x21 : BYTE;
    PORTA_PORTA AT %MW0x22 : BYTE;
    PORTA_PINA AT %MW0x20 : BYTE;
    (* Port B *)
    PORTB_BASE AT %MW0x25 : WORD;
    PORTB_DDRB AT %MW0x24 : BYTE;
    PORTB_PORTB AT %MW0x25 : BYTE;
    PORTB_PINB AT %MW0x23 : BYTE;
    (* Port C *)
    PORTC_BASE AT %MW0x28 : WORD;
    PORTC_DDRC AT %MW0x27 : BYTE;
    PORTC_PORTC AT %MW0x28 : BYTE;
    PORTC_PINC AT %MW0x26 : BYTE;
    (* Port D *)
    PORTD_BASE AT %MW0x2B : WORD;
    PORTD_DDRD AT %MW0x2A : BYTE;
    PORTD_PORTD AT %MW0x2B : BYTE;
    PORTD_PIND AT %MW0x29 : BYTE;
    (* Port E *)
    PORTE_BASE AT %MW0x2E : WORD;
    PORTE_DDRE AT %MW0x2D : BYTE;
    PORTE_PORTE AT %MW0x2E : BYTE;
    PORTE_PINE AT %MW0x2C : BYTE;
    (* Port F *)
    PORTF_BASE AT %MW0x31 : WORD;
    PORTF_DDRF AT %MW0x30 : BYTE;
    PORTF_PORTF AT %MW0x31 : BYTE;
    PORTF_PINF AT %MW0x2F : BYTE;
    (* Port G *)
    PORTG_BASE AT %MW0x34 : WORD;
    PORTG_DDRG AT %MW0x33 : BYTE;
    PORTG_PORTG AT %MW0x34 : BYTE;
    PORTG_PING AT %MW0x32 : BYTE;
    (* USART 0 *)
    USART0_BASE AT %MW0xC0 : WORD;
    USART0_UDR0 AT %MW0xC6 : BYTE;
    USART0_UCSR0A AT %MW0xC0 : BYTE;
    USART0_UCSR0B AT %MW0xC1 : BYTE;
    USART0_UCSR0C AT %MW0xC2 : BYTE;
    USART0_UBRR0L AT %MW0xC4 : BYTE;
    USART0_UBRR0H AT %MW0xC5 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (*  *)
    INT_INT0 : BOOL; (*  *)
    INT_INT1 : BOOL; (*  *)
    INT_INT2 : BOOL; (*  *)
    INT_INT3 : BOOL; (*  *)
    INT_INT4 : BOOL; (*  *)
    INT_INT5 : BOOL; (*  *)
    INT_INT6 : BOOL; (*  *)
    INT_INT7 : BOOL; (*  *)
    INT_PCINT0 : BOOL; (*  *)
    INT_PCINT1 : BOOL; (*  *)
    INT_PCINT2 : BOOL; (*  *)
    INT_WDT : BOOL; (*  *)
    INT_TIM2_COMPA : BOOL; (*  *)
    INT_TIM2_COMPB : BOOL; (*  *)
    INT_TIM2_OVF : BOOL; (*  *)
    INT_TIM1_CAPT : BOOL; (*  *)
    INT_TIM1_COMPA : BOOL; (*  *)
    INT_TIM1_COMPB : BOOL; (*  *)
    INT_TIM1_OVF : BOOL; (*  *)
    INT_TIM0_COMPA : BOOL; (*  *)
    INT_TIM0_COMPB : BOOL; (*  *)
    INT_TIM0_OVF : BOOL; (*  *)
    INT_SPI_STC : BOOL; (*  *)
    INT_USART0_RX : BOOL; (*  *)
    INT_USART0_UDRE : BOOL; (*  *)
    INT_USART0_TX : BOOL; (*  *)

END_VAR

BEGIN
    (* ATmega2560初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|SPL>0|--(监控灯)--
    --|SPH>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_INT0|--[中断处理]--
    --|INT_INT1|--[中断处理]--
    --|INT_INT2|--[中断处理]--
    --|INT_INT3|--[中断处理]--
    --|INT_INT4|--[中断处理]--
    --|INT_INT5|--[中断处理]--
    --|INT_INT6|--[中断处理]--
    --|INT_INT7|--[中断处理]--
    --|INT_PCINT0|--[中断处理]--
    --|INT_PCINT1|--[中断处理]--
    --|INT_PCINT2|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_TIM2_COMPA|--[中断处理]--
    --|INT_TIM2_COMPB|--[中断处理]--
    --|INT_TIM2_OVF|--[中断处理]--
    --|INT_TIM1_CAPT|--[中断处理]--
    --|INT_TIM1_COMPA|--[中断处理]--
    --|INT_TIM1_COMPB|--[中断处理]--
    --|INT_TIM1_OVF|--[中断处理]--
    --|INT_TIM0_COMPA|--[中断处理]--
    --|INT_TIM0_COMPB|--[中断处理]--
    --|INT_TIM0_OVF|--[中断处理]--
    --|INT_SPI_STC|--[中断处理]--
    --|INT_USART0_RX|--[中断处理]--
    --|INT_USART0_UDRE|--[中断处理]--
    --|INT_USART0_TX|--[中断处理]--

END_PROGRAM
