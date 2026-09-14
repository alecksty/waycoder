"""
PIC32MX170F256B设备定义 - Python模块
生成自: Microchip/PIC32/PIC32MX170F256B
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
CPU架构: MIPS32-M4K
位宽: 32位
时钟频率: 50000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class PIC32MX170F256B:
    """PIC32MX170F256B设备类"""

    # 设备信息
    DEVICE_NAME = "PIC32MX170F256B"
    MANUFACTURER = "Microchip"
    FAMILY = "PIC32"
    VERSION = "1.0"
    ARCHITECTURE = "MIPS32-M4K"
    BITS = 32
    CLOCK_FREQUENCY = 50000000

    # 寄存器地址定义
    _0_ADDR = 0x00  # Hard-wired zero
    _1_ADDR = 0x04  # AT
    _2_ADDR = 0x08  # V0
    _3_ADDR = 0x0C  # V1
    _4_ADDR = 0x10  # A0
    _5_ADDR = 0x14  # A1
    _29_ADDR = 0x74  # Stack Pointer (SP)
    _31_ADDR = 0x7C  # Return Address (RA)
    PC_ADDR = 0x80  # Program Counter

    # 内存段定义
    FLASH_START = 0x9D000000
    FLASH_END = 0x9D03FFFF
    FLASH_SIZE = 262144  # Program Flash
    SRAM_START = 0xA0000000
    SRAM_END = 0xA000FFFF
    SRAM_SIZE = 65536  # 
    PERIPHERAL_START = 0xBF800000
    PERIPHERAL_END = 0xBF8FFFFF
    PERIPHERAL_SIZE = 1048576  # 
    BOOTFLASH_START = 0xBFC00000
    BOOTFLASH_END = 0xBFC02FFF
    BOOTFLASH_SIZE = 12288  # Boot Flash

    # 外设定义
    # General Purpose I/O Port A
    PORTA_BASE = 0xBF886000
    PORTA_TRISA_ADDR = 0x00
    PORTA_PORTA_ADDR = 0x10
    PORTA_LATA_ADDR = 0x20
    PORTA_ODCA_ADDR = 0x30
    # General Purpose I/O Port B
    PORTB_BASE = 0xBF886100
    PORTB_TRISB_ADDR = 0x00
    PORTB_PORTB_ADDR = 0x10
    PORTB_LATB_ADDR = 0x20
    PORTB_ODCB_ADDR = 0x30
    # UART1
    UART1_BASE = 0xBF822000
    UART1_UXMODE_ADDR = 0x00
    UART1_UXSTA_ADDR = 0x04
    UART1_UXTXREG_ADDR = 0x08
    UART1_UXRXREG_ADDR = 0x0C
    UART1_UXBRG_ADDR = 0x10

    # 中断向量定义
    INT_RESET = 0  # 
    INT_UART1 = 8  # UART1 Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["$0"] = {
            "address": 0x00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Hard-wired zero",
            "value": 0
        }
        self._registers["$1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "AT",
            "value": 0
        }
        self._registers["$2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "V0",
            "value": 0
        }
        self._registers["$3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "V1",
            "value": 0
        }
        self._registers["$4"] = {
            "address": 0x10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "A0",
            "value": 0
        }
        self._registers["$5"] = {
            "address": 0x14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "A1",
            "value": 0
        }
        self._registers["$29"] = {
            "address": 0x74,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer (SP)",
            "value": 0
        }
        self._registers["$31"] = {
            "address": 0x7C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Return Address (RA)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x80,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PORTA"] = {
            "base": 0xBF886000,
            "type": "GPIO",
            "description": "General Purpose I/O Port A",
            "registers": {
                "TRISA": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORTA": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LATA": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODCA": {
                    "address": 0x30,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTB"] = {
            "base": 0xBF886100,
            "type": "GPIO",
            "description": "General Purpose I/O Port B",
            "registers": {
                "TRISB": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORTB": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LATB": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODCB": {
                    "address": 0x30,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0xBF822000,
            "type": "UART",
            "description": "UART1",
            "registers": {
                "UXMODE": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UXSTA": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UXTXREG": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UXRXREG": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UXBRG": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
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
        return f"PIC32MX170F256B({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = PIC32MX170F256B()
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
