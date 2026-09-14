"""
PIC18F4550设备定义 - Python模块
生成自: Microchip/PIC18/PIC18F4550
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: 8-bit PIC with USB 2.0, 32KB Flash, 2KB SRAM
CPU架构: PIC18
位宽: 8位
时钟频率: 20000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class PIC18F4550:
    """PIC18F4550设备类"""

    # 设备信息
    DEVICE_NAME = "PIC18F4550"
    MANUFACTURER = "Microchip"
    FAMILY = "PIC18"
    VERSION = "1.0"
    ARCHITECTURE = "PIC18"
    BITS = 8
    CLOCK_FREQUENCY = 20000000

    # 寄存器地址定义
    W_ADDR = 0x0E  # Working Register
    STATUS_ADDR = 0xFD8  # Status Register
    STATUS_C_BIT = 0  # Carry Flag
    STATUS_DC_BIT = 1  # Digit Carry Flag
    STATUS_Z_BIT = 2  # Zero Flag
    STATUS_PD_BIT = 3  # Power-Down Flag
    STATUS_TO_BIT = 4  # Time-out Flag
    STATUS_RP_BIT = 0  # Register Bank Select
    STATUS_IRP_BIT = 7  # Indirect Register Bank Select
    BSR_ADDR = 0xFE0  # Bank Select Register
    PORTA_ADDR = 0xF80  # Port A
    PORTB_ADDR = 0xF81  # Port B
    PORTC_ADDR = 0xF82  # Port C
    PORTD_ADDR = 0xF83  # Port D
    PORTE_ADDR = 0xF84  # Port E
    TRISA_ADDR = 0xF92  # Tri-state Port A
    TRISB_ADDR = 0xF93  # Tri-state Port B
    TRISC_ADDR = 0xF94  # Tri-state Port C
    TRISD_ADDR = 0xF95  # Tri-state Port D
    TRISE_ADDR = 0xF96  # Tri-state Port E
    LATA_ADDR = 0xF89  # Latch Port A
    LATB_ADDR = 0xF8A  # Latch Port B
    LATC_ADDR = 0xF8B  # Latch Port C
    LATD_ADDR = 0xF8C  # Latch Port D
    LATE_ADDR = 0xF8D  # Latch Port E
    INTCON_ADDR = 0xFF2  # Interrupt Control
    INTCON_RBIF_BIT = 0  # Port B Interrupt Flag
    INTCON_INT0IF_BIT = 1  # INT0 Interrupt Flag
    INTCON_TMR0IF_BIT = 2  # Timer 0 Interrupt Flag
    INTCON_RBIE_BIT = 3  # Port B Interrupt Enable
    INTCON_INT0IE_BIT = 4  # INT0 Interrupt Enable
    INTCON_TMR0IE_BIT = 5  # Timer 0 Interrupt Enable
    INTCON_PEIE_BIT = 6  # Peripheral Interrupt Enable
    INTCON_GIE_BIT = 7  # Global Interrupt Enable
    PIR1_ADDR = 0xF9E  # Peripheral Interrupt 1
    PIR2_ADDR = 0xF9F  # Peripheral Interrupt 2
    PIE1_ADDR = 0xF9D  # Peripheral Interrupt Enable 1
    PIE2_ADDR = 0xF9C  # Peripheral Interrupt Enable 2
    IPR1_ADDR = 0xF9B  # Interrupt Priority 1
    IPR2_ADDR = 0xF9A  # Interrupt Priority 2
    RCON_ADDR = 0xFD0  # Reset Control
    RCON_NOT_TO_BIT = 3  # Time-out Flag
    RCON_NOT_PD_BIT = 4  # Power-Down Flag
    RCON_NOT_RI_BIT = 5  # RESET Flag
    RCON_NOT_POR_BIT = 6  # Power-on Reset Flag
    RCON_NOT_BOR_BIT = 7  # Brown-out Reset Flag
    T0CON_ADDR = 0xFD1  # Timer 0 Control
    TMR0_ADDR = 0xFD6  # Timer 0 Register
    T1CON_ADDR = 0xFCD  # Timer 1 Control
    TMR1_ADDR = 0xFCE  # Timer 1 Register High
    TMR1L_ADDR = 0xFCF  # Timer 1 Register Low
    T2CON_ADDR = 0xFCA  # Timer 2 Control
    TMR2_ADDR = 0xFCB  # Timer 2 Register
    T3CON_ADDR = 0xFB1  # Timer 3 Control
    TMR3_ADDR = 0xFB3  # Timer 3 Register High
    TMR3L_ADDR = 0xFB2  # Timer 3 Register Low
    SSPCON1_ADDR = 0xFC6  # SSP Control 1
    SSPCON2_ADDR = 0xFC5  # SSP Control 2
    SSPSTAT_ADDR = 0xFC7  # SSP Status
    SSPBUF_ADDR = 0xFC9  # SSP Buffer
    SSPOR_ADDR = 0xFC8  # SSP Shift Register
    ADCON0_ADDR = 0xFC2  # A/D Control 0
    ADCON1_ADDR = 0xFC1  # A/D Control 1
    ADCON2_ADDR = 0xFC0  # A/D Control 2
    ADRES_ADDR = 0xFC3  # A/D Result
    ADRESL_ADDR = 0xFC4  # A/D Result Low
    CCP1CON_ADDR = 0xFD4  # CCP 1 Control
    CCPR1_ADDR = 0xFD6  # CCP 1 Register High
    CCPR1L_ADDR = 0xFD5  # CCP 1 Register Low
    CCP2CON_ADDR = 0xFBA  # CCP 2 Control
    CCPR2_ADDR = 0xFBB  # CCP 2 Register High
    CCPR2L_ADDR = 0xFBC  # CCP 2 Register Low
    USBCON_ADDR = 0xF75  # USB Control
    USBSTAT_ADDR = 0xF74  # USB Status
    UIE_ADDR = 0xF73  # USB Interrupt Enable
    UIR_ADDR = 0xF72  # USB Interrupt Flag
    UCON_ADDR = 0xF71  # USB Control
    USTAT_ADDR = 0xF70  # USB Status
    UEP0_ADDR = 0xF60  # USB Endpoint 0
    UEP1_ADDR = 0xF61  # USB Endpoint 1
    UEP2_ADDR = 0xF62  # USB Endpoint 2
    UEP3_ADDR = 0xF63  # USB Endpoint 3
    UEP4_ADDR = 0xF64  # USB Endpoint 4

    # 内存段定义
    FLASH_START = 0x0000
    FLASH_END = 0x7FFF
    FLASH_SIZE = 32768  # Program Flash (32KB)
    EEPROM_START = 0xF00000
    EEPROM_END = 0xF000FF
    EEPROM_SIZE = 256  # EEPROM (256B)
    SRAM_START = 0x0000
    SRAM_END = 0x07FF
    SRAM_SIZE = 2048  # SRAM (2KB)
    ACCESS_START = 0x0000
    ACCESS_END = 
    ACCESS_SIZE = 1  # Access Bank

    # 外设定义
    # Port A
    PORTA_BASE = 0xF80
    PORTA_PORT_ADDR = 0xF80
    PORTA_TRIS_ADDR = 0xF92
    PORTA_LAT_ADDR = 0xF89
    # Port B
    PORTB_BASE = 0xF81
    PORTB_PORT_ADDR = 0xF81
    PORTB_TRIS_ADDR = 0xF93
    PORTB_LAT_ADDR = 0xF8A
    # Port C
    PORTC_BASE = 0xF82
    PORTC_PORT_ADDR = 0xF82
    PORTC_TRIS_ADDR = 0xF94
    PORTC_LAT_ADDR = 0xF8B
    # Port D
    PORTD_BASE = 0xF83
    PORTD_PORT_ADDR = 0xF83
    PORTD_TRIS_ADDR = 0xF95
    PORTD_LAT_ADDR = 0xF8C
    # Port E
    PORTE_BASE = 0xF84
    PORTE_PORT_ADDR = 0xF84
    PORTE_TRIS_ADDR = 0xF96
    PORTE_LAT_ADDR = 0xF8D
    # Timer 0
    TIMER0_BASE = 0xFD1
    TIMER0_T0CON_ADDR = 0xFD1
    TIMER0_TMR0_ADDR = 0xFD6
    # Timer 1
    TIMER1_BASE = 0xFCD
    TIMER1_T1CON_ADDR = 0xFCD
    TIMER1_TMR1_ADDR = 0xFCF
    TIMER1_TMR1L_ADDR = 0xFCE
    # Timer 2
    TIMER2_BASE = 0xFCA
    TIMER2_T2CON_ADDR = 0xFCA
    TIMER2_TMR2_ADDR = 0xFCB
    # Timer 3
    TIMER3_BASE = 0xFB0
    TIMER3_T3CON_ADDR = 0xFB0
    TIMER3_TMR3_ADDR = 0xFB2
    # A/D Converter
    ADC_BASE = 0xFC2
    ADC_ADCON0_ADDR = 0xFC2
    ADC_ADCON1_ADDR = 0xFC1
    ADC_ADCON2_ADDR = 0xFC0
    ADC_ADRES_ADDR = 0xFC3
    ADC_ADRESL_ADDR = 0xFC4
    # CCP 1
    CCP1_BASE = 0xFD4
    CCP1_CCP1CON_ADDR = 0xFD4
    CCP1_CCPR1_ADDR = 0xFD6
    CCP1_CCPR1L_ADDR = 0xFD5
    # CCP 2
    CCP2_BASE = 0xFBA
    CCP2_CCP2CON_ADDR = 0xFBA
    CCP2_CCPR2_ADDR = 0xFBB
    CCP2_CCPR2L_ADDR = 0xFBC
    # SSP (I2C/SPI)
    SSP_BASE = 0xFC6
    SSP_SSPCON1_ADDR = 0xFC6
    SSP_SSPCON2_ADDR = 0xFC5
    SSP_SSPSTAT_ADDR = 0xFC7
    SSP_SSPBUF_ADDR = 0xFC9
    SSP_SSPOV_ADDR = 0xFC8
    # EUSART
    EUSART_BASE = 0xF15
    EUSART_TXSTA_ADDR = 0xFE2
    EUSART_RCSTA_ADDR = 0xFE3
    EUSART_TXREG_ADDR = 0xFAD
    EUSART_RCREG_ADDR = 0xFAE
    EUSART_SPBRG_ADDR = 0xFAF
    EUSART_SPBRGH_ADDR = 0xFB0
    EUSART_BAUDCON_ADDR = 0xFB8
    # Comparators
    COMPARATOR_BASE = 0xFB4
    COMPARATOR_CMCON_ADDR = 0xFB4
    COMPARATOR_CVRCON_ADDR = 0xFB5
    # USB Module
    USB_BASE = 0xF70
    USB_UCON_ADDR = 0xF71
    USB_USTAT_ADDR = 0xF72
    USB_UIR_ADDR = 0xF73
    USB_UIE_ADDR = 0xF74
    USB_UEP0_ADDR = 0xF80
    USB_UEP1_ADDR = 0xF81
    USB_UEP2_ADDR = 0xF82
    USB_UEP3_ADDR = 0xF83
    USB_BD0_ADDR = 0xF00
    USB_BD1_ADDR = 0xF08
    USB_BD2_ADDR = 0xF10
    USB_BD3_ADDR = 0xF18
    # Oscillator
    OSCCON_BASE = 0xFD3
    OSCCON_OSCCON_ADDR = 0xFD3
    OSCCON_OSCTUNE_ADDR = 0xFD9
    # Watchdog Timer
    WDTCON_BASE = 0xFD1
    WDTCON_WDTCON_ADDR = 0xFD1

    # 中断向量定义
    INT_RESET = 0  # RESET
    INT_INT0 = 1  # External Interrupt 0
    INT_INT1 = 2  # External Interrupt 1
    INT_INT2 = 3  # External Interrupt 2
    INT_TMR0 = 4  # Timer 0 Overflow
    INT_TMR1 = 5  # Timer 1 Overflow
    INT_TMR2 = 6  # Timer 2 Match
    INT_TMR3 = 7  # Timer 3 Overflow
    INT_CCP1 = 8  # CCP 1
    INT_CCP2 = 9  # CCP 2
    INT_SSP = 10  # SSP
    INT_TX = 11  # USART TX
    INT_RC = 12  # USART RX
    INT_ADC = 13  # A/D
    INT_RBO = 14  # Port B Change
    INT_EXT = 15  # External

    # 引脚定义
    PIN_RE3 = 1  # MCLR/VPP/RE3
    PIN_RA0 = 2  # AN0/RA0
    PIN_RA1 = 3  # AN1/RA1
    PIN_RA2 = 4  # AN2/VREF-/RA2
    PIN_RA3 = 5  # AN3/VREF+/RA3
    PIN_RA4 = 6  # AN4/T0CKI/RA4
    PIN_RA5 = 7  # AN5/RE5
    PIN_VSS = 8  # Ground
    PIN_RA7 = 9  # OSC1/CLKI/RA7
    PIN_RA6 = 10  # OSC2/CLKO/RA6
    PIN_RC0 = 11  # T1OSO/T1CKI/RC0
    PIN_RC1 = 12  # T1OSI/RC1
    PIN_RC2 = 13  # CCP1/RC2
    PIN_RC3 = 14  # SCK/SCL/RC3
    PIN_RD0 = 15  # SDO/RD0
    PIN_RD1 = 16  # SDI/RD1
    PIN_RD2 = 17  # RD2
    PIN_RC6 = 18  # TX/CK/RC6
    PIN_RC7 = 19  # RX/DT/RC7
    PIN_VSS = 20  # Ground
    PIN_RD3 = 21  # RD3
    PIN_RD4 = 22  # RD4
    PIN_RD5 = 23  # PWRB/RD5
    PIN_RD6 = 24  # PBC/RD6
    PIN_RD7 = 25  # PCD/RD7
    PIN_RC4 = 26  # D-/RC4
    PIN_RC5 = 27  # D+/RC5
    PIN_RE0 = 28  # AN5/RE0
    PIN_RE1 = 29  # AN6/RE1
    PIN_RE2 = 30  # AN7/RE2
    PIN_VSS = 31  # Ground
    PIN_VDD = 32  # Vdd

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["W"] = {
            "address": 0x0E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Working Register",
            "value": 0
        }
        self._registers["STATUS"] = {
            "address": 0xFD8,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }
        self._registers["BSR"] = {
            "address": 0xFE0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Bank Select Register",
            "value": 0
        }
        self._registers["PORTA"] = {
            "address": 0xF80,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Port A",
            "value": 0
        }
        self._registers["PORTB"] = {
            "address": 0xF81,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Port B",
            "value": 0
        }
        self._registers["PORTC"] = {
            "address": 0xF82,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Port C",
            "value": 0
        }
        self._registers["PORTD"] = {
            "address": 0xF83,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Port D",
            "value": 0
        }
        self._registers["PORTE"] = {
            "address": 0xF84,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Port E",
            "value": 0
        }
        self._registers["TRISA"] = {
            "address": 0xF92,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Tri-state Port A",
            "value": 0
        }
        self._registers["TRISB"] = {
            "address": 0xF93,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Tri-state Port B",
            "value": 0
        }
        self._registers["TRISC"] = {
            "address": 0xF94,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Tri-state Port C",
            "value": 0
        }
        self._registers["TRISD"] = {
            "address": 0xF95,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Tri-state Port D",
            "value": 0
        }
        self._registers["TRISE"] = {
            "address": 0xF96,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Tri-state Port E",
            "value": 0
        }
        self._registers["LATA"] = {
            "address": 0xF89,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Latch Port A",
            "value": 0
        }
        self._registers["LATB"] = {
            "address": 0xF8A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Latch Port B",
            "value": 0
        }
        self._registers["LATC"] = {
            "address": 0xF8B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Latch Port C",
            "value": 0
        }
        self._registers["LATD"] = {
            "address": 0xF8C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Latch Port D",
            "value": 0
        }
        self._registers["LATE"] = {
            "address": 0xF8D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Latch Port E",
            "value": 0
        }
        self._registers["INTCON"] = {
            "address": 0xFF2,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Control",
            "value": 0
        }
        self._registers["PIR1"] = {
            "address": 0xF9E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Peripheral Interrupt 1",
            "value": 0
        }
        self._registers["PIR2"] = {
            "address": 0xF9F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Peripheral Interrupt 2",
            "value": 0
        }
        self._registers["PIE1"] = {
            "address": 0xF9D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Peripheral Interrupt Enable 1",
            "value": 0
        }
        self._registers["PIE2"] = {
            "address": 0xF9C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Peripheral Interrupt Enable 2",
            "value": 0
        }
        self._registers["IPR1"] = {
            "address": 0xF9B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Priority 1",
            "value": 0
        }
        self._registers["IPR2"] = {
            "address": 0xF9A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Priority 2",
            "value": 0
        }
        self._registers["RCON"] = {
            "address": 0xFD0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Reset Control",
            "value": 0
        }
        self._registers["T0CON"] = {
            "address": 0xFD1,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 0 Control",
            "value": 0
        }
        self._registers["TMR0"] = {
            "address": 0xFD6,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 0 Register",
            "value": 0
        }
        self._registers["T1CON"] = {
            "address": 0xFCD,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 Control",
            "value": 0
        }
        self._registers["TMR1"] = {
            "address": 0xFCE,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 Register High",
            "value": 0
        }
        self._registers["TMR1L"] = {
            "address": 0xFCF,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 Register Low",
            "value": 0
        }
        self._registers["T2CON"] = {
            "address": 0xFCA,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 2 Control",
            "value": 0
        }
        self._registers["TMR2"] = {
            "address": 0xFCB,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 2 Register",
            "value": 0
        }
        self._registers["T3CON"] = {
            "address": 0xFB1,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 3 Control",
            "value": 0
        }
        self._registers["TMR3"] = {
            "address": 0xFB3,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 3 Register High",
            "value": 0
        }
        self._registers["TMR3L"] = {
            "address": 0xFB2,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 3 Register Low",
            "value": 0
        }
        self._registers["SSPCON1"] = {
            "address": 0xFC6,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Control 1",
            "value": 0
        }
        self._registers["SSPCON2"] = {
            "address": 0xFC5,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Control 2",
            "value": 0
        }
        self._registers["SSPSTAT"] = {
            "address": 0xFC7,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Status",
            "value": 0
        }
        self._registers["SSPBUF"] = {
            "address": 0xFC9,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Buffer",
            "value": 0
        }
        self._registers["SSPOR"] = {
            "address": 0xFC8,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Shift Register",
            "value": 0
        }
        self._registers["ADCON0"] = {
            "address": 0xFC2,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Control 0",
            "value": 0
        }
        self._registers["ADCON1"] = {
            "address": 0xFC1,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Control 1",
            "value": 0
        }
        self._registers["ADCON2"] = {
            "address": 0xFC0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Control 2",
            "value": 0
        }
        self._registers["ADRES"] = {
            "address": 0xFC3,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Result",
            "value": 0
        }
        self._registers["ADRESL"] = {
            "address": 0xFC4,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Result Low",
            "value": 0
        }
        self._registers["CCP1CON"] = {
            "address": 0xFD4,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 1 Control",
            "value": 0
        }
        self._registers["CCPR1"] = {
            "address": 0xFD6,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 1 Register High",
            "value": 0
        }
        self._registers["CCPR1L"] = {
            "address": 0xFD5,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 1 Register Low",
            "value": 0
        }
        self._registers["CCP2CON"] = {
            "address": 0xFBA,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 2 Control",
            "value": 0
        }
        self._registers["CCPR2"] = {
            "address": 0xFBB,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 2 Register High",
            "value": 0
        }
        self._registers["CCPR2L"] = {
            "address": 0xFBC,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP 2 Register Low",
            "value": 0
        }
        self._registers["USBCON"] = {
            "address": 0xF75,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Control",
            "value": 0
        }
        self._registers["USBSTAT"] = {
            "address": 0xF74,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Status",
            "value": 0
        }
        self._registers["UIE"] = {
            "address": 0xF73,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Interrupt Enable",
            "value": 0
        }
        self._registers["UIR"] = {
            "address": 0xF72,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Interrupt Flag",
            "value": 0
        }
        self._registers["UCON"] = {
            "address": 0xF71,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Control",
            "value": 0
        }
        self._registers["USTAT"] = {
            "address": 0xF70,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Status",
            "value": 0
        }
        self._registers["UEP0"] = {
            "address": 0xF60,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Endpoint 0",
            "value": 0
        }
        self._registers["UEP1"] = {
            "address": 0xF61,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Endpoint 1",
            "value": 0
        }
        self._registers["UEP2"] = {
            "address": 0xF62,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Endpoint 2",
            "value": 0
        }
        self._registers["UEP3"] = {
            "address": 0xF63,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Endpoint 3",
            "value": 0
        }
        self._registers["UEP4"] = {
            "address": 0xF64,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USB Endpoint 4",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PORTA"] = {
            "base": 0xF80,
            "type": "gpio",
            "description": "Port A",
            "registers": {
                "PORT": {
                    "address": 0xF80,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIS": {
                    "address": 0xF92,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LAT": {
                    "address": 0xF89,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTB"] = {
            "base": 0xF81,
            "type": "gpio",
            "description": "Port B",
            "registers": {
                "PORT": {
                    "address": 0xF81,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIS": {
                    "address": 0xF93,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LAT": {
                    "address": 0xF8A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTC"] = {
            "base": 0xF82,
            "type": "gpio",
            "description": "Port C",
            "registers": {
                "PORT": {
                    "address": 0xF82,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIS": {
                    "address": 0xF94,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LAT": {
                    "address": 0xF8B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTD"] = {
            "base": 0xF83,
            "type": "gpio",
            "description": "Port D",
            "registers": {
                "PORT": {
                    "address": 0xF83,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIS": {
                    "address": 0xF95,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LAT": {
                    "address": 0xF8C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTE"] = {
            "base": 0xF84,
            "type": "gpio",
            "description": "Port E",
            "registers": {
                "PORT": {
                    "address": 0xF84,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRIS": {
                    "address": 0xF96,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LAT": {
                    "address": 0xF8D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0xFD1,
            "type": "timer",
            "description": "Timer 0",
            "registers": {
                "T0CON": {
                    "address": 0xFD1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR0": {
                    "address": 0xFD6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER1"] = {
            "base": 0xFCD,
            "type": "timer",
            "description": "Timer 1",
            "registers": {
                "T1CON": {
                    "address": 0xFCD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR1": {
                    "address": 0xFCF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR1L": {
                    "address": 0xFCE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER2"] = {
            "base": 0xFCA,
            "type": "timer",
            "description": "Timer 2",
            "registers": {
                "T2CON": {
                    "address": 0xFCA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR2": {
                    "address": 0xFCB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER3"] = {
            "base": 0xFB0,
            "type": "timer",
            "description": "Timer 3",
            "registers": {
                "T3CON": {
                    "address": 0xFB0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR3": {
                    "address": 0xFB2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0xFC2,
            "type": "adc",
            "description": "A/D Converter",
            "registers": {
                "ADCON0": {
                    "address": 0xFC2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCON1": {
                    "address": 0xFC1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCON2": {
                    "address": 0xFC0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADRES": {
                    "address": 0xFC3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADRESL": {
                    "address": 0xFC4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CCP1"] = {
            "base": 0xFD4,
            "type": "ccp",
            "description": "CCP 1",
            "registers": {
                "CCP1CON": {
                    "address": 0xFD4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR1": {
                    "address": 0xFD6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR1L": {
                    "address": 0xFD5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CCP2"] = {
            "base": 0xFBA,
            "type": "ccp",
            "description": "CCP 2",
            "registers": {
                "CCP2CON": {
                    "address": 0xFBA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR2": {
                    "address": 0xFBB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR2L": {
                    "address": 0xFBC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SSP"] = {
            "base": 0xFC6,
            "type": "ssp",
            "description": "SSP (I2C/SPI)",
            "registers": {
                "SSPCON1": {
                    "address": 0xFC6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPCON2": {
                    "address": 0xFC5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPSTAT": {
                    "address": 0xFC7,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPBUF": {
                    "address": 0xFC9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPOV": {
                    "address": 0xFC8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["EUSART"] = {
            "base": 0xF15,
            "type": "uart",
            "description": "EUSART",
            "registers": {
                "TXSTA": {
                    "address": 0xFE2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCSTA": {
                    "address": 0xFE3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TXREG": {
                    "address": 0xFAD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCREG": {
                    "address": 0xFAE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPBRG": {
                    "address": 0xFAF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPBRGH": {
                    "address": 0xFB0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BAUDCON": {
                    "address": 0xFB8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["COMPARATOR"] = {
            "base": 0xFB4,
            "type": "comparator",
            "description": "Comparators",
            "registers": {
                "CMCON": {
                    "address": 0xFB4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CVRCON": {
                    "address": 0xFB5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0xF70,
            "type": "usb",
            "description": "USB Module",
            "registers": {
                "UCON": {
                    "address": 0xF71,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "USTAT": {
                    "address": 0xF72,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UIR": {
                    "address": 0xF73,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UIE": {
                    "address": 0xF74,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UEP0": {
                    "address": 0xF80,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UEP1": {
                    "address": 0xF81,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UEP2": {
                    "address": 0xF82,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UEP3": {
                    "address": 0xF83,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BD0": {
                    "address": 0xF00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BD1": {
                    "address": 0xF08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BD2": {
                    "address": 0xF10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BD3": {
                    "address": 0xF18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["OSCCON"] = {
            "base": 0xFD3,
            "type": "osc",
            "description": "Oscillator",
            "registers": {
                "OSCCON": {
                    "address": 0xFD3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OSCTUNE": {
                    "address": 0xFD9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["WDTCON"] = {
            "base": 0xFD1,
            "type": "wdt",
            "description": "Watchdog Timer",
            "registers": {
                "WDTCON": {
                    "address": 0xFD1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }

    def read_register(self, name: str) -> int:
        """读取寄存器值"""
        if name in self._registers:
            return self._registers[name]["value"]
        raise KeyError(f"寄存器 {name} 不存在")

    def write_register(self, name: str, value: int):
        """写入寄存器值"""
        if name in self._registers:
            reg = self._registers[name]
            max_value = (1 << (reg["size"] * 8)) - 1
            if value < 0 or value > max_value:
                raise ValueError(f"值 {value} 超出范围 [0, {max_value}]")
            reg["value"] = value
        else:
            raise KeyError(f"寄存器 {name} 不存在")

    def set_bit(self, register_name: str, bit: int, value: bool):
        """设置寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            if value:
                reg["value"] |= (1 << bit)
            else:
                reg["value"] &= ~(1 << bit)
        else:
            raise KeyError(f"寄存器 {register_name} 不存在")

    def get_bit(self, register_name: str, bit: int) -> bool:
        """获取寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            return (reg["value"] >> bit) & 1 == 1
        raise KeyError(f"寄存器 {register_name} 不存在")

    def get_device_info(self) -> dict:
        """获取设备信息"""
        return {
            "name": self.DEVICE_NAME,
            "manufacturer": self.MANUFACTURER,
            "family": self.FAMILY,
            "version": self.VERSION,
            "architecture": self.ARCHITECTURE,
            "bits": self.BITS,
            "clock_frequency": self.CLOCK_FREQUENCY
        }

    def get_register_info(self, name: str) -> Optional[dict]:
        """获取寄存器信息"""
        return self._registers.get(name)

    def get_peripheral_info(self, name: str) -> Optional[dict]:
        """获取外设信息"""
        return self._peripherals.get(name)

    def reset(self):
        """重置设备"""
        for reg in self._registers.values():
            reg["value"] = 0
        for peripheral in self._peripherals.values():
            for reg in peripheral["registers"].values():
                reg["value"] = 0

    def __str__(self) -> str:
        """字符串表示"""
        info = self.get_device_info()
        return f"PIC18F4550({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = PIC18F4550()
    print(f"设备: {device}")
    print(f"设备信息: {device.get_device_info()}")
    print()
    
    # 演示寄存器操作
    if device.Cpu.Registers.RegisterList.Count > 0:
        first_reg = device.Cpu.Registers.RegisterList[0].Name
        print(f"第一个寄存器: {first_reg}")
        device.write_register(first_reg, 0x55)
        value = device.read_register(first_reg)
        print(f"读取值: 0x{value:X}")
