' ATtiny85寄存器定义
' 生成自: Microchip/AVR/ATtiny85
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM

' CPU架构: AVR
' 位宽: 8位
' 时钟频率: 1000000 Hz

' 寄存器定义
' General Purpose Register 0
CONST R0 = 0x00

' General Purpose Register 1
CONST R1 = 0x01

' General Purpose Register 2
CONST R2 = 0x02

' General Purpose Register 3
CONST R3 = 0x03

' General Purpose Register 4
CONST R4 = 0x04

' General Purpose Register 5
CONST R5 = 0x05

' General Purpose Register 6
CONST R6 = 0x06

' General Purpose Register 7
CONST R7 = 0x07

' General Purpose Register 8
CONST R8 = 0x08

' General Purpose Register 9
CONST R9 = 0x09

' General Purpose Register 10
CONST R10 = 0x0A

' General Purpose Register 11
CONST R11 = 0x0B

' General Purpose Register 12
CONST R12 = 0x0C

' General Purpose Register 13
CONST R13 = 0x0D

' General Purpose Register 14
CONST R14 = 0x0E

' General Purpose Register 15
CONST R15 = 0x0F

' General Purpose Register 16
CONST R16 = 0x10

' General Purpose Register 17
CONST R17 = 0x11

' General Purpose Register 18
CONST R18 = 0x12

' General Purpose Register 19
CONST R19 = 0x13

' General Purpose Register 20
CONST R20 = 0x14

' General Purpose Register 21
CONST R21 = 0x15

' General Purpose Register 22
CONST R22 = 0x16

' General Purpose Register 23
CONST R23 = 0x17

' General Purpose Register 24
CONST R24 = 0x18

' General Purpose Register 25
CONST R25 = 0x19

' Register pair X (R27:R26)
CONST X = 0x1A

' Register pair Y (R29:R28)
CONST Y = 0x1C

' Register pair Z (R31:R30)
CONST Z = 0x1E

' Stack Pointer
CONST SP = 0x3D

' Status Register
CONST SREG = 0x3F
CONST SREG_C = 0  ' Carry Flag
CONST SREG_Z = 1  ' Zero Flag
CONST SREG_N = 2  ' Negative Flag
CONST SREG_V = 3  ' Two's Complement Overflow Flag
CONST SREG_S = 4  ' Sign Flag (N xor V)
CONST SREG_H = 5  ' Half Carry Flag
CONST SREG_T = 6  ' Transfer Bit
CONST SREG_I = 7  ' Global Interrupt Enable

' 内存段定义
' Program Flash (8KB)
CONST FLASH_START = 0x0000
CONST FLASH_END = 0x1FFF
CONST FLASH_SIZE = 8192

' Internal SRAM (512B)
CONST SRAM_START = 0x0060
CONST SRAM_END = 0x025F
CONST SRAM_SIZE = 512

' EEPROM (512B)
CONST EEPROM_START = 0x0000
CONST EEPROM_END = 0x01FF
CONST EEPROM_SIZE = 512

' I/O Registers
CONST IO_START = 0x00
CONST IO_END = 0x3F
CONST IO_SIZE = 64

' 外设定义
' Port A
CONST PORTA_BASE = 0x20
CONST PORTA_PINA = 0x20
CONST PORTA_DDRA = 0x21
CONST PORTA_PORTA = 0x22

' Port B
CONST PORTB_BASE = 0x18
CONST PORTB_PINB = 0x16
CONST PORTB_DDRB = 0x17
CONST PORTB_PORTB = 0x18

' Timer/Counter0
CONST TIPO_BASE = 0x20
CONST TIPO_TCCR0A = 0x20
CONST TIPO_TCCR0A_WGM00 = 0  ' Waveform Generation Mode
CONST TIPO_TCCR0A_WGM01 = 1  ' Waveform Generation Mode
CONST TIPO_TCCR0A_COM0B0 = 4  ' Compare Output Mode B
CONST TIPO_TCCR0A_COM0B1 = 5  ' Compare Output Mode B
CONST TIPO_TCCR0A_COM0A0 = 6  ' Compare Output Mode A
CONST TIPO_TCCR0A_COM0A1 = 7  ' Compare Output Mode A
CONST TIPO_TCCR0B = 0x21
CONST TIPO_TCCR0B_CS00 = 0  ' Clock Select
CONST TIPO_TCCR0B_CS01 = 1  ' Clock Select
CONST TIPO_TCCR0B_CS02 = 2  ' Clock Select
CONST TIPO_TCCR0B_WGM02 = 3  ' Waveform Generation Mode
CONST TIPO_TCCR0B_FOC0B = 6  ' Force Output Compare B
CONST TIPO_TCCR0B_FOC0A = 7  ' Force Output Compare A
CONST TIPO_TCNT0 = 0x22
CONST TIPO_OCR0A = 0x23
CONST TIPO_OCR0B = 0x24
CONST TIPO_TIMSK = 0x39
CONST TIPO_TIMSK_TOIE0 = 0  ' Timer/Counter0 Overflow Interrupt Enable
CONST TIPO_TIMSK_OCIE0A = 1  ' Output Compare A Match Interrupt Enable
CONST TIPO_TIMSK_OCIE0B = 2  ' Output Compare B Match Interrupt Enable
CONST TIPO_TIFR = 0x38
CONST TIPO_TIFR_TOV0 = 0  ' Timer/Counter0 Overflow Flag
CONST TIPO_TIFR_OCF0A = 1  ' Output Compare A Flag
CONST TIPO_TIFR_OCF0B = 2  ' Output Compare B Flag

' Timer/Counter1
CONST TMR1_BASE = 0x28
CONST TMR1_TCCR1A = 0x28
CONST TMR1_TCCR1A_PCM1 = 0  ' PWM Mode
CONST TMR1_TCCR1A_COM1A = 0  ' Compare Output Mode A
CONST TMR1_TCCR1A_COM1B = 0  ' Compare Output Mode B
CONST TMR1_TCCR1A_WG13 = 1  ' Waveform Generation Mode
CONST TMR1_TCCR1A_WG10 = 0  ' Waveform Generation Mode
CONST TMR1_TCCR1B = 0x29
CONST TMR1_TCCR1B_CTC1 = 7  ' Clear Timer on Compare
CONST TMR1_TCCR1B_WGM13 = 4  ' Waveform Generation Mode
CONST TMR1_TCCR1B_WGM12 = 3  ' Waveform Generation Mode
CONST TMR1_TCCR1B_CS1 = 0  ' Clock Select
CONST TMR1_TCNT1 = 0x2A
CONST TMR1_OCR1A = 0x2C
CONST TMR1_OCR1B = 0x2E
CONST TMR1_OCR1C = 0x30
CONST TMR1_TIMSK1 = 0x33
CONST TMR1_TIFR1 = 0x32

' ADC Multiplexer
CONST ADMUX_BASE = 0x12
CONST ADMUX_ADMUX = 0x12
CONST ADMUX_ADMUX_MUX = 0  ' Analog Channel Selection
CONST ADMUX_ADMUX_ADLAR = 5  ' ADC Left Adjust Result
CONST ADMUX_ADMUX_REFS = 0  ' Reference Selection
CONST ADMUX_ADCSRA = 0x13
CONST ADMUX_ADCSRA_ADPS = 0  ' ADC Prescaler Select
CONST ADMUX_ADCSRA_ADIE = 3  ' ADC Interrupt Enable
CONST ADMUX_ADCSRA_ADIF = 4  ' ADC Interrupt Flag
CONST ADMUX_ADCSRA_ADATE = 5  ' ADC Auto Trigger Enable
CONST ADMUX_ADCSRA_ADSC = 6  ' ADC Start Conversion
CONST ADMUX_ADCSRA_ADEN = 7  ' ADC Enable
CONST ADMUX_ADCH = 0x14
CONST ADMUX_ADCL = 0x15

' Universal Serial Interface
CONST USI_BASE = 0x18
CONST USI_USIDR = 0x18
CONST USI_USISR = 0x19
CONST USI_USISR_USICNT = 0  ' Counter
CONST USI_USISR_USIDC = 4  ' Data Register
CONST USI_USISR_USIPF = 5  ' Stop Cond Flag
CONST USI_USISR_USIOV = 6  ' Overflow Flag
CONST USI_USISR_USISIF = 7  ' Start Cond Interrupt Flag
CONST USI_USICR = 0x1A
CONST USI_USICR_USICS = 0  ' Clock Source Select
CONST USI_USICR_USISCL = 2  ' SCL strobe
CONST USI_USICR_USIOW = 3  ' SDA output override
CONST USI_USICR_USIOE = 4  ' Output Enable
CONST USI_USICR_USISRE = 5  ' Start Recognition Enable
CONST USI_USICR_USIORE = 6  ' Stop Recognition Enable
CONST USI_USICR_USIGIE = 7  ' Global Interrupt Enable
CONST USI_USIPORT = 0x1B

' MCU Control
CONST MCUCR_BASE = 0x35
CONST MCUCR_MCUCR = 0x35
CONST MCUCR_MCUCR_ISC = 0  ' Interrupt Sense Control
CONST MCUCR_MCUCR_SE = 4  ' Sleep Enable
CONST MCUCR_MCUCR_SM = 0  ' Sleep Mode
CONST MCUCR_MCUCSR = 0x36
CONST MCUCR_MCUCSR_PORF = 0  ' Power-on Reset Flag
CONST MCUCR_MCUCSR_EXTRF = 1  ' External Reset Flag
CONST MCUCR_MCUCSR_WDRF = 2  ' Watchdog Reset Flag
CONST MCUCR_MCUCSR_BORF = 4  ' Brown-out Reset Flag

' Watchdog Timer
CONST WDTCR_BASE = 0x21
CONST WDTCR_WDTCR = 0x21
CONST WDTCR_WDTCR_WDP = 0  ' Watchdog Prescaler
CONST WDTCR_WDTCR_WDE = 3  ' Watchdog Enable
CONST WDTCR_WDTCR_WDIE = 4  ' Watchdog Interrupt Enable

' EEPROM
CONST EEPR_BASE = 0x1C
CONST EEPR_EEAR = 0x1E
CONST EEPR_EEDR = 0x1D
CONST EEPR_EECR = 0x1F
CONST EEPR_EECR_EEPM = 0  ' EEPROM Programming Mode
CONST EEPR_EECR_EERIE = 3  ' EEPROM Ready Interrupt Enable
CONST EEPR_EECR_EEWE = 2  ' EEPROM Write Enable
CONST EEPR_EECR_EEMWE = 1  ' EEPROM Master Write Enable
CONST EEPR_EECR_EERE = 0  ' EEPROM Read Enable

' External Interrupt
CONST GIMSK_BASE = 0x3B
CONST GIMSK_GIMSK = 0x3B
CONST GIMSK_GIMSK_INT0 = 0  ' External Interrupt Request 0 Enable
CONST GIMSK_GIMSK_PCIE = 1  ' Pin Change Interrupt Enable
CONST GIMSK_GIFR = 0x3C
CONST GIMSK_GIFR_INTF0 = 0  ' External Interrupt Flag 0
CONST GIMSK_GIFR_PCIF = 1  ' Pin Change Interrupt Flag

' Pin Change Mask
CONST PCMSK_BASE = 0x15
CONST PCMSK_PCMSK = 0x15

' Store Program Memory
CONST SPMCSR_BASE = 0x37
CONST SPMCSR_SPMCSR = 0x37
CONST SPMCSR_SPMCSR_SPMCR = 0  ' SPM Mode
CONST SPMCSR_SPMCSR_PGERS = 1  ' Page Erase
CONST SPMCSR_SPMCSR_PGWRT = 2  ' Page Write
CONST SPMCSR_SPMCSR_BLBSET = 3  ' Boot Lock Bits Set
CONST SPMCSR_SPMCSR_RWWSRE = 4  ' Read-While-Read Strobe Enable
CONST SPMCSR_SPMCSR_SIGRD = 5  ' Signature Row Read
CONST SPMCSR_SPMCSR_SPMEN = 7  ' SPM Enable

' 中断向量定义
CONST RESET_VECTOR = 0  ' External Reset, Power-on Reset, Brown-out Reset
CONST INT0_VECTOR = 1  ' External Interrupt Request 0
CONST PCINT0_VECTOR = 2  ' Pin Change
CONST WDT_VECTOR = 3  ' Watchdog Timeout
CONST TIM1_COMPA_VECTOR = 4  ' Timer/Counter1 Compare Match A
CONST TIM1_OVF_VECTOR = 5  ' Timer/Counter1 Overflow
CONST TIM0_COMPA_VECTOR = 6  ' Timer/Counter0 Compare Match A
CONST TIM0_OVF_VECTOR = 7  ' Timer/Counter0 Overflow
CONST SPI_STC_VECTOR = 8  ' SPI Serial Transfer Complete
CONST ADC_VECTOR = 9  ' ADC Conversion Complete
CONST USI_START_VECTOR = 10  ' USI Start Condition
CONST USI_OVF_VECTOR = 11  ' USI Overflow
CONST EE_READY_VECTOR = 12  ' EEPROM Ready

' 引脚定义
CONST PIN_PB5 = 1  ' RESET - ADC0 - dW
CONST PIN_PB3 = 2  ' XTAL1 - CLKI - ADC3
CONST PIN_PB4 = 3  ' XTAL2 - ADC2
CONST PIN_PB0 = 4  ' MOSI - AI - ADC0 - T0 - INT0
CONST PIN_PB1 = 5  ' MISO - AI - ADC1 - OC1A - INT1
CONST PIN_PB2 = 6  ' SCK - AI - ADC3 - OC1B
CONST PIN_VCC = 7  ' Supply Voltage
CONST PIN_GND = 8  ' Ground

' 设备初始化子程序
SUB attiny85_init()
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
