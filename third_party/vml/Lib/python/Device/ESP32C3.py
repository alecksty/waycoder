"""
ESP32-C3设备定义 - Python模块
生成自: Espressif/ESP32-C/ESP32-C3
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
CPU架构: RISC-V
位宽: 32位
时钟频率: 160000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ESP32_C3:
    """ESP32-C3设备类"""

    # 设备信息
    DEVICE_NAME = "ESP32-C3"
    MANUFACTURER = "Espressif"
    FAMILY = "ESP32-C"
    VERSION = "1.0"
    ARCHITECTURE = "RISC-V"
    BITS = 32
    CLOCK_FREQUENCY = 160000000

    # 寄存器地址定义
    X1_ADDR = 0x04  # Return Address
    X2_ADDR = 0x08  # Stack Pointer (SP)
    X3_ADDR = 0x0C  # Global Pointer (GP)
    X8_ADDR = 0x20  # Frame Pointer (FP)
    X10_ADDR = 0x28  # Function Argument (A0)
    X11_ADDR = 0x2C  # Function Argument (A1)
    PC_ADDR = 0x3C  # Program Counter

    # 内存段定义
    FLASH_START = 0x42000000
    FLASH_END = 0x427FFFFF
    FLASH_SIZE = 8388608  # Flash via Cache
    SRAM_START = 0x3FC80000
    SRAM_END = 0x3FCE3FFF
    SRAM_SIZE = 409600  # Internal SRAM
    PERIPHERAL_START = 0x60000000
    PERIPHERAL_END = 0x600FFFFF
    PERIPHERAL_SIZE = 1048576  # 

    # 外设定义
    # General Purpose I/O
    GPIO_BASE = 0x60004000
    GPIO_OUT_ADDR = 0x04
    GPIO_OUT_W1TS_ADDR = 0x08
    GPIO_OUT_W1TC_ADDR = 0x0C
    GPIO_IN_ADDR = 0x10
    GPIO_ENABLE_ADDR = 0x20
    GPIO_ENABLE_W1TS_ADDR = 0x24
    GPIO_ENABLE_W1TC_ADDR = 0x28
    # I/O MUX
    IO_MUX_BASE = 0x60009000
    IO_MUX_GPIO0_ADDR = 0x00
    IO_MUX_GPIO1_ADDR = 0x04
    IO_MUX_GPIO2_ADDR = 0x08
    IO_MUX_GPIO3_ADDR = 0x0C
    # RTC Control
    RTC_CNTL_BASE = 0x60008000
    RTC_CNTL_OPTIONS0_ADDR = 0x00
    RTC_CNTL_CLK_CONF_ADDR = 0x30

    # 中断向量定义
    INT_RESET = 1  # 
    INT_MACHINESOFTWARE = 3  # 
    INT_MACHINETIMER = 7  # 
    INT_MACHINEEXTERNAL = 11  # 

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
        self._registers["x8"] = {
            "address": 0x20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Frame Pointer (FP)",
            "value": 0
        }
        self._registers["x10"] = {
            "address": 0x28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A0)",
            "value": 0
        }
        self._registers["x11"] = {
            "address": 0x2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A1)",
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
        self._peripherals["GPIO"] = {
            "base": 0x60004000,
            "type": "GPIO",
            "description": "General Purpose I/O",
            "registers": {
                "OUT": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TS": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TC": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ENABLE": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ENABLE_W1TS": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ENABLE_W1TC": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["IO_MUX"] = {
            "base": 0x60009000,
            "type": "IOMUX",
            "description": "I/O MUX",
            "registers": {
                "GPIO0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO2": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO3": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC_CNTL"] = {
            "base": 0x60008000,
            "type": "ResetClock",
            "description": "RTC Control",
            "registers": {
                "OPTIONS0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_CONF": {
                    "address": 0x30,
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
        return f"ESP32_C3({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ESP32_C3()
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
