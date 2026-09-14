# ATtiny85 设备定义 - R 脚本
# 生成自: Microchip/AVR/ATtiny85
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
# CPU架构: AVR
# 位宽: 8位
# 时钟频率: 1000000 Hz

# 寄存器地址定义
R0_ADDR <- 0x00  # General Purpose Register 0
R1_ADDR <- 0x01  # General Purpose Register 1
R2_ADDR <- 0x02  # General Purpose Register 2
R3_ADDR <- 0x03  # General Purpose Register 3
R4_ADDR <- 0x04  # General Purpose Register 4
R5_ADDR <- 0x05  # General Purpose Register 5
R6_ADDR <- 0x06  # General Purpose Register 6
R7_ADDR <- 0x07  # General Purpose Register 7
R8_ADDR <- 0x08  # General Purpose Register 8
R9_ADDR <- 0x09  # General Purpose Register 9
R10_ADDR <- 0x0A  # General Purpose Register 10
R11_ADDR <- 0x0B  # General Purpose Register 11
R12_ADDR <- 0x0C  # General Purpose Register 12
R13_ADDR <- 0x0D  # General Purpose Register 13
R14_ADDR <- 0x0E  # General Purpose Register 14
R15_ADDR <- 0x0F  # General Purpose Register 15
R16_ADDR <- 0x10  # General Purpose Register 16
R17_ADDR <- 0x11  # General Purpose Register 17
R18_ADDR <- 0x12  # General Purpose Register 18
R19_ADDR <- 0x13  # General Purpose Register 19
R20_ADDR <- 0x14  # General Purpose Register 20
R21_ADDR <- 0x15  # General Purpose Register 21
R22_ADDR <- 0x16  # General Purpose Register 22
R23_ADDR <- 0x17  # General Purpose Register 23
R24_ADDR <- 0x18  # General Purpose Register 24
R25_ADDR <- 0x19  # General Purpose Register 25
X_ADDR <- 0x1A  # Register pair X (R27:R26)
Y_ADDR <- 0x1C  # Register pair Y (R29:R28)
Z_ADDR <- 0x1E  # Register pair Z (R31:R30)
SP_ADDR <- 0x3D  # Stack Pointer
SREG_ADDR <- 0x3F  # Status Register
SREG_C_BIT <- 0  # Carry Flag
SREG_Z_BIT <- 1  # Zero Flag
SREG_N_BIT <- 2  # Negative Flag
SREG_V_BIT <- 3  # Two's Complement Overflow Flag
SREG_S_BIT <- 4  # Sign Flag (N xor V)
SREG_H_BIT <- 5  # Half Carry Flag
SREG_T_BIT <- 6  # Transfer Bit
SREG_I_BIT <- 7  # Global Interrupt Enable

# 内存段定义
FLASH_START <- 0x0000
FLASH_END <- 0x1FFF
FLASH_SIZE <- 8192  # Program Flash (8KB)
SRAM_START <- 0x0060
SRAM_END <- 0x025F
SRAM_SIZE <- 512  # Internal SRAM (512B)
EEPROM_START <- 0x0000
EEPROM_END <- 0x01FF
EEPROM_SIZE <- 512  # EEPROM (512B)
IO_START <- 0x00
IO_END <- 0x3F
IO_SIZE <- 64  # I/O Registers

# 外设定义
# Port A
PORTA_BASE <- 0x20
PORTA_PINA_ADDR <- 0x20
PORTA_DDRA_ADDR <- 0x21
PORTA_PORTA_ADDR <- 0x22
# Port B
PORTB_BASE <- 0x18
PORTB_PINB_ADDR <- 0x16
PORTB_DDRB_ADDR <- 0x17
PORTB_PORTB_ADDR <- 0x18
# Timer/Counter0
TIPO_BASE <- 0x20
TIPO_TCCR0A_ADDR <- 0x20
TIPO_TCCR0A_WGM00_BIT <- 0  # Waveform Generation Mode
TIPO_TCCR0A_WGM01_BIT <- 1  # Waveform Generation Mode
TIPO_TCCR0A_COM0B0_BIT <- 4  # Compare Output Mode B
TIPO_TCCR0A_COM0B1_BIT <- 5  # Compare Output Mode B
TIPO_TCCR0A_COM0A0_BIT <- 6  # Compare Output Mode A
TIPO_TCCR0A_COM0A1_BIT <- 7  # Compare Output Mode A
TIPO_TCCR0B_ADDR <- 0x21
TIPO_TCCR0B_CS00_BIT <- 0  # Clock Select
TIPO_TCCR0B_CS01_BIT <- 1  # Clock Select
TIPO_TCCR0B_CS02_BIT <- 2  # Clock Select
TIPO_TCCR0B_WGM02_BIT <- 3  # Waveform Generation Mode
TIPO_TCCR0B_FOC0B_BIT <- 6  # Force Output Compare B
TIPO_TCCR0B_FOC0A_BIT <- 7  # Force Output Compare A
TIPO_TCNT0_ADDR <- 0x22
TIPO_OCR0A_ADDR <- 0x23
TIPO_OCR0B_ADDR <- 0x24
TIPO_TIMSK_ADDR <- 0x39
TIPO_TIMSK_TOIE0_BIT <- 0  # Timer/Counter0 Overflow Interrupt Enable
TIPO_TIMSK_OCIE0A_BIT <- 1  # Output Compare A Match Interrupt Enable
TIPO_TIMSK_OCIE0B_BIT <- 2  # Output Compare B Match Interrupt Enable
TIPO_TIFR_ADDR <- 0x38
TIPO_TIFR_TOV0_BIT <- 0  # Timer/Counter0 Overflow Flag
TIPO_TIFR_OCF0A_BIT <- 1  # Output Compare A Flag
TIPO_TIFR_OCF0B_BIT <- 2  # Output Compare B Flag
# Timer/Counter1
TMR1_BASE <- 0x28
TMR1_TCCR1A_ADDR <- 0x28
TMR1_TCCR1A_PCM1_BIT <- 0  # PWM Mode
TMR1_TCCR1A_COM1A_BIT <- 0  # Compare Output Mode A
TMR1_TCCR1A_COM1B_BIT <- 0  # Compare Output Mode B
TMR1_TCCR1A_WG13_BIT <- 1  # Waveform Generation Mode
TMR1_TCCR1A_WG10_BIT <- 0  # Waveform Generation Mode
TMR1_TCCR1B_ADDR <- 0x29
TMR1_TCCR1B_CTC1_BIT <- 7  # Clear Timer on Compare
TMR1_TCCR1B_WGM13_BIT <- 4  # Waveform Generation Mode
TMR1_TCCR1B_WGM12_BIT <- 3  # Waveform Generation Mode
TMR1_TCCR1B_CS1_BIT <- 0  # Clock Select
TMR1_TCNT1_ADDR <- 0x2A
TMR1_OCR1A_ADDR <- 0x2C
TMR1_OCR1B_ADDR <- 0x2E
TMR1_OCR1C_ADDR <- 0x30
TMR1_TIMSK1_ADDR <- 0x33
TMR1_TIFR1_ADDR <- 0x32
# ADC Multiplexer
ADMUX_BASE <- 0x12
ADMUX_ADMUX_ADDR <- 0x12
ADMUX_ADMUX_MUX_BIT <- 0  # Analog Channel Selection
ADMUX_ADMUX_ADLAR_BIT <- 5  # ADC Left Adjust Result
ADMUX_ADMUX_REFS_BIT <- 0  # Reference Selection
ADMUX_ADCSRA_ADDR <- 0x13
ADMUX_ADCSRA_ADPS_BIT <- 0  # ADC Prescaler Select
ADMUX_ADCSRA_ADIE_BIT <- 3  # ADC Interrupt Enable
ADMUX_ADCSRA_ADIF_BIT <- 4  # ADC Interrupt Flag
ADMUX_ADCSRA_ADATE_BIT <- 5  # ADC Auto Trigger Enable
ADMUX_ADCSRA_ADSC_BIT <- 6  # ADC Start Conversion
ADMUX_ADCSRA_ADEN_BIT <- 7  # ADC Enable
ADMUX_ADCH_ADDR <- 0x14
ADMUX_ADCL_ADDR <- 0x15
# Universal Serial Interface
USI_BASE <- 0x18
USI_USIDR_ADDR <- 0x18
USI_USISR_ADDR <- 0x19
USI_USISR_USICNT_BIT <- 0  # Counter
USI_USISR_USIDC_BIT <- 4  # Data Register
USI_USISR_USIPF_BIT <- 5  # Stop Cond Flag
USI_USISR_USIOV_BIT <- 6  # Overflow Flag
USI_USISR_USISIF_BIT <- 7  # Start Cond Interrupt Flag
USI_USICR_ADDR <- 0x1A
USI_USICR_USICS_BIT <- 0  # Clock Source Select
USI_USICR_USISCL_BIT <- 2  # SCL strobe
USI_USICR_USIOW_BIT <- 3  # SDA output override
USI_USICR_USIOE_BIT <- 4  # Output Enable
USI_USICR_USISRE_BIT <- 5  # Start Recognition Enable
USI_USICR_USIORE_BIT <- 6  # Stop Recognition Enable
USI_USICR_USIGIE_BIT <- 7  # Global Interrupt Enable
USI_USIPORT_ADDR <- 0x1B
# MCU Control
MCUCR_BASE <- 0x35
MCUCR_MCUCR_ADDR <- 0x35
MCUCR_MCUCR_ISC_BIT <- 0  # Interrupt Sense Control
MCUCR_MCUCR_SE_BIT <- 4  # Sleep Enable
MCUCR_MCUCR_SM_BIT <- 0  # Sleep Mode
MCUCR_MCUCSR_ADDR <- 0x36
MCUCR_MCUCSR_PORF_BIT <- 0  # Power-on Reset Flag
MCUCR_MCUCSR_EXTRF_BIT <- 1  # External Reset Flag
MCUCR_MCUCSR_WDRF_BIT <- 2  # Watchdog Reset Flag
MCUCR_MCUCSR_BORF_BIT <- 4  # Brown-out Reset Flag
# Watchdog Timer
WDTCR_BASE <- 0x21
WDTCR_WDTCR_ADDR <- 0x21
WDTCR_WDTCR_WDP_BIT <- 0  # Watchdog Prescaler
WDTCR_WDTCR_WDE_BIT <- 3  # Watchdog Enable
WDTCR_WDTCR_WDIE_BIT <- 4  # Watchdog Interrupt Enable
# EEPROM
EEPR_BASE <- 0x1C
EEPR_EEAR_ADDR <- 0x1E
EEPR_EEDR_ADDR <- 0x1D
EEPR_EECR_ADDR <- 0x1F
EEPR_EECR_EEPM_BIT <- 0  # EEPROM Programming Mode
EEPR_EECR_EERIE_BIT <- 3  # EEPROM Ready Interrupt Enable
EEPR_EECR_EEWE_BIT <- 2  # EEPROM Write Enable
EEPR_EECR_EEMWE_BIT <- 1  # EEPROM Master Write Enable
EEPR_EECR_EERE_BIT <- 0  # EEPROM Read Enable
# External Interrupt
GIMSK_BASE <- 0x3B
GIMSK_GIMSK_ADDR <- 0x3B
GIMSK_GIMSK_INT0_BIT <- 0  # External Interrupt Request 0 Enable
GIMSK_GIMSK_PCIE_BIT <- 1  # Pin Change Interrupt Enable
GIMSK_GIFR_ADDR <- 0x3C
GIMSK_GIFR_INTF0_BIT <- 0  # External Interrupt Flag 0
GIMSK_GIFR_PCIF_BIT <- 1  # Pin Change Interrupt Flag
# Pin Change Mask
PCMSK_BASE <- 0x15
PCMSK_PCMSK_ADDR <- 0x15
# Store Program Memory
SPMCSR_BASE <- 0x37
SPMCSR_SPMCSR_ADDR <- 0x37
SPMCSR_SPMCSR_SPMCR_BIT <- 0  # SPM Mode
SPMCSR_SPMCSR_PGERS_BIT <- 1  # Page Erase
SPMCSR_SPMCSR_PGWRT_BIT <- 2  # Page Write
SPMCSR_SPMCSR_BLBSET_BIT <- 3  # Boot Lock Bits Set
SPMCSR_SPMCSR_RWWSRE_BIT <- 4  # Read-While-Read Strobe Enable
SPMCSR_SPMCSR_SIGRD_BIT <- 5  # Signature Row Read
SPMCSR_SPMCSR_SPMEN_BIT <- 7  # SPM Enable

# 中断向量定义
INT_RESET <- 0  # External Reset, Power-on Reset, Brown-out Reset
INT_INT0 <- 1  # External Interrupt Request 0
INT_PCINT0 <- 2  # Pin Change
INT_WDT <- 3  # Watchdog Timeout
INT_TIM1_COMPA <- 4  # Timer/Counter1 Compare Match A
INT_TIM1_OVF <- 5  # Timer/Counter1 Overflow
INT_TIM0_COMPA <- 6  # Timer/Counter0 Compare Match A
INT_TIM0_OVF <- 7  # Timer/Counter0 Overflow
INT_SPI_STC <- 8  # SPI Serial Transfer Complete
INT_ADC <- 9  # ADC Conversion Complete
INT_USI_START <- 10  # USI Start Condition
INT_USI_OVF <- 11  # USI Overflow
INT_EE_READY <- 12  # EEPROM Ready

# 引脚定义
PIN_PB5 <- 1  # RESET - ADC0 - dW
PIN_PB3 <- 2  # XTAL1 - CLKI - ADC3
PIN_PB4 <- 3  # XTAL2 - ADC2
PIN_PB0 <- 4  # MOSI - AI - ADC0 - T0 - INT0
PIN_PB1 <- 5  # MISO - AI - ADC1 - OC1A - INT1
PIN_PB2 <- 6  # SCK - AI - ADC3 - OC1B
PIN_VCC <- 7  # Supply Voltage
PIN_GND <- 8  # Ground

