"""
CH32V003设备定义 - Python模块
生成自: WCH/CH32V0/CH32V003
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
CPU架构: RISC-V
位宽: 32位
时钟频率: 48000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class CH32V003:
    """CH32V003设备类"""

    # 设备信息
    DEVICE_NAME = "CH32V003"
    MANUFACTURER = "WCH"
    FAMILY = "CH32V0"
    VERSION = "1.0"
    ARCHITECTURE = "RISC-V"
    BITS = 32
    CLOCK_FREQUENCY = 48000000

    # 寄存器地址定义
    X1_ADDR = 0x04  # Return Address
    X2_ADDR = 0x08  # Stack Pointer (SP)
    X3_ADDR = 0x0C  # Global Pointer (GP)
    PC_ADDR = 0x3C  # Program Counter

    # 内存段定义
    FLASH_START = 0x08000000
    FLASH_END = 0x08003FFF
    FLASH_SIZE = 16384  # 
    SRAM_START = 0x20000000
    SRAM_END = 0x200007FF
    SRAM_SIZE = 2048  # 
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x40003FFF
    PERIPHERAL_SIZE = 16384  # 

    # 外设定义
    # Reset and Clock Control
    RCC_BASE = 0x40021000
    RCC_CTLR_ADDR = 0x00
    RCC_CFGR0_ADDR = 0x04
    RCC_APB2PCENR_ADDR = 0x18
    RCC_APB2PCENR_IOPAEN_BIT = 2  # GPIOA clock enable
    RCC_APB2PCENR_IOPCEN_BIT = 4  # GPIOC clock enable
    RCC_APB2PCENR_IOPDEN_BIT = 5  # GPIOD clock enable
    # General Purpose I/O Port A
    GPIOA_BASE = 0x40010800
    GPIOA_CFGLR_ADDR = 0x00
    GPIOA_CFGHR_ADDR = 0x04
    GPIOA_INDR_ADDR = 0x08
    GPIOA_OUTDR_ADDR = 0x0C
    GPIOA_BSHR_ADDR = 0x10
    GPIOA_BCR_ADDR = 0x14
    # General Purpose I/O Port C
    GPIOC_BASE = 0x40011000
    GPIOC_CFGLR_ADDR = 0x00
    GPIOC_CFGHR_ADDR = 0x04
    GPIOC_INDR_ADDR = 0x08
    GPIOC_OUTDR_ADDR = 0x0C
    GPIOC_BSHR_ADDR = 0x10
    GPIOC_BCR_ADDR = 0x14
    # General Purpose I/O Port D
    GPIOD_BASE = 0x40011400
    GPIOD_CFGLR_ADDR = 0x00
    GPIOD_CFGHR_ADDR = 0x04
    GPIOD_INDR_ADDR = 0x08
    GPIOD_OUTDR_ADDR = 0x0C
    GPIOD_BSHR_ADDR = 0x10
    GPIOD_BCR_ADDR = 0x14
    # USART1
    USART1_BASE = 0x40013800
    USART1_STATR_ADDR = 0x00
    USART1_DATAR_ADDR = 0x04
    USART1_BRR_ADDR = 0x08
    USART1_CTLR1_ADDR = 0x0C

    # 中断向量定义
    INT_RESET = 1  # 
    INT_MACHINESOFTWARE = 3  # 
    INT_MACHINETIMER = 7  # 
    INT_MACHINEEXTERNAL = 11  # 
    INT_USART1 = 25  # USART1 Global Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["x1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Return Address",
            "value": 0
        }
        self._registers["x2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer (SP)",
            "value": 0
        }
        self._registers["x3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Global Pointer (GP)",
            "value": 0
        }
        self._registers["pc"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["RCC"] = {
            "base": 0x40021000,
            "type": "ResetClock",
            "description": "Reset and Clock Control",
            "registers": {
                "CTLR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR0": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2PCENR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x40010800,
            "type": "GPIO",
            "description": "General Purpose I/O Port A",
            "registers": {
                "CFGLR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGHR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSHR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BCR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOC"] = {
            "base": 0x40011000,
            "type": "GPIO",
            "description": "General Purpose I/O Port C",
            "registers": {
                "CFGLR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGHR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSHR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BCR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOD"] = {
            "base": 0x40011400,
            "type": "GPIO",
            "description": "General Purpose I/O Port D",
            "registers": {
                "CFGLR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGHR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSHR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BCR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART1"] = {
            "base": 0x40013800,
            "type": "UART",
            "description": "USART1",
            "registers": {
                "STATR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATAR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTLR1": {
                    "address": 0x0C,
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
        return f"CH32V003({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = CH32V003()
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
