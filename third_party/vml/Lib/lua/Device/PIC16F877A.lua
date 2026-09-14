--[[
  PIC16F877A设备定义 - Lua模块
  生成自: Microchip/PIC/PIC16F877A
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
  CPU架构: PIC16
  位宽: 8位
  时钟频率: 4000000 Hz
]]

local PIC16F877A = {}

-- 设备信息
PIC16F877A.DEVICE_NAME = "PIC16F877A"
PIC16F877A.MANUFACTURER = "Microchip"
PIC16F877A.FAMILY = "PIC"
PIC16F877A.VERSION = "1.0"
PIC16F877A.ARCHITECTURE = "PIC16"
PIC16F877A.BITS = 8
PIC16F877A.CLOCK_FREQUENCY = 4000000

-- 寄存器地址定义
PIC16F877A.W_ADDR = 0x00  -- Working Register
PIC16F877A.STATUS_ADDR = 0x03  -- Status Register
PIC16F877A.STATUS_C_BIT = 0  -- Carry flag
PIC16F877A.STATUS_DC_BIT = 1  -- Digit carry flag
PIC16F877A.STATUS_Z_BIT = 2  -- Zero flag
PIC16F877A.STATUS_PD_BIT = 3  -- Power-down flag
PIC16F877A.STATUS_TO_BIT = 4  -- Time-out flag
PIC16F877A.STATUS_RP_BIT = 5  -- Register bank select
PIC16F877A.STATUS_IRP_BIT = 7  -- Indirect register bank select
PIC16F877A.INTCON_ADDR = 0x0B  -- Interrupt Control Register
PIC16F877A.INTCON_RBIF_BIT = 0  -- PORTB change interrupt flag
PIC16F877A.INTCON_INTF_BIT = 1  -- External interrupt flag
PIC16F877A.INTCON_TMR0IF_BIT = 2  -- TMR0 overflow interrupt flag
PIC16F877A.INTCON_RBIE_BIT = 3  -- PORTB change interrupt enable
PIC16F877A.INTCON_INTE_BIT = 4  -- External interrupt enable
PIC16F877A.INTCON_TMR0IE_BIT = 5  -- TMR0 overflow interrupt enable
PIC16F877A.INTCON_PEIE_BIT = 6  -- Peripheral interrupt enable
PIC16F877A.INTCON_GIE_BIT = 7  -- Global interrupt enable
PIC16F877A.PORTB_ADDR = 0x06  -- PORT B
PIC16F877A.TRISB_ADDR = 0x86  -- TRIS B
PIC16F877A.PORTC_ADDR = 0x07  -- PORT C
PIC16F877A.TRISC_ADDR = 0x87  -- TRIS C
PIC16F877A.PORTD_ADDR = 0x08  -- PORT D
PIC16F877A.TRISD_ADDR = 0x88  -- TRIS D
PIC16F877A.PORTE_ADDR = 0x09  -- PORT E
PIC16F877A.TRISE_ADDR = 0x89  -- TRIS E
PIC16F877A.TMR0_ADDR = 0x01  -- Timer 0
PIC16F877A.OPTION_REG_ADDR = 0x81  -- Option Register
PIC16F877A.PCL_ADDR = 0x02  -- Program Counter Low
PIC16F877A.PCLATH_ADDR = 0x0A  -- Program Counter Latch High
PIC16F877A.FSR_ADDR = 0x04  -- File Select Register
PIC16F877A.EEDATA_ADDR = 0x10C  -- EEPROM Data
PIC16F877A.EEADR_ADDR = 0x10D  -- EEPROM Address
PIC16F877A.EECON1_ADDR = 0x18C  -- EEPROM Control 1
PIC16F877A.EECON1_RD_BIT = 0  -- Read control
PIC16F877A.EECON1_WR_BIT = 1  -- Write control
PIC16F877A.EECON1_WREN_BIT = 2  -- Write enable
PIC16F877A.EECON1_WRERR_BIT = 3  -- Write error flag
PIC16F877A.EECON1_EEPGD_BIT = 7  -- EEPROM program/data select
PIC16F877A.EECON2_ADDR = 0x18D  -- EEPROM Control 2
PIC16F877A.ADRESH_ADDR = 0x1E  -- A/D Result High
PIC16F877A.ADRESL_ADDR = 0x1F  -- A/D Result Low
PIC16F877A.ADCON0_ADDR = 0x1F  -- A/D Control 0
PIC16F877A.ADCON0_ADON_BIT = 0  -- A/D enable
PIC16F877A.ADCON0_GO_DONE_BIT = 2  -- A/D conversion status
PIC16F877A.ADCON0_CHS_BIT = 3  -- Channel select
PIC16F877A.ADCON1_ADDR = 0x9F  -- A/D Control 1
PIC16F877A.SSPSTAT_ADDR = 0x94  -- MSSP Status
PIC16F877A.SSPCON_ADDR = 0x14  -- MSSP Control
PIC16F877A.SSPBUF_ADDR = 0x13  -- SSP Buffer
PIC16F877A.TXREG_ADDR = 0x19  -- USART Transmit Register
PIC16F877A.RCREG_ADDR = 0x1A  -- USART Receive Register
PIC16F877A.SPBRG_ADDR = 0x99  -- Baud Rate Generator
PIC16F877A.TXSTA_ADDR = 0x98  -- TX Status and Control
PIC16F877A.RCSTA_ADDR = 0x18  -- RX Status and Control
PIC16F877A.CCP1CON_ADDR = 0x17  -- CCP1 Control
PIC16F877A.CCPR1L_ADDR = 0x15  -- CCP1 Low
PIC16F877A.CCPR1H_ADDR = 0x16  -- CCP1 High
PIC16F877A.CCP2CON_ADDR = 0x1D  -- CCP2 Control
PIC16F877A.CCPR2L_ADDR = 0x1B  -- CCP2 Low
PIC16F877A.CCPR2H_ADDR = 0x1C  -- CCP2 High
PIC16F877A.T1CON_ADDR = 0x10  -- Timer 1 Control
PIC16F877A.TMR1L_ADDR = 0x0E  -- Timer 1 Low
PIC16F877A.TMR1H_ADDR = 0x0F  -- Timer 1 High
PIC16F877A.T2CON_ADDR = 0x12  -- Timer 2 Control
PIC16F877A.TMR2_ADDR = 0x11  -- Timer 2
PIC16F877A.PR2_ADDR = 0x92  -- Timer 2 Period

-- 内存段定义
PIC16F877A.PROGRAM_START = 0x0000
PIC16F877A.PROGRAM_END = 0x1FFF
PIC16F877A.PROGRAM_SIZE = 8192  -- Program Memory (8KB)
PIC16F877A.DATA_START = 0x20
PIC16F877A.DATA_END = 0x7F
PIC16F877A.DATA_SIZE = 96  -- General Purpose RAM Bank 0
PIC16F877A.SRAM_START = 0xA0
PIC16F877A.SRAM_END = 0xFF
PIC16F877A.SRAM_SIZE = 96  -- General Purpose RAM Bank 1
PIC16F877A.EEPROM_START = 0x2100
PIC16F877A.EEPROM_END = 0x21FF
PIC16F877A.EEPROM_SIZE = 256  -- EEPROM Data Memory

-- 外设定义
-- Port B
PIC16F877A.GPIO_PORTB_BASE = 0x06
PIC16F877A.GPIO_PORTB_PORTB_ADDR = 0x06
PIC16F877A.GPIO_PORTB_TRISB_ADDR = 0x86
-- Port C
PIC16F877A.GPIO_PORTC_BASE = 0x07
PIC16F877A.GPIO_PORTC_PORTC_ADDR = 0x07
PIC16F877A.GPIO_PORTC_TRISC_ADDR = 0x87
-- Port D
PIC16F877A.GPIO_PORTD_BASE = 0x08
PIC16F877A.GPIO_PORTD_PORTD_ADDR = 0x08
PIC16F877A.GPIO_PORTD_TRISD_ADDR = 0x88
-- Timer 0
PIC16F877A.TIMER0_BASE = 0x01
PIC16F877A.TIMER0_TMR0_ADDR = 0x01
PIC16F877A.TIMER0_OPTION_REG_ADDR = 0x81
-- Timer 1
PIC16F877A.TIMER1_BASE = 0x0E
PIC16F877A.TIMER1_T1CON_ADDR = 0x10
PIC16F877A.TIMER1_TMR1L_ADDR = 0x0E
PIC16F877A.TIMER1_TMR1H_ADDR = 0x0F
-- Timer 2
PIC16F877A.TIMER2_BASE = 0x11
PIC16F877A.TIMER2_T2CON_ADDR = 0x12
PIC16F877A.TIMER2_TMR2_ADDR = 0x11
PIC16F877A.TIMER2_PR2_ADDR = 0x92
-- A/D Converter
PIC16F877A.ADC_BASE = 0x1E
PIC16F877A.ADC_ADRESH_ADDR = 0x1E
PIC16F877A.ADC_ADRESL_ADDR = 0x9F
PIC16F877A.ADC_ADCON0_ADDR = 0x1F
PIC16F877A.ADC_ADCON1_ADDR = 0x9F
-- Master Synchronous Serial Port
PIC16F877A.MSSP_BASE = 0x13
PIC16F877A.MSSP_SSPSTAT_ADDR = 0x94
PIC16F877A.MSSP_SSPCON_ADDR = 0x14
PIC16F877A.MSSP_SSPBUF_ADDR = 0x13
-- USART
PIC16F877A.USART_BASE = 0x19
PIC16F877A.USART_TXREG_ADDR = 0x19
PIC16F877A.USART_RCREG_ADDR = 0x1A
PIC16F877A.USART_SPBRG_ADDR = 0x99
PIC16F877A.USART_TXSTA_ADDR = 0x98
PIC16F877A.USART_RCSTA_ADDR = 0x18
-- Capture/Compare/PWM 1
PIC16F877A.CCP1_BASE = 0x15
PIC16F877A.CCP1_CCP1CON_ADDR = 0x17
PIC16F877A.CCP1_CCPR1L_ADDR = 0x15
PIC16F877A.CCP1_CCPR1H_ADDR = 0x16
-- Capture/Compare/PWM 2
PIC16F877A.CCP2_BASE = 0x1B
PIC16F877A.CCP2_CCP2CON_ADDR = 0x1D
PIC16F877A.CCP2_CCPR2L_ADDR = 0x1B
PIC16F877A.CCP2_CCPR2H_ADDR = 0x1C

-- 中断向量定义
PIC16F877A.INT_INT = 1  -- External Interrupt
PIC16F877A.INT_TMR0 = 2  -- Timer 0 Overflow
PIC16F877A.INT_RB = 3  -- PORTB Change
PIC16F877A.INT_CCP1 = 4  -- CCP1
PIC16F877A.INT_CCP2 = 5  -- CCP2
PIC16F877A.INT_TMR1 = 6  -- Timer 1 Overflow
PIC16F877A.INT_TMR2 = 8  -- Timer 2 Overflow
PIC16F877A.INT_SPI = 9  -- SPI/I2C
PIC16F877A.INT_SCI = 10  -- USART Receive
PIC16F877A.INT_SCI = 11  -- USART Transmit
PIC16F877A.INT_ADC = 12  -- A/D Converter
PIC16F877A.INT_EEPROM = 13  -- EEPROM Write Complete

-- 引脚定义
PIC16F877A.PIN_MCLR_VPP = 1  -- Master Clear (Reset)
PIC16F877A.PIN_RA0_AN0 = 2  -- PORTA Bit 0 / Analog 0
PIC16F877A.PIN_RA1_AN1 = 3  -- PORTA Bit 1 / Analog 1
PIC16F877A.PIN_RA2_AN2_VREF = 4  -- PORTA Bit 2 / Analog 2 / VREF-
PIC16F877A.PIN_RA3_AN3_VREFP = 5  -- PORTA Bit 3 / Analog 3 / VREF+
PIC16F877A.PIN_RA4_T0CKI = 6  -- PORTA Bit 4 / Timer 0 Clock Input
PIC16F877A.PIN_RA5_AN4_SS = 7  -- PORTA Bit 4 / Analog 4 / SPI Slave Select
PIC16F877A.PIN_RE0_RD_AN5 = 8  -- PORTE Bit 0 / Read Control / Analog 5
PIC16F877A.PIN_RE1_WR_AN6 = 9  -- PORTE Bit 1 / Write Control / Analog 6
PIC16F877A.PIN_RE2_CS_AN7 = 10  -- PORTE Bit 2 / Chip Select / Analog 7
PIC16F877A.PIN_VDD = 11  -- Positive Supply
PIC16F877A.PIN_VSS = 12  -- Ground
PIC16F877A.PIN_OSC1_CLKIN = 13  -- Oscillator/Clock Input
PIC16F877A.PIN_OSC2_CLKOUT = 14  -- Oscillator/Clock Output
PIC16F877A.PIN_RC0_T1OSO = 15  -- PORTC Bit 0 / Timer 1 Oscillator
PIC16F877A.PIN_RC1_T1OSI = 16  -- PORTC Bit 1 / Timer 1 Oscillator
PIC16F877A.PIN_RC2_CCP1 = 17  -- PORTC Bit 2 / Capture/Compare/PWM 1
PIC16F877A.PIN_RC3_SCK_SCL = 18  -- PORTC Bit 3 / SPI Clock / I2C Clock
PIC16F877A.PIN_RC4_SDI_SDA = 23  -- PORTC Bit 4 / SPI Data In / I2C Data
PIC16F877A.PIN_RC5_SDO = 24  -- PORTC Bit 5 / SPI Data Out
PIC16F877A.PIN_RC6_TX = 25  -- PORTC Bit 6 / USART Transmit
PIC16F877A.PIN_RC7_RX = 26  -- PORTC Bit 7 / USART Receive
PIC16F877A.PIN_RD0 = 19  -- PORTD Bit 0
PIC16F877A.PIN_RD1 = 20  -- PORTD Bit 1
PIC16F877A.PIN_RD2 = 21  -- PORTD Bit 2
PIC16F877A.PIN_RD3 = 22  -- PORTD Bit 3
PIC16F877A.PIN_RD4 = 27  -- PORTD Bit 4
PIC16F877A.PIN_RD5 = 28  -- PORTD Bit 5
PIC16F877A.PIN_RD6 = 29  -- PORTD Bit 6
PIC16F877A.PIN_RD7 = 30  -- PORTD Bit 7
PIC16F877A.PIN_VSS = 31  -- Ground
PIC16F877A.PIN_VDD = 32  -- Positive Supply
PIC16F877A.PIN_RB0_INT = 33  -- PORTB Bit 0 / External Interrupt
PIC16F877A.PIN_RB1 = 34  -- PORTB Bit 1
PIC16F877A.PIN_RB2 = 35  -- PORTB Bit 2
PIC16F877A.PIN_RB3_PGC = 36  -- PORTB Bit 3 / Programming Clock
PIC16F877A.PIN_RB4_PGD = 37  -- PORTB Bit 4 / Programming Data
PIC16F877A.PIN_RB5 = 38  -- PORTB Bit 5
PIC16F877A.PIN_RB6_PGC = 39  -- PORTB Bit 6 / Programming Clock
PIC16F877A.PIN_RB7_PGD = 40  -- PORTB Bit 7 / Programming Data

-- 设备类
function PIC16F877A.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["W"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "Working Register",
            value = 0
        }
        self.registers["STATUS"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
        self.registers["INTCON"] = {
            address = 0x0B,
            size = 1,
            access = "rw",
            description = "Interrupt Control Register",
            value = 0
        }
        self.registers["PORTB"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "PORT B",
            value = 0
        }
        self.registers["TRISB"] = {
            address = 0x86,
            size = 1,
            access = "rw",
            description = "TRIS B",
            value = 0
        }
        self.registers["PORTC"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "PORT C",
            value = 0
        }
        self.registers["TRISC"] = {
            address = 0x87,
            size = 1,
            access = "rw",
            description = "TRIS C",
            value = 0
        }
        self.registers["PORTD"] = {
            address = 0x08,
            size = 1,
            access = "rw",
            description = "PORT D",
            value = 0
        }
        self.registers["TRISD"] = {
            address = 0x88,
            size = 1,
            access = "rw",
            description = "TRIS D",
            value = 0
        }
        self.registers["PORTE"] = {
            address = 0x09,
            size = 1,
            access = "rw",
            description = "PORT E",
            value = 0
        }
        self.registers["TRISE"] = {
            address = 0x89,
            size = 1,
            access = "rw",
            description = "TRIS E",
            value = 0
        }
        self.registers["TMR0"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "Timer 0",
            value = 0
        }
        self.registers["OPTION_REG"] = {
            address = 0x81,
            size = 1,
            access = "rw",
            description = "Option Register",
            value = 0
        }
        self.registers["PCL"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "Program Counter Low",
            value = 0
        }
        self.registers["PCLATH"] = {
            address = 0x0A,
            size = 1,
            access = "rw",
            description = "Program Counter Latch High",
            value = 0
        }
        self.registers["FSR"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "File Select Register",
            value = 0
        }
        self.registers["EEDATA"] = {
            address = 0x10C,
            size = 1,
            access = "rw",
            description = "EEPROM Data",
            value = 0
        }
        self.registers["EEADR"] = {
            address = 0x10D,
            size = 1,
            access = "rw",
            description = "EEPROM Address",
            value = 0
        }
        self.registers["EECON1"] = {
            address = 0x18C,
            size = 1,
            access = "rw",
            description = "EEPROM Control 1",
            value = 0
        }
        self.registers["EECON2"] = {
            address = 0x18D,
            size = 1,
            access = "rw",
            description = "EEPROM Control 2",
            value = 0
        }
        self.registers["ADRESH"] = {
            address = 0x1E,
            size = 1,
            access = "rw",
            description = "A/D Result High",
            value = 0
        }
        self.registers["ADRESL"] = {
            address = 0x1F,
            size = 1,
            access = "rw",
            description = "A/D Result Low",
            value = 0
        }
        self.registers["ADCON0"] = {
            address = 0x1F,
            size = 1,
            access = "rw",
            description = "A/D Control 0",
            value = 0
        }
        self.registers["ADCON1"] = {
            address = 0x9F,
            size = 1,
            access = "rw",
            description = "A/D Control 1",
            value = 0
        }
        self.registers["SSPSTAT"] = {
            address = 0x94,
            size = 1,
            access = "rw",
            description = "MSSP Status",
            value = 0
        }
        self.registers["SSPCON"] = {
            address = 0x14,
            size = 1,
            access = "rw",
            description = "MSSP Control",
            value = 0
        }
        self.registers["SSPBUF"] = {
            address = 0x13,
            size = 1,
            access = "rw",
            description = "SSP Buffer",
            value = 0
        }
        self.registers["TXREG"] = {
            address = 0x19,
            size = 1,
            access = "rw",
            description = "USART Transmit Register",
            value = 0
        }
        self.registers["RCREG"] = {
            address = 0x1A,
            size = 1,
            access = "rw",
            description = "USART Receive Register",
            value = 0
        }
        self.registers["SPBRG"] = {
            address = 0x99,
            size = 1,
            access = "rw",
            description = "Baud Rate Generator",
            value = 0
        }
        self.registers["TXSTA"] = {
            address = 0x98,
            size = 1,
            access = "rw",
            description = "TX Status and Control",
            value = 0
        }
        self.registers["RCSTA"] = {
            address = 0x18,
            size = 1,
            access = "rw",
            description = "RX Status and Control",
            value = 0
        }
        self.registers["CCP1CON"] = {
            address = 0x17,
            size = 1,
            access = "rw",
            description = "CCP1 Control",
            value = 0
        }
        self.registers["CCPR1L"] = {
            address = 0x15,
            size = 1,
            access = "rw",
            description = "CCP1 Low",
            value = 0
        }
        self.registers["CCPR1H"] = {
            address = 0x16,
            size = 1,
            access = "rw",
            description = "CCP1 High",
            value = 0
        }
        self.registers["CCP2CON"] = {
            address = 0x1D,
            size = 1,
            access = "rw",
            description = "CCP2 Control",
            value = 0
        }
        self.registers["CCPR2L"] = {
            address = 0x1B,
            size = 1,
            access = "rw",
            description = "CCP2 Low",
            value = 0
        }
        self.registers["CCPR2H"] = {
            address = 0x1C,
            size = 1,
            access = "rw",
            description = "CCP2 High",
            value = 0
        }
        self.registers["T1CON"] = {
            address = 0x10,
            size = 1,
            access = "rw",
            description = "Timer 1 Control",
            value = 0
        }
        self.registers["TMR1L"] = {
            address = 0x0E,
            size = 1,
            access = "rw",
            description = "Timer 1 Low",
            value = 0
        }
        self.registers["TMR1H"] = {
            address = 0x0F,
            size = 1,
            access = "rw",
            description = "Timer 1 High",
            value = 0
        }
        self.registers["T2CON"] = {
            address = 0x12,
            size = 1,
            access = "rw",
            description = "Timer 2 Control",
            value = 0
        }
        self.registers["TMR2"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "Timer 2",
            value = 0
        }
        self.registers["PR2"] = {
            address = 0x92,
            size = 1,
            access = "rw",
            description = "Timer 2 Period",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPIO_PORTB"] = {
            base = 0x06,
            type = "gpio",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_PORTB"]
        p.registers["PORTB"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["TRISB"] = {
            address = 0x86,
            size = 1,
            value = 0
        }
        self.peripherals["GPIO_PORTC"] = {
            base = 0x07,
            type = "gpio",
            description = "Port C",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_PORTC"]
        p.registers["PORTC"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["TRISC"] = {
            address = 0x87,
            size = 1,
            value = 0
        }
        self.peripherals["GPIO_PORTD"] = {
            base = 0x08,
            type = "gpio",
            description = "Port D",
            registers = {}
        }
        
        local p = self.peripherals["GPIO_PORTD"]
        p.registers["PORTD"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["TRISD"] = {
            address = 0x88,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x01,
            type = "timer",
            description = "Timer 0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["TMR0"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["OPTION_REG"] = {
            address = 0x81,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER1"] = {
            base = 0x0E,
            type = "timer",
            description = "Timer 1",
            registers = {}
        }
        
        local p = self.peripherals["TIMER1"]
        p.registers["T1CON"] = {
            address = 0x10,
            size = 1,
            value = 0
        }
        p.registers["TMR1L"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["TMR1H"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER2"] = {
            base = 0x11,
            type = "timer",
            description = "Timer 2",
            registers = {}
        }
        
        local p = self.peripherals["TIMER2"]
        p.registers["T2CON"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["TMR2"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["PR2"] = {
            address = 0x92,
            size = 1,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x1E,
            type = "adc",
            description = "A/D Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADRESH"] = {
            address = 0x1E,
            size = 1,
            value = 0
        }
        p.registers["ADRESL"] = {
            address = 0x9F,
            size = 1,
            value = 0
        }
        p.registers["ADCON0"] = {
            address = 0x1F,
            size = 1,
            value = 0
        }
        p.registers["ADCON1"] = {
            address = 0x9F,
            size = 1,
            value = 0
        }
        self.peripherals["MSSP"] = {
            base = 0x13,
            type = "spi_i2c",
            description = "Master Synchronous Serial Port",
            registers = {}
        }
        
        local p = self.peripherals["MSSP"]
        p.registers["SSPSTAT"] = {
            address = 0x94,
            size = 1,
            value = 0
        }
        p.registers["SSPCON"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["SSPBUF"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        self.peripherals["USART"] = {
            base = 0x19,
            type = "uart",
            description = "USART",
            registers = {}
        }
        
        local p = self.peripherals["USART"]
        p.registers["TXREG"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["RCREG"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["SPBRG"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["TXSTA"] = {
            address = 0x98,
            size = 1,
            value = 0
        }
        p.registers["RCSTA"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        self.peripherals["CCP1"] = {
            base = 0x15,
            type = "pwm",
            description = "Capture/Compare/PWM 1",
            registers = {}
        }
        
        local p = self.peripherals["CCP1"]
        p.registers["CCP1CON"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["CCPR1L"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["CCPR1H"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        self.peripherals["CCP2"] = {
            base = 0x1B,
            type = "pwm",
            description = "Capture/Compare/PWM 2",
            registers = {}
        }
        
        local p = self.peripherals["CCP2"]
        p.registers["CCP2CON"] = {
            address = 0x1D,
            size = 1,
            value = 0
        }
        p.registers["CCPR2L"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        p.registers["CCPR2H"] = {
            address = 0x1C,
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
            name = PIC16F877A.DEVICE_NAME,
            manufacturer = PIC16F877A.MANUFACTURER,
            family = PIC16F877A.FAMILY,
            version = PIC16F877A.VERSION,
            architecture = PIC16F877A.ARCHITECTURE,
            bits = PIC16F877A.BITS,
            clock_frequency = PIC16F877A.CLOCK_FREQUENCY
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
        return string.format("PIC16F877A(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PIC16F877A.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PIC16F877A.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PIC16F877A.print_device_info(device)
    device = device or PIC16F877A.new()
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

function PIC16F877A.print_registers(device)
    device = device or PIC16F877A.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PIC16F877A.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PIC16F877A.example()
    print("=== PIC16F877A设备示例 ===")
    
    -- 创建设备实例
    local device = PIC16F877A.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PIC16F877A.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["W"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("W", 0x55)
        print("写入 W: " .. PIC16F877A.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("W")
        print("读取 W: " .. PIC16F877A.hex(value))
        
        -- 位操作
        device:set_bit("W", 0, true)
        local bit0 = device:get_bit("W", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    PIC16F877A.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PIC16F877A.lua$") then
    PIC16F877A.example()
end

return PIC16F877A
