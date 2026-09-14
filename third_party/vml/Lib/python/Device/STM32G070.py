"""
STM32G070设备定义 - Python模块
生成自: STMicroelectronics/STM32/STM32G070
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit ARM Cortex-M0+ MCU with 128KB Flash, 36KB RAM, 64MHz
CPU架构: ARM-Cortex-M0+
位宽: 32位
时钟频率: 64000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class STM32G070:
    """STM32G070设备类"""

    # 设备信息
    DEVICE_NAME = "STM32G070"
    MANUFACTURER = "STMicroelectronics"
    FAMILY = "STM32"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M0+"
    BITS = 32
    CLOCK_FREQUENCY = 64000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x04  # 
    R2_ADDR = 0x08  # 
    R3_ADDR = 0x0C  # 
    SP_ADDR = 0x34  # 
    LR_ADDR = 0x38  # 
    PC_ADDR = 0x3C  # 

    # 内存段定义
    FLASH_START = 0x08000000
    FLASH_END = 0x0801FFFF
    FLASH_SIZE = 131072  # 
    SRAM_START = 0x20000000
    SRAM_END = 0x20008FFF
    SRAM_SIZE = 36864  # 
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x4002FFFF
    PERIPHERAL_SIZE = 196608  # 

    # 外设定义
    # Reset and Clock Control
    RCC_BASE = 0x40021000
    RCC_CR_ADDR = 0x00
    RCC_CFGR_ADDR = 0x04
    RCC_AHBRSTR_ADDR = 0x18
    RCC_APBRSTR_ADDR = 0x1C
    # General Purpose I/O Port A
    GPIOA_BASE = 0x50000000
    GPIOA_MODER_ADDR = 0x00
    GPIOA_OTYPER_ADDR = 0x04
    GPIOA_OSPEEDR_ADDR = 0x08
    GPIOA_PUPDR_ADDR = 0x0C
    GPIOA_IDR_ADDR = 0x10
    GPIOA_ODR_ADDR = 0x14
    GPIOA_BSRR_ADDR = 0x18
    GPIOA_BRR_ADDR = 0x28
    # General Purpose I/O Port B
    GPIOB_BASE = 0x50000400
    GPIOB_MODER_ADDR = 0x00
    GPIOB_OTYPER_ADDR = 0x04
    GPIOB_OSPEEDR_ADDR = 0x08
    GPIOB_PUPDR_ADDR = 0x0C
    GPIOB_IDR_ADDR = 0x10
    GPIOB_ODR_ADDR = 0x14
    GPIOB_BSRR_ADDR = 0x18
    GPIOB_BRR_ADDR = 0x28
    # General Purpose I/O Port C
    GPIOC_BASE = 0x50000800
    GPIOC_MODER_ADDR = 0x00
    GPIOC_OTYPER_ADDR = 0x04
    GPIOC_IDR_ADDR = 0x10
    GPIOC_ODR_ADDR = 0x14
    GPIOC_BSRR_ADDR = 0x18

    # 中断向量定义
    INT_RESET = 0  # 
    INT_SVCALL = 11  # 

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
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["LR"] = {
            "address": 0x38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["RCC"] = {
            "base": 0x40021000,
            "type": "ResetClock",
            "description": "Reset and Clock Control",
            "registers": {
                "CR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHBRSTR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APBRSTR": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x50000000,
            "type": "GPIO",
            "description": "General Purpose I/O Port A",
            "registers": {
                "MODER": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTYPER": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OSPEEDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PUPDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOB"] = {
            "base": 0x50000400,
            "type": "GPIO",
            "description": "General Purpose I/O Port B",
            "registers": {
                "MODER": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTYPER": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OSPEEDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PUPDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOC"] = {
            "base": 0x50000800,
            "type": "GPIO",
            "description": "General Purpose I/O Port C",
            "registers": {
                "MODER": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTYPER": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x18,
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
        return f"STM32G070({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = STM32G070()
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
