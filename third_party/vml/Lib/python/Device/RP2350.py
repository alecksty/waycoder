"""
RP2350设备定义 - Python模块
生成自: Raspberry/RP2/RP2350
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
CPU架构: ARM-Cortex-M33
位宽: 32位
时钟频率: 150000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class RP2350:
    """RP2350设备类"""

    # 设备信息
    DEVICE_NAME = "RP2350"
    MANUFACTURER = "Raspberry"
    FAMILY = "RP2"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M33"
    BITS = 32
    CLOCK_FREQUENCY = 150000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x04  # 
    R2_ADDR = 0x08  # 
    R3_ADDR = 0x0C  # 
    R4_ADDR = 0x10  # 
    R5_ADDR = 0x14  # 
    SP_ADDR = 0x34  # 
    LR_ADDR = 0x38  # 
    PC_ADDR = 0x3C  # 

    # 内存段定义
    FLASH_START = 0x10000000
    FLASH_END = 0x107FFFFF
    FLASH_SIZE = 8388608  # XIP Flash
    SRAM_START = 0x20000000
    SRAM_END = 0x20081FFF
    SRAM_SIZE = 532480  # Total SRAM
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x5000FFFF
    PERIPHERAL_SIZE = 16777216  # 

    # 外设定义
    # Single-Cycle I/O (GPIO)
    SIO_BASE = 0xD0000000
    SIO_GPIO_IN_ADDR = 0x004
    SIO_GPIO_OUT_ADDR = 0x010
    SIO_GPIO_OUT_SET_ADDR = 0x014
    SIO_GPIO_OUT_CLR_ADDR = 0x018
    SIO_GPIO_OUT_XOR_ADDR = 0x01C
    SIO_GPIO_OE_ADDR = 0x020
    SIO_GPIO_OE_SET_ADDR = 0x024
    SIO_GPIO_OE_CLR_ADDR = 0x028
    # IO Bank 0 (GPIO control)
    IO_BANK0_BASE = 0x40028000
    IO_BANK0_GPIO0_STATUS_ADDR = 0x000
    IO_BANK0_GPIO0_CTRL_ADDR = 0x004
    IO_BANK0_GPIO1_STATUS_ADDR = 0x008
    IO_BANK0_GPIO1_CTRL_ADDR = 0x00C
    # Pad controls for GPIO 0-29
    PADS_BANK0_BASE = 0x4002C000
    PADS_BANK0_GPIO0_ADDR = 0x000
    PADS_BANK0_GPIO1_ADDR = 0x004
    # Reset Controller
    RESETS_BASE = 0x4000C000
    RESETS_RESET_ADDR = 0x000
    RESETS_RESET_DONE_ADDR = 0x008

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
        self._registers["R4"] = {
            "address": 0x10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R5"] = {
            "address": 0x14,
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
        self._peripherals["SIO"] = {
            "base": 0xD0000000,
            "type": "GPIO",
            "description": "Single-Cycle I/O (GPIO)",
            "registers": {
                "GPIO_IN": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_SET": {
                    "address": 0x014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_CLR": {
                    "address": 0x018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_XOR": {
                    "address": 0x01C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE": {
                    "address": 0x020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE_SET": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE_CLR": {
                    "address": 0x028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["IO_BANK0"] = {
            "base": 0x40028000,
            "type": "IOMUX",
            "description": "IO Bank 0 (GPIO control)",
            "registers": {
                "GPIO0_STATUS": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO0_CTRL": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1_STATUS": {
                    "address": 0x008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1_CTRL": {
                    "address": 0x00C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PADS_BANK0"] = {
            "base": 0x4002C000,
            "type": "PADS",
            "description": "Pad controls for GPIO 0-29",
            "registers": {
                "GPIO0": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RESETS"] = {
            "base": 0x4000C000,
            "type": "ResetControl",
            "description": "Reset Controller",
            "registers": {
                "RESET": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESET_DONE": {
                    "address": 0x008,
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
        return f"RP2350({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = RP2350()
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
