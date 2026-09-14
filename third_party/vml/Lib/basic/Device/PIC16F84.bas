' PIC16F84寄存器定义
' 生成自: Microchip Technology/PIC16/PIC16F84
' 版本: 
' 日期: 
' 作者: 
' 描述: Microchip PIC16F84 8-bit microcontroller with EEPROM

' CPU架构: PIC16
' 位宽: 0位
' 时钟频率: 0 Hz

' 外设定义
' 8-bit timer/counter with prescaler
CONST TIMER0_BASE = 
CONST TIMER0_TMR0 = 0x01

' 16-bit timer/counter with prescaler
CONST TIMER1_BASE = 
CONST TIMER1_TMR1L = 0x0E
CONST TIMER1_TMR1H = 0x0F
CONST TIMER1_T1CON = 0x10
CONST TIMER1_T1CON_TMR1ON = 0  ' Timer1 On
CONST TIMER1_T1CON_TMR1CS = 1  ' Timer1 Clock Source
CONST TIMER1_T1CON_T1SYNC = 2  ' Timer1 External Clock Input Synchronization
CONST TIMER1_T1CON_T1OSCEN = 3  ' Timer1 Oscillator Enable
CONST TIMER1_T1CON_T1CKPS0 = 4  ' Timer1 Input Clock Prescale Select bit 0
CONST TIMER1_T1CON_T1CKPS1 = 5  ' Timer1 Input Clock Prescale Select bit 1

' Watchdog Timer
CONST WATCHDOG_BASE = 
CONST WATCHDOG_WDTCON = 0x07
CONST WATCHDOG_WDTCON_SWDTEN = 0  ' Software Watchdog Timer Enable

' 64-byte EEPROM data memory
CONST EEPROM_BASE = 
CONST EEPROM_EEDATA = 0x08
CONST EEPROM_EEADR = 0x09
CONST EEPROM_EECON1 = 0x88
CONST EEPROM_EECON2 = 0x89

' General Purpose I/O
CONST GPIO_BASE = 
CONST GPIO_PORTA = 0x05
CONST GPIO_PORTB = 0x06
CONST GPIO_TRISA = 0x85
CONST GPIO_TRISB = 0x86

' 中断向量定义
CONST INT_VECTOR = 4  ' External interrupt on RB0/INT pin
CONST TMR0_VECTOR = 4  ' Timer0 overflow interrupt
CONST PORTB_VECTOR = 4  ' PORTB change interrupt (RB4-RB7)
CONST EEPROM_VECTOR = 4  ' EEPROM write complete interrupt

' 设备初始化子程序
SUB pic16f84_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB
