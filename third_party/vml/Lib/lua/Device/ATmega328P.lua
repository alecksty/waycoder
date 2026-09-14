--[[
  ATmega328P设备定义 - Lua模块
  生成自: Atmel/AVR/ATmega328P
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 8-bit AVR microcontroller with 32KB Flash, 2KB SRAM, 1KB EEPROM
  CPU架构: AVR
  位宽: 8位
  时钟频率: 16000000 Hz
]]

local ATmega328P = {}

-- 设备信息
ATmega328P.DEVICE_NAME = "ATmega328P"
ATmega328P.MANUFACTURER = "Atmel"
ATmega328P.FAMILY = "AVR"
ATmega328P.VERSION = "1.0"
ATmega328P.ARCHITECTURE = "AVR"
ATmega328P.BITS = 8
ATmega328P.CLOCK_FREQUENCY = 16000000

-- 寄存器地址定义
ATmega328P.R0_ADDR = 0x00  -- General Purpose Register 0
ATmega328P.R1_ADDR = 0x01  -- General Purpose Register 1
ATmega328P.R2_ADDR = 0x02  -- General Purpose Register 2
ATmega328P.R3_ADDR = 0x03  -- General Purpose Register 3
ATmega328P.R4_ADDR = 0x04  -- General Purpose Register 4
ATmega328P.R5_ADDR = 0x05  -- General Purpose Register 5
ATmega328P.R6_ADDR = 0x06  -- General Purpose Register 6
ATmega328P.R7_ADDR = 0x07  -- General Purpose Register 7
ATmega328P.R8_ADDR = 0x08  -- General Purpose Register 8
ATmega328P.R9_ADDR = 0x09  -- General Purpose Register 9
ATmega328P.R10_ADDR = 0x0A  -- General Purpose Register 10
ATmega328P.R11_ADDR = 0x0B  -- General Purpose Register 11
ATmega328P.R12_ADDR = 0x0C  -- General Purpose Register 12
ATmega328P.R13_ADDR = 0x0D  -- General Purpose Register 13
ATmega328P.R14_ADDR = 0x0E  -- General Purpose Register 14
ATmega328P.R15_ADDR = 0x0F  -- General Purpose Register 15
ATmega328P.R16_ADDR = 0x10  -- General Purpose Register 16
ATmega328P.R17_ADDR = 0x11  -- General Purpose Register 17
ATmega328P.R18_ADDR = 0x12  -- General Purpose Register 18
ATmega328P.R19_ADDR = 0x13  -- General Purpose Register 19
ATmega328P.R20_ADDR = 0x14  -- General Purpose Register 20
ATmega328P.R21_ADDR = 0x15  -- General Purpose Register 21
ATmega328P.R22_ADDR = 0x16  -- General Purpose Register 22
ATmega328P.R23_ADDR = 0x17  -- General Purpose Register 23
ATmega328P.R24_ADDR = 0x18  -- General Purpose Register 24
ATmega328P.R25_ADDR = 0x19  -- General Purpose Register 25
ATmega328P.R26_ADDR = 0x1A  -- General Purpose Register 26 (XL)
ATmega328P.R27_ADDR = 0x1B  -- General Purpose Register 27 (XH)
ATmega328P.R28_ADDR = 0x1C  -- General Purpose Register 28 (YL)
ATmega328P.R29_ADDR = 0x1D  -- General Purpose Register 29 (YH)
ATmega328P.R30_ADDR = 0x1E  -- General Purpose Register 30 (ZL)
ATmega328P.R31_ADDR = 0x1F  -- General Purpose Register 31 (ZH)
ATmega328P.SPL_ADDR = 0x5D  -- Stack Pointer Low
ATmega328P.SPH_ADDR = 0x5E  -- Stack Pointer High
ATmega328P.SREG_ADDR = 0x5F  -- Status Register
ATmega328P.SREG_C_BIT = 0  -- Carry Flag
ATmega328P.SREG_Z_BIT = 1  -- Zero Flag
ATmega328P.SREG_N_BIT = 2  -- Negative Flag
ATmega328P.SREG_V_BIT = 3  -- Two's Complement Overflow Flag
ATmega328P.SREG_S_BIT = 4  -- Sign Flag (N ⊕ V)
ATmega328P.SREG_H_BIT = 5  -- Half Carry Flag
ATmega328P.SREG_T_BIT = 6  -- Transfer Bit
ATmega328P.SREG_I_BIT = 7  -- Global Interrupt Enable

-- 内存段定义
ATmega328P.FLASH_START = 0x0000
ATmega328P.FLASH_END = 0x7FFF
ATmega328P.FLASH_SIZE = 32768  -- Program Flash Memory
ATmega328P.SRAM_START = 0x0100
ATmega328P.SRAM_END = 0x08FF
ATmega328P.SRAM_SIZE = 2048  -- Static RAM
ATmega328P.EEPROM_START = 0x0000
ATmega328P.EEPROM_END = 0x03FF
ATmega328P.EEPROM_SIZE = 1024  -- EEPROM
ATmega328P.IO_START = 0x00
ATmega328P.IO_END = 0x3F
ATmega328P.IO_SIZE = 64  -- I/O Registers
ATmega328P.EXTIO_START = 0x40
ATmega328P.EXTIO_END = 0xFF
ATmega328P.EXTIO_SIZE = 192  -- Extended I/O Registers

-- 外设定义
-- Port B Data Register
ATmega328P.PORTB_BASE = 0x23
ATmega328P.PORTB_PORTB_ADDR = 0x25
ATmega328P.PORTB_DDRB_ADDR = 0x24
ATmega328P.PORTB_PINB_ADDR = 0x23
-- Port C Data Register
ATmega328P.PORTC_BASE = 0x26
ATmega328P.PORTC_PORTC_ADDR = 0x28
ATmega328P.PORTC_DDRC_ADDR = 0x27
ATmega328P.PORTC_PINC_ADDR = 0x26
-- Port D Data Register
ATmega328P.PORTD_BASE = 0x29
ATmega328P.PORTD_PORTD_ADDR = 0x2B
ATmega328P.PORTD_DDRD_ADDR = 0x2A
ATmega328P.PORTD_PIND_ADDR = 0x29
-- 8-bit Timer/Counter0
ATmega328P.TIMER0_BASE = 0x44
ATmega328P.TIMER0_TCCR0A_ADDR = 0x44
ATmega328P.TIMER0_TCCR0A_WGM00_BIT = 0  -- Waveform Generation Mode
ATmega328P.TIMER0_TCCR0A_WGM01_BIT = 1  -- Waveform Generation Mode
ATmega328P.TIMER0_TCCR0A_COM0B0_BIT = 4  -- Compare Output Mode for Channel B
ATmega328P.TIMER0_TCCR0A_COM0B1_BIT = 5  -- Compare Output Mode for Channel B
ATmega328P.TIMER0_TCCR0A_COM0A0_BIT = 6  -- Compare Output Mode for Channel A
ATmega328P.TIMER0_TCCR0A_COM0A1_BIT = 7  -- Compare Output Mode for Channel A
ATmega328P.TIMER0_TCCR0B_ADDR = 0x45
ATmega328P.TIMER0_TCCR0B_CS00_BIT = 0  -- Clock Select
ATmega328P.TIMER0_TCCR0B_CS01_BIT = 1  -- Clock Select
ATmega328P.TIMER0_TCCR0B_CS02_BIT = 2  -- Clock Select
ATmega328P.TIMER0_TCCR0B_WGM02_BIT = 3  -- Waveform Generation Mode
ATmega328P.TIMER0_TCCR0B_FOC0B_BIT = 6  -- Force Output Compare B
ATmega328P.TIMER0_TCCR0B_FOC0A_BIT = 7  -- Force Output Compare A
ATmega328P.TIMER0_TCNT0_ADDR = 0x46
ATmega328P.TIMER0_OCR0A_ADDR = 0x47
ATmega328P.TIMER0_OCR0B_ADDR = 0x48
ATmega328P.TIMER0_TIMSK0_ADDR = 0x6E
ATmega328P.TIMER0_TIMSK0_TOIE0_BIT = 0  -- Timer/Counter0 Overflow Interrupt Enable
ATmega328P.TIMER0_TIMSK0_OCIE0A_BIT = 1  -- Timer/Counter0 Output Compare A Match Interrupt Enable
ATmega328P.TIMER0_TIMSK0_OCIE0B_BIT = 2  -- Timer/Counter0 Output Compare B Match Interrupt Enable
ATmega328P.TIMER0_TIFR0_ADDR = 0x35
ATmega328P.TIMER0_TIFR0_TOV0_BIT = 0  -- Timer/Counter0 Overflow Flag
ATmega328P.TIMER0_TIFR0_OCF0A_BIT = 1  -- Output Compare Flag 0A
ATmega328P.TIMER0_TIFR0_OCF0B_BIT = 2  -- Output Compare Flag 0B
-- Universal Synchronous/Asynchronous Receiver/Transmitter
ATmega328P.USART0_BASE = 0xC0
ATmega328P.USART0_UDR0_ADDR = 0xC6
ATmega328P.USART0_UCSR0A_ADDR = 0xC0
ATmega328P.USART0_UCSR0A_MPCM0_BIT = 0  -- Multi-processor Communication Mode
ATmega328P.USART0_UCSR0A_U2X0_BIT = 1  -- Double the USART Transmission Speed
ATmega328P.USART0_UCSR0A_UPE0_BIT = 2  -- Parity Error
ATmega328P.USART0_UCSR0A_DOR0_BIT = 3  -- Data OverRun
ATmega328P.USART0_UCSR0A_FE0_BIT = 4  -- Frame Error
ATmega328P.USART0_UCSR0A_UDRE0_BIT = 5  -- USART Data Register Empty
ATmega328P.USART0_UCSR0A_TXC0_BIT = 6  -- USART Transmit Complete
ATmega328P.USART0_UCSR0A_RXC0_BIT = 7  -- USART Receive Complete
ATmega328P.USART0_UCSR0B_ADDR = 0xC1
ATmega328P.USART0_UCSR0B_TXB80_BIT = 0  -- Transmit Data Bit 8
ATmega328P.USART0_UCSR0B_RXB80_BIT = 1  -- Receive Data Bit 8
ATmega328P.USART0_UCSR0B_UCSZ02_BIT = 2  -- Character Size
ATmega328P.USART0_UCSR0B_TXEN0_BIT = 3  -- Transmitter Enable
ATmega328P.USART0_UCSR0B_RXEN0_BIT = 4  -- Receiver Enable
ATmega328P.USART0_UCSR0B_UDRIE0_BIT = 5  -- USART Data Register Empty Interrupt Enable
ATmega328P.USART0_UCSR0B_TXCIE0_BIT = 6  -- TX Complete Interrupt Enable
ATmega328P.USART0_UCSR0B_RXCIE0_BIT = 7  -- RX Complete Interrupt Enable
ATmega328P.USART0_UCSR0C_ADDR = 0xC2
ATmega328P.USART0_UCSR0C_UCPOL0_BIT = 0  -- Clock Polarity
ATmega328P.USART0_UCSR0C_UCSZ00_BIT = 1  -- Character Size
ATmega328P.USART0_UCSR0C_UCSZ01_BIT = 2  -- Character Size
ATmega328P.USART0_UCSR0C_USBS0_BIT = 3  -- Stop Bit Select
ATmega328P.USART0_UCSR0C_UPM00_BIT = 4  -- Parity Mode
ATmega328P.USART0_UCSR0C_UPM01_BIT = 5  -- Parity Mode
ATmega328P.USART0_UCSR0C_UMSEL00_BIT = 6  -- USART Mode Select
ATmega328P.USART0_UCSR0C_UMSEL01_BIT = 7  -- USART Mode Select
ATmega328P.USART0_UBRR0_ADDR = 0xC4
-- Analog-to-Digital Converter
ATmega328P.ADC_BASE = 0x78
ATmega328P.ADC_ADMUX_ADDR = 0x7C
ATmega328P.ADC_ADMUX_MUX0_BIT = 0  -- Analog Channel Selection
ATmega328P.ADC_ADMUX_MUX1_BIT = 1  -- Analog Channel Selection
ATmega328P.ADC_ADMUX_MUX2_BIT = 2  -- Analog Channel Selection
ATmega328P.ADC_ADMUX_MUX3_BIT = 3  -- Analog Channel Selection
ATmega328P.ADC_ADMUX_ADLAR_BIT = 5  -- ADC Left Adjust Result
ATmega328P.ADC_ADMUX_REFS0_BIT = 6  -- Reference Selection
ATmega328P.ADC_ADMUX_REFS1_BIT = 7  -- Reference Selection
ATmega328P.ADC_ADCSRA_ADDR = 0x7A
ATmega328P.ADC_ADCSRA_ADPS0_BIT = 0  -- ADC Prescaler Select
ATmega328P.ADC_ADCSRA_ADPS1_BIT = 1  -- ADC Prescaler Select
ATmega328P.ADC_ADCSRA_ADPS2_BIT = 2  -- ADC Prescaler Select
ATmega328P.ADC_ADCSRA_ADIE_BIT = 3  -- ADC Interrupt Enable
ATmega328P.ADC_ADCSRA_ADIF_BIT = 4  -- ADC Interrupt Flag
ATmega328P.ADC_ADCSRA_ADATE_BIT = 5  -- ADC Auto Trigger Enable
ATmega328P.ADC_ADCSRA_ADSC_BIT = 6  -- ADC Start Conversion
ATmega328P.ADC_ADCSRA_ADEN_BIT = 7  -- ADC Enable
ATmega328P.ADC_ADCH_ADDR = 0x79
ATmega328P.ADC_ADCL_ADDR = 0x78

-- 中断向量定义
ATmega328P.INT_INT0 = 1  -- External Interrupt Request 0
ATmega328P.INT_INT1 = 2  -- External Interrupt Request 1
ATmega328P.INT_PCINT0 = 3  -- Pin Change Interrupt Request 0
ATmega328P.INT_PCINT1 = 4  -- Pin Change Interrupt Request 1
ATmega328P.INT_PCINT2 = 5  -- Pin Change Interrupt Request 2
ATmega328P.INT_WDT = 6  -- Watchdog Time-out Interrupt
ATmega328P.INT_TIMER2_COMPA = 7  -- Timer/Counter2 Compare Match A
ATmega328P.INT_TIMER2_COMPB = 8  -- Timer/Counter2 Compare Match B
ATmega328P.INT_TIMER2_OVF = 9  -- Timer/Counter2 Overflow
ATmega328P.INT_TIMER1_CAPT = 10  -- Timer/Counter1 Capture Event
ATmega328P.INT_TIMER1_COMPA = 11  -- Timer/Counter1 Compare Match A
ATmega328P.INT_TIMER1_COMPB = 12  -- Timer/Counter1 Compare Match B
ATmega328P.INT_TIMER1_OVF = 13  -- Timer/Counter1 Overflow
ATmega328P.INT_TIMER0_COMPA = 14  -- Timer/Counter0 Compare Match A
ATmega328P.INT_TIMER0_COMPB = 15  -- Timer/Counter0 Compare Match B
ATmega328P.INT_TIMER0_OVF = 16  -- Timer/Counter0 Overflow
ATmega328P.INT_SPI_STC = 17  -- SPI Serial Transfer Complete
ATmega328P.INT_USART_RX = 18  -- USART Rx Complete
ATmega328P.INT_USART_UDRE = 19  -- USART Data Register Empty
ATmega328P.INT_USART_TX = 20  -- USART Tx Complete
ATmega328P.INT_ADC = 21  -- ADC Conversion Complete
ATmega328P.INT_EE_READY = 22  -- EEPROM Ready
ATmega328P.INT_ANALOG_COMP = 23  -- Analog Comparator
ATmega328P.INT_TWI = 24  -- Two-wire Serial Interface
ATmega328P.INT_SPM_READY = 25  -- Store Program Memory Ready

-- 引脚定义
ATmega328P.PIN_PC6 = 1  -- Reset Pin
ATmega328P.PIN_PD0 = 2  -- Digital I/O, RX (USART)
ATmega328P.PIN_PD1 = 3  -- Digital I/O, TX (USART)
ATmega328P.PIN_PD2 = 4  -- Digital I/O, INT0
ATmega328P.PIN_PD3 = 5  -- Digital I/O, INT1, OC2B
ATmega328P.PIN_PD4 = 6  -- Digital I/O, T0, XCK
ATmega328P.PIN_VCC = 7  -- Supply Voltage
ATmega328P.PIN_GND = 8  -- Ground
ATmega328P.PIN_PB6 = 9  -- Digital I/O, XTAL1
ATmega328P.PIN_PB7 = 10  -- Digital I/O, XTAL2
ATmega328P.PIN_PD5 = 11  -- Digital I/O, T1, OC0B
ATmega328P.PIN_PD6 = 12  -- Digital I/O, AIN0, OC0A
ATmega328P.PIN_PD7 = 13  -- Digital I/O, AIN1
ATmega328P.PIN_PB0 = 14  -- Digital I/O, ICP1, CLKO
ATmega328P.PIN_PB1 = 15  -- Digital I/O, OC1A
ATmega328P.PIN_PB2 = 16  -- Digital I/O, SS, OC1B
ATmega328P.PIN_PB3 = 17  -- Digital I/O, MOSI, OC2A
ATmega328P.PIN_PB4 = 18  -- Digital I/O, MISO
ATmega328P.PIN_PB5 = 19  -- Digital I/O, SCK
ATmega328P.PIN_AVCC = 20  -- Supply Voltage for ADC
ATmega328P.PIN_AREF = 21  -- Analog Reference
ATmega328P.PIN_GND = 22  -- Ground
ATmega328P.PIN_PC0 = 23  -- Digital I/O, ADC0
ATmega328P.PIN_PC1 = 24  -- Digital I/O, ADC1
ATmega328P.PIN_PC2 = 25  -- Digital I/O, ADC2
ATmega328P.PIN_PC3 = 26  -- Digital I/O, ADC3
ATmega328P.PIN_PC4 = 27  -- Digital I/O, ADC4, SDA
ATmega328P.PIN_PC5 = 28  -- Digital I/O, ADC5, SCL

-- 设备类
function ATmega328P.new(memory_base)
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
        self.registers["R26"] = {
            address = 0x1A,
            size = 1,
            access = "rw",
            description = "General Purpose Register 26 (XL)",
            value = 0
        }
        self.registers["R27"] = {
            address = 0x1B,
            size = 1,
            access = "rw",
            description = "General Purpose Register 27 (XH)",
            value = 0
        }
        self.registers["R28"] = {
            address = 0x1C,
            size = 1,
            access = "rw",
            description = "General Purpose Register 28 (YL)",
            value = 0
        }
        self.registers["R29"] = {
            address = 0x1D,
            size = 1,
            access = "rw",
            description = "General Purpose Register 29 (YH)",
            value = 0
        }
        self.registers["R30"] = {
            address = 0x1E,
            size = 1,
            access = "rw",
            description = "General Purpose Register 30 (ZL)",
            value = 0
        }
        self.registers["R31"] = {
            address = 0x1F,
            size = 1,
            access = "rw",
            description = "General Purpose Register 31 (ZH)",
            value = 0
        }
        self.registers["SPL"] = {
            address = 0x5D,
            size = 1,
            access = "rw",
            description = "Stack Pointer Low",
            value = 0
        }
        self.registers["SPH"] = {
            address = 0x5E,
            size = 1,
            access = "rw",
            description = "Stack Pointer High",
            value = 0
        }
        self.registers["SREG"] = {
            address = 0x5F,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTB"] = {
            base = 0x23,
            type = "GPIO",
            description = "Port B Data Register",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["PORTB"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["PINB"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        self.peripherals["PORTC"] = {
            base = 0x26,
            type = "GPIO",
            description = "Port C Data Register",
            registers = {}
        }
        
        local p = self.peripherals["PORTC"]
        p.registers["PORTC"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["DDRC"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["PINC"] = {
            address = 0x26,
            size = 1,
            value = 0
        }
        self.peripherals["PORTD"] = {
            base = 0x29,
            type = "GPIO",
            description = "Port D Data Register",
            registers = {}
        }
        
        local p = self.peripherals["PORTD"]
        p.registers["PORTD"] = {
            address = 0x2B,
            size = 1,
            value = 0
        }
        p.registers["DDRD"] = {
            address = 0x2A,
            size = 1,
            value = 0
        }
        p.registers["PIND"] = {
            address = 0x29,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x44,
            type = "Timer",
            description = "8-bit Timer/Counter0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["TCCR0A"] = {
            address = 0x44,
            size = 1,
            value = 0
        }
        p.registers["TCCR0B"] = {
            address = 0x45,
            size = 1,
            value = 0
        }
        p.registers["TCNT0"] = {
            address = 0x46,
            size = 1,
            value = 0
        }
        p.registers["OCR0A"] = {
            address = 0x47,
            size = 1,
            value = 0
        }
        p.registers["OCR0B"] = {
            address = 0x48,
            size = 1,
            value = 0
        }
        p.registers["TIMSK0"] = {
            address = 0x6E,
            size = 1,
            value = 0
        }
        p.registers["TIFR0"] = {
            address = 0x35,
            size = 1,
            value = 0
        }
        self.peripherals["USART0"] = {
            base = 0xC0,
            type = "UART",
            description = "Universal Synchronous/Asynchronous Receiver/Transmitter",
            registers = {}
        }
        
        local p = self.peripherals["USART0"]
        p.registers["UDR0"] = {
            address = 0xC6,
            size = 1,
            value = 0
        }
        p.registers["UCSR0A"] = {
            address = 0xC0,
            size = 1,
            value = 0
        }
        p.registers["UCSR0B"] = {
            address = 0xC1,
            size = 1,
            value = 0
        }
        p.registers["UCSR0C"] = {
            address = 0xC2,
            size = 1,
            value = 0
        }
        p.registers["UBRR0"] = {
            address = 0xC4,
            size = 2,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x78,
            type = "ADC",
            description = "Analog-to-Digital Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADMUX"] = {
            address = 0x7C,
            size = 1,
            value = 0
        }
        p.registers["ADCSRA"] = {
            address = 0x7A,
            size = 1,
            value = 0
        }
        p.registers["ADCH"] = {
            address = 0x79,
            size = 1,
            value = 0
        }
        p.registers["ADCL"] = {
            address = 0x78,
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
            name = ATmega328P.DEVICE_NAME,
            manufacturer = ATmega328P.MANUFACTURER,
            family = ATmega328P.FAMILY,
            version = ATmega328P.VERSION,
            architecture = ATmega328P.ARCHITECTURE,
            bits = ATmega328P.BITS,
            clock_frequency = ATmega328P.CLOCK_FREQUENCY
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
        return string.format("ATmega328P(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ATmega328P.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ATmega328P.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ATmega328P.print_device_info(device)
    device = device or ATmega328P.new()
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

function ATmega328P.print_registers(device)
    device = device or ATmega328P.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ATmega328P.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ATmega328P.example()
    print("=== ATmega328P设备示例 ===")
    
    -- 创建设备实例
    local device = ATmega328P.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ATmega328P.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ATmega328P.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ATmega328P.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ATmega328P.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ATmega328P.lua$") then
    ATmega328P.example()
end

return ATmega328P
