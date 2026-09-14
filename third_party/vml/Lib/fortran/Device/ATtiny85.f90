! ATtiny85 设备定义 - Fortran 模块
! 生成自: Microchip/AVR/ATtiny85
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
! CPU架构: AVR
! 位宽: 8位
! 时钟频率: 1000000 Hz

module attiny85_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: R0_ADDR = 0x00  ! General Purpose Register 0
  integer, parameter :: R1_ADDR = 0x01  ! General Purpose Register 1
  integer, parameter :: R2_ADDR = 0x02  ! General Purpose Register 2
  integer, parameter :: R3_ADDR = 0x03  ! General Purpose Register 3
  integer, parameter :: R4_ADDR = 0x04  ! General Purpose Register 4
  integer, parameter :: R5_ADDR = 0x05  ! General Purpose Register 5
  integer, parameter :: R6_ADDR = 0x06  ! General Purpose Register 6
  integer, parameter :: R7_ADDR = 0x07  ! General Purpose Register 7
  integer, parameter :: R8_ADDR = 0x08  ! General Purpose Register 8
  integer, parameter :: R9_ADDR = 0x09  ! General Purpose Register 9
  integer, parameter :: R10_ADDR = 0x0A  ! General Purpose Register 10
  integer, parameter :: R11_ADDR = 0x0B  ! General Purpose Register 11
  integer, parameter :: R12_ADDR = 0x0C  ! General Purpose Register 12
  integer, parameter :: R13_ADDR = 0x0D  ! General Purpose Register 13
  integer, parameter :: R14_ADDR = 0x0E  ! General Purpose Register 14
  integer, parameter :: R15_ADDR = 0x0F  ! General Purpose Register 15
  integer, parameter :: R16_ADDR = 0x10  ! General Purpose Register 16
  integer, parameter :: R17_ADDR = 0x11  ! General Purpose Register 17
  integer, parameter :: R18_ADDR = 0x12  ! General Purpose Register 18
  integer, parameter :: R19_ADDR = 0x13  ! General Purpose Register 19
  integer, parameter :: R20_ADDR = 0x14  ! General Purpose Register 20
  integer, parameter :: R21_ADDR = 0x15  ! General Purpose Register 21
  integer, parameter :: R22_ADDR = 0x16  ! General Purpose Register 22
  integer, parameter :: R23_ADDR = 0x17  ! General Purpose Register 23
  integer, parameter :: R24_ADDR = 0x18  ! General Purpose Register 24
  integer, parameter :: R25_ADDR = 0x19  ! General Purpose Register 25
  integer, parameter :: X_ADDR = 0x1A  ! Register pair X (R27:R26)
  integer, parameter :: Y_ADDR = 0x1C  ! Register pair Y (R29:R28)
  integer, parameter :: Z_ADDR = 0x1E  ! Register pair Z (R31:R30)
  integer, parameter :: SP_ADDR = 0x3D  ! Stack Pointer
  integer, parameter :: SREG_ADDR = 0x3F  ! Status Register
  integer, parameter :: SREG_C_BIT = 0  ! Carry Flag
  integer, parameter :: SREG_Z_BIT = 1  ! Zero Flag
  integer, parameter :: SREG_N_BIT = 2  ! Negative Flag
  integer, parameter :: SREG_V_BIT = 3  ! Two's Complement Overflow Flag
  integer, parameter :: SREG_S_BIT = 4  ! Sign Flag (N xor V)
  integer, parameter :: SREG_H_BIT = 5  ! Half Carry Flag
  integer, parameter :: SREG_T_BIT = 6  ! Transfer Bit
  integer, parameter :: SREG_I_BIT = 7  ! Global Interrupt Enable

  ! 内存段定义
  integer, parameter :: FLASH_START = 0x0000
  integer, parameter :: FLASH_END = 0x1FFF
  integer, parameter :: FLASH_SIZE = 8192  ! Program Flash (8KB)
  integer, parameter :: SRAM_START = 0x0060
  integer, parameter :: SRAM_END = 0x025F
  integer, parameter :: SRAM_SIZE = 512  ! Internal SRAM (512B)
  integer, parameter :: EEPROM_START = 0x0000
  integer, parameter :: EEPROM_END = 0x01FF
  integer, parameter :: EEPROM_SIZE = 512  ! EEPROM (512B)
  integer, parameter :: IO_START = 0x00
  integer, parameter :: IO_END = 0x3F
  integer, parameter :: IO_SIZE = 64  ! I/O Registers

  ! 外设定义
  ! Port A
  integer, parameter :: PORTA_BASE = 0x20
  integer, parameter :: PORTA_PINA_ADDR = 0x20
  integer, parameter :: PORTA_DDRA_ADDR = 0x21
  integer, parameter :: PORTA_PORTA_ADDR = 0x22
  ! Port B
  integer, parameter :: PORTB_BASE = 0x18
  integer, parameter :: PORTB_PINB_ADDR = 0x16
  integer, parameter :: PORTB_DDRB_ADDR = 0x17
  integer, parameter :: PORTB_PORTB_ADDR = 0x18
  ! Timer/Counter0
  integer, parameter :: TIPO_BASE = 0x20
  integer, parameter :: TIPO_TCCR0A_ADDR = 0x20
  integer, parameter :: TIPO_TCCR0A_WGM00_BIT = 0  ! Waveform Generation Mode
  integer, parameter :: TIPO_TCCR0A_WGM01_BIT = 1  ! Waveform Generation Mode
  integer, parameter :: TIPO_TCCR0A_COM0B0_BIT = 4  ! Compare Output Mode B
  integer, parameter :: TIPO_TCCR0A_COM0B1_BIT = 5  ! Compare Output Mode B
  integer, parameter :: TIPO_TCCR0A_COM0A0_BIT = 6  ! Compare Output Mode A
  integer, parameter :: TIPO_TCCR0A_COM0A1_BIT = 7  ! Compare Output Mode A
  integer, parameter :: TIPO_TCCR0B_ADDR = 0x21
  integer, parameter :: TIPO_TCCR0B_CS00_BIT = 0  ! Clock Select
  integer, parameter :: TIPO_TCCR0B_CS01_BIT = 1  ! Clock Select
  integer, parameter :: TIPO_TCCR0B_CS02_BIT = 2  ! Clock Select
  integer, parameter :: TIPO_TCCR0B_WGM02_BIT = 3  ! Waveform Generation Mode
  integer, parameter :: TIPO_TCCR0B_FOC0B_BIT = 6  ! Force Output Compare B
  integer, parameter :: TIPO_TCCR0B_FOC0A_BIT = 7  ! Force Output Compare A
  integer, parameter :: TIPO_TCNT0_ADDR = 0x22
  integer, parameter :: TIPO_OCR0A_ADDR = 0x23
  integer, parameter :: TIPO_OCR0B_ADDR = 0x24
  integer, parameter :: TIPO_TIMSK_ADDR = 0x39
  integer, parameter :: TIPO_TIMSK_TOIE0_BIT = 0  ! Timer/Counter0 Overflow Interrupt Enable
  integer, parameter :: TIPO_TIMSK_OCIE0A_BIT = 1  ! Output Compare A Match Interrupt Enable
  integer, parameter :: TIPO_TIMSK_OCIE0B_BIT = 2  ! Output Compare B Match Interrupt Enable
  integer, parameter :: TIPO_TIFR_ADDR = 0x38
  integer, parameter :: TIPO_TIFR_TOV0_BIT = 0  ! Timer/Counter0 Overflow Flag
  integer, parameter :: TIPO_TIFR_OCF0A_BIT = 1  ! Output Compare A Flag
  integer, parameter :: TIPO_TIFR_OCF0B_BIT = 2  ! Output Compare B Flag
  ! Timer/Counter1
  integer, parameter :: TMR1_BASE = 0x28
  integer, parameter :: TMR1_TCCR1A_ADDR = 0x28
  integer, parameter :: TMR1_TCCR1A_PCM1_BIT = 0  ! PWM Mode
  integer, parameter :: TMR1_TCCR1A_COM1A_BIT = 0  ! Compare Output Mode A
  integer, parameter :: TMR1_TCCR1A_COM1B_BIT = 0  ! Compare Output Mode B
  integer, parameter :: TMR1_TCCR1A_WG13_BIT = 1  ! Waveform Generation Mode
  integer, parameter :: TMR1_TCCR1A_WG10_BIT = 0  ! Waveform Generation Mode
  integer, parameter :: TMR1_TCCR1B_ADDR = 0x29
  integer, parameter :: TMR1_TCCR1B_CTC1_BIT = 7  ! Clear Timer on Compare
  integer, parameter :: TMR1_TCCR1B_WGM13_BIT = 4  ! Waveform Generation Mode
  integer, parameter :: TMR1_TCCR1B_WGM12_BIT = 3  ! Waveform Generation Mode
  integer, parameter :: TMR1_TCCR1B_CS1_BIT = 0  ! Clock Select
  integer, parameter :: TMR1_TCNT1_ADDR = 0x2A
  integer, parameter :: TMR1_OCR1A_ADDR = 0x2C
  integer, parameter :: TMR1_OCR1B_ADDR = 0x2E
  integer, parameter :: TMR1_OCR1C_ADDR = 0x30
  integer, parameter :: TMR1_TIMSK1_ADDR = 0x33
  integer, parameter :: TMR1_TIFR1_ADDR = 0x32
  ! ADC Multiplexer
  integer, parameter :: ADMUX_BASE = 0x12
  integer, parameter :: ADMUX_ADMUX_ADDR = 0x12
  integer, parameter :: ADMUX_ADMUX_MUX_BIT = 0  ! Analog Channel Selection
  integer, parameter :: ADMUX_ADMUX_ADLAR_BIT = 5  ! ADC Left Adjust Result
  integer, parameter :: ADMUX_ADMUX_REFS_BIT = 0  ! Reference Selection
  integer, parameter :: ADMUX_ADCSRA_ADDR = 0x13
  integer, parameter :: ADMUX_ADCSRA_ADPS_BIT = 0  ! ADC Prescaler Select
  integer, parameter :: ADMUX_ADCSRA_ADIE_BIT = 3  ! ADC Interrupt Enable
  integer, parameter :: ADMUX_ADCSRA_ADIF_BIT = 4  ! ADC Interrupt Flag
  integer, parameter :: ADMUX_ADCSRA_ADATE_BIT = 5  ! ADC Auto Trigger Enable
  integer, parameter :: ADMUX_ADCSRA_ADSC_BIT = 6  ! ADC Start Conversion
  integer, parameter :: ADMUX_ADCSRA_ADEN_BIT = 7  ! ADC Enable
  integer, parameter :: ADMUX_ADCH_ADDR = 0x14
  integer, parameter :: ADMUX_ADCL_ADDR = 0x15
  ! Universal Serial Interface
  integer, parameter :: USI_BASE = 0x18
  integer, parameter :: USI_USIDR_ADDR = 0x18
  integer, parameter :: USI_USISR_ADDR = 0x19
  integer, parameter :: USI_USISR_USICNT_BIT = 0  ! Counter
  integer, parameter :: USI_USISR_USIDC_BIT = 4  ! Data Register
  integer, parameter :: USI_USISR_USIPF_BIT = 5  ! Stop Cond Flag
  integer, parameter :: USI_USISR_USIOV_BIT = 6  ! Overflow Flag
  integer, parameter :: USI_USISR_USISIF_BIT = 7  ! Start Cond Interrupt Flag
  integer, parameter :: USI_USICR_ADDR = 0x1A
  integer, parameter :: USI_USICR_USICS_BIT = 0  ! Clock Source Select
  integer, parameter :: USI_USICR_USISCL_BIT = 2  ! SCL strobe
  integer, parameter :: USI_USICR_USIOW_BIT = 3  ! SDA output override
  integer, parameter :: USI_USICR_USIOE_BIT = 4  ! Output Enable
  integer, parameter :: USI_USICR_USISRE_BIT = 5  ! Start Recognition Enable
  integer, parameter :: USI_USICR_USIORE_BIT = 6  ! Stop Recognition Enable
  integer, parameter :: USI_USICR_USIGIE_BIT = 7  ! Global Interrupt Enable
  integer, parameter :: USI_USIPORT_ADDR = 0x1B
  ! MCU Control
  integer, parameter :: MCUCR_BASE = 0x35
  integer, parameter :: MCUCR_MCUCR_ADDR = 0x35
  integer, parameter :: MCUCR_MCUCR_ISC_BIT = 0  ! Interrupt Sense Control
  integer, parameter :: MCUCR_MCUCR_SE_BIT = 4  ! Sleep Enable
  integer, parameter :: MCUCR_MCUCR_SM_BIT = 0  ! Sleep Mode
  integer, parameter :: MCUCR_MCUCSR_ADDR = 0x36
  integer, parameter :: MCUCR_MCUCSR_PORF_BIT = 0  ! Power-on Reset Flag
  integer, parameter :: MCUCR_MCUCSR_EXTRF_BIT = 1  ! External Reset Flag
  integer, parameter :: MCUCR_MCUCSR_WDRF_BIT = 2  ! Watchdog Reset Flag
  integer, parameter :: MCUCR_MCUCSR_BORF_BIT = 4  ! Brown-out Reset Flag
  ! Watchdog Timer
  integer, parameter :: WDTCR_BASE = 0x21
  integer, parameter :: WDTCR_WDTCR_ADDR = 0x21
  integer, parameter :: WDTCR_WDTCR_WDP_BIT = 0  ! Watchdog Prescaler
  integer, parameter :: WDTCR_WDTCR_WDE_BIT = 3  ! Watchdog Enable
  integer, parameter :: WDTCR_WDTCR_WDIE_BIT = 4  ! Watchdog Interrupt Enable
  ! EEPROM
  integer, parameter :: EEPR_BASE = 0x1C
  integer, parameter :: EEPR_EEAR_ADDR = 0x1E
  integer, parameter :: EEPR_EEDR_ADDR = 0x1D
  integer, parameter :: EEPR_EECR_ADDR = 0x1F
  integer, parameter :: EEPR_EECR_EEPM_BIT = 0  ! EEPROM Programming Mode
  integer, parameter :: EEPR_EECR_EERIE_BIT = 3  ! EEPROM Ready Interrupt Enable
  integer, parameter :: EEPR_EECR_EEWE_BIT = 2  ! EEPROM Write Enable
  integer, parameter :: EEPR_EECR_EEMWE_BIT = 1  ! EEPROM Master Write Enable
  integer, parameter :: EEPR_EECR_EERE_BIT = 0  ! EEPROM Read Enable
  ! External Interrupt
  integer, parameter :: GIMSK_BASE = 0x3B
  integer, parameter :: GIMSK_GIMSK_ADDR = 0x3B
  integer, parameter :: GIMSK_GIMSK_INT0_BIT = 0  ! External Interrupt Request 0 Enable
  integer, parameter :: GIMSK_GIMSK_PCIE_BIT = 1  ! Pin Change Interrupt Enable
  integer, parameter :: GIMSK_GIFR_ADDR = 0x3C
  integer, parameter :: GIMSK_GIFR_INTF0_BIT = 0  ! External Interrupt Flag 0
  integer, parameter :: GIMSK_GIFR_PCIF_BIT = 1  ! Pin Change Interrupt Flag
  ! Pin Change Mask
  integer, parameter :: PCMSK_BASE = 0x15
  integer, parameter :: PCMSK_PCMSK_ADDR = 0x15
  ! Store Program Memory
  integer, parameter :: SPMCSR_BASE = 0x37
  integer, parameter :: SPMCSR_SPMCSR_ADDR = 0x37
  integer, parameter :: SPMCSR_SPMCSR_SPMCR_BIT = 0  ! SPM Mode
  integer, parameter :: SPMCSR_SPMCSR_PGERS_BIT = 1  ! Page Erase
  integer, parameter :: SPMCSR_SPMCSR_PGWRT_BIT = 2  ! Page Write
  integer, parameter :: SPMCSR_SPMCSR_BLBSET_BIT = 3  ! Boot Lock Bits Set
  integer, parameter :: SPMCSR_SPMCSR_RWWSRE_BIT = 4  ! Read-While-Read Strobe Enable
  integer, parameter :: SPMCSR_SPMCSR_SIGRD_BIT = 5  ! Signature Row Read
  integer, parameter :: SPMCSR_SPMCSR_SPMEN_BIT = 7  ! SPM Enable

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! External Reset, Power-on Reset, Brown-out Reset
  integer, parameter :: INT_INT0 = 1  ! External Interrupt Request 0
  integer, parameter :: INT_PCINT0 = 2  ! Pin Change
  integer, parameter :: INT_WDT = 3  ! Watchdog Timeout
  integer, parameter :: INT_TIM1_COMPA = 4  ! Timer/Counter1 Compare Match A
  integer, parameter :: INT_TIM1_OVF = 5  ! Timer/Counter1 Overflow
  integer, parameter :: INT_TIM0_COMPA = 6  ! Timer/Counter0 Compare Match A
  integer, parameter :: INT_TIM0_OVF = 7  ! Timer/Counter0 Overflow
  integer, parameter :: INT_SPI_STC = 8  ! SPI Serial Transfer Complete
  integer, parameter :: INT_ADC = 9  ! ADC Conversion Complete
  integer, parameter :: INT_USI_START = 10  ! USI Start Condition
  integer, parameter :: INT_USI_OVF = 11  ! USI Overflow
  integer, parameter :: INT_EE_READY = 12  ! EEPROM Ready

  ! 引脚定义
  integer, parameter :: PIN_PB5 = 1  ! RESET - ADC0 - dW
  integer, parameter :: PIN_PB3 = 2  ! XTAL1 - CLKI - ADC3
  integer, parameter :: PIN_PB4 = 3  ! XTAL2 - ADC2
  integer, parameter :: PIN_PB0 = 4  ! MOSI - AI - ADC0 - T0 - INT0
  integer, parameter :: PIN_PB1 = 5  ! MISO - AI - ADC1 - OC1A - INT1
  integer, parameter :: PIN_PB2 = 6  ! SCK - AI - ADC3 - OC1B
  integer, parameter :: PIN_VCC = 7  ! Supply Voltage
  integer, parameter :: PIN_GND = 8  ! Ground

end module attiny85_device
