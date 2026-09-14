--[[
  PIC18F4550设备定义 - Lua模块
  生成自: Microchip/PIC18/PIC18F4550
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
  CPU架构: PIC18
  位宽: 8位
  时钟频率: 20000000 Hz
]]

local PIC18F4550 = {}

-- 设备信息
PIC18F4550.DEVICE_NAME = "PIC18F4550"
PIC18F4550.MANUFACTURER = "Microchip"
PIC18F4550.FAMILY = "PIC18"
PIC18F4550.VERSION = "1.0"
PIC18F4550.ARCHITECTURE = "PIC18"
PIC18F4550.BITS = 8
PIC18F4550.CLOCK_FREQUENCY = 20000000

-- 寄存器地址定义
PIC18F4550.W_ADDR = 0x0E  -- Working Register
PIC18F4550.STATUS_ADDR = 0xFD8  -- Status Register
PIC18F4550.STATUS_C_BIT = 0  -- Carry Flag
PIC18F4550.STATUS_DC_BIT = 1  -- Digit Carry Flag
PIC18F4550.STATUS_Z_BIT = 2  -- Zero Flag
PIC18F4550.STATUS_PD_BIT = 3  -- Power-Down Flag
PIC18F4550.STATUS_TO_BIT = 4  -- Time-out Flag
PIC18F4550.STATUS_RP_BIT = 0  -- Register Bank Select
PIC18F4550.STATUS_IRP_BIT = 7  -- Indirect Register Bank Select
PIC18F4550.BSR_ADDR = 0xFE0  -- Bank Select Register
PIC18F4550.PORTA_ADDR = 0xF80  -- Port A
PIC18F4550.PORTB_ADDR = 0xF81  -- Port B
PIC18F4550.PORTC_ADDR = 0xF82  -- Port C
PIC18F4550.PORTD_ADDR = 0xF83  -- Port D
PIC18F4550.PORTE_ADDR = 0xF84  -- Port E
PIC18F4550.TRISA_ADDR = 0xF92  -- Tri-state Port A
PIC18F4550.TRISB_ADDR = 0xF93  -- Tri-state Port B
PIC18F4550.TRISC_ADDR = 0xF94  -- Tri-state Port C
PIC18F4550.TRISD_ADDR = 0xF95  -- Tri-state Port D
PIC18F4550.TRISE_ADDR = 0xF96  -- Tri-state Port E
PIC18F4550.LATA_ADDR = 0xF89  -- Latch Port A
PIC18F4550.LATB_ADDR = 0xF8A  -- Latch Port B
PIC18F4550.LATC_ADDR = 0xF8B  -- Latch Port C
PIC18F4550.LATD_ADDR = 0xF8C  -- Latch Port D
PIC18F4550.LATE_ADDR = 0xF8D  -- Latch Port E
PIC18F4550.INTCON_ADDR = 0xFF2  -- Interrupt Control
PIC18F4550.INTCON_RBIF_BIT = 0  -- Port B Interrupt Flag
PIC18F4550.INTCON_INT0IF_BIT = 1  -- INT0 Interrupt Flag
PIC18F4550.INTCON_TMR0IF_BIT = 2  -- Timer 0 Interrupt Flag
PIC18F4550.INTCON_RBIE_BIT = 3  -- Port B Interrupt Enable
PIC18F4550.INTCON_INT0IE_BIT = 4  -- INT0 Interrupt Enable
PIC18F4550.INTCON_TMR0IE_BIT = 5  -- Timer 0 Interrupt Enable
PIC18F4550.INTCON_PEIE_BIT = 6  -- Peripheral Interrupt Enable
PIC18F4550.INTCON_GIE_BIT = 7  -- Global Interrupt Enable
PIC18F4550.PIR1_ADDR = 0xF9E  -- Peripheral Interrupt 1
PIC18F4550.PIR2_ADDR = 0xF9F  -- Peripheral Interrupt 2
PIC18F4550.PIE1_ADDR = 0xF9D  -- Peripheral Interrupt Enable 1
PIC18F4550.PIE2_ADDR = 0xF9C  -- Peripheral Interrupt Enable 2
PIC18F4550.IPR1_ADDR = 0xF9B  -- Interrupt Priority 1
PIC18F4550.IPR2_ADDR = 0xF9A  -- Interrupt Priority 2
PIC18F4550.RCON_ADDR = 0xFD0  -- Reset Control
PIC18F4550.RCON_NOT_TO_BIT = 3  -- Time-out Flag
PIC18F4550.RCON_NOT_PD_BIT = 4  -- Power-Down Flag
PIC18F4550.RCON_NOT_RI_BIT = 5  -- RESET Flag
PIC18F4550.RCON_NOT_POR_BIT = 6  -- Power-on Reset Flag
PIC18F4550.RCON_NOT_BOR_BIT = 7  -- Brown-out Reset Flag
PIC18F4550.T0CON_ADDR = 0xFD1  -- Timer 0 Control
PIC18F4550.TMR0_ADDR = 0xFD6  -- Timer 0 Register
PIC18F4550.T1CON_ADDR = 0xFCD  -- Timer 1 Control
PIC18F4550.TMR1_ADDR = 0xFCE  -- Timer 1 Register High
PIC18F4550.TMR1L_ADDR = 0xFCF  -- Timer 1 Register Low
PIC18F4550.T2CON_ADDR = 0xFCA  -- Timer 2 Control
PIC18F4550.TMR2_ADDR = 0xFCB  -- Timer 2 Register
PIC18F4550.T3CON_ADDR = 0xFB1  -- Timer 3 Control
PIC18F4550.TMR3_ADDR = 0xFB3  -- Timer 3 Register High
PIC18F4550.TMR3L_ADDR = 0xFB2  -- Timer 3 Register Low
PIC18F4550.SSPCON1_ADDR = 0xFC6  -- SSP Control 1
PIC18F4550.SSPCON2_ADDR = 0xFC5  -- SSP Control 2
PIC18F4550.SSPSTAT_ADDR = 0xFC7  -- SSP Status
PIC18F4550.SSPBUF_ADDR = 0xFC9  -- SSP Buffer
PIC18F4550.SSPOR_ADDR = 0xFC8  -- SSP Shift Register
PIC18F4550.ADCON0_ADDR = 0xFC2  -- A/D Control 0
PIC18F4550.ADCON1_ADDR = 0xFC1  -- A/D Control 1
PIC18F4550.ADCON2_ADDR = 0xFC0  -- A/D Control 2
PIC18F4550.ADRES_ADDR = 0xFC3  -- A/D Result
PIC18F4550.ADRESL_ADDR = 0xFC4  -- A/D Result Low
PIC18F4550.CCP1CON_ADDR = 0xFD4  -- CCP 1 Control
PIC18F4550.CCPR1_ADDR = 0xFD6  -- CCP 1 Register High
PIC18F4550.CCPR1L_ADDR = 0xFD5  -- CCP 1 Register Low
PIC18F4550.CCP2CON_ADDR = 0xFBA  -- CCP 2 Control
PIC18F4550.CCPR2_ADDR = 0xFBB  -- CCP 2 Register High
PIC18F4550.CCPR2L_ADDR = 0xFBC  -- CCP 2 Register Low
PIC18F4550.USBCON_ADDR = 0xF75  -- USB Control
PIC18F4550.USBSTAT_ADDR = 0xF74  -- USB Status
PIC18F4550.UIE_ADDR = 0xF73  -- USB Interrupt Enable
PIC18F4550.UIR_ADDR = 0xF72  -- USB Interrupt Flag
PIC18F4550.UCON_ADDR = 0xF71  -- USB Control
PIC18F4550.USTAT_ADDR = 0xF70  -- USB Status
PIC18F4550.UEP0_ADDR = 0xF60  -- USB Endpoint 0
PIC18F4550.UEP1_ADDR = 0xF61  -- USB Endpoint 1
PIC18F4550.UEP2_ADDR = 0xF62  -- USB Endpoint 2
PIC18F4550.UEP3_ADDR = 0xF63  -- USB Endpoint 3
PIC18F4550.UEP4_ADDR = 0xF64  -- USB Endpoint 4

-- 内存段定义
PIC18F4550.FLASH_START = 0x0000
PIC18F4550.FLASH_END = 0x7FFF
PIC18F4550.FLASH_SIZE = 32768  -- Program Flash (32KB)
PIC18F4550.EEPROM_START = 0xF00000
PIC18F4550.EEPROM_END = 0xF000FF
PIC18F4550.EEPROM_SIZE = 256  -- EEPROM (256B)
PIC18F4550.SRAM_START = 0x0000
PIC18F4550.SRAM_END = 0x07FF
PIC18F4550.SRAM_SIZE = 2048  -- SRAM (2KB)
PIC18F4550.ACCESS_START = 0x0000
PIC18F4550.ACCESS_END = 
PIC18F4550.ACCESS_SIZE = 1  -- Access Bank

-- 外设定义
-- Port A
PIC18F4550.PORTA_BASE = 0xF80
PIC18F4550.PORTA_PORT_ADDR = 0xF80
PIC18F4550.PORTA_TRIS_ADDR = 0xF92
PIC18F4550.PORTA_LAT_ADDR = 0xF89
-- Port B
PIC18F4550.PORTB_BASE = 0xF81
PIC18F4550.PORTB_PORT_ADDR = 0xF81
PIC18F4550.PORTB_TRIS_ADDR = 0xF93
PIC18F4550.PORTB_LAT_ADDR = 0xF8A
-- Port C
PIC18F4550.PORTC_BASE = 0xF82
PIC18F4550.PORTC_PORT_ADDR = 0xF82
PIC18F4550.PORTC_TRIS_ADDR = 0xF94
PIC18F4550.PORTC_LAT_ADDR = 0xF8B
-- Port D
PIC18F4550.PORTD_BASE = 0xF83
PIC18F4550.PORTD_PORT_ADDR = 0xF83
PIC18F4550.PORTD_TRIS_ADDR = 0xF95
PIC18F4550.PORTD_LAT_ADDR = 0xF8C
-- Port E
PIC18F4550.PORTE_BASE = 0xF84
PIC18F4550.PORTE_PORT_ADDR = 0xF84
PIC18F4550.PORTE_TRIS_ADDR = 0xF96
PIC18F4550.PORTE_LAT_ADDR = 0xF8D
-- Timer 0
PIC18F4550.TIMER0_BASE = 0xFD1
PIC18F4550.TIMER0_T0CON_ADDR = 0xFD1
PIC18F4550.TIMER0_TMR0_ADDR = 0xFD6
-- Timer 1
PIC18F4550.TIMER1_BASE = 0xFCD
PIC18F4550.TIMER1_T1CON_ADDR = 0xFCD
PIC18F4550.TIMER1_TMR1_ADDR = 0xFCF
PIC18F4550.TIMER1_TMR1L_ADDR = 0xFCE
-- Timer 2
PIC18F4550.TIMER2_BASE = 0xFCA
PIC18F4550.TIMER2_T2CON_ADDR = 0xFCA
PIC18F4550.TIMER2_TMR2_ADDR = 0xFCB
-- Timer 3
PIC18F4550.TIMER3_BASE = 0xFB0
PIC18F4550.TIMER3_T3CON_ADDR = 0xFB0
PIC18F4550.TIMER3_TMR3_ADDR = 0xFB2
-- A/D Converter
PIC18F4550.ADC_BASE = 0xFC2
PIC18F4550.ADC_ADCON0_ADDR = 0xFC2
PIC18F4550.ADC_ADCON1_ADDR = 0xFC1
PIC18F4550.ADC_ADCON2_ADDR = 0xFC0
PIC18F4550.ADC_ADRES_ADDR = 0xFC3
PIC18F4550.ADC_ADRESL_ADDR = 0xFC4
-- CCP 1
PIC18F4550.CCP1_BASE = 0xFD4
PIC18F4550.CCP1_CCP1CON_ADDR = 0xFD4
PIC18F4550.CCP1_CCPR1_ADDR = 0xFD6
PIC18F4550.CCP1_CCPR1L_ADDR = 0xFD5
-- CCP 2
PIC18F4550.CCP2_BASE = 0xFBA
PIC18F4550.CCP2_CCP2CON_ADDR = 0xFBA
PIC18F4550.CCP2_CCPR2_ADDR = 0xFBB
PIC18F4550.CCP2_CCPR2L_ADDR = 0xFBC
-- SSP (I2C/SPI)
PIC18F4550.SSP_BASE = 0xFC6
PIC18F4550.SSP_SSPCON1_ADDR = 0xFC6
PIC18F4550.SSP_SSPCON2_ADDR = 0xFC5
PIC18F4550.SSP_SSPSTAT_ADDR = 0xFC7
PIC18F4550.SSP_SSPBUF_ADDR = 0xFC9
PIC18F4550.SSP_SSPOV_ADDR = 0xFC8
-- EUSART
PIC18F4550.EUSART_BASE = 0xF15
PIC18F4550.EUSART_TXSTA_ADDR = 0xFE2
PIC18F4550.EUSART_RCSTA_ADDR = 0xFE3
PIC18F4550.EUSART_TXREG_ADDR = 0xFAD
PIC18F4550.EUSART_RCREG_ADDR = 0xFAE
PIC18F4550.EUSART_SPBRG_ADDR = 0xFAF
PIC18F4550.EUSART_SPBRGH_ADDR = 0xFB0
PIC18F4550.EUSART_BAUDCON_ADDR = 0xFB8
-- Comparators
PIC18F4550.COMPARATOR_BASE = 0xFB4
PIC18F4550.COMPARATOR_CMCON_ADDR = 0xFB4
PIC18F4550.COMPARATOR_CVRCON_ADDR = 0xFB5
-- USB Module
PIC18F4550.USB_BASE = 0xF70
PIC18F4550.USB_UCON_ADDR = 0xF71
PIC18F4550.USB_USTAT_ADDR = 0xF72
PIC18F4550.USB_UIR_ADDR = 0xF73
PIC18F4550.USB_UIE_ADDR = 0xF74
PIC18F4550.USB_UEP0_ADDR = 0xF80
PIC18F4550.USB_UEP1_ADDR = 0xF81
PIC18F4550.USB_UEP2_ADDR = 0xF82
PIC18F4550.USB_UEP3_ADDR = 0xF83
PIC18F4550.USB_BD0_ADDR = 0xF00
PIC18F4550.USB_BD1_ADDR = 0xF08
PIC18F4550.USB_BD2_ADDR = 0xF10
PIC18F4550.USB_BD3_ADDR = 0xF18
-- Oscillator
PIC18F4550.OSCCON_BASE = 0xFD3
PIC18F4550.OSCCON_OSCCON_ADDR = 0xFD3
PIC18F4550.OSCCON_OSCTUNE_ADDR = 0xFD9
-- Watchdog Timer
PIC18F4550.WDTCON_BASE = 0xFD1
PIC18F4550.WDTCON_WDTCON_ADDR = 0xFD1

-- 中断向量定义
PIC18F4550.INT_RESET = 0  -- RESET
PIC18F4550.INT_INT0 = 1  -- External Interrupt 0
PIC18F4550.INT_INT1 = 2  -- External Interrupt 1
PIC18F4550.INT_INT2 = 3  -- External Interrupt 2
PIC18F4550.INT_TMR0 = 4  -- Timer 0 Overflow
PIC18F4550.INT_TMR1 = 5  -- Timer 1 Overflow
PIC18F4550.INT_TMR2 = 6  -- Timer 2 Match
PIC18F4550.INT_TMR3 = 7  -- Timer 3 Overflow
PIC18F4550.INT_CCP1 = 8  -- CCP 1
PIC18F4550.INT_CCP2 = 9  -- CCP 2
PIC18F4550.INT_SSP = 10  -- SSP
PIC18F4550.INT_TX = 11  -- USART TX
PIC18F4550.INT_RC = 12  -- USART RX
PIC18F4550.INT_ADC = 13  -- A/D
PIC18F4550.INT_RBO = 14  -- Port B Change
PIC18F4550.INT_EXT = 15  -- External

-- 引脚定义
PIC18F4550.PIN_RE3 = 1  -- MCLR/VPP/RE3
PIC18F4550.PIN_RA0 = 2  -- AN0/RA0
PIC18F4550.PIN_RA1 = 3  -- AN1/RA1
PIC18F4550.PIN_RA2 = 4  -- AN2/VREF-/RA2
PIC18F4550.PIN_RA3 = 5  -- AN3/VREF+/RA3
PIC18F4550.PIN_RA4 = 6  -- AN4/T0CKI/RA4
PIC18F4550.PIN_RA5 = 7  -- AN5/RE5
PIC18F4550.PIN_VSS = 8  -- Ground
PIC18F4550.PIN_RA7 = 9  -- OSC1/CLKI/RA7
PIC18F4550.PIN_RA6 = 10  -- OSC2/CLKO/RA6
PIC18F4550.PIN_RC0 = 11  -- T1OSO/T1CKI/RC0
PIC18F4550.PIN_RC1 = 12  -- T1OSI/RC1
PIC18F4550.PIN_RC2 = 13  -- CCP1/RC2
PIC18F4550.PIN_RC3 = 14  -- SCK/SCL/RC3
PIC18F4550.PIN_RD0 = 15  -- SDO/RD0
PIC18F4550.PIN_RD1 = 16  -- SDI/RD1
PIC18F4550.PIN_RD2 = 17  -- RD2
PIC18F4550.PIN_RC6 = 18  -- TX/CK/RC6
PIC18F4550.PIN_RC7 = 19  -- RX/DT/RC7
PIC18F4550.PIN_VSS = 20  -- Ground
PIC18F4550.PIN_RD3 = 21  -- RD3
PIC18F4550.PIN_RD4 = 22  -- RD4
PIC18F4550.PIN_RD5 = 23  -- PWRB/RD5
PIC18F4550.PIN_RD6 = 24  -- PBC/RD6
PIC18F4550.PIN_RD7 = 25  -- PCD/RD7
PIC18F4550.PIN_RC4 = 26  -- D-/RC4
PIC18F4550.PIN_RC5 = 27  -- D+/RC5
PIC18F4550.PIN_RE0 = 28  -- AN5/RE0
PIC18F4550.PIN_RE1 = 29  -- AN6/RE1
PIC18F4550.PIN_RE2 = 30  -- AN7/RE2
PIC18F4550.PIN_VSS = 31  -- Ground
PIC18F4550.PIN_VDD = 32  -- Vdd

-- 设备类
function PIC18F4550.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["W"] = {
            address = 0x0E,
            size = 1,
            access = "rw",
            description = "Working Register",
            value = 0
        }
        self.registers["STATUS"] = {
            address = 0xFD8,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
        self.registers["BSR"] = {
            address = 0xFE0,
            size = 1,
            access = "rw",
            description = "Bank Select Register",
            value = 0
        }
        self.registers["PORTA"] = {
            address = 0xF80,
            size = 1,
            access = "rw",
            description = "Port A",
            value = 0
        }
        self.registers["PORTB"] = {
            address = 0xF81,
            size = 1,
            access = "rw",
            description = "Port B",
            value = 0
        }
        self.registers["PORTC"] = {
            address = 0xF82,
            size = 1,
            access = "rw",
            description = "Port C",
            value = 0
        }
        self.registers["PORTD"] = {
            address = 0xF83,
            size = 1,
            access = "rw",
            description = "Port D",
            value = 0
        }
        self.registers["PORTE"] = {
            address = 0xF84,
            size = 1,
            access = "rw",
            description = "Port E",
            value = 0
        }
        self.registers["TRISA"] = {
            address = 0xF92,
            size = 1,
            access = "rw",
            description = "Tri-state Port A",
            value = 0
        }
        self.registers["TRISB"] = {
            address = 0xF93,
            size = 1,
            access = "rw",
            description = "Tri-state Port B",
            value = 0
        }
        self.registers["TRISC"] = {
            address = 0xF94,
            size = 1,
            access = "rw",
            description = "Tri-state Port C",
            value = 0
        }
        self.registers["TRISD"] = {
            address = 0xF95,
            size = 1,
            access = "rw",
            description = "Tri-state Port D",
            value = 0
        }
        self.registers["TRISE"] = {
            address = 0xF96,
            size = 1,
            access = "rw",
            description = "Tri-state Port E",
            value = 0
        }
        self.registers["LATA"] = {
            address = 0xF89,
            size = 1,
            access = "rw",
            description = "Latch Port A",
            value = 0
        }
        self.registers["LATB"] = {
            address = 0xF8A,
            size = 1,
            access = "rw",
            description = "Latch Port B",
            value = 0
        }
        self.registers["LATC"] = {
            address = 0xF8B,
            size = 1,
            access = "rw",
            description = "Latch Port C",
            value = 0
        }
        self.registers["LATD"] = {
            address = 0xF8C,
            size = 1,
            access = "rw",
            description = "Latch Port D",
            value = 0
        }
        self.registers["LATE"] = {
            address = 0xF8D,
            size = 1,
            access = "rw",
            description = "Latch Port E",
            value = 0
        }
        self.registers["INTCON"] = {
            address = 0xFF2,
            size = 1,
            access = "rw",
            description = "Interrupt Control",
            value = 0
        }
        self.registers["PIR1"] = {
            address = 0xF9E,
            size = 1,
            access = "rw",
            description = "Peripheral Interrupt 1",
            value = 0
        }
        self.registers["PIR2"] = {
            address = 0xF9F,
            size = 1,
            access = "rw",
            description = "Peripheral Interrupt 2",
            value = 0
        }
        self.registers["PIE1"] = {
            address = 0xF9D,
            size = 1,
            access = "rw",
            description = "Peripheral Interrupt Enable 1",
            value = 0
        }
        self.registers["PIE2"] = {
            address = 0xF9C,
            size = 1,
            access = "rw",
            description = "Peripheral Interrupt Enable 2",
            value = 0
        }
        self.registers["IPR1"] = {
            address = 0xF9B,
            size = 1,
            access = "rw",
            description = "Interrupt Priority 1",
            value = 0
        }
        self.registers["IPR2"] = {
            address = 0xF9A,
            size = 1,
            access = "rw",
            description = "Interrupt Priority 2",
            value = 0
        }
        self.registers["RCON"] = {
            address = 0xFD0,
            size = 1,
            access = "rw",
            description = "Reset Control",
            value = 0
        }
        self.registers["T0CON"] = {
            address = 0xFD1,
            size = 1,
            access = "rw",
            description = "Timer 0 Control",
            value = 0
        }
        self.registers["TMR0"] = {
            address = 0xFD6,
            size = 1,
            access = "rw",
            description = "Timer 0 Register",
            value = 0
        }
        self.registers["T1CON"] = {
            address = 0xFCD,
            size = 1,
            access = "rw",
            description = "Timer 1 Control",
            value = 0
        }
        self.registers["TMR1"] = {
            address = 0xFCE,
            size = 1,
            access = "rw",
            description = "Timer 1 Register High",
            value = 0
        }
        self.registers["TMR1L"] = {
            address = 0xFCF,
            size = 1,
            access = "rw",
            description = "Timer 1 Register Low",
            value = 0
        }
        self.registers["T2CON"] = {
            address = 0xFCA,
            size = 1,
            access = "rw",
            description = "Timer 2 Control",
            value = 0
        }
        self.registers["TMR2"] = {
            address = 0xFCB,
            size = 1,
            access = "rw",
            description = "Timer 2 Register",
            value = 0
        }
        self.registers["T3CON"] = {
            address = 0xFB1,
            size = 1,
            access = "rw",
            description = "Timer 3 Control",
            value = 0
        }
        self.registers["TMR3"] = {
            address = 0xFB3,
            size = 1,
            access = "rw",
            description = "Timer 3 Register High",
            value = 0
        }
        self.registers["TMR3L"] = {
            address = 0xFB2,
            size = 1,
            access = "rw",
            description = "Timer 3 Register Low",
            value = 0
        }
        self.registers["SSPCON1"] = {
            address = 0xFC6,
            size = 1,
            access = "rw",
            description = "SSP Control 1",
            value = 0
        }
        self.registers["SSPCON2"] = {
            address = 0xFC5,
            size = 1,
            access = "rw",
            description = "SSP Control 2",
            value = 0
        }
        self.registers["SSPSTAT"] = {
            address = 0xFC7,
            size = 1,
            access = "rw",
            description = "SSP Status",
            value = 0
        }
        self.registers["SSPBUF"] = {
            address = 0xFC9,
            size = 1,
            access = "rw",
            description = "SSP Buffer",
            value = 0
        }
        self.registers["SSPOR"] = {
            address = 0xFC8,
            size = 1,
            access = "rw",
            description = "SSP Shift Register",
            value = 0
        }
        self.registers["ADCON0"] = {
            address = 0xFC2,
            size = 1,
            access = "rw",
            description = "A/D Control 0",
            value = 0
        }
        self.registers["ADCON1"] = {
            address = 0xFC1,
            size = 1,
            access = "rw",
            description = "A/D Control 1",
            value = 0
        }
        self.registers["ADCON2"] = {
            address = 0xFC0,
            size = 1,
            access = "rw",
            description = "A/D Control 2",
            value = 0
        }
        self.registers["ADRES"] = {
            address = 0xFC3,
            size = 1,
            access = "rw",
            description = "A/D Result",
            value = 0
        }
        self.registers["ADRESL"] = {
            address = 0xFC4,
            size = 1,
            access = "rw",
            description = "A/D Result Low",
            value = 0
        }
        self.registers["CCP1CON"] = {
            address = 0xFD4,
            size = 1,
            access = "rw",
            description = "CCP 1 Control",
            value = 0
        }
        self.registers["CCPR1"] = {
            address = 0xFD6,
            size = 1,
            access = "rw",
            description = "CCP 1 Register High",
            value = 0
        }
        self.registers["CCPR1L"] = {
            address = 0xFD5,
            size = 1,
            access = "rw",
            description = "CCP 1 Register Low",
            value = 0
        }
        self.registers["CCP2CON"] = {
            address = 0xFBA,
            size = 1,
            access = "rw",
            description = "CCP 2 Control",
            value = 0
        }
        self.registers["CCPR2"] = {
            address = 0xFBB,
            size = 1,
            access = "rw",
            description = "CCP 2 Register High",
            value = 0
        }
        self.registers["CCPR2L"] = {
            address = 0xFBC,
            size = 1,
            access = "rw",
            description = "CCP 2 Register Low",
            value = 0
        }
        self.registers["USBCON"] = {
            address = 0xF75,
            size = 1,
            access = "rw",
            description = "USB Control",
            value = 0
        }
        self.registers["USBSTAT"] = {
            address = 0xF74,
            size = 1,
            access = "rw",
            description = "USB Status",
            value = 0
        }
        self.registers["UIE"] = {
            address = 0xF73,
            size = 1,
            access = "rw",
            description = "USB Interrupt Enable",
            value = 0
        }
        self.registers["UIR"] = {
            address = 0xF72,
            size = 1,
            access = "rw",
            description = "USB Interrupt Flag",
            value = 0
        }
        self.registers["UCON"] = {
            address = 0xF71,
            size = 1,
            access = "rw",
            description = "USB Control",
            value = 0
        }
        self.registers["USTAT"] = {
            address = 0xF70,
            size = 1,
            access = "rw",
            description = "USB Status",
            value = 0
        }
        self.registers["UEP0"] = {
            address = 0xF60,
            size = 1,
            access = "rw",
            description = "USB Endpoint 0",
            value = 0
        }
        self.registers["UEP1"] = {
            address = 0xF61,
            size = 1,
            access = "rw",
            description = "USB Endpoint 1",
            value = 0
        }
        self.registers["UEP2"] = {
            address = 0xF62,
            size = 1,
            access = "rw",
            description = "USB Endpoint 2",
            value = 0
        }
        self.registers["UEP3"] = {
            address = 0xF63,
            size = 1,
            access = "rw",
            description = "USB Endpoint 3",
            value = 0
        }
        self.registers["UEP4"] = {
            address = 0xF64,
            size = 1,
            access = "rw",
            description = "USB Endpoint 4",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTA"] = {
            base = 0xF80,
            type = "gpio",
            description = "Port A",
            registers = {}
        }
        
        local p = self.peripherals["PORTA"]
        p.registers["PORT"] = {
            address = 0xF80,
            size = 1,
            value = 0
        }
        p.registers["TRIS"] = {
            address = 0xF92,
            size = 1,
            value = 0
        }
        p.registers["LAT"] = {
            address = 0xF89,
            size = 1,
            value = 0
        }
        self.peripherals["PORTB"] = {
            base = 0xF81,
            type = "gpio",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["PORT"] = {
            address = 0xF81,
            size = 1,
            value = 0
        }
        p.registers["TRIS"] = {
            address = 0xF93,
            size = 1,
            value = 0
        }
        p.registers["LAT"] = {
            address = 0xF8A,
            size = 1,
            value = 0
        }
        self.peripherals["PORTC"] = {
            base = 0xF82,
            type = "gpio",
            description = "Port C",
            registers = {}
        }
        
        local p = self.peripherals["PORTC"]
        p.registers["PORT"] = {
            address = 0xF82,
            size = 1,
            value = 0
        }
        p.registers["TRIS"] = {
            address = 0xF94,
            size = 1,
            value = 0
        }
        p.registers["LAT"] = {
            address = 0xF8B,
            size = 1,
            value = 0
        }
        self.peripherals["PORTD"] = {
            base = 0xF83,
            type = "gpio",
            description = "Port D",
            registers = {}
        }
        
        local p = self.peripherals["PORTD"]
        p.registers["PORT"] = {
            address = 0xF83,
            size = 1,
            value = 0
        }
        p.registers["TRIS"] = {
            address = 0xF95,
            size = 1,
            value = 0
        }
        p.registers["LAT"] = {
            address = 0xF8C,
            size = 1,
            value = 0
        }
        self.peripherals["PORTE"] = {
            base = 0xF84,
            type = "gpio",
            description = "Port E",
            registers = {}
        }
        
        local p = self.peripherals["PORTE"]
        p.registers["PORT"] = {
            address = 0xF84,
            size = 1,
            value = 0
        }
        p.registers["TRIS"] = {
            address = 0xF96,
            size = 1,
            value = 0
        }
        p.registers["LAT"] = {
            address = 0xF8D,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0xFD1,
            type = "timer",
            description = "Timer 0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["T0CON"] = {
            address = 0xFD1,
            size = 1,
            value = 0
        }
        p.registers["TMR0"] = {
            address = 0xFD6,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER1"] = {
            base = 0xFCD,
            type = "timer",
            description = "Timer 1",
            registers = {}
        }
        
        local p = self.peripherals["TIMER1"]
        p.registers["T1CON"] = {
            address = 0xFCD,
            size = 1,
            value = 0
        }
        p.registers["TMR1"] = {
            address = 0xFCF,
            size = 1,
            value = 0
        }
        p.registers["TMR1L"] = {
            address = 0xFCE,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER2"] = {
            base = 0xFCA,
            type = "timer",
            description = "Timer 2",
            registers = {}
        }
        
        local p = self.peripherals["TIMER2"]
        p.registers["T2CON"] = {
            address = 0xFCA,
            size = 1,
            value = 0
        }
        p.registers["TMR2"] = {
            address = 0xFCB,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER3"] = {
            base = 0xFB0,
            type = "timer",
            description = "Timer 3",
            registers = {}
        }
        
        local p = self.peripherals["TIMER3"]
        p.registers["T3CON"] = {
            address = 0xFB0,
            size = 1,
            value = 0
        }
        p.registers["TMR3"] = {
            address = 0xFB2,
            size = 1,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0xFC2,
            type = "adc",
            description = "A/D Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADCON0"] = {
            address = 0xFC2,
            size = 1,
            value = 0
        }
        p.registers["ADCON1"] = {
            address = 0xFC1,
            size = 1,
            value = 0
        }
        p.registers["ADCON2"] = {
            address = 0xFC0,
            size = 1,
            value = 0
        }
        p.registers["ADRES"] = {
            address = 0xFC3,
            size = 1,
            value = 0
        }
        p.registers["ADRESL"] = {
            address = 0xFC4,
            size = 1,
            value = 0
        }
        self.peripherals["CCP1"] = {
            base = 0xFD4,
            type = "ccp",
            description = "CCP 1",
            registers = {}
        }
        
        local p = self.peripherals["CCP1"]
        p.registers["CCP1CON"] = {
            address = 0xFD4,
            size = 1,
            value = 0
        }
        p.registers["CCPR1"] = {
            address = 0xFD6,
            size = 1,
            value = 0
        }
        p.registers["CCPR1L"] = {
            address = 0xFD5,
            size = 1,
            value = 0
        }
        self.peripherals["CCP2"] = {
            base = 0xFBA,
            type = "ccp",
            description = "CCP 2",
            registers = {}
        }
        
        local p = self.peripherals["CCP2"]
        p.registers["CCP2CON"] = {
            address = 0xFBA,
            size = 1,
            value = 0
        }
        p.registers["CCPR2"] = {
            address = 0xFBB,
            size = 1,
            value = 0
        }
        p.registers["CCPR2L"] = {
            address = 0xFBC,
            size = 1,
            value = 0
        }
        self.peripherals["SSP"] = {
            base = 0xFC6,
            type = "ssp",
            description = "SSP (I2C/SPI)",
            registers = {}
        }
        
        local p = self.peripherals["SSP"]
        p.registers["SSPCON1"] = {
            address = 0xFC6,
            size = 1,
            value = 0
        }
        p.registers["SSPCON2"] = {
            address = 0xFC5,
            size = 1,
            value = 0
        }
        p.registers["SSPSTAT"] = {
            address = 0xFC7,
            size = 1,
            value = 0
        }
        p.registers["SSPBUF"] = {
            address = 0xFC9,
            size = 1,
            value = 0
        }
        p.registers["SSPOV"] = {
            address = 0xFC8,
            size = 1,
            value = 0
        }
        self.peripherals["EUSART"] = {
            base = 0xF15,
            type = "uart",
            description = "EUSART",
            registers = {}
        }
        
        local p = self.peripherals["EUSART"]
        p.registers["TXSTA"] = {
            address = 0xFE2,
            size = 1,
            value = 0
        }
        p.registers["RCSTA"] = {
            address = 0xFE3,
            size = 1,
            value = 0
        }
        p.registers["TXREG"] = {
            address = 0xFAD,
            size = 1,
            value = 0
        }
        p.registers["RCREG"] = {
            address = 0xFAE,
            size = 1,
            value = 0
        }
        p.registers["SPBRG"] = {
            address = 0xFAF,
            size = 1,
            value = 0
        }
        p.registers["SPBRGH"] = {
            address = 0xFB0,
            size = 1,
            value = 0
        }
        p.registers["BAUDCON"] = {
            address = 0xFB8,
            size = 1,
            value = 0
        }
        self.peripherals["COMPARATOR"] = {
            base = 0xFB4,
            type = "comparator",
            description = "Comparators",
            registers = {}
        }
        
        local p = self.peripherals["COMPARATOR"]
        p.registers["CMCON"] = {
            address = 0xFB4,
            size = 1,
            value = 0
        }
        p.registers["CVRCON"] = {
            address = 0xFB5,
            size = 1,
            value = 0
        }
        self.peripherals["USB"] = {
            base = 0xF70,
            type = "usb",
            description = "USB Module",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["UCON"] = {
            address = 0xF71,
            size = 1,
            value = 0
        }
        p.registers["USTAT"] = {
            address = 0xF72,
            size = 1,
            value = 0
        }
        p.registers["UIR"] = {
            address = 0xF73,
            size = 1,
            value = 0
        }
        p.registers["UIE"] = {
            address = 0xF74,
            size = 1,
            value = 0
        }
        p.registers["UEP0"] = {
            address = 0xF80,
            size = 1,
            value = 0
        }
        p.registers["UEP1"] = {
            address = 0xF81,
            size = 1,
            value = 0
        }
        p.registers["UEP2"] = {
            address = 0xF82,
            size = 1,
            value = 0
        }
        p.registers["UEP3"] = {
            address = 0xF83,
            size = 1,
            value = 0
        }
        p.registers["BD0"] = {
            address = 0xF00,
            size = 1,
            value = 0
        }
        p.registers["BD1"] = {
            address = 0xF08,
            size = 1,
            value = 0
        }
        p.registers["BD2"] = {
            address = 0xF10,
            size = 1,
            value = 0
        }
        p.registers["BD3"] = {
            address = 0xF18,
            size = 1,
            value = 0
        }
        self.peripherals["OSCCON"] = {
            base = 0xFD3,
            type = "osc",
            description = "Oscillator",
            registers = {}
        }
        
        local p = self.peripherals["OSCCON"]
        p.registers["OSCCON"] = {
            address = 0xFD3,
            size = 1,
            value = 0
        }
        p.registers["OSCTUNE"] = {
            address = 0xFD9,
            size = 1,
            value = 0
        }
        self.peripherals["WDTCON"] = {
            base = 0xFD1,
            type = "wdt",
            description = "Watchdog Timer",
            registers = {}
        }
        
        local p = self.peripherals["WDTCON"]
        p.registers["WDTCON"] = {
            address = 0xFD1,
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
            name = PIC18F4550.DEVICE_NAME,
            manufacturer = PIC18F4550.MANUFACTURER,
            family = PIC18F4550.FAMILY,
            version = PIC18F4550.VERSION,
            architecture = PIC18F4550.ARCHITECTURE,
            bits = PIC18F4550.BITS,
            clock_frequency = PIC18F4550.CLOCK_FREQUENCY
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
        return string.format("PIC18F4550(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PIC18F4550.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PIC18F4550.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PIC18F4550.print_device_info(device)
    device = device or PIC18F4550.new()
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

function PIC18F4550.print_registers(device)
    device = device or PIC18F4550.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PIC18F4550.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PIC18F4550.example()
    print("=== PIC18F4550设备示例 ===")
    
    -- 创建设备实例
    local device = PIC18F4550.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PIC18F4550.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["W"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("W", 0x55)
        print("写入 W: " .. PIC18F4550.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("W")
        print("读取 W: " .. PIC18F4550.hex(value))
        
        -- 位操作
        device:set_bit("W", 0, true)
        local bit0 = device:get_bit("W", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    PIC18F4550.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PIC18F4550.lua$") then
    PIC18F4550.example()
end

return PIC18F4550
