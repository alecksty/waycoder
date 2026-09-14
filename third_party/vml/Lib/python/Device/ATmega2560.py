"""
ATmega2560设备定义 - Python模块
生成自: Atmel/AVR/ATmega2560
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
CPU架构: AVR
位宽: 8位
时钟频率: 16000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ATmega2560:
    """ATmega2560设备类"""

    # 设备信息
    DEVICE_NAME = "ATmega2560"
    MANUFACTURER = "Atmel"
    FAMILY = "AVR"
    VERSION = "1.0"
    ARCHITECTURE = "AVR"
    BITS = 8
    CLOCK_FREQUENCY = 16000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x01  # 
    R2_ADDR = 0x02  # 
    SPL_ADDR = 0x5D  # 
    SPH_ADDR = 0x5E  # 
    SREG_ADDR = 0x5F  # 

    # 内存段定义
    FLASH_START = 0x0000
    FLASH_END = 0x3FFFF
    FLASH_SIZE = 262144  # 
    SRAM_START = 0x0200
    SRAM_END = 0x21FF
    SRAM_SIZE = 8192  # 
    EEPROM_START = 0x0000
    EEPROM_END = 0x0FFF
    EEPROM_SIZE = 4096  # 
    IO_START = 0x00
    IO_END = 0x3F
    IO_SIZE = 64  # 
    EXTIO_START = 0x40
    EXTIO_END = 0xFF
    EXTIO_SIZE = 192  # 

    # 外设定义
    # Port A
    PORTA_BASE = 0x22
    PORTA_DDRA_ADDR = 0x21
    PORTA_PORTA_ADDR = 0x22
    PORTA_PINA_ADDR = 0x20
    # Port B
    PORTB_BASE = 0x25
    PORTB_DDRB_ADDR = 0x24
    PORTB_PORTB_ADDR = 0x25
    PORTB_PINB_ADDR = 0x23
    # Port C
    PORTC_BASE = 0x28
    PORTC_DDRC_ADDR = 0x27
    PORTC_PORTC_ADDR = 0x28
    PORTC_PINC_ADDR = 0x26
    # Port D
    PORTD_BASE = 0x2B
    PORTD_DDRD_ADDR = 0x2A
    PORTD_PORTD_ADDR = 0x2B
    PORTD_PIND_ADDR = 0x29
    # Port E
    PORTE_BASE = 0x2E
    PORTE_DDRE_ADDR = 0x2D
    PORTE_PORTE_ADDR = 0x2E
    PORTE_PINE_ADDR = 0x2C
    # Port F
    PORTF_BASE = 0x31
    PORTF_DDRF_ADDR = 0x30
    PORTF_PORTF_ADDR = 0x31
    PORTF_PINF_ADDR = 0x2F
    # Port G
    PORTG_BASE = 0x34
    PORTG_DDRG_ADDR = 0x33
    PORTG_PORTG_ADDR = 0x34
    PORTG_PING_ADDR = 0x32
    # USART 0
    USART0_BASE = 0xC0
    USART0_UDR0_ADDR = 0xC6
    USART0_UCSR0A_ADDR = 0xC0
    USART0_UCSR0B_ADDR = 0xC1
    USART0_UCSR0C_ADDR = 0xC2
    USART0_UBRR0L_ADDR = 0xC4
    USART0_UBRR0H_ADDR = 0xC5

    # 中断向量定义
    INT_RESET = 1  # 
    INT_INT0 = 2  # 
    INT_INT1 = 3  # 
    INT_INT2 = 4  # 
    INT_INT3 = 5  # 
    INT_INT4 = 6  # 
    INT_INT5 = 7  # 
    INT_INT6 = 8  # 
    INT_INT7 = 9  # 
    INT_PCINT0 = 10  # 
    INT_PCINT1 = 11  # 
    INT_PCINT2 = 12  # 
    INT_WDT = 13  # 
    INT_TIM2_COMPA = 14  # 
    INT_TIM2_COMPB = 15  # 
    INT_TIM2_OVF = 16  # 
    INT_TIM1_CAPT = 17  # 
    INT_TIM1_COMPA = 18  # 
    INT_TIM1_COMPB = 19  # 
    INT_TIM1_OVF = 20  # 
    INT_TIM0_COMPA = 21  # 
    INT_TIM0_COMPB = 22  # 
    INT_TIM0_OVF = 23  # 
    INT_SPI_STC = 24  # 
    INT_USART0_RX = 25  # 
    INT_USART0_UDRE = 26  # 
    INT_USART0_TX = 27  # 

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["R0"] = {
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SPL"] = {
            "address": 0x5D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SPH"] = {
            "address": 0x5E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SREG"] = {
            "address": 0x5F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PORTA"] = {
            "base": 0x22,
            "type": "GPIO",
            "description": "Port A",
            "registers": {
                "DDRA": {
                    "address": 0x21,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTA": {
                    "address": 0x22,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINA": {
                    "address": 0x20,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTB"] = {
            "base": 0x25,
            "type": "GPIO",
            "description": "Port B",
            "registers": {
                "DDRB": {
                    "address": 0x24,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTB": {
                    "address": 0x25,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINB": {
                    "address": 0x23,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTC"] = {
            "base": 0x28,
            "type": "GPIO",
            "description": "Port C",
            "registers": {
                "DDRC": {
                    "address": 0x27,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTC": {
                    "address": 0x28,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINC": {
                    "address": 0x26,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTD"] = {
            "base": 0x2B,
            "type": "GPIO",
            "description": "Port D",
            "registers": {
                "DDRD": {
                    "address": 0x2A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTD": {
                    "address": 0x2B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PIND": {
                    "address": 0x29,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTE"] = {
            "base": 0x2E,
            "type": "GPIO",
            "description": "Port E",
            "registers": {
                "DDRE": {
                    "address": 0x2D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTE": {
                    "address": 0x2E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINE": {
                    "address": 0x2C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTF"] = {
            "base": 0x31,
            "type": "GPIO",
            "description": "Port F",
            "registers": {
                "DDRF": {
                    "address": 0x30,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTF": {
                    "address": 0x31,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINF": {
                    "address": 0x2F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTG"] = {
            "base": 0x34,
            "type": "GPIO",
            "description": "Port G",
            "registers": {
                "DDRG": {
                    "address": 0x33,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTG": {
                    "address": 0x34,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PING": {
                    "address": 0x32,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["USART0"] = {
            "base": 0xC0,
            "type": "UART",
            "description": "USART 0",
            "registers": {
                "UDR0": {
                    "address": 0xC6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR0A": {
                    "address": 0xC0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR0B": {
                    "address": 0xC1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR0C": {
                    "address": 0xC2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UBRR0L": {
                    "address": 0xC4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UBRR0H": {
                    "address": 0xC5,
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
        return f"ATmega2560({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ATmega2560()
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
