(*
 * ATmega32U4梯形图定义
 * 生成自: Atmel/AVR/ATmega32U4
 * 版本: 1.0
 * 日期: 2026-04-28
 * 作者: VML Team
 * 描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
 * CPU架构: AVR
 * 位宽: 8位
 * 时钟频率: 16000000 Hz
 *)

PROGRAM ATmega32U4
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : BYTE; (*  *)
    R1 AT %M0x01 : BYTE; (*  *)
    R2 AT %M0x02 : BYTE; (*  *)
    R3 AT %M0x03 : BYTE; (*  *)
    R4 AT %M0x04 : BYTE; (*  *)
    R5 AT %M0x05 : BYTE; (*  *)
    R6 AT %M0x06 : BYTE; (*  *)
    R7 AT %M0x07 : BYTE; (*  *)
    R8 AT %M0x08 : BYTE; (*  *)
    R9 AT %M0x09 : BYTE; (*  *)
    R10 AT %M0x0A : BYTE; (*  *)
    R11 AT %M0x0B : BYTE; (*  *)
    R12 AT %M0x0C : BYTE; (*  *)
    R13 AT %M0x0D : BYTE; (*  *)
    R14 AT %M0x0E : BYTE; (*  *)
    R15 AT %M0x0F : BYTE; (*  *)
    R16 AT %M0x10 : BYTE; (*  *)
    R17 AT %M0x11 : BYTE; (*  *)
    R18 AT %M0x12 : BYTE; (*  *)
    R19 AT %M0x13 : BYTE; (*  *)
    R20 AT %M0x14 : BYTE; (*  *)
    R21 AT %M0x15 : BYTE; (*  *)
    R22 AT %M0x16 : BYTE; (*  *)
    R23 AT %M0x17 : BYTE; (*  *)
    R24 AT %M0x18 : BYTE; (*  *)
    R25 AT %M0x19 : BYTE; (*  *)
    R26 AT %M0x1A : BYTE; (*  *)
    R27 AT %M0x1B : BYTE; (*  *)
    R28 AT %M0x1C : BYTE; (*  *)
    R29 AT %M0x1D : BYTE; (*  *)
    R30 AT %M0x1E : BYTE; (*  *)
    R31 AT %M0x1F : BYTE; (*  *)
    SPL AT %M0x5D : BYTE; (*  *)
    SPH AT %M0x5E : BYTE; (*  *)
    SREG AT %M0x5F : BYTE; (*  *)

    (* 外设变量 *)
    (* Port B *)
    PORTB_BASE AT %MW0x23 : WORD;
    PORTB_PORTB AT %MW0x25 : BYTE;
    PORTB_DDRB AT %MW0x24 : BYTE;
    PORTB_PINB AT %MW0x23 : BYTE;
    (* Port C *)
    PORTC_BASE AT %MW0x26 : WORD;
    PORTC_PORTC AT %MW0x28 : BYTE;
    PORTC_DDRC AT %MW0x27 : BYTE;
    PORTC_PINC AT %MW0x26 : BYTE;
    (* Port D *)
    PORTD_BASE AT %MW0x29 : WORD;
    PORTD_PORTD AT %MW0x2B : BYTE;
    PORTD_DDRD AT %MW0x2A : BYTE;
    PORTD_PIND AT %MW0x29 : BYTE;
    (* Port E *)
    PORTE_BASE AT %MW0x2C : WORD;
    PORTE_PORTE AT %MW0x2E : BYTE;
    PORTE_DDRE AT %MW0x2D : BYTE;
    PORTE_PINE AT %MW0x2C : BYTE;
    (* USART1 *)
    UART1_BASE AT %MW0xC8 : WORD;
    UART1_UDR1 AT %MW0xCE : BYTE;
    UART1_UCSR1A AT %MW0xC8 : BYTE;
    UART1_UCSR1B AT %MW0xC9 : BYTE;
    UART1_UCSR1C AT %MW0xCA : BYTE;
    UART1_UBRR1 AT %MW0xCC : WORD;
    (* USB Controller *)
    USB_BASE AT %MW0xD0 : WORD;
    USB_UDCON AT %MW0xD0 : BYTE;
    USB_UDIEN AT %MW0xD1 : BYTE;
    USB_UDINT AT %MW0xD2 : BYTE;

    (* 中断变量 *)
    INT_INT0 : BOOL; (* External Interrupt 0 *)
    INT_INT1 : BOOL; (* External Interrupt 1 *)
    INT_INT2 : BOOL; (* External Interrupt 2 *)
    INT_INT3 : BOOL; (* External Interrupt 3 *)
    INT_INT4 : BOOL; (* External Interrupt 4 *)
    INT_INT5 : BOOL; (* External Interrupt 5 *)
    INT_INT6 : BOOL; (* External Interrupt 6 *)
    INT_PCINT0 : BOOL; (* Pin Change Interrupt 0 *)
    INT_USB_GENERAL : BOOL; (* USB General *)
    INT_USB_ENDPOINT : BOOL; (* USB Endpoint *)
    INT_WDT : BOOL; (* Watchdog Timeout *)
    INT_TIMER1_CAPT : BOOL; (* Timer1 Capture *)
    INT_TIMER1_COMPA : BOOL; (* Timer1 Compare A *)
    INT_TIMER1_COMPB : BOOL; (* Timer1 Compare B *)
    INT_TIMER1_OVF : BOOL; (* Timer1 Overflow *)
    INT_TIMER0_COMPA : BOOL; (* Timer0 Compare A *)
    INT_TIMER0_COMPB : BOOL; (* Timer0 Compare B *)
    INT_TIMER0_OVF : BOOL; (* Timer0 Overflow *)
    INT_SPI_STC : BOOL; (* SPI Transfer Complete *)
    INT_UART1_RX : BOOL; (* UART1 Receive *)
    INT_UART1_UDRE : BOOL; (* UART1 Data Register Empty *)
    INT_UART1_TX : BOOL; (* UART1 Transmit *)
    INT_ADC : BOOL; (* ADC Conversion Complete *)

END_VAR

BEGIN
    (* ATmega32U4初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_INT0|--[中断处理]--
    --|INT_INT1|--[中断处理]--
    --|INT_INT2|--[中断处理]--
    --|INT_INT3|--[中断处理]--
    --|INT_INT4|--[中断处理]--
    --|INT_INT5|--[中断处理]--
    --|INT_INT6|--[中断处理]--
    --|INT_PCINT0|--[中断处理]--
    --|INT_USB_GENERAL|--[中断处理]--
    --|INT_USB_ENDPOINT|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_TIMER1_CAPT|--[中断处理]--
    --|INT_TIMER1_COMPA|--[中断处理]--
    --|INT_TIMER1_COMPB|--[中断处理]--
    --|INT_TIMER1_OVF|--[中断处理]--
    --|INT_TIMER0_COMPA|--[中断处理]--
    --|INT_TIMER0_COMPB|--[中断处理]--
    --|INT_TIMER0_OVF|--[中断处理]--
    --|INT_SPI_STC|--[中断处理]--
    --|INT_UART1_RX|--[中断处理]--
    --|INT_UART1_UDRE|--[中断处理]--
    --|INT_UART1_TX|--[中断处理]--
    --|INT_ADC|--[中断处理]--

END_PROGRAM
