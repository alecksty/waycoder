--[[
  ATtiny85设备定义 - Lua模块
  生成自: Microchip/AVR/ATtiny85
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 8-bit AVR microcontroller with 8KB Flash, 512B SRAM, 512B EEPROM
  CPU架构: AVR
  位宽: 8位
  时钟频率: 1000000 Hz
]]

local ATtiny85 = {}

-- 设备信息
ATtiny85.DEVICE_NAME = "ATtiny85"
ATtiny85.MANUFACTURER = "Microchip"
ATtiny85.FAMILY = "AVR"
ATtiny85.VERSION = "1.0"
ATtiny85.ARCHITECTURE = "AVR"
ATtiny85.BITS = 8
ATtiny85.CLOCK_FREQUENCY = 1000000

-- 寄存器地址定义
ATtiny85.R0_ADDR = 0x00  -- General Purpose Register 0
ATtiny85.R1_ADDR = 0x01  -- General Purpose Register 1
ATtiny85.R2_ADDR = 0x02  -- General Purpose Register 2
ATtiny85.R3_ADDR = 0x03  -- General Purpose Register 3
ATtiny85.R4_ADDR = 0x04  -- General Purpose Register 4
ATtiny85.R5_ADDR = 0x05  -- General Purpose Register 5
ATtiny85.R6_ADDR = 0x06  -- General Purpose Register 6
ATtiny85.R7_ADDR = 0x07  -- General Purpose Register 7
ATtiny85.R8_ADDR = 0x08  -- General Purpose Register 8
ATtiny85.R9_ADDR = 0x09  -- General Purpose Register 9
ATtiny85.R10_ADDR = 0x0A  -- General Purpose Register 10
ATtiny85.R11_ADDR = 0x0B  -- General Purpose Register 11
ATtiny85.R12_ADDR = 0x0C  -- General Purpose Register 12
ATtiny85.R13_ADDR = 0x0D  -- General Purpose Register 13
ATtiny85.R14_ADDR = 0x0E  -- General Purpose Register 14
ATtiny85.R15_ADDR = 0x0F  -- General Purpose Register 15
ATtiny85.R16_ADDR = 0x10  -- General Purpose Register 16
ATtiny85.R17_ADDR = 0x11  -- General Purpose Register 17
ATtiny85.R18_ADDR = 0x12  -- General Purpose Register 18
ATtiny85.R19_ADDR = 0x13  -- General Purpose Register 19
ATtiny85.R20_ADDR = 0x14  -- General Purpose Register 20
ATtiny85.R21_ADDR = 0x15  -- General Purpose Register 21
ATtiny85.R22_ADDR = 0x16  -- General Purpose Register 22
ATtiny85.R23_ADDR = 0x17  -- General Purpose Register 23
ATtiny85.R24_ADDR = 0x18  -- General Purpose Register 24
ATtiny85.R25_ADDR = 0x19  -- General Purpose Register 25
ATtiny85.X_ADDR = 0x1A  -- Register pair X (R27:R26)
ATtiny85.Y_ADDR = 0x1C  -- Register pair Y (R29:R28)
ATtiny85.Z_ADDR = 0x1E  -- Register pair Z (R31:R30)
ATtiny85.SP_ADDR = 0x3D  -- Stack Pointer
ATtiny85.SREG_ADDR = 0x3F  -- Status Register
ATtiny85.SREG_C_BIT = 0  -- Carry Flag
ATtiny85.SREG_Z_BIT = 1  -- Zero Flag
ATtiny85.SREG_N_BIT = 2  -- Negative Flag
ATtiny85.SREG_V_BIT = 3  -- Two's Complement Overflow Flag
ATtiny85.SREG_S_BIT = 4  -- Sign Flag (N xor V)
ATtiny85.SREG_H_BIT = 5  -- Half Carry Flag
ATtiny85.SREG_T_BIT = 6  -- Transfer Bit
ATtiny85.SREG_I_BIT = 7  -- Global Interrupt Enable

-- 内存段定义
ATtiny85.FLASH_START = 0x0000
ATtiny85.FLASH_END = 0x1FFF
ATtiny85.FLASH_SIZE = 8192  -- Program Flash (8KB)
ATtiny85.SRAM_START = 0x0060
ATtiny85.SRAM_END = 0x025F
ATtiny85.SRAM_SIZE = 512  -- Internal SRAM (512B)
ATtiny85.EEPROM_START = 0x0000
ATtiny85.EEPROM_END = 0x01FF
ATtiny85.EEPROM_SIZE = 512  -- EEPROM (512B)
ATtiny85.IO_START = 0x00
ATtiny85.IO_END = 0x3F
ATtiny85.IO_SIZE = 64  -- I/O Registers

-- 外设定义
-- Port A
ATtiny85.PORTA_BASE = 0x20
ATtiny85.PORTA_PINA_ADDR = 0x20
ATtiny85.PORTA_DDRA_ADDR = 0x21
ATtiny85.PORTA_PORTA_ADDR = 0x22
-- Port B
ATtiny85.PORTB_BASE = 0x18
ATtiny85.PORTB_PINB_ADDR = 0x16
ATtiny85.PORTB_DDRB_ADDR = 0x17
ATtiny85.PORTB_PORTB_ADDR = 0x18
-- Timer/Counter0
ATtiny85.TIPO_BASE = 0x20
ATtiny85.TIPO_TCCR0A_ADDR = 0x20
ATtiny85.TIPO_TCCR0A_WGM00_BIT = 0  -- Waveform Generation Mode
ATtiny85.TIPO_TCCR0A_WGM01_BIT = 1  -- Waveform Generation Mode
ATtiny85.TIPO_TCCR0A_COM0B0_BIT = 4  -- Compare Output Mode B
ATtiny85.TIPO_TCCR0A_COM0B1_BIT = 5  -- Compare Output Mode B
ATtiny85.TIPO_TCCR0A_COM0A0_BIT = 6  -- Compare Output Mode A
ATtiny85.TIPO_TCCR0A_COM0A1_BIT = 7  -- Compare Output Mode A
ATtiny85.TIPO_TCCR0B_ADDR = 0x21
ATtiny85.TIPO_TCCR0B_CS00_BIT = 0  -- Clock Select
ATtiny85.TIPO_TCCR0B_CS01_BIT = 1  -- Clock Select
ATtiny85.TIPO_TCCR0B_CS02_BIT = 2  -- Clock Select
ATtiny85.TIPO_TCCR0B_WGM02_BIT = 3  -- Waveform Generation Mode
ATtiny85.TIPO_TCCR0B_FOC0B_BIT = 6  -- Force Output Compare B
ATtiny85.TIPO_TCCR0B_FOC0A_BIT = 7  -- Force Output Compare A
ATtiny85.TIPO_TCNT0_ADDR = 0x22
ATtiny85.TIPO_OCR0A_ADDR = 0x23
ATtiny85.TIPO_OCR0B_ADDR = 0x24
ATtiny85.TIPO_TIMSK_ADDR = 0x39
ATtiny85.TIPO_TIMSK_TOIE0_BIT = 0  -- Timer/Counter0 Overflow Interrupt Enable
ATtiny85.TIPO_TIMSK_OCIE0A_BIT = 1  -- Output Compare A Match Interrupt Enable
ATtiny85.TIPO_TIMSK_OCIE0B_BIT = 2  -- Output Compare B Match Interrupt Enable
ATtiny85.TIPO_TIFR_ADDR = 0x38
ATtiny85.TIPO_TIFR_TOV0_BIT = 0  -- Timer/Counter0 Overflow Flag
ATtiny85.TIPO_TIFR_OCF0A_BIT = 1  -- Output Compare A Flag
ATtiny85.TIPO_TIFR_OCF0B_BIT = 2  -- Output Compare B Flag
-- Timer/Counter1
ATtiny85.TMR1_BASE = 0x28
ATtiny85.TMR1_TCCR1A_ADDR = 0x28
ATtiny85.TMR1_TCCR1A_PCM1_BIT = 0  -- PWM Mode
ATtiny85.TMR1_TCCR1A_COM1A_BIT = 0  -- Compare Output Mode A
ATtiny85.TMR1_TCCR1A_COM1B_BIT = 0  -- Compare Output Mode B
ATtiny85.TMR1_TCCR1A_WG13_BIT = 1  -- Waveform Generation Mode
ATtiny85.TMR1_TCCR1A_WG10_BIT = 0  -- Waveform Generation Mode
ATtiny85.TMR1_TCCR1B_ADDR = 0x29
ATtiny85.TMR1_TCCR1B_CTC1_BIT = 7  -- Clear Timer on Compare
ATtiny85.TMR1_TCCR1B_WGM13_BIT = 4  -- Waveform Generation Mode
ATtiny85.TMR1_TCCR1B_WGM12_BIT = 3  -- Waveform Generation Mode
ATtiny85.TMR1_TCCR1B_CS1_BIT = 0  -- Clock Select
ATtiny85.TMR1_TCNT1_ADDR = 0x2A
ATtiny85.TMR1_OCR1A_ADDR = 0x2C
ATtiny85.TMR1_OCR1B_ADDR = 0x2E
ATtiny85.TMR1_OCR1C_ADDR = 0x30
ATtiny85.TMR1_TIMSK1_ADDR = 0x33
ATtiny85.TMR1_TIFR1_ADDR = 0x32
-- ADC Multiplexer
ATtiny85.ADMUX_BASE = 0x12
ATtiny85.ADMUX_ADMUX_ADDR = 0x12
ATtiny85.ADMUX_ADMUX_MUX_BIT = 0  -- Analog Channel Selection
ATtiny85.ADMUX_ADMUX_ADLAR_BIT = 5  -- ADC Left Adjust Result
ATtiny85.ADMUX_ADMUX_REFS_BIT = 0  -- Reference Selection
ATtiny85.ADMUX_ADCSRA_ADDR = 0x13
ATtiny85.ADMUX_ADCSRA_ADPS_BIT = 0  -- ADC Prescaler Select
ATtiny85.ADMUX_ADCSRA_ADIE_BIT = 3  -- ADC Interrupt Enable
ATtiny85.ADMUX_ADCSRA_ADIF_BIT = 4  -- ADC Interrupt Flag
ATtiny85.ADMUX_ADCSRA_ADATE_BIT = 5  -- ADC Auto Trigger Enable
ATtiny85.ADMUX_ADCSRA_ADSC_BIT = 6  -- ADC Start Conversion
ATtiny85.ADMUX_ADCSRA_ADEN_BIT = 7  -- ADC Enable
ATtiny85.ADMUX_ADCH_ADDR = 0x14
ATtiny85.ADMUX_ADCL_ADDR = 0x15
-- Universal Serial Interface
ATtiny85.USI_BASE = 0x18
ATtiny85.USI_USIDR_ADDR = 0x18
ATtiny85.USI_USISR_ADDR = 0x19
ATtiny85.USI_USISR_USICNT_BIT = 0  -- Counter
ATtiny85.USI_USISR_USIDC_BIT = 4  -- Data Register
ATtiny85.USI_USISR_USIPF_BIT = 5  -- Stop Cond Flag
ATtiny85.USI_USISR_USIOV_BIT = 6  -- Overflow Flag
ATtiny85.USI_USISR_USISIF_BIT = 7  -- Start Cond Interrupt Flag
ATtiny85.USI_USICR_ADDR = 0x1A
ATtiny85.USI_USICR_USICS_BIT = 0  -- Clock Source Select
ATtiny85.USI_USICR_USISCL_BIT = 2  -- SCL strobe
ATtiny85.USI_USICR_USIOW_BIT = 3  -- SDA output override
ATtiny85.USI_USICR_USIOE_BIT = 4  -- Output Enable
ATtiny85.USI_USICR_USISRE_BIT = 5  -- Start Recognition Enable
ATtiny85.USI_USICR_USIORE_BIT = 6  -- Stop Recognition Enable
ATtiny85.USI_USICR_USIGIE_BIT = 7  -- Global Interrupt Enable
ATtiny85.USI_USIPORT_ADDR = 0x1B
-- MCU Control
ATtiny85.MCUCR_BASE = 0x35
ATtiny85.MCUCR_MCUCR_ADDR = 0x35
ATtiny85.MCUCR_MCUCR_ISC_BIT = 0  -- Interrupt Sense Control
ATtiny85.MCUCR_MCUCR_SE_BIT = 4  -- Sleep Enable
ATtiny85.MCUCR_MCUCR_SM_BIT = 0  -- Sleep Mode
ATtiny85.MCUCR_MCUCSR_ADDR = 0x36
ATtiny85.MCUCR_MCUCSR_PORF_BIT = 0  -- Power-on Reset Flag
ATtiny85.MCUCR_MCUCSR_EXTRF_BIT = 1  -- External Reset Flag
ATtiny85.MCUCR_MCUCSR_WDRF_BIT = 2  -- Watchdog Reset Flag
ATtiny85.MCUCR_MCUCSR_BORF_BIT = 4  -- Brown-out Reset Flag
-- Watchdog Timer
ATtiny85.WDTCR_BASE = 0x21
ATtiny85.WDTCR_WDTCR_ADDR = 0x21
ATtiny85.WDTCR_WDTCR_WDP_BIT = 0  -- Watchdog Prescaler
ATtiny85.WDTCR_WDTCR_WDE_BIT = 3  -- Watchdog Enable
ATtiny85.WDTCR_WDTCR_WDIE_BIT = 4  -- Watchdog Interrupt Enable
-- EEPROM
ATtiny85.EEPR_BASE = 0x1C
ATtiny85.EEPR_EEAR_ADDR = 0x1E
ATtiny85.EEPR_EEDR_ADDR = 0x1D
ATtiny85.EEPR_EECR_ADDR = 0x1F
ATtiny85.EEPR_EECR_EEPM_BIT = 0  -- EEPROM Programming Mode
ATtiny85.EEPR_EECR_EERIE_BIT = 3  -- EEPROM Ready Interrupt Enable
ATtiny85.EEPR_EECR_EEWE_BIT = 2  -- EEPROM Write Enable
ATtiny85.EEPR_EECR_EEMWE_BIT = 1  -- EEPROM Master Write Enable
ATtiny85.EEPR_EECR_EERE_BIT = 0  -- EEPROM Read Enable
-- External Interrupt
ATtiny85.GIMSK_BASE = 0x3B
ATtiny85.GIMSK_GIMSK_ADDR = 0x3B
ATtiny85.GIMSK_GIMSK_INT0_BIT = 0  -- External Interrupt Request 0 Enable
ATtiny85.GIMSK_GIMSK_PCIE_BIT = 1  -- Pin Change Interrupt Enable
ATtiny85.GIMSK_GIFR_ADDR = 0x3C
ATtiny85.GIMSK_GIFR_INTF0_BIT = 0  -- External Interrupt Flag 0
ATtiny85.GIMSK_GIFR_PCIF_BIT = 1  -- Pin Change Interrupt Flag
-- Pin Change Mask
ATtiny85.PCMSK_BASE = 0x15
ATtiny85.PCMSK_PCMSK_ADDR = 0x15
-- Store Program Memory
ATtiny85.SPMCSR_BASE = 0x37
ATtiny85.SPMCSR_SPMCSR_ADDR = 0x37
ATtiny85.SPMCSR_SPMCSR_SPMCR_BIT = 0  -- SPM Mode
ATtiny85.SPMCSR_SPMCSR_PGERS_BIT = 1  -- Page Erase
ATtiny85.SPMCSR_SPMCSR_PGWRT_BIT = 2  -- Page Write
ATtiny85.SPMCSR_SPMCSR_BLBSET_BIT = 3  -- Boot Lock Bits Set
ATtiny85.SPMCSR_SPMCSR_RWWSRE_BIT = 4  -- Read-While-Read Strobe Enable
ATtiny85.SPMCSR_SPMCSR_SIGRD_BIT = 5  -- Signature Row Read
ATtiny85.SPMCSR_SPMCSR_SPMEN_BIT = 7  -- SPM Enable

-- 中断向量定义
ATtiny85.INT_RESET = 0  -- External Reset, Power-on Reset, Brown-out Reset
ATtiny85.INT_INT0 = 1  -- External Interrupt Request 0
ATtiny85.INT_PCINT0 = 2  -- Pin Change
ATtiny85.INT_WDT = 3  -- Watchdog Timeout
ATtiny85.INT_TIM1_COMPA = 4  -- Timer/Counter1 Compare Match A
ATtiny85.INT_TIM1_OVF = 5  -- Timer/Counter1 Overflow
ATtiny85.INT_TIM0_COMPA = 6  -- Timer/Counter0 Compare Match A
ATtiny85.INT_TIM0_OVF = 7  -- Timer/Counter0 Overflow
ATtiny85.INT_SPI_STC = 8  -- SPI Serial Transfer Complete
ATtiny85.INT_ADC = 9  -- ADC Conversion Complete
ATtiny85.INT_USI_START = 10  -- USI Start Condition
ATtiny85.INT_USI_OVF = 11  -- USI Overflow
ATtiny85.INT_EE_READY = 12  -- EEPROM Ready

-- 引脚定义
ATtiny85.PIN_PB5 = 1  -- RESET - ADC0 - dW
ATtiny85.PIN_PB3 = 2  -- XTAL1 - CLKI - ADC3
ATtiny85.PIN_PB4 = 3  -- XTAL2 - ADC2
ATtiny85.PIN_PB0 = 4  -- MOSI - AI - ADC0 - T0 - INT0
ATtiny85.PIN_PB1 = 5  -- MISO - AI - ADC1 - OC1A - INT1
ATtiny85.PIN_PB2 = 6  -- SCK - AI - ADC3 - OC1B
ATtiny85.PIN_VCC = 7  -- Supply Voltage
ATtiny85.PIN_GND = 8  -- Ground

-- 设备类
function ATtiny85.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "General Purpose Register 1",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x08,
            size = 1,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x09,
            size = 1,
            access = "rw",
            description = "General Purpose Register 9",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x0A,
            size = 1,
            access = "rw",
            description = "General Purpose Register 10",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x0B,
            size = 1,
            access = "rw",
            description = "General Purpose Register 11",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x0C,
            size = 1,
            access = "rw",
            description = "General Purpose Register 12",
            value = 0
        }
        self.registers["R13"] = {
            address = 0x0D,
            size = 1,
            access = "rw",
            description = "General Purpose Register 13",
            value = 0
        }
        self.registers["R14"] = {
            address = 0x0E,
            size = 1,
            access = "rw",
            description = "General Purpose Register 14",
            value = 0
        }
        self.registers["R15"] = {
            address = 0x0F,
            size = 1,
            access = "rw",
            description = "General Purpose Register 15",
            value = 0
        }
        self.registers["R16"] = {
            address = 0x10,
            size = 1,
            access = "rw",
            description = "General Purpose Register 16",
            value = 0
        }
        self.registers["R17"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "General Purpose Register 17",
            value = 0
        }
        self.registers["R18"] = {
            address = 0x12,
            size = 1,
            access = "rw",
            description = "General Purpose Register 18",
            value = 0
        }
        self.registers["R19"] = {
            address = 0x13,
            size = 1,
            access = "rw",
            description = "General Purpose Register 19",
            value = 0
        }
        self.registers["R20"] = {
            address = 0x14,
            size = 1,
            access = "rw",
            description = "General Purpose Register 20",
            value = 0
        }
        self.registers["R21"] = {
            address = 0x15,
            size = 1,
            access = "rw",
            description = "General Purpose Register 21",
            value = 0
        }
        self.registers["R22"] = {
            address = 0x16,
            size = 1,
            access = "rw",
            description = "General Purpose Register 22",
            value = 0
        }
        self.registers["R23"] = {
            address = 0x17,
            size = 1,
            access = "rw",
            description = "General Purpose Register 23",
            value = 0
        }
        self.registers["R24"] = {
            address = 0x18,
            size = 1,
            access = "rw",
            description = "General Purpose Register 24",
            value = 0
        }
        self.registers["R25"] = {
            address = 0x19,
            size = 1,
            access = "rw",
            description = "General Purpose Register 25",
            value = 0
        }
        self.registers["X"] = {
            address = 0x1A,
            size = 2,
            access = "rw",
            description = "Register pair X (R27:R26)",
            value = 0
        }
        self.registers["Y"] = {
            address = 0x1C,
            size = 2,
            access = "rw",
            description = "Register pair Y (R29:R28)",
            value = 0
        }
        self.registers["Z"] = {
            address = 0x1E,
            size = 2,
            access = "rw",
            description = "Register pair Z (R31:R30)",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x3D,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["SREG"] = {
            address = 0x3F,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTA"] = {
            base = 0x20,
            type = "gpio",
            description = "Port A",
            registers = {}
        }
        
        local p = self.peripherals["PORTA"]
        p.registers["PINA"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["DDRA"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["PORTA"] = {
            address = 0x22,
            size = 1,
            value = 0
        }
        self.peripherals["PORTB"] = {
            base = 0x18,
            type = "gpio",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["PINB"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["PORTB"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        self.peripherals["TIPO"] = {
            base = 0x20,
            type = "timer",
            description = "Timer/Counter0",
            registers = {}
        }
        
        local p = self.peripherals["TIPO"]
        p.registers["TCCR0A"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["TCCR0B"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["TCNT0"] = {
            address = 0x22,
            size = 1,
            value = 0
        }
        p.registers["OCR0A"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        p.registers["OCR0B"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["TIMSK"] = {
            address = 0x39,
            size = 1,
            value = 0
        }
        p.registers["TIFR"] = {
            address = 0x38,
            size = 1,
            value = 0
        }
        self.peripherals["TMR1"] = {
            base = 0x28,
            type = "timer",
            description = "Timer/Counter1",
            registers = {}
        }
        
        local p = self.peripherals["TMR1"]
        p.registers["TCCR1A"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["TCCR1B"] = {
            address = 0x29,
            size = 1,
            value = 0
        }
        p.registers["TCNT1"] = {
            address = 0x2A,
            size = 2,
            value = 0
        }
        p.registers["OCR1A"] = {
            address = 0x2C,
            size = 2,
            value = 0
        }
        p.registers["OCR1B"] = {
            address = 0x2E,
            size = 2,
            value = 0
        }
        p.registers["OCR1C"] = {
            address = 0x30,
            size = 2,
            value = 0
        }
        p.registers["TIMSK1"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["TIFR1"] = {
            address = 0x32,
            size = 1,
            value = 0
        }
        self.peripherals["ADMUX"] = {
            base = 0x12,
            type = "adc",
            description = "ADC Multiplexer",
            registers = {}
        }
        
        local p = self.peripherals["ADMUX"]
        p.registers["ADMUX"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["ADCSRA"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["ADCH"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["ADCL"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        self.peripherals["USI"] = {
            base = 0x18,
            type = "usi",
            description = "Universal Serial Interface",
            registers = {}
        }
        
        local p = self.peripherals["USI"]
        p.registers["USIDR"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["USISR"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["USICR"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["USIPORT"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        self.peripherals["MCUCR"] = {
            base = 0x35,
            type = "sys",
            description = "MCU Control",
            registers = {}
        }
        
        local p = self.peripherals["MCUCR"]
        p.registers["MCUCR"] = {
            address = 0x35,
            size = 1,
            value = 0
        }
        p.registers["MCUCSR"] = {
            address = 0x36,
            size = 1,
            value = 0
        }
        self.peripherals["WDTCR"] = {
            base = 0x21,
            type = "wdt",
            description = "Watchdog Timer",
            registers = {}
        }
        
        local p = self.peripherals["WDTCR"]
        p.registers["WDTCR"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        self.peripherals["EEPR"] = {
            base = 0x1C,
            type = "eeprom",
            description = "EEPROM",
            registers = {}
        }
        
        local p = self.peripherals["EEPR"]
        p.registers["EEAR"] = {
            address = 0x1E,
            size = 1,
            value = 0
        }
        p.registers["EEDR"] = {
            address = 0x1D,
            size = 1,
            value = 0
        }
        p.registers["EECR"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        self.peripherals["GIMSK"] = {
            base = 0x3B,
            type = "exti",
            description = "External Interrupt",
            registers = {}
        }
        
        local p = self.peripherals["GIMSK"]
        p.registers["GIMSK"] = {
            address = 0x3B,
            size = 1,
            value = 0
        }
        p.registers["GIFR"] = {
            address = 0x3C,
            size = 1,
            value = 0
        }
        self.peripherals["PCMSK"] = {
            base = 0x15,
            type = "pcint",
            description = "Pin Change Mask",
            registers = {}
        }
        
        local p = self.peripherals["PCMSK"]
        p.registers["PCMSK"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        self.peripherals["SPMCSR"] = {
            base = 0x37,
            type = "spm",
            description = "Store Program Memory",
            registers = {}
        }
        
        local p = self.peripherals["SPMCSR"]
        p.registers["SPMCSR"] = {
            address = 0x37,
            size = 1,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = ATtiny85.DEVICE_NAME,
            manufacturer = ATtiny85.MANUFACTURER,
            family = ATtiny85.FAMILY,
            version = ATtiny85.VERSION,
            architecture = ATtiny85.ARCHITECTURE,
            bits = ATtiny85.BITS,
            clock_frequency = ATtiny85.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("ATtiny85(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ATtiny85.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ATtiny85.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ATtiny85.print_device_info(device)
    device = device or ATtiny85.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function ATtiny85.print_registers(device)
    device = device or ATtiny85.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ATtiny85.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ATtiny85.example()
    print("=== ATtiny85设备示例 ===")
    
    -- 创建设备实例
    local device = ATtiny85.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ATtiny85.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ATtiny85.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ATtiny85.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ATtiny85.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ATtiny85.lua$") then
    ATtiny85.example()
end

return ATtiny85
