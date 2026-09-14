"""
PIC16F877A设备定义 - Python模块
生成自: Microchip/PIC/PIC16F877A
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: 8-bit PIC microcontroller with 8KB Flash, 368 bytes RAM, 256 bytes EEPROM
CPU架构: PIC16
位宽: 8位
时钟频率: 4000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class PIC16F877A:
    """PIC16F877A设备类"""

    # 设备信息
    DEVICE_NAME = "PIC16F877A"
    MANUFACTURER = "Microchip"
    FAMILY = "PIC"
    VERSION = "1.0"
    ARCHITECTURE = "PIC16"
    BITS = 8
    CLOCK_FREQUENCY = 4000000

    # 寄存器地址定义
    W_ADDR = 0x00  # Working Register
    STATUS_ADDR = 0x03  # Status Register
    STATUS_C_BIT = 0  # Carry flag
    STATUS_DC_BIT = 1  # Digit carry flag
    STATUS_Z_BIT = 2  # Zero flag
    STATUS_PD_BIT = 3  # Power-down flag
    STATUS_TO_BIT = 4  # Time-out flag
    STATUS_RP_BIT = 5  # Register bank select
    STATUS_IRP_BIT = 7  # Indirect register bank select
    INTCON_ADDR = 0x0B  # Interrupt Control Register
    INTCON_RBIF_BIT = 0  # PORTB change interrupt flag
    INTCON_INTF_BIT = 1  # External interrupt flag
    INTCON_TMR0IF_BIT = 2  # TMR0 overflow interrupt flag
    INTCON_RBIE_BIT = 3  # PORTB change interrupt enable
    INTCON_INTE_BIT = 4  # External interrupt enable
    INTCON_TMR0IE_BIT = 5  # TMR0 overflow interrupt enable
    INTCON_PEIE_BIT = 6  # Peripheral interrupt enable
    INTCON_GIE_BIT = 7  # Global interrupt enable
    PORTB_ADDR = 0x06  # PORT B
    TRISB_ADDR = 0x86  # TRIS B
    PORTC_ADDR = 0x07  # PORT C
    TRISC_ADDR = 0x87  # TRIS C
    PORTD_ADDR = 0x08  # PORT D
    TRISD_ADDR = 0x88  # TRIS D
    PORTE_ADDR = 0x09  # PORT E
    TRISE_ADDR = 0x89  # TRIS E
    TMR0_ADDR = 0x01  # Timer 0
    OPTION_REG_ADDR = 0x81  # Option Register
    PCL_ADDR = 0x02  # Program Counter Low
    PCLATH_ADDR = 0x0A  # Program Counter Latch High
    FSR_ADDR = 0x04  # File Select Register
    EEDATA_ADDR = 0x10C  # EEPROM Data
    EEADR_ADDR = 0x10D  # EEPROM Address
    EECON1_ADDR = 0x18C  # EEPROM Control 1
    EECON1_RD_BIT = 0  # Read control
    EECON1_WR_BIT = 1  # Write control
    EECON1_WREN_BIT = 2  # Write enable
    EECON1_WRERR_BIT = 3  # Write error flag
    EECON1_EEPGD_BIT = 7  # EEPROM program/data select
    EECON2_ADDR = 0x18D  # EEPROM Control 2
    ADRESH_ADDR = 0x1E  # A/D Result High
    ADRESL_ADDR = 0x1F  # A/D Result Low
    ADCON0_ADDR = 0x1F  # A/D Control 0
    ADCON0_ADON_BIT = 0  # A/D enable
    ADCON0_GO_DONE_BIT = 2  # A/D conversion status
    ADCON0_CHS_BIT = 3  # Channel select
    ADCON1_ADDR = 0x9F  # A/D Control 1
    SSPSTAT_ADDR = 0x94  # MSSP Status
    SSPCON_ADDR = 0x14  # MSSP Control
    SSPBUF_ADDR = 0x13  # SSP Buffer
    TXREG_ADDR = 0x19  # USART Transmit Register
    RCREG_ADDR = 0x1A  # USART Receive Register
    SPBRG_ADDR = 0x99  # Baud Rate Generator
    TXSTA_ADDR = 0x98  # TX Status and Control
    RCSTA_ADDR = 0x18  # RX Status and Control
    CCP1CON_ADDR = 0x17  # CCP1 Control
    CCPR1L_ADDR = 0x15  # CCP1 Low
    CCPR1H_ADDR = 0x16  # CCP1 High
    CCP2CON_ADDR = 0x1D  # CCP2 Control
    CCPR2L_ADDR = 0x1B  # CCP2 Low
    CCPR2H_ADDR = 0x1C  # CCP2 High
    T1CON_ADDR = 0x10  # Timer 1 Control
    TMR1L_ADDR = 0x0E  # Timer 1 Low
    TMR1H_ADDR = 0x0F  # Timer 1 High
    T2CON_ADDR = 0x12  # Timer 2 Control
    TMR2_ADDR = 0x11  # Timer 2
    PR2_ADDR = 0x92  # Timer 2 Period

    # 内存段定义
    PROGRAM_START = 0x0000
    PROGRAM_END = 0x1FFF
    PROGRAM_SIZE = 8192  # Program Memory (8KB)
    DATA_START = 0x20
    DATA_END = 0x7F
    DATA_SIZE = 96  # General Purpose RAM Bank 0
    SRAM_START = 0xA0
    SRAM_END = 0xFF
    SRAM_SIZE = 96  # General Purpose RAM Bank 1
    EEPROM_START = 0x2100
    EEPROM_END = 0x21FF
    EEPROM_SIZE = 256  # EEPROM Data Memory

    # 外设定义
    # Port B
    GPIO_PORTB_BASE = 0x06
    GPIO_PORTB_PORTB_ADDR = 0x06
    GPIO_PORTB_TRISB_ADDR = 0x86
    # Port C
    GPIO_PORTC_BASE = 0x07
    GPIO_PORTC_PORTC_ADDR = 0x07
    GPIO_PORTC_TRISC_ADDR = 0x87
    # Port D
    GPIO_PORTD_BASE = 0x08
    GPIO_PORTD_PORTD_ADDR = 0x08
    GPIO_PORTD_TRISD_ADDR = 0x88
    # Timer 0
    TIMER0_BASE = 0x01
    TIMER0_TMR0_ADDR = 0x01
    TIMER0_OPTION_REG_ADDR = 0x81
    # Timer 1
    TIMER1_BASE = 0x0E
    TIMER1_T1CON_ADDR = 0x10
    TIMER1_TMR1L_ADDR = 0x0E
    TIMER1_TMR1H_ADDR = 0x0F
    # Timer 2
    TIMER2_BASE = 0x11
    TIMER2_T2CON_ADDR = 0x12
    TIMER2_TMR2_ADDR = 0x11
    TIMER2_PR2_ADDR = 0x92
    # A/D Converter
    ADC_BASE = 0x1E
    ADC_ADRESH_ADDR = 0x1E
    ADC_ADRESL_ADDR = 0x9F
    ADC_ADCON0_ADDR = 0x1F
    ADC_ADCON1_ADDR = 0x9F
    # Master Synchronous Serial Port
    MSSP_BASE = 0x13
    MSSP_SSPSTAT_ADDR = 0x94
    MSSP_SSPCON_ADDR = 0x14
    MSSP_SSPBUF_ADDR = 0x13
    # USART
    USART_BASE = 0x19
    USART_TXREG_ADDR = 0x19
    USART_RCREG_ADDR = 0x1A
    USART_SPBRG_ADDR = 0x99
    USART_TXSTA_ADDR = 0x98
    USART_RCSTA_ADDR = 0x18
    # Capture/Compare/PWM 1
    CCP1_BASE = 0x15
    CCP1_CCP1CON_ADDR = 0x17
    CCP1_CCPR1L_ADDR = 0x15
    CCP1_CCPR1H_ADDR = 0x16
    # Capture/Compare/PWM 2
    CCP2_BASE = 0x1B
    CCP2_CCP2CON_ADDR = 0x1D
    CCP2_CCPR2L_ADDR = 0x1B
    CCP2_CCPR2H_ADDR = 0x1C

    # 中断向量定义
    INT_INT = 1  # External Interrupt
    INT_TMR0 = 2  # Timer 0 Overflow
    INT_RB = 3  # PORTB Change
    INT_CCP1 = 4  # CCP1
    INT_CCP2 = 5  # CCP2
    INT_TMR1 = 6  # Timer 1 Overflow
    INT_TMR2 = 8  # Timer 2 Overflow
    INT_SPI = 9  # SPI/I2C
    INT_SCI = 10  # USART Receive
    INT_SCI = 11  # USART Transmit
    INT_ADC = 12  # A/D Converter
    INT_EEPROM = 13  # EEPROM Write Complete

    # 引脚定义
    PIN_MCLR_VPP = 1  # Master Clear (Reset)
    PIN_RA0_AN0 = 2  # PORTA Bit 0 / Analog 0
    PIN_RA1_AN1 = 3  # PORTA Bit 1 / Analog 1
    PIN_RA2_AN2_VREF = 4  # PORTA Bit 2 / Analog 2 / VREF-
    PIN_RA3_AN3_VREFP = 5  # PORTA Bit 3 / Analog 3 / VREF+
    PIN_RA4_T0CKI = 6  # PORTA Bit 4 / Timer 0 Clock Input
    PIN_RA5_AN4_SS = 7  # PORTA Bit 4 / Analog 4 / SPI Slave Select
    PIN_RE0_RD_AN5 = 8  # PORTE Bit 0 / Read Control / Analog 5
    PIN_RE1_WR_AN6 = 9  # PORTE Bit 1 / Write Control / Analog 6
    PIN_RE2_CS_AN7 = 10  # PORTE Bit 2 / Chip Select / Analog 7
    PIN_VDD = 11  # Positive Supply
    PIN_VSS = 12  # Ground
    PIN_OSC1_CLKIN = 13  # Oscillator/Clock Input
    PIN_OSC2_CLKOUT = 14  # Oscillator/Clock Output
    PIN_RC0_T1OSO = 15  # PORTC Bit 0 / Timer 1 Oscillator
    PIN_RC1_T1OSI = 16  # PORTC Bit 1 / Timer 1 Oscillator
    PIN_RC2_CCP1 = 17  # PORTC Bit 2 / Capture/Compare/PWM 1
    PIN_RC3_SCK_SCL = 18  # PORTC Bit 3 / SPI Clock / I2C Clock
    PIN_RC4_SDI_SDA = 23  # PORTC Bit 4 / SPI Data In / I2C Data
    PIN_RC5_SDO = 24  # PORTC Bit 5 / SPI Data Out
    PIN_RC6_TX = 25  # PORTC Bit 6 / USART Transmit
    PIN_RC7_RX = 26  # PORTC Bit 7 / USART Receive
    PIN_RD0 = 19  # PORTD Bit 0
    PIN_RD1 = 20  # PORTD Bit 1
    PIN_RD2 = 21  # PORTD Bit 2
    PIN_RD3 = 22  # PORTD Bit 3
    PIN_RD4 = 27  # PORTD Bit 4
    PIN_RD5 = 28  # PORTD Bit 5
    PIN_RD6 = 29  # PORTD Bit 6
    PIN_RD7 = 30  # PORTD Bit 7
    PIN_VSS = 31  # Ground
    PIN_VDD = 32  # Positive Supply
    PIN_RB0_INT = 33  # PORTB Bit 0 / External Interrupt
    PIN_RB1 = 34  # PORTB Bit 1
    PIN_RB2 = 35  # PORTB Bit 2
    PIN_RB3_PGC = 36  # PORTB Bit 3 / Programming Clock
    PIN_RB4_PGD = 37  # PORTB Bit 4 / Programming Data
    PIN_RB5 = 38  # PORTB Bit 5
    PIN_RB6_PGC = 39  # PORTB Bit 6 / Programming Clock
    PIN_RB7_PGD = 40  # PORTB Bit 7 / Programming Data

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
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Working Register",
            "value": 0
        }
        self._registers["STATUS"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }
        self._registers["INTCON"] = {
            "address": 0x0B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Control Register",
            "value": 0
        }
        self._registers["PORTB"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "PORT B",
            "value": 0
        }
        self._registers["TRISB"] = {
            "address": 0x86,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "TRIS B",
            "value": 0
        }
        self._registers["PORTC"] = {
            "address": 0x07,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "PORT C",
            "value": 0
        }
        self._registers["TRISC"] = {
            "address": 0x87,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "TRIS C",
            "value": 0
        }
        self._registers["PORTD"] = {
            "address": 0x08,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "PORT D",
            "value": 0
        }
        self._registers["TRISD"] = {
            "address": 0x88,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "TRIS D",
            "value": 0
        }
        self._registers["PORTE"] = {
            "address": 0x09,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "PORT E",
            "value": 0
        }
        self._registers["TRISE"] = {
            "address": 0x89,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "TRIS E",
            "value": 0
        }
        self._registers["TMR0"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 0",
            "value": 0
        }
        self._registers["OPTION_REG"] = {
            "address": 0x81,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Option Register",
            "value": 0
        }
        self._registers["PCL"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Program Counter Low",
            "value": 0
        }
        self._registers["PCLATH"] = {
            "address": 0x0A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Program Counter Latch High",
            "value": 0
        }
        self._registers["FSR"] = {
            "address": 0x04,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "File Select Register",
            "value": 0
        }
        self._registers["EEDATA"] = {
            "address": 0x10C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "EEPROM Data",
            "value": 0
        }
        self._registers["EEADR"] = {
            "address": 0x10D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "EEPROM Address",
            "value": 0
        }
        self._registers["EECON1"] = {
            "address": 0x18C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "EEPROM Control 1",
            "value": 0
        }
        self._registers["EECON2"] = {
            "address": 0x18D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "EEPROM Control 2",
            "value": 0
        }
        self._registers["ADRESH"] = {
            "address": 0x1E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Result High",
            "value": 0
        }
        self._registers["ADRESL"] = {
            "address": 0x1F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Result Low",
            "value": 0
        }
        self._registers["ADCON0"] = {
            "address": 0x1F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Control 0",
            "value": 0
        }
        self._registers["ADCON1"] = {
            "address": 0x9F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "A/D Control 1",
            "value": 0
        }
        self._registers["SSPSTAT"] = {
            "address": 0x94,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "MSSP Status",
            "value": 0
        }
        self._registers["SSPCON"] = {
            "address": 0x14,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "MSSP Control",
            "value": 0
        }
        self._registers["SSPBUF"] = {
            "address": 0x13,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "SSP Buffer",
            "value": 0
        }
        self._registers["TXREG"] = {
            "address": 0x19,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USART Transmit Register",
            "value": 0
        }
        self._registers["RCREG"] = {
            "address": 0x1A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "USART Receive Register",
            "value": 0
        }
        self._registers["SPBRG"] = {
            "address": 0x99,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Baud Rate Generator",
            "value": 0
        }
        self._registers["TXSTA"] = {
            "address": 0x98,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "TX Status and Control",
            "value": 0
        }
        self._registers["RCSTA"] = {
            "address": 0x18,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "RX Status and Control",
            "value": 0
        }
        self._registers["CCP1CON"] = {
            "address": 0x17,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP1 Control",
            "value": 0
        }
        self._registers["CCPR1L"] = {
            "address": 0x15,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP1 Low",
            "value": 0
        }
        self._registers["CCPR1H"] = {
            "address": 0x16,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP1 High",
            "value": 0
        }
        self._registers["CCP2CON"] = {
            "address": 0x1D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP2 Control",
            "value": 0
        }
        self._registers["CCPR2L"] = {
            "address": 0x1B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP2 Low",
            "value": 0
        }
        self._registers["CCPR2H"] = {
            "address": 0x1C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "CCP2 High",
            "value": 0
        }
        self._registers["T1CON"] = {
            "address": 0x10,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 Control",
            "value": 0
        }
        self._registers["TMR1L"] = {
            "address": 0x0E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 Low",
            "value": 0
        }
        self._registers["TMR1H"] = {
            "address": 0x0F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 1 High",
            "value": 0
        }
        self._registers["T2CON"] = {
            "address": 0x12,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 2 Control",
            "value": 0
        }
        self._registers["TMR2"] = {
            "address": 0x11,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 2",
            "value": 0
        }
        self._registers["PR2"] = {
            "address": 0x92,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Timer 2 Period",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GPIO_PORTB"] = {
            "base": 0x06,
            "type": "gpio",
            "description": "Port B",
            "registers": {
                "PORTB": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISB": {
                    "address": 0x86,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_PORTC"] = {
            "base": 0x07,
            "type": "gpio",
            "description": "Port C",
            "registers": {
                "PORTC": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISC": {
                    "address": 0x87,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_PORTD"] = {
            "base": 0x08,
            "type": "gpio",
            "description": "Port D",
            "registers": {
                "PORTD": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISD": {
                    "address": 0x88,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x01,
            "type": "timer",
            "description": "Timer 0",
            "registers": {
                "TMR0": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OPTION_REG": {
                    "address": 0x81,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER1"] = {
            "base": 0x0E,
            "type": "timer",
            "description": "Timer 1",
            "registers": {
                "T1CON": {
                    "address": 0x10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR1L": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR1H": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER2"] = {
            "base": 0x11,
            "type": "timer",
            "description": "Timer 2",
            "registers": {
                "T2CON": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR2": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PR2": {
                    "address": 0x92,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x1E,
            "type": "adc",
            "description": "A/D Converter",
            "registers": {
                "ADRESH": {
                    "address": 0x1E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADRESL": {
                    "address": 0x9F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCON0": {
                    "address": 0x1F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCON1": {
                    "address": 0x9F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["MSSP"] = {
            "base": 0x13,
            "type": "spi_i2c",
            "description": "Master Synchronous Serial Port",
            "registers": {
                "SSPSTAT": {
                    "address": 0x94,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPCON": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPBUF": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["USART"] = {
            "base": 0x19,
            "type": "uart",
            "description": "USART",
            "registers": {
                "TXREG": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCREG": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPBRG": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TXSTA": {
                    "address": 0x98,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCSTA": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CCP1"] = {
            "base": 0x15,
            "type": "pwm",
            "description": "Capture/Compare/PWM 1",
            "registers": {
                "CCP1CON": {
                    "address": 0x17,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR1L": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR1H": {
                    "address": 0x16,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CCP2"] = {
            "base": 0x1B,
            "type": "pwm",
            "description": "Capture/Compare/PWM 2",
            "registers": {
                "CCP2CON": {
                    "address": 0x1D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR2L": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR2H": {
                    "address": 0x1C,
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
        return f"PIC16F877A({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = PIC16F877A()
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
