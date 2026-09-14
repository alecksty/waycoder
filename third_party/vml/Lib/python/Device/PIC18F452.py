"""
PIC18F452设备定义 - Python模块
生成自: Microchip Technology/PIC18/PIC18F452
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
CPU架构: PIC18
位宽: 8位
时钟频率: 20000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class PIC18F452:
    """PIC18F452设备类"""

    # 设备信息
    DEVICE_NAME = "PIC18F452"
    MANUFACTURER = "Microchip Technology"
    FAMILY = "PIC18"
    VERSION = "1.0"
    ARCHITECTURE = "PIC18"
    BITS = 8
    CLOCK_FREQUENCY = 20000000

    # 寄存器地址定义
    WREG_ADDR = 0xFE8  # Working Register
    STATUS_ADDR = 0xFD8  # Status Register
    BSR_ADDR = 0xFE0  # Bank Select Register
    PCL_ADDR = 0xFF9  # Program Counter Low
    PCLATH_ADDR = 0xFFA  # Program Counter Latch High
    PCLATU_ADDR = 0xFFB  # Program Counter Latch Upper
    TOSU_ADDR = 0xFFF  # Top of Stack Upper
    TOSH_ADDR = 0xFFE  # Top of Stack High
    TOSL_ADDR = 0xFFD  # Top of Stack Low

    # 外设定义
    # Port A
    PORTA_BASE = 
    PORTA_PORTA_ADDR = 0xF80
    PORTA_TRISA_ADDR = 0xF92
    PORTA_LATA_ADDR = 0xF89
    # Port B
    PORTB_BASE = 
    PORTB_PORTB_ADDR = 0xF81
    PORTB_TRISB_ADDR = 0xF93
    PORTB_LATB_ADDR = 0xF8A
    # Port C
    PORTC_BASE = 
    PORTC_PORTC_ADDR = 0xF82
    PORTC_TRISC_ADDR = 0xF94
    PORTC_LATC_ADDR = 0xF8B
    # Port D
    PORTD_BASE = 
    PORTD_PORTD_ADDR = 0xF83
    PORTD_TRISD_ADDR = 0xF95
    PORTD_LATD_ADDR = 0xF8C
    # Port E
    PORTE_BASE = 
    PORTE_PORTE_ADDR = 0xF84
    PORTE_TRISE_ADDR = 0xF96
    PORTE_LATE_ADDR = 0xF8D
    # Timer0
    TMR0_BASE = 
    TMR0_TMR0L_ADDR = 0xFD6
    TMR0_TMR0H_ADDR = 0xFD7
    TMR0_T0CON_ADDR = 0xFD5
    # Timer1
    TMR1_BASE = 
    TMR1_TMR1L_ADDR = 0xFCE
    TMR1_TMR1H_ADDR = 0xFCF
    TMR1_T1CON_ADDR = 0xFCD
    # Timer2
    TMR2_BASE = 
    TMR2_TMR2_ADDR = 0xFCC
    TMR2_PR2_ADDR = 0xFCB
    TMR2_T2CON_ADDR = 0xFCA
    # Timer3
    TMR3_BASE = 
    TMR3_TMR3L_ADDR = 0xFB2
    TMR3_TMR3H_ADDR = 0xFB3
    TMR3_T3CON_ADDR = 0xFB1
    # Analog-to-Digital Converter
    ADC_BASE = 
    ADC_ADRESL_ADDR = 0xFC3
    ADC_ADRESH_ADDR = 0xFC4
    ADC_ADCON0_ADDR = 0xFC2
    ADC_ADCON1_ADDR = 0xFC1
    # Universal Synchronous Asynchronous Receiver Transmitter
    USART_BASE = 
    USART_TXREG_ADDR = 0xFAC
    USART_RCREG_ADDR = 0xFAB
    USART_SPBRG_ADDR = 0xFAF
    USART_TXSTA_ADDR = 0xFAD
    USART_RCSTA_ADDR = 0xFAE
    # Synchronous Serial Port
    SSP_BASE = 
    SSP_SSPBUF_ADDR = 0xFC9
    SSP_SSPADD_ADDR = 0xFC8
    SSP_SSPSTAT_ADDR = 0xFC7
    SSP_SSPCON1_ADDR = 0xFC6
    SSP_SSPCON2_ADDR = 0xFC5
    # Capture/Compare/PWM 1
    CCP1_BASE = 
    CCP1_CCPR1L_ADDR = 0xFBE
    CCP1_CCPR1H_ADDR = 0xFBF
    CCP1_CCP1CON_ADDR = 0xFBD
    # Capture/Compare/PWM 2
    CCP2_BASE = 
    CCP2_CCPR2L_ADDR = 0xFBA
    CCP2_CCPR2H_ADDR = 0xFBB
    CCP2_CCP2CON_ADDR = 0xFB9

    # 中断向量定义
    INT_HIGH_PRIORITY = 8  # High priority interrupt
    INT_LOW_PRIORITY = 24  # Low priority interrupt
    INT_RESET = 0  # Reset vector

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["WREG"] = {
            "address": 0xFE8,
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
        self._registers["PCL"] = {
            "address": 0xFF9,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Program Counter Low",
            "value": 0
        }
        self._registers["PCLATH"] = {
            "address": 0xFFA,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Program Counter Latch High",
            "value": 0
        }
        self._registers["PCLATU"] = {
            "address": 0xFFB,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Program Counter Latch Upper",
            "value": 0
        }
        self._registers["TOSU"] = {
            "address": 0xFFF,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Top of Stack Upper",
            "value": 0
        }
        self._registers["TOSH"] = {
            "address": 0xFFE,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Top of Stack High",
            "value": 0
        }
        self._registers["TOSL"] = {
            "address": 0xFFD,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Top of Stack Low",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PORTA"] = {
            "base": ,
            "type": "GPIO",
            "description": "Port A",
            "registers": {
                "PORTA": {
                    "address": 0xF80,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISA": {
                    "address": 0xF92,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LATA": {
                    "address": 0xF89,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTB"] = {
            "base": ,
            "type": "GPIO",
            "description": "Port B",
            "registers": {
                "PORTB": {
                    "address": 0xF81,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISB": {
                    "address": 0xF93,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LATB": {
                    "address": 0xF8A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTC"] = {
            "base": ,
            "type": "GPIO",
            "description": "Port C",
            "registers": {
                "PORTC": {
                    "address": 0xF82,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISC": {
                    "address": 0xF94,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LATC": {
                    "address": 0xF8B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTD"] = {
            "base": ,
            "type": "GPIO",
            "description": "Port D",
            "registers": {
                "PORTD": {
                    "address": 0xF83,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISD": {
                    "address": 0xF95,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LATD": {
                    "address": 0xF8C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTE"] = {
            "base": ,
            "type": "GPIO",
            "description": "Port E",
            "registers": {
                "PORTE": {
                    "address": 0xF84,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TRISE": {
                    "address": 0xF96,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LATE": {
                    "address": 0xF8D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TMR0"] = {
            "base": ,
            "type": "Timer",
            "description": "Timer0",
            "registers": {
                "TMR0L": {
                    "address": 0xFD6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR0H": {
                    "address": 0xFD7,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T0CON": {
                    "address": 0xFD5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TMR1"] = {
            "base": ,
            "type": "Timer",
            "description": "Timer1",
            "registers": {
                "TMR1L": {
                    "address": 0xFCE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR1H": {
                    "address": 0xFCF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T1CON": {
                    "address": 0xFCD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TMR2"] = {
            "base": ,
            "type": "Timer",
            "description": "Timer2",
            "registers": {
                "TMR2": {
                    "address": 0xFCC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PR2": {
                    "address": 0xFCB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T2CON": {
                    "address": 0xFCA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TMR3"] = {
            "base": ,
            "type": "Timer",
            "description": "Timer3",
            "registers": {
                "TMR3L": {
                    "address": 0xFB2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMR3H": {
                    "address": 0xFB3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T3CON": {
                    "address": 0xFB1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": ,
            "type": "ADC",
            "description": "Analog-to-Digital Converter",
            "registers": {
                "ADRESL": {
                    "address": 0xFC3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADRESH": {
                    "address": 0xFC4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
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
            }
        }
        self._peripherals["USART"] = {
            "base": ,
            "type": "Serial",
            "description": "Universal Synchronous Asynchronous Receiver Transmitter",
            "registers": {
                "TXREG": {
                    "address": 0xFAC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCREG": {
                    "address": 0xFAB,
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
                "TXSTA": {
                    "address": 0xFAD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RCSTA": {
                    "address": 0xFAE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SSP"] = {
            "base": ,
            "type": "SPI",
            "description": "Synchronous Serial Port",
            "registers": {
                "SSPBUF": {
                    "address": 0xFC9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSPADD": {
                    "address": 0xFC8,
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
            }
        }
        self._peripherals["CCP1"] = {
            "base": ,
            "type": "PWM",
            "description": "Capture/Compare/PWM 1",
            "registers": {
                "CCPR1L": {
                    "address": 0xFBE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR1H": {
                    "address": 0xFBF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCP1CON": {
                    "address": 0xFBD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CCP2"] = {
            "base": ,
            "type": "PWM",
            "description": "Capture/Compare/PWM 2",
            "registers": {
                "CCPR2L": {
                    "address": 0xFBA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCPR2H": {
                    "address": 0xFBB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CCP2CON": {
                    "address": 0xFB9,
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
        return f"PIC18F452({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = PIC18F452()
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
