// ATtiny85 设备定义 - Dart 库
// 生成自: Microchip/AVR/ATtiny85
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 1000000 Hz

class ATtiny85Device {
  static const String deviceName = "ATtiny85";
  static const String manufacturer = "Microchip";
  static const String family = "AVR";
  static const String version = "1.0";
  static const String architecture = "AVR";
  static const int bits = 8;
  static const int clockFrequency = 1000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00;  // General Purpose Register 0
  static const int R1_ADDR = 0x01;  // General Purpose Register 1
  static const int R2_ADDR = 0x02;  // General Purpose Register 2
  static const int R3_ADDR = 0x03;  // General Purpose Register 3
  static const int R4_ADDR = 0x04;  // General Purpose Register 4
  static const int R5_ADDR = 0x05;  // General Purpose Register 5
  static const int R6_ADDR = 0x06;  // General Purpose Register 6
  static const int R7_ADDR = 0x07;  // General Purpose Register 7
  static const int R8_ADDR = 0x08;  // General Purpose Register 8
  static const int R9_ADDR = 0x09;  // General Purpose Register 9
  static const int R10_ADDR = 0x0A;  // General Purpose Register 10
  static const int R11_ADDR = 0x0B;  // General Purpose Register 11
  static const int R12_ADDR = 0x0C;  // General Purpose Register 12
  static const int R13_ADDR = 0x0D;  // General Purpose Register 13
  static const int R14_ADDR = 0x0E;  // General Purpose Register 14
  static const int R15_ADDR = 0x0F;  // General Purpose Register 15
  static const int R16_ADDR = 0x10;  // General Purpose Register 16
  static const int R17_ADDR = 0x11;  // General Purpose Register 17
  static const int R18_ADDR = 0x12;  // General Purpose Register 18
  static const int R19_ADDR = 0x13;  // General Purpose Register 19
  static const int R20_ADDR = 0x14;  // General Purpose Register 20
  static const int R21_ADDR = 0x15;  // General Purpose Register 21
  static const int R22_ADDR = 0x16;  // General Purpose Register 22
  static const int R23_ADDR = 0x17;  // General Purpose Register 23
  static const int R24_ADDR = 0x18;  // General Purpose Register 24
  static const int R25_ADDR = 0x19;  // General Purpose Register 25
  static const int X_ADDR = 0x1A;  // Register pair X (R27:R26)
  static const int Y_ADDR = 0x1C;  // Register pair Y (R29:R28)
  static const int Z_ADDR = 0x1E;  // Register pair Z (R31:R30)
  static const int SP_ADDR = 0x3D;  // Stack Pointer
  static const int SREG_ADDR = 0x3F;  // Status Register
  static const int SREG_C_BIT = 0;  // Carry Flag
  static const int SREG_Z_BIT = 1;  // Zero Flag
  static const int SREG_N_BIT = 2;  // Negative Flag
  static const int SREG_V_BIT = 3;  // Two's Complement Overflow Flag
  static const int SREG_S_BIT = 4;  // Sign Flag (N xor V)
  static const int SREG_H_BIT = 5;  // Half Carry Flag
  static const int SREG_T_BIT = 6;  // Transfer Bit
  static const int SREG_I_BIT = 7;  // Global Interrupt Enable

  // 内存段定义
  static const int FLASH_START = 0x0000;
  static const int FLASH_END = 0x1FFF;
  static const int FLASH_SIZE = 8192;  // Program Flash (8KB)
  static const int SRAM_START = 0x0060;
  static const int SRAM_END = 0x025F;
  static const int SRAM_SIZE = 512;  // Internal SRAM (512B)
  static const int EEPROM_START = 0x0000;
  static const int EEPROM_END = 0x01FF;
  static const int EEPROM_SIZE = 512;  // EEPROM (512B)
  static const int IO_START = 0x00;
  static const int IO_END = 0x3F;
  static const int IO_SIZE = 64;  // I/O Registers

  // 外设定义
  // Port A
  static const int PORTA_BASE = 0x20;
  static const int PORTA_PINA_ADDR = 0x20;
  static const int PORTA_DDRA_ADDR = 0x21;
  static const int PORTA_PORTA_ADDR = 0x22;
  // Port B
  static const int PORTB_BASE = 0x18;
  static const int PORTB_PINB_ADDR = 0x16;
  static const int PORTB_DDRB_ADDR = 0x17;
  static const int PORTB_PORTB_ADDR = 0x18;
  // Timer/Counter0
  static const int TIPO_BASE = 0x20;
  static const int TIPO_TCCR0A_ADDR = 0x20;
  static const int TIPO_TCCR0A_WGM00_BIT = 0;  // Waveform Generation Mode
  static const int TIPO_TCCR0A_WGM01_BIT = 1;  // Waveform Generation Mode
  static const int TIPO_TCCR0A_COM0B0_BIT = 4;  // Compare Output Mode B
  static const int TIPO_TCCR0A_COM0B1_BIT = 5;  // Compare Output Mode B
  static const int TIPO_TCCR0A_COM0A0_BIT = 6;  // Compare Output Mode A
  static const int TIPO_TCCR0A_COM0A1_BIT = 7;  // Compare Output Mode A
  static const int TIPO_TCCR0B_ADDR = 0x21;
  static const int TIPO_TCCR0B_CS00_BIT = 0;  // Clock Select
  static const int TIPO_TCCR0B_CS01_BIT = 1;  // Clock Select
  static const int TIPO_TCCR0B_CS02_BIT = 2;  // Clock Select
  static const int TIPO_TCCR0B_WGM02_BIT = 3;  // Waveform Generation Mode
  static const int TIPO_TCCR0B_FOC0B_BIT = 6;  // Force Output Compare B
  static const int TIPO_TCCR0B_FOC0A_BIT = 7;  // Force Output Compare A
  static const int TIPO_TCNT0_ADDR = 0x22;
  static const int TIPO_OCR0A_ADDR = 0x23;
  static const int TIPO_OCR0B_ADDR = 0x24;
  static const int TIPO_TIMSK_ADDR = 0x39;
  static const int TIPO_TIMSK_TOIE0_BIT = 0;  // Timer/Counter0 Overflow Interrupt Enable
  static const int TIPO_TIMSK_OCIE0A_BIT = 1;  // Output Compare A Match Interrupt Enable
  static const int TIPO_TIMSK_OCIE0B_BIT = 2;  // Output Compare B Match Interrupt Enable
  static const int TIPO_TIFR_ADDR = 0x38;
  static const int TIPO_TIFR_TOV0_BIT = 0;  // Timer/Counter0 Overflow Flag
  static const int TIPO_TIFR_OCF0A_BIT = 1;  // Output Compare A Flag
  static const int TIPO_TIFR_OCF0B_BIT = 2;  // Output Compare B Flag
  // Timer/Counter1
  static const int TMR1_BASE = 0x28;
  static const int TMR1_TCCR1A_ADDR = 0x28;
  static const int TMR1_TCCR1A_PCM1_BIT = 0;  // PWM Mode
  static const int TMR1_TCCR1A_COM1A_BIT = 0;  // Compare Output Mode A
  static const int TMR1_TCCR1A_COM1B_BIT = 0;  // Compare Output Mode B
  static const int TMR1_TCCR1A_WG13_BIT = 1;  // Waveform Generation Mode
  static const int TMR1_TCCR1A_WG10_BIT = 0;  // Waveform Generation Mode
  static const int TMR1_TCCR1B_ADDR = 0x29;
  static const int TMR1_TCCR1B_CTC1_BIT = 7;  // Clear Timer on Compare
  static const int TMR1_TCCR1B_WGM13_BIT = 4;  // Waveform Generation Mode
  static const int TMR1_TCCR1B_WGM12_BIT = 3;  // Waveform Generation Mode
  static const int TMR1_TCCR1B_CS1_BIT = 0;  // Clock Select
  static const int TMR1_TCNT1_ADDR = 0x2A;
  static const int TMR1_OCR1A_ADDR = 0x2C;
  static const int TMR1_OCR1B_ADDR = 0x2E;
  static const int TMR1_OCR1C_ADDR = 0x30;
  static const int TMR1_TIMSK1_ADDR = 0x33;
  static const int TMR1_TIFR1_ADDR = 0x32;
  // ADC Multiplexer
  static const int ADMUX_BASE = 0x12;
  static const int ADMUX_ADMUX_ADDR = 0x12;
  static const int ADMUX_ADMUX_MUX_BIT = 0;  // Analog Channel Selection
  static const int ADMUX_ADMUX_ADLAR_BIT = 5;  // ADC Left Adjust Result
  static const int ADMUX_ADMUX_REFS_BIT = 0;  // Reference Selection
  static const int ADMUX_ADCSRA_ADDR = 0x13;
  static const int ADMUX_ADCSRA_ADPS_BIT = 0;  // ADC Prescaler Select
  static const int ADMUX_ADCSRA_ADIE_BIT = 3;  // ADC Interrupt Enable
  static const int ADMUX_ADCSRA_ADIF_BIT = 4;  // ADC Interrupt Flag
  static const int ADMUX_ADCSRA_ADATE_BIT = 5;  // ADC Auto Trigger Enable
  static const int ADMUX_ADCSRA_ADSC_BIT = 6;  // ADC Start Conversion
  static const int ADMUX_ADCSRA_ADEN_BIT = 7;  // ADC Enable
  static const int ADMUX_ADCH_ADDR = 0x14;
  static const int ADMUX_ADCL_ADDR = 0x15;
  // Universal Serial Interface
  static const int USI_BASE = 0x18;
  static const int USI_USIDR_ADDR = 0x18;
  static const int USI_USISR_ADDR = 0x19;
  static const int USI_USISR_USICNT_BIT = 0;  // Counter
  static const int USI_USISR_USIDC_BIT = 4;  // Data Register
  static const int USI_USISR_USIPF_BIT = 5;  // Stop Cond Flag
  static const int USI_USISR_USIOV_BIT = 6;  // Overflow Flag
  static const int USI_USISR_USISIF_BIT = 7;  // Start Cond Interrupt Flag
  static const int USI_USICR_ADDR = 0x1A;
  static const int USI_USICR_USICS_BIT = 0;  // Clock Source Select
  static const int USI_USICR_USISCL_BIT = 2;  // SCL strobe
  static const int USI_USICR_USIOW_BIT = 3;  // SDA output override
  static const int USI_USICR_USIOE_BIT = 4;  // Output Enable
  static const int USI_USICR_USISRE_BIT = 5;  // Start Recognition Enable
  static const int USI_USICR_USIORE_BIT = 6;  // Stop Recognition Enable
  static const int USI_USICR_USIGIE_BIT = 7;  // Global Interrupt Enable
  static const int USI_USIPORT_ADDR = 0x1B;
  // MCU Control
  static const int MCUCR_BASE = 0x35;
  static const int MCUCR_MCUCR_ADDR = 0x35;
  static const int MCUCR_MCUCR_ISC_BIT = 0;  // Interrupt Sense Control
  static const int MCUCR_MCUCR_SE_BIT = 4;  // Sleep Enable
  static const int MCUCR_MCUCR_SM_BIT = 0;  // Sleep Mode
  static const int MCUCR_MCUCSR_ADDR = 0x36;
  static const int MCUCR_MCUCSR_PORF_BIT = 0;  // Power-on Reset Flag
  static const int MCUCR_MCUCSR_EXTRF_BIT = 1;  // External Reset Flag
  static const int MCUCR_MCUCSR_WDRF_BIT = 2;  // Watchdog Reset Flag
  static const int MCUCR_MCUCSR_BORF_BIT = 4;  // Brown-out Reset Flag
  // Watchdog Timer
  static const int WDTCR_BASE = 0x21;
  static const int WDTCR_WDTCR_ADDR = 0x21;
  static const int WDTCR_WDTCR_WDP_BIT = 0;  // Watchdog Prescaler
  static const int WDTCR_WDTCR_WDE_BIT = 3;  // Watchdog Enable
  static const int WDTCR_WDTCR_WDIE_BIT = 4;  // Watchdog Interrupt Enable
  // EEPROM
  static const int EEPR_BASE = 0x1C;
  static const int EEPR_EEAR_ADDR = 0x1E;
  static const int EEPR_EEDR_ADDR = 0x1D;
  static const int EEPR_EECR_ADDR = 0x1F;
  static const int EEPR_EECR_EEPM_BIT = 0;  // EEPROM Programming Mode
  static const int EEPR_EECR_EERIE_BIT = 3;  // EEPROM Ready Interrupt Enable
  static const int EEPR_EECR_EEWE_BIT = 2;  // EEPROM Write Enable
  static const int EEPR_EECR_EEMWE_BIT = 1;  // EEPROM Master Write Enable
  static const int EEPR_EECR_EERE_BIT = 0;  // EEPROM Read Enable
  // External Interrupt
  static const int GIMSK_BASE = 0x3B;
  static const int GIMSK_GIMSK_ADDR = 0x3B;
  static const int GIMSK_GIMSK_INT0_BIT = 0;  // External Interrupt Request 0 Enable
  static const int GIMSK_GIMSK_PCIE_BIT = 1;  // Pin Change Interrupt Enable
  static const int GIMSK_GIFR_ADDR = 0x3C;
  static const int GIMSK_GIFR_INTF0_BIT = 0;  // External Interrupt Flag 0
  static const int GIMSK_GIFR_PCIF_BIT = 1;  // Pin Change Interrupt Flag
  // Pin Change Mask
  static const int PCMSK_BASE = 0x15;
  static const int PCMSK_PCMSK_ADDR = 0x15;
  // Store Program Memory
  static const int SPMCSR_BASE = 0x37;
  static const int SPMCSR_SPMCSR_ADDR = 0x37;
  static const int SPMCSR_SPMCSR_SPMCR_BIT = 0;  // SPM Mode
  static const int SPMCSR_SPMCSR_PGERS_BIT = 1;  // Page Erase
  static const int SPMCSR_SPMCSR_PGWRT_BIT = 2;  // Page Write
  static const int SPMCSR_SPMCSR_BLBSET_BIT = 3;  // Boot Lock Bits Set
  static const int SPMCSR_SPMCSR_RWWSRE_BIT = 4;  // Read-While-Read Strobe Enable
  static const int SPMCSR_SPMCSR_SIGRD_BIT = 5;  // Signature Row Read
  static const int SPMCSR_SPMCSR_SPMEN_BIT = 7;  // SPM Enable

  // 中断向量定义
  static const int INT_RESET = 0;  // External Reset, Power-on Reset, Brown-out Reset
  static const int INT_INT0 = 1;  // External Interrupt Request 0
  static const int INT_PCINT0 = 2;  // Pin Change
  static const int INT_WDT = 3;  // Watchdog Timeout
  static const int INT_TIM1_COMPA = 4;  // Timer/Counter1 Compare Match A
  static const int INT_TIM1_OVF = 5;  // Timer/Counter1 Overflow
  static const int INT_TIM0_COMPA = 6;  // Timer/Counter0 Compare Match A
  static const int INT_TIM0_OVF = 7;  // Timer/Counter0 Overflow
  static const int INT_SPI_STC = 8;  // SPI Serial Transfer Complete
  static const int INT_ADC = 9;  // ADC Conversion Complete
  static const int INT_USI_START = 10;  // USI Start Condition
  static const int INT_USI_OVF = 11;  // USI Overflow
  static const int INT_EE_READY = 12;  // EEPROM Ready

  // 引脚定义
  static const int PIN_PB5 = 1;  // RESET - ADC0 - dW
  static const int PIN_PB3 = 2;  // XTAL1 - CLKI - ADC3
  static const int PIN_PB4 = 3;  // XTAL2 - ADC2
  static const int PIN_PB0 = 4;  // MOSI - AI - ADC0 - T0 - INT0
  static const int PIN_PB1 = 5;  // MISO - AI - ADC1 - OC1A - INT1
  static const int PIN_PB2 = 6;  // SCK - AI - ADC3 - OC1B
  static const int PIN_VCC = 7;  // Supply Voltage
  static const int PIN_GND = 8;  // Ground

}
