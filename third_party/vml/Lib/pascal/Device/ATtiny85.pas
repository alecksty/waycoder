unit attiny85;

interface

// ATtiny85寄存器定义
// 生成自: Microchip/AVR/ATtiny85
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM

// CPU架构: AVR
// 位宽: 8位
// 时钟频率: 1000000 Hz

const

  // 寄存器定义
  // General Purpose Register 0
  R0 = 0x00;

  // General Purpose Register 1
  R1 = 0x01;

  // General Purpose Register 2
  R2 = 0x02;

  // General Purpose Register 3
  R3 = 0x03;

  // General Purpose Register 4
  R4 = 0x04;

  // General Purpose Register 5
  R5 = 0x05;

  // General Purpose Register 6
  R6 = 0x06;

  // General Purpose Register 7
  R7 = 0x07;

  // General Purpose Register 8
  R8 = 0x08;

  // General Purpose Register 9
  R9 = 0x09;

  // General Purpose Register 10
  R10 = 0x0A;

  // General Purpose Register 11
  R11 = 0x0B;

  // General Purpose Register 12
  R12 = 0x0C;

  // General Purpose Register 13
  R13 = 0x0D;

  // General Purpose Register 14
  R14 = 0x0E;

  // General Purpose Register 15
  R15 = 0x0F;

  // General Purpose Register 16
  R16 = 0x10;

  // General Purpose Register 17
  R17 = 0x11;

  // General Purpose Register 18
  R18 = 0x12;

  // General Purpose Register 19
  R19 = 0x13;

  // General Purpose Register 20
  R20 = 0x14;

  // General Purpose Register 21
  R21 = 0x15;

  // General Purpose Register 22
  R22 = 0x16;

  // General Purpose Register 23
  R23 = 0x17;

  // General Purpose Register 24
  R24 = 0x18;

  // General Purpose Register 25
  R25 = 0x19;

  // Register pair X (R27:R26)
  X = 0x1A;

  // Register pair Y (R29:R28)
  Y = 0x1C;

  // Register pair Z (R31:R30)
  Z = 0x1E;

  // Stack Pointer
  SP = 0x3D;

  // Status Register
  SREG = 0x3F;
  SREG_C = 0;  // Carry Flag
  SREG_Z = 1;  // Zero Flag
  SREG_N = 2;  // Negative Flag
  SREG_V = 3;  // Two's Complement Overflow Flag
  SREG_S = 4;  // Sign Flag (N xor V)
  SREG_H = 5;  // Half Carry Flag
  SREG_T = 6;  // Transfer Bit
  SREG_I = 7;  // Global Interrupt Enable

  // 内存段定义
  // Program Flash (8KB)
  FLASH_START = 0x0000;
  FLASH_END = 0x1FFF;
  FLASH_SIZE = 8192;

  // Internal SRAM (512B)
  SRAM_START = 0x0060;
  SRAM_END = 0x025F;
  SRAM_SIZE = 512;

  // EEPROM (512B)
  EEPROM_START = 0x0000;
  EEPROM_END = 0x01FF;
  EEPROM_SIZE = 512;

  // I/O Registers
  IO_START = 0x00;
  IO_END = 0x3F;
  IO_SIZE = 64;

  // 外设定义
  // Port A
  PORTA_BASE = 0x20;
  PORTA_PINA = 0x20;
  PORTA_DDRA = 0x21;
  PORTA_PORTA = 0x22;

  // Port B
  PORTB_BASE = 0x18;
  PORTB_PINB = 0x16;
  PORTB_DDRB = 0x17;
  PORTB_PORTB = 0x18;

  // Timer/Counter0
  TIPO_BASE = 0x20;
  TIPO_TCCR0A = 0x20;
  TIPO_TCCR0A_WGM00 = 0;  // Waveform Generation Mode
  TIPO_TCCR0A_WGM01 = 1;  // Waveform Generation Mode
  TIPO_TCCR0A_COM0B0 = 4;  // Compare Output Mode B
  TIPO_TCCR0A_COM0B1 = 5;  // Compare Output Mode B
  TIPO_TCCR0A_COM0A0 = 6;  // Compare Output Mode A
  TIPO_TCCR0A_COM0A1 = 7;  // Compare Output Mode A
  TIPO_TCCR0B = 0x21;
  TIPO_TCCR0B_CS00 = 0;  // Clock Select
  TIPO_TCCR0B_CS01 = 1;  // Clock Select
  TIPO_TCCR0B_CS02 = 2;  // Clock Select
  TIPO_TCCR0B_WGM02 = 3;  // Waveform Generation Mode
  TIPO_TCCR0B_FOC0B = 6;  // Force Output Compare B
  TIPO_TCCR0B_FOC0A = 7;  // Force Output Compare A
  TIPO_TCNT0 = 0x22;
  TIPO_OCR0A = 0x23;
  TIPO_OCR0B = 0x24;
  TIPO_TIMSK = 0x39;
  TIPO_TIMSK_TOIE0 = 0;  // Timer/Counter0 Overflow Interrupt Enable
  TIPO_TIMSK_OCIE0A = 1;  // Output Compare A Match Interrupt Enable
  TIPO_TIMSK_OCIE0B = 2;  // Output Compare B Match Interrupt Enable
  TIPO_TIFR = 0x38;
  TIPO_TIFR_TOV0 = 0;  // Timer/Counter0 Overflow Flag
  TIPO_TIFR_OCF0A = 1;  // Output Compare A Flag
  TIPO_TIFR_OCF0B = 2;  // Output Compare B Flag

  // Timer/Counter1
  TMR1_BASE = 0x28;
  TMR1_TCCR1A = 0x28;
  TMR1_TCCR1A_PCM1 = 0;  // PWM Mode
  TMR1_TCCR1A_COM1A = 0;  // Compare Output Mode A
  TMR1_TCCR1A_COM1B = 0;  // Compare Output Mode B
  TMR1_TCCR1A_WG13 = 1;  // Waveform Generation Mode
  TMR1_TCCR1A_WG10 = 0;  // Waveform Generation Mode
  TMR1_TCCR1B = 0x29;
  TMR1_TCCR1B_CTC1 = 7;  // Clear Timer on Compare
  TMR1_TCCR1B_WGM13 = 4;  // Waveform Generation Mode
  TMR1_TCCR1B_WGM12 = 3;  // Waveform Generation Mode
  TMR1_TCCR1B_CS1 = 0;  // Clock Select
  TMR1_TCNT1 = 0x2A;
  TMR1_OCR1A = 0x2C;
  TMR1_OCR1B = 0x2E;
  TMR1_OCR1C = 0x30;
  TMR1_TIMSK1 = 0x33;
  TMR1_TIFR1 = 0x32;

  // ADC Multiplexer
  ADMUX_BASE = 0x12;
  ADMUX_ADMUX = 0x12;
  ADMUX_ADMUX_MUX = 0;  // Analog Channel Selection
  ADMUX_ADMUX_ADLAR = 5;  // ADC Left Adjust Result
  ADMUX_ADMUX_REFS = 0;  // Reference Selection
  ADMUX_ADCSRA = 0x13;
  ADMUX_ADCSRA_ADPS = 0;  // ADC Prescaler Select
  ADMUX_ADCSRA_ADIE = 3;  // ADC Interrupt Enable
  ADMUX_ADCSRA_ADIF = 4;  // ADC Interrupt Flag
  ADMUX_ADCSRA_ADATE = 5;  // ADC Auto Trigger Enable
  ADMUX_ADCSRA_ADSC = 6;  // ADC Start Conversion
  ADMUX_ADCSRA_ADEN = 7;  // ADC Enable
  ADMUX_ADCH = 0x14;
  ADMUX_ADCL = 0x15;

  // Universal Serial Interface
  USI_BASE = 0x18;
  USI_USIDR = 0x18;
  USI_USISR = 0x19;
  USI_USISR_USICNT = 0;  // Counter
  USI_USISR_USIDC = 4;  // Data Register
  USI_USISR_USIPF = 5;  // Stop Cond Flag
  USI_USISR_USIOV = 6;  // Overflow Flag
  USI_USISR_USISIF = 7;  // Start Cond Interrupt Flag
  USI_USICR = 0x1A;
  USI_USICR_USICS = 0;  // Clock Source Select
  USI_USICR_USISCL = 2;  // SCL strobe
  USI_USICR_USIOW = 3;  // SDA output override
  USI_USICR_USIOE = 4;  // Output Enable
  USI_USICR_USISRE = 5;  // Start Recognition Enable
  USI_USICR_USIORE = 6;  // Stop Recognition Enable
  USI_USICR_USIGIE = 7;  // Global Interrupt Enable
  USI_USIPORT = 0x1B;

  // MCU Control
  MCUCR_BASE = 0x35;
  MCUCR_MCUCR = 0x35;
  MCUCR_MCUCR_ISC = 0;  // Interrupt Sense Control
  MCUCR_MCUCR_SE = 4;  // Sleep Enable
  MCUCR_MCUCR_SM = 0;  // Sleep Mode
  MCUCR_MCUCSR = 0x36;
  MCUCR_MCUCSR_PORF = 0;  // Power-on Reset Flag
  MCUCR_MCUCSR_EXTRF = 1;  // External Reset Flag
  MCUCR_MCUCSR_WDRF = 2;  // Watchdog Reset Flag
  MCUCR_MCUCSR_BORF = 4;  // Brown-out Reset Flag

  // Watchdog Timer
  WDTCR_BASE = 0x21;
  WDTCR_WDTCR = 0x21;
  WDTCR_WDTCR_WDP = 0;  // Watchdog Prescaler
  WDTCR_WDTCR_WDE = 3;  // Watchdog Enable
  WDTCR_WDTCR_WDIE = 4;  // Watchdog Interrupt Enable

  // EEPROM
  EEPR_BASE = 0x1C;
  EEPR_EEAR = 0x1E;
  EEPR_EEDR = 0x1D;
  EEPR_EECR = 0x1F;
  EEPR_EECR_EEPM = 0;  // EEPROM Programming Mode
  EEPR_EECR_EERIE = 3;  // EEPROM Ready Interrupt Enable
  EEPR_EECR_EEWE = 2;  // EEPROM Write Enable
  EEPR_EECR_EEMWE = 1;  // EEPROM Master Write Enable
  EEPR_EECR_EERE = 0;  // EEPROM Read Enable

  // External Interrupt
  GIMSK_BASE = 0x3B;
  GIMSK_GIMSK = 0x3B;
  GIMSK_GIMSK_INT0 = 0;  // External Interrupt Request 0 Enable
  GIMSK_GIMSK_PCIE = 1;  // Pin Change Interrupt Enable
  GIMSK_GIFR = 0x3C;
  GIMSK_GIFR_INTF0 = 0;  // External Interrupt Flag 0
  GIMSK_GIFR_PCIF = 1;  // Pin Change Interrupt Flag

  // Pin Change Mask
  PCMSK_BASE = 0x15;
  PCMSK_PCMSK = 0x15;

  // Store Program Memory
  SPMCSR_BASE = 0x37;
  SPMCSR_SPMCSR = 0x37;
  SPMCSR_SPMCSR_SPMCR = 0;  // SPM Mode
  SPMCSR_SPMCSR_PGERS = 1;  // Page Erase
  SPMCSR_SPMCSR_PGWRT = 2;  // Page Write
  SPMCSR_SPMCSR_BLBSET = 3;  // Boot Lock Bits Set
  SPMCSR_SPMCSR_RWWSRE = 4;  // Read-While-Read Strobe Enable
  SPMCSR_SPMCSR_SIGRD = 5;  // Signature Row Read
  SPMCSR_SPMCSR_SPMEN = 7;  // SPM Enable

  // 中断向量定义
  RESET_VECTOR = 0;  // External Reset, Power-on Reset, Brown-out Reset
  INT0_VECTOR = 1;  // External Interrupt Request 0
  PCINT0_VECTOR = 2;  // Pin Change
  WDT_VECTOR = 3;  // Watchdog Timeout
  TIM1_COMPA_VECTOR = 4;  // Timer/Counter1 Compare Match A
  TIM1_OVF_VECTOR = 5;  // Timer/Counter1 Overflow
  TIM0_COMPA_VECTOR = 6;  // Timer/Counter0 Compare Match A
  TIM0_OVF_VECTOR = 7;  // Timer/Counter0 Overflow
  SPI_STC_VECTOR = 8;  // SPI Serial Transfer Complete
  ADC_VECTOR = 9;  // ADC Conversion Complete
  USI_START_VECTOR = 10;  // USI Start Condition
  USI_OVF_VECTOR = 11;  // USI Overflow
  EE_READY_VECTOR = 12;  // EEPROM Ready

  // 引脚定义
  PIN_PB5 = 1;  // RESET - ADC0 - dW
  PIN_PB3 = 2;  // XTAL1 - CLKI - ADC3
  PIN_PB4 = 3;  // XTAL2 - ADC2
  PIN_PB0 = 4;  // MOSI - AI - ADC0 - T0 - INT0
  PIN_PB1 = 5;  // MISO - AI - ADC1 - OC1A - INT1
  PIN_PB2 = 6;  // SCK - AI - ADC3 - OC1B
  PIN_VCC = 7;  // Supply Voltage
  PIN_GND = 8;  // Ground

type
  TATtiny85 = record
    // 设备状态记录
  end;

// 设备初始化函数
procedure attiny85_init;

// 常用函数
function read_register(addr: Word): Byte;
procedure write_register(addr: Word; value: Byte);

implementation

procedure attiny85_init;
begin
  // 初始化代码
end;

function read_register(addr: Word): Byte;
begin
  // 读取寄存器值
  Result := 0;
end;

procedure write_register(addr: Word; value: Byte);
begin
  // 写入寄存器值
end;

end.
