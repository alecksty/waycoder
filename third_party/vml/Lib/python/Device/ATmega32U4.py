"""
ATmega32U4设备定义 - Python模块
生成自: Atmel/AVR/ATmega32U4
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
CPU架构: AVR
位宽: 8位
时钟频率: 16000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ATmega32U4:
    """ATmega32U4设备类"""

    # 设备信息
    DEVICE_NAME = "ATmega32U4"
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
    R3_ADDR = 0x03  # 
    R4_ADDR = 0x04  # 
    R5_ADDR = 0x05  # 
    R6_ADDR = 0x06  # 
    R7_ADDR = 0x07  # 
    R8_ADDR = 0x08  # 
    R9_ADDR = 0x09  # 
    R10_ADDR = 0x0A  # 
    R11_ADDR = 0x0B  # 
    R12_ADDR = 0x0C  # 
    R13_ADDR = 0x0D  # 
    R14_ADDR = 0x0E  # 
    R15_ADDR = 0x0F  # 
    R16_ADDR = 0x10  # 
    R17_ADDR = 0x11  # 
    R18_ADDR = 0x12  # 
    R19_ADDR = 0x13  # 
    R20_ADDR = 0x14  # 
    R21_ADDR = 0x15  # 
    R22_ADDR = 0x16  # 
    R23_ADDR = 0x17  # 
    R24_ADDR = 0x18  # 
    R25_ADDR = 0x19  # 
    R26_ADDR = 0x1A  # 
    R27_ADDR = 0x1B  # 
    R28_ADDR = 0x1C  # 
    R29_ADDR = 0x1D  # 
    R30_ADDR = 0x1E  # 
    R31_ADDR = 0x1F  # 
    SPL_ADDR = 0x5D  # 
    SPH_ADDR = 0x5E  # 
    SREG_ADDR = 0x5F  # 

    # 内存段定义
    FLASH_START = 0x0000
    FLASH_END = 0x7FFF
    FLASH_SIZE = 32768  # Program Flash Memory
    SRAM_START = 0x0100
    SRAM_END = 0x0AFF
    SRAM_SIZE = 2560  # Static RAM
    EEPROM_START = 0x0000
    EEPROM_END = 0x03FF
    EEPROM_SIZE = 1024  # EEPROM
    IO_START = 0x00
    IO_END = 0x3F
    IO_SIZE = 64  # I/O Registers
    EXTIO_START = 0x40
    EXTIO_END = 0xFF
    EXTIO_SIZE = 192  # Extended I/O Registers

    # 外设定义
    # Port B
    PORTB_BASE = 0x23
    PORTB_PORTB_ADDR = 0x25
    PORTB_DDRB_ADDR = 0x24
    PORTB_PINB_ADDR = 0x23
    # Port C
    PORTC_BASE = 0x26
    PORTC_PORTC_ADDR = 0x28
    PORTC_DDRC_ADDR = 0x27
    PORTC_PINC_ADDR = 0x26
    # Port D
    PORTD_BASE = 0x29
    PORTD_PORTD_ADDR = 0x2B
    PORTD_DDRD_ADDR = 0x2A
    PORTD_PIND_ADDR = 0x29
    # Port E
    PORTE_BASE = 0x2C
    PORTE_PORTE_ADDR = 0x2E
    PORTE_DDRE_ADDR = 0x2D
    PORTE_PINE_ADDR = 0x2C
    # USART1
    UART1_BASE = 0xC8
    UART1_UDR1_ADDR = 0xCE
    UART1_UCSR1A_ADDR = 0xC8
    UART1_UCSR1B_ADDR = 0xC9
    UART1_UCSR1C_ADDR = 0xCA
    UART1_UBRR1_ADDR = 0xCC
    # USB Controller
    USB_BASE = 0xD0
    USB_UDCON_ADDR = 0xD0
    USB_UDIEN_ADDR = 0xD1
    USB_UDINT_ADDR = 0xD2

    # 中断向量定义
    INT_INT0 = 1  # External Interrupt 0
    INT_INT1 = 2  # External Interrupt 1
    INT_INT2 = 3  # External Interrupt 2
    INT_INT3 = 4  # External Interrupt 3
    INT_INT4 = 5  # External Interrupt 4
    INT_INT5 = 6  # External Interrupt 5
    INT_INT6 = 7  # External Interrupt 6
    INT_PCINT0 = 8  # Pin Change Interrupt 0
    INT_USB_GENERAL = 9  # USB General
    INT_USB_ENDPOINT = 10  # USB Endpoint
    INT_WDT = 11  # Watchdog Timeout
    INT_TIMER1_CAPT = 12  # Timer1 Capture
    INT_TIMER1_COMPA = 13  # Timer1 Compare A
    INT_TIMER1_COMPB = 14  # Timer1 Compare B
    INT_TIMER1_OVF = 15  # Timer1 Overflow
    INT_TIMER0_COMPA = 16  # Timer0 Compare A
    INT_TIMER0_COMPB = 17  # Timer0 Compare B
    INT_TIMER0_OVF = 18  # Timer0 Overflow
    INT_SPI_STC = 19  # SPI Transfer Complete
    INT_UART1_RX = 20  # UART1 Receive
    INT_UART1_UDRE = 21  # UART1 Data Register Empty
    INT_UART1_TX = 22  # UART1 Transmit
    INT_ADC = 23  # ADC Conversion Complete

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
        self._registers["R3"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R4"] = {
            "address": 0x04,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R5"] = {
            "address": 0x05,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R6"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R7"] = {
            "address": 0x07,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R8"] = {
            "address": 0x08,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R9"] = {
            "address": 0x09,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R10"] = {
            "address": 0x0A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R11"] = {
            "address": 0x0B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R12"] = {
            "address": 0x0C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R13"] = {
            "address": 0x0D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R14"] = {
            "address": 0x0E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R15"] = {
            "address": 0x0F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R16"] = {
            "address": 0x10,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R17"] = {
            "address": 0x11,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R18"] = {
            "address": 0x12,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R19"] = {
            "address": 0x13,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R20"] = {
            "address": 0x14,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R21"] = {
            "address": 0x15,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R22"] = {
            "address": 0x16,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R23"] = {
            "address": 0x17,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R24"] = {
            "address": 0x18,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R25"] = {
            "address": 0x19,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R26"] = {
            "address": 0x1A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R27"] = {
            "address": 0x1B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R28"] = {
            "address": 0x1C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R29"] = {
            "address": 0x1D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R30"] = {
            "address": 0x1E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R31"] = {
            "address": 0x1F,
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
        self._peripherals["PORTB"] = {
            "base": 0x23,
            "type": "GPIO",
            "description": "Port B",
            "registers": {
                "PORTB": {
                    "address": 0x25,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0x24,
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
            "base": 0x26,
            "type": "GPIO",
            "description": "Port C",
            "registers": {
                "PORTC": {
                    "address": 0x28,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRC": {
                    "address": 0x27,
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
            "base": 0x29,
            "type": "GPIO",
            "description": "Port D",
            "registers": {
                "PORTD": {
                    "address": 0x2B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRD": {
                    "address": 0x2A,
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
            "base": 0x2C,
            "type": "GPIO",
            "description": "Port E",
            "registers": {
                "PORTE": {
                    "address": 0x2E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRE": {
                    "address": 0x2D,
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
        self._peripherals["UART1"] = {
            "base": 0xC8,
            "type": "UART",
            "description": "USART1",
            "registers": {
                "UDR1": {
                    "address": 0xCE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR1A": {
                    "address": 0xC8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR1B": {
                    "address": 0xC9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UCSR1C": {
                    "address": 0xCA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UBRR1": {
                    "address": 0xCC,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0xD0,
            "type": "USB",
            "description": "USB Controller",
            "registers": {
                "UDCON": {
                    "address": 0xD0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UDIEN": {
                    "address": 0xD1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "UDINT": {
                    "address": 0xD2,
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
        return f"ATmega32U4({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ATmega32U4()
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
