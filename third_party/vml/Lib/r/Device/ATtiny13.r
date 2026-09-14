# ATtiny13 设备定义 - R 脚本
# 生成自: Atmel/AVR/ATtiny13
# 版本: 1.0
# 日期: 2026-04-28
# 作者: VML Team
# 描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
# CPU架构: AVR
# 位宽: 8位
# 时钟频率: 20000000 Hz

# 寄存器地址定义
R0_ADDR <- 0x00  # 
R1_ADDR <- 0x01  # 
R2_ADDR <- 0x02  # 
R16_ADDR <- 0x10  # 
R17_ADDR <- 0x11  # 
R26_ADDR <- 0x1A  # XL
R27_ADDR <- 0x1B  # XH
R28_ADDR <- 0x1C  # YL
R29_ADDR <- 0x1D  # YH
R30_ADDR <- 0x1E  # ZL
R31_ADDR <- 0x1F  # ZH
SPL_ADDR <- 0x5D  # Stack Pointer Low
SPH_ADDR <- 0x5E  # Stack Pointer High
SREG_ADDR <- 0x5F  # Status Register

# 内存段定义
FLASH_START <- 0x0000
FLASH_END <- 0x03FF
FLASH_SIZE <- 1024  # 
SRAM_START <- 0x0060
SRAM_END <- 0x009F
SRAM_SIZE <- 64  # 
EEPROM_START <- 0x0000
EEPROM_END <- 0x003F
EEPROM_SIZE <- 64  # 
IO_START <- 0x00
IO_END <- 0x1F
IO_SIZE <- 32  # 
EXTIO_START <- 0x20
EXTIO_END <- 0x5F
EXTIO_SIZE <- 64  # 

# 外设定义
# Port B (only port)
PORTB_BASE <- 0x18
PORTB_DDRB_ADDR <- 0x17
PORTB_PORTB_ADDR <- 0x18
PORTB_PINB_ADDR <- 0x19
# 8-bit Timer/Counter0
TIMER0_BASE <- 0x33
TIMER0_TCCR0A_ADDR <- 0x33
TIMER0_TCCR0B_ADDR <- 0x33
TIMER0_TCNT0_ADDR <- 0x32
TIMER0_OCR0A_ADDR <- 0x36
TIMER0_OCR0B_ADDR <- 0x35
TIMER0_TIMSK0_ADDR <- 0x39
TIMER0_TIFR0_ADDR <- 0x38
# Analog-to-Digital
ADC_BASE <- 0x04
ADC_ADMUX_ADDR <- 0x07
ADC_ADCSRA_ADDR <- 0x06
ADC_ADCL_ADDR <- 0x04
ADC_ADCH_ADDR <- 0x05

# 中断向量定义
INT_RESET <- 1  # 
INT_INT0 <- 2  # External Interrupt 0
INT_PCINT0 <- 3  # Pin Change Interrupt
INT_TIM0_OVF <- 4  # Timer0 Overflow
INT_TIM0_COMPA <- 5  # Timer0 Compare A
INT_WDT <- 6  # Watchdog Timeout
INT_ADC <- 7  # ADC Conversion Complete

